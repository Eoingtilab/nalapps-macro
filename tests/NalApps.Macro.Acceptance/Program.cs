using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NalApps.Macro.Core;
using NalApps.Macro.Models;

namespace NalApps.Macro.Acceptance;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    [STAThread]
    private static async Task<int> Main()
    {
        Console.WriteLine("NalaApps Macro comprehensive acceptance test");
        Console.WriteLine("ISO/IEC/IEEE 29119-aligned test process · ISO/IEC 25010 quality coverage");

        Run("KEY-AZ", TestAlphabet);
        Run("KEY-09", TestNumbers);
        Run("KEY-F1-F24", TestFunctionKeys);
        Run("KEY-SHORTCUTS", TestShortcuts);
        Run("KEY-INVALID", TestInvalidKeys);
        Run("VAL-ALL-TYPES", TestAllStepValidation);
        Run("VAL-BOUNDARIES", TestBoundaries);

        await RunAsync("MOUSE-MOVE", TestMouseMove);
        await RunAsync("MOUSE-LEFT", TestLeftClick);
        await RunAsync("MOUSE-RIGHT", TestRightClick);
        await RunAsync("MOUSE-DOUBLE", TestDoubleClick);
        await RunAsync("MOUSE-REPEAT", TestRepeatedClick);
        await RunAsync("MOUSE-CONTINUOUS", TestContinuousClick);
        await RunAsync("WHEEL-UP-COUNT", () => TestWheelCount(120));
        await RunAsync("WHEEL-DOWN-COUNT", () => TestWheelCount(-120));
        await RunAsync("WHEEL-DURATION", TestWheelDuration);
        await RunAsync("WHEEL-CANCEL", TestWheelCancellation);
        await RunAsync("TEXT-UNICODE", TestUnicodeText);
        await RunAsync("KEY-PRESS", TestKeyPress);
        await RunAsync("KEY-HOLD", TestKeyHold);
        await RunAsync("KEY-HOLD-CANCEL", TestKeyHoldCancellation);
        await RunAsync("DELAY", TestDelay);
        Run("SERIALIZATION", TestSerialization);
        Run("LEGACY-SCHEMA", TestLegacySchema);
        Run("UI-CONTRACTS", TestUiContracts);
        Run("UI-VISIBILITY-CONTRACT", TestNoHiddenRunCode);
        Run("DESIGN-CONTRACT", TestDesignContracts);
        Run("RELEASE-CONTRACT", TestReleaseContracts);

        Console.WriteLine($"RESULT: {(_failed == 0 ? "PASS" : "FAIL")} · passed={_passed} · failed={_failed}");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action action)
    {
        try { action(); _passed++; Console.WriteLine($"[PASS] {name}"); }
        catch (Exception ex) { _failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
    }

    private static async Task RunAsync(string name, Func<Task> action)
    {
        try { await action(); _passed++; Console.WriteLine($"[PASS] {name}"); }
        catch (Exception ex) { _failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
    }

    private static void TestAlphabet()
    {
        for (char c = 'A'; c <= 'Z'; c++) Equal((ushort)c, KeyExpressionParser.Parse(c.ToString()).Single());
    }

    private static void TestNumbers()
    {
        for (char c = '0'; c <= '9'; c++) Equal((ushort)c, KeyExpressionParser.Parse(c.ToString()).Single());
    }

    private static void TestFunctionKeys()
    {
        for (int i = 1; i <= 24; i++) Equal((ushort)(0x70 + i - 1), KeyExpressionParser.Parse($"F{i}").Single());
    }

    private static void TestShortcuts()
    {
        Seq(new ushort[] { 0x11, 0x43 }, KeyExpressionParser.Parse("CTRL+C"));
        Seq(new ushort[] { 0x11, 0x10, 0x53 }, KeyExpressionParser.Parse("CTRL+SHIFT+S"));
        Seq(new ushort[] { 0x12, 0x09 }, KeyExpressionParser.Parse("ALT+TAB"));
        Seq(new ushort[] { 0x5B, 0x44 }, KeyExpressionParser.Parse("WIN+D"));
        Equal((ushort)0x20, KeyExpressionParser.Parse("스페이스바").Single());
    }

    private static void TestInvalidKeys()
    {
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse(""));
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse("CTRL+CTRL+C"));
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse("UNKNOWN"));
        Throws<InvalidOperationException>(() => KeyExpressionParser.Parse("CTRL+ALT+DELETE"));
    }

    private static void TestAllStepValidation()
    {
        MacroStep[] valid =
        [
            new() { Type = MacroStepType.MouseMove, HasPosition = true, X = 1, Y = 2 },
            new() { Type = MacroStepType.LeftClick, RepeatCount = 1, IntervalMilliseconds = 100 },
            new() { Type = MacroStepType.RightClick, RepeatCount = 1, IntervalMilliseconds = 100 },
            new() { Type = MacroStepType.DoubleClick, RepeatCount = 1, IntervalMilliseconds = 100 },
            new() { Type = MacroStepType.MouseWheel, Value = 120, RepeatCount = 3, IntervalMilliseconds = 140 },
            new() { Type = MacroStepType.MouseWheel, Value = -120, DurationMilliseconds = 10_000, IntervalMilliseconds = 140 },
            new() { Type = MacroStepType.TextInput, Text = "테스트", IntervalMilliseconds = 20 },
            new() { Type = MacroStepType.KeyPress, Text = "ENTER" },
            new() { Type = MacroStepType.KeyHold, Text = "SPACE", Value = 1_000 },
            new() { Type = MacroStepType.Delay, Value = 1_000 }
        ];
        foreach (var step in valid) Empty(MacroStepValidator.Validate(step));
    }

    private static void TestBoundaries()
    {
        NotEmpty(MacroStepValidator.Validate(new() { Type = MacroStepType.Delay, Value = 999 }));
        Empty(MacroStepValidator.Validate(new() { Type = MacroStepType.Delay, Value = 86_400_000 }));
        NotEmpty(MacroStepValidator.Validate(new() { Type = MacroStepType.Delay, Value = 86_400_001 }));
        NotEmpty(MacroStepValidator.Validate(new() { Type = MacroStepType.MouseWheel, Value = 0, RepeatCount = 1, IntervalMilliseconds = 140 }));
        NotEmpty(MacroStepValidator.Validate(new() { Type = MacroStepType.LeftClick, RepeatCount = 0, IntervalMilliseconds = 100 }));
        NotEmpty(MacroStepValidator.Validate(new() { Type = MacroStepType.TextInput, Text = "", IntervalMilliseconds = 20 }));
    }

    private static async Task TestMouseMove()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.MouseMove, HasPosition = true, X = 10, Y = 20 }, default);
        Seq(new[] { "move:10,20" }, f.Driver.Events);
    }

    private static async Task TestLeftClick()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.LeftClick, RepeatCount = 1, IntervalMilliseconds = 100 }, default);
        ContainsInOrder(f.Driver.Events, "activate-cursor", "down:Left", "up:Left");
    }

    private static async Task TestRightClick()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.RightClick, RepeatCount = 1, IntervalMilliseconds = 100 }, default);
        ContainsInOrder(f.Driver.Events, "activate-cursor", "down:Right", "up:Right");
    }

    private static async Task TestDoubleClick()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.DoubleClick, RepeatCount = 1, IntervalMilliseconds = 100 }, default);
        Equal(2, f.Driver.Events.Count(x => x == "down:Left"));
        Equal(2, f.Driver.Events.Count(x => x == "up:Left"));
    }

    private static async Task TestRepeatedClick()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.LeftClick, RepeatCount = 3, IntervalMilliseconds = 200 }, default);
        Equal(3, f.Driver.Events.Count(x => x == "down:Left"));
        Equal(3, f.Driver.Events.Count(x => x == "up:Left"));
        Equal(2, f.Delay.Durations.Count(x => x == 200));
    }

    private static async Task TestContinuousClick()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.LeftClick, DurationMilliseconds = 1_000, IntervalMilliseconds = 200 }, default);
        True(f.Driver.Events.Count(x => x == "down:Left") >= 5);
    }

    private static async Task TestWheelCount(int delta)
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.MouseWheel, Value = delta, RepeatCount = 5, IntervalMilliseconds = 140 }, default);
        Equal(5, f.Driver.Events.Count(x => x == $"wheel:{delta}"));
        False(f.Driver.Events.Any(x => x.StartsWith("up:", StringComparison.Ordinal)));
    }

    private static async Task TestWheelDuration()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.MouseWheel, Value = -120, DurationMilliseconds = 1_000, IntervalMilliseconds = 200 }, default);
        Equal(5, f.Driver.Events.Count(x => x == "wheel:-120"));
        Equal(4, f.Delay.Durations.Count(x => x == 200));
    }

    private static async Task TestWheelCancellation()
    {
        using var cts = new CancellationTokenSource();
        var driver = new FakeDriver();
        var delay = new FakeDelay((_, count) => { if (count >= 3) cts.Cancel(); });
        var executor = new MacroExecutor(driver, delay);
        await ThrowsAsync<OperationCanceledException>(() => executor.ExecuteStepAsync(new() { Type = MacroStepType.MouseWheel, Value = -120, DurationMilliseconds = 10_000, IntervalMilliseconds = 140 }, cts.Token));
        True(driver.Events.Count(x => x == "wheel:-120") >= 1);
    }

    private static async Task TestUnicodeText()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.TextInput, Text = "가\n\tA", IntervalMilliseconds = 0 }, default);
        ContainsInOrder(f.Driver.Events, "unicode:가", "key-down:13", "key-up:13", "key-down:9", "key-up:9", "unicode:A");
    }

    private static async Task TestKeyPress()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.KeyPress, Text = "CTRL+C" }, default);
        ContainsInOrder(f.Driver.Events, "key-down:17", "key-down:67", "key-up:67", "key-up:17");
    }

    private static async Task TestKeyHold()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.KeyHold, Text = "SPACE", Value = 1_000 }, default);
        ContainsInOrder(f.Driver.Events, "key-down:32", "key-up:32");
        True(f.Delay.Durations.Contains(1_000));
    }

    private static async Task TestKeyHoldCancellation()
    {
        using var cts = new CancellationTokenSource();
        var driver = new FakeDriver();
        var delay = new FakeDelay((ms, _) => { if (ms == 1_000) cts.Cancel(); });
        var executor = new MacroExecutor(driver, delay);
        await ThrowsAsync<OperationCanceledException>(() => executor.ExecuteStepAsync(new() { Type = MacroStepType.KeyHold, Text = "CTRL+C", Value = 1_000 }, cts.Token));
        ContainsInOrder(driver.Events, "key-down:17", "key-down:67", "key-up:67", "key-up:17");
    }

    private static async Task TestDelay()
    {
        var f = Fixture();
        await f.Executor.ExecuteStepAsync(new() { Type = MacroStepType.Delay, Value = 5_000 }, default);
        True(f.Delay.Durations.Contains(5_000));
    }

    private static void TestSerialization()
    {
        var source = new MacroDocument { Name = "all", RepeatCount = 2, Steps = [new() { Type = MacroStepType.MouseWheel, Value = -120, DurationMilliseconds = 10_000, IntervalMilliseconds = 140 }] };
        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<MacroDocument>(json) ?? throw new InvalidOperationException("deserialize failed");
        Equal(2, restored.SchemaVersion);
        Equal(10_000, restored.Steps.Single().DurationMilliseconds);
    }

    private static void TestLegacySchema()
    {
        const string json = "{\"SchemaVersion\":1,\"Name\":\"legacy\",\"RepeatCount\":1,\"InfiniteRepeat\":false,\"Steps\":[{\"Type\":1,\"X\":0,\"Y\":0,\"Value\":0,\"Text\":\"\"}]}";
        var doc = JsonSerializer.Deserialize<MacroDocument>(json) ?? throw new InvalidOperationException();
        var step = doc.Steps.Single();
        step.NormalizeLegacyDefaults();
        Equal(1, step.RepeatCount);
        Equal(100, step.IntervalMilliseconds);
    }

    private static void TestUiContracts()
    {
        var root = Root();
        var main = File.ReadAllText(Path.Combine(root, "src", "NalApps.Macro", "MainWindow.xaml"));
        foreach (var text in new[] { "키보드", "마우스", "시간", "문자", "실행", "일시정지", "중지", "DeleteInline_Click" }) Contains(main, text);
        var mouse = File.ReadAllText(Path.Combine(root, "src", "NalApps.Macro", "MouseActionDialog.xaml"));
        foreach (var text in new[] { "한 번", "횟수 지정", "연속 실행", "마우스 동작 적용" }) Contains(mouse, text);
    }

    private static void TestNoHiddenRunCode()
    {
        var file = File.ReadAllText(Path.Combine(Root(), "src", "NalApps.Macro", "MainWindow.UiActions.cs"));
        False(file.Contains("Hide();", StringComparison.Ordinal));
        False(file.Contains("RunHidden_Click", StringComparison.Ordinal));
        Contains(file, "RunVisible_Click");
    }

    private static void TestDesignContracts()
    {
        var theme = File.ReadAllText(Path.Combine(Root(), "src", "NalApps.Macro", "Themes", "NalaApps.DesignSystem.xaml"));
        Contains(theme, "PrimaryButtonStyle");
        Contains(theme, "DangerButtonStyle");
        True(theme.Split("#FFFFFF", StringSplitOptions.None).Length >= 4);
        Contains(theme, "NalaCard");
    }

    private static void TestReleaseContracts()
    {
        var project = File.ReadAllText(Path.Combine(Root(), "src", "NalApps.Macro", "NalApps.Macro.csproj"));
        Contains(project, "<TargetFramework>net8.0-windows</TargetFramework>");
        Contains(project, "<PublishSingleFile>true</PublishSingleFile>");
        Contains(project, "<SelfContained>true</SelfContained>");
        var workflow = File.ReadAllText(Path.Combine(Root(), ".github", "workflows", "build.yml"));
        Contains(workflow, "-warnaserror");
        Contains(workflow, "SHA256SUMS.txt");
    }

    private static (MacroExecutor Executor, FakeDriver Driver, FakeDelay Delay) Fixture()
    {
        var driver = new FakeDriver();
        var delay = new FakeDelay();
        return (new MacroExecutor(driver, delay), driver, delay);
    }

    private static string Root()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "NalApps.Macro", "NalApps.Macro.csproj"))) return dir.FullName;
                dir = dir.Parent;
            }
        }
        throw new DirectoryNotFoundException();
    }

    private static void Equal<T>(T expected, T actual) where T : notnull { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"expected={expected}, actual={actual}"); }
    private static void True(bool value) { if (!value) throw new InvalidOperationException("expected true"); }
    private static void False(bool value) { if (value) throw new InvalidOperationException("expected false"); }
    private static void Empty<T>(IReadOnlyCollection<T> values) { if (values.Count != 0) throw new InvalidOperationException(string.Join(" | ", values)); }
    private static void NotEmpty<T>(IReadOnlyCollection<T> values) { if (values.Count == 0) throw new InvalidOperationException("expected errors"); }
    private static void Contains(string source, string value) { if (!source.Contains(value, StringComparison.Ordinal)) throw new InvalidOperationException($"missing: {value}"); }
    private static void Seq<T>(IEnumerable<T> expected, IEnumerable<T> actual) { if (!expected.SequenceEqual(actual)) throw new InvalidOperationException($"expected=[{string.Join(',', expected)}], actual=[{string.Join(',', actual)}]"); }
    private static void ContainsInOrder(IReadOnlyList<string> actual, params string[] expected)
    {
        int at = 0;
        foreach (var item in actual) if (at < expected.Length && item == expected[at]) at++;
        if (at != expected.Length) throw new InvalidOperationException($"sequence missing: {string.Join(" -> ", expected)}; actual={string.Join(',', actual)}");
    }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"expected {typeof(T).Name}"); }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new InvalidOperationException($"expected {typeof(T).Name}"); }

    private sealed class FakeDelay : IMacroDelay
    {
        private readonly Action<int, int>? _callback;
        public List<int> Durations { get; } = [];
        public FakeDelay(Action<int, int>? callback = null) => _callback = callback;
        public Task DelayAsync(int milliseconds, CancellationToken token)
        {
            Durations.Add(milliseconds);
            _callback?.Invoke(milliseconds, Durations.Count);
            return token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
        }
    }

    private sealed class FakeDriver : IMacroInputDriver
    {
        public List<string> Events { get; } = [];
        public bool MoveMouse(int x, int y) { Events.Add($"move:{x},{y}"); return true; }
        public bool ActivateWindowAtPoint(int x, int y) { Events.Add($"activate:{x},{y}"); return true; }
        public bool ActivateWindowUnderCursor() { Events.Add("activate-cursor"); return true; }
        public void MouseButtonDown(MouseButtonKind button) => Events.Add($"down:{button}");
        public void MouseButtonUp(MouseButtonKind button) => Events.Add($"up:{button}");
        public void MouseWheel(int delta) => Events.Add($"wheel:{delta}");
        public void SendUnicodeCharacter(char character) => Events.Add($"unicode:{character}");
        public void KeyDown(ushort virtualKey) => Events.Add($"key-down:{virtualKey}");
        public void KeyUp(ushort virtualKey) => Events.Add($"key-up:{virtualKey}");
        public void ReleaseSafetyState() => Events.Add("release-safety");
    }
}
