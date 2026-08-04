using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NalApps.Macro.Core;

public sealed class WindowsMacroInputDriver : IMacroInputDriver
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventWheel = 0x0800;

    public bool MoveMouse(int x, int y)
    {
        return SetCursorPos(x, y);
    }

    public void MouseButtonDown(MouseButtonKind button)
    {
        mouse_event(button == MouseButtonKind.Left ? MouseEventLeftDown : MouseEventRightDown, 0, 0, 0, UIntPtr.Zero);
    }

    public void MouseButtonUp(MouseButtonKind button)
    {
        mouse_event(button == MouseButtonKind.Left ? MouseEventLeftUp : MouseEventRightUp, 0, 0, 0, UIntPtr.Zero);
    }

    public void MouseWheel(int delta)
    {
        mouse_event(MouseEventWheel, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
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
    }

    public void KeyUp(ushort virtualKey)
    {
        SendVirtualKey(virtualKey, true);
    }

    public void ReleaseSafetyState()
    {
        MouseButtonUp(MouseButtonKind.Left);
        MouseButtonUp(MouseButtonKind.Right);

        foreach (var virtualKey in new ushort[] { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 })
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
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
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
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
