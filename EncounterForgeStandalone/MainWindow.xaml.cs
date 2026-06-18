using EncounterForgeStandalone.Models;
using EncounterForgeStandalone.ViewModels;
using EncounterForgeStandalone.Views;

namespace EncounterForgeStandalone;

public partial class MainWindow : Window
{
    readonly BuilderViewModel _vm;
    CalibrationWindow? _calWindow;

    public MainWindow()
    {
        InitializeComponent();
        this.ApplyDarkTheme();
        _vm = new BuilderViewModel();
        DataContext = _vm;
        _vm.GenerationComplete += OnGenerationComplete;
    }

    void OnGenerationComplete(EncounterResult result)
    {
        var win = new ResultsWindow(result) { Owner = this };
        win.Show();
    }

    void OnCalibrateClick(object sender, RoutedEventArgs e)
    {
        if (_calWindow is { IsLoaded: true }) { _calWindow.Focus(); return; }
        _calWindow = new CalibrationWindow { Owner = this };
        _calWindow.IntensityChanged += () => _vm.NotifyIntensityChanged();
        _calWindow.Show();
    }

    void OnAboutClick(object sender, RoutedEventArgs e)
    {
        new Views.AboutWindow { Owner = this }.ShowDialog();
    }

    void OnCloseClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
