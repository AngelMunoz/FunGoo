module MetroGallery.Gallery

open System
open System.Collections.Generic
open Goo
open FunGoo.Children
open FunGoo.Metro
open MetroGallery.GalleryStore

/// The named scenes of the gallery: the factory of each scene for a getter of
/// the host window.
type ScenesFactory =
  (unit -> Window voption) -> IReadOnlyDictionary<string, Func<Cell>>

/// The tab and the mode that the gallery shows first.
[<Struct; NoComparison>]
type GalleryStart = { Tab: string; Mode: MetroMode }

/// The gallery: a Pivot with the tabs, an app bar, a settings flyout, and a
/// content dialog. The headless check of the gallery reads the state and
/// drives the gallery through these members. `Gallery.create` makes the cell.
[<AbstractClass>]
type GalleryCell() =
  inherit Cell()

  /// Identifier of the active tab.
  abstract ActiveTab: string

  /// The appearance mode.
  abstract Mode: MetroMode

  /// The accent of the theme.
  abstract AccentKind: MetroAccentKind

  /// <c>true</c> when the settings flyout is open.
  abstract SettingsOpen: bool

  /// <c>true</c> when the content dialog is open.
  abstract DialogOpen: bool

  /// The mounted Pivot, for the headless check.
  abstract Pivot: MetroPivotCell voption

  /// The mounted semantic zoom tab, for the headless check.
  abstract ZoomTab: ZoomTab.ZoomTabCell voption

  /// The mounted selection tab, for the headless check.
  abstract SelectionTab: SelectionTab.SelectionTabCell voption

  /// The mounted inputs tab, for the headless check.
  abstract InputsTab: InputsTab.InputsTabCell voption

  /// The mounted date and time tab, for the headless check.
  abstract DateTimeTab: DateTimeTab.DateTimeTabCell voption

  /// The mounted settings flyout, for the headless check.
  abstract SettingsFlyout: MetroSettingsFlyoutCell voption

  /// The mounted content dialog, for the headless check.
  abstract ContentDialog: MetroContentDialogCell voption

  /// The number of builds, for the headless check.
  abstract Builds: int

  /// Shows the tab with the identifier <paramref name="id"/>, then rebuilds
  /// the gallery.
  abstract SelectTab: id: string -> unit

  /// Sets the appearance mode, then rebuilds the gallery.
  abstract SetMode: mode: MetroMode -> unit

  /// Sets the accent of the theme, then rebuilds the gallery.
  abstract SetAccent: accent: MetroAccentKind -> unit

  /// Opens or closes the settings flyout, then rebuilds the gallery.
  abstract SetSettingsOpen: isOpen: bool -> unit

  /// Opens or closes the content dialog, then rebuilds the gallery.
  abstract SetDialogOpen: isOpen: bool -> unit

/// The Metro theme of the gallery at its start: the dark mode and the blue
/// accent.
let themeConfig: MetroConfig = {
  MetroConfig.defaultConfig with
      Mode = MetroMode.Dark
      Accent = MetroAccentKind.Blue
}

/// The start of the gallery: the overview tab in the dark mode.
let overviewStart: GalleryStart = {
  Tab = "overview"
  Mode = MetroMode.Dark
}

/// One event of the gallery. Each callback of the gallery gives one event.
[<Struct; NoComparison>]
type GalleryEvent =
  | TabSelected of id: string
  | ModeChanged of mode: MetroMode
  | AccentChanged of accent: MetroAccentKind
  | AppBarExpandedChanged of expanded: bool
  | SettingsOpenChanged of isOpen: bool
  | DialogOpenChanged of isOpen: bool
  | DialogResolved of confirmed: bool
  | CommandInvoked of id: string

/// State of the gallery. One mutable field per concern.
type GalleryState = {
  /// The values that the controls tab writes and that the hub reads.
  Store: GalleryStore
  mutable ActiveTab: string
  mutable Mode: MetroMode
  mutable AccentKind: MetroAccentKind
  mutable AppBarExpanded: bool
  mutable SettingsOpen: bool
  mutable DialogOpen: bool
  // The number of builds, for the headless check of the gallery.
  mutable Builds: int
}

/// The factories of the cells that the gallery mounts. `create` makes them
/// once, so that each mount reuses its factory.
type GalleryFactories = {
  ControlsTab: unit -> Cell
  FlipViewTab: unit -> Cell
  ListsTab: unit -> Cell
  SelectionTab: unit -> Cell
  ZoomTab: unit -> Cell
  InputsTab: unit -> Cell
  DateTimeTab: unit -> Cell
  ContinuumDemo: unit -> Cell
  StaggerDemo: unit -> Cell
  LiveTile: unit -> MetroLiveTileCell
  SettingsFlyout: unit -> MetroSettingsFlyoutCell
  ContentDialog: unit -> MetroContentDialogCell
}

/// The values that the functions of the gallery work on.
type GalleryContext = {
  State: GalleryState
  Factories: GalleryFactories
  /// Applies a mode to the host window.
  ApplyMode: MetroMode -> unit
  /// Applies an accent to the host window.
  ApplyAccent: MetroAccentKind -> unit
  /// Rebuilds the mounted hub. The sections of the hub read the store and
  /// the state of the gallery, and an equal input does not rebuild the hub.
  RebuildHub: unit -> unit
}

let pivotItems = [|
  { Id = "overview"; Header = "overview" }
  {
    Id = "typography"
    Header = "typography"
  }
  { Id = "controls"; Header = "controls" }
  {
    Id = "flip-view"
    Header = "flip view"
  }
  { Id = "lists"; Header = "lists" }
  {
    Id = "selection"
    Header = "selection"
  }
  { Id = "zoom"; Header = "zoom" }
  { Id = "inputs"; Header = "inputs" }
  {
    Id = "datetime"
    Header = "date & time"
  }
  { Id = "motion"; Header = "motion" }
|]

let hubSections = [|
  {
    Id = "start"
    Header = "start tiles"
    Width = 340.0
  }
  {
    Id = "status"
    Header = "at a glance"
    Width = 320.0
  }
  {
    Id = "principles"
    Header = "metro ethos"
    Width = 340.0
  }
|]

/// The layout of the start section of the hub. It holds no state.
let tileGrid =
  TileGridLayout.create {
    CellSize = 72.0
    Gap = 8.0
    Columns = 4
    Sizes = [|
      MetroTileSize.Wide
      MetroTileSize.Medium
      MetroTileSize.Small
      MetroTileSize.Small
      MetroTileSize.Small
      MetroTileSize.Small
      MetroTileSize.Wide
    |]
  }

let liveFaces = [|
  {
    Title = "System Telemetry"
    Subtitle = ValueSome "Vulkan retained compositor"
    Metric = ValueSome "60 fps"
    AccentOverride = ValueNone
  }
  {
    Title = "System Telemetry"
    Subtitle = ValueSome "Zero-rebuild paint animations"
    Metric = ValueSome "0 ms"
    AccentOverride = ValueSome(Color.Rgb(0, 171, 169))
  }
  {
    Title = "System Telemetry"
    Subtitle = ValueSome "21 authentic Metro accents"
    Metric = ValueSome "21"
    AccentOverride = ValueSome(Color.Rgb(216, 0, 115))
  }
|]

let primaryCommands = [|
  {
    Id = "theme"
    Label = "Theme"
    IconName = "brightness_6"
    Disabled = false
  }
  {
    Id = "settings"
    Label = "Settings"
    IconName = "palette"
    Disabled = false
  }
  {
    Id = "dialog"
    Label = "Dialog"
    IconName = "chat"
    Disabled = false
  }
|]

let secondaryCommands = [|
  {
    Id = "reset"
    Label = "Reset gallery state"
    IconName = "restart_alt"
    Disabled = false
  }
  {
    Id = "about"
    Label = "Open content dialog"
    IconName = "info"
    Disabled = false
  }
|]

/// A store with the first values of the gallery.
let initialStore() : GalleryStore = {
  TileTapCount = 0
  MailCount = 12
  LastAction = "Ready"
  SliderValue = 65.0
  TextValue = "Modern UI"
  SecretValue = "metro123"
  ToggleEnabled = true
  CheckAccepted = true
  RadioChoice = "compact"
  TogglePressed = false
}

/// Sets each value of the store back to its first value. The last action
/// becomes "State reset".
let resetStore(store: GalleryStore) : unit =
  store.TileTapCount <- 0
  store.MailCount <- 12
  store.LastAction <- "State reset"
  store.SliderValue <- 65.0
  store.TextValue <- "Modern UI"
  store.SecretValue <- "metro123"
  store.ToggleEnabled <- true
  store.CheckAccepted <- true
  store.RadioChoice <- "compact"
  store.TogglePressed <- false

/// The next mode of the theme command.
let nextModeOf(mode: MetroMode) : MetroMode =
  match mode with
  | MetroMode.Dark -> MetroMode.Light
  | MetroMode.Light -> MetroMode.Dark
  | MetroMode.System -> MetroMode.Dark

/// Sets the mode of the gallery and of the host window.
let setMode(ctx: GalleryContext, mode: MetroMode) : unit =
  ctx.State.Mode <- mode
  ctx.ApplyMode mode

/// Sets the mode, the store, and the open states back to their first values.
let reset(ctx: GalleryContext) : unit =
  let state = ctx.State
  state.ActiveTab <- "overview"
  resetStore state.Store
  state.AppBarExpanded <- false
  ctx.RebuildHub()

/// Applies one command of the app bar.
let applyCommand(ctx: GalleryContext, id: string) : unit =
  let state = ctx.State

  match id with
  | "theme" ->
    let next = nextModeOf state.Mode
    setMode(ctx, next)
    state.Store.LastAction <- $"Mode -> {next}"
    ctx.RebuildHub()
  | "settings" ->
    state.SettingsOpen <- true
    state.AppBarExpanded <- false
  | "dialog"
  | "about" ->
    state.DialogOpen <- true
    state.AppBarExpanded <- false
  | "reset" -> reset ctx
  | _ -> ()

/// Applies one event to the state of the gallery.
let applyEvent(ctx: GalleryContext, event: GalleryEvent) : unit =
  let state = ctx.State

  match event with
  | TabSelected id -> state.ActiveTab <- id
  | ModeChanged mode -> setMode(ctx, mode)
  | AccentChanged accent ->
    state.AccentKind <- accent
    ctx.ApplyAccent accent
  | AppBarExpandedChanged expanded -> state.AppBarExpanded <- expanded
  | SettingsOpenChanged isOpen -> state.SettingsOpen <- isOpen
  | DialogOpenChanged isOpen -> state.DialogOpen <- isOpen
  | DialogResolved confirmed ->
    state.Store.LastAction <-
      (if confirmed then "Dialog confirmed" else "Dialog cancelled")

    state.DialogOpen <- false
    ctx.RebuildHub()
  | CommandInvoked id -> applyCommand(ctx, id)

/// Writes the action of one iconic tile of the hub. The Hub cell builds the
/// tile, so it owns the callback of the tile: the owner rule rebuilds the
/// Hub cell, and its sections read the store again.
let openTile(store: GalleryStore, tile: string) : unit =
  store.TileTapCount <- store.TileTapCount + 1
  store.LastAction <- $"Opened {tile}"

/// Increments the Mail tile count and total tile taps.
let openMailTile(store: GalleryStore) : unit =
  store.MailCount <- store.MailCount + 1
  openTile(store, "Mail tile")

/// Writes the action of the live tile. The live tile gives each click to the
/// Hub cell through `InvokeInputCallback`, so the owner rule rebuilds the
/// Hub cell.
let tapLiveTile(store: GalleryStore) : unit =
  store.TileTapCount <- store.TileTapCount + 1
  store.LastAction <- $"Live tile tapped ({store.TileTapCount})"

/// The start section of the hub: the live tile and six iconic tiles.
let startTiles(ctx: GalleryContext, onEvent: GalleryEvent -> unit) : Blob =
  let store = ctx.State.Store

  Container(Layout = tileGrid)
    .Children(
      MetroLiveTile.mount(
        "gallery-live-tile",
        MetroLiveTile.defaults liveFaces,
        ctx.Factories.LiveTile
      ),
      MetroTilt.mount(
        "tile-mail",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Mail", "mail") with
                Count = ValueSome store.MailCount
                Size = MetroTileSize.Medium
          },
          fun () -> openMailTile store
        )
      ),
      MetroTilt.mount(
        "tile-calendar",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Calendar", "calendar_today") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(0, 163, 0))
          },
          fun () -> openTile(store, "Calendar")
        )
      ),
      MetroTilt.mount(
        "tile-music",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Music", "music_note") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(250, 104, 0))
          },
          fun () -> openTile(store, "Music")
        )
      ),
      MetroTilt.mount(
        "tile-photos",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Photos", "photo") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(162, 0, 255))
          },
          fun () -> openTile(store, "Photos")
        )
      ),
      MetroTilt.mount(
        "tile-settings",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Settings", "settings") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(100, 118, 135))
          },
          fun () -> onEvent(SettingsOpenChanged true)
        )
      ),
      MetroTilt.mount(
        "tile-controls",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Explore Controls", "tune") with
                Size = MetroTileSize.Wide
                AccentOverride = ValueSome(Color.Rgb(0, 80, 239))
          },
          fun () -> onEvent(TabSelected "controls")
        )
      )
    )

/// The status section of the hub: the last action, the slider value as a
/// progress bar, and two buttons.
let statusSection(ctx: GalleryContext, onEvent: GalleryEvent -> unit) : Blob =
  let state = ctx.State

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
    .Children(
      MetroText.subtitle "Glance and go",
      MetroText.secondary
        $"Last action: {state.Store.LastAction}. Tile taps: {state.Store.TileTapCount}.",
      MetroText.caption "LIVE PROGRESS",
      Container()
        .Children(
          MetroProgressBar.build(
            "hub-det-bar",
            MetroProgressBar.determinate(state.Store.SliderValue / 100.0)
          )
        ),
      Container(
        FlexDirection = FlexDirection.Row,
        Gap = Length.Pixels 12.0,
        AlignItems = AlignItems.Center
      )
        .Children(
          MetroTilt.mount(
            "hub-customize-theme",
            MetroButton.accent(
              "Customize Theme",
              fun () -> onEvent(SettingsOpenChanged true)
            )
          ),
          MetroTilt.mount(
            "hub-show-dialog",
            MetroButton.standard(
              "Show Dialog",
              fun () -> onEvent(DialogOpenChanged true)
            )
          )
        )
    )

/// The principles section of the hub: two short texts.
let principlesSection() : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.subtitle "Fierce reduction",
      MetroText.body(
        "Typography, whitespace, and alignment replace "
        + "card boxes and drop shadows."
      ),
      MetroText.subtitle "Authentically digital",
      MetroText.secondary(
        "Sharp 0 px corners, 2 px borders, and fast "
        + "cubic Bezier deceleration."
      )
    )

/// Builds one section of the hub. The Hub cell calls this function inside
/// its own `Build`, so it owns the callbacks of the section.
let hubSection
  (ctx: GalleryContext, onEvent: GalleryEvent -> unit, sectionId: string)
  : Blob =
  match sectionId with
  | "start" -> startTiles(ctx, onEvent)
  | "status" -> statusSection(ctx, onEvent)
  | _ -> principlesSection()

/// The input of the Pivot.
let pivotInput(state: GalleryState) : MetroPivotInput = {
  MetroPivot.defaults(pivotItems, state.ActiveTab) with
      Title = ValueSome "FUNGOO METRO"
}

/// The content of one tab. The Pivot calls this function inside its own
/// `Build`.
let tabContent
  (ctx: GalleryContext, hubFactory: unit -> MetroHubCell, tabId: string)
  : Blob =
  match tabId with
  | "overview" ->
    MetroHub.mount(
      "gallery-hub",
      MetroHub.defaults("metro gallery", hubSections),
      hubFactory
    )
  | "typography" -> TypographyTab.view()
  | "controls" -> Cell.Mount<Cell>("controls-tab", ctx.Factories.ControlsTab)
  | "flip-view" -> Cell.Mount<Cell>("flip-view-tab", ctx.Factories.FlipViewTab)
  | "lists" -> Cell.Mount<Cell>("lists-tab", ctx.Factories.ListsTab)
  | "selection" -> Cell.Mount<Cell>("selection-tab", ctx.Factories.SelectionTab)
  | "zoom" -> Cell.Mount<Cell>("zoom-tab", ctx.Factories.ZoomTab)
  | "inputs" -> Cell.Mount<Cell>("inputs-tab", ctx.Factories.InputsTab)
  | "datetime" -> Cell.Mount<Cell>("datetime-tab", ctx.Factories.DateTimeTab)
  | "motion" ->
    MotionTab.view(ctx.Factories.ContinuumDemo, ctx.Factories.StaggerDemo)
  | _ -> TypographyTab.view()

/// The content of the settings flyout: the three modes and one swatch for
/// each Metro accent.
let settingsContent(state: GalleryState, onEvent: GalleryEvent -> unit) : Blob =
  let theme = Metro.read()

  let swatchGrid =
    Container(
      FlexDirection = FlexDirection.Row,
      FlexWrap = FlexWrap.Wrap,
      Gap = Length.Pixels 8.0
    )

  let accents = MetroAccent.named

  for i = 0 to accents.Count - 1 do
    let kind, name, accent = accents[i]
    let isSelected = state.AccentKind = kind

    let borderColor =
      if isSelected then theme.Palette.Foreground else accent.Base

    // One delegate for the click and for the two keys.
    let selectAction = Action(fun () -> onEvent(AccentChanged kind))

    let swatch =
      Button(
        Key = $"accent-{name}",
        Width = Length.Pixels 44.0,
        Height = Length.Pixels 44.0,
        BackgroundColor = accent.Base,
        BorderRadius = Length.Pixels 0.0,
        BoxShadows = Array.empty,
        BorderWidth = Edges(Length.Pixels(if isSelected then 3.0 else 1.0)),
        BorderColor = Edges<Color>(borderColor),
        Cursor = Cursor.Pointer,
        Focusable = true,
        TabStop = true,
        OnClick = selectAction,
        KeyBindings = [|
          KeyBinding(Key.Enter, KeyModifiers.None, selectAction)
          KeyBinding(Key.Space, KeyModifiers.None, selectAction)
        |],
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Button,
            Name = name,
            Selected = Nullable isSelected
          )
      )

    swatchGrid.Add(swatch)

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 20.0)
    .Children(
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
        .Children(
          MetroText.caption "APPEARANCE",
          Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 8.0)
            .Children(
              MetroTilt.mount(
                "settings-mode-dark",
                MetroButton.build(
                  {
                    MetroButton.defaults "Dark" with
                        Pressed = ValueSome(state.Mode = MetroMode.Dark)
                  },
                  fun () -> onEvent(ModeChanged MetroMode.Dark)
                )
              ),
              MetroTilt.mount(
                "settings-mode-light",
                MetroButton.build(
                  {
                    MetroButton.defaults "Light" with
                        Pressed = ValueSome(state.Mode = MetroMode.Light)
                  },
                  fun () -> onEvent(ModeChanged MetroMode.Light)
                )
              ),
              MetroTilt.mount(
                "settings-mode-system",
                MetroButton.build(
                  {
                    MetroButton.defaults "System" with
                        Pressed = ValueSome(state.Mode = MetroMode.System)
                  },
                  fun () -> onEvent(ModeChanged MetroMode.System)
                )
              )
            )
        ),
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
        .Children(
          MetroText.caption $"ACCENT ({accents.Count} METRO COLORS)",
          swatchGrid
        )
    )

/// The settings flyout of the gallery.
let settingsFlyout(ctx: GalleryContext) : Blob =
  MetroSettingsFlyout.mount(
    "gallery-settings-flyout",
    MetroSettingsFlyout.defaults("Settings", ctx.State.SettingsOpen),
    ctx.Factories.SettingsFlyout
  )

/// The app bar of the gallery.
let appBar(state: GalleryState, onEvent: GalleryEvent -> unit) : Blob =
  MetroAppBar.build(
    {
      MetroAppBar.defaults primaryCommands with
          Secondary = secondaryCommands
          Expanded = state.AppBarExpanded
    },
    (fun id -> onEvent(CommandInvoked id)),
    fun () -> onEvent(AppBarExpandedChanged(not state.AppBarExpanded))
  )

/// The content dialog of the gallery.
let contentDialog(ctx: GalleryContext) : Blob =
  MetroContentDialog.mount(
    "gallery-content-dialog",
    {
      MetroContentDialog.defaults("Metro Content Dialog", ctx.State.DialogOpen) with
          PrimaryText = ValueSome "Confirm"
          SecondaryText = ValueSome "Cancel"
    },
    ctx.Factories.ContentDialog
  )

/// The gallery page: the workspace with the Pivot, then the app bar, the
/// settings flyout, and the content dialog.
let page
  (
    ctx: GalleryContext,
    pivotFactory: unit -> MetroPivotCell,
    onEvent: GalleryEvent -> unit
  ) : Blob =
  let state = ctx.State
  let theme = Metro.read()

  let workspace =
    Container(
      Key = "gallery-workspace",
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      Width = Length.Percent 100.0,
      Padding =
        Edges(
          Length.Pixels 24.0,
          Length.Pixels 32.0,
          Length.Pixels 12.0,
          Length.Pixels 32.0
        ),
      Overflow = Overflow.Hidden
    )
      .Children(
        MetroPivot.mount("gallery-pivot", pivotInput state, pivotFactory)
      )

  let bar =
    Container(Key = "gallery-bar", FlexShrink = 0.0)
      .Children(appBar(state, onEvent))

  let flyout = settingsFlyout ctx
  let dialog = contentDialog ctx

  Container(
    BasedOn = Metro.canvasStyle theme,
    FlexDirection = FlexDirection.Column,
    Width = Length.Percent 100.0,
    Height = Length.Percent 100.0,
    Overflow = Overflow.Hidden
  )
    .Children(workspace, bar, flyout, dialog)

/// Builds the tree of the gallery. It counts its builds for the headless
/// check.
let build
  (
    ctx: GalleryContext,
    pivotFactory: unit -> MetroPivotCell,
    onEvent: GalleryEvent -> unit
  ) : Blob =
  ctx.State.Builds <- ctx.State.Builds + 1
  page(ctx, pivotFactory, onEvent)

/// Applies one event for the headless check, then rebuilds the gallery. The
/// check writes the state of the gallery from outside a callback: no owner
/// rule rebuilds the gallery, thus the gallery rebuilds itself.
let applyFromCheck
  (cell: GalleryCell, ctx: GalleryContext, event: GalleryEvent)
  : unit =
  applyEvent(ctx, event)
  cell.Rebuild()

/// Creates the gallery. <paramref name="getWindow"/> gives the host window
/// when it exists. <paramref name="start"/> is the first tab and the first
/// mode. The closure holds the state.
let create
  (getWindow: unit -> Window voption, start: GalleryStart)
  : GalleryCell =
  let state: GalleryState = {
    Store = initialStore()
    ActiveTab = start.Tab
    Mode = start.Mode
    AccentKind = MetroAccentKind.Blue
    AppBarExpanded = false
    SettingsOpen = false
    DialogOpen = false
    Builds = 0
  }

  // The mounted cells, for the headless check of the gallery.
  let mutable pivotRef: MetroPivotCell voption = ValueNone
  let mutable selectionTabRef: SelectionTab.SelectionTabCell voption = ValueNone
  let mutable zoomTabRef: ZoomTab.ZoomTabCell voption = ValueNone
  let mutable inputsTabRef: InputsTab.InputsTabCell voption = ValueNone
  let mutable dateTimeTabRef: DateTimeTab.DateTimeTabCell voption = ValueNone
  let mutable flyoutRef: MetroSettingsFlyoutCell voption = ValueNone
  let mutable dialogRef: MetroContentDialogCell voption = ValueNone

  // The mounted hub, for a rebuild of its sections.
  let mutable hubRef: MetroHubCell voption = ValueNone

  // The slot of the cell: the callbacks of the sections of the hub and of the
  // settings flyout content are made before the cell exists.
  let mutable self: GalleryCell voption = ValueNone

  let applyMode(mode: MetroMode) =
    getWindow() |> ValueOption.iter(fun window -> Metro.setMode(window, mode))

  let applyAccent(accent: MetroAccentKind) =
    getWindow()
    |> ValueOption.iter(fun window -> Metro.setAccent(window, accent))

  // The sections of the hub read the store and the state of the gallery, but
  // an equal input does not rebuild the hub. The callbacks that change these
  // values are on the app bar and on the content dialog: Blobs or typed cells
  // of the gallery cell. The owner rule rebuilds the gallery cell, thus the
  // gallery cell rebuilds the hub also, when the overview tab shows it.
  let rebuildHub() =
    if state.ActiveTab = "overview" then
      hubRef |> ValueOption.iter(fun hub -> hub.Rebuild())

  let mutable ctxSlot: GalleryContext voption = ValueNone

  let onEvent(event: GalleryEvent) =
    ctxSlot |> ValueOption.iter(fun ctx -> applyEvent(ctx, event))

  // The Hub cell and the SettingsFlyout cell call their render callbacks
  // inside their own Build, so they own those child callbacks: the owner rule
  // rebuilds the child cell and not this cell. Any callback that changes the
  // state of this cell gives its event here, which also rebuilds this cell.
  let onChildEvent(event: GalleryEvent) =
    onEvent event
    self |> ValueOption.iter(fun cell -> cell.Rebuild())

  let flyoutFactory() : MetroSettingsFlyoutCell =
    let cell =
      MetroSettingsFlyout.create(
        (fun () -> onEvent(SettingsOpenChanged false)),
        fun () -> settingsContent(state, onChildEvent)
      )

    flyoutRef <- ValueSome cell
    cell

  let dialogFactory() : MetroContentDialogCell =
    let cell =
      MetroContentDialog.create(
        (function
        | MetroContentDialogAction.Primary -> onEvent(DialogResolved true)
        | MetroContentDialogAction.Secondary -> onEvent(DialogResolved false)
        | MetroContentDialogAction.Dismiss -> onEvent(DialogOpenChanged false)),
        fun () ->
          MetroText.body(
            "Full-width Metro band dialog presented via Goo.Portal with "
            + "modal focus trapping, 2 px accent borders, and zero radius."
          )
      )

    dialogRef <- ValueSome cell
    cell

  let factories: GalleryFactories = {
    ControlsTab = fun () -> ControlsTab.create state.Store
    FlipViewTab = FlipViewTab.create
    ListsTab = ListsTab.create
    SelectionTab =
      fun () ->
        let tab = SelectionTab.create()
        selectionTabRef <- ValueSome tab
        tab :> Cell
    ZoomTab =
      fun () ->
        let tab = ZoomTab.create()
        zoomTabRef <- ValueSome tab
        tab :> Cell
    InputsTab =
      fun () ->
        let tab = InputsTab.create()
        inputsTabRef <- ValueSome tab
        tab :> Cell
    DateTimeTab =
      fun () ->
        let tab = DateTimeTab.create()
        dateTimeTabRef <- ValueSome tab
        tab :> Cell
    ContinuumDemo = ContinuumDemo.create
    StaggerDemo = StaggerDemo.create
    LiveTile = MetroLiveTile.createFactory(fun () -> tapLiveTile state.Store)
    SettingsFlyout = flyoutFactory
    ContentDialog = dialogFactory
  }

  let ctx: GalleryContext = {
    State = state
    Factories = factories
    ApplyMode = applyMode
    ApplyAccent = applyAccent
    RebuildHub = rebuildHub
  }

  ctxSlot <- ValueSome ctx

  let hubFactory() : MetroHubCell =
    let cell =
      MetroHub.create(fun sectionId -> hubSection(ctx, onChildEvent, sectionId))

    hubRef <- ValueSome cell
    cell

  let pivotFactory() : MetroPivotCell =
    let cell =
      MetroPivot.create(
        (fun id -> onEvent(TabSelected id)),
        fun tabId -> tabContent(ctx, hubFactory, tabId)
      )

    pivotRef <- ValueSome cell
    cell

  let cell = {
    new GalleryCell() with
      override _.ActiveTab = state.ActiveTab
      override _.Mode = state.Mode
      override _.AccentKind = state.AccentKind
      override _.SettingsOpen = state.SettingsOpen
      override _.DialogOpen = state.DialogOpen
      override _.Pivot = pivotRef
      override _.SelectionTab = selectionTabRef
      override _.ZoomTab = zoomTabRef
      override _.InputsTab = inputsTabRef
      override _.DateTimeTab = dateTimeTabRef
      override _.SettingsFlyout = flyoutRef
      override _.ContentDialog = dialogRef
      override _.Builds = state.Builds

      override this.SelectTab(id) =
        applyFromCheck(this, ctx, TabSelected id)

      override this.SetMode(mode) =
        applyFromCheck(this, ctx, ModeChanged mode)

      override this.SetAccent(accent) =
        applyFromCheck(this, ctx, AccentChanged accent)

      override this.SetSettingsOpen(isOpen) =
        applyFromCheck(this, ctx, SettingsOpenChanged isOpen)

      override this.SetDialogOpen(isOpen) =
        applyFromCheck(this, ctx, DialogOpenChanged isOpen)

      override _.Build() : Blob = build(ctx, pivotFactory, onEvent)
  }

  self <- ValueSome cell
  cell
