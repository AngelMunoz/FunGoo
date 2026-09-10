# Changelog

## [Unreleased]

## [0.2.0] - 2026-09-09

### Changed

- **Core:** The Goo dependency is now 0.5.3. The RaznorGoo sample moves to Goo and Goo.Svg 0.5.3.

## [0.1.0] - 2026-09-08

### Added

- **Core:** Children helpers for `Container` and `Button`: append children in one call from a sequence or individual elements.
- **Core:** Fluent setter extensions for `Window`, `TextEditorController`, `ShaderEffect`, and `TextCommandEvent`: chain configuration calls and keep the same instance back.
- **Core:** `voption` wrappers for the `ElementHandle` text queries, and inline conversion helpers from `float` and `int` to `Length` and from CSS color strings to `Color`.
- **Core:** `Virtual` bindings for Goo's virtualized lists: build from an `IReadOnlyList`, a sequence, or a count and indexer without touching G#'s compiler-generated entry points.
- **Samples:** RaznorGoo, a media player built on Goo: adaptive playlist and playback state, LibVLCSharp audio behind an environment record, runtime SVG icons, and a custom file and folder picker.
