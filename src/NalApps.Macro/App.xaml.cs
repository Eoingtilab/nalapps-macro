using System.Windows;
using System.Windows.Threading;
using NalApps.Macro.Core;

namespace NalApps.Macro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var path = AppCrashReporter.Write("WPF Dispatcher", e.Exception);
        var location = string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : $"\n\n오류 기록:\n{path}";

        MessageBox.Show(
            "처리 중 오류가 발생했지만 프로그램은 종료하지 않았습니다.\n" +
            "입력값을 확인한 뒤 다시 시도해 주세요." + location,
            "NalaApps Macro 오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            AppCrashReporter.Write("AppDomain", exception);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppCrashReporter.Write("Unobserved Task", e.Exception);
        e.SetObserved();
    }
}
