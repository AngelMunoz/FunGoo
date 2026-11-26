module MetroGallery.Gallery

open System
open System.Collections.Generic
open Elmish
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Mvu
open MetroGallery.GalleryStore
open MetroGallery.Pages

/// The named scenes of the gallery: the factory of each scene for a getter of
/// the host window.
type ScenesFactory =
  (unit -> Window voption) -> IReadOnlyDictionary<string, Func<Cell>>

/// The tab and the mode that the gallery shows first.
[<Struct; NoComparison>]
type GalleryStart = { Tab: string; Mode: MetroMode }

/// The model of the gallery shell: the active tab, the mode, the accent, and
/// the open states of the app bar, the settings flyout, and the dialog.
[<Struct; NoComparison>]
type GalleryModel = {
  ActiveTab: string
  Mode: MetroMode
  AccentKind: MetroAccentKind
  AppBarExpanded: bool
  SettingsOpen: bool
  DialogOpen: bool
}

/// One message of the gallery shell. Each callback of the shell posts one
/// message, and `update` applies it.
type GalleryMsg =
  | TabSelected of id: string
  | ModeChanged of mode: MetroMode
  | AccentChanged of accent: MetroAccentKind
  | AppBarExpandedChanged of expanded: bool
  | SettingsOpenChanged of isOpen: bool
  | DialogOpenChanged of isOpen: bool
  | DialogResolved of confirmed: bool
  | CommandInvoked of id: string
  | Stop

/// The gallery: a Pivot with the pages, an app bar, a settings flyout, and a
/// content dialog. The cell is a `MvuCell`: each callback posts a message and
/// the Elmish loop rebuilds the cell. The headless check of the gallery reads
/// the state and drives the gallery through these members.
[<AbstractClass>]
type GalleryCell
  (
    post: (unit -> unit) -> bool,
    stop: GalleryMsg,
    program: Program<GalleryStart, GalleryModel, GalleryMsg, Blob>,
    initArg: GalleryStart
  ) =
  inherit
    MvuCell<GalleryStart, GalleryModel, GalleryMsg>(
      post,
      stop,
      program,
      initArg
    )

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

  /// The mounted overlays tab, for the headless check.
  abstract OverlaysTab: OverlaysTab.OverlaysTabCell voption

  /// The mounted notifications tab, for the headless check.
  abstract NotificationsTab: NotificationsTab.NotificationsTabCell voption

  /// The mounted navigation tab, for the headless check.
  abstract NavigationTab: NavigationTab.NavigationTabCell voption

  /// The mounted primitives and layout tab, for the headless check.
  abstract PrimitivesTab: PrimitivesTab.PrimitivesTabCell voption

  /// The mounted settings flyout, for the headless check.
  abstract SettingsFlyout: MetroSettingsFlyoutCell voption

  /// The mounted content dialog, for the headless check.
  abstract ContentDialog: MetroContentDialogCell voption

  /// The number of builds, for the headless check.
  abstract Builds: int

  /// Shows the tab with the identifier <paramref name="id"/>.
  abstract SelectTab: id: string -> unit

  /// Sets the appearance mode.
  abstract SetMode: mode: MetroMode -> unit

  /// Sets the accent of the theme.
  abstract SetAccent: accent: MetroAccentKind -> unit

  /// Opens or closes the settings flyout.
  abstract SetSettingsOpen: isOpen: bool -> unit

  /// Opens or closes the content dialog.
  abstract SetDialogOpen: isOpen: bool -> unit

/// The Metro theme of the gallery at its start: the dark mode and the blue
/// accent.
let themeConfig: MetroConfig = {
  MetroConfig.defaultConfig with
      Mode = MetroMode.Dark
      Accent = MetroAccentKind.Blue
}

/// The start of the gallery: the hub tab in the dark mode.
let hubStart: GalleryStart = { Tab = "hub"; Mode = MetroMode.Dark }

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
  OverlaysTab: unit -> Cell
  NotificationsTab: unit -> Cell
  NavigationTab: unit -> Cell
  PrimitivesTab: unit -> Cell
  ContinuumDemo: unit -> Cell
  StaggerDemo: unit -> Cell
  LiveTile: unit -> MetroLiveTileCell
  CycleTile: unit -> MetroCycleTileCell
  FlipTile: unit -> MetroFlipTileCell
  SettingsFlyout: unit -> MetroSettingsFlyoutCell
  ContentDialog: unit -> MetroContentDialogCell
}

/// The environment of the Elmish program of the shell: the values that
/// `update` and `view` work on.
type ShellEnv = {
  /// The values that the controls tab writes and that the hub reads.
  Store: GalleryStore
  /// Applies a mode to the host window.
  ApplyMode: MetroMode -> unit
  /// Applies an accent to the host window.
  ApplyAccent: MetroAccentKind -> unit
  /// Rebuilds the mounted hub, when the hub page shows it.
  RequestHubRebuild: unit -> unit
  /// The factories of the cells that the gallery mounts.
  Factories: GalleryFactories
  /// Makes the hub cell.
  Hub: unit -> MetroHubCell
  /// Makes the Pivot cell.
  Pivot: unit -> MetroPivotCell
  /// Makes the temporary input page.
  InputPage: unit -> Cell
  /// Makes the temporary collections page.
  CollectionsPage: unit -> Cell
  /// Makes the temporary navigate page.
  NavigatePage: unit -> Cell
  /// Makes the temporary dialogs page.
  DialogsPage: unit -> Cell
  /// Makes the temporary motion page.
  MotionPage: unit -> Cell
  /// The latest model that `view` published. Factories made before the cell
  /// read it instead of a stale model.
  mutable Latest: GalleryModel
  /// The number of builds, for the headless check of the gallery.
  mutable Builds: int
}

let pivotItems = [|
  { Id = "hub"; Header = "hub" }
  { Id = "type"; Header = "type" }
  { Id = "input"; Header = "input" }
  {
    Id = "collections"
    Header = "collections"
  }
  { Id = "navigate"; Header = "navigate" }
  { Id = "dialogs"; Header = "dialogs" }
  { Id = "motion"; Header = "motion" }
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

/// The first model of the shell for <paramref name="start"/>.
let init(start: GalleryStart) : GalleryModel * Cmd<GalleryMsg> =
  {
    ActiveTab = start.Tab
    Mode = start.Mode
    AccentKind = MetroAccentKind.Blue
    AppBarExpanded = false
    SettingsOpen = false
    DialogOpen = false
  },
  Cmd.none

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

/// Sets the mode, the store, and the open states back to their first values.
let reset(env: ShellEnv, model: GalleryModel) : GalleryModel =
  resetStore env.Store
  env.RequestHubRebuild()

  {
    model with
        ActiveTab = "hub"
        AppBarExpanded = false
  }

/// Applies one command of the app bar.
let applyCommand
  (env: ShellEnv, model: GalleryModel, id: string)
  : GalleryModel =
  match id with
  | "theme" ->
    let next = nextModeOf model.Mode
    env.ApplyMode next
    env.Store.LastAction <- $"Mode -> {next}"
    env.RequestHubRebuild()
    { model with Mode = next }
  | "settings" -> {
      model with
          SettingsOpen = true
          AppBarExpanded = false
    }
  | "dialog"
  | "about" -> {
      model with
          DialogOpen = true
          AppBarExpanded = false
    }
  | "reset" -> reset(env, model)
  | _ -> model

/// Applies one message to the model of the shell. The handlers that touch the
/// window or the hub run on the post thread of the Elmish loop.
let update
  (env: ShellEnv, msg: GalleryMsg, model: GalleryModel)
  : GalleryModel * Cmd<GalleryMsg> =
  match msg with
  | TabSelected id -> { model with ActiveTab = id }, Cmd.none
  | ModeChanged mode ->
    env.ApplyMode mode
    { model with Mode = mode }, Cmd.none
  | AccentChanged accent ->
    env.ApplyAccent accent
    { model with AccentKind = accent }, Cmd.none
  | AppBarExpandedChanged expanded ->
    { model with AppBarExpanded = expanded }, Cmd.none
  | SettingsOpenChanged isOpen -> { model with SettingsOpen = isOpen }, Cmd.none
  | DialogOpenChanged isOpen -> { model with DialogOpen = isOpen }, Cmd.none
  | DialogResolved confirmed ->
    env.Store.LastAction <-
      (if confirmed then "Dialog confirmed" else "Dialog cancelled")

    env.RequestHubRebuild()
    { model with DialogOpen = false }, Cmd.none
  | CommandInvoked id -> applyCommand(env, model, id), Cmd.none
  | Stop -> model, Cmd.none

/// The input of the Pivot.
let pivotInput(model: GalleryModel) : MetroPivotInput = {
  MetroPivot.defaults(pivotItems, model.ActiveTab) with
      Title = ValueSome "FUNGOO METRO"
}

/// The content of one tab. The Pivot calls this function inside its own
/// `Build`.
let tabContent(env: ShellEnv, tabId: string) : Blob =
  match tabId with
  | "hub" -> HubPage.mountHub env.Hub
  | "type" -> TypePage.view()
  | "input" -> Cell.Mount<Cell>("input-page", env.InputPage)
  | "collections" -> Cell.Mount<Cell>("collections-page", env.CollectionsPage)
  | "navigate" -> Cell.Mount<Cell>("navigate-page", env.NavigatePage)
  | "dialogs" -> Cell.Mount<Cell>("dialogs-page", env.DialogsPage)
  | "motion" -> Cell.Mount<Cell>("motion-page", env.MotionPage)
  | _ -> TypePage.view()

/// The content of the settings flyout: the three modes and one swatch for
/// each Metro accent.
let settingsContent(model: GalleryModel, dispatch: GalleryMsg -> unit) : Blob =
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
    let isSelected = model.AccentKind = kind

    let borderColor =
      if isSelected then theme.Palette.Foreground else accent.Base

    // One delegate for the click and for the two keys.
    let selectAction = Action(fun () -> dispatch(AccentChanged kind))

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
                        Pressed = ValueSome(model.Mode = MetroMode.Dark)
                  },
                  fun () -> dispatch(ModeChanged MetroMode.Dark)
                )
              ),
              MetroTilt.mount(
                "settings-mode-light",
                MetroButton.build(
                  {
                    MetroButton.defaults "Light" with
                        Pressed = ValueSome(model.Mode = MetroMode.Light)
                  },
                  fun () -> dispatch(ModeChanged MetroMode.Light)
                )
              ),
              MetroTilt.mount(
                "settings-mode-system",
                MetroButton.build(
                  {
                    MetroButton.defaults "System" with
                        Pressed = ValueSome(model.Mode = MetroMode.System)
                  },
                  fun () -> dispatch(ModeChanged MetroMode.System)
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

/// The app bar of the gallery.
let appBar(model: GalleryModel, dispatch: GalleryMsg -> unit) : Blob =
  MetroAppBar.build(
    {
      MetroAppBar.defaults primaryCommands with
          Secondary = secondaryCommands
          Expanded = model.AppBarExpanded
    },
    (fun id -> dispatch(CommandInvoked id)),
    fun () -> dispatch(AppBarExpandedChanged(not model.AppBarExpanded))
  )

/// The gallery page: the workspace with the Pivot, then the app bar, the
/// settings flyout, and the content dialog.
let page
  (env: ShellEnv, model: GalleryModel, dispatch: GalleryMsg -> unit)
  : Blob =
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
      .Children(MetroPivot.mount("gallery-pivot", pivotInput model, env.Pivot))

  let bar =
    Container(Key = "gallery-bar", FlexShrink = 0.0)
      .Children(appBar(model, dispatch))

  let flyout =
    MetroSettingsFlyout.mount(
      "gallery-settings-flyout",
      MetroSettingsFlyout.defaults("Settings", model.SettingsOpen),
      env.Factories.SettingsFlyout
    )

  let dialog =
    MetroContentDialog.mount(
      "gallery-content-dialog",
      {
        MetroContentDialog.defaults("Metro Content Dialog", model.DialogOpen) with
            PrimaryText = ValueSome "Confirm"
            SecondaryText = ValueSome "Cancel"
      },
      env.Factories.ContentDialog
    )

  Container(
    BasedOn = Metro.canvasStyle theme,
    FlexDirection = FlexDirection.Column,
    Width = Length.Percent 100.0,
    Height = Length.Percent 100.0,
    Overflow = Overflow.Hidden
  )
    .Children(workspace, bar, flyout, dialog)

/// Builds the tree of the shell from the latest model and dispatch pair. It
/// counts its builds and publishes the model for the factories that the cell
/// made before the Elmish loop started.
let view
  (env: ShellEnv, model: GalleryModel, dispatch: GalleryMsg -> unit)
  : Blob =
  env.Latest <- model
  env.Builds <- env.Builds + 1
  page(env, model, dispatch)

/// The Elmish program of the shell.
let program
  (env: ShellEnv)
  : Program<GalleryStart, GalleryModel, GalleryMsg, Blob> =
  Program.mkProgram
    init
    (fun msg model -> update(env, msg, model))
    (fun model dispatch -> view(env, model, dispatch))
  |> Program.withTermination (fun msg -> msg = Stop) ignore

/// Creates the gallery. <paramref name="getWindow"/> gives the host window
/// when it exists. <paramref name="start"/> is the first tab and the first
/// mode. The closure holds the state.
let create
  (getWindow: unit -> Window voption, start: GalleryStart)
  : GalleryCell =
  let store = initialStore()

  // The mounted cells, for the headless check of the gallery.
  let mutable pivotRef: MetroPivotCell voption = ValueNone
  let mutable selectionTabRef: SelectionTab.SelectionTabCell voption = ValueNone
  let mutable zoomTabRef: ZoomTab.ZoomTabCell voption = ValueNone
  let mutable inputsTabRef: InputsTab.InputsTabCell voption = ValueNone
  let mutable dateTimeTabRef: DateTimeTab.DateTimeTabCell voption = ValueNone
  let mutable overlaysTabRef: OverlaysTab.OverlaysTabCell voption = ValueNone

  let mutable notificationsTabRef: NotificationsTab.NotificationsTabCell voption =
    ValueNone

  let mutable navigationTabRef: NavigationTab.NavigationTabCell voption =
    ValueNone

  let mutable primitivesTabRef: PrimitivesTab.PrimitivesTabCell voption =
    ValueNone

  let mutable flyoutRef: MetroSettingsFlyoutCell voption = ValueNone
  let mutable dialogRef: MetroContentDialogCell voption = ValueNone

  // The mounted hub, for a rebuild of its sections.
  let mutable hubRef: MetroHubCell voption = ValueNone

  // The dispatch of the Elmish loop. The factories below close over it before
  // the cell exists, so the slot starts empty and `create` fills it.
  let mutable dispatchSlot: GalleryMsg -> unit = fun _ -> ()

  let dispatchMsg(msg: GalleryMsg) : unit = dispatchSlot msg

  // The environment, filled below. The callbacks made before it read it
  // through this slot.
  let mutable envSlot: ShellEnv voption = ValueNone

  let envOf() : ShellEnv = envSlot |> ValueOption.get

  let applyMode(mode: MetroMode) =
    getWindow() |> ValueOption.iter(fun window -> Metro.setMode(window, mode))

  let applyAccent(accent: MetroAccentKind) =
    getWindow()
    |> ValueOption.iter(fun window -> Metro.setAccent(window, accent))

  // The sections of the hub read the store, but an equal input does not
  // rebuild the hub. The messages that change these values rebuild the hub
  // here.
  let requestHubRebuild() =
    if (envOf().Latest.ActiveTab) = "hub" then
      hubRef |> ValueOption.iter(fun hub -> hub.Rebuild())

  let flyoutFactory() : MetroSettingsFlyoutCell =
    let cell =
      MetroSettingsFlyout.create(
        (fun () -> dispatchMsg(SettingsOpenChanged false)),
        fun () -> settingsContent(envOf().Latest, dispatchMsg)
      )

    flyoutRef <- ValueSome cell
    cell

  let dialogFactory() : MetroContentDialogCell =
    let cell =
      MetroContentDialog.create(
        (function
        | MetroContentDialogAction.Primary -> dispatchMsg(DialogResolved true)
        | MetroContentDialogAction.Secondary ->
          dispatchMsg(DialogResolved false)
        | MetroContentDialogAction.Dismiss ->
          dispatchMsg(DialogOpenChanged false)),
        fun () ->
          MetroText.body(
            "Full-width Metro band dialog presented via Goo.Portal with "
            + "modal focus trapping, 2 px accent borders, and zero radius."
          )
      )

    dialogRef <- ValueSome cell
    cell

  let factories: GalleryFactories = {
    ControlsTab = fun () -> ControlsTab.create store
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
    OverlaysTab =
      fun () ->
        let tab = OverlaysTab.create()
        overlaysTabRef <- ValueSome tab
        tab :> Cell
    NotificationsTab =
      fun () ->
        let tab = NotificationsTab.create()
        notificationsTabRef <- ValueSome tab
        tab :> Cell
    NavigationTab =
      fun () ->
        let tab = NavigationTab.create()
        navigationTabRef <- ValueSome tab
        tab :> Cell
    PrimitivesTab =
      fun () ->
        let tab = PrimitivesTab.create()
        primitivesTabRef <- ValueSome tab
        tab :> Cell
    ContinuumDemo = ContinuumDemo.create
    StaggerDemo = StaggerDemo.create
    LiveTile = MetroLiveTile.createFactory(fun () -> HubPage.tapLiveTile store)
    CycleTile =
      MetroCycleTile.createFactory(fun () -> HubPage.tapCycleTile store)
    FlipTile = MetroFlipTile.createFactory(fun () -> HubPage.tapFlipTile store)
    SettingsFlyout = flyoutFactory
    ContentDialog = dialogFactory
  }

  let callbacks: HubPage.HubCallbacks = {
    OpenTab = (fun id -> dispatchMsg(TabSelected id))
    OpenSettings = (fun () -> dispatchMsg(SettingsOpenChanged true))
    OpenDialog = (fun () -> dispatchMsg(DialogOpenChanged true))
  }

  let hubFactory() : MetroHubCell =
    let cell =
      HubPage.createHub(
        store,
        callbacks,
        (factories.LiveTile, factories.CycleTile, factories.FlipTile)
      )

    hubRef <- ValueSome cell
    cell

  let pivotFactory() : MetroPivotCell =
    let cell =
      MetroPivot.create(
        (fun id -> dispatchMsg(TabSelected id)),
        fun tabId -> tabContent(envOf(), tabId)
      )

    pivotRef <- ValueSome cell
    cell

  let inputPageFactory() : Cell =
    InputPage.create {
      Controls = factories.ControlsTab
      Inputs = factories.InputsTab
      DateTime = factories.DateTimeTab
      Selection = factories.SelectionTab
    }

  let collectionsPageFactory() : Cell =
    CollectionsPage.create {
      Lists = factories.ListsTab
      FlipView = factories.FlipViewTab
      Zoom = factories.ZoomTab
    }

  let navigatePageFactory() : Cell =
    NavigatePage.create {
      Navigation = factories.NavigationTab
      Primitives = factories.PrimitivesTab
    }

  let dialogsPageFactory() : Cell =
    DialogsPage.create {
      Overlays = factories.OverlaysTab
      Notifications = factories.NotificationsTab
    }

  let motionPageFactory() : Cell =
    MotionPage.create {
      Continuum = factories.ContinuumDemo
      Stagger = factories.StaggerDemo
    }

  let firstModel, _ = init start

  let env: ShellEnv = {
    Store = store
    ApplyMode = applyMode
    ApplyAccent = applyAccent
    RequestHubRebuild = requestHubRebuild
    Factories = factories
    Hub = hubFactory
    Pivot = pivotFactory
    InputPage = inputPageFactory
    CollectionsPage = collectionsPageFactory
    NavigatePage = navigatePageFactory
    DialogsPage = dialogsPageFactory
    MotionPage = motionPageFactory
    Latest = firstModel
    Builds = 0
  }

  envSlot <- ValueSome env

  let post(action: unit -> unit) : bool =
    getWindow()
    |> ValueOption.map(fun window -> window.Post(fun () -> action()))
    |> ValueOption.defaultValue false

  let cell = {
    new GalleryCell(post, Stop, program env, start) with
      override _.ActiveTab = (envOf().Latest.ActiveTab)
      override _.Mode = (envOf().Latest.Mode)
      override _.AccentKind = (envOf().Latest.AccentKind)
      override _.SettingsOpen = (envOf().Latest.SettingsOpen)
      override _.DialogOpen = (envOf().Latest.DialogOpen)
      override _.Pivot = pivotRef
      override _.SelectionTab = selectionTabRef
      override _.ZoomTab = zoomTabRef
      override _.InputsTab = inputsTabRef
      override _.DateTimeTab = dateTimeTabRef
      override _.OverlaysTab = overlaysTabRef
      override _.NotificationsTab = notificationsTabRef
      override _.NavigationTab = navigationTabRef
      override _.PrimitivesTab = primitivesTabRef
      override _.SettingsFlyout = flyoutRef
      override _.ContentDialog = dialogRef
      override _.Builds = (envOf().Builds)

      override _.SelectTab(id) = dispatchMsg(TabSelected id)

      override _.SetMode(mode) = dispatchMsg(ModeChanged mode)

      override _.SetAccent(accent) = dispatchMsg(AccentChanged accent)

      override _.SetSettingsOpen(isOpen) =
        dispatchMsg(SettingsOpenChanged isOpen)

      override _.SetDialogOpen(isOpen) = dispatchMsg(DialogOpenChanged isOpen)
  }

  dispatchSlot <- (fun msg -> cell.Dispatch msg |> ignore)
  cell
