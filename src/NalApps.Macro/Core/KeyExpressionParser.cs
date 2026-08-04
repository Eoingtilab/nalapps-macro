namespace NalApps.Macro.Core;

public static class KeyExpressionParser
{
    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11,
        ["CONTROL"] = 0x11,
        ["컨트롤"] = 0x11,
        ["LCTRL"] = 0xA2,
        ["RCTRL"] = 0xA3,
        ["ALT"] = 0x12,
        ["알트"] = 0x12,
        ["LALT"] = 0xA4,
        ["RALT"] = 0xA5,
        ["SHIFT"] = 0x10,
        ["쉬프트"] = 0x10,
        ["시프트"] = 0x10,
        ["LSHIFT"] = 0xA0,
        ["RSHIFT"] = 0xA1,
        ["WIN"] = 0x5B,
        ["WINDOWS"] = 0x5B,
        ["윈도우"] = 0x5B,
        ["ENTER"] = 0x0D,
        ["RETURN"] = 0x0D,
        ["엔터"] = 0x0D,
        ["TAB"] = 0x09,
        ["탭"] = 0x09,
        ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B,
        ["이스케이프"] = 0x1B,
        ["SPACE"] = 0x20,
        ["SPACEBAR"] = 0x20,
        ["스페이스"] = 0x20,
        ["스페이스바"] = 0x20,
        ["BACKSPACE"] = 0x08,
        ["백스페이스"] = 0x08,
        ["DELETE"] = 0x2E,
        ["DEL"] = 0x2E,
        ["삭제"] = 0x2E,
        ["INSERT"] = 0x2D,
        ["INS"] = 0x2D,
        ["HOME"] = 0x24,
        ["END"] = 0x23,
        ["PAGEUP"] = 0x21,
        ["PGUP"] = 0x21,
        ["PAGEDOWN"] = 0x22,
        ["PGDN"] = 0x22,
        ["UP"] = 0x26,
        ["DOWN"] = 0x28,
        ["LEFT"] = 0x25,
        ["RIGHT"] = 0x27,
        ["CAPSLOCK"] = 0x14,
        ["NUMLOCK"] = 0x90,
        ["SCROLLLOCK"] = 0x91,
        ["PRINTSCREEN"] = 0x2C,
        ["PRTSC"] = 0x2C,
        ["PAUSE"] = 0x13,
        ["BREAK"] = 0x13,
        ["APPS"] = 0x5D,
        ["MENU"] = 0x5D,
        ["NUM+"] = 0x6B,
        ["NUMPLUS"] = 0x6B,
        ["NUM-"] = 0x6D,
        ["NUMMINUS"] = 0x6D,
        ["NUM*"] = 0x6A,
        ["NUMMULTIPLY"] = 0x6A,
        ["NUM/"] = 0x6F,
        ["NUMDIVIDE"] = 0x6F,
        ["NUM."] = 0x6E,
        ["NUMDECIMAL"] = 0x6E,
        [";"] = 0xBA,
        ["="] = 0xBB,
        [","] = 0xBC,
        ["-"] = 0xBD,
        ["."] = 0xBE,
        ["/"] = 0xBF,
        ["`"] = 0xC0,
        ["["] = 0xDB,
        ["\\"] = 0xDC,
        ["]"] = 0xDD,
        ["'"] = 0xDE
    };

    public static IReadOnlyList<ushort> Parse(string? expression)
    {
        var normalized = (expression ?? string.Empty).Replace('＋', '+');
        var tokens = normalized
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            throw new InvalidOperationException("키 또는 조합키를 입력해 주세요.");
        }

        if (tokens.Length > 8)
        {
            throw new InvalidOperationException("한 동작에는 최대 8개의 키를 조합할 수 있습니다.");
        }

        var result = new List<ushort>(tokens.Length);
        foreach (var token in tokens)
        {
            var key = ParseToken(token);
            if (result.Contains(key))
            {
                throw new InvalidOperationException($"같은 키를 중복해서 사용할 수 없습니다: {token}");
            }

            result.Add(key);
        }

        if (result.Contains(0x11) && result.Contains(0x12) && result.Contains(0x2E))
        {
            throw new InvalidOperationException("Ctrl+Alt+Delete는 Windows 보안 정책상 매크로 입력으로 실행할 수 없습니다.");
        }

        return result;
    }

    public static ushort ParseToken(string token)
    {
        var upper = token.Trim().ToUpperInvariant();
        if (NamedKeys.TryGetValue(upper, out var named))
        {
            return named;
        }

        if (upper.Length == 1)
        {
            var character = upper[0];
            if (character is >= 'A' and <= 'Z')
            {
                return character;
            }

            if (character is >= '0' and <= '9')
            {
                return character;
            }
        }

        if (upper.Length is 2 or 3 && upper[0] == 'F' && int.TryParse(upper[1..], out var functionNumber) && functionNumber is >= 1 and <= 24)
        {
            return (ushort)(0x70 + functionNumber - 1);
        }

        if (upper.StartsWith("NUM", StringComparison.Ordinal) && upper.Length == 4 && char.IsDigit(upper[3]))
        {
            return (ushort)(0x60 + (upper[3] - '0'));
        }

        throw new InvalidOperationException($"지원하지 않는 키 입력입니다: {token}");
    }

    public static bool IsModifier(ushort virtualKey)
    {
        return virtualKey is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;
    }
}
