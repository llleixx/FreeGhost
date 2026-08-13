# Changelog

All notable changes to FreeGhost are documented here.

## Unreleased

## 1.0.0 - 2026-08-13

- Updated the PEAK compatibility baseline to 2.0.a (Steam build 24676019).
- Adapted vanilla-client Ghost position encoding to PEAK 2.0's fixed world-up offset.
- Kept position encoding aligned with the Ghost's actual target during spectate transitions.
- Added regression coverage for the PEAK 2.0 Ghost formula and close-position encoding.
- Added first-person free ghost movement using PEAK's rebound movement, jump, crouch, sprint, and look inputs.
- Added a ModConfig-compatible key binding for toggling between free movement and PEAK's vanilla spectate camera, plus configurable movement speeds.
- Added a configurable free-flight radius centered on each free-mode entry position, defaulting to 1 km.
- Added vanilla-client ghost position synchronization through existing `lookValues` and `spectateZoom` fields.
- Added analytic position encoding, nearest half-float quantization, finite-value validation, and safe fallback behavior.
- Bundled runtime and coordinate code into a single `FreeGhost.dll` for installation.
- Added automatic cleanup on revive, scene teardown, and feature disable.
