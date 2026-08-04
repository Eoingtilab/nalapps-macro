using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Microsoft.Win32;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow : Window
{
    private const int HotkeyPick = 1001;
    private const int HotkeyRun = 1002;
    private const int HotkeyPause = 1003;
    private const int HotkeyStop = 1004;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const int WmHotkey = 0x0312;
    private const int MaxMacroRepeat = 100_000;

    private readonly ObservableCollection<MacroStep> _steps = new();
    private readonly WindowsMacroInputDriver _inputDriver = new();
    private readonly MacroExecutor _executor;
    private CancellationTokenSource? _runCts;
    private bool _paused;
    private bool _running;
    private HwndSource? _source;
    private IntPtr _handle;
    private int _lastX;
    private int _lastY;
    private bool _hasPosition;

    public MainWindow()
    {
        InitializeComponent();
        _executor = new MacroExecutor(_inputDriver, new SystemMacroDelay());
        StepList.ItemsSource = _steps;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);

        var failures = new List<string>();
        if (!RegisterHotKey(_handle, HotkeyPick, ModControl | ModAlt, 0x77)) failures.Add("Ctrl+Alt+F8");
        if (!RegisterHotKey(_handle, HotkeyRun, ModControl | ModAlt, 0x78)) failures.Add("Ctrl+Alt+F9");
        if (!RegisterHotKey(_handle, HotkeyPause, ModControl | ModAlt, 0x79)) failures.Add("Ctrl+Alt+F10");
        if (!RegisterHotKey(_handle, HotkeyStop, ModControl | ModAlt, 0x7B)) failures.Add("Ctrl+Alt+F12");

        if (failures.Count > 0)
        {
            MessageBox.Show(
                "다른 프로그램이 다음 전역 단축키를 사용 중입니다.\n" + string.Join(", ", failures),
                "전역 단축키 충돌",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        handled = true;
        switch (wParam.ToInt32())
        {
            case HotkeyPick:
                CaptureCurrentPosition();
                break;
            case HotkeyRun:
                _ = RunMacroAsync();
                break;
            case HotkeyPause:
                TogglePause();
                break;
            case HotkeyStop:
                StopMacro();
                break;
        }

        return IntPtr.Zero;
    }

    private void NewMacro_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        _steps.Clear();
        MacroNameBox.Text = "새 매크로";
        RepeatCountBox.Text = "1";
        InfiniteRepeatCheck.IsChecked = false;
        _hasPosition = false;
        ClearEditor();
        SetStatus("새 매크로가 준비되었습니다.");
    }

    private void AddKeyboard_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var dialog = new KeyActionDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.CreatedStep is not null)
        {
            AddStep(dialog.CreatedStep);
        }
    }

    private void AddMouse_Click(object sender, RoutedEventArgs e)
    {
        OpenMouseDialog(MouseActionPreset.LeftClick);
    }

    private void AddDelay_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var dialog = new DelayDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.CreatedStep is not null)
        {
            AddStep(dialog.CreatedStep);
        }
    }

    private void AddText_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var dialog = new TextInputDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.CreatedStep is not null)
        {
            AddStep(dialog.CreatedStep);
        }
    }

    private void OpenActionMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_running || sender is not Button button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void AddMousePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        var preset = menuItem.Tag?.ToString() switch
        {
            "move" => MouseActionPreset.Move,
            "right" => MouseActionPreset.RightClick,
            "double" => MouseActionPreset.DoubleClick,
            "continuous-left" => MouseActionPreset.ContinuousLeftClick,
            "continuous-right" => MouseActionPreset.ContinuousRightClick,
            "wheel-up" => MouseActionPreset.WheelUp,
            "wheel-down" => MouseActionPreset.WheelDown,
            _ => MouseActionPreset.LeftClick
        };

        OpenMouseDialog(preset);
    }

    private void AddQuickAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        switch (menuItem.Tag?.ToString())
        {
            case "key-hold":
                var keyDialog = new KeyActionDialog(new MacroStep
                {
                    Type = MacroStepType.KeyHold,
                    Text = "SPACE",
                    Value = 10_000
                }) { Owner = this };
                if (keyDialog.ShowDialog() == true && keyDialog.CreatedStep is not null)
                {
                    AddStep(keyDialog.CreatedStep);
                }
                break;
            case "text":
                AddText_Click(sender, e);
                break;
            case "delay":
                AddDelay_Click(sender, e);
                break;
        }
    }

    private void OpenMouseDialog(MouseActionPreset preset, MacroStep? initialStep = null)
    {
        if (_running)
        {
            return;
        }

        var dialog = new MouseActionDialog(
            preset,
            initialStep,
            _hasPosition ? _lastX : null,
            _hasPosition ? _lastY : null)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.CreatedStep is not null)
        {
            if (initialStep is null)
            {
                AddStep(dialog.CreatedStep);
            }
            else
            {
                ReplaceSelectedStep(dialog.CreatedStep);
            }
        }
    }

    private void CaptureCurrentPosition()
    {
        if (_running)
        {
            return;
        }

        if (!GetCursorPos(out var point))
        {
            SetStatus("현재 마우스 위치를 읽지 못했습니다.");
            return;
        }

        _lastX = point.X;
        _lastY = point.Y;
        _hasPosition = true;
        AddStep(new MacroStep
        {
            Type = MacroStepType.MouseMove,
            HasPosition = true,
            X = point.X,
            Y = point.Y
        });
        Activate();
        SetStatus($"현재 위치 X {point.X} / Y {point.Y}를 마우스 이동 단계로 추가했습니다.");
    }

    private void AddStep(MacroStep step)
    {
        if (_running)
        {
            return;
        }

        step.NormalizeLegacyDefaults();
        var errors = MacroStepValidator.Validate(step);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "동작 추가 실패", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _steps.Add(step);
        StepList.SelectedItem = step;
        StepList.ScrollIntoView(step);
        RememberPosition(step);
        SetStatus($"{step.Summary} 동작을 추가했습니다.");
    }

    private void ReplaceSelectedStep(MacroStep replacement)
    {
        if (StepList.SelectedItem is not MacroStep selected)
        {
            AddStep(replacement);
            return;
        }

        replacement.NormalizeLegacyDefaults();
        var errors = MacroStepValidator.Validate(replacement);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "동작 수정 실패", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var index = _steps.IndexOf(selected);
        if (index < 0)
        {
            AddStep(replacement);
            return;
        }

        _steps[index] = replacement;
        StepList.SelectedItem = replacement;
        RememberPosition(replacement);
        SetStatus("선택한 동작을 수정했습니다.");
    }

    private void EditSelectedStep_Click(object sender, RoutedEventArgs e)
    {
        if (_running || StepList.SelectedItem is not MacroStep step)
        {
            SetStatus("편집할 단계를 먼저 선택해 주세요.");
            return;
        }

        switch (step.Type)
        {
            case MacroStepType.MouseMove:
            case MacroStepType.LeftClick:
            case MacroStepType.RightClick:
            case MacroStepType.DoubleClick:
            case MacroStepType.MouseWheel:
                OpenMouseDialog(ToMousePreset(step), step);
                break;
            case MacroStepType.KeyPress:
            case MacroStepType.KeyHold:
                var keyDialog = new KeyActionDialog(step) { Owner = this };
                if (keyDialog.ShowDialog() == true && keyDialog.CreatedStep is not null)
                {
                    ReplaceSelectedStep(keyDialog.CreatedStep);
                }
                break;
            case MacroStepType.Delay:
                var delayDialog = new DelayDialog(step) { Owner = this };
                if (delayDialog.ShowDialog() == true && delayDialog.CreatedStep is not null)
                {
                    ReplaceSelectedStep(delayDialog.CreatedStep);
                }
                break;
            case MacroStepType.TextInput:
                var textDialog = new TextInputDialog(step) { Owner = this };
                if (textDialog.ShowDialog() == true && textDialog.CreatedStep is not null)
                {
                    ReplaceSelectedStep(textDialog.CreatedStep);
                }
                break;
        }
    }

    private static MouseActionPreset ToMousePreset(MacroStep step)
    {
        if (step.DurationMilliseconds > 0 && step.Type == MacroStepType.LeftClick)
        {
            return MouseActionPreset.ContinuousLeftClick;
        }

        if (step.DurationMilliseconds > 0 && step.Type == MacroStepType.RightClick)
        {
            return MouseActionPreset.ContinuousRightClick;
        }

        return step.Type switch
        {
            MacroStepType.MouseMove => MouseActionPreset.Move,
            MacroStepType.RightClick => MouseActionPreset.RightClick,
            MacroStepType.DoubleClick => MouseActionPreset.DoubleClick,
            MacroStepType.MouseWheel when step.Value >= 0 => MouseActionPreset.WheelUp,
            MacroStepType.MouseWheel => MouseActionPreset.WheelDown,
            _ => MouseActionPreset.LeftClick
        };
    }

    private void StepList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StepList.SelectedItem is not MacroStep step)
        {
            ClearEditor();
            return;
        }

        SelectedTypeText.Text = step.Summary;
        XBox.Text = step.HasPosition ? step.X.ToString() : string.Empty;
        YBox.Text = step.HasPosition ? step.Y.ToString() : string.Empty;
        TextValueBox.Text = step.Text;
        NumberValueBox.Text = GetPrimaryTimeValue(step).ToString();
    }

    private void ApplyStep_Click(object sender, RoutedEventArgs e)
    {
        if (_running || StepList.SelectedItem is not MacroStep selected)
        {
            return;
        }

        var step = CloneStep(selected);
        var hasX = int.TryParse(XBox.Text?.Trim(), out var x);
        var hasY = int.TryParse(YBox.Text?.Trim(), out var y);
        if (hasX != hasY)
        {
            MessageBox.Show("X와 Y 좌표를 모두 입력하거나 모두 비워 주세요.", "빠른 설정", MessageBoxButton.OK, MessageBoxImage.Information);
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
        }

        if (step.Type is MacroStepType.KeyPress or MacroStepType.KeyHold or MacroStepType.TextInput)
        {
            step.Text = TextValueBox.Text ?? string.Empty;
        }

        if (int.TryParse(NumberValueBox.Text, out var milliseconds))
        {
            if (step.Type is MacroStepType.Delay or MacroStepType.KeyHold)
            {
                step.Value = milliseconds;
            }
            else if (step.DurationMilliseconds > 0 && step.Type is MacroStepType.LeftClick or MacroStepType.RightClick or MacroStepType.DoubleClick)
            {
                step.DurationMilliseconds = milliseconds;
            }
        }

        ReplaceSelectedStep(step);
    }

    private static int GetPrimaryTimeValue(MacroStep step)
    {
        if (step.Type is MacroStepType.Delay or MacroStepType.KeyHold)
        {
            return step.Value;
        }

        if (step.DurationMilliseconds > 0)
        {
            return step.DurationMilliseconds;
        }

        return 0;
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
        if (_running || StepList.SelectedItem is not MacroStep step)
        {
            return;
        }

        if (step.Type is not (MacroStepType.Delay or MacroStepType.KeyHold) && step.DurationMilliseconds <= 0)
        {
            SetStatus("시간 조절은 대기, 키 누르고 있기, 연속 클릭 단계에서 사용할 수 있습니다.");
            return;
        }

        var current = int.TryParse(SecondsValueEditorBox.Text?.Trim(), out var seconds)
            ? seconds
            : Math.Max(1, GetPrimaryTimeValue(step) / 1000);
        SecondsValueEditorBox.Text = Math.Clamp(current + offset, 1, 86_400).ToString();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        MoveSelected(-1);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        MoveSelected(1);
    }

    private void MoveSelected(int offset)
    {
        if (_running || StepList.SelectedItem is not MacroStep step)
        {
            return;
        }

        var oldIndex = _steps.IndexOf(step);
        var newIndex = oldIndex + offset;
        if (newIndex < 0 || newIndex >= _steps.Count)
        {
            return;
        }

        _steps.Move(oldIndex, newIndex);
        StepList.SelectedItem = step;
    }

    private void DeleteStep_Click(object sender, RoutedEventArgs e)
    {
        if (_running || StepList.SelectedItem is not MacroStep step)
        {
            SetStatus("삭제할 단계를 먼저 선택해 주세요.");
            return;
        }

        _steps.Remove(step);
        ClearEditor();
        SetStatus("선택한 단계를 삭제했습니다.");
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        await RunMacroAsync();
    }

    private async Task RunMacroAsync()
    {
        if (_running)
        {
            if (_paused)
            {
                TogglePause();
            }
            return;
        }

        var validationErrors = ValidateMacro();
        if (validationErrors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, validationErrors), "매크로 확인", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(RepeatCountBox.Text, out var repeat) || repeat is < 1 or > MaxMacroRepeat)
        {
            MessageBox.Show($"반복 횟수는 1~{MaxMacroRepeat:N0}회 범위로 입력해 주세요.", "반복 횟수", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }
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
                for (var index = 0; index < _steps.Count; index++)
                {
                    await WaitWhilePausedAsync(_runCts.Token);
                    _runCts.Token.ThrowIfCancellationRequested();

                    ProgressText.Text = infinite
                        ? $"반복 {cycle}회 · 단계 {index + 1}/{_steps.Count}"
                        : $"반복 {cycle}/{repeat} · 단계 {index + 1}/{_steps.Count}";
                    StepList.SelectedIndex = index;
                    StepList.ScrollIntoView(_steps[index]);
                    await _executor.ExecuteStepAsync(_steps[index], _runCts.Token);
                }
            }

            SetStatus("실행이 완료되었습니다.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("실행을 중지했습니다.");
        }
        catch (Exception exception)
        {
            SetStatus("오류로 실행이 중단되었습니다.");
            MessageBox.Show(exception.Message, "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _executor.ReleaseSafetyState();
            _running = false;
            _paused = false;
            SetEditingEnabled(true);
            ProgressText.Text = "Ctrl+Alt+F8 위치 · Ctrl+Alt+F9 시작 · Ctrl+Alt+F10 일시정지 · Ctrl+Alt+F12 중지";
        }
    }

    private List<string> ValidateMacro()
    {
        var errors = new List<string>();
        if (_steps.Count == 0)
        {
            errors.Add("실행할 단계를 먼저 추가해 주세요.");
            return errors;
        }

        for (var index = 0; index < _steps.Count; index++)
        {
            var stepErrors = MacroStepValidator.Validate(_steps[index]);
            foreach (var error in stepErrors)
            {
                errors.Add($"{index + 1}번 단계: {error}");
            }
        }

        return errors;
    }

    private async Task WaitWhilePausedAsync(CancellationToken cancellationToken)
    {
        while (_paused)
        {
            await Task.Delay(80, cancellationToken);
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (!_running)
        {
            return;
        }

        _paused = !_paused;
        SetStatus(_paused
            ? "현재 동작이 끝난 뒤 일시정지합니다. Ctrl+Alt+F10으로 재개하세요."
            : "실행을 재개했습니다.");
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopMacro();
    }

    private void StopMacro()
    {
        _runCts?.Cancel();
        _executor.ReleaseSafetyState();
    }

    private void TestPosition_Click(object sender, RoutedEventArgs e)
    {
        if (StepList.SelectedItem is MacroStep selected && selected.HasPosition)
        {
            _lastX = selected.X;
            _lastY = selected.Y;
            _hasPosition = true;
        }

        if (!_hasPosition)
        {
            MessageBox.Show("고정 위치가 지정된 마우스 단계를 먼저 선택해 주세요.", "위치 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!_inputDriver.MoveMouse(_lastX, _lastY))
        {
            MessageBox.Show("저장된 위치로 마우스를 이동하지 못했습니다.", "위치 테스트", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SetStatus($"X {_lastX} / Y {_lastY} 위치로 마우스를 이동했습니다.");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var errors = ValidateMacro();
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "저장 전 확인", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "NalaApps 매크로 (*.nalmacro.json)|*.nalmacro.json|JSON 파일 (*.json)|*.json",
            FileName = SanitizeFileName(MacroNameBox.Text) + ".nalmacro.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var document = new MacroDocument
        {
            Name = string.IsNullOrWhiteSpace(MacroNameBox.Text) ? "새 매크로" : MacroNameBox.Text.Trim(),
            RepeatCount = int.TryParse(RepeatCountBox.Text, out var repeat) && repeat > 0 ? repeat : 1,
            InfiniteRepeat = InfiniteRepeatCheck.IsChecked == true,
            Steps = _steps.Select(CloneStep).ToList()
        };

        var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);
        SetStatus("매크로를 저장했습니다.");
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "NalaApps 매크로 (*.nalmacro.json;*.json)|*.nalmacro.json;*.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var document = JsonSerializer.Deserialize<MacroDocument>(json);
            if (document is null || document.SchemaVersion is < 1 or > 2 || document.Steps is null || document.Steps.Count > 10_000)
            {
                throw new InvalidDataException("지원하지 않거나 손상된 매크로 파일입니다.");
            }

            var loadedSteps = new List<MacroStep>(document.Steps.Count);
            for (var index = 0; index < document.Steps.Count; index++)
            {
                var step = document.Steps[index];
                step.NormalizeLegacyDefaults();
                var errors = MacroStepValidator.Validate(step);
                if (errors.Count > 0)
                {
                    throw new InvalidDataException($"{index + 1}번 단계가 올바르지 않습니다.\n{string.Join(Environment.NewLine, errors)}");
                }
                loadedSteps.Add(step);
            }

            _steps.Clear();
            foreach (var step in loadedSteps)
            {
                _steps.Add(step);
            }

            MacroNameBox.Text = string.IsNullOrWhiteSpace(document.Name) ? "새 매크로" : document.Name;
            RepeatCountBox.Text = Math.Clamp(document.RepeatCount, 1, MaxMacroRepeat).ToString();
            InfiniteRepeatCheck.IsChecked = document.InfiniteRepeat;

            var lastPosition = _steps.LastOrDefault(step => step.HasPosition);
            if (lastPosition is not null)
            {
                _lastX = lastPosition.X;
                _lastY = lastPosition.Y;
                _hasPosition = true;
            }
            else
            {
                _hasPosition = false;
            }

            ClearEditor();
            SetStatus("매크로를 불러왔습니다.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "불러오기 실패", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string SanitizeFileName(string? value)
    {
        var name = string.IsNullOrWhiteSpace(value) ? "새 매크로" : value.Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }
        return name;
    }

    private void SetEditingEnabled(bool enabled)
    {
        MacroNameBox.IsEnabled = enabled;
        RepeatCountBox.IsEnabled = enabled;
        InfiniteRepeatCheck.IsEnabled = enabled;
        StepList.IsEnabled = enabled;
        ActionPanel.IsEnabled = enabled;
    }

    private void ClearEditor()
    {
        SelectedTypeText.Text = "선택된 단계 없음";
        XBox.Clear();
        YBox.Clear();
        TextValueBox.Clear();
        NumberValueBox.Clear();
    }

    private void RememberPosition(MacroStep step)
    {
        if (!step.HasPosition)
        {
            return;
        }

        _lastX = step.X;
        _lastY = step.Y;
        _hasPosition = true;
    }

    private static MacroStep CloneStep(MacroStep step)
    {
        return new MacroStep
        {
            Type = step.Type,
            X = step.X,
            Y = step.Y,
            Value = step.Value,
            Text = step.Text,
            HasPosition = step.HasPosition,
            RepeatCount = step.RepeatCount,
            IntervalMilliseconds = step.IntervalMilliseconds,
            DurationMilliseconds = step.DurationMilliseconds
        };
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _runCts?.Cancel();
        _executor.ReleaseSafetyState();

        if (_handle != IntPtr.Zero)
        {
            UnregisterHotKey(_handle, HotkeyPick);
            UnregisterHotKey(_handle, HotkeyRun);
            UnregisterHotKey(_handle, HotkeyPause);
            UnregisterHotKey(_handle, HotkeyStop);
        }

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);
}
