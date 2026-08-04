using System.Windows;
using System.Windows.Interop;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow
{
    private void Advanced_SourceInitialized(object? sender, EventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(AdvancedWndProc);
    }

    private IntPtr AdvancedWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyRun)
        {
            handled = true;
            _ = RunMacroAdvancedAsync();
        }

        return IntPtr.Zero;
    }

    private void AddAdvancedKey_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;

        var dialog = new KeyActionDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        try
        {
            ValidateKeyExpression(dialog.KeyExpression);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "키보드 동작", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        AddStep(new MacroStep
        {
            Type = dialog.HoldKey ? MacroStepType.KeyHold : MacroStepType.KeyPress,
            Text = dialog.KeyExpression,
            Value = dialog.HoldMilliseconds
        });

        SetStatus(dialog.HoldKey
            ? $"{dialog.KeyExpression} 키를 {dialog.HoldMilliseconds / 1000d:0.###}초 동안 누르는 동작을 추가했습니다."
            : $"{dialog.KeyExpression} 키 입력을 추가했습니다.");
    }

    private void AddAdvancedDelay_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;

        var dialog = new DelayDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        AddStep(new MacroStep
        {
            Type = MacroStepType.Delay,
            Value = dialog.DelayMilliseconds
        });

        SetStatus($"{dialog.DelayMilliseconds / 1000d:0.###}초 대기 동작을 추가했습니다.");
    }

    private void DecreaseSelectedSeconds_Click(object sender, RoutedEventArgs e)
    {
        ChangeSelectedSeconds(-1);
    }

    private void IncreaseSelectedSeconds_Click(object sender, RoutedEventArgs e)
    {
        ChangeSelectedSeconds(1);
    }

    private void ChangeSelectedSeconds(int offset)
    {
        if (_running || StepList.SelectedItem is not MacroStep step) return;
        if (step.Type is not (MacroStepType.Delay or MacroStepType.KeyHold))
        {
            SetStatus("시간 조절은 대기 또는 키 누르고 있기 단계에서 사용할 수 있습니다.");
            return;
        }

        var current = int.TryParse(SecondsValueEditorBox.Text?.Trim(), out var seconds)
            ? seconds
            : Math.Max(1, step.Value / 1000);
        SecondsValueEditorBox.Text = Math.Clamp(current + offset, 1, 86400).ToString();
    }

    private async void RunAdvanced_Click(object sender, RoutedEventArgs e)
    {
        await RunMacroAdvancedAsync();
    }

    private async Task RunMacroAdvancedAsync()
    {
        if (_running)
        {
            if (_paused) TogglePause();
            return;
        }

        if (_steps.Count == 0)
        {
            MessageBox.Show("실행할 단계를 먼저 추가해 주세요.", "날라앱스 매크로");
            return;
        }

        if (!int.TryParse(RepeatCountBox.Text, out var repeat) || repeat < 1)
        {
            repeat = 1;
        }

        try
        {
            foreach (var step in _steps.Where(step => step.Type is MacroStepType.KeyPress or MacroStepType.KeyHold))
            {
                ValidateKeyExpression(step.Text);
            }
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "키보드 동작 확인", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var infinite = InfiniteRepeatCheck.IsChecked == true;
        if (infinite)
        {
            var answer = MessageBox.Show(
                "무한 반복을 시작할까요?\nCtrl+Alt+F12로 즉시 중지할 수 있습니다.",
                "무한 반복",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes) return;
        }

        _runCts?.Cancel();
        _runCts = new CancellationTokenSource();
        _running = true;
        _paused = false;
        SetEditingEnabled(false);

        try
        {
            for (var countdown = 3; countdown >= 1; countdown--)
            {
                SetStatus($"{countdown}초 후 실행합니다.");
                await Task.Delay(1000, _runCts.Token);
            }

            var cycle = 0;
            while (infinite || cycle < repeat)
            {
                cycle++;

                for (var i = 0; i < _steps.Count; i++)
                {
                    await WaitWhilePausedAsync(_runCts.Token);
                    _runCts.Token.ThrowIfCancellationRequested();

                    ProgressText.Text = infinite
                        ? $"반복 {cycle}회 · 단계 {i + 1}/{_steps.Count}"
                        : $"반복 {cycle}/{repeat} · 단계 {i + 1}/{_steps.Count}";

                    StepList.SelectedIndex = i;
                    StepList.ScrollIntoView(_steps[i]);
                    await ExecuteAdvancedStepAsync(_steps[i], _runCts.Token);
                }
            }

            SetStatus("실행이 완료되었습니다.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("실행을 중지했습니다.");
        }
        catch (Exception ex)
        {
            SetStatus("오류로 실행이 중단되었습니다.");
            MessageBox.Show(ex.Message, "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ReleaseCommonModifiers();
            _running = false;
            _paused = false;
            SetEditingEnabled(true);
            ProgressText.Text = "시작 Ctrl+Alt+F9 · 중지 Ctrl+Alt+F12";
        }
    }

    private static async Task ExecuteAdvancedStepAsync(MacroStep step, CancellationToken token)
    {
        if (step.Type == MacroStepType.KeyHold)
        {
            await HoldKeyExpressionAsync(step.Text, Math.Clamp(step.Value, 1000, 86400000), token);
            return;
        }

        await ExecuteStepAsync(step, token);
    }

    private static async Task HoldKeyExpressionAsync(string expression, int durationMilliseconds, CancellationToken token)
    {
        var virtualKeys = ParseKeyExpression(expression);

        foreach (var virtualKey in virtualKeys)
        {
            SendVirtualKey(virtualKey, false);
        }

        try
        {
            await Task.Delay(durationMilliseconds, token);
        }
        finally
        {
            for (var i = virtualKeys.Count - 1; i >= 0; i--)
            {
                SendVirtualKey(virtualKeys[i], true);
            }
        }
    }

    private static void ValidateKeyExpression(string expression)
    {
        _ = ParseKeyExpression(expression);
    }

    private static List<ushort> ParseKeyExpression(string expression)
    {
        var parts = (expression ?? string.Empty)
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            throw new InvalidOperationException("키 또는 조합키를 입력해 주세요.");
        }

        var virtualKeys = new List<ushort>();
        foreach (var part in parts)
        {
            virtualKeys.Add(ParseVirtualKey(part));
        }

        return virtualKeys;
    }
}
