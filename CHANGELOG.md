# Changelog

## [Unreleased]

### Added

- **Samples:** RaznorGoo gains a file-system service behind an `IFileSystem` entry in its environment record: list a folder's sub-folders and extension-filtered files in one call, search the entries of one folder by name, case-insensitive, list the ready drives of the system, and ask for the parent folder of a path. Extension filters accept `*.mp3`, `.mp3`, or `mp3` forms, and unreadable folders return empty results instead of errors.
- **Samples:** The RaznorGoo file picker shows the drive list above every drive root, so folders on other drives are now reachable. The picker and the playlist loader go through the file-system service instead of direct `System.IO` calls.

### Changed

- **Samples:** The RaznorGoo player bar is now a mounted `MediaTransport` cell from `Goo.Widgets`: position, seek, volume, and play state render from immutable input snapshots, seek and volume work against the playback service, and the hand-rolled media menu and progress widgets are gone. Playback state reports seconds, and the now-playing title derives adaptively from the selection.
- **Samples:** RaznorGoo renders the playlist and the player bar as mounted cells that rebuild only on the events that touch them, the playback events are subscribed after the app is wired instead of during service construction, and playback ticks no longer re-render the whole window. The app file is split into a bottom-bar widget, a layout view, and a slim composition root.
- **Samples:** RaznorGoo builds its menu buttons, transport buttons, playlist rows, progress bar, and file picker rows and buttons from `Goo.Widgets` through `FunGoo.Widgets`, gaining hover, focus, and accessibility semantics. Icons come from the Material Symbols set, with one hand-parsed SVG kept for the missing "repeat off" glyph.

## [0.3.0] - 2026-09-09

### Added

- **Widgets:** New `FunGoo.Widgets` package for the `Goo.Widgets` library, pinned to Goo.Widgets 0.1.1. Widgets compose through F# constructor property assignment, and generated optional setters cover nullable widget properties: call with a value to set it, or with no argument to reset it to the widget default.

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
