using System.Windows;
using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp;

// Qualify WPF Application explicitly because the solution also contains
// the namespace LANRescue.Application.
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Load saved language before MainWindow is created.
        LocalizationManager.Initialize();

        // Existing preferences.
        ThemeManager.Initialize();
        MotionSettings.Current.Initialize();

        base.OnStartup(e);
    }
}
