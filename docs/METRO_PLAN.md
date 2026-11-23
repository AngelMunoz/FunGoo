# FunGoo.Metro — Durable Architecture and Delivery Plan

This document is the durable source of truth for `FunGoo.Metro`: a native Metro design system for [Goo](https://github.com/obselate/goo) and `FunGoo`, inspired by Microsoft's Metro philosophy and Metrino's token and motion system.

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
   - True 3D perspective (Turnstile, FlipTile, Dialog Swivel) uses a Slang `ShaderEffect` prototype (`FunGoo.Metro/Shaders/metro_perspective.slang`). Compiling `.slang` to `.goo-effect` requires `slangc 2026.16` (`SLANG_SDK`) and `spirv-val` from Vulkan SDK `1.4.357.0` (`VULKAN_SDK`). Shader compilation is opt-in via `-p:CompileMetroShaders=true`; when the `.goo-effect` artifact is absent or `ReducedMotion` is active, `MetroPerspectiveEffect` falls back cleanly to 2D transforms.
5. **Headless testing:**
   - Goo's `EmbeddedWindowHost` runs full `Window` lifecycle, `Context` subscriptions, `ElementHandle` metrics, `LayoutAlgorithm`, `Portal`, and `PlatformInput` inside `xunit` tests without a GPU or display server.

## Stackable PR Roadmap

Each PR stacks on the previous branch via `gh stack`, builds cleanly, formats with `dotnet fantomas .`, and passes all headless tests.

| Stack # | Branch | Scope | Status |
| --- | --- | --- | --- |
| PR 1 | `metro/01-foundation` | Durable plan, `FunGoo.Metro` + `FunGoo.Metro.Tests` projects, 21 Metrino accents, dark/light tokens, typography/spacing/motion constants, dual-context `MetroTheme` + `Goo.Widgets.Theme` bridge, headless tests | Complete |
| PR 2 | `metro/02-motion-and-shader` | Native `Simulation` curves (`CubicBezierSimulation`, `ExponentialEaseSimulation`), stagger delays, Continuum flight helper, 2D turnstile/swivel fallback, Slang `metro_perspective.slang` prototype + `MetroPerspectiveEffect` runtime loader, headless tests | Planned |
| PR 3 | `metro/03-typography-and-controls` | `MetroText` (10-role ramp), `MetroButton`, `MetroTextBox`, `MetroPasswordBox`, `MetroToggleSwitch` (sharp rectangular), `MetroCheckBox`, `MetroRadioButton`, `MetroSlider` (square thumb), `MetroProgressBar` (determinate + 5-dot), `MetroProgressRing` (5-dot orbit), headless + allocation tests | Planned |
| PR 4 | `metro/04-navigation-layout-gallery` | `MetroPivot` (peeking headers, slide, keyboard circular loop, swipe), `MetroHub` / `MetroPanorama` (peeking sections + scroll parallax), `IconicTile` / `LiveTile` + `TileGridLayout`, `MetroAppBar`, `MetroSettingsFlyout`, `MetroContentDialog`, `samples/MetroGallery` (`--check` + interactive UI), `CHANGELOG.md` | Planned |

### Future Follow-Ups (Post-Stack)

- Compile and embed `metro_perspective.goo-effect` once `SLANG_SDK` (2026.16) and `VULKAN_SDK` (1.4.357.0) are available in CI or dev environment (`dotnet build FunGoo.Metro/FunGoo.Metro.fsproj -p:CompileMetroShaders=true`).
- Evaluate bundled humanist light-weight sans-serif font (for example, Selawik) via `FontSource`.
- Add `SemanticZoom`, `LongListSelector`, `FlipView`, and date/time roller pickers.
- Add RTL logical property verification across Metro navigation and layout controls.

## Verification Gates (Required Per PR)

1. `dotnet fantomas --check .` passes (run `dotnet fantomas .` before committing).
2. Every `.fs` file in `FunGoo.Metro/` has a paired `.fsi` file with XML doc comments in `.fsi` only.
3. `dotnet build FunGoo.slnx` succeeds with 0 errors.
4. `dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj` passes all tests.
5. Allocation checks verify zero unexpected per-build churn in Release builds.
6. Code review gate passes `AGENTS.md`, `goo-fsharp`, `goo-widgets-fsharp`, and `metro-design-system` checklists.
