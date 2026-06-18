using EncounterForgeStandalone.ViewModels;

namespace EncounterForgeStandalone.Views;

public partial class CalibrationWindow : Window
{
    readonly CalibrationViewModel _vm;
    public event Action? IntensityChanged;

    public CalibrationWindow()
    {
        InitializeComponent();
        this.ApplyDarkTheme();
        _vm = new CalibrationViewModel();
        _vm.IntensityChanged += () => IntensityChanged?.Invoke();
        DataContext = _vm;
    }

    void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
