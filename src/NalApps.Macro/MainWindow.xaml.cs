using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
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

    private readonly ObservableCollection<MacroStep> _steps = new();
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
        StepList.ItemsSource = _steps;
        SourceInitialized += MainWindow_SourceInitialized;
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
                "다른 프로그램이 다음 단축키를 사용 중입니다.\n" + string.Join(", ", failures),
                "전역 단축키 충돌",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey) return IntPtr.Zero;

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
        if (_running) return;

        _steps.Clear();
        MacroNameBox.Text = "새 매크로";
        RepeatCountBox.Text = "1";
        InfiniteRepeatCheck.IsChecked = false;
        _hasPosition = false;
        ClearEditor();
        SetStatus("새 매크로가 준비되었습니다.");
    }

    private void PickPosition_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;

        var picker = new PositionPickerWindow { Owner = this };
        Hide();
        var result = picker.ShowDialog();
        Show();
        Activate();

        if (result == true)
        {
            AddPosition(picker.SelectedX, picker.SelectedY);
        }
        else
        {
            SetStatus("위치 선택을 취소했습니다.");
        }
    }

    private void CaptureCurrentPosition()
    {
        if (_running) return;

        if (!GetCursorPos(out var point))
        {
            SetStatus("현재 마우스 위치를 읽지 못했습니다.");
            return;
        }

        AddPosition(point.X, point.Y);
        Activate();
    }

    private void AddPosition(int x, int y)
    {
        _lastX = x;
        _lastY = y;
        _hasPosition = true;

        var step = new MacroStep
        {
            Type = MacroStepType.MouseMove,
            X = x,
            Y = y
        };

        _steps.Add(step);
        StepList.SelectedItem = step;
        StepList.ScrollIntoView(step);
        SetStatus($"마우스 위치 X {x} / Y {y}를 저장했습니다.");
    }

    private void AddLeftClick_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.LeftClick });
    private void AddRightClick_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.RightClick });
    private void AddDoubleClick_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.DoubleClick });
    private void AddWheel_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.MouseWheel, Value = -120 });
    private void AddText_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.TextInput, Text = "입력할 문구" });
    private void AddKey_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.KeyPress, Text = "ENTER" });
    private void AddDelay_Click(object sender, RoutedEventArgs e) => AddStep(new MacroStep { Type = MacroStepType.Delay, Value = 1000 });

    private void AddStep(MacroStep step)
    {
        if (_running) return;

        _steps.Add(step);
        StepList.SelectedItem = step;
        StepList.ScrollIntoView(step);
    }

    private void StepList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (StepList.SelectedItem is not MacroStep step)
        {
            ClearEditor();
            return;
        }

        SelectedTypeText.Text = step.Summary;
        XBox.Text = step.X.ToString();
        YBox.Text = step.Y.ToString();
        TextValueBox.Text = step.Text;
        NumberValueBox.Text = step.Value.ToString();
    }

    private void ApplyStep_Click(object sender, RoutedEventArgs e)
    {
        if (_running || StepList.SelectedItem is not MacroStep step) return;

        if (int.TryParse(XBox.Text, out var x)) step.X = x;
        if (int.TryParse(YBox.Text, out var y)) step.Y = y;
        if (int.TryParse(NumberValueBox.Text, out var value)) step.Value = value;
        step.Text = TextValueBox.Text ?? string.Empty;

        if (step.Type == MacroStepType.MouseMove)
        {
            _lastX = step.X;
            _lastY = step.Y;
            _hasPosition = true;
        }

        StepList.Items.Refresh();
        SelectedTypeText.Text = step.Summary;
        SetStatus("선택한 단계의 설정을 적용했습니다.");
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int offset)
    {
        if (_running || StepList.SelectedItem is not MacroStep step) return;

        var oldIndex = _steps.IndexOf(step);
        var newIndex = oldIndex + offset;
        if (newIndex < 0 || newIndex >= _steps.Count) return;

        _steps.Move(oldIndex, newIndex);
        StepList.SelectedItem = step;
    }

    private void DeleteStep_Click(object sender, RoutedEventArgs e)
    {
        if (_running || StepList.SelectedItem is not MacroStep step) return;

        _steps.Remove(step);
        ClearEditor();
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        await RunMacroAsync();
    }

    private async Task RunMacroAsync()
    {
        if (_running)
        {
            if (_paused) TogglePause();
            return;
        }

        if (_steps.Count == 0)
        {
            MessageBox.Show("실행할 단계를 먼저 추가해 주세요.", "날앱스 매크로");
            return;
        }

        if (!int.TryParse(RepeatCountBox.Text, out var repeat) || repeat < 1)
        {
            repeat = 1;
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
                    await ExecuteStepAsync(_steps[i], _runCts.Token);
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

    private async Task WaitWhilePausedAsync(CancellationToken token)
    {
        while (_paused)
        {
            await Task.Delay(80, token);
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => TogglePause();

    private void TogglePause()
    {
        if (!_running) return;

        _paused = !_paused;
        SetStatus(_paused ? "일시정지됨 · Ctrl+Alt+F9로 재개" : "실행을 재개했습니다.");
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => StopMacro();

    private void StopMacro()
    {
        _runCts?.Cancel();
        ReleaseCommonModifiers();
    }

    private void TestPosition_Click(object sender, RoutedEventArgs e)
    {
        if (StepList.SelectedItem is MacroStep selected && selected.Type == MacroStepType.MouseMove)
        {
            _lastX = selected.X;
            _lastY = selected.Y;
            _hasPosition = true;
        }

        if (!_hasPosition)
        {
            MessageBox.Show("먼저 화면에서 위치를 지정해 주세요.", "위치 테스트");
            return;
        }

        if (!SetCursorPos(_lastX, _lastY))
        {
            MessageBox.Show("저장된 위치로 마우스를 이동하지 못했습니다.", "위치 테스트");
            return;
        }

        SetStatus("저장된 위치로 마우스를 이동했습니다.");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;

        var dialog = new SaveFileDialog
        {
            Filter = "NalApps 매크로 (*.nalmacro.json)|*.nalmacro.json|JSON 파일 (*.json)|*.json",
            FileName = SanitizeFileName(MacroNameBox.Text) + ".nalmacro.json"
        };

        if (dialog.ShowDialog(this) != true) return;

        var document = new MacroDocument
        {
            Name = string.IsNullOrWhiteSpace(MacroNameBox.Text) ? "새 매크로" : MacroNameBox.Text.Trim(),
            RepeatCount = int.TryParse(RepeatCountBox.Text, out var repeat) && repeat > 0 ? repeat : 1,
            InfiniteRepeat = InfiniteRepeatCheck.IsChecked == true,
            Steps = _steps.ToList()
        };

        var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);
        SetStatus("매크로를 저장했습니다.");
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;

        var dialog = new OpenFileDialog
        {
            Filter = "NalApps 매크로 (*.nalmacro.json;*.json)|*.nalmacro.json;*.json"
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var document = JsonSerializer.Deserialize<MacroDocument>(json);

            if (document is null || document.SchemaVersion != 1 || document.Steps is null || document.Steps.Count > 10000)
            {
                throw new InvalidDataException("지원하지 않거나 손상된 매크로 파일입니다.");
            }

            _steps.Clear();
            foreach (var step in document.Steps)
            {
                _steps.Add(step);
            }

            MacroNameBox.Text = string.IsNullOrWhiteSpace(document.Name) ? "새 매크로" : document.Name;
            RepeatCountBox.Text = Math.Max(1, document.RepeatCount).ToString();
            InfiniteRepeatCheck.IsChecked = document.InfiniteRepeat;

            var lastPosition = _steps.LastOrDefault(step => step.Type == MacroStepType.MouseMove);
            if (lastPosition is not null)
            {
                _lastX = lastPosition.X;
                _lastY = lastPosition.Y;
                _hasPosition = true;
            }

            SetStatus("매크로를 불러왔습니다.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "불러오기 실패", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }

    private void ClearEditor()
    {
        SelectedTypeText.Text = "선택된 단계 없음";
        XBox.Clear();
        YBox.Clear();
        TextValueBox.Clear();
        NumberValueBox.Clear();
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    private static async Task ExecuteStepAsync(MacroStep step, CancellationToken token)
    {
        switch (step.Type)
        {
            case MacroStepType.MouseMove:
                if (!SetCursorPos(step.X, step.Y))
                {
                    throw new InvalidOperationException("마우스 위치를 이동하지 못했습니다.");
                }
                break;

            case MacroStepType.LeftClick:
                MouseClick(MouseEventLeftDown, MouseEventLeftUp);
                break;

            case MacroStepType.RightClick:
                MouseClick(MouseEventRightDown, MouseEventRightUp);
                break;

            case MacroStepType.DoubleClick:
                MouseClick(MouseEventLeftDown, MouseEventLeftUp);
                await Task.Delay(80, token);
                MouseClick(MouseEventLeftDown, MouseEventLeftUp);
                break;

            case MacroStepType.MouseWheel:
                mouse_event(MouseEventWheel, 0, 0, unchecked((uint)step.Value), UIntPtr.Zero);
                break;

            case MacroStepType.TextInput:
                SendUnicodeText(step.Text);
                break;

            case MacroStepType.KeyPress:
                SendKeyExpression(step.Text);
                break;

            case MacroStepType.Delay:
                await Task.Delay(Math.Max(0, step.Value), token);
                break;
        }
    }

    private static void MouseClick(uint downFlag, uint upFlag)
    {
        mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
        mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
    }

    private static void SendUnicodeText(string text)
    {
        foreach (var ch in text ?? string.Empty)
        {
            var inputs = new[]
            {
                CreateUnicodeInput(ch, false),
                CreateUnicodeInput(ch, true)
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    private static INPUT CreateUnicodeInput(char ch, bool keyUp)
    {
        return new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wScan = ch,
                    dwFlags = KeyEventUnicode | (keyUp ? KeyEventKeyUp : 0)
                }
            }
        };
    }

    private static void SendKeyExpression(string expression)
    {
        var parts = (expression ?? string.Empty)
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0) return;

        var virtualKeys = new List<ushort>();
        foreach (var part in parts)
        {
            virtualKeys.Add(ParseVirtualKey(part));
        }

        foreach (var virtualKey in virtualKeys)
        {
            SendVirtualKey(virtualKey, false);
        }

        for (var i = virtualKeys.Count - 1; i >= 0; i--)
        {
            SendVirtualKey(virtualKeys[i], true);
        }
    }

    private static ushort ParseVirtualKey(string token)
    {
        var upper = token.Trim().ToUpperInvariant();
        return upper switch
        {
            "CTRL" or "CONTROL" => 0x11,
            "ALT" => 0x12,
            "SHIFT" => 0x10,
            "WIN" or "WINDOWS" => 0x5B,
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "SPACE" => 0x20,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            _ when upper.Length == 1 => (ushort)char.ToUpperInvariant(upper[0]),
            _ => throw new InvalidOperationException($"지원하지 않는 키 입력입니다: {token}")
        };
    }

    private static void SendVirtualKey(ushort virtualKey, bool keyUp)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        dwFlags = keyUp ? KeyEventKeyUp : 0
                    }
                }
            }
        };

        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void ReleaseCommonModifiers()
    {
        SendVirtualKey(0x10, true);
        SendVirtualKey(0x11, true);
        SendVirtualKey(0x12, true);
        SendVirtualKey(0x5B, true);
        SendVirtualKey(0x5C, true);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _runCts?.Cancel();
        ReleaseCommonModifiers();

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

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventWheel = 0x0800;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);
}
