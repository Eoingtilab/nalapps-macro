using System.Windows;
using System.Windows.Controls;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class TextInputDialog : Window
{
    public MacroStep? CreatedStep { get; private set; }

    public TextInputDialog(MacroStep? initialStep = null)
    {
        InitializeComponent();

        if (initialStep?.Type == MacroStepType.TextInput)
        {
            TextValueBox.Text = initialStep.Text;
            IntervalBox.Text = Math.Clamp(initialStep.IntervalMilliseconds, 0, MacroStepValidator.MaxIntervalMilliseconds).ToString();
        }

        Loaded += (_, _) =>
        {
            TextValueBox.Focus();
            TextValueBox.CaretIndex = TextValueBox.Text.Length;
        };
    }

    private void DecreaseInterval_Click(object sender, RoutedEventArgs e)
    {
        IntervalBox.Text = Math.Max(0, ParseInt(IntervalBox.Text, 20) - 10).ToString();
    }

    private void IncreaseInterval_Click(object sender, RoutedEventArgs e)
    {
        IntervalBox.Text = Math.Min(MacroStepValidator.MaxIntervalMilliseconds, ParseInt(IntervalBox.Text, 20) + 10).ToString();
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
        var text = TextValueBox.Text ?? string.Empty;
        var interval = ParseInt(IntervalBox.Text, 20);
        var step = new MacroStep
        {
            Type = MacroStepType.TextInput,
            Text = text,
            IntervalMilliseconds = interval
        };

        var errors = MacroStepValidator.Validate(step);
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "문자 입력", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CreatedStep = step;
        DialogResult = true;
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value?.Trim(), out var parsed) ? parsed : fallback;
    }
}
