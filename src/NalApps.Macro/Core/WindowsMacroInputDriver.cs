using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NalApps.Macro.Core;

public sealed class WindowsMacroInputDriver : IMacroInputDriver
{
    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventWheel = 0x0800;
    private const uint GetAncestorRoot = 2;

    private readonly object _inputStateLock = new();
    private readonly HashSet<MouseButtonKind> _pressedMouseButtons = [];
    private readonly HashSet<ushort> _pressedKeys = [];

    public bool MoveMouse(int x, int y)
    {
        return SetCursorPos(x, y);
    }

    public bool ActivateWindowAtPoint(int x, int y)
    {
        return ActivateWindow(new NativePoint { X = x, Y = y });
    }

    public bool ActivateWindowUnderCursor()
    {
        return GetCursorPos(out var point) && ActivateWindow(point);
    }

    public void MouseButtonDown(MouseButtonKind button)
    {
        SendMouse(button == MouseButtonKind.Left ? MouseEventLeftDown : MouseEventRightDown, 0);
        lock (_inputStateLock)
        {
            _pressedMouseButtons.Add(button);
        }
    }

    public void MouseButtonUp(MouseButtonKind button)
    {
        bool wasPressed;
        lock (_inputStateLock)
        {
            wasPressed = _pressedMouseButtons.Remove(button);
        }

        if (!wasPressed)
        {
            return;
        }

        SendMouse(button == MouseButtonKind.Left ? MouseEventLeftUp : MouseEventRightUp, 0);
    }

    public void MouseWheel(int delta)
    {
        SendMouse(MouseEventWheel, unchecked((uint)delta));
    }

    public void SendUnicodeCharacter(char character)
    {
        var inputs = new[]
        {
            CreateUnicodeInput(character, false),
            CreateUnicodeInput(character, true)
        };

        SendChecked(inputs);
    }

    public void KeyDown(ushort virtualKey)
    {
        SendVirtualKey(virtualKey, false);
        lock (_inputStateLock)
        {
            _pressedKeys.Add(virtualKey);
        }
    }

    public void KeyUp(ushort virtualKey)
    {
        bool wasPressed;
        lock (_inputStateLock)
        {
            wasPressed = _pressedKeys.Remove(virtualKey);
        }

        if (!wasPressed)
        {
            return;
        }

        SendVirtualKey(virtualKey, true);
    }

    public void ReleaseSafetyState()
    {
        MouseButtonKind[] buttons;
        ushort[] keys;

        lock (_inputStateLock)
        {
            buttons = _pressedMouseButtons.ToArray();
            keys = _pressedKeys.ToArray();
            _pressedMouseButtons.Clear();
            _pressedKeys.Clear();
        }

        foreach (var button in buttons)
        {
            try
            {
                SendMouse(button == MouseButtonKind.Left ? MouseEventLeftUp : MouseEventRightUp, 0);
            }
            catch (Win32Exception)
            {
                // Best-effort cleanup during cancellation or shutdown.
            }
        }

        foreach (var virtualKey in keys.Reverse())
        {
            try
            {
                SendVirtualKey(virtualKey, true);
            }
            catch (Win32Exception)
            {
                // Best-effort cleanup during cancellation or shutdown.
            }
        }
    }

    private static bool ActivateWindow(NativePoint point)
    {
        var child = WindowFromPoint(point);
        if (child == IntPtr.Zero)
        {
            return false;
        }

        var root = GetAncestor(child, GetAncestorRoot);
        if (root == IntPtr.Zero)
        {
            root = child;
        }

        if (IsIconic(root))
        {
            ShowWindow(root, 9);
        }

        BringWindowToTop(root);
        SetForegroundWindow(root);

        var foreground = GetForegroundWindow();
        var currentThread = GetCurrentThreadId();
        var foregroundThread = foreground == IntPtr.Zero
            ? 0u
            : GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var targetThread = GetWindowThreadProcessId(child, IntPtr.Zero);

        var attachedForeground = foregroundThread != 0 && foregroundThread != currentThread &&
                                 AttachThreadInput(currentThread, foregroundThread, true);
        var attachedTarget = targetThread != 0 && targetThread != currentThread && targetThread != foregroundThread &&
                             AttachThreadInput(currentThread, targetThread, true);

        try
        {
            SetActiveWindow(root);
            SetFocus(child);
        }
        finally
        {
            if (attachedTarget)
            {
                AttachThreadInput(currentThread, targetThread, false);
            }

            if (attachedForeground)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }

        return GetForegroundWindow() == root || IsChild(root, GetFocus());
    }

    private static void SendMouse(uint flags, uint mouseData)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = mouseData,
                        dwFlags = flags
                    }
                }
            }
        };

        SendChecked(inputs);
    }

    private static void SendVirtualKey(ushort virtualKey, bool keyUp)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        dwFlags = keyUp ? KeyEventKeyUp : 0
                    }
                }
            }
        };

        SendChecked(inputs);
    }

    private static INPUT CreateUnicodeInput(char character, bool keyUp)
    {
        return new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wScan = character,
                    dwFlags = KeyEventUnicode | (keyUp ? KeyEventKeyUp : 0)
                }
            }
        };
    }

    private static void SendChecked(INPUT[] inputs)
    {
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != (uint)inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows 입력 전송에 실패했습니다.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsChild(IntPtr parent, IntPtr child);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);
}
