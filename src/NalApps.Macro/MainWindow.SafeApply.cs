using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow
{
    internal void SafeApplySelectedStep()
    {
        if (_running || StepList.SelectedItem is not MacroStep selected)
        {
            SetStatus("빠른 설정을 적용할 단계를 먼저 선택해 주세요.");
            return;
        }

        try
        {
            var step = CloneStep(selected);

            var xText = XBox.Text?.Trim() ?? string.Empty;
            var yText = YBox.Text?.Trim() ?? string.Empty;
            var hasX = int.TryParse(xText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x);
            var hasY = int.TryParse(yText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y);

            if (string.IsNullOrEmpty(xText) != string.IsNullOrEmpty(yText) || hasX != hasY)
            {
                MessageBox.Show(
                    this,
                    "X와 Y 좌표를 모두 올바른 정수로 입력하거나 모두 비워 주세요.",
                    "빠른 설정",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (hasX && hasY)
            {
                step.HasPosition = true;
                step.X = x;
                step.Y = y;
            }
            else if (step.Type != MacroStepType.MouseMove)
            {
                step.HasPosition = false;
                step.X = 0;
                step.Y = 0;
            }

            if (step.Type is MacroStepType.KeyPress or MacroStepType.KeyHold or MacroStepType.TextInput)
            {
                step.Text = TextValueBox.Text ?? string.Empty;
            }

            if (step.Type is MacroStepType.Delay or MacroStepType.KeyHold || step.DurationMilliseconds > 0)
            {
                var secondsText = SecondsValueEditorBox.Text?.Trim() ?? string.Empty;
                if (!double.TryParse(secondsText, NumberStyles.Float, CultureInfo.CurrentCulture, out var seconds) &&
                    !double.TryParse(secondsText, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
                {
                    MessageBox.Show(
                        this,
                        "시간은 초 단위 숫자로 입력해 주세요. 예: 1, 10, 50",
                        "빠른 설정",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (seconds < 1 || seconds > 86_400)
                {
                    MessageBox.Show(
                        this,
                        "시간은 1초부터 86,400초까지 입력할 수 있습니다.",
                        "빠른 설정",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var milliseconds = checked((int)Math.Round(seconds * 1000d));
                if (step.Type is MacroStepType.Delay or MacroStepType.KeyHold)
                {
                    step.Value = milliseconds;
                }
                else
                {
                    step.DurationMilliseconds = milliseconds;
                }
            }

            var errors = MacroStepValidator.Validate(step);
            if (errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    string.Join(Environment.NewLine, errors),
                    "빠른 설정 적용 실패",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ReplaceSelectedStep(step);
            SetStatus("빠른 설정을 적용했습니다.");
        }
        catch (Exception exception)
        {
            CrashReporter.Write("SafeApplySelectedStep", exception);
            MessageBox.Show(
                this,
                "빠른 설정을 적용하는 중 오류가 발생했습니다. 프로그램은 종료되지 않았습니다.\n\n" + exception.Message,
                "빠른 설정 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            SetStatus("빠른 설정 적용 오류가 기록되었습니다.");
        }
    }
}

internal static class MainWindowApplyGuard
{
    internal static void Register()
    {
        EventManager.RegisterClassHandler(
            typeof(Button),
            Button.ClickEvent,
            new RoutedEventHandler(InterceptApplyButton),
            true);
    }

    private static void InterceptApplyButton(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Content?.ToString() != "빠른 설정 적용" ||
            Window.GetWindow(button) is not MainWindow window)
        {
            return;
        }

        e.Handled = true;
        window.SafeApplySelectedStep();
    }
}
