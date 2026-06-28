using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EncounterForgeStandalone.Engine;
using EncounterForgeStandalone.Export;
using EncounterForgeStandalone.Models;
using Microsoft.Win32;

namespace EncounterForgeStandalone.ViewModels;

partial class ResultsViewModel : ObservableObject
{
    EncounterResult _result;

    [ObservableProperty] List<CreatureCardViewModel> _creatures = [];
    [ObservableProperty] string _partyDprHp = "";
    [ObservableProperty] string _groupDprHp = "";
    [ObservableProperty] string _groupAc = "";
    [ObservableProperty] string _roundsText = "";
    [ObservableProperty] string _outcomeText = "";
    [ObservableProperty] string _outcomeKey = "";
    [ObservableProperty] bool _isExporting;
    [ObservableProperty] bool _isRegenerating;

    public ResultsViewModel(EncounterResult result)
    {
        _result = result;
        Populate(result);
    }

    void Populate(EncounterResult result)
    {
        _result = result;
        Creatures = result.Results.Select(r => new CreatureCardViewModel(r)).ToList();

        var groupHp = result.Results.Sum(r => r.Profile.Hp);
        var groupDpr = result.Results.Sum(r => r.Profile.Dpr);
        var avgAc = result.Results.Count > 0
            ? (int)Math.Round(result.Results.Average(r => r.Profile.Ac))
            : 0;

        PartyDprHp = $"{result.Party.Dpr:F1} DPR / {(int)Math.Round(result.Party.Hp)} HP";
        GroupDprHp = $"{groupDpr:F1} DPR / {groupHp} HP";
        GroupAc = $"AC {avgAc}";
        RoundsText = $"{result.Rounds.RoundsToDefeat:F1} / {result.Rounds.RoundsToThreaten:F1}";
        OutcomeText = OutcomeLabel(result.Outcome);
        OutcomeKey = result.Outcome;
    }

    [RelayCommand]
    async Task Regenerate()
    {
        IsRegenerating = true;
        try
        {
            var result = await Task.Run(() =>
                Generator.Generate(_result.PlayerCount, _result.PlayerLevel, _result.EnemyCount,
                    _result.Difficulty, _result.Theme, _result.Solo, _result.IntensityOffset, _result.DprFirst));
            Populate(result);
        }
        finally { IsRegenerating = false; }
    }

    [RelayCommand]
    async Task ExportPdf()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save Stat Block",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = _result.Results.Count == 1
                ? $"{_result.Results[0].Name}.pdf"
                : $"Encounter_cnt{_result.PlayerCount}_lvl{_result.PlayerLevel}_e{_result.EnemyCount}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf",
            DefaultExt = "pdf"
        };

        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            await Task.Run(() => PdfExporter.Export(_result, dlg.FileName));
            Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        finally { IsExporting = false; }
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

class CreatureCardViewModel
{
    public string Name { get; }
    public string Cr { get; }
    public string Ac { get; }
    public string Hp { get; }
    public string Dpr { get; }
    public Creature Creature { get; }

    public CreatureCardViewModel(CreatureResult result)
    {
        Name = result.Name;
        Cr = $"CR {result.Cr}";
        Ac = $"AC {result.Profile.Ac}";
        Hp = $"{result.Profile.Hp} HP";
        Dpr = $"{result.Profile.Dpr:F1} DPR";
        Creature = result.Creature;
    }
}
