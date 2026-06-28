using System.Text.RegularExpressions;
using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class CreatureAssembler
{
    static readonly Dictionary<string, (string[] Primary, string[] Secondary)> ThemeSkillProfile = new()
    {
        ["beast"] = (["prc", "sur"], ["ste"]),
        ["undead"] = (["prc", "ste"], []),
        ["aberration"] = (["prc"], ["ste", "arc"]),
        ["humanoid"] = (["prc"], ["ath", "ste", "itm", "per", "ins"]),
        ["elemental"] = (["ath"], ["prc"]),
        ["fey"] = (["prc", "ste"], ["dec", "per"]),
        ["fiend"] = (["dec", "prc"], ["itm", "ins"]),
        ["dragon"] = (["prc", "ste"], ["his"]),
        ["construct"] = (["ath", "prc"], []),
        ["monstrosity"] = (["prc", "sur"], ["ath", "ste"]),
        ["any"] = (["prc"], [])
    };

    static readonly Dictionary<string, string[]> ChassisSkillBonus = new()
    {
        ["brute"] = ["ath"],
        ["lurker"] = ["ste", "prc"],
        ["skirmisher"] = ["acr", "ath"],
        ["controller"] = ["arc", "ins"],
        ["artillery"] = ["arc", "inv"],
        ["leader"] = ["per", "ins", "itm"]
    };

    static readonly Dictionary<string, (int Darkvision, int Blindsight, int Tremorsense, int Truesight)> ThemeSenses = new()
    {
        ["beast"] = (0, 0, 0, 0),
        ["undead"] = (60, 0, 0, 0),
        ["aberration"] = (60, 30, 0, 0),
        ["humanoid"] = (0, 0, 0, 0),
        ["elemental"] = (60, 0, 30, 0),
        ["fey"] = (60, 0, 0, 0),
        ["fiend"] = (120, 0, 0, 0),
        ["dragon"] = (120, 30, 0, 0),
        ["construct"] = (60, 60, 0, 0),
        ["monstrosity"] = (60, 0, 0, 0),
        ["any"] = (30, 0, 0, 0)
    };

    static readonly Dictionary<string, List<(string Type, double Chance, int Base)>> ThemeSpeedExtras = new()
    {
        ["beast"] = [("swim", 0.30, 30)],
        ["undead"] = [("fly", 0.20, 30)],
        ["aberration"] = [("swim", 0.30, 30), ("climb", 0.30, 20)],
        ["humanoid"] = [],
        ["elemental"] = [("swim", 0.25, 40), ("fly", 0.25, 40)],
        ["fey"] = [("fly", 0.40, 40)],
        ["fiend"] = [("fly", 0.40, 40)],
        ["dragon"] = [("fly", 0.85, 60), ("swim", 0.30, 30)],
        ["construct"] = [],
        ["monstrosity"] = [("swim", 0.20, 30), ("climb", 0.20, 20)],
        ["any"] = []
    };

    static readonly string[] SizeOrder = ["tiny", "sm", "med", "lg", "huge", "grg"];

    static readonly Dictionary<string, string[]> ChassisSizeByTier = new()
    {
        ["brute"] = ["med", "lg", "lg", "huge", "huge", "huge"],
        ["lurker"] = ["med", "med", "med", "med", "lg", "lg"],
        ["skirmisher"] = ["med", "med", "lg", "lg", "lg", "lg"],
        ["controller"] = ["med", "med", "med", "med", "lg", "lg"],
        ["artillery"] = ["med", "med", "med", "med", "lg", "lg"],
        ["leader"] = ["med", "med", "med", "lg", "lg", "lg"]
    };

    static readonly Dictionary<string, string> ThemeSizeCap = new()
    {
        ["humanoid"] = "med", ["fey"] = "lg", ["undead"] = "lg", ["fiend"] = "lg",
        ["aberration"] = "huge", ["beast"] = "huge", ["dragon"] = "grg",
        ["elemental"] = "huge", ["construct"] = "huge", ["monstrosity"] = "huge", ["any"] = "huge"
    };

    static readonly Dictionary<string, double> SmallChance = new()
    {
        ["humanoid"] = 0.20, ["fey"] = 0.30, ["beast"] = 0.25
    };

    public static Creature Assemble(string cr, string theme, string? forceName, bool isSolo, bool isSummon = false, bool dprFirst = false, double targetDpr = 0)
    {
        var chassis = PickChassis(theme);
        var tier = CrEngine.GetTier(cr);
        var crNum = CrEngine.ToNumber(cr);
        var profBonus = CrEngine.GetProfBonus(cr);
        var tierKey = $"tier{tier}";
        var stats = chassis.Tiers.TryGetValue(tierKey, out var t) ? t : chassis.Tiers.Values.First();

        var traitCount = Math.Min(1 + tier / 2, 4);
        double maxCrAdjust = tier <= 2 ? 1 : tier <= 4 ? 2 : 3;

        var traits = TraitSelector.SelectTraits(DataLoader.Traits, cr, theme, traitCount, maxCrAdjust);

        if (tier >= 5 || isSolo)
        {
            var legendary = DataLoader.LegendaryTraits;
            if (isSolo)
            {
                var actionPool = legendary.Where(t2 => t2.LegendaryType == "action").ToList();
                var otherPool = legendary.Where(t2 => t2.LegendaryType != "action").ToList();
                traits = [..traits, ..TraitSelector.SelectTraits(actionPool, cr, theme, 1, 0),
                                    ..TraitSelector.SelectTraits(otherPool, cr, theme, 1, 0)];
            }
            else
            {
                traits = [..traits, ..TraitSelector.SelectTraits(legendary, cr, theme, 1, 0)];
            }
        }

        var actionCount = isSolo ? 3 : tier <= 1 ? 1 : tier <= 3 ? 2 : 3;
        var excludeIds = (isSolo || isSummon) ? new HashSet<string> { "summon_lesser" } : new HashSet<string>();
        var actions = PickActions(cr, theme, chassis.Type, actionCount, tierKey, crNum, excludeIds);

        var spellInfo = SpellEngine.SelectSpells(theme, tier, chassis.Type, profBonus,
            stats.Int, stats.Wis, stats.Cha);

        if (dprFirst && targetDpr > 0 && actions.Count > 0)
            ApplyDprFirst(actions, spellInfo, traits, tier, isSolo, targetDpr);

        var name = !string.IsNullOrWhiteSpace(forceName) ? forceName.Trim() : GenerateName(theme);
        var extras = GenerateExtras(theme, chassis.Type, tier);

        var finalAc = stats.Ac;
        var finalHp = stats.Hp;
        var resistances = new List<string>(stats.Resistances);
        var immunities = new List<string>(stats.Immunities);
        var condImmunities = new List<string>(stats.ConditionImmunities);
        var speeds = new Dictionary<string, int>(stats.Speeds);

        foreach (var (speedType, val) in extras.SpeedExtras)
        {
            if (!speeds.TryGetValue(speedType, out var current) || current < val)
                speeds[speedType] = val;
        }

        foreach (var trait in traits)
        {
            var fx = trait.Effect;
            if (fx == null) continue;
            if (fx.AcBonus.HasValue) finalAc += fx.AcBonus.Value;
            if (fx.HpBonus.HasValue) finalHp += fx.HpBonus.Value;
            if (fx.Resistance != null) resistances.AddRange(fx.Resistance);
            if (fx.Immunity != null) immunities.AddRange(fx.Immunity);
            if (fx.ConditionImmunity != null) condImmunities.AddRange(fx.ConditionImmunity);
            if (fx.Speed != null)
            {
                foreach (var (k, v) in fx.Speed) speeds[k] = v;
            }
        }

        if (isSolo) finalAc += 2;

        return new Creature
        {
            Name = name,
            Cr = cr,
            Theme = theme,
            ChassisType = chassis.Type,
            ProfBonus = profBonus,
            Skills = extras.Skills,
            Senses = extras.Senses,
            Stats = new CreatureStats
            {
                Str = stats.Str, Dex = stats.Dex, Con = stats.Con,
                Int = stats.Int, Wis = stats.Wis, Cha = stats.Cha,
                Hp = finalHp, Ac = finalAc,
                Size = ResolveSize(theme, chassis.Type, tier),
                Speeds = speeds
            },
            Traits = traits,
            Actions = actions,
            Resistances = resistances.Distinct().ToList(),
            Immunities = immunities.Distinct().ToList(),
            ConditionImmunities = condImmunities.Distinct().ToList(),
            SpellInfo = spellInfo,
            Solo = isSolo
        };
    }

    static readonly HashSet<string> AttackTypes = ["mwak", "rwak", "rsak"];

    static void FisherYates(List<ActionData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // themed entries interleaved 75/25 before generic "any" fallbacks — mirrors JS themedFirst()
    static List<ActionData> ThemedFirst(List<ActionData> pool, string theme)
    {
        var themed = pool.Where(a => theme != "any" && (a.Tags?.Contains(theme) ?? false)).ToList();
        var generic = pool.Where(a => !(theme != "any" && (a.Tags?.Contains(theme) ?? false))).ToList();
        FisherYates(themed);
        FisherYates(generic);
        var result = new List<ActionData>();
        int t = 0, g = 0;
        while (t < themed.Count || g < generic.Count)
        {
            if (t < themed.Count && (g >= generic.Count || Random.Shared.NextDouble() < 0.75))
                result.Add(themed[t++]);
            else
                result.Add(generic[g++]);
        }
        return result;
    }

    static ChassisData PickChassis(string theme)
    {
        var filtered = theme == "any"
            ? DataLoader.Chassis
            : DataLoader.Chassis.Where(c => c.Themes.Contains(theme) || c.Themes.Contains("any")).ToList();
        var pool = filtered.Count > 0 ? filtered : DataLoader.Chassis;
        return pool[Random.Shared.Next(pool.Count)];
    }

    static List<ActionData> PickActions(string cr, string theme, string chassisType, int count, string tierKey, double crNum, HashSet<string>? excludeIds = null)
    {
        var filtered = DataLoader.Actions.Where(a =>
        {
            if (excludeIds != null && excludeIds.Contains(a.Id)) return false;
            if (a.CrMin.HasValue && crNum < a.CrMin) return false;
            if (a.CrMax.HasValue && crNum > a.CrMax) return false;
            if (a.ChassisAffinity != null && !a.ChassisAffinity.Contains(chassisType) && !a.ChassisAffinity.Contains("any")) return false;
            if (theme != "any" && a.Tags?.Count > 0 && !a.Tags.Contains(theme) && !a.Tags.Contains("any")) return false;
            return true;
        }).ToList();

        bool isArtillery = chassisType == "artillery";
        var meleePool = ThemedFirst(filtered.Where(a => a.ActionType == "mwak").ToList(), theme);
        var rangedPool = ThemedFirst(filtered.Where(a => AttackTypes.Contains(a.ActionType) && a.ActionType != "mwak").ToList(), theme);
        var specialPool = ThemedFirst(filtered.Where(a => !AttackTypes.Contains(a.ActionType)).ToList(), theme);

        // Artillery archetype leads with ranged; all other chassis guarantee a melee attack first.
        var primaryPool = isArtillery ? rangedPool : meleePool;
        var secondaryPool = isArtillery ? meleePool : rangedPool;

        ActionData? firstPick = primaryPool.Count > 0 ? primaryPool[0] : (secondaryPool.Count > 0 ? secondaryPool[0] : null);
        int primarySkip = firstPick != null && primaryPool.Count > 0 ? 1 : 0;
        int secondarySkip = firstPick != null && primaryPool.Count == 0 ? 1 : 0;

        if (firstPick == null)
        {
            // Loose fallback: re-query entire dataset ignoring theme/chassis, only CR bounds and excludes.
            var looseMelee = DataLoader.Actions.Where(a =>
                (excludeIds == null || !excludeIds.Contains(a.Id)) &&
                (!a.CrMin.HasValue || crNum >= a.CrMin) &&
                (!a.CrMax.HasValue || crNum <= a.CrMax) &&
                a.ActionType == "mwak").ToList();
            FisherYates(looseMelee);
            var looseRanged = DataLoader.Actions.Where(a =>
                (excludeIds == null || !excludeIds.Contains(a.Id)) &&
                (!a.CrMin.HasValue || crNum >= a.CrMin) &&
                (!a.CrMax.HasValue || crNum <= a.CrMax) &&
                AttackTypes.Contains(a.ActionType) && a.ActionType != "mwak").ToList();
            FisherYates(looseRanged);
            var lp = isArtillery ? looseRanged : looseMelee;
            var ls = isArtillery ? looseMelee : looseRanged;
            firstPick = lp.Count > 0 ? lp[0] : (ls.Count > 0 ? ls[0] : null);
        }

        var actions = new List<ActionData>();
        if (firstPick != null) actions.Add(firstPick);
        var specialSlots = Math.Min(count - 1, specialPool.Count);
        actions.AddRange(specialPool.Take(specialSlots));
        var remaining = count - actions.Count;
        if (remaining > 0) actions.AddRange(primaryPool.Skip(primarySkip).Take(remaining));
        if (actions.Count < count) actions.AddRange(secondaryPool.Skip(secondarySkip).Take(count - actions.Count));

        foreach (var action in actions)
            action.ResolvedDamage = (action.DamageTiers?.TryGetValue(tierKey, out var d) == true ? d : null) ?? action.DamageFallback;

        return actions;
    }

    static (Dictionary<string, int> Skills, Dictionary<string, int> Senses, Dictionary<string, int> SpeedExtras)
        GenerateExtras(string theme, string chassisType, int tier)
    {
        var skills = new Dictionary<string, int>();
        var profile = ThemeSkillProfile.TryGetValue(theme, out var p) ? p : ThemeSkillProfile["any"];
        var chassisBonus = ChassisSkillBonus.TryGetValue(chassisType, out var cb) ? cb : [];
        bool canExpert = tier >= 4;

        foreach (var s in profile.Primary)
            skills[s] = canExpert ? 2 : 1;
        foreach (var s in profile.Secondary)
            if (Random.Shared.NextDouble() < 0.5) skills[s] = 1;
        foreach (var s in chassisBonus)
            if (!skills.ContainsKey(s)) skills[s] = 1;

        var rawSenses = ThemeSenses.TryGetValue(theme, out var sens) ? sens : ThemeSenses["any"];
        var senses = new Dictionary<string, int>();
        int dv = rawSenses.Darkvision, bs = rawSenses.Blindsight, ts = rawSenses.Tremorsense;
        if (tier >= 3)
        {
            if (dv > 0 && dv < 120) dv = Math.Min(dv + 30, 120);
            if (bs > 0) bs = Math.Min(bs + 15, 60);
            if (ts > 0) ts = Math.Min(ts + 15, 60);
        }
        if (tier >= 5 && new[] { "fiend", "dragon", "aberration" }.Contains(theme))
            senses["truesight"] = 10;
        if (dv > 0) senses["darkvision"] = dv;
        if (bs > 0) senses["blindsight"] = bs;
        if (ts > 0) senses["tremorsense"] = ts;

        var speedExtras = new Dictionary<string, int>();
        if (ThemeSpeedExtras.TryGetValue(theme, out var extras))
        {
            foreach (var (type, chance, baseVal) in extras)
            {
                if (Random.Shared.NextDouble() < chance)
                    speedExtras[type] = baseVal + (tier - 1) / 2 * 10;
            }
        }

        return (skills, senses, speedExtras);
    }

    static string ResolveSize(string theme, string chassisType, int tier)
    {
        if (tier == 1 && SmallChance.TryGetValue(theme, out var chance) && Random.Shared.NextDouble() < chance)
            return "sm";
        var tierSizes = ChassisSizeByTier.TryGetValue(chassisType, out var ts) ? ts : ChassisSizeByTier["controller"];
        var baseSize = tierSizes[Math.Min(tier - 1, 5)];
        var cap = ThemeSizeCap.TryGetValue(theme, out var c) ? c : "huge";
        var capIdx = Array.IndexOf(SizeOrder, cap);
        var baseIdx = Array.IndexOf(SizeOrder, baseSize);
        return SizeOrder[Math.Min(baseIdx, capIdx)];
    }

    static string Pick(List<string> list) => list[Random.Shared.Next(list.Count)];

    static string GenerateName(string theme)
    {
        var names = DataLoader.Names;
        var types = DataLoader.CreatureTypes;
        var typePool = types.TryGetValue(theme, out var tp) && tp.Count > 0 ? tp : types["any"];
        var noun = Pick(typePool);

        if (theme == "humanoid" && names.FirstNames?.Count > 0)
        {
            var first = Pick(names.FirstNames);
            var baseName = $"{first} {noun}";
            if (names.Titles?.Count > 0 && Random.Shared.NextDouble() < 0.30)
                return $"{Pick(names.Titles)} {baseName}";
            return baseName;
        }

        List<string> prefixPool = (names.ThemePrefixes?.TryGetValue(theme, out var tp2) == true && tp2.Count > 0)
            ? tp2 : names.Prefixes;
        return $"{Pick(prefixPool)} {noun}";
    }

    // Replaces tier-based dice counts with values computed directly from the DPR target.
    // Mirrors the module's dprFirstDamage path: targetPerActionDPR → computeDiceFromTarget().
    static void ApplyDprFirst(List<ActionData> actions, SpellInfo? spellInfo, List<TraitData> traits, int tier, bool isSolo, double targetDpr)
    {
        var spellDpr = spellInfo != null
            ? (tier <= 1 ? 1 : tier <= 3 ? 2 : tier <= 5 ? 3 : 4) * 5 * CombatEstimator.HitChance * 0.3
            : 0;

        // Legendary factor = extra DPR contribution from legendary actions per regular action slot
        double legendaryMulti = 0;
        if (isSolo)
        {
            var legendaryActions = traits.Where(t => t.LegendaryType == "action").ToList();
            if (legendaryActions.Count > 0)
            {
                var avgCost = legendaryActions.Average(t => t.LegendaryCost ?? 1);
                legendaryMulti = avgCost > 0 ? 3.0 / avgCost : 0;
            }
        }
        var legendaryFactor = 1 + legendaryMulti / Math.Max(1, actions.Count);
        var targetActionTotal = Math.Max(0, (targetDpr - spellDpr) / legendaryFactor);
        var targetPerAction = targetActionTotal / actions.Count;

        foreach (var action in actions)
        {
            if (action.ResolvedDamage == null) continue;
            var chance = action.ActionType == "save" ? CombatEstimator.SaveHitChance : CombatEstimator.HitChance;
            var recharge = CombatEstimator.RechargeMultipliers.TryGetValue(action.Recharge ?? "", out var r) ? r : 1.0;
            var effective = chance * recharge * (action.AoeTargets ?? 1);
            action.ResolvedDamage = [ComputeDiceFromTarget(action.ResolvedDamage[0], targetPerAction, effective), action.ResolvedDamage[1]];
        }
    }

    // Keeps the die face from the existing formula, sets dice count to match targetDpr/effective.
    static string ComputeDiceFromTarget(string formula, double targetDpr, double effective)
    {
        if (effective <= 0) return formula;
        var m = Regex.Match(formula.Trim(), @"^(\d+)d(\d+)\s*([+-]\s*\d+)?$", RegexOptions.IgnoreCase);
        if (!m.Success) return formula;
        var sides = int.Parse(m.Groups[2].Value);
        var faceAvg = (sides + 1) / 2.0;
        var diceCount = Math.Max(1, (int)Math.Round(targetDpr / (effective * faceAvg)));
        return $"{diceCount}d{sides}";
    }
}
