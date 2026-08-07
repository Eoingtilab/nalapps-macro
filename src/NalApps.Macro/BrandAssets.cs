using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using NalApps.Macro.Core;

namespace NalApps.Macro;

internal static class BrandAssets
{
    private const string SplashResourceSuffix = "Assets.SplashImage.jpg.b64";
    private const string IconResourceSuffix = "Assets.NallaMacro.ico.b64";

    public static BitmapImage? TryLoadSplashImage() => TryLoadBase64Bitmap(SplashResourceSuffix);

    public static BitmapImage? TryLoadApplicationIcon() => TryLoadBase64Bitmap(IconResourceSuffix);

    private static BitmapImage? TryLoadBase64Bitmap(string resourceSuffix)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                CrashReporter.Write(
                    $"BrandAssetsMissing:{resourceSuffix}",
                    new InvalidOperationException(
                        $"Embedded brand resource was not found. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}"));
                return null;
            }

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is null)
            {
                CrashReporter.Write(
                    $"BrandAssetsStream:{resourceName}",
                    new InvalidOperationException("Embedded brand resource stream could not be opened."));
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
            CrashReporter.Write($"BrandAssets:{resourceSuffix}", ex);
            return null;
        }
    }
}
