using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NalApps.Macro;

public partial class SplashWindow : Window
{
    private const int DwmWindowCornerPreference = 33;
    private const int DwmWindowCornerRound = 2;

    public SplashWindow()
    {
        InitializeComponent();

        var splashImage = LoadSplashBitmap();
        IntroImage.Source = splashImage;
        Background = new ImageBrush(splashImage)
        {
            Stretch = Stretch.Fill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };

        Icon = BrandAssets.TryLoadApplicationIcon();
    }

    public bool IsSplashImageReady =>
        IntroImage.Source is BitmapSource bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0;

    private static BitmapSource LoadSplashBitmap()
    {
        var uri = new Uri("pack://application:,,,/Assets/SplashImage.jpg", UriKind.Absolute);
        var resource = Application.GetResourceStream(uri)
            ?? throw new FileNotFoundException("컴파일된 인트로 이미지 리소스를 찾지 못했습니다.", uri.ToString());

        using var resourceStream = resource.Stream;
        using var memory = new MemoryStream();
        resourceStream.CopyTo(memory);
        memory.Position = 0;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
        bitmap.StreamSource = memory;
        bitmap.EndInit();

        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            throw new InvalidDataException($"인트로 이미지 크기가 올바르지 않습니다: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
        }

        bitmap.Freeze();
        return bitmap;
    }

    private void SplashWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            var preference = DwmWindowCornerRound;
            _ = DwmSetWindowAttribute(
                handle,
                DwmWindowCornerPreference,
                ref preference,
                Marshal.SizeOf<int>());
        }
        catch
        {
            // DWM rounded-corner support is cosmetic only; unsupported Windows versions continue normally.
        }
    }

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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
