using System.IO;
using System.Text;

namespace NalApps.Macro.Core;

internal static class CrashReporter
{
    internal static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NalaApps",
        "Macro",
        "logs",
        "crash.log");

    internal static void Write(string context, Exception exception)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var message = new StringBuilder()
                .AppendLine("============================================================")
                .AppendLine($"Time: {DateTimeOffset.Now:O}")
                .AppendLine($"Context: {context}")
                .AppendLine($"Exception: {exception.GetType().FullName}")
                .AppendLine($"Message: {exception.Message}")
                .AppendLine(exception.StackTrace)
                .AppendLine()
                .ToString();

            File.AppendAllText(LogPath, message, Encoding.UTF8);
        }
        catch
        {
            // Crash reporting must never terminate the utility.
        }
    }
}
