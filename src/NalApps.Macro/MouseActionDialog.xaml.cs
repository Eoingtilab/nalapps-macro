using System.Windows;
using System.Windows.Controls;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public enum MouseActionPreset
{
    Move,
    LeftClick,
    RightClick,
    DoubleClick,
    ContinuousLeftClick,
    ContinuousRightClick,
    WheelUp,
    WheelDown
}

public partial class MouseActionDialog : Window
{
    private const int MaxSeconds = 86_400;
    private const int MaxRepeatCount = 100_000;
    private readonly MacroStep? _initialStep;

    public MacroStep? CreatedStep { get; private set; }

    public MouseActionDialog(
        MouseActionPreset preset = MouseActionPreset.LeftClick,
        MacroStep? initialStep = null,
        int? suggestedX = null,
        int? suggestedY = null)
    {
        _initialStep = initialStep;
        InitializeComponent();

        if (initialStep is not null)
        {
            LoadStep(initialStep);
            return;
        }

        ApplyPreset(preset);
        if (suggestedX.HasValue && suggestedY.HasValue)
        {
            FixedPositionCheck.IsChecked = true;
            XBox.Text = suggestedX.Value.ToString();
            YBox.Text = suggestedY.Value.ToString();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        var owner = Owner;
        base.OnClosed(e);

        if (owner is not null && owner.IsVisible)
        {
            owner.Activate();
            owner.Focus();
        }
    }

    private void LoadStep(MacroStep step)
    {
        SelectAction(step.Type switch
        {
            MacroStepType.MouseMove => "move",
            MacroStepType.LeftClick => "left",
            MacroStepType.RightClick => "right",
            MacroStepType.DoubleClick => "double",
            MacroStepType.MouseWheel when step.Value >= 0 => "wheelup",
            MacroStepType.MouseWheel => "wheeldown",
            _ => "left"
        });

        FixedPositionCheck.IsChecked = step.HasPosition;
        XBox.Text = step.X.ToString();
        YBox.Text = step.Y.ToString();
        IntervalBox.Text = Math.Clamp(step.IntervalMilliseconds, 10, 60_000).ToString();

        if (step.DurationMilliseconds > 0)
        {
            DurationRadio.IsChecked = true;
            DurationSecondsBox.Text = Math.Max(1, step.DurationMilliseconds / 1000).ToString();
        }
        else if (step.RepeatCount > 1)
        {
            CountRadio.IsChecked = true;
            RepeatCountBox.Text = step.RepeatCount.ToString();
        }
        else
        {
            OnceRadio.IsChecked = true;
        }
    }

    private void ApplyPreset(MouseActionPreset preset)
    {
        switch (preset)
        {
            case MouseActionPreset.Move:
                SelectAction("move");
                break;
            case MouseActionPreset.RightClick:
                SelectAction("right");
                break;
            case MouseActionPreset.DoubleClick:
                SelectAction("double");
                break;
            case MouseActionPreset.ContinuousLeftClick:
                SelectAction("left");
                DurationRadio.IsChecked = true;
                DurationSecondsBox.Text = "10";
                IntervalBox.Text = "100";
                break;
            case MouseActionPreset.ContinuousRightClick:
                SelectAction("right");
                DurationRadio.IsChecked = true;
                DurationSecondsBox.Text = "10";
                IntervalBox.Text = "100";
                break;
            case MouseActionPreset.WheelUp:
                SelectAction("wheelup");
                break;
            case MouseActionPreset.WheelDown:
                SelectAction("wheeldown");
                break;
            default:
                SelectAction("left");
                break;
        }
    }

    private void SelectAction(string tag)
    {
        foreach (var item in ActionBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                ActionBox.SelectedItem = item;
                return;
            }
        }
    }

    private void ActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RepeatPanel is null)
        {
            return;
        }

        var moveOnly = SelectedActionTag() == "move";
        RepeatPanel.IsEnabled = !moveOnly;
        if (moveOnly)
        {
            FixedPositionCheck.IsChecked = true;
        }
    }

    private void PositionMode_Changed(object sender, RoutedEventArgs e)
    {
        if (PositionPanel is not null)
        {
            PositionPanel.IsEnabled = FixedPositionCheck?.IsChecked == true;
        }
    }

    private void RepeatMode_Changed(object sender, RoutedEventArgs e)
    {
        if (RepeatCountBox is null || DurationSecondsBox is null)
        {
            return;
        }

        RepeatCountBox.IsEnabled = CountRadio?.IsChecked == true;
        DurationSecondsBox.IsEnabled = DurationRadio?.IsChecked == true;
    }

    private void PickPosition_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new PositionPickerWindow { Owner = Owner };
            Hide();
            var result = picker.ShowDialog();
            Show();
            Activate();

            if (result == true)
            {
                FixedPositionCheck.IsChecked = true;
                XBox.Text = picker.SelectedX.ToString();
                YBox.Text = picker.SelectedY.ToString();
            }
        }
        catch (Exception exception)
        {
            ShowActionError("마우스 위치 선택", exception);
            if (!IsVisible)
            {
                Show();
            }
            Activate();
        }
    }

    private void DecreaseInterval_Click(object sender, RoutedEventArgs e)
    {
        IntervalBox.Text = Math.Max(10, ParseInt(IntervalBox.Text, 100) - 10).ToString();
    }

    private void IncreaseInterval_Click(object sender, RoutedEventArgs e)
    {
        IntervalBox.Text = Math.Min(60_000, ParseInt(IntervalBox.Text, 100) + 10).ToString();
    }

    private void IntervalPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out var milliseconds))
        {
            IntervalBox.Text = milliseconds.ToString();
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyMouseAction();
        }
        catch (Exception exception)
        {
            ShowActionError("마우스 동작 적용", exception);
        }
    }

    private void ApplyMouseAction()
    {
        var action = SelectedActionTag();
        var hasPosition = FixedPositionCheck.IsChecked == true;
        var x = ParseInt(XBox.Text, 0);
        var y = ParseInt(YBox.Text, 0);

        if ((action == "move" || hasPosition) &&
            (!int.TryParse(XBox.Text?.Trim(), out x) || !int.TryParse(YBox.Text?.Trim(), out y)))
        {
            MessageBox.Show(this, "X와 Y 좌표를 입력하거나 화면에서 위치를 선택해 주세요.", "마우스 동작", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var interval = ParseInt(IntervalBox.Text, 100);
        if (interval is < 10 or > 60_000)
        {
            MessageBox.Show(this, "동작 간격은 10~60,000ms 범위로 입력해 주세요.", "마우스 동작", MessageBoxButton.OK, MessageBoxImage.Information);
            IntervalBox.Focus();
            IntervalBox.SelectAll();
            return;
        }

        var repeatCount = 1;
        var durationMilliseconds = 0;
        if (CountRadio.IsChecked == true)
        {
            repeatCount = ParseInt(RepeatCountBox.Text, 1);
            if (repeatCount is < 1 or > MaxRepeatCount)
            {
                MessageBox.Show(this, $"반복 횟수는 1~{MaxRepeatCount:N0}회 범위로 입력해 주세요.", "마우스 동작", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        else if (DurationRadio.IsChecked == true)
        {
            var seconds = ParseInt(DurationSecondsBox.Text, 10);
            if (seconds is < 1 or > MaxSeconds)
            {
                MessageBox.Show(this, "연속 실행 시간은 1~86,400초 범위로 입력해 주세요.", "마우스 동작", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            durationMilliseconds = checked(seconds * 1000);
        }

        var step = new MacroStep
        {
            Type = action switch
            {
                "move" => MacroStepType.MouseMove,
                "right" => MacroStepType.RightClick,
                "double" => MacroStepType.DoubleClick,
                "wheelup" or "wheeldown" => MacroStepType.MouseWheel,
                _ => MacroStepType.LeftClick
            },
            HasPosition = action == "move" || hasPosition,
            X = x,
            Y = y,
            Value = action == "wheelup" ? 120 : action == "wheeldown" ? -120 : 0,
            RepeatCount = repeatCount,
            IntervalMilliseconds = interval,
            DurationMilliseconds = durationMilliseconds,
            Text = _initialStep?.Text ?? string.Empty
        };

        var errors = MacroStepValidator.Validate(step);
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "마우스 동작", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CreatedStep = step;
        DialogResult = true;
    }

    private void ShowActionError(string context, Exception exception)
    {
        var path = AppCrashReporter.Write(context, exception);
        var location = string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : $"\n\n오류 기록:\n{path}";

        MessageBox.Show(
            this,
            "동작을 적용하지 못했습니다. 프로그램은 종료하지 않았습니다." + location,
            "마우스 동작 오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private string SelectedActionTag()
    {
        return ActionBox.SelectedItem is ComboBoxItem item
            ? item.Tag?.ToString() ?? "left"
            : "left";
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value?.Trim(), out var parsed) ? parsed : fallback;
    }
}
