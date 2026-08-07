using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NalApps.Macro;

public partial class SplashWindow : Window
{
    private const string SplashRelativePath = "Assets/NallaMacro_Splash.png";

    public SplashWindow()
    {
        InitializeComponent();
        IntroImage.Source = LoadIntroImage();
        Icon = BrandAssets.TryLoadApplicationIcon();
    }

    public bool HasLoadedIntroImage =>
        IntroImage.Source is BitmapSource bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0;

    private static BitmapSource LoadIntroImage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "NallaMacro_Splash.png");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"인트로 이미지 파일을 찾을 수 없습니다: {SplashRelativePath}", path);
        }

        using var stream = File.OpenRead(path);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            throw new InvalidDataException($"인트로 이미지 크기가 올바르지 않습니다: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
        }

        return bitmap;
    }

    public async Task PlayFiveSecondIntroAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        if (!HasLoadedIntroImage)
        {
            throw new InvalidOperationException("인트로 이미지가 UI에 로드되지 않았습니다.");
        }

        BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            FillBehavior = FillBehavior.HoldEnd
        });

        await Task.Delay(4_300);

        BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            FillBehavior = FillBehavior.HoldEnd
        });

        await Task.Delay(350);
    }
}
