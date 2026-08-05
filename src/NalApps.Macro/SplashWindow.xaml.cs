using System.Windows;
using System.Windows.Media.Animation;

namespace NalApps.Macro;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public async Task PlayAsync()
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(500),
            FillBehavior = FillBehavior.HoldEnd
        });

        await Task.Delay(4_500);

        BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(500),
            FillBehavior = FillBehavior.HoldEnd
        });

        await Task.Delay(500);
    }
}
