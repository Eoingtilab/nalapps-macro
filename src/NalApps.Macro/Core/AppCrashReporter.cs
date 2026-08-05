using System.Diagnostics;
using System.IO;
using System.Text;

namespace NalApps.Macro.Core;

public static class AppCrashReporter
{
    private static readonly object Sync = new();

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NalaApps",
        "Macro",
        "Logs");

    public static string Write(string context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);
                var path = Path.Combine(LogDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
                var builder = new StringBuilder();
                builder.AppendLine("NalaApps Macro crash report");
                builder.AppendLine($"Time: {DateTimeOffset.Now:O}");
                builder.AppendLine($"Context: {context}");
                builder.AppendLine($"Version: {typeof(AppCrashReporter).Assembly.GetName().Version}");
                builder.AppendLine($"OS: {Environment.OSVersion}");
                builder.AppendLine($"64-bit process: {Environment.Is64BitProcess}");
                builder.AppendLine();
                builder.AppendLine(exception.ToString());
                File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
                return path;
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public static void OpenLogDirectory()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = LogDirectory,
                UseShellExecute = true
            });
        }
        catch
        {
            // Diagnostics must never cause a second application failure.
        }
    }
}
