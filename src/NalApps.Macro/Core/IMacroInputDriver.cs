namespace NalApps.Macro.Core;

public enum MouseButtonKind
{
    Left,
    Right
}

public interface IMacroInputDriver
{
    bool MoveMouse(int x, int y);
    bool ActivateWindowAtPoint(int x, int y);
    bool ActivateWindowUnderCursor();
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
