namespace EncounterForgeStandalone.Models;

public class CreatureStats
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Cha { get; set; }
    public int Hp { get; set; }
    public int Ac { get; set; }
    public string Size { get; set; } = "med";
    public Dictionary<string, int> Speeds { get; set; } = new();
}

public class SpellInfo
{
    public List<SpellData> Cantrips { get; set; } = new();
    public List<SpellData> Leveled { get; set; } = new();
    public string CastingStat { get; set; } = "int";
    public int SpellDc { get; set; }
    public int SpellBonus { get; set; }
}

public class Creature
{
    public string Name { get; set; } = "";
    public string Cr { get; set; } = "1";
    public string Theme { get; set; } = "any";
    public string ChassisType { get; set; } = "brute";
    public int ProfBonus { get; set; } = 2;
    public Dictionary<string, int> Skills { get; set; } = new();
    public Dictionary<string, int> Senses { get; set; } = new();
    public CreatureStats Stats { get; set; } = new();
    public List<TraitData> Traits { get; set; } = new();
    public List<ActionData> Actions { get; set; } = new();
    public List<string> Resistances { get; set; } = new();
    public List<string> Immunities { get; set; } = new();
    public List<string> ConditionImmunities { get; set; } = new();
    public SpellInfo? SpellInfo { get; set; }
    public bool Solo { get; set; }
}
