using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<MacroStep> _steps = [];
    private CancellationTokenSource? _runCts;
    private int _lastX;
    private int _lastY;

    public MainWindow()
    {
        InitializeComponent();
        StepList.ItemsSource = _steps;
    }

    private void NewMacro_Click(object sender, RoutedEventArgs e)
    {
        _steps.Clear();
        CoordinateText.Text = "아직 지정되지 않음";
        StatusText.Text = "새 매크로가 준비되었습니다.";
    }

    private void PickPosition_Click(object sender, RoutedEventArgs e)
    {
        GetCursorPos(out var point);
        _lastX = point.X;
        _lastY = point.Y;
        _steps.Add(new MacroStep { Type = MacroStepType.MouseMove, X = _lastX, Y = _lastY });
        CoordinateText.Text = $"X {_lastX} / Y {_lastY}";
        StatusText.Text = "현재 마우스 위치를 저장했습니다.";
    }

    private void AddLeftClick_Click(object sender, RoutedEventArgs e) =>
        _steps.Add(new MacroStep { Type = MacroStepType.LeftClick });

    private void AddText_Click(object sender, RoutedEventArgs e) =>
        _steps.Add(new MacroStep { Type = MacroStepType.TextInput, Text = "입력할 문구" });

    private void AddDelay_Click(object sender, RoutedEventArgs e) =>
        _steps.Add(new MacroStep { Type = MacroStepType.Delay, Value = 1000 });

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (_steps.Count == 0)
        {
            MessageBox.Show("실행할 단계를 먼저 추가해 주세요.", "날앱스 매크로");
            return;
        }

        _runCts?.Cancel();
        _runCts = new CancellationTokenSource();
        StatusText.Text = "3초 후 실행합니다. 중지: Ctrl+Alt+F12";

        try
        {
            await Task.Delay(3000, _runCts.Token);
            foreach (var step in _steps)
            {
                _runCts.Token.ThrowIfCancellationRequested();
                await ExecuteStepAsync(step, _runCts.Token);
            }
            StatusText.Text = "실행 완료";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "실행 중지됨";
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => _runCts?.Cancel();

    private void TestPosition_Click(object sender, RoutedEventArgs e)
    {
        SetCursorPos(_lastX, _lastY);
        StatusText.Text = "저장된 위치로 마우스를 이동했습니다.";
    }

    private static async Task ExecuteStepAsync(MacroStep step, CancellationToken token)
    {
        switch (step.Type)
        {
            case MacroStepType.MouseMove:
                SetCursorPos(step.X, step.Y);
                break;
            case MacroStepType.LeftClick:
                mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
                mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
                break;
            case MacroStepType.Delay:
                await Task.Delay(Math.Max(0, step.Value), token);
                break;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
