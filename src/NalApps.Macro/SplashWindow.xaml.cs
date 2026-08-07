using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace NalApps.Macro;

public partial class SplashWindow : Window
{
    private const string IntroResourceSuffix = "Assets.Intro.jpg.b64";

    public SplashWindow()
    {
        InitializeComponent();
        IntroImage.Source = LoadIntroImage();
    }

    private static BitmapImage LoadIntroImage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(IntroResourceSuffix, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException("인트로 이미지 리소스를 찾을 수 없습니다.");
        }

        using var resourceStream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("인트로 이미지 리소스를 열 수 없습니다.");
        using var reader = new StreamReader(resourceStream);
        var bytes = Convert.FromBase64String(reader.ReadToEnd().Trim());

        using var imageStream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = imageStream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
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
