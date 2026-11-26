# FunGoo.Metro — Durable Architecture and Delivery Plan

This document is the durable source of truth for `FunGoo.Metro`: a native Metro design system for [Goo](https://github.com/obselate/goo) and `FunGoo`, written in F#.

## Design Authority

- The `metro-design-system` skill (`.agents/skills/metro-design/SKILL.md`) is the design rule.
- [Metrino](https://github.com/AngelMunoz/metrino) (`E:\Metrino`, a TypeScript web library, version 0.6.0 on 2026-11-22) is a reference for implementation details only: token values, component structure, motion numbers.
- When Metrino and the Metro skill disagree, the Metro skill wins.

## Delivery Rules

- One small unit of work per control family, delivered as well defined, verifiable, and deliverable commits. No gigantic change. Do not stack pull requests.
- Every commit passes the review checklists of `goo-fsharp` and `goo-widgets-fsharp`.
- No reflection in library code. Write new tests with the public Goo API only.
- Write F# functions directly. Add an `Action` or `Func` wrapper only when the compiler requires it (hard fact 18).
- Attach a `goo render` image to each change that adds or changes a control.

## Code Shape Rules

The first 26 rows of the roadmap produced `FunGoo.Metro`, `samples/MetroGallery`, and the tests as named classes with `as this`. Four causes pushed that way: the review checklist of `goo-fsharp` did not examine the form of a cell, the example of its delegate-caching rule was a class, its checklist named the author of a callback where it must name the owner, and no skill showed an object expression that reaches its own cell. The skills are corrected (commit `ee13321`). These rules keep the code in the corrected shape. `goo-fsharp` has the examples.

1. **A cell is a module.** The module holds plain functions and one `create` function that returns an object expression. The closure of `create` holds the state. A test calls each plain function with no cell.
2. **No named class holds logic or state.** `as this` is the sign. An object expression behaves like the type that it expresses: `Cell`, `Cell<TInput>`, `Simulation`, and `EmbeddedWindowHost` all take one (hard fact 16).
3. **An abstract cell type only for a public surface.** It is necessary when outside code drives the state of the cell through members (a shell that the composition root navigates, a control that a check reads), or when a typed cell gives a callback to its host through `InvokeInputCallback` (hard fact 16). A typed cell with neither is an object expression over `Cell<TInput>`.
4. **A service is an interface and a `create` function.** A factory that holds callbacks or a cache is a service: one interface, one module, one `create` that returns an object expression.
5. **Callbacks are plain F# functions.** F# converts a function to a delegate where a Goo API takes one (hard fact 18).
6. **A host of a widget does not rebuild itself.** Goo runs the input callback of a Goo.Widgets control in the callback scope of the host cell (hard fact 15). A `Rebuild()` in such a handler is redundant. A typed cell of this library gives the callbacks of its host to the host cell in the same way, through `InvokeInputCallback`.
7. **A check reads state through a public surface:** the members of an abstract cell type, a state record that the check holds, or a public view builder. A public member that only a check reads is the test seam of the control: declare it in the signature file and say so in its comment. A public member that nothing reads is removed.
8. **A file has four parts, in this order:** the data types, the abstract cell type when rule 3 needs it, the plain functions, and `create` last.

Each commit of the re-shape and each new control passes gate 9.

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
   - The default UI family is Selawik, embedded under the SIL Open Font License (`FunGoo.Metro/Fonts`): light (300), regular (400), and bold (700) registered through `FontSource` when a theme attaches. A custom `FontFamily` in the config still overrides the default.
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

13. **Virtual collections:**
    - `VirtualItems` returns a `Virtual` element that is its own scroll viewport. Set the wrap layout, the width, and the height on that element. A wrapper that holds the height leaves the element without a size, and it shows no items.
    - The key of the content of a virtual item must be equal to the item key, or absent.
    - An item that is equal to its previous value is not built again. Put the changing state of one item, such as its selection, into the item value.
    - Give `VirtualItems` one key delegate and one builder delegate per owner cell, so that every build reuses them.

14. **Programmatic scroll of a collection:**
    - `ElementHandle.ScrollToItem(key, alignment, settle)` works only on a mounted virtual element. It returns `false` for other elements.
    - The scroll viewport of `ListView` is an internal element of the widget. Its handle is not public, and the root handle that `ConfigureRoot` receives is set after the callback. Code outside the widget cannot scroll a `ListView` to an item.
    - To scroll a collection from code, own the `Virtual` element and assign your own `ElementHandle` to it.

15. **Callback ownership:**
    - A `Blob` records its `CallbackOwner` when it is created. The owner rule rebuilds that cell after a callback.
    - A host callback that a cell calls inside its own `Build` (`renderContent`, `renderSection`, `renderItem`, `render`) creates its Blobs in that cell. The callbacks of those Blobs belong to the cell, and not to the host.
    - When such a callback changes the state of the host cell, it must also rebuild the host cell.
    - One factory of `MetroListView`, of `MetroGridView`, or of `MetroComboBox` sends the events of all its controls to one handler, and the events do not name the control. Use one factory for each control.
    - A timer action of a cell runs through `RunOwnedAction`, and Goo rebuilds the cell after the action (`RebuildAfterCallback`). Measured on 2026-11-23 in a headless window with a probe cell: after a window timer ran the action, the cell built again, with no `Rebuild()` call in the action. A window timer uses the wall clock and not the frame time. A test that waits for a timer waits in real time.
    - A Goo.Widgets control calls `InvokeInputCallback` for the callbacks of its input (45 widget source files, among them `Slider`, `NumberInput`, `RatingControl`, `ComboBox`, and `ListView`). Goo runs the callback in the callback scope of the host cell, and the owner rule rebuilds the host. A host handler of a widget callback writes its state and returns. It does not call `Rebuild()`. Measured on 2026-11-23 in a headless window: one click on a spin button of `NumberInput` rebuilt the host from 1 to 2 builds, with no `Rebuild()` call.
    - Since the re-shape (R5, R6, R7), each typed cell of `FunGoo.Metro` gives the callbacks of its host (`onSelect`, `onEvent`, `onClick`) to the host cell through `InvokeInputCallback`, as a Goo.Widgets control does. Thus no host rebuilds itself from such a callback. No cell of `FunGoo.Metro` calls `Rebuild()`. Exceptions: a cell calls `Rebuild()` only when no callback and no timer caused the change. The animated overlays call it at the end of a close animation, to unmount the closed surface. `MetroViewboxCell` calls it when its viewport size or its content size changes.

16. **Object expressions (measured on 2026-11-23 in a headless window):**
    - An object expression behaves like the type that it expresses. `{ new Cell() with ... }`, `{ new Cell<TInput>() with ... }`, `{ new Simulation() with ... }`, and `{ new EmbeddedWindowHost() with ... }` compile and run. `Build(input)`, `InputChanged`, `Mounted`, and `Unmounting` are overridden in the object expression.
    - A plain typed cell needs no named type. `Cell.Mount<TInput, Cell<TInput>>(key, input, factory)` mounts an object expression over `Cell<TInput>`. The cell builds, and a changed input rebuilds it. The factory form is necessary: the form without a factory needs a class that F# can construct.
    - An abstract cell type holds the public surface: `[<AbstractClass>] type XCell() = inherit Cell<XInput>()`, with `abstract` members and no `let` binding. `create` returns `{ new XCell() with ... }`, and `Cell.Mount<XInput, XCell>` takes the abstract type as its type argument.
    - An object expression cannot declare a member of its own (FS0767). A forwarding member needs the abstract type.
    - `InvokeInputCallback` is a protected member of `Cell<TInput>`. F# does not permit a call to a protected member from a lambda of an object expression (FS0491). The abstract cell type declares the forwarding member, `member this.Raise(callback: Action<string>, value: string) = this.InvokeInputCallback(callback, value)`, and the lambda calls `this.Raise`. Measured: the host of such a cell rebuilt from 1 to 2 builds with no `Rebuild()` call.
    - A cell reaches itself outside `Build` in three ways. The cell is a parameter of a plain function. A self slot (`let mutable self: Cell voption = ValueNone`, set after the object expression) gives `Rebuild()` to a callback that was made before the cell. `AnimateValue` is called after the object expression exists. `let rec` on an object expression compiles with warning FS0040.
    - `InvokeInputCallback` is on `Cell<TInput>` only. A plain `Cell` has no member that gives a callback to a host: `BeginCallback`, `EndCallback`, `RunOwnedAction`, and `RebuildAfterCallback` are internal. A cell that must give a callback to its host is a typed cell.
    - A Goo.Widgets control (`Slider`, `NumberInput`, `RatingControl`, `ComboBox`, `ListView`) declares no public member that starts the callback of its input. `Window` has no public member that injects a pointer event or a key event. Thus a check that uses the public API only drives a host rebuild through a typed cell of this library: the `OnClick` of a Blob of its `View`, or its `Handle` member. The gallery check does this in its steps 1, 7, and 9.
    - `View` builds the tree from the current state on each read. A changed `View` does not prove that the cell built again. A build count on the abstract cell type proves it (`Builds`). An input of a mounted cell also proves it, when only a build of the host changes that input (`IsZoomedOut`).
    - Observed: the retained node of the root Blob of a mounted cell did not keep the `Key` of that Blob. A check that looks for a node by key needs a keyed child below the root.

17. **API of Goo 0.31.0:**
    - `Window.Post(action) bool` puts work on the UI thread. `Cell.Post` is its protected form. Goo has no `Window.TryPost`.
    - `Cell.Mount` takes the key before the factory: `Cell.Mount<Cell>(key, factory)`.
    - The virtual collection helper of FunGoo is `FunGoo.Virtual.create items itemWidth itemHeight itemKey itemBuilder`, with F# functions. `VirtualLists` is not in the package.
    - `VirtualItems` reads the theme. A `Build()` that creates it needs the build context of a mounted cell. A direct call outside a mount throws `Context requires an owned build or virtual callback`. Mount the cell in a headless `EmbeddedWindowHost` for that check.

18. **Function to delegate:**
    - F# converts a function value to `Action` or `Func` at a delegate parameter, property, or constructor: `this.Raise(onSelect, id)`, `Cell.Mount<Cell>(key, factory)`, `MediaTransportInput(OnPlayPause = onPlayPause)`. A lambda converts at a property, at a constructor, and at a method argument: `Button(OnClick = fun () -> ...)`.
    - The compiler requires a typed wrapper (`Action<float>(...)`, `Func<Song, Blob>(...)`) in four cases: a lambda passed to an F# function whose parameter has a delegate type (FS0002, for example the third argument of `MetroSlider.mount`), a lambda with a parameter that has no type (`Action(fun value -> ...)` binds the non-generic `Action`), a conditional with `null`, and a lambda in a record field of a delegate type (FS0002, for example `OnSelect = fun id -> ...` where the field has the type `Action<string>`).
    - A function value needs no wrapper, also at a parameter of an F# function that has a delegate type, and also as a record field of a function type. `let onSliderChange (value: float) = ...` then `MetroSlider.mount(key, props, onSliderChange)` compiles. `MetroFlipView.mount(key, input, factory)` takes `unit -> MetroFlipViewCell`. Measured on 2026-11-23 (R9, R9b).
    - Measured with `NumberInputInput` and `MediaTransportInput`: a function value that F# converts at each build keeps the input equal, because the target of the delegate is the function value. One delegate made once keeps the input equal. A lambda with a capture that is written inside `Build` makes the input different.

19. **Portals render nothing in `goo render`:**
    - `goo render` of Goo.DevTools 0.31.0 shows no `Portal` content. Measured on 2026-11-24: an anchored portal, a portal with no size, and a portal with a size and a scrim all render nothing in the frame.
    - Verify an overlay with a headless geometry test in `EmbeddedWindowHost`. There a portal lays out correctly from the first frame.

## Control Catalog Gap (2026-11-22)

State of the catalog after stack #16 (PR #14 to #21, merged to `main` on 2026-11-25; the seven stack branches are deleted):

| Family | Metrino controls | In `FunGoo.Metro` | Missing |
| --- | --- | --- | --- | --- |
| Buttons | Button, DropdownButton, HyperlinkButton, RepeatButton | Button, Hyperlink (variant) | DropdownButton, RepeatButton |
| Date and time | Calendar, CalendarDatePicker, DatePicker, DatePickerRoller, TimePicker, TimePickerRoller | Calendar (`MetroCalendar`), CalendarDatePicker (`MetroCalendarDatePicker`), DatePicker (`MetroDatePicker`), DatePickerRoller (`MetroDatePickerRoller`), TimePicker (`MetroTimePicker`), TimePickerRoller (`MetroTimePickerRoller`) | none |
| Dialogs | ContentDialog, Flyout, MessageDialog, SettingsFlyout, TeachingTip | ContentDialog, SettingsFlyout, Flyout (`MetroFlyout`), MessageDialog (`MetroMessageDialog`), TeachingTip (`MetroTeachingTip`) | none |
| Inputs | AutoSuggestBox, CheckBox, ComboBox, NumberBox, PasswordBox, RadioButton, Rating, RichEditBox, Slider, TextBox, ToggleSwitch | CheckBox, RadioButton, Slider, TextBox (with password mode), ToggleSwitch, NumberBox (`MetroNumberBox`), Rating (`MetroRating`), ComboBox and AutoSuggestBox (`MetroComboBoxFactory`) | RichEditBox |
| Layout | Canvas, Grid, ScrollViewer, StackPanel, TileGrid, VariableSizedWrapGrid, Viewbox, WrapPanel | TileGridLayout, ScrollViewer (`MetroScrollViewer`), VariableSizedWrapGrid (`MetroVariableSizedWrapGrid`), Viewbox (`MetroViewbox`), WrapPanel (`MetroWrapPanel`) | Canvas, Grid, StackPanel |
| Navigation | AppBar, AppBarButton, AppBarSeparator, AppBarToggleButton, Hub, HubSection, Panorama, PanoramaItem, Pivot, PivotItem, SplitView, TabView, TabViewItem | AppBar (commands only), Hub (with sections), Pivot, AppBarButton (`MetroAppBarButton`), AppBarSeparator (`MetroAppBarSeparator`), AppBarToggleButton (`MetroAppBarToggleButton`), Panorama (`MetroPanorama`), SplitView (`MetroSplitView`), TabView (`MetroTabView`) | none |
| Primitives | Border, ContextMenu, Expander, Icon, Image, InfoBar, MediaElement, MenuFlyout, PersonPicture, RichTextBlock, TextBlock, Toast, Tooltip | TextBlock (as `MetroText`), Icon (as `MaterialIcons` in tiles), Border (`MetroBorder`), ContextMenu (`MetroContextMenu`), Expander (`MetroExpander`), Image (`MetroImage`), InfoBar (`MetroInfoBar`), MenuFlyout (`MetroMenuFlyout`), PersonPicture (`MetroPersonPicture`), RichTextBlock (`MetroRichTextBlock`), Toast (`MetroToast`), Tooltip (`MetroTooltip`) | MediaElement |
| Selection | FlipView, GridView, ListBox, ListPicker, ListView, LongListSelector, SemanticZoom, TreeView | FlipView, ListView (`MetroListView`), ListBox (`MetroListBox`), GridView (`MetroGridView`), SemanticZoom (`MetroSemanticZoomCell`), ListPicker (`MetroListPicker`), LongListSelector (`MetroLongListSelector`), TreeView (`MetroTreeView`) | none |
| Tiles | CycleTile, FlipTile, IconicTile, LiveTile | IconicTile, LiveTile, CycleTile (`MetroCycleTile`), FlipTile (`MetroFlipTile`) | none |

## Stackable PR Roadmap (GitHub Stack `#5`)

Stack `#5` was delivered as stacked pull requests. GitHub merged the 12 pull requests to `main` on 2026-11-23 with the rebase method, and the 12 branches are deleted. The table is the record of that stack. New work goes to `main` as commits (see Delivery Rules).

| Stack # | Branch | Pull Request | Scope | Status |
| --- | --- | --- | --- | --- |
| PR 1 | `metro/01-foundation` | #1 | Durable plan, `FunGoo.Metro` + `FunGoo.Metro.Tests` projects, 21 Metrino accents, dark/light tokens, typography/spacing/motion constants, dual-context `MetroTheme` + `Goo.Widgets.Theme` bridge, headless tests | Complete |
| PR 2 | `metro/02-motion-and-shader` | #2 | Native `Simulation` curves (`CubicBezierSimulation`, `ExponentialEaseSimulation`), stagger delays, Continuum flight helper, 2D turnstile/swivel fallback, Slang `metro_perspective.slang` prototype + `MetroPerspectiveEffect` runtime loader, headless tests | Complete |
| PR 3 | `metro/03-typography-and-controls` | #3 | `MetroText` (10-role ramp), `MetroButton`, `MetroTextBox`, `MetroPasswordBox`, `MetroToggleSwitch` (sharp rectangular), `MetroCheckBox`, `MetroRadioButton`, `MetroSlider` (square thumb), `MetroProgressBar` (determinate + 5-dot), `MetroProgressRing` (5-dot orbit), headless + allocation tests | Complete |
| PR 4 | `metro/04-navigation-layout-gallery` | #4 | `MetroPivot` (peeking headers, slide, keyboard circular loop, swipe), `MetroHub` / `MetroPanorama` (peeking sections + scroll parallax), `IconicTile` / `LiveTile` + `TileGridLayout`, `MetroAppBar`, `MetroSettingsFlyout`, `MetroContentDialog`, `samples/MetroGallery` (`--check` + interactive UI), `CHANGELOG.md` | Complete |
| PR 5 | `metro/05-render-scenes` | #6 | Live tile starts its cycle timer only on an open window, so `goo render` works. Gallery registers one `Window.Scenes` scene per tab and follows `--theme`. Plan: design authority, delivery rules, new hard facts, catalog gap, planned PR set, known issues | Complete |
| PR 6 | `metro/06-flip-view` | #7 | `MetroFlipView`: one item at a time, edge previous and next buttons, indicator dots, swipe, `Left`/`Right`/`PageUp`/`PageDown`/`Home`/`End` keys, slide transition, public view builder (`MetroFlipView.buildView`, `View`, `State`, `Handle`), gallery tab and scene, reflection-free tests, shared `TestSupport` helpers | Complete |
| PR 7 | `metro/07-public-view-builders` | | Public view builders for the existing stateful controls, so that the tests and the gallery check use the public API only | Complete with exception. See the note below the table |
| PR 8 | `metro/08-list-view` | #8 | `MetroListView`: `Goo.Widgets.ListView` with sharp 44 px rows, a 3 px accent indicator and an accent tint on selected rows, selection modes `Activate`, `Single`, and `Multiple`, one event channel, a factory that keeps one callback set, gallery tab and scene, reflection-free tests | Complete |
| PR 9 | `metro/09-list-box-grid` | #9 | `MetroListBox`: a sharp 2 px bordered box around a Metro list. `MetroGridViewFactory` (now `MetroGridView.createFactory`): a virtual wrapped grid of square tile buttons on `VirtualItems`, with a 2 px accent border and an accent tint on selected tiles, selection modes `Activate`, `Single`, and `Multiple`, gallery additions, reflection-free tests | Complete |
| PR 10 | `metro/10-long-list-selector` | #14 | Grouped list with group headers and a jump list. Needs its own virtual list with its own handle, because `ListView` gives no public access to its scroll viewport (hard fact 14) | Complete |
| PR 11 | `metro/11-semantic-zoom` | #10 | `MetroSemanticZoomCell`: a zoomed-in detail view and a zoomed-out overview view of the same content, a zoom toggle button in the top end corner, a scale and fade transition on each zoom, reduced motion, public view seam (`MetroSemanticZoom.buildView`, `View`, `State`, `Handle`), gallery tab and scene, reflection-free tests | Complete |
| PR 12 | `metro/12-looping-selector` | #15 | Shared roller column (`MetroLoopingSelector`) | Complete |
| PR 13 | `metro/13-date-time-pickers` | #15 | `MetroDatePicker` and `MetroTimePicker` on the roller; dropdown variants | Complete |
| PR 14 | `metro/14-calendar` | #17 | `MetroCalendar`, `MetroCalendarDatePicker` | Complete |
| PR 15 | `metro/13-combo-suggest` | #12 | `MetroComboBoxFactory` (now `MetroComboBox.createFactory`) on `Goo.Widgets.Inputs.ComboBox`: a combo box (`MetroComboBox.defaults`) and an auto-suggest box (`MetroComboBox.autoSuggest`) with one event channel (`Selected`, `QueryChanged`, `OpenChanged`), a pure `toData` mapping, cached callbacks and mapped options, the gallery `inputs` tab (which replaces the `number` tab) and its `inputs` scene, reflection-free tests | Complete |
| PR 16 | `metro/12-number-rating` | #11 | `MetroNumberBox` on `Goo.Widgets.Inputs.NumberInput` (spin buttons, bounds, step, label) and `MetroRating` on `Goo.Widgets.Inputs.RatingControl` (accent stars), each with a pure `toInput` mapping, a gallery tab and scene, reflection-free tests | Complete |
| PR 17 | `metro/17-dialogs` | #18 | `MetroFlyout`, `MetroMessageDialog` | Complete |
| PR 18 | `metro/18-menus-tooltips` | #18 | `MetroContextMenu`, `MetroTooltip` | Complete |
| PR 19 | `metro/19-notifications` | #19 | `MetroToast`, `MetroInfoBar`, `MetroTeachingTip` | Complete |
| PR 20 | `metro/20-split-tab` | #20 | `MetroSplitView`, `MetroTabView` | Complete |
| PR 21 | `metro/21-app-bar-parts` | #20 | `MetroAppBarButton`, `MetroAppBarToggleButton`, `MetroAppBarSeparator`, `MetroPanorama` | Complete |
| PR 22 | `metro/22-primitives` | #21 | `MetroExpander`, `MetroBorder`, `MetroImage`, `MetroPersonPicture` | Complete |
| PR 23 | `metro/23-layout` | #21 | `MetroScrollViewer`, `MetroViewbox`, `MetroWrapPanel`, `MetroVariableSizedWrapGrid` | Complete |
| PR 24 | `main` | — | `MetroFlipTile`, `MetroCycleTile` on `main` (2026-11-25). `MetroDropdownButton` and `MetroRepeatButton` are cut: ComboBox/MenuFlyout and plain buttons cover their jobs with less chrome | Complete |
| PR 25 | `metro/25-rtl` | | RTL logical property verification across navigation and layout controls: 7 headless tests in `RtlTests`, logical-property fixes, no gallery scene (a scene needs a full tab) | Complete |
| PR 26 | `metro/14-gallery-zoom-inputs-fix` | #13 | Gallery fix. The zoom tab now zooms in when a group tile is selected, because it rebuilds itself from a callback that the zoom cell owns. The inputs tab has one handler and one factory for each combo box. The gallery check runs both flows, and two mutations showed that each check fails on its defect. Four signature files explain callback ownership. Two factory signatures state one factory for each control. Audit pass on `goo-fsharp` and `goo-widgets-fsharp`. | Complete |
| PR 27 | `main` | — | `goo-fsharp` and `goo-widgets-fsharp` corrected: the object expression is the form of a cell, a table states who rebuilds after a callback, callbacks are plain functions, and four API calls are corrected (`Window.Post`, the argument order of `Cell.Mount`, the typed mount, `FunGoo.Virtual.create`). The skills refer to no other skill. Commit `ee13321`. | Complete |

Rules for the planned rows: each row is one unit of work on one theme, delivered as commits on `main`: one new test file, one gallery scene. Verify the base `Goo.Widgets` API in the repository before the row starts. Split a row when it grows past the size of PR 3. The Code Shape Re-shape is complete (R10, 2026-11-23). Each new row keeps its code in the shape of the Code Shape Rules.

**PR 7 is complete with an explicit exception (option (a), 2026-11-25).** Every tree assertion that a public view builder can serve now reads the public API: `MetroPivotCell.View`, `MetroLiveTileCell.View`, `MetroCycleTileCell.View`, `MetroFlipTileCell.View`, `MetroFlipViewCell.View` with `MetroFlipView.buildView`, `MetroSemanticZoomCell.View` with `MetroSemanticZoom.buildView`, `MetroContentDialogCell.View` with `Handle`, `MetroSettingsFlyoutCell.View`, and the progress cells (`MetroProgressBar.buildWith`, `MetroProgressRing.mountWith`). The Hub needs no view builder: its tests assert only the public `ScrollHandle` and `HeaderTranslate`, so rule 7 forbids adding one. The remaining reflective calls read values with no public read (Known Issues, item 1): style values (hard fact 6), retained Goo widget nodes (hard fact 6 and 16), and animation ticks (hard fact 12). New controls use public view seams from the start, as `MetroFlipView` does. Option (b) (`goo render` with `goo diff`) stays a possible future step and is not done.

Metrino controls with no planned port: `RichEditBox` (no rich editing base in Goo.Widgets 0.22.2), `MediaElement` (no media base), and `Canvas`, `StackPanel`, `Grid` (Goo `Container`, `Row`, `Column`, and `Goo.Widgets.Grid` already supply these layouts under the Metro theme). Confirm each decision in its own row before work starts.

## Code Shape Re-shape (2026-11-23)

Scope: `FunGoo.Metro/`, `tests/FunGoo.Metro.Tests/`, and `samples/MetroGallery/`. The target is the set of Code Shape Rules. The behavior of each control stays, with one intended change: each typed cell of the library gives the callbacks of its host to the host cell (rule 6), so a host does not rebuild itself from such a callback. Out of scope: `FunGoo`, `FunGoo.Widgets`, `FunGoo.Mvu` (a released package), `samples/RaznorGoo`, and `samples/MvuSpike`. `FunGoo.Metro` is not released and `CHANGELOG.md` names no class of it, thus a changed public name needs no changelog entry.

**Status: complete on 2026-11-23.** The subsection "Result of the re-shape" at the end of this section lists the commits, the deviations from the steps table, and the exceptions of gate 9.

### What was a named class before the re-shape

| Where | Named class | Public surface | Readers |
| --- | --- | --- | --- |
| `MetroPivot.fs` | `MetroPivotCell(onSelect, renderContent)` | `ContentTranslate`, `ContentOpacity` | tests |
| `MetroHub.fs` | `MetroHubCell(renderSection)` | `ScrollHandle`, `HeaderTranslate` | tests |
| `MetroFlipView.fs` | `MetroFlipViewCell(onSelect, renderItem)` | `TrackTranslate`, `OutgoingId`, `IsSliding`, `State`, `View`, `Handle` | tests |
| `MetroSemanticZoom.fs` | `MetroSemanticZoomCell(onEvent, render)` | `ScaleAnim`, `OpacityAnim`, `IsZoomedOut`, `State`, `View`, `Handle` | tests and the gallery check |
| `MetroTiles.fs` | `MetroLiveTileCell(onClick)` | `ActiveFaceIndex`, `IsTimerActive`, `FaceTranslate`, `FaceOpacity` | tests. No reader for `FaceTranslate` and `FaceOpacity` |
| `MetroTiles.fs` | `TileGridLayout(cellSize, gap, columns, sizes)` | `CellSize`, `Gap`, `Columns` | tests: 4 constructions, 2 reads of `Columns` |
| `MetroProgress.fs` | `MetroIndeterminateBarCell`, `MetroProgressRingCell` (in no `.fsi` file) | none | `MetroProgressBar`, `MetroProgressRing`, and 2 reflective lookups by type name in the tests |
| `MetroListView.fs`, `MetroCollections.fs`, `MetroComboBox.fs` | `MetroListViewFactory`, `MetroGridViewFactory`, `MetroComboBoxFactory` | `CreateInput`, `Mount`, `Build`, `BuildItem` | gallery: 6 constructions, tests: 8 constructions |
| `MetroMotion.fs`, `MetroProgress.fs` | `CubicBezierSimulation`, `ExponentialEaseSimulation`, `PeriodicSimulation`, and 5 private simulations | `Position`, `Velocity`, `Done`, `IsInfinite` | tests: 16 constructions |
| `MetroTheme.fs` | `MetroAttachment` (private) | none | `Metro.attach`, `Metro.setMode`, `Metro.setAccent` |
| `samples/MetroGallery` | 8 cells with `as this`, and `HeadlessCheckHost`. 23 calls of `this.Rebuild()`. `Program.fs` has 1425 lines. | 22 members for the gallery check | the gallery check |
| `tests/FunGoo.Metro.Tests` | `CaptureCell` (1 public copy and 2 private copies), `DualThemeProbe`, `ScopeHostCell`, `ContinuumProbeCell`, `HeadlessWindowHost` | `Captured`, `Track`, `Scale`, `Alpha`, `DuringBuild`, `Wakes` | 9 test files |

### The target shape of each kind

- **A typed cell with a public surface** (`MetroPivotCell`, `MetroHubCell`, `MetroFlipViewCell`, `MetroSemanticZoomCell`, `MetroLiveTileCell`): the type stays and becomes abstract. It has `[<AbstractClass>]`, `inherit Cell<XInput>`, the public members as `abstract` members, one forwarding member per callback shape (`member this.Raise(callback: Action<string>, value: string) = this.InvokeInputCallback(callback, value)`), and no `let` binding. The module gets `create (…) : MetroXCell`, which returns an object expression. `createFactory` and `mount` keep their signatures, so a host changes only the constructor call. The cell calls the callback of the host through `this.Raise`.
- **The two internal progress cells:** internal abstract cell types with `create`, mounted with the factory form of `Cell.Mount`. The type names stay for the 2 reflective lookups of the tests (known issue 1).
- **A factory** (`MetroListViewFactory`, `MetroGridViewFactory`, `MetroComboBoxFactory`): one interface (`IMetroListViewFactory`, `IMetroGridViewFactory`, `IMetroComboBoxFactory`) and one `create` function (`MetroListView.createFactory (onEvent) : IMetroListViewFactory`). The cache of the mapped rows is in the closure.
- **`TileGridLayout`:** `TileGridLayout.create (cellSize, gap, columns, sizes) : LayoutAlgorithm` returns an object expression. One plain function gives the resolved values, and the tests read them there.
- **A simulation:** a plain function that returns an object expression over `Simulation`: `MetroMotion.cubicBezier (...)`, `MetroMotion.exponentialEase (...)`, `MetroMotion.periodic (periodSeconds)`, and private functions of the same form in `ProgressInternals`. The math stays in private plain functions.
- **`MetroAttachment`:** a record with mutable fields and plain functions. The theme module creates the delegate at the call.
- **A test probe:** an abstract probe type holds the members that the tests read (`CaptureCell`: `Captured`, `Track`, `Scale`, `Alpha`, `DuringBuild`), and `TestSupport` holds the `create` function. The 2 private copies of `CaptureCell` go. `HeadlessWindowHost` becomes a function that returns an object expression.
- **A gallery tab:** one file per tab, with the 4 parts of rule 8. `ControlsTab.fs` leaves `Program.fs`. An abstract cell type exists only for a cell that the check reads: `GalleryCell`, `ZoomTabCell`, `InputsTabCell`. `FlipViewTabCell` and `ListsTabCell` have no member and become plain object expressions. `HeadlessCheckHost` becomes a function.
- **Rebuilds in the gallery:** the 9 `Rebuild()` calls in the handlers of the widget inputs go now (hard fact 15). The calls in the handlers of the Pivot, of the FlipView, and of the zoom toggle go when the library gives the callback to the host cell. 11 calls stay: 4 in callbacks of a render function for the Hub, 2 in callbacks of a render function for the zoom cell, and 5 in the members that the check calls. The gallery cell and the zoom tab keep a self slot for the render functions (hard fact 16).

### Steps

Each step is one commit on `main`, and it passes the nine gates. A step that changes a public name changes its callers in the same commit.

| Step | Commit | Scope | New test | Done when |
| --- | --- | --- | --- | --- |
| R1 | `refactor(metro tests): make the test probes object expressions` | `TestSupport.fs`, `TestHost.fs`, and the 4 files with a private probe | none | Gate 9 passes for `tests/`, and the 52 tests pass. |
| R2 | `refactor(metro): make the theme attachment a record with functions` | `MetroTheme.fs` | none | No class in `MetroTheme.fs`. No `.fsi` file changes. |
| R3 | `refactor(metro): make the simulations object expressions` | `MetroMotion.fs`, `MetroProgress.fs`, `MetroMotion.fsi`, `MotionAndShaderTests.fs` | `Position` and `Velocity` allocate nothing in a Release build | The 3 public simulations are functions. No type in a `.fsi` file inherits `Simulation`. |
| R4 | `refactor(metro): make the tile grid a create function` | `MetroTiles.fs`, `MetroTiles.fsi`, `NavigationAndOverlaysTests.fs` | none | `TileGridLayout.create` returns a `LayoutAlgorithm`. |
| R5 | `feat(metro): the Pivot and the Hub give the callbacks of the host to the host cell` | `MetroPivot.fs`, `MetroHub.fs`, their `.fsi` files, `NavigationAndOverlaysTests.fs`, `Program.fs` | `A host of the Pivot rebuilds through the owner rule`. Mutation: a direct call of the callback makes the test fail | The 2 cell types are abstract. The gallery needs no `this` for the Pivot. |
| R6 | `feat(metro): the FlipView and the SemanticZoom give the callbacks of the host to the host cell` | `MetroFlipView.fs`, `MetroSemanticZoom.fs`, their `.fsi` files, `FlipViewTests.fs`, `SemanticZoomTests.fs`, `FlipViewTab.fs`, `ZoomTab.fs` | The same test for each control | The 2 cell types are abstract. |
| R7 | `refactor(metro): make the live tile and the 2 progress cells object expressions` | `MetroTiles.fs`, `MetroProgress.fs`, `MetroTiles.fsi`, `ControlsTests.fs`, `NavigationAndOverlaysTests.fs`, `Program.fs` | `A host of the live tile rebuilds through the owner rule` | The 3 cell types are abstract. `FaceTranslate` and `FaceOpacity` are gone. The ring mounts with the factory form. |
| R8 | `refactor(metro): make the list, grid, and combo factories interfaces with a create function` | `MetroListView.fs`, `MetroCollections.fs`, `MetroComboBox.fs`, their `.fsi` files, `ListsTab.fs`, `InputsTab.fs`, 3 test files | none | No `Factory` class. `MetroListBox.build` takes the interface. |
| R9 | `refactor(metro gallery): make each tab a module with a create function` | `samples/MetroGallery/*` and `MetroGallery.fsproj` | One step in the gallery check: a tab with a widget input rebuilds with no call of `Rebuild()` | One file per tab. `Program.fs` holds the composition root and `main`. `as this` is gone. The 11 calls of `Rebuild()` that stay have a comment that names the owner. |
| R10 | `docs(metro): close the code shape re-shape` | `docs/METRO_PLAN.md` | none | Gate 9 passes with no exception, or this plan lists each exception with its reason. This section is complete. |

An object expression is a compiler-generated class, as a named class is. Gate 7 holds for each step. `Position` and `Velocity` of a simulation run for each animation frame, so R3 measures them.

### Result of the re-shape (2026-11-23)

Each commit is on `main`, and each passed the nine gates. The 16 rendered frames of each commit are identical to the frames of `0bae3dd`, the commit before the re-shape.

| Step | Commit | Tests |
| --- | --- | --- |
| R1 | `3bb41ed` | 52 |
| R2 | `a7c887d` | 52 |
| R3 | `45e0b3d` | 53 |
| R4 | `e018ffb` | 53 |
| R5 | `e7a4d00` | 54 |
| R6 | `d4ef494` | 54 |
| R7 | `7edc83c` | 55 |
| R8 | `5d3d09d` | 55 |
| R9 | `5307fed` | 55 |
| R9b | `4a9b420` | 55 |
| R9c | `e9e5881` | 56 |
| R10 | the commit of this plan change | 56 |

The state after R10:

- `as this` has no hit in `FunGoo.Metro`, `samples/MetroGallery`, and `tests/FunGoo.Metro.Tests`. Each `inherit Cell` in the three directories is an abstract cell type. No cell of `FunGoo.Metro` calls `Rebuild()`. Exceptions: a cell calls `Rebuild()` only when no callback and no timer caused the change. The animated overlays call it at the end of a close animation, to unmount the closed surface. `MetroViewboxCell` calls it when its viewport size or its content size changes. No code of `FunGoo.Metro` and no code of the gallery reads a member through reflection. (`MetroShader` opens `System.Reflection` for `Assembly`, to load the embedded shader resource.)
- The abstract cell types of the library and of the gallery are `MetroPivotCell`, `MetroHubCell`, `MetroFlipViewCell`, `MetroSemanticZoomCell`, `MetroLiveTileCell`, `MetroIndeterminateBarCell`, `MetroProgressRingCell`, `GalleryCell`, `ZoomTabCell`, and `InputsTabCell`. Each has a member that outside code drives, a member that a check reads, or a forwarding member.
- `samples/MetroGallery` has one file module per tab (13 files). `Program.fs` holds the composition root and `main`: 75 lines, from 1429.
- The gallery check has 16 steps (0 to 15) and uses the public API only.

Deviations from the steps table, with the reason:

- **R2** made the theme attachment a private interface with a `create` function. The table says a record with mutable fields and plain functions. The state record has four mutable fields and the plain functions do the work. The interface lets the dictionary of the attachments hold one object expression for each window, with the configuration and the two operations that the theme module calls.
- **R7** made the two progress cells public abstract types with the test seams `TranslateAnims`, `OpacityAnims`, and `View`. `MetroProgressBar.buildWith` and `MetroProgressRing.mountWith` take the factory of the cell, so that a test keeps the cell. `build` and `mount` keep their signatures.
- **R8** moved each helper of `ListInternals`, `ComboInternals`, and `GridInternals` into the module of its one user. A factory maps an item array or an option array one time. Before, each input mapped the array and then discarded the result when the cache had it. Two cache tests assert that an input with the same array allocates nothing.
- **R9**: the new step of the gallery check is step 9. The table says "a tab with a widget input". The step drives the zoom cell of the zoom tab, and asserts that the tab builds one time with no `Rebuild()` in its handler. The step does not drive a Goo.Widgets control, because of two limits of Goo (hard fact 16): `InvokeInputCallback` is on `Cell<TInput>` only, and a Goo.Widgets control has no public member that starts the callback of its input. The three tabs with a widget input have no `Rebuild()` call. Four sites of `Rebuild()` stay in the gallery, and each has a comment that names the owner: the callbacks of the sections of the Hub (4 callbacks), the callbacks of the zoom cell (2 callbacks), the 5 members that the check calls, and the rebuild of the mounted hub. Step 1 of the check reads the tree of the Pivot through `View` inside a build, and asserts that the gallery builds one time after a click on a header.
- **R9b** is not in the table. `createFactory` and `mount` of the Pivot, the Hub, and the live tile take and return a function, as those of the FlipView and the SemanticZoom do. The `Func` made the tests write nine constructors that the compiler does not need.
- **R9c** is not in the table. The timer action of the live tile does not call `Rebuild()`. Goo rebuilds the cell after a timer action (hard fact 15). A new test with a probe cell measures it.

Exceptions of gate 9 after the re-shape:

- **Reflection in three test files.** See Known Issues, item 1. Each call reads a value that has no public read, or drives a control that has no public member for it. The controls were functions before the re-shape, or their tests changed to the public API in their step.
- **Public functions with four or more parameters.** See Known Issues, item 7.

## Known Issues (Work in Progress)

1. **Reflection in three test files (explicit PR 7 exception).** The gallery check uses the public API only since R9. Every tree assertion that a public view builder can serve reads the public `View` (PR 7 note above the table). The calls below stay because the public API has no read for them, and rule 7 forbids adding a public member that only a check reads unless it is declared as a test seam. New tests use the public API only. (a) Style values (hard fact 6: style properties are write-only, `Style.Entries` is internal). `ControlsTests`: `entriesMethod`, `readEntries`, `tryLastEntry`, `requireEntry`, `boxShadowCount`; `readNodeLength` reads retained-node length fields; the `<BorderTopColor>k__BackingField` read asserts the error field border. `NavigationAndOverlaysTests`: `entriesMethod`, `readEntries`, `tryLastEntry`, `requireEntry`, `boxShadowCount`. (b) Retained Goo widget nodes with no public read (hard facts 6 and 16). `ControlsTests`: `windowNodeField` with `findNodeByKey` reads the `entry` node of a mounted text widget (two uses in the Metro inputs test); the `<Input>k__BackingField` reads reach the `Touch` and `Slider` widget inputs, and the `Content` field reads reach the touch content that holds the `TextEntry`; none of these widgets exposes a public node or input. `NavigationAndOverlaysTests`: `childCount` and `childAt` read `Portal.Children` through reflection because the property has no public read. (c) Animation ticks (hard fact 12: a headless host never advances animations). `MotionAndShaderTests`: `updateTreeMethod` with `pumpMotionFrames` ticks `Window.UpdateTree` so the flip-tile shrink and the Continuum probes move. `NavigationAndOverlaysTests`: `updateTreeMethod` with the local `tickFlip` ticks the same gate for the flip-tile shrink assertions. Delegate invocations such as `OnClick.Invoke()`, `OnPress.Invoke()`, `OnChange.Invoke()`, and `Func.Invoke` are public API calls, not reflection, and are not part of this exception.
2. **Tile text color: fixed.** Metro rule: an accent surface uses a white foreground. `MetroAccent.OnAccent` is `Color.White` for every accent, so tile text and tile icons are white on the accent fill. Metrino paints white text on its iconic, live, and cycle tiles.
3. **Toggle thumb color: fixed.** The same Metro rule applies. The checked `MetroToggleSwitch` thumb is white on the accent track, because the thumb uses `MetroAccent.OnAccent`.
4. **Pivot header overflow: resolved.** PR #14 lets the header strip pan without a visible scrollbar and keeps the active tab header in view.
5. **Code shape: resolved.** R1 to R10 replaced each named class that held logic or state in `FunGoo.Metro`, `samples/MetroGallery`, and the tests. `as this` has no hit, each `inherit Cell` is an abstract cell type, and the 9 redundant calls of `Rebuild()` in the handlers of the widget inputs are gone.
6. **Live tile timer callback: resolved.** R9c removed the `Rebuild()` call from the timer action of the live tile. Hard fact 15 has the measurement that permits it.
7. **Public functions with four or more parameters: resolved (2026-11-25).** `MetroMotion.cubicBezier` takes `MetroCubicBezierArgs`, `Continuum.playEnter` and `Continuum.playExit` take `MetroContinuumPlayArgs`, and `MetroContentDialog.build` takes the properties and one `MetroContentDialogHandlers` record. No public function of `FunGoo.Metro` has more than 3 parameters.
8. **RTL absolute insets need a fresh mount to mirror (2026-11-25).** Flex order and logical padding and borders mirror on a live `Direction` switch with no cell rebuild (Hub, Panorama, WrapPanel, SplitView, InfoBar). Absolute logical insets (`Start`/`End`) mirror on a fresh RTL mount (SemanticZoom toggle, `RtlTests`), but a live direction switch with a cell rebuild gave inconsistent toggle geometry between two identical probes (8 px and 552 px), so a live switch is not asserted. This needs a Goo-side minimal repro. The Pivot keeps the physical `Left`/`Right` arrow keys in RTL by design: the keys name physical keys, and swapping their meaning needs a decision of its own.

### Resuming on Another Machine

1. Update `main`. Stack `#5` is merged, and its branches are deleted:
   ```sh
   git fetch origin && git checkout main && git pull --ff-only
   ```
2. Build and run all `182` headless tests plus the gallery check:
   ```sh
   dotnet build FunGoo.slnx
   dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj
   dotnet run --project samples/MetroGallery/MetroGallery.fsproj -- --check
   ```
3. Formatting on a Windows checkout. With `core.autocrlf=true`, the working tree holds CRLF and `.editorconfig` requires LF. On a fresh checkout, `dotnet fantomas --check .` then reports every file, with line endings only as the difference. Run `dotnet fantomas .` once, then confirm with `git diff --ignore-cr-at-eol --stat` that only the files of the PR changed. Restore the other files with `git checkout -- <file>`.
4. Render gallery frames for review (`goo` is the Goo DevTools CLI):
   ```sh
   goo render samples/MetroGallery/MetroGallery.fsproj --list
   goo render samples/MetroGallery/MetroGallery.fsproj --scene controls --theme light --size 1200x800 --out controls-light.png
   ```
5. Shader compilation in CI. The release workflow checks out the pinned Goo submodule and runs `Goo/.github/scripts/install-shader-toolchain-linux-x64.sh`, so the Slang and SPIRV-Tools versions follow the Goo release that the submodule records and keep no copy in FunGoo (the packaged LunarG `spirv-val` reports an RC build string and fails the Goo validator check). It then builds with `-p:CompileMetroShaders=true`, packs `FunGoo.Metro` with the compiled `.goo-effect` shader embedded, and probes the packed package from a clean SDK-free consumer that asserts `MetroPerspective.isEmbeddedShaderAvailable`. A local machine without the SDKs still builds and tests without the shader:
   - Build and test with:
     ```sh
     dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj -p:CompileMetroShaders=true
     ```
   - Without the SDKs the embedded shader is absent and `MetroPerspectiveEffect` falls back to 2D transforms.

## Future Follow-Ups (After the Planned PR Set)

- Decide whether to wire the live 3D perspective flip transition into `MetroLiveTile` (FlipTile). The `MetroPivot` turnstile and the `MetroContentDialog` swivel already run through the perspective shader with 2D fallbacks. CI compiles the `metro_perspective` shader and packs it in the `FunGoo.Metro` package, so check-in of a precompiled artifact and local SDK installs are no longer needed for shipping.
- Evaluate semilight and semibold Selawik weights if a future type role maps to them (the current ramp uses 300, 400, and 700 only).

## Verification Gates (Required Per Commit)

1. `dotnet fantomas --check .` passes (run `dotnet fantomas .` before committing).
2. Every `.fs` file in `FunGoo.Metro/` has a paired `.fsi` file with XML doc comments in `.fsi` only.
3. `dotnet build FunGoo.slnx` succeeds with 0 errors in Debug and in Release.
4. `dotnet test tests/FunGoo.Metro.Tests/FunGoo.Metro.Tests.fsproj` passes all tests in Debug and in Release.
5. `dotnet run --project samples/MetroGallery/MetroGallery.fsproj -- --check` passes.
6. For a change of a control: `goo render` writes a frame of the new showcase scene, and the pull request or the issue attaches the image.
7. Allocation checks verify zero unexpected per-build churn in Release builds.
8. Code review gate passes `AGENTS.md`, `goo-fsharp`, `goo-widgets-fsharp`, and `metro-design-system` checklists. No reflection in library code. New tests use the public API only.
9. Code shape gate (see Code Shape Rules): `grep -rn "as this" FunGoo.Metro samples/MetroGallery tests/FunGoo.Metro.Tests` returns no hit. Each `inherit Cell` in these three directories is an abstract cell type: `[<AbstractClass>]`, abstract members and forwarding members only, and no `let` binding. Each constructor (`new:`) in a `.fsi` file of `FunGoo.Metro` belongs to an abstract cell type. The gate permits a named class only for a type that F# must name for a G# API; the commit message states that reason.
