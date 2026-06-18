using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EncounterForgeStandalone.Engine;
using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.ViewModels;

partial class BuilderViewModel : ObservableObject
{
    int _pCount = Math.Clamp(AppSettings.Instance.PlayerCount, 1, 8);
    int _pLevel = Math.Clamp(AppSettings.Instance.PlayerLevel, 1, 20);
    int _eCount = Math.Clamp(AppSettings.Instance.EnemyCount, 1, 10);

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EnemyCountVisible))]
    bool _solo = AppSettings.Instance.Solo;

    [ObservableProperty] string _playerCount = "";
    [ObservableProperty] string _playerLevel = "";
    [ObservableProperty] string _enemyCount = "";
    [ObservableProperty] string _difficulty = AppSettings.Instance.Difficulty;
    [ObservableProperty] string _theme = AppSettings.Instance.Theme;
    [ObservableProperty] bool _isGenerating;

    [ObservableProperty] string _targetCr = "-";
    [ObservableProperty] string _enemyDprHp = "-";
    [ObservableProperty] string _partyDprHp = "-";
    [ObservableProperty] string _rounds = "-";
    [ObservableProperty] string _outcome = "-";
    [ObservableProperty] string _outcomeKey = "manageable";

    public bool EnemyCountVisible => !Solo;

    public event Action<EncounterResult>? GenerationComplete;

    public BuilderViewModel()
    {
        _playerCount = _pCount.ToString();
        _playerLevel = _pLevel.ToString();
        _enemyCount = _eCount.ToString();
        UpdateReadout();
    }

    partial void OnPlayerCountChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (int.TryParse(value, out var n))
        {
            var c = Math.Clamp(n, 1, 8);
            _pCount = c;
            if (c != n) { PlayerCount = c.ToString(); return; }
        }
        else
        {
            PlayerCount = _pCount.ToString(); return;
        }
        UpdateReadout();
    }

    partial void OnPlayerLevelChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (int.TryParse(value, out var n))
        {
            var c = Math.Clamp(n, 1, 20);
            _pLevel = c;
            if (c != n) { PlayerLevel = c.ToString(); return; }
        }
        else
        {
            PlayerLevel = _pLevel.ToString(); return;
        }
        UpdateReadout();
    }

    partial void OnEnemyCountChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (int.TryParse(value, out var n))
        {
            var c = Math.Clamp(n, 1, 10);
            _eCount = c;
            if (c != n) { EnemyCount = c.ToString(); return; }
        }
        else
        {
            EnemyCount = _eCount.ToString(); return;
        }
        UpdateReadout();
    }

    partial void OnDifficultyChanged(string value) => UpdateReadout();
    partial void OnSoloChanged(bool value) => UpdateReadout();

    [RelayCommand] void IncrementPlayerCount() { _pCount = Math.Min(8, _pCount + 1); PlayerCount = _pCount.ToString(); }
    [RelayCommand] void DecrementPlayerCount() { _pCount = Math.Max(1, _pCount - 1); PlayerCount = _pCount.ToString(); }
    [RelayCommand] void IncrementPlayerLevel() { _pLevel = Math.Min(20, _pLevel + 1); PlayerLevel = _pLevel.ToString(); }
    [RelayCommand] void DecrementPlayerLevel() { _pLevel = Math.Max(1, _pLevel - 1); PlayerLevel = _pLevel.ToString(); }
    [RelayCommand] void IncrementEnemyCount() { _eCount = Math.Min(10, _eCount + 1); EnemyCount = _eCount.ToString(); }
    [RelayCommand] void DecrementEnemyCount() { _eCount = Math.Max(1, _eCount - 1); EnemyCount = _eCount.ToString(); }

    void UpdateReadout()
    {
        try
        {
            var r = Generator.ComputeReadout(_pCount, _pLevel, _eCount,
                Difficulty, Solo, AppSettings.Instance.CombatIntensity);
            TargetCr = $"CR {r.TargetCr}";
            EnemyDprHp = $"{r.EnemyDpr:F1} DPR / {r.EnemyHp} HP";
            PartyDprHp = $"{r.PartyDpr:F1} DPR / {r.PartyHp} HP";
            Rounds = $"{r.RoundsDefeat:F1} / {r.RoundsThreaten:F1}";
            Outcome = OutcomeLabel(r.Outcome);
            OutcomeKey = r.Outcome;
        }
        catch { }
    }

    [RelayCommand]
    async Task Generate()
    {
        IsGenerating = true;
        try
        {
            var result = await Task.Run(() =>
                Generator.Generate(_pCount, _pLevel, _eCount,
                    Difficulty, Theme, Solo, AppSettings.Instance.CombatIntensity));
            SaveSettings();
            GenerationComplete?.Invoke(result);
        }
        finally { IsGenerating = false; }
    }

    public void NotifyIntensityChanged() => UpdateReadout();

    void SaveSettings()
    {
        AppSettings.Instance.PlayerCount = _pCount;
        AppSettings.Instance.PlayerLevel = _pLevel;
        AppSettings.Instance.EnemyCount = _eCount;
        AppSettings.Instance.Difficulty = Difficulty;
        AppSettings.Instance.Theme = Theme;
        AppSettings.Instance.Solo = Solo;
        AppSettings.Instance.Save();
    }

    static string OutcomeLabel(string key) => key switch
    {
        "easy" => "Likely Victory",
        "manageable" => "Balanced",
        "risky" => "Risky",
        "dangerous" => "Dangerous",
        _ => key
    };
}
