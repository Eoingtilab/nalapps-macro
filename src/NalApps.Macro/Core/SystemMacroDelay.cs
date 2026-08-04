namespace NalApps.Macro.Core;

public sealed class SystemMacroDelay : IMacroDelay
{
    public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
    {
        return Task.Delay(Math.Max(0, milliseconds), cancellationToken);
    }
}
