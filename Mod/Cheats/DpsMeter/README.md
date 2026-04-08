# DPS Meter Module Notes

This folder contains the DPS meter feature entrypoint and DPS-specific runtime orchestration.

## Structure

- `DpsMeter.cs`
  - Public feature API used by lifecycle and hooks.
  - Owns session stats, panel rendering, and reset behavior.
  - Delegates online parsing, color calibration, and ownership classification to helpers.

- `OnlineDamageOwnershipTracker.cs`
  - Tracks local HP-drop timing and evaluates ownership inclusion logic.
  - Uses shared `OnlineDamageOwnershipFilter` for distance and mode classification.

- `../DpsMeterShared/OnlineDamageTextSampler.cs`
  - Shared online text parsing + dedupe pipeline.
  - Converts visible number text into numeric damage and removes duplicate samples.

- `../DpsMeterShared/OnlineCritColorCalibrator.cs`
  - Shared adaptive crit color learner + classifier.
  - Exposes top color stats and learned normal/crit colors.

- `../DpsMeterShared/OnlineDamageOwnershipFilter.cs`
  - Shared pure decision logic for online sample ownership modes:
    - `AllVisible`
    - `LikelyOutgoing`
    - `LikelyIncoming`

## Data Flow (Online Raw)

1. `DamageNumberDiagnostics` forwards text/color/world position into `DpsMeter`.
2. `OnlineDamageTextSampler` parses and dedupes samples.
3. `OnlineDamageOwnershipTracker` applies filter mode (distance + HP correlation).
4. Accepted samples update DPS stats.
5. `OnlineCritColorCalibrator` updates crit estimation and calibration stats.
6. `DpsMeter` renders panel output and emits periodic summaries.

## Maintenance Rules

- Keep `DpsMeter.cs` as orchestration + UI; avoid moving parser/calibration internals back in.
- Put reusable logic in `DpsMeterShared` when it does not require direct `DpsMeter` state.
- Keep `OnUpdate()` allocation-light (no LINQ/new collections in hot paths).
- Preserve existing logging keys and summary text unless intentionally changing telemetry consumers.
