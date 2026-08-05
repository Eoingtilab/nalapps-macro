namespace NalApps.Macro.Core;

public enum MouseButtonKind
{
    Left,
    Right
}

public interface IMacroInputDriver
{
    bool MoveMouse(int x, int y);

    // Existing test doubles and external drivers remain source-compatible.
    // Returning false means no activation occurred, so the executor skips
    // the activation-settle delay. The Windows production driver overrides both.
    bool ActivateWindowAtPoint(int x, int y) => false;
    bool ActivateWindowUnderCursor() => false;

    void MouseButtonDown(MouseButtonKind button);
    void MouseButtonUp(MouseButtonKind button);
    void MouseWheel(int delta);
    void SendUnicodeCharacter(char character);
    void KeyDown(ushort virtualKey);
    void KeyUp(ushort virtualKey);
    void ReleaseSafetyState();
}

public interface IMacroDelay
{
    Task DelayAsync(int milliseconds, CancellationToken cancellationToken);
}
