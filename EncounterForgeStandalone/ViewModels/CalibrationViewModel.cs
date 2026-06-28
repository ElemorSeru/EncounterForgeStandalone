using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EncounterForgeStandalone.Engine;

namespace EncounterForgeStandalone.ViewModels;

partial class CalibrationViewModel : ObservableObject
{
    const int MinOffset = -3;
    const int MaxOffset = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDefault))]
    [NotifyPropertyChangedFor(nameof(IsNotDefault))]
    [NotifyPropertyChangedFor(nameof(AtMin))]
    [NotifyPropertyChangedFor(nameof(AtMax))]
    int _offset;

    [ObservableProperty] string _offsetDisplay = "0";
    [ObservableProperty] string _stepLabel = "Default intensity";
    [ObservableProperty] List<DifficultyPreviewRow> _diffRows = [];
    [ObservableProperty] bool _dprFirst = AppSettings.Instance.DprFirst;

    public bool IsDefault => Offset == 0;
    public bool IsNotDefault => Offset != 0;
    public bool AtMin => Offset <= MinOffset;
    public bool AtMax => Offset >= MaxOffset;

    public event Action? IntensityChanged;

    public CalibrationViewModel()
    {
        _offset = AppSettings.Instance.CombatIntensity;
        Refresh();
    }

    [RelayCommand]
    void Increment()
    {
        if (Offset >= MaxOffset) return;
        Offset++;
        Apply();
    }

    [RelayCommand]
    void Decrement()
    {
        if (Offset <= MinOffset) return;
        Offset--;
        Apply();
    }

    // CommandParameter from XAML arrives as string
    [RelayCommand]
    void SetOffset(string stepStr)
    {
        if (!int.TryParse(stepStr, out var step)) return;
        if (step < MinOffset || step > MaxOffset) return;
        Offset = step;
        Apply();
    }

    [RelayCommand]
    void Reset()
    {
        Offset = 0;
        Apply();
    }

    partial void OnDprFirstChanged(bool value)
    {
        AppSettings.Instance.DprFirst = value;
        AppSettings.Instance.Save();
        IntensityChanged?.Invoke();
    }

    void Apply()
    {
        AppSettings.Instance.CombatIntensity = Offset;
        AppSettings.Instance.Save();
        Refresh();
        IntensityChanged?.Invoke();
    }

    void Refresh()
    {
        OffsetDisplay = Offset > 0 ? $"+{Offset}" : Offset.ToString();
        StepLabel = Offset switch
        {
            0  => "Default intensity",
            1  => "Slightly more intense",
            2  => "Noticeably more intense",
            3  => "Very intense",
            -1 => "Slightly more lenient",
            -2 => "Noticeably more lenient",
            -3 => "Very lenient",
            _  => $"Offset {Offset}"
        };
        DiffRows = CombatEstimator.BuildDifficultyPreview(Offset);
    }
}
