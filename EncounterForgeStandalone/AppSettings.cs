using System.Text.Json;
using System.IO;

namespace EncounterForgeStandalone;

class AppSettings
{
    static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EncounterForgeStandalone", "settings.json");

    public int PlayerCount { get; set; } = 4;
    public int PlayerLevel { get; set; } = 1;
    public int EnemyCount { get; set; } = 1;
    public string Difficulty { get; set; } = "medium";
    public string Theme { get; set; } = "any";
    public bool Solo { get; set; } = false;
    public int CombatIntensity { get; set; } = 0;

    static AppSettings? _instance;
    public static AppSettings Instance => _instance ??= Load();

    static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
