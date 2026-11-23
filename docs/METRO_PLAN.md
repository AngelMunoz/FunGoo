# FunGoo.Metro — Durable Architecture and Delivery Plan

This document is the durable source of truth for `FunGoo.Metro`: a native Metro design system for [Goo](https://github.com/obselate/goo) and `FunGoo`, written in F#.

## Design Authority

- The `metro-design-system` skill (`.agents/skills/metro-design/SKILL.md`) is the design rule.
- [Metrino](https://github.com/AngelMunoz/metrino) (`E:\Metrino`, a TypeScript web library, version 0.6.0 on 2026-11-22) is a reference for implementation details only: token values, component structure, motion numbers.
- When Metrino and the Metro skill disagree, the Metro skill wins.

## Delivery Rules

- One small, verifiable PR per control family. No gigantic PR.
- Every PR passes the review checklists of `goo-fsharp` and `goo-widgets-fsharp`.
- No reflection in library code. Write new tests with the public Goo API only.
- Write F# functions directly. Add an `Action` or `Func` wrapper only when the compiler requires it.
- Attach a `goo render` image to each PR that adds or changes a control.

## Hard Facts (Verified Against Goo 0.31.0 and Goo.Widgets 0.22.2)

1. **Two-layer theming split:**
   - `Goo` core (written in the G# language) has no `Theme` type. It provides `Context<'T>` (window + subtree scoped reactive reads), `Style` (`BasedOn`, pseudo-states, typography, transitions), `Window.Preferences` / `Window.PreferencesChanged`, `FontSource`, and `Window.SetColors`.
   - `Goo.Widgets` (G#) implements `Theme` (`public data partial struct`), `Theme.Attach`, `Theme.SetMode`, and `ThemeScope` on top of `Context<Theme>`.
2. **Custom `Goo.Widgets.Theme` rules:**
   - Nullable radii (`ControlRadius`, `PanelRadius`, `RadiusSm`..`RadiusXl`) preserve explicit `Nullable 0.0` through `Theme.ResolveDefaults()`.
   - Shadow arrays (`PanelShadows`, `OverlayShadows`, `ControlShadows`, `FieldShadows`) preserve explicit empty arrays `[||]` through `ResolveDefaults()`.
   - Setting `Theme.Variant = Func<bool, Theme>(...)` enables `Theme.SetMode` and OS light/dark tracking. `HighContrast`, `TextScaleFactor`, and `ReducedMotion` apply automatically after `Variant`.
   - Custom `Variant` delegates bypass built-in OS accent override; `FunGoo.Metro` owns accent selection (21 Metrino palettes, custom color, or explicit OS accent sync).
   - `Theme.ResolveDefaults()` resets `BorderWidth = 0.0` to `1.0`. Use `BorderWidth = 2.0` on the Metro `Goo.Widgets.Theme` (matching Metro's 2 px control borders) and explicit `0.0` borders on borderless containers.
   - `HoverColor`, `PressedColor`, `ActiveColor`, and quiet semantic fills have no fallback in `ResolveDefaults()` and must be populated explicitly.
3. **Typography:**
   - Use system sans-serif fallback (`""` / platform sans-serif) for v1. Custom bundled font evaluation via `FontSource` is deferred.
   - `Goo.Widgets.Typography` lacks `Hero` (56 px) and `Title` (42 px) roles and uses one heading weight; `FunGoo.Metro` ships `MetroText` for the complete 10-role Metro type ramp with weights `300`, `400`, and `700`.
4. **Motion and 3D Perspective:**
   - `Style.TransitionEasing` supports only `Linear`, `EaseIn`, `EaseOut`, and `EaseInOut`.
   - `Goo.Simulation` is `public open class`. `FunGoo.Metro` subclasses `Simulation` for exact closed-form `CubicBezierSimulation` (`(0.1, 0.9, 0.2, 1)` and page glide `(0.25, 0.46, 0.45, 0.94)`) and `ExponentialEaseSimulation` (Continuum `150 ms`), bound via `Cell.AnimateValue` to paint slots (`TranslateAnim`, `OpacityAnim`, `ScaleAnim`, color anims) with zero rebuilds.
   - `PanelTransform` is strictly 2D (no `perspective`, `rotateX`, or `rotateY`).
   - True 3D perspective (Turnstile, FlipTile, Dialog Swivel) uses a Slang `ShaderEffect` prototype (`FunGoo.Metro/Shaders/metro_perspective.slang`), applied through the `Style.ShaderEffect` property. Compiling `.slang` to `.goo-effect` requires `slangc 2026.16` (`SLANG_SDK`) and `spirv-val` from Vulkan SDK `1.4.357.0` (`VULKAN_SDK`). Shader compilation is opt-in via `-p:CompileMetroShaders=true`; when the `.goo-effect` artifact is absent or `ReducedMotion` is active, `MetroPerspectiveEffect` falls back cleanly to 2D transforms.
5. **Headless testing:**
   - Goo's `EmbeddedWindowHost` runs full `Window` lifecycle, `Context` subscriptions, `ElementHandle` metrics, `LayoutAlgorithm`, `Portal`, and `PlatformInput` inside `xunit` tests without a GPU or display server.
6. **Style values are write-only:**
   - The `Style` properties on a `Blob` have setters and no public getters (`Color`, `FontSize`, `TranslateAnim`, and the other style fields). `Style.Entries` is internal. A test cannot read a declared color or font size back without reflection.
   - `Style.TryGetTextStyle` is the one public read. It returns the explicit inline `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `Color`, and `TextDecoration` of a `Text` or `Button`.
   - Verify visuals with `goo render` (and `goo diff` for pixel comparisons). In tests, assert public structure and behavior only: `Children`, `Key`, `Accessibility`, callbacks, `KeyBindings`, and `Anim` members.
7. **A typed cell builds only through the reconciler:**
   - `Cell<TInput>.Build(input)` and `InputChanged` are protected members of Goo. The F# compiler cannot call them from outside the cell (`FS0501`, `FS0509` on 2026-11-22). No public API returns the tree of a mounted child cell.
   - To assert the tree of a stateful control, give the control a public view builder (a function that returns the `Blob` tree for an explicit state) and let the cell delegate to it.
8. **Window timers need an open window:**
   - `Cell.Every` and `Cell.After` throw `InvalidOperationException` ("Window timers require an open window") when the host window is not open.
   - A `goo render` frame mounts cells without opening the window. Test `Window.IsOpen` before a cell starts a timer.
9. **Display-free images:**
   - `goo render <project> --out <png>` builds an app and writes one PNG frame with no display. Options: `--size WxH` (logical size), `--scale N` (pixels per logical unit), `--theme light|dark` (sets `Window.Preferences.Theme`), `--scene NAME`.
   - Scenes come from `Window.Scenes` (`Dictionary<string, Func<Cell>>`). Each factory returns a fresh, unattached cell. `goo render <project> --list` prints the scene names.
   - `goo render` waits for GPU image, path, and text uploads. It does not wait for app services and does not advance app animations.
10. **Direction and scroll API:**
    - Logical style properties exist: `Start`, `End`, `Inset`, `PaddingStart`, `PaddingEnd`, `MarginStart`, `MarginEnd`, `BorderStartWidth`, `BorderEndWidth`, `BorderStartColor`, `BorderEndColor`, and `TextAlign.Start` / `End`. `Direction` is `Auto`, `LeftToRight`, or `RightToLeft`.
    - There is no scroll-snap style property. Use `ElementHandle`: `ScrollTo`, `JumpTo`, `ScrollToItem(key, alignment, settle)`, `ScrollOffsetOf`, `IsScrollSettling`, `ScrollOffset`, `ScrollRange`, and the `MetricsChanged` event.
11. **Overlays and gestures:**
    - Style properties for overlays: `Position` (`Relative`, `Absolute`, `Static`), `Inset`, `Start`, `End`, `Top`, `Bottom`, and `ZIndex`.
    - `Blob` exposes `OnPointerEnter`, `OnPointerLeave`, `OnPointerDrag`, `PointerDragThreshold`, `PointerDragAxis`, `OnWheel`, `OnFocus`, `OnBlur`, and `Gestures`.
    - `Cell.Animate` has an overload that takes an `onChange` callback, for values that are not `Anim` style properties.
12. **A headless test cannot advance an animation:**
    - `EmbeddedWindowHost.RenderFrame(dt)` calls `Window.PumpEmbedded`, which sets `frameAllowed` to `false` when the window has no presentation target (`windowTarget` is `nil`). A test host with no Vulkan surface has no target, so animations keep `Running = true` and never change `Value`.
    - `Window.Pump(dt)` is public, but it uses the same gate for an embedded host. The Goo own motion tests use internal fixtures (`MotionPump`).
    - In a GPU-free test, assert the start of a motion (`Running`, `Target`, the tree) and timer-driven ends. Do not assert animation progress. Window timers (`Cell.After`, `Cell.Every`) do fire on `RenderFrame`, and they follow the wall clock, so a test waits in real time.
    - Verify motion values with a rendered frame (`goo render` does not advance app animations, so it shows the rest state).

## Control Catalog Gap (2026-11-22)

Metrino registers 74 controls. `FunGoo.Metro` ships 17 public control and layout types:
`MetroText`, `MetroButton`, `MetroTextBox`, `MetroToggleSwitch`, `MetroCheckBox`, `MetroRadioButton`, `MetroSlider`, `MetroProgressBar`, `MetroProgressRing`, `MetroPivotCell`, `MetroHubCell`, `TileGridLayout`, `MetroIconicTile`, `MetroLiveTileCell`, `MetroAppBar`, `MetroSettingsFlyout`, and `MetroContentDialog`.

| Family | Metrino controls | In `FunGoo.Metro` | Missing |
| --- | --- | --- | --- | --- |
| Buttons | Button, DropdownButton, HyperlinkButton, RepeatButton | Button, Hyperlink (variant) | DropdownButton, RepeatButton |
| Date and time | Calendar, CalendarDatePicker, DatePicker, DatePickerRoller, TimePicker, TimePickerRoller | none | all 6 |
| Dialogs | ContentDialog, Flyout, MessageDialog, SettingsFlyout, TeachingTip | ContentDialog, SettingsFlyout | Flyout, MessageDialog, TeachingTip |
| Inputs | AutoSuggestBox, CheckBox, ComboBox, NumberBox, PasswordBox, RadioButton, Rating, RichEditBox, Slider, TextBox, ToggleSwitch | CheckBox, RadioButton, Slider, TextBox (with password mode), ToggleSwitch | AutoSuggestBox, ComboBox, NumberBox, Rating, RichEditBox |
| Layout | Canvas, Grid, ScrollViewer, StackPanel, TileGrid, VariableSizedWrapGrid, Viewbox, WrapPanel | TileGridLayout | Canvas, Grid, ScrollViewer, StackPanel, VariableSizedWrapGrid, Viewbox, WrapPanel |
| Navigation | AppBar, AppBarButton, AppBarSeparator, AppBarToggleButton, Hub, HubSection, Panorama, PanoramaItem, Pivot, PivotItem, SplitView, TabView, TabViewItem | AppBar (commands only), Hub (with sections), Pivot | AppBarButton, AppBarSeparator, AppBarToggleButton, Panorama, SplitView, TabView |
| Primitives | Border, ContextMenu, Expander, Icon, Image, InfoBar, MediaElement, MenuFlyout, PersonPicture, RichTextBlock, TextBlock, Toast, Tooltip | TextBlock (as `MetroText`), Icon (as `MaterialIcons` in tiles) | Border, ContextMenu, Expander, Image, InfoBar, MediaElement, MenuFlyout, PersonPicture, RichTextBlock, Toast, Tooltip |
| Selection | FlipView, GridView, ListBox, ListPicker, ListView, LongListSelector, SemanticZoom, TreeView | none | all 8 |
| Tiles | CycleTile, FlipTile, IconicTile, LiveTile | IconicTile, LiveTile | CycleTile, FlipTile |

## Stackable PR Roadmap (GitHub Stack `#5`)

Each PR stacks on the previous branch via `gh stack`, builds cleanly, formats with `dotnet fantomas .`, and passes all headless tests.

| Stack # | Branch | Pull Request | Scope | Status |
| --- | --- | --- | --- | --- |
| PR 1 | `metro/01-foundation` | #1 | Durable plan, `FunGoo.Metro` + `FunGoo.Metro.Tests` projects, 21 Metrino accents, dark/light tokens, typography/spacing/motion constants, dual-context `MetroTheme` + `Goo.Widgets.Theme` bridge, headless tests | Complete |
| PR 2 | `metro/02-motion-and-shader` | #2 | Native `Simulation` curves (`CubicBezierSimulation`, `ExponentialEaseSimulation`), stagger delays, Continuum flight helper, 2D turnstile/swivel fallback, Slang `metro_perspective.slang` prototype + `MetroPerspectiveEffect` runtime loader, headless tests | Complete |
| PR 3 | `metro/03-typography-and-controls` | #3 | `MetroText` (10-role ramp), `MetroButton`, `MetroTextBox`, `MetroPasswordBox`, `MetroToggleSwitch` (sharp rectangular), `MetroCheckBox`, `MetroRadioButton`, `MetroSlider` (square thumb), `MetroProgressBar` (determinate + 5-dot), `MetroProgressRing` (5-dot orbit), headless + allocation tests | Complete |
| PR 4 | `metro/04-navigation-layout-gallery` | #4 | `MetroPivot` (peeking headers, slide, keyboard circular loop, swipe), `MetroHub` / `MetroPanorama` (peeking sections + scroll parallax), `IconicTile` / `LiveTile` + `TileGridLayout`, `MetroAppBar`, `MetroSettingsFlyout`, `MetroContentDialog`, `samples/MetroGallery` (`--check` + interactive UI), `CHANGELOG.md` | Complete |
| PR 5 | `metro/05-render-scenes` | #6 | Live tile starts its cycle timer only on an open window, so `goo render` works. Gallery registers one `Window.Scenes` scene per tab and follows `--theme`. Plan: design authority, delivery rules, new hard facts, catalog gap, planned PR set, known issues | Complete |
| PR 6 | `metro/06-flip-view` | | `MetroFlipView`: one item at a time, edge previous and next buttons, indicator dots, swipe, `Left`/`Right`/`PageUp`/`PageDown`/`Home`/`End` keys, slide transition, public view builder (`MetroFlipView.buildView`, `View`, `State`, `Handle`), gallery tab and scene, reflection-free tests, shared `TestSupport` helpers | Complete |
| PR 7 | `metro/07-public-view-builders` | | Public view builders for the existing stateful controls. Tests and the gallery check use the public API only. No reflection | Planned |
| PR 8 | `metro/08-list-family` | | `MetroListView`, `MetroListBox`, `MetroGridView` on `Goo.Widgets.ListView`: sharp rows, selection modes | Planned |
| PR 9 | `metro/09-long-list-selector` | | Grouped list with group headers and a jump list (`ElementHandle.ScrollToItem`) | Planned |
| PR 10 | `metro/10-semantic-zoom` | | Zoomed-in and zoomed-out views with group synchronization | Planned |
| PR 11 | `metro/11-looping-selector` | | Shared roller column (`MetroLoopingSelector`) | Planned |
| PR 12 | `metro/12-date-time-pickers` | | `MetroDatePicker` and `MetroTimePicker` on the roller; dropdown variants | Planned |
| PR 13 | `metro/13-calendar` | | `MetroCalendar`, `MetroCalendarDatePicker` | Planned |
| PR 14 | `metro/14-combo-suggest` | | `MetroComboBox`, `MetroAutoSuggestBox` | Planned |
| PR 15 | `metro/15-number-rating` | | `MetroNumberBox`, `MetroRating` | Planned |
| PR 16 | `metro/16-dialogs` | | `MetroFlyout`, `MetroMessageDialog` | Planned |
| PR 17 | `metro/17-menus-tooltips` | | `MetroContextMenu`, `MetroTooltip` | Planned |
| PR 18 | `metro/18-notifications` | | `MetroToast`, `MetroInfoBar`, `MetroTeachingTip` | Planned |
| PR 19 | `metro/19-split-tab` | | `MetroSplitView`, `MetroTabView` | Planned |
| PR 20 | `metro/20-app-bar-parts` | | `MetroAppBarButton`, `MetroAppBarToggleButton`, `MetroAppBarSeparator`, `MetroPanorama` | Planned |
| PR 21 | `metro/21-primitives` | | `MetroExpander`, `MetroBorder`, `MetroImage`, `MetroPersonPicture` | Planned |
| PR 22 | `metro/22-layout` | | `MetroScrollViewer`, `MetroViewbox`, `MetroWrapPanel`, `MetroVariableSizedWrapGrid` | Planned |
| PR 23 | `metro/23-tiles-buttons` | | `MetroFlipTile`, `MetroCycleTile`, `MetroDropdownButton`, `MetroRepeatButton` | Planned |
| PR 24 | `metro/24-rtl` | | RTL logical property verification across navigation and layout controls | Planned |

Rules for the planned rows: each row is one PR, one theme, one new test file, one gallery scene. Verify the base `Goo.Widgets` API in the repository before the PR starts. Split a row into two PRs when it grows past the size of PR 3.

Metrino controls with no planned port: `RichEditBox` (no rich editing base in Goo.Widgets 0.22.2), `MediaElement` (no media base), and `Canvas`, `StackPanel`, `Grid` (Goo `Container`, `Row`, `Column`, and `Goo.Widgets.Grid` already supply these layouts under the Metro theme). Confirm each decision in its own row before work starts.

## Known Issues (Work in Progress)

1. **Reflection in tests and in the gallery check.** `ThemeTests`, `MotionAndShaderTests`, `ControlsTests`, `NavigationAndOverlaysTests`, and `runChecks` in `samples/MetroGallery/Program.fs` read Goo internals by reflection: `Style.Entries`, `Window.node`, `Cell.directChild`, `UpdateTree`, and retained node fields. This is brittle. PR 7 replaces it through public view builders. New tests use the public API only.
2. **Tile text color.** Tile text is black on the Blue accent, because `MetroAccent.fromColor` selects the contrast-safe foreground. Metrino paints white text on its iconic, live, and cycle tiles. Decide with the Metro skill and apply to all tiles.
3. **Toggle thumb color.** The `controls` scene frame shows a dark thumb on the accent track in light mode. Review against the Metro skill.

### Resuming on Another Machine

1. Check out the stack top:
   ```sh
   gh stack checkout 5
   # or: git fetch origin && git checkout metro/05-render-scenes
   ```
2. Build and run all `37` headless tests plus the gallery smoke check:
   ```sh
   dotnet build FunGoo.slnx
   dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj
   dotnet run --project samples/MetroGallery/MetroGallery.fsproj -- --check
   ```
3. Formatting on a Windows checkout. With `core.autocrlf=true`, the working tree holds CRLF and `.editorconfig` requires LF. On a fresh checkout, `dotnet fantomas --check .` then reports every file, with line endings only as the difference. Run `dotnet fantomas .` once, then confirm with `git diff --ignore-cr-at-eol --stat` that only the files of the PR changed. Restore the other files with `git checkout -- <file>`.
3. Render gallery frames for review and for PR images (`goo` is the Goo DevTools CLI):
   ```sh
   goo render samples/MetroGallery/MetroGallery.fsproj --list
   goo render samples/MetroGallery/MetroGallery.fsproj --scene controls --theme light --size 1200x800 --out controls-light.png
   ```
4. Optional shader compilation. The machine used on 2026-11-22 has no `SLANG_SDK` and no `VULKAN_SDK`, so it cannot compile the perspective shader:
   - Requires Slang `2026.16` (`SLANG_SDK`) and Vulkan SDK `1.4.357.0` (`VULKAN_SDK`) — see `Goo/docs/platform-preflight.md`.
   - Build and test with:
     ```sh
     dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj -p:CompileMetroShaders=true
     ```
   - To ship the precompiled binary to machines without the shader SDKs, copy the generated `.goo-effect` artifact from `FunGoo.Metro/obj/` to `FunGoo.Metro/Shaders/metro_perspective.goo-effect`.

## Future Follow-Ups (After the Planned PR Set)

- Decide whether to check in precompiled `FunGoo.Metro/Shaders/metro_perspective.goo-effect` and wire live 3D perspective transitions into `MetroPivot`, `MetroLiveTile` (FlipTile), and `MetroContentDialog` (Swivel). This needs a machine with the Slang and Vulkan SDKs.
- Evaluate bundled humanist light-weight sans-serif font (for example, Selawik) via `FontSource`.

## Verification Gates (Required Per PR)

1. `dotnet fantomas --check .` passes (run `dotnet fantomas .` before committing).
2. Every `.fs` file in `FunGoo.Metro/` has a paired `.fsi` file with XML doc comments in `.fsi` only.
3. `dotnet build FunGoo.slnx` succeeds with 0 errors in Debug and in Release.
4. `dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj` passes all tests in Debug and in Release.
5. `dotnet run --project samples/MetroGallery/MetroGallery.fsproj -- --check` passes.
6. For a control PR: `goo render` writes a frame of the new showcase scene, and the PR attaches the image.
7. Allocation checks verify zero unexpected per-build churn in Release builds.
8. Code review gate passes `AGENTS.md`, `goo-fsharp`, `goo-widgets-fsharp`, and `metro-design-system` checklists. No reflection in library code. New tests use the public API only.
