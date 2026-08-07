using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NalApps.Macro;

public partial class SplashWindow : Window
{
    private const string SplashResourceName = "NallaMacro.Splash.png";
    private const int DwmWindowCornerPreference = 33;
    private const int DwmWindowCornerRound = 2;

    public SplashWindow()
    {
        InitializeComponent();
        IntroImage.Source = LoadSplashBitmap();
        Icon = BrandAssets.TryLoadApplicationIcon();
    }

    public bool IsSplashImageReady =>
        IntroImage.Source is BitmapSource bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0;

    private static BitmapSource LoadSplashBitmap()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(SplashResourceName)
            ?? throw new FileNotFoundException(
                $"인트로 임베디드 리소스를 찾지 못했습니다: {SplashResourceName}. " +
                $"available={string.Join(",", assembly.GetManifestResourceNames())}");

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
            // Rounded corners are cosmetic only. Unsupported Windows versions continue normally.
        }
    }

    public async Task PlayAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        if (!IsSplashImageReady)
        {
            throw new InvalidOperationException("PNG 인트로 이미지가 렌더링 준비 상태가 아닙니다.");
        }

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
