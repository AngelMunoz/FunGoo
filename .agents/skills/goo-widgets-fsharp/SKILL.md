---
name: goo-widgets-fsharp
description: Drive the Goo.Widgets control library from F#. Covers value widgets (data structs with Build) versus mounted widgets (Cell of WidgetInput), the controlled-input contract (Value/OnChange/OnCommit, OnClick, OnSelect/OnSelectionChange, Open/OnOpenChange, ExpandedIds), why a widget callback rebuilds the host cell, the delegate-caching rule for input snapshots with an object-expression host, nullable-token fluent setters, styling precedence and Create-Part factories, and virtualized lists. Use whenever F# code mounts or configures ListView, DataGrid, TreeView, Slider, TextField, ModalDialog, TabBar, MediaTransport, ProgressBar, NumberInput, ComboBox, or any other Goo.Widgets control.
compatibility: Requires the .NET 10 SDK and projects referencing Goo.Widgets and FunGoo.Widgets.
---

# Goo.Widgets from F#

Read `goo-fsharp` first: it holds the Cell/`Build`/Blob model, the owner rule, mounting,
and the state ladder. This skill covers the control library on top of that model. A
complete worked application lives in `samples/RaznorGoo` in this repository.

## Two widget kinds

A **value widget** is a data struct with `Build() -> Blob`. It holds no state. F# does not
apply the G# implicit conversion to `Blob`, so call `.Build()` when composing children:

```fsharp
Container().Children(
    ProgressBar(Value = 0.6).Build(),
    EmptyState(Title = "No songs").Build())
```

A **mounted widget** is a stateful `Cell<'TInput>` class. Mount it with the two-generic
form and a stable key:

```fsharp
Cell.Mount<MediaTransportInput, MediaTransport>(
    "media-transport",
    MediaTransportInput(
        Playing = false, Position = 12.0, Duration = 180.0, Volume = 0.5,
        OnPlayPause = onPlayPause, OnNext = onNext, OnPrevious = onPrevious))
```

The widget keeps its internals (handles, drag state, type-ahead) in the Cell instance.
App state and I/O stay outside, in your store and services.

## The controlled-input contract

The host owns the state. The widget renders from the input and emits requests through
nullable callbacks. After a callback fires, the owner rule rebuilds your Cell: the widget
calls `InvokeInputCallback`, which runs your delegate in the callback scope of your Cell.
Your next `Build` supplies the updated input, and the widget reseeds from it. Thus a handler
writes the host state and returns. It needs no call of `Rebuild()`.

| Concern | Fields |
| --- | --- |
| Primary value | `Value` + `OnChange`, plus `OnCommit` for the final value |
| Activation | `OnClick` for one target, `On<Part>Click` for a part |
| Single selection | `SelectedId` + `OnSelect` |
| Multi selection | `SelectedIds` + `OnSelectionChange` |
| Open state | `Open` + `OnOpenChange` (+ `OnDismiss` only for a reason or item) |
| Expansion | `ExpandedIds` + `OnExpandedChange` |
| Query | `Query` + `OnQueryChange` |
| Field metadata | `Label`, `Description`, `Error` (never `Issue`) |
| Gating | `Disabled` blocks interaction and Tab; `ReadOnly` blocks edits, keeps selection and copy |

Rules:

- Update your copy of the value, then return from the handler. Never mutate the input's
  collections in place; build a new array or set.
- An empty selection set clears the selection. Activation is not selection.
- Uncontrolled mode: omit both the state field and the callback, and the widget keeps its
  own. Pick the mode once per widget; do not infer it from a missing callback.
- A widget never mutates caller collections. Treat every callback as a request.

This covers the callbacks that the widget gets through its input. Two callbacks do not get
it, and your Cell must call `Rebuild()` for them: a delegate that a typed Cell of yours puts
on a Blob of its own, and a callback that you write in a render function for a Cell that
calls that function inside its `Build`. `goo-fsharp` has the table "Who rebuilds after a
callback".

## The delegate-caching rule

The sharpest rule in this library. A mounted input is compared whole with
`EqualityComparer<'TInput>.Default`. A closure created fresh inside a parent `Build`
changes the snapshot every rebuild, so the widget rebuilds whenever the parent rebuilds —
even when nothing changed.

Create the delegates once, in the closure of `create`, before the object expression:

```fsharp
let create (env: #IProvidePlayback, player: PlayerState, onTrackChanged: unit -> unit)
  : Cell =
  // Created once, before the object expression exists: every input snapshot
  // carries the same delegate instances, so an unchanged input skips the rebuild.
  let onPlayPause () = playPause (env, player)
  let onNext () = skipNext (env, player)
  let onPrevious () = skipPrevious (env, player)
  let onSeek seconds = seek (env, seconds)
  let onVolumeChanged value = setVolume (env, value)

  { new Cell() with
      override _.Build() : Blob =
        Cell.Mount<MediaTransportInput, MediaTransport>(
          "media-transport",
          MediaTransportInput(
            Playing = player.Playing,
            Position = float player.Position,
            Duration = float player.Length,
            Volume = env.Playback.Volume,
            OnPlayPause = onPlayPause,
            OnNext = onNext,
            OnPrevious = onPrevious,
            OnPositionCommit = onSeek,
            OnVolumeChange = onVolumeChanged,
            OnVolumeCommit = onVolumeChanged
          )
        ) }
```

`samples/RaznorGoo/Widgets/BottomBar.fs` in this repository is this form. Plain Blob
callbacks are different: a fresh `Button(OnClick = fun () -> ...)` authored by your own Cell
is fine. Callbacks never gate node reuse, and the owner rule rebuilds your Cell anyway.

## Configuring from F#

- Plain fields set through constructor named properties:
  `ListRow(Title = song.Name, Selected = isSelected)`.
- Nullable token fields (`Color`, `FontSize`, `Gap`, `Padding`, `Width`, `Opacity`,
  `TransitionMs`, ...) use the generated optional setters, which take `'T voption`, copy
  the struct, and are `static member inline` — the chain allocates nothing:

```fsharp
ListRow(Title = song.Name, Selected = isSelected, MinHeight = Nullable (lengthFromFloat 36.0))
    .backgroundColor(Color.Rgb(24, 31, 43))
    .selectedBackgroundColor(Color.Rgb(40, 52, 74))
    .Build()
```

- Nullable value arguments pass by name: `Padding = Nullable 20.`.
- Setters that collide with F# keywords use double-backtick names: `` widget.``open``(ValueSome true) ``.
- Part-style and factory fields are set the same way, when the widget accepts them.

## Styling

- Precedence is fixed: theme defaults, then explicit nullable tokens, then the part
  `Style` — applied in that order through `BasedOn`. Assign `BasedOn` before overrides.
- Part styles are nullable fields named `RootStyle` and `<Part>Style`:
  `TrackStyle`, `FillStyle`, `ThumbStyle`.
- Part factories are nullable `Func` fields named `Create<Part>`. They receive the owning
  descriptor first and a prepared default part last. Change visuals only. Preserve
  identity, row height, input routing, disabled state, and semantics.
- Pseudo-state styles sit on any Blob: `Hover`, `Active`, `Focus`, `DisabledStyle`.
  Animate with `TransitionMs`.
- Themes attach at the root (`Theme.Attach(window)`); a subtree overrides with
  `ThemeScope`. Shared palettes: `Syntax`, `Diff`, `SeriesPalette`.

## Virtualized lists

ListView, DataGrid, and TreeView realize rows through a virtual source. Feed them the
FunGoo way:

- Supply an array or an `IReadOnlyList<'T>` view. Never materialize a `seq` per build.
- One stable, unique string key per item. Index keys break on reorder.
- Fixed item height. `FunGoo.Virtual.create` takes the key and the builder as F# functions.
  Write the two functions in the closure of `create`, not in `Build`: a lambda inside `Build`
  is a new closure at each build, and a function that reads the state of the cell is an
  ambient dependency when it sits at the module level. In the closure, a test controls the
  functions through the factory.

```fsharp
let create (songs: Song array, onSelect: Song -> unit) : Cell =
    let itemKey (song: Song) = song.Id
    let buildRow (song: Song) : Blob = songRow (song, onSelect)

    { new Cell() with
        override _.Build() : Blob =
          Container(Height = 400.0)
            .Children(FunGoo.Virtual.create songs 240.0 36.0 itemKey buildRow) }
```

- `FunGoo.Virtual.ofIndexed` wraps count plus indexer in a list view without copying.
- `VirtualItems` reads the theme, so a `Build()` that creates it needs the build context of
  a mounted cell. Mount the cell in a headless `EmbeddedWindowHost` for that check.

## Hosts and overlays

- `ModalDialogHost`: mount once, key it, never dispose it inside `Build`. Pass the
  `ModalDialog` value as its input; the host owns presentation.
- Popover, Tooltip, Menu, and Drawer present through the window's overlay. No manual
  z-order, no manual positioning.
- `FindBar` and `SearchList` are value structs whose `Build` mounts an internal state
  cell; treat them as value widgets and call `.Build()`.

## Widget map

| Job | Widgets |
| --- | --- |
| Actions | ActionButton, IconButton, ToggleButton |
| Inputs | TextField, TextArea, NumberInput, Slider, Checkbox, RadioButton, RadioGroup, ToggleSwitch, TokenInput, Stepper, Calendar, DatePicker, TimePicker, ColorPicker, ComboBox, RatingControl, FindBar, SearchList, TriggerMenu, ToggleGroup |
| Lists and data | ListView, DataGrid, TreeView, ListRow, Chip, AttachmentChip, Badge, PropertyGrid, TimeAxis |
| Feedback | ProgressBar, ProgressSummary, Banner, DismissibleContextBar, EmptyState, Skeleton, Spinner, Toast + ToastHost, TeachingTip |
| Layout | Card, Grid, Drawer, Disclosure, MasterDetail, SplitPane, AppBar, WindowChrome, SectionHeader, StatusBar, ModalDialog, HoverCard, Popover, SettingsLayout |
| Navigation | TabBar, Breadcrumb, Menu, MenuButton, CommandPalette, NavigationRail |
| Media and charts | AsyncImage, Avatar, MediaCard, MediaTransport, DonutChart, StackedBar, GraphCanvas, GraphNodeCard |
| Content and theme | CodeBlock, Kbd, MarkdownView (separate package), ThemeScope, MaterialIcons |

## Checklist for review

List each of these in the diff:

- `.Build()` missing on a value widget composed as a child.
- A delegate created inside `Build` for a mounted widget input.
- A handler of a widget callback that calls `Rebuild()` on its own Cell. The owner rule
  already does it.
- A callback that changes the state of your Cell and does not rebuild it, when the
  callback is not on the input of the widget: a render function, or a Blob of a typed Cell
  of yours.
- A typed Cell of yours that calls a callback of its host without `InvokeInputCallback`.
- A named class that holds the logic of a Cell (see `goo-fsharp`).
- Host state mutated by the widget's own code instead of updated through a callback.
- An `OnChange` handler that mutates the input's collection in place.
- A `seq` materialized per build for a virtual list; keys unstable or index-based.
- A `Create<Part>` factory that changes identity, row height, routing, or disabled state.
- `BasedOn` assigned after the overrides it should base.
- A widget holding app state or doing I/O.

Each item: fix it, or state why it stays.

## Verify

In the target project:

1. `dotnet build` — the mount and setter shapes above must compile as written.
2. Call the parent `Build()` headless and assert the mounted input's fields on
   the returned tree.
3. Rebuild the parent twice with unchanged store data and measure allocated bytes around
   the second `Build()`: a cached-delegate input adds near zero; fresh closures show up
   here.
4. Check the rebuild of the host: run one callback of the widget through the retained node
   of the widget, then assert that the host built again. A host with a correct input
   callback builds again with no call of `Rebuild()`.
