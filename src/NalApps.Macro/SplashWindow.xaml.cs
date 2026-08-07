using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NalApps.Macro;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        if (IntroImage.Source is not BitmapSource bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            throw new InvalidOperationException("컴파일된 인트로 이미지를 불러오지 못했습니다.");
        }

        Icon = BrandAssets.TryLoadApplicationIcon();
    }

    public bool IsSplashImageReady =>
        IntroImage.Source is BitmapSource bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0;

    public async Task PlayAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(500),
            FillBehavior = FillBehavior.HoldEnd
        });

        await Task.Delay(4_000);

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
