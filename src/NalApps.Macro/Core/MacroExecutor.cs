using System.Diagnostics;
using NalApps.Macro.Models;

namespace NalApps.Macro.Core;

public sealed class MacroExecutor
{
	private const int TargetActivationDelayMilliseconds = 120;
	private const int KeyboardFocusDelayMilliseconds = 80;
	private const int MousePressMilliseconds = 25;
	private const int MinimumWheelIntervalMilliseconds = 140;
	private const int CountdownTickMilliseconds = 1_000;

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
				await PrepareKeyboardTargetAsync(cancellationToken);
				await ExecuteTextAsync(step, cancellationToken);
				break;
			case MacroStepType.KeyPress:
				await PrepareKeyboardTargetAsync(cancellationToken);
				await ExecuteKeyAsync(step.Text, 0, cancellationToken);
				break;
			case MacroStepType.KeyHold:
				await PrepareKeyboardTargetAsync(cancellationToken);
				await ExecuteKeyAsync(step.Text, step.Value, cancellationToken);
				break;
			case MacroStepType.Delay:
				await ExecuteDelayWithCountdownAsync(step, cancellationToken);
				break;
			default:
				throw new InvalidOperationException($"지원하지 않는 동작입니다: {step.Type}");
		}
	}

	public void ReleaseSafetyState() => _input.ReleaseSafetyState();

	private async Task ExecuteDelayWithCountdownAsync(MacroStep step, CancellationToken cancellationToken)
	{
		using var countdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		var stopwatch = Stopwatch.StartNew();
		step.RuntimeStatus = $"{Math.Ceiling(step.Value / 1000d):0}초 남음";
		var countdownTask = UpdateDelayCountdownAsync(step, stopwatch, countdownCts.Token);

		try
		{
			await _delay.DelayAsync(step.Value, cancellationToken);
		}
		finally
		{
			countdownCts.Cancel();
			try
			{
				await countdownTask;
			}
			catch (OperationCanceledException)
			{
			}
			step.RuntimeStatus = string.Empty;
		}
	}

	private static async Task UpdateDelayCountdownAsync(MacroStep step, Stopwatch stopwatch, CancellationToken cancellationToken)
	{
		while (true)
		{
			await Task.Delay(CountdownTickMilliseconds, cancellationToken);
			var remaining = Math.Max(0, step.Value - stopwatch.ElapsedMilliseconds);
			step.RuntimeStatus = remaining > 0
				? $"{Math.Ceiling(remaining / 1000d):0}초 남음"
				: "곧 완료";
		}
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

	private async Task<bool> PrepareKeyboardTargetAsync(CancellationToken cancellationToken)
	{
		var activated = _input.ActivateWindowUnderCursor();
		if (activated)
		{
			await _delay.DelayAsync(KeyboardFocusDelayMilliseconds, cancellationToken);
		}

		return activated;
	}

	private async Task<bool> PrepareMouseTargetAsync(MacroStep step, CancellationToken cancellationToken)
	{
		bool activated;
		if (step.HasPosition)
		{
			if (!_input.MoveMouse(step.X, step.Y))
			{
				throw new InvalidOperationException($"마우스를 X {step.X}, Y {step.Y} 위치로 이동하지 못했습니다.");
			}

			activated = _input.ActivateWindowAtPoint(step.X, step.Y);
		}
		else
		{
			activated = _input.ActivateWindowUnderCursor();
		}

		if (activated)
		{
			await _delay.DelayAsync(TargetActivationDelayMilliseconds, cancellationToken);
		}

		return activated;
	}

	private async Task ExecuteMouseClickAsync(MacroStep step, MouseButtonKind button, bool doubleClick, CancellationToken cancellationToken)
	{
		var physicalTiming = await PrepareMouseTargetAsync(step, cancellationToken);

		if (step.DurationMilliseconds > 0)
		{
			var elapsed = 0;
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await ClickOnceAsync(button, doubleClick, physicalTiming, cancellationToken);

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
			await ClickOnceAsync(button, doubleClick, physicalTiming, cancellationToken);
			if (index + 1 < step.RepeatCount)
			{
				await _delay.DelayAsync(step.IntervalMilliseconds, cancellationToken);
			}
		}
	}

	private async Task ClickOnceAsync(MouseButtonKind button, bool doubleClick, bool physicalTiming, CancellationToken cancellationToken)
	{
		await ClickButtonAsync(button, physicalTiming, cancellationToken);
		if (!doubleClick)
		{
			return;
		}

		await _delay.DelayAsync(80, cancellationToken);
		await ClickButtonAsync(button, physicalTiming, cancellationToken);
	}

	private async Task ClickButtonAsync(MouseButtonKind button, bool physicalTiming, CancellationToken cancellationToken)
	{
		_input.MouseButtonDown(button);
		try
		{
			if (physicalTiming)
			{
				await _delay.DelayAsync(MousePressMilliseconds, cancellationToken);
			}
		}
		finally
		{
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
	}

	private async Task ExecuteMouseWheelAsync(MacroStep step, CancellationToken cancellationToken)
	{
		await PrepareMouseTargetAsync(step, cancellationToken);

		var interval = Math.Max(step.IntervalMilliseconds, MinimumWheelIntervalMilliseconds);
		if (step.DurationMilliseconds > 0)
		{
			var elapsed = 0;
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				_input.MouseWheel(step.Value);

				if (elapsed + interval >= step.DurationMilliseconds)
				{
					break;
				}

				await _delay.DelayAsync(interval, cancellationToken);
				elapsed += interval;
			}

			return;
		}

		for (var index = 0; index < step.RepeatCount; index++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			_input.MouseWheel(step.Value);
			if (index + 1 < step.RepeatCount)
			{
				await _delay.DelayAsync(interval, cancellationToken);
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

			await _delay.DelayAsync(holdMilliseconds > 0 ? holdMilliseconds : 25, cancellationToken);
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
