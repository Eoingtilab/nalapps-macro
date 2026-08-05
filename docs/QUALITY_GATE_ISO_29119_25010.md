# NalaApps Macro Quality Gate

Status: execution pending

This change exists to trigger and document the full Windows quality gate for the current main branch.

## Standards alignment

- ISO/IEC/IEEE 29119: requirements-based, risk-based, functional, regression, boundary, negative, integration, UI lifecycle, packaging and traceability testing.
- ISO/IEC 25010: functional suitability, reliability, usability, performance efficiency, compatibility, security, maintainability and portability.

## Mandatory automated coverage

- Keyboard: A-Z, 0-9, F1-F24, aliases, shortcuts, invalid combinations, press and hold, cancellation and release.
- Mouse: move, left click, right click, double click, repeat count, duration mode, wheel count, wheel duration, cancellation and pressed-state cleanup.
- Timing: delay boundaries, intervals and duration semantics.
- Text: Unicode, Enter, Tab and per-character interval.
- Persistence: current schema round-trip and legacy compatibility.
- UI: all primary actions, visible run controls, no hidden-run path, dialog apply lifecycle and colored-button contrast.
- Build and distribution: warnings as errors, Windows Release build, self-contained single-file publish, ZIP integrity and SHA-256 generation.

## Release rule

Do not approve or release unless all Windows quality-gate jobs pass with zero failed tests and the produced executable artifact exists.
