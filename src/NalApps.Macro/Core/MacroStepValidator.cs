using NalApps.Macro.Models;

namespace NalApps.Macro.Core;

public static class MacroStepValidator
{
    public const int MaxDurationMilliseconds = 86_400_000;
    public const int MaxRepeatCount = 100_000;
    public const int MaxTextLength = 100_000;
    public const int MaxIntervalMilliseconds = 60_000;

    public static IReadOnlyList<string> Validate(MacroStep? step)
    {
        var errors = new List<string>();
        if (step is null)
        {
            errors.Add("동작 정보가 없습니다.");
            return errors;
        }

        switch (step.Type)
        {
            case MacroStepType.MouseMove:
                if (!step.HasPosition)
                {
                    errors.Add("마우스 이동 위치를 지정해 주세요.");
                }
                ValidatePosition(step, errors);
                break;

            case MacroStepType.LeftClick:
            case MacroStepType.RightClick:
            case MacroStepType.DoubleClick:
                ValidatePosition(step, errors);
                ValidateRepeat(step, errors);
                break;

            case MacroStepType.MouseWheel:
                ValidatePosition(step, errors);
                ValidateRepeat(step, errors);
                if (step.Value == 0)
                {
                    errors.Add("마우스 휠 이동량은 0이 될 수 없습니다.");
                }
                break;

            case MacroStepType.TextInput:
                if (string.IsNullOrEmpty(step.Text))
                {
                    errors.Add("입력할 문자를 작성해 주세요.");
                }
                else if (step.Text.Length > MaxTextLength)
                {
                    errors.Add($"문자 입력은 최대 {MaxTextLength:N0}자까지 사용할 수 있습니다.");
                }

                if (step.IntervalMilliseconds < 0 || step.IntervalMilliseconds > MaxIntervalMilliseconds)
                {
                    errors.Add($"문자 입력 간격은 0~{MaxIntervalMilliseconds:N0}ms 범위여야 합니다.");
                }
                break;

            case MacroStepType.KeyPress:
                ValidateKeyExpression(step.Text, errors);
                break;

            case MacroStepType.KeyHold:
                ValidateKeyExpression(step.Text, errors);
                ValidateDuration(step.Value, "키 누르기 시간", errors);
                break;

            case MacroStepType.Delay:
                ValidateDuration(step.Value, "대기 시간", errors);
                break;

            default:
                errors.Add("지원하지 않는 동작 형식입니다.");
                break;
        }

        return errors;
    }

    public static void EnsureValid(MacroStep step)
    {
        var errors = Validate(step);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }
    }

    private static void ValidateKeyExpression(string expression, ICollection<string> errors)
    {
        try
        {
            _ = KeyExpressionParser.Parse(expression);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }
    }

    private static void ValidatePosition(MacroStep step, ICollection<string> errors)
    {
        if (!step.HasPosition)
        {
            return;
        }

        if (step.X is < -100_000 or > 100_000 || step.Y is < -100_000 or > 100_000)
        {
            errors.Add("마우스 좌표가 허용 범위를 벗어났습니다.");
        }
    }

    private static void ValidateRepeat(MacroStep step, ICollection<string> errors)
    {
        if (step.IntervalMilliseconds < 10 || step.IntervalMilliseconds > MaxIntervalMilliseconds)
        {
            errors.Add($"반복 간격은 10~{MaxIntervalMilliseconds:N0}ms 범위여야 합니다.");
        }

        if (step.DurationMilliseconds > 0)
        {
            ValidateDuration(step.DurationMilliseconds, "연속 실행 시간", errors);
            return;
        }

        if (step.RepeatCount < 1 || step.RepeatCount > MaxRepeatCount)
        {
            errors.Add($"반복 횟수는 1~{MaxRepeatCount:N0}회 범위여야 합니다.");
        }
    }

    private static void ValidateDuration(int milliseconds, string label, ICollection<string> errors)
    {
        if (milliseconds < 1_000 || milliseconds > MaxDurationMilliseconds)
        {
            errors.Add($"{label}은 1~86,400초 범위여야 합니다.");
        }
    }
}
