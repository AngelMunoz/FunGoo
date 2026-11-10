# Changelog

## [Unreleased]

## [0.7.0] - 2026-11-09

### Changed

- **Core:** **Breaking:** The Goo dependency is now 0.29.1 (was 0.28.7). Goo 0.29 removes `Goo.Tokens`, `Cell.MountSeeded`, and `Goo.Command` with `Button.Command` and `KeyBinding.Command`; apps that use those APIs must move to direct token values, `Cell.Mount`, and `OnClick` or `Action`. `Window.Post` now returns `bool`. Apps gain callback ownership fixes, so an input callback rebuilds the cell that made the content, and headless render support. The generated FunGoo setter, children, and interop surface is unchanged, so no FunGoo call site must change.
- **Widgets:** The Goo.Widgets dependency is now 0.21.0 (was 0.19.8). Widgets gain theme depth shadows, a `TextField` handle for focus control, ComboBox popup padding, and crisp WindowChrome caption icons. The generated FunGoo.Widgets binding surface is unchanged, so no FunGoo.Widgets call site must change.
- **Samples:** RaznorGoo moves to Goo 0.29.1.

## [0.6.0] - 2026-11-07

### Added

- **Widgets:** New optional setter `handleColor` for the SplitPane divider color. Call it with a color to paint the divider, or with no argument to keep the theme border color.

### Changed

- **Core:** The Goo dependency is now 0.28.7 (was 0.28.3). Apps gain Goo's rendering and input fixes: focus rings stay inside rounded clipping parents, scrolling moves whole physical pixels without relayout, deferred virtual rows realize before the next render, element handles report clamped scroll offsets, VSync works on software Vulkan devices, and initial windows stay inside the display's usable area.
- **Widgets:** The Goo.Widgets dependency is now 0.19.8 (was 0.19.3). Widgets gain inset row focus rings, content-sized Checkbox and RadioButton focus rings, a SplitPane divider drawn as a hover-highlighting theme-width line (`HandleSize` now sizes the pointer target), Banner dismiss buttons that stay inside their bounds, and tighter Markdown line spacing with content-sized list markers.
- **Samples:** RaznorGoo moves to Goo 0.28.7.

## [0.5.0] - 2026-11-06

### Changed

- **Core:** **Breaking:** The Goo dependency is now 0.28.3 (was 0.7.9). Goo merged its former optional packages into the main assembly and discontinued the Goo.Svg package: SVG loading, spell check, rich text, gestures, native dialogs and menus, printing, and notifications now come from the Goo package alone. Generated setters cover the merged types in their new sub-namespaces, and `FunGoo.Style.percent` keeps its signature while calling Goo's named `Length.Percent` factory.
- **Widgets:** **Breaking:** The Goo.Widgets dependency is now 0.19.3 (was 0.2.9). Generated optional setters follow the Goo.Widgets API migration: size fields such as `MinHeight` and `Width` take nullable `Length` values, numeric style fields are nullable tokens, and selection callbacks follow the new `OnChange`/`OnCommit` naming (MediaTransport uses `OnPositionChange`/`OnPositionCommit` and `OnVolumeChange`/`OnVolumeCommit`).
- **Widgets:** **Breaking:** The `FunGoo.Virtual` module moved from the FunGoo package to FunGoo.Widgets. Goo removed its package-level virtual collection function in 0.7.22; the `create`, `ofSeq`, and `ofIndexed` helpers keep their signatures and now build on Goo.Widgets' `VirtualItems`.
- **Samples:** RaznorGoo moves to Goo 0.28.3 and drops the discontinued Goo.Svg package reference. Playlist rows, file-picker rows, and the empty state pass nullable lengths for their minimum heights, and the player bar uses the renamed MediaTransport callbacks.

## [0.4.0] - 2026-10-15

### Added

- **Core:** New `FunGoo.Style` module with `percent`, a percentage length helper for the Goo `Percent` function, and `topEdge`, a top-only border-width edge value. Both cover the Goo APIs removed in 0.6.6 (`Length.Percent`, per-side border-width properties).
- **Samples:** RaznorGoo gains a file-system service behind an `IFileSystem` entry in its environment record: list a folder's sub-folders and extension-filtered files in one call, search the entries of one folder by name, case-insensitive, list the ready drives of the system, and ask for the parent folder of a path. Extension filters accept `*.mp3`, `.mp3`, or `mp3` forms, and unreadable folders return empty results instead of errors.
- **Samples:** The RaznorGoo file picker shows the drive list above every drive root, so folders on other drives are now reachable. The picker and the playlist loader go through the file-system service instead of direct `System.IO` calls.

### Changed

- **Core:** **Breaking:** The Goo dependency is now 0.7.9 (was 0.5.3). The generated surface follows the new Goo API: `Padding`, `Margin`, `BorderWidth`, and `BorderColor` setters take grouped `Edges` values, and the interop module gains `Edges` and `Edges<Color>` conversion helpers plus a `tryCreate` wrapper for the new static `NativeFileDrag.TryCreate`.
- **Widgets:** **Breaking:** The Goo.Widgets dependency is now 0.2.9 (was 0.1.1). Generated optional setters cover the new widget fields, including Calendar, DatePicker, ComboBox, Tooltip, and Markdown-era widgets; button text is now authored through `Content` instead of `Label`.
- **Samples:** RaznorGoo moves to Goo and Goo.Svg 0.7.9: percentage sizes go through `FunGoo.Style.percent`, the player bar's top border goes through `topEdge`, menu and picker buttons author `Content`, and the playlist and file-picker scrollbars now own authored track and thumb parts, which Goo requires to paint a scrollbar.
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
