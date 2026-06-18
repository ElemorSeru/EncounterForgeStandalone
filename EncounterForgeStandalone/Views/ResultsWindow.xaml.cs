using EncounterForgeStandalone.Models;
using EncounterForgeStandalone.ViewModels;

namespace EncounterForgeStandalone.Views;

public partial class ResultsWindow : Window
{
    public ResultsWindow(EncounterResult result)
    {
        InitializeComponent();
        this.ApplyDarkTheme();
        DataContext = new ResultsViewModel(result);
    }
}
