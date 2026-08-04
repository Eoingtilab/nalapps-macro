using System.Windows;
using System.Windows.Controls;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class KeyActionDialog : Window
{
    private const int MaxSeconds = 86_400;
    private readonly MacroStep? _initialStep;

    public MacroStep? CreatedStep { get; private set; }

    public KeyActionDialog(MacroStep? initialStep = null)
    {
        _initialStep = initialStep;
        InitializeComponent();

        if (initialStep is null)
        {
            return;
        }

        KeyExpressionBox.Text = initialStep.Text;
        if (initialStep.Type == MacroStepType.KeyHold)
        {
            HoldRadio.IsChecked = true;
            HoldSecondsBox.Text = Math.Max(1, initialStep.Value / 1000).ToString();
        }
        else
        {
            PressOnceRadio.IsChecked = true;
        }
    }

    private void PresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KeyExpressionBox is null || PresetBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var value = item.Tag?.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(value))
        {
            KeyExpressionBox.Text = value;
        }
        else
        {
            KeyExpressionBox.Focus();
            KeyExpressionBox.SelectAll();
        }
    }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (HoldPanel is not null)
        {
            HoldPanel.IsEnabled = HoldRadio?.IsChecked == true;
        }
    }

    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
        HoldSecondsBox.Text = Math.Max(1, ParseSeconds(HoldSecondsBox.Text, 10) - 1).ToString();
    }

    private void Increase_Click(object sender, RoutedEventArgs e)
    {
        HoldSecondsBox.Text = Math.Min(MaxSeconds, ParseSeconds(HoldSecondsBox.Text, 10) + 1).ToString();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var expression = (KeyExpressionBox.Text ?? string.Empty).Trim().ToUpperInvariant();
        try
        {
            _ = KeyExpressionParser.Parse(expression);
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(this, exception.Message, "키보드 동작", MessageBoxButton.OK, MessageBoxImage.Information);
            KeyExpressionBox.Focus();
            KeyExpressionBox.SelectAll();
            return;
        }

        var hold = HoldRadio.IsChecked == true;
        var seconds = ParseSeconds(HoldSecondsBox.Text, 10);
        if (hold && (seconds < 1 || seconds > MaxSeconds))
        {
            MessageBox.Show(this, "누르고 있을 시간은 1초부터 86,400초까지 입력할 수 있습니다.", "키보드 동작", MessageBoxButton.OK, MessageBoxImage.Information);
            HoldSecondsBox.Focus();
            HoldSecondsBox.SelectAll();
            return;
        }

        CreatedStep = new MacroStep
        {
            Type = hold ? MacroStepType.KeyHold : MacroStepType.KeyPress,
            Text = expression,
            Value = hold ? checked(seconds * 1000) : 0,
            RepeatCount = _initialStep?.RepeatCount ?? 1,
            IntervalMilliseconds = _initialStep?.IntervalMilliseconds ?? 100
        };

        DialogResult = true;
    }

    private static int ParseSeconds(string? value, int fallback)
    {
        return int.TryParse(value?.Trim(), out var seconds) ? seconds : fallback;
    }
}
