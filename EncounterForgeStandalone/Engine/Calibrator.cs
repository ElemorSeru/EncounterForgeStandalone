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
            ApplyDamageScale(creature.Actions, remaining);
    }

    static double ActionsDpr(List<ActionData> actions)
        => actions.Where(a => a.ResolvedDamage != null)
                  .Sum(a =>
                  {
                      var chance = a.ActionType == "save" ? CombatEstimator.SaveHitChance : CombatEstimator.HitChance;
                      var recharge = CombatEstimator.RechargeMultipliers.TryGetValue(a.Recharge ?? "", out var r) ? r : 1.0;
                      var aoe = a.AoeTargets ?? 1;
                      return CombatEstimator.DiceAverage(a.ResolvedDamage![0]) * chance * recharge * aoe;
                  });

    static void SwapActionForDpr(Creature creature, double targetPerActionDpr)
    {
        var tier = CrEngine.GetTier(creature.Cr);
        var tierKey = $"tier{tier}";
        var crNum = CrEngine.ToNumber(creature.Cr);
        var usedIds = creature.Actions.Select(a => a.Id).ToHashSet();

        // Find which slot to replace before filtering candidates.
        var replaceIdx = 0;
        var worstDpr = double.MaxValue;
        for (int i = 0; i < creature.Actions.Count; i++)
        {
            var d = creature.Actions[i].ResolvedDamage != null
                ? CombatEstimator.DiceAverage(creature.Actions[i].ResolvedDamage![0])
                : 0;
            if (d < worstDpr) { worstDpr = d; replaceIdx = i; }
        }

        // If we'd be removing the only melee attack, only consider melee replacements.
        var meleeCount = creature.Actions.Count(a => a.ActionType == "mwak");
        var replacingLastMelee = meleeCount == 1 && creature.Actions[replaceIdx].ActionType == "mwak";

        var allCandidates = DataLoader.Actions.Where(a =>
        {
            if (usedIds.Contains(a.Id)) return false;
            if (!a.DamageTiers?.ContainsKey(tierKey) ?? true) return false;
            if (a.CrMin.HasValue && crNum < a.CrMin) return false;
            if (a.CrMax.HasValue && crNum > a.CrMax) return false;
            if (a.ChassisAffinity != null && !a.ChassisAffinity.Contains(creature.ChassisType) && !a.ChassisAffinity.Contains("any")) return false;
            if (creature.Theme != "any" && a.Tags?.Count > 0 && !a.Tags.Contains(creature.Theme) && !a.Tags.Contains("any")) return false;
            return true;
        }).ToList();

        var candidates = replacingLastMelee
            ? allCandidates.Where(a => a.ActionType == "mwak").ToList()
            : allCandidates;

        if (candidates.Count == 0) return;

        double ValueFn(ActionData a)
        {
            var dmg = a.DamageTiers![tierKey];
            var chance = a.ActionType == "save" ? CombatEstimator.SaveHitChance : CombatEstimator.HitChance;
            var recharge = CombatEstimator.RechargeMultipliers.TryGetValue(a.Recharge ?? "", out var r) ? r : 1.0;
            var aoe = a.AoeTargets ?? 1;
            return CombatEstimator.DiceAverage(dmg[0]) * chance * recharge * aoe;
        }

        var tolerance = targetPerActionDpr * 0.2;
        var close = candidates.Where(a => Math.Abs(ValueFn(a) - targetPerActionDpr) <= tolerance).ToList();
        if (close.Count == 0)
        {
            var bestDiff = candidates.Min(a => Math.Abs(ValueFn(a) - targetPerActionDpr));
            close = candidates.Where(a => Math.Abs(ValueFn(a) - targetPerActionDpr) <= bestDiff + 0.01).ToList();
        }

        // prefer themed options (randomized) before touching generic pool
        var themedClose = close.Where(a => creature.Theme != "any" && (a.Tags?.Contains(creature.Theme) ?? false)).ToList();
        var themedAll = candidates.Where(a => creature.Theme != "any" && (a.Tags?.Contains(creature.Theme) ?? false)).ToList();
        ActionData chosen;
        if (themedClose.Count > 0)
        {
            chosen = themedClose[Random.Shared.Next(themedClose.Count)];
        }
        else if (themedAll.Count > 0)
        {
            // Widen to 40% and pick randomly to avoid funnelling to one action
            var wideTolerance = targetPerActionDpr * 0.4;
            var wideThemed = themedAll.Where(a => Math.Abs(ValueFn(a) - targetPerActionDpr) <= wideTolerance).ToList();
            var pool = wideThemed.Count > 0 ? wideThemed : themedAll;
            chosen = pool[Random.Shared.Next(pool.Count)];
        }
        else
        {
            chosen = close[Random.Shared.Next(close.Count)];
        }
        chosen.ResolvedDamage = chosen.DamageTiers![tierKey];

        creature.Actions[replaceIdx] = chosen;
    }

    static void ApplyDamageScale(List<ActionData> actions, double totalDeltaDpr)
    {
        if (actions.Count == 0) return;
        var perAction = totalDeltaDpr / actions.Count;
        foreach (var action in actions)
        {
            if (action.ResolvedDamage == null) continue;
            var chance = action.ActionType == "save" ? CombatEstimator.SaveHitChance : CombatEstimator.HitChance;
            var recharge = CombatEstimator.RechargeMultipliers.TryGetValue(action.Recharge ?? "", out var r) ? r : 1.0;
            var aoe = action.AoeTargets ?? 1;
            var effective = chance * recharge * aoe;
            var diceDelta = effective > 0 ? (int)Math.Round(perAction / effective) : 0;
            if (diceDelta == 0) continue;
            action.ResolvedDamage = [ScaleDamage(action.ResolvedDamage[0], diceDelta), action.ResolvedDamage[1]];
        }
    }

    // Handles high CR damage and balances others
    static string ScaleDamage(string formula, int delta)
    {
        if (delta == 0) return formula;
        var m = Regex.Match(formula.Trim(), @"^(\d+)d(\d+)\s*([+-]\s*\d+)?$", RegexOptions.IgnoreCase);
        if (!m.Success) return formula;
        var count = int.Parse(m.Groups[1].Value);
        var sides = int.Parse(m.Groups[2].Value);
        var existing = m.Groups[3].Success ? int.Parse(m.Groups[3].Value.Replace(" ", "")) : 0;
        var faceAvg = (sides + 1) / 2.0;
        var total = count * faceAvg + existing + delta;
        var newCount = Math.Max(1, (int)Math.Round(total / faceAvg));
        var remainder = (int)Math.Round(total - newCount * faceAvg);
        if (remainder == 0) return $"{newCount}d{sides}";
        return $"{newCount}d{sides}{(remainder > 0 ? "+" : "")}{remainder}";
    }
}
