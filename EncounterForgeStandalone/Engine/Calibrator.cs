using System.Text.RegularExpressions;
using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class Calibrator
{
    const int AcNudgeMax = 2;
    const double DprTolerance = 0.15;
    const double HpRatioNudge = 0.3;
    const double ActionDprTolerance = 0.5;

    public static void Calibrate(Creature creature, double targetHp, double targetDpr)
    {
        var profile = CombatEstimator.EstimateCreatureProfile(creature);

        targetHp = Math.Max(1, Math.Round(targetHp));
        targetDpr = Math.Max(0, targetDpr);

        var hpRatio = profile.Hp > 0 ? targetHp / profile.Hp : 1;
        var dprRatio = profile.Dpr > 0 ? targetDpr / profile.Dpr : 1;

        creature.Stats.Hp = (int)targetHp;

        int acNudge = 0;
        if (hpRatio >= 1 + HpRatioNudge) acNudge++;
        if (hpRatio <= 1 - HpRatioNudge) acNudge--;
        if (dprRatio > 1 + DprTolerance) acNudge--;
        if (dprRatio < 1 - DprTolerance) acNudge++;
        acNudge = Math.Clamp(acNudge, -AcNudgeMax, AcNudgeMax);
        if (acNudge != 0)
            creature.Stats.Ac = Math.Max(10, creature.Stats.Ac + acNudge);

        if (creature.Actions.Count == 0) return;

        var spellDpr = CombatEstimator.EstimateSpellDpr(creature);
        var legendaryFactor = 1 + CombatEstimator.LegendaryActionMultiplier(creature) / creature.Actions.Count;
        var targetActionTotal = Math.Max(0, (targetDpr - spellDpr) / legendaryFactor);

        if (Math.Abs(dprRatio - 1) > DprTolerance)
            SwapActionForDpr(creature, targetActionTotal / creature.Actions.Count);

        var remaining = targetActionTotal - ActionsDpr(creature.Actions);
        if (Math.Abs(remaining) > ActionDprTolerance)
            ApplyFlatDamageBonus(creature.Actions, remaining);
    }

    static double ActionsDpr(List<ActionData> actions)
        => actions.Where(a => a.ResolvedDamage != null)
                  .Sum(a => CombatEstimator.DiceAverage(a.ResolvedDamage![0]))
                  * CombatEstimator.HitChance;

    static void SwapActionForDpr(Creature creature, double targetPerActionDpr)
    {
        var tier = CrEngine.GetTier(creature.Cr);
        var tierKey = $"tier{tier}";
        var crNum = CrEngine.ToNumber(creature.Cr);
        var usedIds = creature.Actions.Select(a => a.Id).ToHashSet();

        var candidates = DataLoader.Actions.Where(a =>
        {
            if (usedIds.Contains(a.Id)) return false;
            if (!a.DamageTiers?.ContainsKey(tierKey) ?? true) return false;
            if (a.CrMin.HasValue && crNum < a.CrMin) return false;
            if (a.CrMax.HasValue && crNum > a.CrMax) return false;
            if (a.ChassisAffinity != null && !a.ChassisAffinity.Contains(creature.ChassisType) && !a.ChassisAffinity.Contains("any")) return false;
            if (creature.Theme != "any" && a.Tags?.Count > 0 && !a.Tags.Contains(creature.Theme) && !a.Tags.Contains("any")) return false;
            return true;
        }).ToList();

        if (candidates.Count == 0) return;

        double ValueFn(ActionData a)
        {
            var dmg = a.DamageTiers![tierKey];
            return CombatEstimator.DiceAverage(dmg[0]) * CombatEstimator.HitChance;
        }

        var tolerance = targetPerActionDpr * 0.2;
        var close = candidates.Where(a => Math.Abs(ValueFn(a) - targetPerActionDpr) <= tolerance).ToList();
        if (close.Count == 0)
        {
            var bestDiff = candidates.Min(a => Math.Abs(ValueFn(a) - targetPerActionDpr));
            close = candidates.Where(a => Math.Abs(ValueFn(a) - targetPerActionDpr) <= bestDiff + 0.01).ToList();
        }

        var chosen = close[Random.Shared.Next(close.Count)];
        chosen.ResolvedDamage = chosen.DamageTiers![tierKey];

        var replaceIdx = 0;
        var worstDpr = double.MaxValue;
        for (int i = 0; i < creature.Actions.Count; i++)
        {
            var d = creature.Actions[i].ResolvedDamage != null
                ? CombatEstimator.DiceAverage(creature.Actions[i].ResolvedDamage![0])
                : 0;
            if (d < worstDpr) { worstDpr = d; replaceIdx = i; }
        }
        creature.Actions[replaceIdx] = chosen;
    }

    static void ApplyFlatDamageBonus(List<ActionData> actions, double totalDeltaDpr)
    {
        var perAction = totalDeltaDpr / actions.Count;
        var diceDelta = (int)Math.Round(perAction / CombatEstimator.HitChance);
        if (diceDelta == 0) return;
        foreach (var action in actions)
        {
            if (action.ResolvedDamage == null) continue;
            action.ResolvedDamage = [AddFlatModifier(action.ResolvedDamage[0], diceDelta), action.ResolvedDamage[1]];
        }
    }

    static string AddFlatModifier(string formula, int delta)
    {
        if (delta == 0) return formula;
        var m = Regex.Match(formula.Trim(), @"^(\d+d\d+)\s*([+-]\s*\d+)?$", RegexOptions.IgnoreCase);
        if (!m.Success) return formula;
        var dice = m.Groups[1].Value;
        var existing = m.Groups[2].Success ? int.Parse(m.Groups[2].Value.Replace(" ", "")) : 0;
        var diceAvg = CombatEstimator.DiceAverage(dice);
        var mod = existing + delta;
        mod = Math.Max(mod, (int)Math.Ceiling(1 - diceAvg));
        if (mod == 0) return dice;
        return $"{dice}{(mod > 0 ? "+" : "")}{mod}";
    }
}
