using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using NalApps.Macro.Core;

namespace NalApps.Macro;

internal static class BrandAssets
{
    private const string SplashResource = "NalApps.Macro.Assets.SplashImage.jpg.b64";
    private const string IconResource = "NalApps.Macro.Assets.NallaMacro.ico.b64";

    public static BitmapImage? TryLoadSplashImage() => TryLoadBase64Bitmap(SplashResource);

    public static BitmapImage? TryLoadApplicationIcon() => TryLoadBase64Bitmap(IconResource);

    private static BitmapImage? TryLoadBase64Bitmap(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is null)
            {
                return null;
            }

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
        catch (Exception ex)
        {
            CrashReporter.Write($"BrandAssets:{resourceName}", ex);
            return null;
        }
    }
}
