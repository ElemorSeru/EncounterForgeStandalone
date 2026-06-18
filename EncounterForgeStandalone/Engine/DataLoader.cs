using System.IO;
using System.Text.Json;
using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class DataLoader
{
    static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };
    static string DataRoot => Path.Combine(AppContext.BaseDirectory, "data", "en");

    static T Load<T>(string relative)
    {
        var path = Path.Combine(DataRoot, relative);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Opts)
            ?? throw new InvalidDataException($"Failed to deserialize {relative}");
    }

    static List<ChassisData>? _chassis;
    static List<ActionData>? _actions;
    static List<TraitData>? _traits;
    static List<TraitData>? _legendary;
    static List<SpellData>? _spells;
    static Dictionary<string, CrBaselineEntry>? _crBaseline;
    static ClassDprRoot? _classDpr;
    static DescriptorNames? _names;
    static Dictionary<string, List<string>>? _types;

    public static List<ChassisData> Chassis => _chassis ??= Load<List<ChassisData>>("chassis/archetypes.json");
    public static List<TraitData> LegendaryTraits => _legendary ??= Load<List<TraitData>>("traits/legendary.json");
    public static List<SpellData> Spells => _spells ??= Load<List<SpellData>>("spells/spells.json");
    public static Dictionary<string, CrBaselineEntry> CrBaseline => _crBaseline ??= Load<Dictionary<string, CrBaselineEntry>>("balance/cr-baseline.json");
    public static ClassDprRoot ClassDpr => _classDpr ??= Load<ClassDprRoot>("balance/class-dpr.json");
    public static DescriptorNames Names => _names ??= Load<DescriptorNames>("descriptors/names.json");
    public static Dictionary<string, List<string>> CreatureTypes => _types ??= Load<Dictionary<string, List<string>>>("descriptors/types.json");

    public static List<ActionData> Actions => _actions ??= LoadActions();
    public static List<TraitData> Traits => _traits ??= LoadTraits();

    static List<ActionData> LoadActions()
    {
        var all = new List<ActionData>();
        foreach (var cat in new[] { "melee", "ranged", "special" })
        {
            try { all.AddRange(Load<List<ActionData>>($"actions/{cat}.json")); }
            catch { }
        }
        return all;
    }

    static List<TraitData> LoadTraits()
    {
        var all = new List<TraitData>();
        foreach (var cat in new[] { "defensive", "offensive", "movement", "senses", "passive", "reactions" })
        {
            try { all.AddRange(Load<List<TraitData>>($"traits/{cat}.json")); }
            catch { }
        }
        return all;
    }
}
