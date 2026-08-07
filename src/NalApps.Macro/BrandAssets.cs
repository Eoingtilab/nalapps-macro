using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace NalApps.Macro;

internal static class BrandAssets
{
    private const string SplashResource = "NalApps.Macro.Assets.SplashImage.jpg.b64";
    private const string IconResource = "NalApps.Macro.Assets.NallaMacro.ico.b64";

    public static BitmapImage LoadSplashImage() => LoadBase64Bitmap(SplashResource);

    public static BitmapImage LoadApplicationIcon() => LoadBase64Bitmap(IconResource);

    private static BitmapImage LoadBase64Bitmap(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded brand resource not found: {resourceName}");
        using var reader = new StreamReader(resourceStream);
        var raw = reader.ReadToEnd();
        var normalized = new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray());
        var bytes = Convert.FromBase64String(normalized);

        using var imageStream = new MemoryStream(bytes, writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = imageStream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
