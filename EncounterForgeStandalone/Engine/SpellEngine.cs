using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class SpellEngine
{
    static readonly Dictionary<string, (bool Always, double Chance)> SpellcasterChassis = new()
    {
        ["controller"] = (true, 1.0),
        ["leader"] = (true, 1.0),
        ["artillery"] = (false, 0.50)
    };

    static readonly Dictionary<int, (int Cantrips, int Leveled)> SpellCount = new()
    {
        [1] = (2, 1), [2] = (2, 1), [3] = (2, 2),
        [4] = (2, 2), [5] = (2, 3), [6] = (2, 3)
    };

    static readonly Dictionary<string, string> CastingStatLabel = new()
    {
        ["int"] = "Intelligence", ["wis"] = "Wisdom", ["cha"] = "Charisma"
    };

    static bool IsSpellcaster(string chassisType)
    {
        if (!SpellcasterChassis.TryGetValue(chassisType, out var cfg)) return false;
        return cfg.Always || Random.Shared.NextDouble() < cfg.Chance;
    }

    static List<SpellData> FilterPool(List<SpellData> pool, string theme, int tier)
        => pool.Where(s =>
        {
            if (s.TierMin.HasValue && tier < s.TierMin) return false;
            if (s.TierMax.HasValue && tier > s.TierMax) return false;
            if (theme == "any") return true;
            if (s.Tags == null || s.Tags.Count == 0) return true;
            return s.Tags.Contains(theme) || s.Tags.Contains("any");
        }).ToList();

    static List<SpellData> PickN(List<SpellData> pool, int n)
        => pool.OrderBy(_ => Random.Shared.NextDouble()).Take(n).ToList();

    public static SpellInfo? SelectSpells(string theme, int tier, string chassisType, int profBonus, int intStat, int wisStat, int chaStat)
    {
        if (!IsSpellcaster(chassisType)) return null;

        var pool = DataLoader.Spells;
        var filtered = FilterPool(pool, theme, tier);
        var counts = SpellCount[Math.Min(tier, 6)];

        var cantrips = PickN(filtered.Where(s => s.Cantrip).ToList(), counts.Cantrips);
        var leveled = PickN(filtered.Where(s => !s.Cantrip).ToList(), counts.Leveled);

        var stats = new[] { ("int", intStat), ("wis", wisStat), ("cha", chaStat) };
        var (castingStat, statVal) = stats.OrderByDescending(x => x.Item2).First();
        var castingMod = (statVal - 10) / 2;
        var spellDc = 8 + profBonus + castingMod;
        var spellBonus = profBonus + castingMod;

        return new SpellInfo
        {
            Cantrips = cantrips,
            Leveled = leveled,
            CastingStat = castingStat,
            SpellDc = spellDc,
            SpellBonus = spellBonus
        };
    }

    public static string GetCastingStatLabel(string stat)
        => CastingStatLabel.TryGetValue(stat, out var label) ? label : "Intelligence";

    public static string BuildSpellcastingText(SpellInfo info, string creatureName)
    {
        var stat = GetCastingStatLabel(info.CastingStat);
        var bonus = info.SpellBonus >= 0 ? $"+{info.SpellBonus}" : $"{info.SpellBonus}";
        var cantripNames = info.Cantrips.Count > 0
            ? string.Join(", ", info.Cantrips.Select(s => Capitalize(s.Name)))
            : "None";
        var leveledNames = info.Leveled.Count > 0
            ? string.Join(", ", info.Leveled.Select(s => Capitalize(s.Name)))
            : "None";

        return $"Spellcasting. {creatureName} uses {stat} as its spellcasting ability (spell save DC {info.SpellDc}, {bonus} to hit with spell attacks). " +
               $"Cantrips (at will): {cantripNames}. " +
               $"1st level (3 slots): {leveledNames}.";
    }

    static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];
}
