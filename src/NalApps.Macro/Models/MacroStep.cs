using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

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
	KeyHold,
	Delay
}

public sealed class MacroStep : INotifyPropertyChanged
{
	private string _runtimeStatus = string.Empty;

	public MacroStepType Type { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public int Value { get; set; }
	public string Text { get; set; } = string.Empty;
	public bool HasPosition { get; set; }
	public int RepeatCount { get; set; } = 1;
	public int IntervalMilliseconds { get; set; } = 100;
	public int DurationMilliseconds { get; set; }

	[JsonIgnore]
	public string RuntimeStatus
	{
		get => _runtimeStatus;
		set
		{
			if (string.Equals(_runtimeStatus, value, StringComparison.Ordinal))
			{
				return;
			}

			_runtimeStatus = value;
			OnPropertyChanged();
		}
	}

	public string Summary => Type switch
	{
		MacroStepType.MouseMove => HasPosition
			? $"마우스 이동  X {X} / Y {Y}"
			: "마우스 이동",
		MacroStepType.LeftClick => BuildMouseSummary("왼쪽 클릭"),
		MacroStepType.RightClick => BuildMouseSummary("오른쪽 클릭"),
		MacroStepType.DoubleClick => BuildMouseSummary("더블 클릭"),
		MacroStepType.MouseWheel => BuildWheelSummary(),
		MacroStepType.TextInput => $"문자 입력  {Preview(Text)}",
		MacroStepType.KeyPress => $"키 입력  {Text}",
		MacroStepType.KeyHold => $"키 누르고 있기  {Text} · {Value / 1000d:0.###}초",
		MacroStepType.Delay => $"대기  {Value / 1000d:0.###}초",
		_ => Type.ToString()
	};

	public event PropertyChangedEventHandler? PropertyChanged;

	public void NormalizeLegacyDefaults()
	{
		if (RepeatCount < 1)
		{
			RepeatCount = 1;
		}

		if (IntervalMilliseconds < 0)
		{
			IntervalMilliseconds = 0;
		}

		if (Type == MacroStepType.MouseMove && !HasPosition && (X != 0 || Y != 0))
		{
			HasPosition = true;
		}
	}

	private string BuildMouseSummary(string action)
	{
		var position = HasPosition ? $" · X {X} / Y {Y}" : " · 현재 위치";
		if (DurationMilliseconds > 0)
		{
			return $"마우스 {action} 연속 · {DurationMilliseconds / 1000d:0.###}초 · {IntervalMilliseconds}ms 간격{position}";
		}

		if (RepeatCount > 1)
		{
			return $"마우스 {action} · {RepeatCount}회 · {IntervalMilliseconds}ms 간격{position}";
		}

		return $"마우스 {action}{position}";
	}

	private string BuildWheelSummary()
	{
		var direction = Value >= 0 ? "위" : "아래";
		var position = HasPosition ? $" · X {X} / Y {Y}" : " · 현재 위치";
		return $"마우스 휠 {direction} · {RepeatCount}회{position}";
	}

	private static string Preview(string value)
	{
		var normalized = (value ?? string.Empty)
			.Replace("\r", string.Empty, StringComparison.Ordinal)
			.Replace("\n", " ↵ ", StringComparison.Ordinal)
			.Replace("\t", " ⇥ ", StringComparison.Ordinal);

		return normalized.Length <= 28 ? normalized : normalized[..28] + "…";
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed class MacroDocument
{
	public int SchemaVersion { get; set; } = 2;
	public string Name { get; set; } = "새 매크로";
	public int RepeatCount { get; set; } = 1;
	public bool InfiniteRepeat { get; set; }
	public List<MacroStep> Steps { get; set; } = [];
}
