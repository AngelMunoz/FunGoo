module MetroGallery.Program

open System
open System.Collections
open System.Collections.Generic
open System.Reflection
open Goo
open FunGoo.Children
open FunGoo.Metro

type private HeadlessCheckHost() =
  inherit EmbeddedWindowHost()

  override _.RequestFrame() = ()

  override _.LoadVulkanLibrary() =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

  override _.GetVulkanGetInstanceProcAddr() =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

  override _.UnloadVulkanLibrary() =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

  override _.GetVulkanInstanceExtensions() =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

  override _.CreateVulkanSurface
    (_instance: nativeint, _surface: byref<uint64>)
    =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

  override _.DestroyVulkanSurface(_instance: nativeint, _surface: uint64) =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

type private GalleryStore = {
  mutable TileTapCount: int
  mutable LastAction: string
  mutable SliderValue: float
  mutable TextValue: string
  mutable SecretValue: string
  mutable ToggleEnabled: bool
  mutable CheckAccepted: bool
  mutable RadioChoice: string
  mutable TogglePressed: bool
}

[<Sealed>]
type private ContinuumDemoCell() as this =
  inherit Cell()

  let translateAnim = this.AnimateValue(Point(X = 0.0, Y = 0.0))
  let opacityAnim = this.AnimateValue(1.0)
  let mutable status = "Ready"

  let playEnter(theme: MetroTheme) =
    Continuum.playEnter(
      theme,
      translateAnim,
      opacityAnim,
      Continuum.defaultEnterDelta
    )

    status <- "Entered (130 px, -70 px -> 0, 0)"

  let playExit(theme: MetroTheme) =
    Continuum.playExit(
      theme,
      translateAnim,
      opacityAnim,
      Continuum.defaultExitDelta
    )

    status <- "Exited (0, 0 -> 225 px, 73 px)"

  let reset() =
    translateAnim.Snap(Point(X = 0.0, Y = 0.0))
    opacityAnim.Snap(1.0)
    status <- "Reset"

  override _.Build() : Blob =
    let theme = Metro.read()

    let flightTarget =
      Container(
        Width = Length.Pixels 260.0,
        Padding = Edges(Length.Pixels 16.0),
        BackgroundColor = theme.Accent.Base,
        Color = theme.Accent.OnAccent,
        BorderRadius = Length.Pixels 0.0,
        BoxShadows = Array.empty,
        TranslateAnim = translateAnim,
        OpacityAnim = opacityAnim
      )
        .Children(
          MetroText.build {
            MetroText.defaults("Shared Element", MetroTextRole.Subtitle) with
                Tone = MetroTextTone.OnAccent
          },
          MetroText.build {
            MetroText.defaults(
              "Continuum exponential ease-out",
              MetroTextRole.Caption
            ) with
                Tone = MetroTextTone.OnAccent
          }
        )

    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
      .Children(
        MetroText.header "continuum flight",
        MetroText.secondary status,
        Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 8.0)
          .Children(
            MetroButton.accent("Play Enter", fun () -> playEnter theme),
            MetroButton.standard("Play Exit", fun () -> playExit theme),
            MetroButton.subtle("Reset", reset)
          ),
        Container(
          Height = Length.Pixels 104.0,
          AlignItems = AlignItems.Center,
          Overflow = Overflow.Hidden
        )
          .Children(flightTarget)
      )

[<Sealed>]
type private StaggerDemoCell() as this =
  inherit Cell()

  let labels = [|
    "01  Typography-first hierarchy"
    "02  Content over chrome"
    "03  Sub-linear entrance stagger"
    "04  Direct paint-bound motion"
    "05  Zero-radius sharp geometry"
  |]

  let translates =
    Array.init labels.Length (fun _ ->
      this.AnimateValue(Point(X = 0.0, Y = 0.0)))

  let opacities = Array.init labels.Length (fun _ -> this.AnimateValue(1.0))

  let replay(theme: MetroTheme) =
    for i = 0 to labels.Length - 1 do
      if theme.ReducedMotion then
        translates[i].Snap(Point(X = 0.0, Y = 0.0))
        opacities[i].Snap(1.0)
      else
        let spec = MetroMotion.staggered(i, MetroMotion.normal)

        translates[i]
          .Play(Point(X = 40.0, Y = 0.0), Point(X = 0.0, Y = 0.0), spec)

        opacities[i].Play(0.0, 1.0, spec)

  override _.Build() : Blob =
    let theme = Metro.read()

    let list =
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)

    for i = 0 to labels.Length - 1 do
      let delayMs = MetroMotion.staggerDelayMs i

      let row =
        Container(
          Key = $"stagger-{i}",
          FlexDirection = FlexDirection.Row,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.SpaceBetween,
          Padding = Edges(Length.Pixels 10.0, Length.Pixels 14.0),
          BackgroundColor = theme.Palette.BackgroundAlt,
          BorderWidth =
            Edges(
              Length.Pixels 0.0,
              Length.Pixels 0.0,
              Length.Pixels 0.0,
              Length.Pixels 4.0
            ),
          BorderColor = Edges<Color>(theme.Accent.Base),
          TranslateAnim = translates[i],
          OpacityAnim = opacities[i]
        )
          .Children(
            MetroText.body labels[i],
            MetroText.caption $"+{int delayMs} ms"
          )

      list.Add(row)

    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
      .Children(
        MetroText.header "staggered entrance",
        Container(FlexDirection = FlexDirection.Row)
          .Children(
            MetroButton.standard("Replay Stagger", fun () -> replay theme)
          ),
        list
      )

[<Sealed>]
type private ControlsTabCell(store: GalleryStore) as this =
  inherit Cell()

  let sliderOnChange =
    Action<float>(fun v ->
      store.SliderValue <- v
      this.Rebuild())

  override _.Build() : Blob =
    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      OverflowY = Overflow.Scroll,
      OverflowX = Overflow.Hidden,
      Gap = Length.Pixels 24.0,
      Padding =
        Edges(
          Length.Pixels 4.0,
          Length.Pixels 12.0,
          Length.Pixels 24.0,
          Length.Pixels 0.0
        )
    )
      .Children(
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 10.0
        )
          .Children(
            MetroText.header "buttons",
            Container(
              FlexDirection = FlexDirection.Row,
              FlexWrap = FlexWrap.Wrap,
              Gap = Length.Pixels 12.0,
              AlignItems = AlignItems.Center
            )
              .Children(
                MetroButton.standard(
                  "Standard",
                  fun () -> store.LastAction <- "Standard clicked"
                ),
                MetroButton.accent(
                  "Accent",
                  fun () -> store.LastAction <- "Accent clicked"
                ),
                MetroButton.hyperlink(
                  "Hyperlink",
                  fun () -> store.LastAction <- "Hyperlink clicked"
                ),
                MetroButton.subtle(
                  "Subtle",
                  fun () -> store.LastAction <- "Subtle clicked"
                ),
                MetroButton.build(
                  {
                    MetroButton.defaults(
                      if store.TogglePressed then "Pinned" else "Pin"
                    ) with
                        Pressed = ValueSome store.TogglePressed
                  },
                  fun () -> store.TogglePressed <- not store.TogglePressed
                ),
                MetroButton.build(
                  {
                    MetroButton.defaults "Disabled" with
                        Disabled = true
                  },
                  ignore
                )
              )
          ),
        Container(
          FlexDirection = FlexDirection.Row,
          FlexWrap = FlexWrap.Wrap,
          Gap = Length.Pixels 24.0
        )
          .Children(
            MetroTextBox.build(
              {
                MetroTextBox.defaults store.TextValue with
                    Label = ValueSome "Display Name"
                    Placeholder = ValueSome "Type here"
                    Width = ValueSome(Length.Pixels 280.0)
              },
              (fun v -> store.TextValue <- v),
              ValueSome(fun v -> store.LastAction <- $"Submitted '{v}'")
            ),
            MetroTextBox.build(
              {
                MetroTextBox.defaults store.SecretValue with
                    Label = ValueSome "Passcode"
                    Password = true
                    RevealButton = true
                    Width = ValueSome(Length.Pixels 280.0)
              },
              (fun v -> store.SecretValue <- v),
              ValueNone
            )
          ),
        Container(
          FlexDirection = FlexDirection.Row,
          FlexWrap = FlexWrap.Wrap,
          Gap = Length.Pixels 32.0,
          AlignItems = AlignItems.FlexStart
        )
          .Children(
            MetroToggleSwitch.build(
              {
                MetroToggleSwitch.defaults store.ToggleEnabled with
                    Header = ValueSome "Progress Ring"
              },
              fun v -> store.ToggleEnabled <- v
            ),
            MetroCheckBox.build(
              {
                Label = "Enable sharp borders"
                Checked = store.CheckAccepted
                Disabled = false
              },
              fun v -> store.CheckAccepted <- v
            ),
            Container(
              FlexDirection = FlexDirection.Column,
              Gap = Length.Pixels 6.0
            )
              .Children(
                MetroText.caption "DENSITY",
                MetroRadioButton.build(
                  {
                    Label = "Compact"
                    Selected = store.RadioChoice = "compact"
                    Disabled = false
                  },
                  fun () -> store.RadioChoice <- "compact"
                ),
                MetroRadioButton.build(
                  {
                    Label = "Comfortable"
                    Selected = store.RadioChoice = "comfortable"
                    Disabled = false
                  },
                  fun () -> store.RadioChoice <- "comfortable"
                ),
                MetroRadioButton.build(
                  {
                    Label = "Touch"
                    Selected = store.RadioChoice = "touch"
                    Disabled = false
                  },
                  fun () -> store.RadioChoice <- "touch"
                )
              )
          ),
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 12.0,
          MaxWidth = Length.Pixels 420.0
        )
          .Children(
            MetroText.header "slider & progress",
            Container()
              .Children(
                MetroSlider.mount(
                  "gallery-slider",
                  {
                    MetroSlider.defaults(store.SliderValue, 0.0, 100.0) with
                        Label =
                          ValueSome $"Completion ({int store.SliderValue}%%)"
                        Step = 5.0
                  },
                  sliderOnChange
                )
              ),
            MetroText.caption "DETERMINATE",
            Container()
              .Children(
                MetroProgressBar.build(
                  "gallery-det-progress",
                  MetroProgressBar.determinate(store.SliderValue / 100.0)
                )
              ),
            MetroText.caption "INDETERMINATE",
            Container()
              .Children(
                MetroProgressBar.build(
                  "gallery-ind-progress",
                  MetroProgressBar.indeterminate
                )
              ),
            Container(
              FlexDirection = FlexDirection.Row,
              AlignItems = AlignItems.Center,
              Gap = Length.Pixels 16.0
            )
              .Children(
                Container()
                  .Children(
                    MetroProgressRing.mount(
                      "gallery-ring",
                      MetroProgressRing.defaults store.ToggleEnabled
                    )
                  ),
                MetroText.secondary(
                  if store.ToggleEnabled then "Ring active" else "Ring paused"
                )
              )
          )
      )

[<Sealed>]
type private GalleryCell
  (getWindow: unit -> Window voption, initialState: (string * MetroMode) voption) as this
  =
  inherit Cell()

  let store: GalleryStore = {
    TileTapCount = 0
    LastAction = "Ready"
    SliderValue = 65.0
    TextValue = "Modern UI"
    SecretValue = "metro123"
    ToggleEnabled = true
    CheckAccepted = true
    RadioChoice = "compact"
    TogglePressed = false
  }

  let mutable activeTab =
    initialState |> ValueOption.map fst |> ValueOption.defaultValue "overview"

  let mutable mode =
    initialState
    |> ValueOption.map snd
    |> ValueOption.defaultValue MetroMode.Dark

  let mutable accentKind = MetroAccentKind.Blue
  let mutable appBarExpanded = false
  let mutable settingsOpen = false
  let mutable dialogOpen = false
  let mutable hubCellRef: MetroHubCell voption = ValueNone

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
    { Id = "zoom"; Header = "zoom" }
    {
      Id = "number-rating"
      Header = "number"
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

  let tileGridSizes = [|
    MetroTileSize.Wide
    MetroTileSize.Medium
    MetroTileSize.Small
    MetroTileSize.Small
    MetroTileSize.Small
    MetroTileSize.Small
    MetroTileSize.Wide
  |]

  let tileGridLayout = TileGridLayout(72.0, 8.0, 4, tileGridSizes)

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

  let controlsTabFactory = Func<Cell>(fun () -> ControlsTabCell(store) :> Cell)
  let flipViewTabFactory = fun () -> FlipViewTab.FlipViewTabCell() :> Cell
  let listsTabFactory = fun () -> ListsTab.ListsTabCell() :> Cell
  let zoomTabFactory = fun () -> ZoomTab.ZoomTabCell() :> Cell

  let numberRatingTabFactory =
    fun () -> NumberRatingTab.NumberRatingTabCell() :> Cell

  let continuumFactory = Func<Cell>(fun () -> ContinuumDemoCell() :> Cell)
  let staggerFactory = Func<Cell>(fun () -> StaggerDemoCell() :> Cell)

  let rebuildHub() =
    if activeTab = "overview" then
      hubCellRef |> ValueOption.iter(fun hub -> hub.Rebuild())

  let applyMode(nextMode: MetroMode) =
    mode <- nextMode
    getWindow() |> ValueOption.iter(fun w -> Metro.setMode(w, nextMode))

  let applyAccent(nextAccent: MetroAccentKind) =
    accentKind <- nextAccent
    getWindow() |> ValueOption.iter(fun w -> Metro.setAccent(w, nextAccent))

  let toggleTheme() =
    let next =
      match mode with
      | MetroMode.Dark -> MetroMode.Light
      | MetroMode.Light -> MetroMode.Dark
      | MetroMode.System -> MetroMode.Dark

    applyMode next
    store.LastAction <- $"Mode -> {next}"
    rebuildHub()

  let handleAppBarCommand(id: string) =
    match id with
    | "theme" -> toggleTheme()
    | "settings" ->
      settingsOpen <- true
      appBarExpanded <- false
    | "dialog"
    | "about" ->
      dialogOpen <- true
      appBarExpanded <- false
    | "reset" ->
      activeTab <- "overview"
      store.TileTapCount <- 0
      store.LastAction <- "State reset"
      store.SliderValue <- 65.0
      store.TextValue <- "Modern UI"
      store.SecretValue <- "metro123"
      store.ToggleEnabled <- true
      store.CheckAccepted <- true
      store.RadioChoice <- "compact"
      store.TogglePressed <- false
      appBarExpanded <- false
      rebuildHub()
    | _ -> ()

  let liveTileFactory =
    MetroLiveTile.createFactory(fun () ->
      store.TileTapCount <- store.TileTapCount + 1
      store.LastAction <- $"Live tile tapped ({store.TileTapCount})"
      rebuildHub())

  let renderHubSection(sectionId: string) : Blob =
    match sectionId with
    | "start" ->
      Container(Layout = tileGridLayout)
        .Children(
          Container()
            .Children(
              MetroLiveTile.mount(
                "gallery-live-tile",
                MetroLiveTile.defaults liveFaces,
                liveTileFactory
              )
            ),
          MetroIconicTile.build(
            {
              MetroIconicTile.defaults("Mail", "mail") with
                  Count = ValueSome(12 + store.TileTapCount)
                  Size = MetroTileSize.Medium
            },
            fun () ->
              store.TileTapCount <- store.TileTapCount + 1
              store.LastAction <- "Opened Mail tile"
          ),
          MetroIconicTile.build(
            {
              MetroIconicTile.defaults("Calendar", "calendar_today") with
                  Size = MetroTileSize.Small
                  AccentOverride = ValueSome(Color.Rgb(0, 163, 0))
            },
            fun () ->
              store.TileTapCount <- store.TileTapCount + 1
              store.LastAction <- "Opened Calendar"
          ),
          MetroIconicTile.build(
            {
              MetroIconicTile.defaults("Music", "music_note") with
                  Size = MetroTileSize.Small
                  AccentOverride = ValueSome(Color.Rgb(250, 104, 0))
            },
            fun () ->
              store.TileTapCount <- store.TileTapCount + 1
              store.LastAction <- "Opened Music"
          ),
          MetroIconicTile.build(
            {
              MetroIconicTile.defaults("Photos", "photo") with
                  Size = MetroTileSize.Small
                  AccentOverride = ValueSome(Color.Rgb(162, 0, 255))
            },
            fun () ->
              store.TileTapCount <- store.TileTapCount + 1
              store.LastAction <- "Opened Photos"
          ),
          MetroIconicTile.build(
            {
              MetroIconicTile.defaults("Settings", "settings") with
                  Size = MetroTileSize.Small
                  AccentOverride = ValueSome(Color.Rgb(100, 118, 135))
            },
            fun () ->
              settingsOpen <- true
              this.Rebuild()
          ),
          MetroIconicTile.build(
            {
              MetroIconicTile.defaults("Explore Controls", "tune") with
                  Size = MetroTileSize.Wide
                  AccentOverride = ValueSome(Color.Rgb(0, 80, 239))
            },
            fun () ->
              activeTab <- "controls"
              this.Rebuild()
          )
        )
    | "status" ->
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
        .Children(
          MetroText.subtitle "Glance and go",
          MetroText.secondary
            $"Last action: {store.LastAction}. Tile taps: {store.TileTapCount}.",
          MetroText.caption "LIVE PROGRESS",
          Container()
            .Children(
              MetroProgressBar.build(
                "hub-det-bar",
                MetroProgressBar.determinate(store.SliderValue / 100.0)
              )
            ),
          Container(
            FlexDirection = FlexDirection.Row,
            Gap = Length.Pixels 12.0,
            AlignItems = AlignItems.Center
          )
            .Children(
              MetroButton.accent(
                "Customize Theme",
                fun () ->
                  settingsOpen <- true
                  this.Rebuild()
              ),
              MetroButton.standard(
                "Show Dialog",
                fun () ->
                  dialogOpen <- true
                  this.Rebuild()
              )
            )
        )
    | _ ->
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

  let hubFactory =
    Func<MetroHubCell>(fun () ->
      let cell = MetroHubCell(renderHubSection)
      hubCellRef <- ValueSome cell
      cell)

  let renderTypographyTab() : Blob =
    let samples = [|
      MetroTextRole.Hero, "Hero 56 px Light"
      MetroTextRole.Title, "Title 42 px Light"
      MetroTextRole.Header, "Header 28 px Light"
      MetroTextRole.Subheader, "Subheader 26 px Light"
      MetroTextRole.Header2, "Header2 26 px Regular"
      MetroTextRole.Subtitle, "Subtitle 20 px Light"
      MetroTextRole.Medium, "Medium 16 px Regular — lead copy"
      MetroTextRole.Body, "Body 14 px Regular — primary reading text"
      MetroTextRole.Caption, "Caption 12 px Regular — secondary metadata"
      MetroTextRole.Badge, "Badge 11 px Bold Uppercase"
    |]

    let col =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        OverflowY = Overflow.Scroll,
        OverflowX = Overflow.Hidden,
        Gap = Length.Pixels 14.0,
        Padding =
          Edges(
            Length.Pixels 4.0,
            Length.Pixels 12.0,
            Length.Pixels 24.0,
            Length.Pixels 0.0
          )
      )

    for i = 0 to samples.Length - 1 do
      let role, sampleText = samples[i]
      let spec = MetroTypography.spec role

      let row =
        Container(
          Key = $"typo-{i}",
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 2.0
        )
          .Children(
            MetroText.caption
              $"{role} — {int spec.FontSize} px, weight {spec.FontWeight}",
            MetroText.build(MetroText.defaults(sampleText, role))
          )

      col.Add(row)

    col

  let renderMotionTab() : Blob =
    let shaderStatus =
      if MetroPerspective.isEmbeddedShaderAvailable then
        "Compiled Slang 3D perspective shader embedded"
      else
        "2D Fallback Active (compiled .goo-effect not embedded)"

    let struct (turnTransform, turnAlpha) =
      MetroFallbackMotion.turnstile2D(0.5, true)

    let struct (swivelTransform, swivelAlpha) =
      MetroFallbackMotion.swivel2D(0.5, true)

    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      OverflowY = Overflow.Scroll,
      OverflowX = Overflow.Hidden,
      Gap = Length.Pixels 24.0,
      Padding =
        Edges(
          Length.Pixels 4.0,
          Length.Pixels 12.0,
          Length.Pixels 24.0,
          Length.Pixels 0.0
        )
    )
      .Children(
        Container()
          .Children(Cell.Mount<Cell>("continuum-demo", continuumFactory)),
        Container().Children(Cell.Mount<Cell>("stagger-demo", staggerFactory)),
        Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 6.0)
          .Children(
            MetroText.header "perspective shader",
            MetroText.body shaderStatus,
            MetroText.secondary(
              $"Turnstile 2D: scaleX={turnTransform.ScaleX:F2}, "
              + $"opacity={turnAlpha:F2}"
            ),
            MetroText.secondary(
              $"Swivel 2D: scaleY={swivelTransform.ScaleY:F2}, "
              + $"opacity={swivelAlpha:F2}"
            )
          )
      )

  let renderPivotContent(tabId: string) : Blob =
    match tabId with
    | "overview" ->
      MetroHub.mount(
        "gallery-hub",
        MetroHub.defaults("metro gallery", hubSections),
        hubFactory
      )
    | "typography" -> renderTypographyTab()
    | "controls" -> Cell.Mount<Cell>("controls-tab", controlsTabFactory)
    | "flip-view" -> Cell.Mount<Cell>("flip-view-tab", flipViewTabFactory)
    | "lists" -> Cell.Mount<Cell>("lists-tab", listsTabFactory)
    | "zoom" -> Cell.Mount<Cell>("zoom-tab", zoomTabFactory)
    | "number-rating" ->
      Cell.Mount<Cell>("number-rating-tab", numberRatingTabFactory)
    | "motion" -> renderMotionTab()
    | _ -> renderTypographyTab()

  let pivotFactory =
    MetroPivot.createFactory(
      (fun id ->
        activeTab <- id
        this.Rebuild()),
      renderPivotContent
    )

  let renderSettingsContent() : Blob =
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
      let isSelected = accentKind = kind

      let borderColor =
        if isSelected then theme.Palette.Foreground else accent.Base

      let selectAction = Action(fun () -> applyAccent kind)

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
            Container(
              FlexDirection = FlexDirection.Row,
              Gap = Length.Pixels 8.0
            )
              .Children(
                MetroButton.build(
                  {
                    MetroButton.defaults "Dark" with
                        Pressed = ValueSome(mode = MetroMode.Dark)
                  },
                  fun () -> applyMode MetroMode.Dark
                ),
                MetroButton.build(
                  {
                    MetroButton.defaults "Light" with
                        Pressed = ValueSome(mode = MetroMode.Light)
                  },
                  fun () -> applyMode MetroMode.Light
                ),
                MetroButton.build(
                  {
                    MetroButton.defaults "System" with
                        Pressed = ValueSome(mode = MetroMode.System)
                  },
                  fun () -> applyMode MetroMode.System
                )
              )
          ),
        Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
          .Children(
            MetroText.caption $"ACCENT ({accents.Count} METRO COLORS)",
            swatchGrid
          )
      )

  member _.ActiveTab = activeTab
  member _.Mode = mode
  member _.AccentKind = accentKind
  member _.SettingsOpen = settingsOpen
  member _.DialogOpen = dialogOpen

  member this.SelectTab(id: string) =
    activeTab <- id
    this.Rebuild()

  member this.SetMode(nextMode: MetroMode) =
    applyMode nextMode
    this.Rebuild()

  member this.SetAccent(nextAccent: MetroAccentKind) =
    applyAccent nextAccent
    this.Rebuild()

  member this.SetSettingsOpen(isOpen: bool) =
    settingsOpen <- isOpen
    this.Rebuild()

  member this.SetDialogOpen(isOpen: bool) =
    dialogOpen <- isOpen
    this.Rebuild()

  override _.Build() : Blob =
    let theme = Metro.read()

    let workspace =
      Container(
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
          MetroPivot.mount(
            "gallery-pivot",
            {
              MetroPivot.defaults(pivotItems, activeTab) with
                  Title = ValueSome "FUNGOO METRO"
            },
            pivotFactory
          )
        )

    let appBar =
      MetroAppBar.build(
        {
          MetroAppBar.defaults primaryCommands with
              Secondary = secondaryCommands
              Expanded = appBarExpanded
        },
        handleAppBarCommand,
        fun () -> appBarExpanded <- not appBarExpanded
      )

    let settingsFlyout =
      MetroSettingsFlyout.build(
        MetroSettingsFlyout.defaults("Settings", settingsOpen),
        (fun () -> settingsOpen <- false),
        renderSettingsContent
      )

    let contentDialog =
      MetroContentDialog.build(
        {
          MetroContentDialog.defaults("Metro Content Dialog", dialogOpen) with
              PrimaryText = ValueSome "Confirm"
              SecondaryText = ValueSome "Cancel"
        },
        (fun () ->
          store.LastAction <- "Dialog confirmed"
          dialogOpen <- false
          rebuildHub()),
        (fun () ->
          store.LastAction <- "Dialog cancelled"
          dialogOpen <- false
          rebuildHub()),
        (fun () -> dialogOpen <- false),
        fun () ->
          MetroText.body(
            "Full-width Metro band dialog presented via Goo.Portal with "
            + "modal focus trapping, 2 px accent borders, and zero radius."
          )
      )

    Container(
      BasedOn = Metro.canvasStyle theme,
      FlexDirection = FlexDirection.Column,
      Width = Length.Percent 100.0,
      Height = Length.Percent 100.0,
      Overflow = Overflow.Hidden
    )
      .Children(workspace, appBar, settingsFlyout, contentDialog)

let private findNodeByKey(window: Window, key: string) : obj voption =
  let flags =
    BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

  let rootNode = typeof<Window>.GetField("node", flags).GetValue(window)

  let rec visit(node: obj) : obj voption =
    if isNull node then
      ValueNone
    else
      let nt = node.GetType()

      let nodeKey =
        nt.GetField("<Key>k__BackingField", flags).GetValue(node) :?> string

      if nodeKey = key then
        ValueSome node
      else
        let children =
          nt.GetField("<Children>k__BackingField", flags).GetValue(node)
          :?> IList

        let mutable found = ValueNone

        if not(isNull children) then
          for i = 0 to children.Count - 1 do
            if found.IsNone then
              found <- visit children[i]

        found

  visit rootNode

/// Gallery tabs that also register as named `goo render` scenes, in name order.
let private sceneTabs = [|
  "controls"
  "flip-view"
  "lists"
  "motion"
  "number-rating"
  "overview"
  "typography"
  "zoom"
|]

/// Maps the window theme preference onto a Metro appearance mode.
let private sceneMode(window: Window) : MetroMode =
  if window.Preferences.Theme = SystemTheme.Dark then
    MetroMode.Dark
  else
    MetroMode.Light

/// Named gallery states for `goo render --scene <name>`. A scene starts on its
/// tab and follows the `--theme` preference in the Metro palette and the
/// gallery mode state.
let private galleryScenes(getWindow: unit -> Window voption) =
  let scenes = Dictionary<string, Func<Cell>>()

  for tab in sceneTabs do
    scenes.[tab] <-
      fun () ->
        let mutable mode = MetroMode.Dark

        getWindow()
        |> ValueOption.iter(fun w ->
          mode <- sceneMode w
          Metro.setMode(w, mode))

        GalleryCell(getWindow, ValueSome(tab, mode)) :> Cell

  scenes

let private runChecks() : int =
  let mutable windowSlot: Window voption = ValueNone
  let gallery = GalleryCell((fun () -> windowSlot), ValueNone)
  use host = new HeadlessCheckHost()
  host.Resize(1280, 800, 1280, 800)

  let window =
    Window(
      Title = "Metro Gallery Check",
      Root = gallery,
      Scenes = galleryScenes(fun () -> windowSlot)
    )

  windowSlot <- ValueSome window

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  window.Attach(host) |> ignore

  let pump(n: int) =
    for _ = 1 to n do
      host.RenderFrame(0.016)

  pump 3

  // 0. Every gallery tab is registered as a named `goo render` scene.
  let registeredScenes =
    if isNull window.Scenes then
      Array.empty
    else
      window.Scenes.Keys |> Seq.sort |> Array.ofSeq

  if registeredScenes <> sceneTabs then
    failwith $"Expected scenes {sceneTabs}, got '{registeredScenes}'."

  // 1. Click a real Pivot tab header via retained Node OnClick to verify
  // cross-cell rebuild from MetroPivotCell -> GalleryCell.
  let flags =
    BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

  let typoTabNode =
    match findNodeByKey(window, "pivot-tab-typography") with
    | ValueSome n -> n
    | ValueNone -> failwith "Expected retained node 'pivot-tab-typography'."

  let onClick =
    typoTabNode.GetType().GetProperty("OnClick", flags).GetValue(typoTabNode)
    :?> Action

  if isNull onClick then
    failwith "Expected Pivot tab OnClick handler."

  onClick.Invoke()
  pump 2

  if gallery.ActiveTab <> "typography" then
    failwith
      $"Expected tab click to switch activeTab to 'typography', got '{gallery.ActiveTab}'."

  // 2. Exercise remaining Pivot tabs across two full roundtrips to ensure
  // unmounted tab cells remount cleanly with fresh instances.
  for tab in
    [|
      "controls"
      "flip-view"
      "lists"
      "zoom"
      "motion"
      "overview"
      "number-rating"
      "controls"
      "flip-view"
      "lists"
      "zoom"
      "typography"
      "motion"
      "overview"
      "number-rating"
    |] do
    gallery.SelectTab tab
    pump 2

    if gallery.ActiveTab <> tab then
      failwith $"Expected active tab '{tab}', got '{gallery.ActiveTab}'."

  // 3. Toggle Dark -> Light -> Dark and verify MetroTheme.
  gallery.SetMode MetroMode.Light
  pump 2

  if (Metro.readWindow window).IsDark then
    failwith "Expected Light mode after toggling theme."

  gallery.SetMode MetroMode.Dark
  pump 2
  let restoredTheme = Metro.readWindow window

  if not restoredTheme.IsDark then
    failwith "Expected Dark mode after restoring theme."

  // 4. Cycle accent swatches and verify theme accent updates.
  for kind in
    [| MetroAccentKind.Teal; MetroAccentKind.Magenta; MetroAccentKind.Blue |] do
    gallery.SetAccent kind
    pump 1
    let expected = MetroAccent.resolve(kind, ValueNone).Base
    let actual = (Metro.readWindow window).Accent.Base

    if actual <> expected then
      failwith $"Accent mismatch for {kind}."

  // 5. Open and dismiss SettingsFlyout.
  gallery.SetSettingsOpen true
  pump 2

  if not gallery.SettingsOpen then
    failwith "Expected SettingsFlyout to be open."

  gallery.SetSettingsOpen false
  pump 2

  // 6. Open and dismiss ContentDialog.
  gallery.SetDialogOpen true
  pump 2

  if not gallery.DialogOpen then
    failwith "Expected ContentDialog to be open."

  gallery.SetDialogOpen false
  pump 2

  printfn "MetroGallery checks passed."
  0

[<EntryPoint>]
let main(args: string array) : int =
  if args |> Array.exists(fun a -> a = "--check") then
    runChecks()
  else
    Application.Configure(
      "MetroGallery",
      "0.1.0",
      "io.github.fungoo.metrogallery"
    )

    let mutable windowSlot: Window voption = ValueNone
    let gallery = GalleryCell((fun () -> windowSlot), ValueNone)

    let window =
      Window(
        Title = "Metro Gallery",
        Size = LayoutSize(1200, 800),
        Root = gallery,
        Scenes = galleryScenes(fun () -> windowSlot)
      )

    windowSlot <- ValueSome window

    Metro.attach(
      window,
      {
        MetroConfig.defaultConfig with
            Mode = MetroMode.Dark
            Accent = MetroAccentKind.Blue
      }
    )

    Application.Run window
    0
