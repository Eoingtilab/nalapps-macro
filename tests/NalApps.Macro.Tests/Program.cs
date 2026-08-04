using System.Text.Json;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro.Tests;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static async Task<int> Main()
    {
        Console.WriteLine("NalaApps Macro v1.1.0 automated verification");
        Console.WriteLine("ISO/IEC/IEEE 29119-aligned functional and regression test harness");
        Console.WriteLine();

        Run("KEY-001 A~Z virtual-key parsing", TestAlphabetKeys);
        Run("KEY-002 0~9 virtual-key parsing", TestNumberKeys);
        Run("KEY-003 common shortcut parsing", TestShortcutKeys);
        Run("KEY-004 F1~F24 parsing", TestFunctionKeys);
        Run("KEY-005 Korean key aliases", TestKoreanAliases);
        Run("KEY-006 duplicate key rejection", TestDuplicateKeyRejection);
        Run("KEY-007 unsupported key rejection", TestUnsupportedKeyRejection);
        Run("KEY-008 secure attention rejection", TestSecureAttentionRejection);

        Run("VAL-001 50-second delay accepted", TestFiftySecondDelayValidation);
        Run("VAL-002 delay boundary validation", TestDelayBoundaryValidation);
        Run("VAL-003 10-second space hold accepted", TestSpaceHoldValidation);
        Run("VAL-004 empty text rejected", TestEmptyTextValidation);
        Run("VAL-005 continuous click accepted", TestContinuousClickValidation);
        Run("VAL-006 unsafe click interval rejected", TestClickIntervalValidation);

        await RunAsync("EXE-001 current-position left click", TestCurrentPositionClickAsync);
        await RunAsync("EXE-002 fixed-position click moves first", TestFixedPositionClickAsync);
        await RunAsync("EXE-003 repeated clicks honor count and interval", TestRepeatedClickAsync);
        await RunAsync("EXE-004 continuous click honors duration", TestContinuousClickAsync);
        await RunAsync("EXE-005 double click sends two clicks", TestDoubleClickAsync);
        await RunAsync("EXE-006 right click uses right button", TestRightClickAsync);
        await RunAsync("EXE-007 wheel repeats", TestWheelAsync);
        await RunAsync("EXE-008 Unicode, Enter and Tab text", TestTextInputAsync);
        await RunAsync("EXE-009 text input interval", TestTextIntervalAsync);
        await RunAsync("EXE-010 shortcut press order", TestShortcutExecutionAsync);
        await RunAsync("EXE-011 held key releases after duration", TestHeldKeyAsync);
        await RunAsync("EXE-012 canceled held key releases safely", TestCanceledHeldKeyAsync);
        await RunAsync("EXE-013 failed mouse move is reported", TestMouseMoveFailureAsync);

        Run("SER-001 schema v2 round trip", TestSerializationRoundTrip);
        Run("SER-002 schema v1 compatibility defaults", TestLegacySerializationCompatibility);
        Run("MOD-001 summaries expose configured behavior", TestSummaries);
        Run("UI-001 every main action has a click handler", TestMainWindowActionContracts);
        Run("UI-002 colored button text is white", TestColoredButtonContract);
        Run("BLD-001 product version is 1.1.0", TestProductVersion);

        Console.WriteLine();
        Console.WriteLine($"RESULT: {(_failed == 0 ? "PASS" : "FAIL")} · passed={_passed} · failed={_failed}");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        catch (Exception exception)
        {
            _failed++;
            Console.WriteLine($"[FAIL] {name}: {exception.Message}");
        }
    }

    private static async Task RunAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        catch (Exception exception)
        {
            _failed++;
            Console.WriteLine($"[FAIL] {name}: {exception.Message}");
        }
    }

    private static void TestAlphabetKeys()
    {
        for (var character = 'A'; character <= 'Z'; character++)
        {
            var keys = KeyExpressionParser.Parse(character.ToString());
            Equal((ushort)character, keys.Single(), $"Key {character}");
        }
    }

    private static void TestNumberKeys()
    {
        for (var character = '0'; character <= '9'; character++)
        {
            var keys = KeyExpressionParser.Parse(character.ToString());
            Equal((ushort)character, keys.Single(), $"Key {character}");
        }
    }

    private static void TestShortcutKeys()
    {
        SequenceEqual(new ushort[] { 0x11, 0x43 }, KeyExpressionParser.Parse("CTRL+C"), "Ctrl+C");
        SequenceEqual(new ushort[] { 0x11, 0x10, 0x53 }, KeyExpressionParser.Parse("CTRL+SHIFT+S"), "Ctrl+Shift+S");
        SequenceEqual(new ushort[] { 0x12, 0x09 }, KeyExpressionParser.Parse("ALT+TAB"), "Alt+Tab");
        SequenceEqual(new ushort[] { 0x5B, 0x44 }, KeyExpressionParser.Parse("WIN+D"), "Win+D");
    }

    private static void TestFunctionKeys()
    {
        for (var number = 1; number <= 24; number++)
        {
            var key = KeyExpressionParser.Parse($"F{number}").Single();
            Equal((ushort)(0x70 + number - 1), key, $"F{number}");
        }
    }

    private static void TestKoreanAliases()
    {
        SequenceEqual(new ushort[] { 0x11, 0x43 }, KeyExpressionParser.Parse("컨트롤+C"), "Korean Ctrl alias");
        Equal((ushort)0x20, KeyExpressionParser.Parse("스페이스바").Single(), "Korean Space alias");
    }

    private static void TestDuplicateKeyRejection()
    {
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse("CTRL+CTRL+C"));
    }

    private static void TestUnsupportedKeyRejection()
    {
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse("UNKNOWN_KEY"));
    }

    private static void TestSecureAttentionRejection()
    {
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse("CTRL+ALT+DELETE"));
    }

    private static void TestFiftySecondDelayValidation()
    {
        var step = new MacroStep { Type = MacroStepType.Delay, Value = 50_000 };
        Empty(MacroStepValidator.Validate(step), "50-second delay should be valid");
    }

    private static void TestDelayBoundaryValidation()
    {
        NotEmpty(MacroStepValidator.Validate(new MacroStep { Type = MacroStepType.Delay, Value = 999 }), "Sub-second delay should be rejected");
        Empty(MacroStepValidator.Validate(new MacroStep { Type = MacroStepType.Delay, Value = 86_400_000 }), "24-hour delay boundary");
        NotEmpty(MacroStepValidator.Validate(new MacroStep { Type = MacroStepType.Delay, Value = 86_400_001 }), "Over 24 hours should be rejected");
    }

    private static void TestSpaceHoldValidation()
    {
        var step = new MacroStep { Type = MacroStepType.KeyHold, Text = "SPACE", Value = 10_000 };
        Empty(MacroStepValidator.Validate(step), "10-second space hold should be valid");
    }

    private static void TestEmptyTextValidation()
    {
        NotEmpty(MacroStepValidator.Validate(new MacroStep { Type = MacroStepType.TextInput, Text = string.Empty }), "Empty text should be rejected");
    }

    private static void TestContinuousClickValidation()
    {
        var step = new MacroStep
        {
            Type = MacroStepType.LeftClick,
            DurationMilliseconds = 10_000,
            IntervalMilliseconds = 100
        };
        Empty(MacroStepValidator.Validate(step), "Continuous click should be valid");
    }

    private static void TestClickIntervalValidation()
    {
        var step = new MacroStep
        {
            Type = MacroStepType.LeftClick,
            RepeatCount = 2,
            IntervalMilliseconds = 1
        };
        NotEmpty(MacroStepValidator.Validate(step), "1ms click interval should be rejected");
    }

    private static async Task TestCurrentPositionClickAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.LeftClick,
            RepeatCount = 1,
            IntervalMilliseconds = 100
        }, CancellationToken.None);

        SequenceEqual(new[] { "mouse-down:Left", "mouse-up:Left" }, fixture.Driver.Events, "Current-position click events");
    }

    private static async Task TestFixedPositionClickAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.LeftClick,
            HasPosition = true,
            X = 123,
            Y = 456,
            RepeatCount = 1,
            IntervalMilliseconds = 100
        }, CancellationToken.None);

        SequenceEqual(new[] { "move:123,456", "mouse-down:Left", "mouse-up:Left" }, fixture.Driver.Events, "Fixed-position click events");
    }

    private static async Task TestRepeatedClickAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.LeftClick,
            RepeatCount = 3,
            IntervalMilliseconds = 100
        }, CancellationToken.None);

        Equal(6, fixture.Driver.Events.Count, "Three clicks should produce six button events");
        SequenceEqual(new[] { 100, 100 }, fixture.Delay.Durations, "Repeat delays");
    }

    private static async Task TestContinuousClickAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.LeftClick,
            DurationMilliseconds = 350,
            IntervalMilliseconds = 100
        }, CancellationToken.None);

        Equal(8, fixture.Driver.Events.Count, "350ms/100ms should execute four clicks");
        SequenceEqual(new[] { 100, 100, 100 }, fixture.Delay.Durations, "Continuous delays");
    }

    private static async Task TestDoubleClickAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.DoubleClick,
            RepeatCount = 1,
            IntervalMilliseconds = 100
        }, CancellationToken.None);

        SequenceEqual(new[]
        {
            "mouse-down:Left", "mouse-up:Left", "mouse-down:Left", "mouse-up:Left"
        }, fixture.Driver.Events, "Double click events");
        SequenceEqual(new[] { 80 }, fixture.Delay.Durations, "Double click internal delay");
    }

    private static async Task TestRightClickAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.RightClick,
            RepeatCount = 1,
            IntervalMilliseconds = 100
        }, CancellationToken.None);

        SequenceEqual(new[] { "mouse-down:Right", "mouse-up:Right" }, fixture.Driver.Events, "Right click events");
    }

    private static async Task TestWheelAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.MouseWheel,
            Value = -120,
            RepeatCount = 3,
            IntervalMilliseconds = 250
        }, CancellationToken.None);

        SequenceEqual(new[] { "wheel:-120", "wheel:-120", "wheel:-120" }, fixture.Driver.Events, "Wheel events");
        SequenceEqual(new[] { 250, 250 }, fixture.Delay.Durations, "Wheel repeat delays");
    }

    private static async Task TestTextInputAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.TextInput,
            Text = "가\n\tA",
            IntervalMilliseconds = 0
        }, CancellationToken.None);

        SequenceEqual(new[]
        {
            "unicode:가", "key-down:13", "key-up:13", "key-down:9", "key-up:9", "unicode:A"
        }, fixture.Driver.Events, "Text input events");
    }

    private static async Task TestTextIntervalAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.TextInput,
            Text = "ABC",
            IntervalMilliseconds = 50
        }, CancellationToken.None);

        SequenceEqual(new[] { 50, 50 }, fixture.Delay.Durations, "Character delays");
    }

    private static async Task TestShortcutExecutionAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.KeyPress,
            Text = "CTRL+SHIFT+S"
        }, CancellationToken.None);

        SequenceEqual(new[]
        {
            "key-down:17", "key-down:16", "key-down:83", "key-up:83", "key-up:16", "key-up:17"
        }, fixture.Driver.Events, "Shortcut event order");
    }

    private static async Task TestHeldKeyAsync()
    {
        var fixture = CreateFixture();
        await fixture.Executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.KeyHold,
            Text = "SPACE",
            Value = 10_000
        }, CancellationToken.None);

        SequenceEqual(new[] { "key-down:32", "key-up:32" }, fixture.Driver.Events, "Held key events");
        SequenceEqual(new[] { 10_000 }, fixture.Delay.Durations, "Held key duration");
    }

    private static async Task TestCanceledHeldKeyAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var driver = new FakeDriver();
        var delay = new FakeDelay(() => cancellation.Cancel());
        var executor = new MacroExecutor(driver, delay);

        await ThrowsAsync<OperationCanceledException>(() => executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.KeyHold,
            Text = "CTRL+C",
            Value = 10_000
        }, cancellation.Token));

        SequenceEqual(new[]
        {
            "key-down:17", "key-down:67", "key-up:67", "key-up:17"
        }, driver.Events, "Canceled hold must release in reverse order");
    }

    private static async Task TestMouseMoveFailureAsync()
    {
        var driver = new FakeDriver { MoveResult = false };
        var executor = new MacroExecutor(driver, new FakeDelay());
        await ThrowsAsync<InvalidOperationException>(() => executor.ExecuteStepAsync(new MacroStep
        {
            Type = MacroStepType.MouseMove,
            HasPosition = true,
            X = 1,
            Y = 2
        }, CancellationToken.None));
    }

    private static void TestSerializationRoundTrip()
    {
        var source = new MacroDocument
        {
            Name = "roundtrip",
            RepeatCount = 2,
            Steps =
            [
                new MacroStep
                {
                    Type = MacroStepType.LeftClick,
                    HasPosition = true,
                    X = 100,
                    Y = 200,
                    DurationMilliseconds = 10_000,
                    IntervalMilliseconds = 100
                },
                new MacroStep { Type = MacroStepType.TextInput, Text = "테스트", IntervalMilliseconds = 20 }
            ]
        };

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<MacroDocument>(json) ?? throw new InvalidOperationException("Deserialization failed");
        Equal(2, restored.SchemaVersion, "Schema version");
        Equal(2, restored.Steps.Count, "Step count");
        Equal(10_000, restored.Steps[0].DurationMilliseconds, "Duration round trip");
        Equal("테스트", restored.Steps[1].Text, "Unicode round trip");
    }

    private static void TestLegacySerializationCompatibility()
    {
        const string json = "{\"SchemaVersion\":1,\"Name\":\"legacy\",\"RepeatCount\":1,\"InfiniteRepeat\":false,\"Steps\":[{\"Type\":1,\"X\":0,\"Y\":0,\"Value\":0,\"Text\":\"\"}]}";
        var document = JsonSerializer.Deserialize<MacroDocument>(json) ?? throw new InvalidOperationException("Legacy deserialization failed");
        var step = document.Steps.Single();
        step.NormalizeLegacyDefaults();
        Equal(1, step.RepeatCount, "Legacy repeat default");
        Equal(100, step.IntervalMilliseconds, "Legacy interval default");
        Empty(MacroStepValidator.Validate(step), "Legacy click should remain executable");
    }

    private static void TestSummaries()
    {
        var continuous = new MacroStep
        {
            Type = MacroStepType.LeftClick,
            DurationMilliseconds = 10_000,
            IntervalMilliseconds = 100
        };
        Contains("연속", continuous.Summary, "Continuous summary");
        Contains("10초", continuous.Summary, "Duration summary");

        var hold = new MacroStep { Type = MacroStepType.KeyHold, Text = "SPACE", Value = 10_000 };
        Contains("SPACE", hold.Summary, "Hold key summary");
        Contains("10초", hold.Summary, "Hold duration summary");
    }

    private static void TestMainWindowActionContracts()
    {
        var root = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "NalApps.Macro", "MainWindow.xaml"));
        foreach (var handler in new[]
        {
            "AddKeyboard_Click", "AddMouse_Click", "AddDelay_Click", "AddText_Click",
            "OpenActionMenu_Click", "AddMousePreset_Click", "AddQuickAction_Click", "Run_Click"
        })
        {
            Contains(handler, xaml, $"Missing UI handler {handler}");
        }

        Contains("연속 왼쪽 클릭", xaml, "Continuous left click menu");
        Contains("연속 오른쪽 클릭", xaml, "Continuous right click menu");
    }

    private static void TestColoredButtonContract()
    {
        var root = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "NalApps.Macro", "Themes", "NalaApps.DesignSystem.xaml"));
        Contains("x:Key=\"PrimaryButtonStyle\"", xaml, "Primary style");
        Contains("x:Key=\"DangerButtonStyle\"", xaml, "Danger style");
        var whiteCount = CountOccurrences(xaml, "Value=\"#FFFFFF\"");
        True(whiteCount >= 4, "Primary and danger styles must force white text");
        Contains("NalaColoredButtonTemplate", xaml, "Colored button template");
    }

    private static void TestProductVersion()
    {
        var root = FindRepositoryRoot();
        var project = File.ReadAllText(Path.Combine(root, "src", "NalApps.Macro", "NalApps.Macro.csproj"));
        Contains("<Version>1.1.0</Version>", project, "Product version");
    }

    private static Fixture CreateFixture()
    {
        var driver = new FakeDriver();
        var delay = new FakeDelay();
        return new Fixture(driver, delay, new MacroExecutor(driver, delay));
    }

    private static string FindRepositoryRoot()
    {
        var candidates = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var candidate in candidates)
        {
            var current = new DirectoryInfo(candidate);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "src", "NalApps.Macro", "MainWindow.xaml")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
        }
    }

    private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException($"{message}: expected=[{string.Join(",", expected)}], actual=[{string.Join(",", actual)}]");
        }
    }

    private static void Empty<T>(IReadOnlyCollection<T> values, string message)
    {
        if (values.Count != 0)
        {
            throw new InvalidOperationException($"{message}: {string.Join(" | ", values)}");
        }
    }

    private static void NotEmpty<T>(IReadOnlyCollection<T> values, string message)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Contains(string expected, string actual, string message)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{message}: '{expected}' not found");
        }
    }

    private static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}");
    }

    private static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}");
    }

    private sealed record Fixture(FakeDriver Driver, FakeDelay Delay, MacroExecutor Executor);

    private sealed class FakeDelay : IMacroDelay
    {
        private readonly Action? _onDelay;
        public List<int> Durations { get; } = [];

        public FakeDelay(Action? onDelay = null)
        {
            _onDelay = onDelay;
        }

        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            Durations.Add(milliseconds);
            _onDelay?.Invoke();
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }
    }

    private sealed class FakeDriver : IMacroInputDriver
    {
        public List<string> Events { get; } = [];
        public bool MoveResult { get; set; } = true;

        public bool MoveMouse(int x, int y)
        {
            Events.Add($"move:{x},{y}");
            return MoveResult;
        }

        public void MouseButtonDown(MouseButtonKind button) => Events.Add($"mouse-down:{button}");
        public void MouseButtonUp(MouseButtonKind button) => Events.Add($"mouse-up:{button}");
        public void MouseWheel(int delta) => Events.Add($"wheel:{delta}");
        public void SendUnicodeCharacter(char character) => Events.Add($"unicode:{character}");
        public void KeyDown(ushort virtualKey) => Events.Add($"key-down:{virtualKey}");
        public void KeyUp(ushort virtualKey) => Events.Add($"key-up:{virtualKey}");
        public void ReleaseSafetyState() => Events.Add("release-safety");
    }
}
