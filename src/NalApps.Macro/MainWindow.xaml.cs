using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
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

    private readonly ObservableCollection<MacroStep> _steps = [];
    private CancellationTokenSource? _runCts;
    private bool _paused;
    private bool _running;
    private HwndSource? _source;
    private IntPtr _handle;
    private int _lastX;
    private int _lastY;

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
        _source.AddHook(WndProc);

        var failures = new List<string>();
        if (!RegisterHotKey(_handle, HotkeyPick, ModControl | ModAlt, 0x77)) failures.Add("Ctrl+Alt+F8");
        if (!RegisterHotKey(_handle, HotkeyRun, ModControl | ModAlt, 0x78)) failures.Add("Ctrl+Alt+F9");
        if (!RegisterHotKey(_handle, HotkeyPause, ModControl | ModAlt, 0x79)) failures.Add("Ctrl+Alt+F10");
        if (!RegisterHotKey(_handle, HotkeyStop, ModControl | ModAlt, 0x7B)) failures.Add("Ctrl+Alt+F12");

        if (failures.Count > 0)
            MessageBox.Show($"다른 프로그램이 다음 단축키를 사용 중입니다.\n{string.Join(", ", failures)}", "전역 단축키 충돌", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey) return IntPtr.Zero;
        handled = true;
        switch (wParam.ToInt32())
        {
            case HotkeyPick: CaptureCurrentPosition(); break;
            case HotkeyRun: _ = RunMacroAsync(); break;
            case HotkeyPause: TogglePause(); break;
            case HotkeyStop: StopMacro(); break;
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
            AddPosition(picker.SelectedX, picker.SelectedY);
        else
            SetStatus("위치 선택을 취소했습니다.");
    }

    private void CaptureCurrentPosition()
    {
        if (_running) return;
        GetCursorPos(out var point);
        AddPosition(point.X, point.Y);
        Activate();
    }

    private void AddPosition(int x, int y)
    {
        _lastX = x;
        _lastY = y;
        var step = new MacroStep { Type = MacroStepType.MouseMove, X = x, Y = y };
        _steps.Add(step);
        StepList.SelectedItem = step;
        SetStatus($"마우스 위치 X {x} / Y {y}를 저장했습니다.");
    }

    private void AddLeftClick_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.LeftClick });
    private void AddRightClick_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.RightClick });
    private void AddDoubleClick_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.DoubleClick });
    private void AddWheel_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.MouseWheel, Value = -120 });
    private void AddText_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.TextInput, Text = "입력할 문구" });
    private void AddKey_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.KeyPress, Text = "ENTER" });
    private void AddDelay_Click(object sender, RoutedEventArgs e) => AddStep(new() { Type = MacroStepType.Delay, Value = 1000 });

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

    private async void Run_Click(object sender, RoutedEventArgs e) => await RunMacroAsync();

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

        var repeat = 1;
        if (!int.TryParse(RepeatCountBox.Text, out repeat) || repeat < 1) repeat = 1;
        var infinite = InfiniteRepeatCheck.IsChecked == true;
        if (infinite && MessageBox.Show("무한 반복을 시작할까요?\nCtrl+Alt+F12로 즉시 중지할 수 있습니다.", "무한 반복", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

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
                    ProgressText.Text = $"반복 {(infinite ? cycle + "회" : cycle + "/" + repeat)} · 단계 {i + 1}/{_steps.Count}";
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
            await Task.Delay(80, token);
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
        if (StepList.SelectedItem is MacroStep { Type: MacroStepType.MouseMove } selected)
        {
            _lastX = selected.X;
            _lastY = selected.Y;
        }
        if (_lastX == 0 && _lastY == 0)
        {
            MessageBox.Show("먼저 화면에서 위치를 지정해 주세요.", "위치 테스트");
            return;
        }
        SetCursorPos(_lastX, _lastY);
        SetStatus("저장된 위치로 마우스를 이동했습니다.");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
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
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
        SetStatus("매크로를 저장했습니다.");
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        var dialog = new OpenFileDialog { Filter = "NalApps 매크로 (*.nalmacro.json;*.json)|*.nalmacro.json;*.json" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var document = JsonSerializer.Deserialize<MacroDocument>(File.ReadAllText(dialog.FileName));
            if (document is null || document.SchemaVersion != 1 || document.Steps.Count > 10000)
                throw new InvalidDataException("지원하지 않거나 손상된 매크로 파일입니다.");
            _steps.Clear();
            foreach (var step in document.Steps) _steps.Add(step);
            MacroNameBox.Text = document.Name;
            RepeatCountBox.Text = Math.Max(1, document.RepeatCount).ToString();
            InfiniteRepeatCheck.IsChecked = document.InfiniteRepeat;
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
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    private void SetEditingEnabled(bool enabled)
    {
        MacroNameBox.IsEnabled = enabled;
        RepeatCountBox.IsEnabled = enabled;
        InfiniteRepeatCheck.IsEnabled = enabled;
    }

    private void ClearEditor()
    {
        SelectedTypeText.Text = "선택된 단계 없음";
        XBox.Clear(); YBox.Clear(); TextValueBox.Clear(); NumberValueBox.Clear();
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private static async Task ExecuteStepAsync(MacroStep step, CancellationToken token)
    {
        switch (step.Type)
        {
            case MacroStepType.MouseMove:
                if (!SetCursorPos(step.X, step.Y)) throw new InvalidOperationException("마우스 위치를 이동하지 못했습니다.");
                break;
            case MacroStepType.LeftClick:
                MouseClick(0x0002, 0x0004); break;
            case MacroStepType.RightClick:
                MouseClick(0x0008, 0x0010); break;
            case MacroStepType.DoubleClick:
                MouseClick(0x0002, 0x0004); await Task.Delay(80, token); MouseClick(0x0002, 0x0004); break;
            case MacroStepType.MouseWheel:
                mouse_event(0x0800, 0, 0, unchecked((uint)step.Value), UIntPtr.Zero); break;
            case MacroStepType.TextInput:
                SendUnicodeText(step.Text); break;
            case MacroStepType.KeyPress:
                SendKeyExpression(step.Text); break;
            case MacroStepType.Delay:
                await Task.Delay(Math.Max(0, step.Value), token); break;
        }
    }

    private static void MouseClick(uint down, uint up)
    {
        mouse_event(down, 0, 0, 0, UIntPtr.Zero);
        mouse_event(up, 0, 0, 0, UIntPtr.Zero);
    }

    private static void SendUnicodeText(string text)
    {
        foreach (var ch in text ?? string.Empty)
        {
            var inputs = new[]
            {
                new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = 0x0004 } } },
                new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = 0x0004 | 0x0002 } } }
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    private static void SendKeyExpression(string expression)
    {
        var parts = (expression ?? string.Empty).Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return;
        var modifiers = new List<ushort>();
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var modifier = parts[i].ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => (ushort)0x11,
                "ALT" => (ushort)0x12,
                "SHIFT" => (ushort)0x10,
                "WIN" or "WINDOWS" => (ushort)0x5B,
                _ => (ushort)0
            };
            if (modifier != 0) modifiers.Add(modifier);
        }
        var key = ParseVirtualKey(parts[^1]);
        if (key == 0) throw new InvalidOperationException($"지원하지 않는 키입니다: {parts[^1]}");
        foreach (var modifier in modifiers) SendVirtualKey(modifier, false);
        SendVirtualKey(key, false);
        SendVirtualKey(key, true);
        for (var i = modifiers.Count - 1; i >= 0; i--) SendVirtualKey(modifiers[i], true);
    }

    private static ushort ParseVirtualKey(string value)
    {
        var upper = value.Trim().ToUpperInvariant();
        if (upper.Length == 1)
        {
            var vk = VkKeyScan(upper[0]);
            return (ushort)(vk & 0xFF);
        }
        if (upper.StartsWith('F') && int.TryParse(upper[1..], out var f) && f is >= 1 and <= 24) return (ushort)(0x70 + f - 1);
        return upper switch
        {
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "SPACE" => 0x20,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "LEFT" => 0x25,
            "UP" => 0x26,
            "RIGHT" => 0x27,
            "DOWN" => 0x28,
            _ => 0
        };
    }

    private static void SendVirtualKey(ushort key, bool keyUp)
    {
        var input = new[] { new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = keyUp ? 0x0002u : 0u } } } };
        SendInput(1, input, Marshal.SizeOf<INPUT>());
    }

    private static void ReleaseCommonModifiers()
    {
        foreach (var key in new ushort[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }) SendVirtualKey(key, true);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        StopMacro();
        if (_handle != IntPtr.Zero)
        {
            UnregisterHotKey(_handle, HotkeyPick);
            UnregisterHotKey(_handle, HotkeyRun);
            UnregisterHotKey(_handle, HotkeyPause);
            UnregisterHotKey(_handle, HotkeyStop);
        }
        _source?.RemoveHook(WndProc);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] private static extern short VkKeyScan(char ch);
}
