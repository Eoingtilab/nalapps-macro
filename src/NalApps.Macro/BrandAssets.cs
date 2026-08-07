using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using NalApps.Macro.Core;

namespace NalApps.Macro;

internal static class BrandAssets
{
    private const string SplashResourcePath = "Assets/SplashImage.jpg.b64";
    private const string IconResourcePath = "Assets/NallaMacro.ico.b64";

    public static BitmapImage? TryLoadSplashImage() => TryLoadBase64Bitmap(SplashResourcePath);

    public static BitmapImage? TryLoadApplicationIcon() => TryLoadBase64Bitmap(IconResourcePath);

    private static BitmapImage? TryLoadBase64Bitmap(string resourcePath)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
            var resourceInfo = Application.GetResourceStream(uri);
            if (resourceInfo?.Stream is null)
            {
                throw new FileNotFoundException($"WPF pack resource was not found: {resourcePath}");
            }

            using var resourceStream = resourceInfo.Stream;
            using var reader = new StreamReader(resourceStream);
            var raw = reader.ReadToEnd();
            var normalized = new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray());
            var bytes = Convert.FromBase64String(normalized);

            using var imageStream = new MemoryStream(bytes, writable: false);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
            image.StreamSource = imageStream;
            image.EndInit();

            if (image.PixelWidth <= 0 || image.PixelHeight <= 0)
            {
                throw new InvalidDataException($"Decoded brand image has invalid dimensions: {image.PixelWidth}x{image.PixelHeight}.");
            }

            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            CrashReporter.Write($"BrandAssets:{resourcePath}", ex);
            return null;
        }
    }
}
