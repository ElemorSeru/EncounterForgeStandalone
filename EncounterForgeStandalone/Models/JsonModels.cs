using System.Text.Json.Serialization;

namespace EncounterForgeStandalone.Models;

public class ChassisStats
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Con { get; set; }
    [JsonPropertyName("int")] public int Int { get; set; }
    public int Wis { get; set; }
    public int Cha { get; set; }
    public int Hp { get; set; }
    public int Ac { get; set; }
    public string Size { get; set; } = "med";
    public Dictionary<string, int> Speeds { get; set; } = new();
    public List<string> Resistances { get; set; } = new();
    public List<string> Immunities { get; set; } = new();
    [JsonPropertyName("condition_immunities")] public List<string> ConditionImmunities { get; set; } = new();
}

public class ChassisData
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Label { get; set; } = "";
    public List<string> Themes { get; set; } = new();
    public Dictionary<string, ChassisStats> Tiers { get; set; } = new();
}

public class ActionData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    [JsonPropertyName("action_type")] public string ActionType { get; set; } = "";
    public int Range { get; set; }
    public string Description { get; set; } = "";
    [JsonPropertyName("damage_fallback")] public string[]? DamageFallback { get; set; }
    [JsonPropertyName("damage_tiers")] public Dictionary<string, string[]>? DamageTiers { get; set; }
    [JsonPropertyName("cr_min")] public double? CrMin { get; set; }
    [JsonPropertyName("cr_max")] public double? CrMax { get; set; }
    [JsonPropertyName("chassis_affinity")] public List<string>? ChassisAffinity { get; set; }
    public List<string>? Tags { get; set; }

    // set during assembly
    [JsonIgnore] public string[]? ResolvedDamage { get; set; }
}

public class TraitEffect
{
    [JsonPropertyName("ac_bonus")] public int? AcBonus { get; set; }
    [JsonPropertyName("hp_bonus")] public int? HpBonus { get; set; }
    public List<string>? Resistance { get; set; }
    public List<string>? Immunity { get; set; }
    [JsonPropertyName("condition_immunity")] public List<string>? ConditionImmunity { get; set; }
    public Dictionary<string, int>? Speed { get; set; }
}

public class TraitData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    [JsonPropertyName("cr_min")] public double CrMin { get; set; }
    [JsonPropertyName("cr_max")] public double CrMax { get; set; }
    [JsonPropertyName("cr_adjustment")] public double CrAdjustment { get; set; }
    public int Weight { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = "";
    public TraitEffect? Effect { get; set; }
    [JsonPropertyName("legendary_type")] public string? LegendaryType { get; set; }
    [JsonPropertyName("legendary_cost")] public double? LegendaryCost { get; set; }
    [JsonPropertyName("resistance_uses")] public int? ResistanceUses { get; set; }
    [JsonPropertyName("activation_type")] public string? ActivationType { get; set; }
}

public class SpellData
{
    public string Name { get; set; } = "";
    public string School { get; set; } = "";
    public bool Cantrip { get; set; }
    [JsonPropertyName("tier_min")] public int? TierMin { get; set; }
    [JsonPropertyName("tier_max")] public int? TierMax { get; set; }
    public List<string>? Tags { get; set; }
}

public class CrBaselineEntry
{
    public int Prof { get; set; }
    public int Ac { get; set; }
    public double Hp { get; set; }
    [JsonPropertyName("attack_bonus")] public int AttackBonus { get; set; }
    public double Dpr { get; set; }
    [JsonPropertyName("save_dc")] public int SaveDc { get; set; }
}

public class ClassEntry
{
    public string Role { get; set; } = "";
    public List<double> Levels { get; set; } = new();
}

public class ClassDprRoot
{
    public Dictionary<string, ClassEntry> Classes { get; set; } = new();
}

public class DescriptorNames
{
    public List<string> Prefixes { get; set; } = new();
    [JsonPropertyName("theme_prefixes")] public Dictionary<string, List<string>>? ThemePrefixes { get; set; }
    [JsonPropertyName("first_names")] public List<string>? FirstNames { get; set; }
    public List<string>? Titles { get; set; }
}
