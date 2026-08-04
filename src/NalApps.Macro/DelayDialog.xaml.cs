using System.Windows;
using System.Windows.Controls;

namespace NalApps.Macro;

public partial class DelayDialog : Window
{
    private const int MaxSeconds = 86400;

    public int DelayMilliseconds { get; private set; } = 1000;

    public DelayDialog()
    {
        InitializeComponent();
    }

    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
        SecondsBox.Text = Math.Max(1, ParseSeconds(SecondsBox.Text, 1) - 1).ToString();
    }

    private void Increase_Click(object sender, RoutedEventArgs e)
    {
        SecondsBox.Text = Math.Min(MaxSeconds, ParseSeconds(SecondsBox.Text, 1) + 1).ToString();
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out var seconds))
        {
            SecondsBox.Text = seconds.ToString();
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SecondsBox.Text?.Trim(), out var seconds) || seconds < 1 || seconds > MaxSeconds)
        {
            MessageBox.Show(this, "대기 시간은 1초부터 86,400초까지 입력할 수 있습니다.", "대기 시간", MessageBoxButton.OK, MessageBoxImage.Information);
            SecondsBox.Focus();
            SecondsBox.SelectAll();
            return;
        }

        DelayMilliseconds = checked(seconds * 1000);
        DialogResult = true;
    }

    private static int ParseSeconds(string? value, int fallback)
    {
        return int.TryParse(value?.Trim(), out var seconds) ? seconds : fallback;
    }
}
