using NalApps.Macro.Models;

namespace NalApps.Macro.Core;

public sealed class MacroExecutor
{
    private readonly IMacroInputDriver _input;
    private readonly IMacroDelay _delay;

    public MacroExecutor(IMacroInputDriver input, IMacroDelay delay)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
    }

    public async Task ExecuteStepAsync(MacroStep step, CancellationToken cancellationToken)
    {
        MacroStepValidator.EnsureValid(step);
        cancellationToken.ThrowIfCancellationRequested();

        switch (step.Type)
        {
            case MacroStepType.MouseMove:
                MoveMouse(step);
                break;

            case MacroStepType.LeftClick:
                await ExecuteMouseClickAsync(step, MouseButtonKind.Left, false, cancellationToken);
                break;

            case MacroStepType.RightClick:
                await ExecuteMouseClickAsync(step, MouseButtonKind.Right, false, cancellationToken);
                break;

            case MacroStepType.DoubleClick:
                await ExecuteMouseClickAsync(step, MouseButtonKind.Left, true, cancellationToken);
                break;

            case MacroStepType.MouseWheel:
                await ExecuteMouseWheelAsync(step, cancellationToken);
                break;

            case MacroStepType.TextInput:
                await ExecuteTextAsync(step, cancellationToken);
                break;

            case MacroStepType.KeyPress:
                await ExecuteKeyAsync(step.Text, 0, cancellationToken);
                break;

            case MacroStepType.KeyHold:
                await ExecuteKeyAsync(step.Text, step.Value, cancellationToken);
                break;

            case MacroStepType.Delay:
                await _delay.DelayAsync(step.Value, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"지원하지 않는 동작입니다: {step.Type}");
        }
    }

    public void ReleaseSafetyState()
    {
        _input.ReleaseSafetyState();
    }

    private void MoveMouse(MacroStep step)
    {
        if (!step.HasPosition)
        {
            throw new InvalidOperationException("마우스 위치가 지정되지 않았습니다.");
        }

        if (!_input.MoveMouse(step.X, step.Y))
        {
            throw new InvalidOperationException($"마우스를 X {step.X}, Y {step.Y} 위치로 이동하지 못했습니다.");
        }
    }

    private async Task ExecuteMouseClickAsync(MacroStep step, MouseButtonKind button, bool doubleClick, CancellationToken cancellationToken)
    {
        if (step.HasPosition && !_input.MoveMouse(step.X, step.Y))
        {
            throw new InvalidOperationException($"마우스를 X {step.X}, Y {step.Y} 위치로 이동하지 못했습니다.");
        }

        if (step.DurationMilliseconds > 0)
        {
            var elapsed = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ClickOnceAsync(button, doubleClick, cancellationToken);

                if (elapsed + step.IntervalMilliseconds >= step.DurationMilliseconds)
                {
                    break;
                }

                await _delay.DelayAsync(step.IntervalMilliseconds, cancellationToken);
                elapsed += step.IntervalMilliseconds;
            }

            return;
        }

        for (var index = 0; index < step.RepeatCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ClickOnceAsync(button, doubleClick, cancellationToken);
            if (index + 1 < step.RepeatCount)
            {
                await _delay.DelayAsync(step.IntervalMilliseconds, cancellationToken);
            }
        }
    }

    private async Task ClickOnceAsync(MouseButtonKind button, bool doubleClick, CancellationToken cancellationToken)
    {
        ClickButton(button);
        if (!doubleClick)
        {
            return;
        }

        await _delay.DelayAsync(80, cancellationToken);
        ClickButton(button);
    }

    private void ClickButton(MouseButtonKind button)
    {
        _input.MouseButtonDown(button);
        try
        {
            _input.MouseButtonUp(button);
        }
        catch
        {
            _input.ReleaseSafetyState();
            throw;
        }
    }

    private async Task ExecuteMouseWheelAsync(MacroStep step, CancellationToken cancellationToken)
    {
        if (step.HasPosition && !_input.MoveMouse(step.X, step.Y))
        {
            throw new InvalidOperationException($"마우스를 X {step.X}, Y {step.Y} 위치로 이동하지 못했습니다.");
        }

        for (var index = 0; index < step.RepeatCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _input.MouseWheel(step.Value);
            if (index + 1 < step.RepeatCount)
            {
                await _delay.DelayAsync(step.IntervalMilliseconds, cancellationToken);
            }
        }
    }

    private async Task ExecuteTextAsync(MacroStep step, CancellationToken cancellationToken)
    {
        for (var index = 0; index < step.Text.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var character = step.Text[index];

            if (character == '\r')
            {
                if (index + 1 < step.Text.Length && step.Text[index + 1] == '\n')
                {
                    index++;
                }

                PressSingleKey(0x0D);
            }
            else if (character == '\n')
            {
                PressSingleKey(0x0D);
            }
            else if (character == '\t')
            {
                PressSingleKey(0x09);
            }
            else
            {
                _input.SendUnicodeCharacter(character);
            }

            if (step.IntervalMilliseconds > 0 && index + 1 < step.Text.Length)
            {
                await _delay.DelayAsync(step.IntervalMilliseconds, cancellationToken);
            }
        }
    }

    private async Task ExecuteKeyAsync(string expression, int holdMilliseconds, CancellationToken cancellationToken)
    {
        var keys = KeyExpressionParser.Parse(expression);
        var pressed = new List<ushort>(keys.Count);

        try
        {
            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _input.KeyDown(key);
                pressed.Add(key);
            }

            if (holdMilliseconds > 0)
            {
                await _delay.DelayAsync(holdMilliseconds, cancellationToken);
            }
        }
        finally
        {
            for (var index = pressed.Count - 1; index >= 0; index--)
            {
                _input.KeyUp(pressed[index]);
            }
        }
    }

    private void PressSingleKey(ushort virtualKey)
    {
        _input.KeyDown(virtualKey);
        _input.KeyUp(virtualKey);
    }
}
