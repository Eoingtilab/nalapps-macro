using System.Windows;
using System.Windows.Threading;
using NalApps.Macro.Core;

namespace NalApps.Macro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        MainWindowApplyGuard.Register();
        DispatcherUnhandledException += HandleDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
        base.OnStartup(e);

        var mainWindow = new MainWindow
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Icon = BrandAssets.TryLoadApplicationIcon()
        };

        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
        mainWindow.Activate();
    }

    private static void HandleDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashReporter.Write("DispatcherUnhandledException", e.Exception);
        MessageBox.Show(
            "예기치 않은 오류가 발생했지만 프로그램 종료를 막았습니다.\n\n" +
            e.Exception.Message +
            "\n\n오류 기록: " + CrashReporter.LogPath,
            "날라매크로 오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void HandleUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            CrashReporter.Write("AppDomain.UnhandledException", exception);
        }
    }

    private static void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashReporter.Write("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }
}
