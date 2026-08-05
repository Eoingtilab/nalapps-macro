using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NalApps.Macro.UiSmoke;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Console.WriteLine("NalaApps Macro WPF UI smoke test");

        var application = new App
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        application.InitializeComponent();

        var mainWindow = new MainWindow();
        application.MainWindow = mainWindow;

        try
        {
            mainWindow.Show();
            TestMouseApplyKeepsMainWindowAlive(mainWindow);
            Console.WriteLine("[PASS] UI-003 mouse action apply keeps application alive and adds a step");
            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[FAIL] UI-003 {exception}");
            return 1;
        }
        finally
        {
            foreach (var window in Application.Current.Windows.Cast<Window>().ToArray())
            {
                window.Close();
            }

            application.Shutdown();
        }
    }

    private static void TestMouseApplyKeepsMainWindowAlive(MainWindow mainWindow)
    {
        Exception? timerFailure = null;
        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        timer.Tick += (_, _) =>
        {
            try
            {
                var dialog = Application.Current.Windows
                    .OfType<MouseActionDialog>()
                    .FirstOrDefault(window => window.IsVisible);

                if (dialog is null)
                {
                    return;
                }

                var applyButton = FindVisualDescendant<Button>(
                    dialog,
                    button => string.Equals(button.Content?.ToString(), "마우스 동작 적용", StringComparison.Ordinal));

                if (applyButton is null)
                {
                    throw new InvalidOperationException("마우스 동작 적용 버튼을 찾지 못했습니다.");
                }

                timer.Stop();
                applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception exception)
            {
                timerFailure = exception;
                timer.Stop();

                foreach (var dialog in Application.Current.Windows.OfType<MouseActionDialog>().ToArray())
                {
                    dialog.Close();
                }
            }
        };

        timer.Start();

        var addMouseMethod = typeof(MainWindow).GetMethod(
            "AddMouse_Click",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(MainWindow).FullName, "AddMouse_Click");

        addMouseMethod.Invoke(mainWindow, new object[] { mainWindow, new RoutedEventArgs() });
        timer.Stop();

        if (timerFailure is not null)
        {
            throw new InvalidOperationException("마우스 동작 적용 자동 클릭에 실패했습니다.", timerFailure);
        }

        var stepList = mainWindow.FindName("StepList") as ListBox
            ?? throw new InvalidOperationException("메인 창의 단계 목록을 찾지 못했습니다.");

        if (stepList.Items.Count != 1)
        {
            throw new InvalidOperationException($"마우스 동작 적용 후 단계 수가 1이 아닙니다: {stepList.Items.Count}");
        }

        if (!mainWindow.IsVisible)
        {
            throw new InvalidOperationException("마우스 동작 적용 후 메인 창이 닫혔습니다.");
        }

        if (Application.Current.Windows.OfType<MainWindow>().Count(window => window.IsVisible) != 1)
        {
            throw new InvalidOperationException("마우스 동작 적용 후 메인 창 수가 올바르지 않습니다.");
        }
    }

    private static T? FindVisualDescendant<T>(DependencyObject root, Func<T, bool> predicate)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed && predicate(typed))
            {
                return typed;
            }

            var nested = FindVisualDescendant(child, predicate);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
