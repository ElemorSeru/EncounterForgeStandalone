using System.Text.RegularExpressions;
using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class CombatEstimator
{
    public const double HitChance = 0.65;
    public const double SaveHitChance = 0.5;
    public static readonly Dictionary<string, double> RechargeMultipliers = new()
    {
        ["5-6"] = 0.4,
        ["5"] = 0.5,
        ["6"] = 0.35
    };
    const double PartyHpBase = 10;
    const double PartyHpPerLevel = 6.5;
    const double IntensityStep = 0.12;
    const double MinEnemyHp = 5;
    const double MinEnemyDpr = 1;
    const double ActionEconomyStep = 0.04;
    const double ActionEconomyCap = 1.2;
    const double SoloHpFactor = 1.5;
    const double SoloDprFactor = 1.3;

    static readonly Dictionary<string, (double Defeat, double Threaten)> DefaultTargets = new()
    {
        ["easy"] = (2.0, 6.0),
        ["medium"] = (3.0, 5.4),
        ["hard"] = (4.0, 4.4),
        ["deadly"] = (5.0, 3.5)
    };

    public static double DiceAverage(string? formula)
    {
        if (string.IsNullOrEmpty(formula)) return 0;
        var m = Regex.Match(formula.Trim(), @"^(\d+)d(\d+)\s*([+-]\s*\d+)?", RegexOptions.IgnoreCase);
        if (!m.Success)
            return double.TryParse(formula, out var flat) ? flat : 0;
        var count = int.Parse(m.Groups[1].Value);
        var sides = int.Parse(m.Groups[2].Value);
        var mod = m.Groups[3].Success ? double.Parse(m.Groups[3].Value.Replace(" ", "")) : 0;
        return count * (sides + 1) / 2.0 + mod;
    }

    static (double Defeat, double Threaten) AdjustedTargets(string difficulty, int intensityOffset)
    {
        var d = DefaultTargets.TryGetValue(difficulty, out var t) ? t : DefaultTargets["medium"];
        if (intensityOffset == 0) return d;
        var factor = 1 + intensityOffset * IntensityStep;
        return (d.Defeat, Math.Round(d.Threaten / factor, 2));
    }

    public static EncounterEnvelope ComputeEnvelope(PartyEstimate party, string difficulty, int count, bool isSolo, int intensityOffset = 0)
    {
        var (rtd, rtt) = AdjustedTargets(difficulty, intensityOffset);
        var n = Math.Max(count, 1);
        var groupHp = party.Dpr * rtd;
        var economy = Math.Min(1 + (n - 1) * ActionEconomyStep, ActionEconomyCap);
        var groupDpr = (party.Hp / rtt) * economy;
        var perHp = groupHp / n;
        var perDpr = groupDpr / n;
        if (isSolo) { perHp *= SoloHpFactor; perDpr *= SoloDprFactor; }
        perHp = Math.Max(MinEnemyHp, perHp);
        perDpr = Math.Max(MinEnemyDpr, perDpr);
        return new EncounterEnvelope(perHp * n, perDpr * n, perHp, perDpr);
    }

    public static string NearestCrForStats(double hp, double dpr)
    {
        var baseline = DataLoader.CrBaseline;
        string best = "0";
        double bestDist = double.MaxValue;
        foreach (var (cr, stats) in baseline)
        {
            var d = Math.Abs(hp - stats.Hp) / Math.Max(stats.Hp, 1)
                  + Math.Abs(dpr - stats.Dpr) / Math.Max(stats.Dpr, 1);
            if (d < bestDist) { bestDist = d; best = cr; }
        }
        return best;
    }

    public static PartyEstimate EstimatePartyGeneric(int playerCount, int playerLevel)
    {
        var level = Math.Clamp(playerLevel, 1, 20);
        var classes = DataLoader.ClassDpr.Classes.Values.ToList();
        var avgDpr = classes.Average(c => c.Levels[level - 1]);
        var hpPerChar = PartyHpBase + (level - 1) * PartyHpPerLevel;
        return new PartyEstimate(avgDpr * playerCount, hpPerChar * playerCount);
    }

    public static CreatureProfile EstimateCreatureProfile(Creature creature)
    {
        var hp = creature.Stats?.Hp ?? 0;
        var ac = creature.Stats?.Ac ?? 0;
        var actionDpr = creature.Actions
            .Where(a => a.ResolvedDamage != null)
            .Sum(a =>
            {
                var chance = a.ActionType == "save" ? SaveHitChance : HitChance;
                var recharge = RechargeMultipliers.TryGetValue(a.Recharge ?? "", out var r) ? r : 1.0;
                var aoe = a.AoeTargets ?? 1;
                return DiceAverage(a.ResolvedDamage![0]) * chance * recharge * aoe;
            });
        var perAction = creature.Actions.Count > 0 ? actionDpr / creature.Actions.Count : 0;
        var legendaryDpr = creature.Solo ? EstimateLegendaryDpr(creature, perAction) : 0;
        var spellDpr = EstimateSpellDpr(creature);
        return new CreatureProfile(actionDpr + legendaryDpr + spellDpr, hp, ac);
    }

    public static double LegendaryActionMultiplier(Creature creature)
    {
        if (!creature.Solo) return 0;
        var la = creature.Traits.Where(t => t.LegendaryType == "action").ToList();
        if (la.Count == 0) return 0;
        var avgCost = la.Average(t => t.LegendaryCost ?? 1);
        return avgCost > 0 ? 3.0 / avgCost : 0;
    }

    static double EstimateLegendaryDpr(Creature creature, double perActionDpr)
    {
        var multi = LegendaryActionMultiplier(creature);
        return perActionDpr * multi;
    }

    public static double EstimateSpellDpr(Creature creature)
    {
        if (creature.SpellInfo == null) return 0;
        var tier = CrEngine.GetTier(creature.Cr);
        var dice = tier <= 1 ? 1 : tier <= 3 ? 2 : tier <= 5 ? 3 : 4;
        return dice * 5 * HitChance * 0.3;
    }

    public static EncounterRounds EstimateRounds(PartyEstimate party, double groupHp, double groupDpr)
    {
        var rtd = party.Dpr > 0 ? groupHp / party.Dpr : 99.0;
        var rtt = groupDpr > 0 ? party.Hp / groupDpr : 99.0;
        return new EncounterRounds(
            Math.Max(1, Math.Round(rtd, 1)),
            Math.Max(1, Math.Round(rtt, 1)));
    }

    public static string EstimateOutcome(EncounterRounds rounds)
    {
        var ratio = rounds.RoundsToThreaten / rounds.RoundsToDefeat;
        if (ratio >= 2.5) return "easy";
        if (ratio >= 1.5) return "manageable";
        if (ratio >= 0.9) return "risky";
        return "dangerous";
    }

    public static List<DifficultyPreviewRow> BuildDifficultyPreview(int intensityOffset)
    {
        var party = EstimatePartyGeneric(4, 5);
        var diffs = new[] { "easy", "medium", "hard", "deadly" };
        var defaultRows = diffs.Select(d =>
        {
            var env = ComputeEnvelope(party, d, 1, false, 0);
            return EstimateRounds(party, env.GroupHp, env.GroupDpr);
        }).ToList();

        return diffs.Select((d, i) =>
        {
            var env = ComputeEnvelope(party, d, 1, false, intensityOffset);
            var rounds = EstimateRounds(party, env.GroupHp, env.GroupDpr);
            var outcome = EstimateOutcome(rounds);
            return new DifficultyPreviewRow(d, rounds.RoundsToDefeat, rounds.RoundsToThreaten,
                defaultRows[i].RoundsToDefeat, defaultRows[i].RoundsToThreaten, outcome);
        }).ToList();
    }
}

public record EncounterEnvelope(double GroupHp, double GroupDpr, double PerEnemyHp, double PerEnemyDpr);

public record DifficultyPreviewRow(
    string Difficulty, double Defeat, double Threaten,
    double DefaultDefeat, double DefaultThreaten, string Outcome);
