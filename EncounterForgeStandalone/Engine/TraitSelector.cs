using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class TraitSelector
{
    static List<TraitData> Filter(List<TraitData> pool, string cr, string theme)
    {
        var crNum = CrEngine.ToNumber(cr);
        return pool.Where(t =>
        {
            if (t.CrMin > 0 && crNum < t.CrMin) return false;
            if (t.CrMax > 0 && crNum > t.CrMax) return false;
            if (theme == "any") return true;
            if (t.Tags.Count == 0) return true;
            return t.Tags.Contains(theme) || t.Tags.Contains("any");
        }).ToList();
    }

    public static List<TraitData> SelectTraits(List<TraitData> pool, string cr, string theme, int targetCount, double maxCrAdjust)
    {
        var available = Filter(pool, cr, theme);
        var selected = new List<TraitData>();
        var usedIds = new HashSet<string>();
        double totalAdjust = 0;

        foreach (var trait in available.OrderBy(_ => Random.Shared.NextDouble()))
        {
            if (selected.Count >= targetCount) break;
            if (usedIds.Contains(trait.Id)) continue;
            var adj = trait.CrAdjustment;
            if (totalAdjust + adj > maxCrAdjust) continue;
            selected.Add(trait);
            usedIds.Add(trait.Id);
            totalAdjust += adj;
        }
        return selected;
    }
}
