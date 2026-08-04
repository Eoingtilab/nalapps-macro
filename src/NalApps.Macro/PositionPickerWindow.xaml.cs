using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NalApps.Macro;

public partial class PositionPickerWindow : Window
{
    public int SelectedX { get; private set; }
    public int SelectedY { get; private set; }

    public PositionPickerWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Loaded += (_, _) =>
        {
            Activate();
            Focus();
            Mouse.Capture(this);
        };
        Closed += (_, _) => Mouse.Capture(null);
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        var local = e.GetPosition(this);
        var screen = PointToScreen(local);
        SelectedX = (int)Math.Round(screen.X);
        SelectedY = (int)Math.Round(screen.Y);
        CoordinateText.Text = $"X {SelectedX} / Y {SelectedY}";

        VerticalLine.X1 = VerticalLine.X2 = local.X;
        VerticalLine.Y1 = 0;
        VerticalLine.Y2 = ActualHeight;
        HorizontalLine.X1 = 0;
        HorizontalLine.X2 = ActualWidth;
        HorizontalLine.Y1 = HorizontalLine.Y2 = local.Y;
        Canvas.SetLeft(TargetCircle, local.X - 15);
        Canvas.SetTop(TargetCircle, local.Y - 15);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var screen = PointToScreen(e.GetPosition(this));
        SelectedX = (int)Math.Round(screen.X);
        SelectedY = (int)Math.Round(screen.Y);
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
