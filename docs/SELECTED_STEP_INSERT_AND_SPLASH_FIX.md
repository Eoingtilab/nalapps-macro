# Selected-step insertion and splash rendering fix

- Replaced the former `아래 삽입` toggle with a `선택 단계 아래 추가` checkbox.
- When checked, newly created keyboard/mouse/time/text/action steps are inserted immediately below the currently selected step.
- When unchecked, newly created steps continue to append at the bottom.
- Existing edit and inline-copy behavior are unchanged.
- Splash rendering now uses an opaque WPF surface and assigns the decoded splash image to both the image control and window background brush to avoid blank-white rendering on affected Windows/WPF configurations.
