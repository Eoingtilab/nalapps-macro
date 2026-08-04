namespace NalApps.Macro.Models;

public enum MacroStepType
{
    MouseMove,
    LeftClick,
    RightClick,
    DoubleClick,
    MouseWheel,
    TextInput,
    KeyPress,
    Delay
}

public sealed class MacroStep
{
    public MacroStepType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;

    public string Summary => Type switch
    {
        MacroStepType.MouseMove => $"마우스 이동  X {X} / Y {Y}",
        MacroStepType.LeftClick => "마우스 왼쪽 클릭",
        MacroStepType.RightClick => "마우스 오른쪽 클릭",
        MacroStepType.DoubleClick => "마우스 더블 클릭",
        MacroStepType.MouseWheel => $"마우스 휠  {Value}",
        MacroStepType.TextInput => $"문자 입력  {Text}",
        MacroStepType.KeyPress => $"키 입력  {Text}",
        MacroStepType.Delay => $"대기  {Value}ms",
        _ => Type.ToString()
    };
}

public sealed class MacroDocument
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "새 매크로";
    public int RepeatCount { get; set; } = 1;
    public bool InfiniteRepeat { get; set; }
    public List<MacroStep> Steps { get; set; } = [];
}
