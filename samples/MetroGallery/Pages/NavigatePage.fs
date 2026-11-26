module MetroGallery.Pages.NavigatePage

open Goo
open Goo.Widgets.Navigation
open FunGoo.Children
open FunGoo.Metro

/// Demo data of the navigate page. Crumbs, tabs, panorama sections, pivot
/// items, spacing values, ramp roles, and bitmaps keep the pixel-equal
/// values of the retired navigation and primitives tabs.
module Content =

  let initialCrumbs: MetroBreadcrumbItem array = [|
    MetroBreadcrumbBar.iconItem("root", "Start", "home")
    MetroBreadcrumbBar.item("apps", "Collection")
    MetroBreadcrumbBar.item("media", "Music + Video")
    MetroBreadcrumbBar.item("now", "Now Playing")
  |]

  let initialTabs: MetroTabItem array = [|
    MetroTabView.pinnedItem("overview", "Overview")
    MetroTabView.iconItem("queue", "Play Queue", "queue_music")
    MetroTabView.item("lyrics", "Lyrics")
  |]

  let panoramaSections: MetroPanoramaSection array = [|
    MetroPanorama.sizedSection("featured", "featured", 240.0)
    MetroPanorama.sizedSection("recent", "recent", 240.0)
    MetroPanorama.sizedSection("artists", "artists", 240.0)
  |]

  let pivotItems: MetroPivotItem array = [|
    { Id = "stops"; Header = "stops" }
    { Id = "lines"; Header = "lines" }
  |]

  let pivotTextOf(id: string) : string =
    match id with
    | "stops" ->
      "The frame of this gallery is a Pivot. Headers peek, content glides."
    | _ -> "One Pivot nests here to show the control; pages live in the shell."

  let spacing: (string * float) array = [|
    "xs · 4", MetroSpacing.xs
    "sm · 8", MetroSpacing.sm
    "md · 12", MetroSpacing.md
    "lg · 16", MetroSpacing.lg
    "xl · 24", MetroSpacing.xl
    "xxl · 40", MetroSpacing.xxl
  |]

  let ramp: (string * MetroTextRole) array = [|
    "hero", MetroTextRole.Hero
    "title", MetroTextRole.Title
    "header", MetroTextRole.Header
    "subheader", MetroTextRole.Subheader
    "header2", MetroTextRole.Header2
    "subtitle", MetroTextRole.Subtitle
    "medium", MetroTextRole.Medium
    "body", MetroTextRole.Body
    "caption", MetroTextRole.Caption
    "badge", MetroTextRole.Badge
  |]

module Assets =

  // Deterministic 48x48 geometric Metro bitmap for MetroImage showcase.
  let sampleBitmap: ImageSource =
    let w = 48
    let h = 48
    let px = Array.zeroCreate<byte>(w * h * 4)

    for y = 0 to h - 1 do
      for x = 0 to w - 1 do
        let idx = (y * w + x) * 4
        let quad = ((x / 12) + (y / 12)) % 3

        let r, g, b =
          match quad with
          | 0 -> 0uy, 120uy, 212uy
          | 1 -> 0uy, 171uy, 169uy
          | _ -> 216uy, 0uy, 115uy

        px[idx] <- r
        px[idx + 1] <- g
        px[idx + 2] <- b
        px[idx + 3] <- 255uy

    new ImageSource(w, h, px)

  let gridSpans: MetroGridSpan array = [|
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
  |]

  let gridColors: (string * Color) array = [|
    "2x2", Color.Rgb(0, 120, 212)
    "2x1", Color.Rgb(0, 163, 0)
    "1x1", Color.Rgb(240, 150, 9)
    "1x1", Color.Rgb(162, 0, 255)
    "1x2", Color.Rgb(216, 0, 115)
    "2x1", Color.Rgb(0, 171, 169)
    "1x1", Color.Rgb(100, 118, 135)
  |]

/// State of the page. One mutable field per concern.
type NavigateState = {
  mutable Crumbs: MetroBreadcrumbItem array
  mutable Tabs: MetroTabItem array
  mutable ActiveSubTab: string
  mutable NextTabSeq: int
  mutable PivotSelected: string
  mutable SplitOpen: bool
  mutable SplitMode: MetroSplitViewMode
  mutable SplitPlacement: MetroSplitViewPlacement
  mutable PinChecked: bool
  mutable ShuffleChecked: bool
  mutable ExpanderOpen: bool
  mutable GridOrientation: MetroWrapOrientation
  mutable ViewboxStretch: MetroStretch
  mutable LastTabInput: TabBarInput
  mutable LastBreadcrumbInput: BreadcrumbInput
  mutable LastAction: string
  mutable Builds: int
}

type NavigateFactories = {
  Pivot: unit -> MetroPivotCell
  SplitView: unit -> MetroSplitViewCell
  Panorama: unit -> MetroPanoramaCell
  TabView: IMetroTabViewFactory
  Breadcrumb: IMetroBreadcrumbFactory
}

/// The navigate page. The headless check of the gallery reads the move and
/// the arrange state through these members.
[<AbstractClass>]
type NavigatePageCell() =
  inherit Cell()

  /// The mounted split view.
  abstract SplitView: MetroSplitViewCell voption

  /// The mounted panorama.
  abstract Panorama: MetroPanoramaCell voption

  /// Widget input of the tab view.
  abstract TabBarInput: TabBarInput

  /// Widget input of the breadcrumb bar.
  abstract BreadcrumbInput: BreadcrumbInput

  /// Identifier of the active document tab.
  abstract ActiveSubTab: string

  /// The number of open document tabs.
  abstract TabCount: int

  /// Text of the last action, for the headless check of the gallery.
  abstract LastAction: string

  /// <c>true</c> when the arrange expander is open.
  abstract ExpanderOpen: bool

  /// The packing flow of the variable grid.
  abstract GridOrientation: MetroWrapOrientation

  /// The stretch of both viewboxes.
  abstract ViewboxStretch: MetroStretch

  /// The mounted responsive viewbox.
  abstract ViewboxCell: MetroViewboxCell voption

  /// The number of builds, for the headless check of the gallery.
  abstract Builds: int

  /// Opens or closes the arrange expander.
  abstract SetExpanderOpen: expanded: bool -> unit

  /// Sets the packing flow of the variable grid.
  abstract SetGridOrientation: orientation: MetroWrapOrientation -> unit

  /// Sets the stretch of both viewboxes.
  abstract SetViewboxStretch: stretch: MetroStretch -> unit

/// One block of the page: a subheader in the type ramp, then the content.
let block(title: string, content: Blob) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.subheader title, content)

/// One column of a two-column flow: a subheader above the content.
let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 12.0
  )
    .Children(MetroText.subheader title, content)

let buildSplitPane(state: NavigateState, onSelectNav: string -> unit) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    Padding = Edges(Length.Pixels 8.0),
    Gap = Length.Pixels 6.0
  )
    .Children(
      MetroButton.subtle("H  Home", fun () -> onSelectNav "Home"),
      MetroButton.subtle("L  Library", fun () -> onSelectNav "Library"),
      MetroButton.subtle("R  Radio", fun () -> onSelectNav "Radio")
    )

let buildSplitContent(state: NavigateState) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    Padding = Edges(Length.Pixels 12.0),
    Gap = Length.Pixels 6.0
  )
    .Children(
      MetroText.subtitle $"Mode: {state.SplitMode}",
      MetroText.secondary
        $"Pane open: {state.SplitOpen} ({state.SplitPlacement})"
    )

let buildPanoramaBackground() : Blob =
  let theme = Metro.read()

  Container(
    FlexDirection = FlexDirection.Row,
    AlignItems = AlignItems.Center,
    Gap = Length.Pixels 24.0,
    Padding = Edges(Length.Pixels 16.0)
  )
    .Children(
      Container(
        Width = Length.Pixels 120.0,
        Height = Length.Pixels 120.0,
        BackgroundColor = theme.Accent.Base
      ),
      Container(
        Width = Length.Pixels 180.0,
        Height = Length.Pixels 120.0,
        BackgroundColor = theme.Accent.Light
      ),
      Container(
        Width = Length.Pixels 140.0,
        Height = Length.Pixels 120.0,
        BackgroundColor = theme.Accent.Dark
      )
    )
  :> Blob

let buildPanoramaSection(secId: string) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  let copy =
    match secId with
    | "featured" -> "Background 0.15x, Hero title 0.35x, section strip 1.0x."
    | "recent" -> "Scroll horizontally with wheel or touch with zero rebuilds."
    | _ -> "Peeking adjacent sections invite horizontal exploration."

  Container(
    Padding = Edges(Length.Pixels 12.0),
    BackgroundColor = p.BackgroundAlt
  )
    .Children(MetroText.body copy)
  :> Blob

/// The pivot demo of the move section: the shell frame at glance size.
let pivotBlock(state: NavigateState, factories: NavigateFactories) : Blob =
  block(
    "pivot",
    Container(
      FlexDirection = FlexDirection.Column,
      Width = Length.Percent 100.0,
      Height = Length.Pixels 240.0,
      FlexShrink = 0.0
    )
      .Children(
        MetroPivot.mount(
          "navigate-pivot-demo",
          MetroPivot.defaults(Content.pivotItems, state.PivotSelected),
          factories.Pivot
        )
      )
  )

/// The breadcrumb and app bar strip of the move section.
let stripBlock(state: NavigateState, factories: NavigateFactories) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  let crumbProps = MetroBreadcrumbBar.defaults state.Crumbs
  state.LastBreadcrumbInput <- factories.Breadcrumb.CreateInput crumbProps

  block(
    "breadcrumb & app bar",
    Container(
      Width = Length.Percent 100.0,
      FlexDirection = FlexDirection.Row,
      AlignItems = AlignItems.Center,
      JustifyContent = JustifyContent.SpaceBetween,
      Gap = Length.Pixels 16.0,
      Padding = Edges(Length.Pixels 8.0, Length.Pixels 12.0),
      BackgroundColor = p.BackgroundAlt
    )
      .Children(
        Container(FlexGrow = 1.0, MinWidth = Length.Pixels 0.0)
          .Children(
            factories.Breadcrumb.Mount("gallery-breadcrumb", crumbProps)
          ),
        Container(
          FlexDirection = FlexDirection.Row,
          AlignItems = AlignItems.Center,
          FlexShrink = 0.0,
          Gap = Length.Pixels 4.0
        )
          .Children(
            MetroAppBarButton.build(
              MetroAppBarButton.defaults("Play", "play_arrow"),
              fun () -> state.LastAction <- "AppBar: Play"
            ),
            MetroAppBarButton.build(
              MetroAppBarButton.compact("Skip", "skip_next"),
              fun () -> state.LastAction <- "AppBar: Skip"
            ),
            MetroAppBarSeparator.standard(),
            MetroAppBarToggleButton.build(
              MetroAppBarToggleButton.defaults(
                "Pin",
                "push_pin",
                state.PinChecked
              ),
              fun next ->
                state.PinChecked <- next
                state.LastAction <- $"Pin toggled: {next}"
            ),
            MetroAppBarToggleButton.build(
              MetroAppBarToggleButton.defaults(
                "Shuffle",
                "shuffle",
                state.ShuffleChecked
              ),
              fun next ->
                state.ShuffleChecked <- next
                state.LastAction <- $"Shuffle toggled: {next}"
            )
          )
      )
  )

/// The split view and tab view content of the move section.
let splitContent(state: NavigateState, factories: NavigateFactories) : Blob =
  let tabProps = {
    MetroTabView.defaults(state.Tabs, state.ActiveSubTab) with
        ShowAddButton = true
  }

  state.LastTabInput <- factories.TabView.CreateInput tabProps

  let splitInput = {
    MetroSplitView.defaults state.SplitOpen with
        DisplayMode = state.SplitMode
        PanePlacement = state.SplitPlacement
        OpenPaneLength = 180.0
        CompactPaneLength = 44.0
  }

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      Container(
        Key = "gallery-split-controls",
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.Wrap,
        Gap = Length.Pixels 8.0
      )
        .Children(
          MetroButton.accent(
            (if state.SplitOpen then "Close Pane" else "Open Pane"),
            fun () -> state.SplitOpen <- not state.SplitOpen
          ),
          MetroButton.standard(
            "Compact",
            fun () -> state.SplitMode <- MetroSplitViewMode.Compact
          ),
          MetroButton.standard(
            "Inline",
            fun () -> state.SplitMode <- MetroSplitViewMode.Inline
          ),
          MetroButton.standard(
            "Overlay",
            fun () -> state.SplitMode <- MetroSplitViewMode.Overlay
          ),
          MetroButton.standard(
            "Flip Edge",
            fun () ->
              state.SplitPlacement <-
                match state.SplitPlacement with
                | MetroSplitViewPlacement.Start -> MetroSplitViewPlacement.End
                | MetroSplitViewPlacement.End -> MetroSplitViewPlacement.Start
          )
        ),
      Container(
        Key = "gallery-split-frame",
        Width = Length.Percent 100.0,
        Height = Length.Pixels 164.0,
        BackgroundColor = (Metro.read()).Palette.BackgroundAlt
      )
        .Children(
          MetroSplitView.mount(
            "gallery-split-view",
            splitInput,
            factories.SplitView
          )
        ),
      factories.TabView.Build(
        "gallery-tab-view",
        tabProps,
        fun selectedId ->
          MetroText.body
            $"Active document tab: '{selectedId}' ({state.Tabs.Length} open tabs)."
      )
    )

/// The panorama content of the move section. It keeps the half-width flex
/// column of the retired tab, so the sections overflow the viewport and
/// the parallax demo stays scrollable.
let panoramaContent(factories: NavigateFactories) : Blob =
  let theme = Metro.read()

  Container(
    Width = Length.Percent 100.0,
    Height = Length.Pixels 296.0,
    BackgroundColor = theme.Palette.BackgroundAlt
  )
    .Children(
      MetroPanorama.mount(
        "gallery-panorama",
        MetroPanorama.defaults("zune media", Content.panoramaSections),
        factories.Panorama
      )
    )
  :> Blob

/// One spacing tile of the rules strip: accent bars at Metro spacing
/// widths, captioned in the caption role.
let spacingTile(theme: MetroTheme, width: float, height: float) : Blob =
  let bars =
    Container(
      FlexDirection = FlexDirection.Row,
      AlignItems = AlignItems.FlexEnd,
      Gap = Length.Pixels 12.0
    )

  for label, value in Content.spacing do
    bars.Add(
      Container(
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.FlexStart,
        Gap = Length.Pixels 4.0
      )
        .Children(
          Container(
            Width = Length.Pixels value,
            Height = Length.Pixels 12.0,
            BackgroundColor = theme.Accent.Base
          ),
          MetroText.caption label
        )
    )

  Container(
    Width = Length.Pixels width,
    Height = Length.Pixels height,
    FlexDirection = FlexDirection.Column,
    JustifyContent = JustifyContent.Center,
    Gap = Length.Pixels 8.0
  )
    .Children(MetroText.caption "SPACING SCALE · 4 PX BASE", bars)
  :> Blob

/// One ramp tile of the rules strip: one specimen in its ramp role,
/// captioned with its pixel size.
let rampTile(roleName: string, role: MetroTextRole) : Blob =
  let struct (cell, _) =
    MetroTileMetrics.pixelSize(104.0, 8.0, MetroTileSize.Small)

  let px = int (MetroTypography.spec role).FontSize

  Container(
    Width = Length.Pixels cell,
    Height = Length.Pixels cell,
    FlexDirection = FlexDirection.Column,
    AlignItems = AlignItems.Center,
    JustifyContent = JustifyContent.Center,
    Gap = Length.Pixels 4.0
  )
    .Children(
      MetroText.build(MetroText.defaults("Ag", role)),
      MetroText.caption $"{roleName} · {px}"
    )
  :> Blob

/// The rules strip that opens the arrange section. It shows the 8 px
/// spacing scale and the type ramp through MetroText and MetroTokens
/// values, lays the rule tiles out through TileGridLayout, and captions
/// each rule in ramp type. No decorative boxes.
let rulesStrip() : Blob =
  let theme = Metro.read()

  let struct (wideW, wideH) =
    MetroTileMetrics.pixelSize(104.0, 8.0, MetroTileSize.Wide)

  let grid =
    TileGridLayout.create {
      CellSize = 104.0
      Gap = 8.0
      Columns = 5
      Sizes =
        Array.append
          [| MetroTileSize.Wide |]
          (Array.create Content.ramp.Length MetroTileSize.Small)
    }

  let tiles = Container(Layout = grid)
  tiles.Add(spacingTile(theme, wideW, wideH))

  for roleName, role in Content.ramp do
    tiles.Add(rampTile(roleName, role))

  block(
    "metro rules",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary
          "Fierce reduction: whitespace and alignment instead of boxes. The \
           spacing scale steps in 4 px units; the ramp sets every title.",
        tiles :> Blob
      )
  )

let chip(label: string) : Blob =
  let theme = Metro.read()

  MetroBorder.build(
    {
      MetroBorder.defaults() with
          Thickness = Edges(Length.Pixels 1.0)
          BorderColor = ValueSome theme.Palette.Border
          Background = ValueSome theme.Palette.BackgroundAlt
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 10.0)
    },
    MetroText.caption label
  )

let chipRounded(label: string) : Blob =
  let theme = Metro.read()

  MetroBorder.build(
    {
      MetroBorder.defaults() with
          Thickness = Edges(Length.Pixels 1.0)
          BorderColor = ValueSome theme.Palette.Border
          Background = ValueSome theme.Palette.BackgroundAlt
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 10.0)
          CornerRadius = 8.0
    },
    MetroText.caption label
  )

// The natural 200x48 stamp that both viewboxes scale. A plain function, so
// the responsive viewbox cell can rebuild it inside its own build.
// Typography alone carries the demo: no border or fill wrapper.
let viewboxStamp() : Blob =
  Container(
    Width = Length.Pixels 200.0,
    Height = Length.Pixels 48.0,
    AlignItems = AlignItems.Center,
    JustifyContent = JustifyContent.Center
  )
    .Children(
      MetroText.build {
        MetroText.defaults("METRO VIEWBOX", MetroTextRole.Subtitle) with
            Wrap = TextWrap.NoWrap
      }
    )
  :> Blob

/// The primitives column of the arrange section.
let primitivesCol(state: NavigateState) : Blob =
  let iconAndAvatarRow =
    Container(
      FlexDirection = FlexDirection.Row,
      AlignItems = AlignItems.Center,
      FlexWrap = FlexWrap.Wrap,
      Gap = Length.Pixels 14.0
    )
      .Children(
        MetroIcon.build(MetroIcon.sized("explore", MetroIconSize.Small)),
        MetroIcon.build(MetroIcon.sized("bolt", MetroIconSize.Normal)),
        MetroIcon.build {
          MetroIcon.sized("verified", MetroIconSize.Large) with
              Tone = MetroTextTone.Accent
        },
        MetroIcon.build(MetroIcon.sized("auto_awesome", MetroIconSize.XLarge)),
        MetroPersonPicture.build {
          MetroPersonPicture.defaults "Ada Lovelace" with
              AvatarSize = MetroAvatarSize.Small
              Presence = MetroPresence.Available
        },
        MetroPersonPicture.build {
          MetroPersonPicture.defaults "Grace Hopper" with
              AvatarSize = MetroAvatarSize.Normal
              Presence = MetroPresence.Busy
              AccentOverride = ValueSome(Color.Rgb(216, 0, 115))
        },
        MetroPersonPicture.build {
          MetroPersonPicture.defaults "Alan Turing" with
              AvatarSize = MetroAvatarSize.Large
              Presence = MetroPresence.Away
              AccentOverride = ValueSome(Color.Rgb(0, 163, 0))
        },
        MetroImage.build {
          MetroImage.defaults(
            ValueSome(Assets.sampleBitmap :> ImageSourceProvider)
          ) with
              Width = ValueSome(Length.Pixels 64.0)
              Height = ValueSome(Length.Pixels 64.0)
              Bordered = true
              AccessibilityLabel = ValueSome "Metro mosaic sample"
        },
        MetroImage.build {
          MetroImage.defaults ValueNone with
              Width = ValueSome(Length.Pixels 64.0)
              Height = ValueSome(Length.Pixels 64.0)
              Bordered = true
              Placeholder = ValueSome(MetroIcon.named "image")
              Fallback = ValueSome(MetroIcon.named "broken_image")
        }
      )

  let richBlock =
    MetroRichTextBlock.build(
      MetroRichTextBlock.defaults [|
        MetroRichTextBlock.bold "Fierce reduction: "
        MetroRichTextBlock.plain "combine "
        MetroRichTextBlock.italic "expressive typography"
        MetroRichTextBlock.plain ", "
        MetroRichTextBlock.accent "accent links"
        MetroRichTextBlock.plain ", and inline "
        MetroRichTextBlock.code "TextSpan[]"
        MetroRichTextBlock.plain " runs in one layout pass."
      |]
    )

  let expander =
    MetroExpander.build(
      MetroExpander.defaults(
        "Metro Design Metadata (Expander)",
        state.ExpanderOpen
      ),
      (fun next ->
        state.ExpanderOpen <- next
        state.LastAction <- $"Expander expanded: {next}"),
      MetroText.secondary
        "Sharp 0 px disclosure container preserving mounted child state while collapsed."
    )

  column(
    "primitives & rich text",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
      .Children(
        MetroText.caption "ICONS, PERSON PICTURES, & IMAGES",
        iconAndAvatarRow,
        MetroText.caption "RICH TEXT BLOCK",
        richBlock,
        MetroText.caption "EXPANDER",
        expander
      )
  )

/// The layout column of the arrange section.
let layoutCol
  (state: NavigateState, viewboxFactory: unit -> MetroViewboxCell)
  : Blob =
  let gridItems =
    Assets.gridColors
    |> Array.mapi(fun idx (lbl, col) ->
      Container(
        Key = $"varg-tile-{idx}",
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        AlignItems = AlignItems.FlexStart,
        JustifyContent = JustifyContent.FlexEnd,
        Padding = Edges(Length.Pixels 8.0),
        BackgroundColor = col
      )
        .Children(
          MetroText.build {
            MetroText.defaults(lbl, MetroTextRole.Caption) with
                Tone = MetroTextTone.Custom(Color.Rgb(255, 255, 255))
          }
        )
      :> Blob)

  let gridArgs = {
    MetroVariableSizedWrapGrid.defaults(56.0, 48.0, Assets.gridSpans) with
        Orientation = state.GridOrientation
        Gap = 6.0
        MaximumRowsOrColumns =
          ValueSome(
            match state.GridOrientation with
            | MetroWrapOrientation.Horizontal -> 4
            | MetroWrapOrientation.Vertical -> 3
          )
  }

  let viewboxBox =
    MetroViewbox.build(
      {
        MetroViewbox.defaults(LayoutSize(260.0, 76.0), LayoutSize(200.0, 48.0)) with
            Stretch = state.ViewboxStretch
      },
      viewboxStamp()
    )

  let responsiveViewbox =
    MetroViewbox.mount(
      "navigate-viewbox-responsive",
      {
        MetroViewbox.inputDefaults(LayoutSize(200.0, 48.0)) with
            Stretch = state.ViewboxStretch
            Width = Length.Pixels 260.0
            Height = Length.Pixels 76.0
      },
      viewboxFactory
    )

  column(
    "variable grid, wrap, & viewbox",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
      .Children(
        Container(
          FlexDirection = FlexDirection.Row,
          FlexWrap = FlexWrap.Wrap,
          Gap = Length.Pixels 8.0
        )
          .Children(
            MetroButton.standard(
              $"Grid: {state.GridOrientation}",
              fun () ->
                state.GridOrientation <-
                  match state.GridOrientation with
                  | MetroWrapOrientation.Horizontal ->
                    MetroWrapOrientation.Vertical
                  | MetroWrapOrientation.Vertical ->
                    MetroWrapOrientation.Horizontal

                state.LastAction <- $"Grid flow: {state.GridOrientation}"
            ),
            MetroButton.standard(
              "Uniform",
              fun () ->
                state.ViewboxStretch <- MetroStretch.Uniform
                state.LastAction <- "Viewbox: Uniform"
            ),
            MetroButton.standard(
              "Fill",
              fun () ->
                state.ViewboxStretch <- MetroStretch.Fill
                state.LastAction <- "Viewbox: Fill"
            ),
            MetroButton.standard(
              "None",
              fun () ->
                state.ViewboxStretch <- MetroStretch.None
                state.LastAction <- "Viewbox: None"
            )
          ),
        MetroVariableSizedWrapGrid.build(gridArgs, gridItems),
        MetroText.caption "WRAP PANEL",
        MetroWrapPanel.build(
          MetroWrapPanel.defaults(),
          [|
            chip " SHARP 0 PX "
            chip " TYPOGRAPHY "
            chip " MOTION "
            chip " CONTENT OVER CHROME "
            chipRounded " ROUNDED 8 PX "
          |]
        ),
        MetroText.caption $"VIEWBOX ({state.ViewboxStretch})",
        Container(FlexDirection = FlexDirection.Row).Children(viewboxBox),
        MetroText.caption "VIEWBOX (RESPONSIVE)",
        Container(FlexDirection = FlexDirection.Row).Children(responsiveViewbox)
      )
  )

/// The tree of the page: the page title, the move section, then the
/// arrange section. The controls keep their sizes, order, and demo data.
let view
  (
    state: NavigateState,
    factories: NavigateFactories,
    viewboxFactory: unit -> MetroViewboxCell
  ) : Blob =
  state.Builds <- state.Builds + 1

  MetroScrollViewer.build(
    MetroScrollViewer.vertical(),
    Container(
      FlexDirection = FlexDirection.Column,
      Width = Length.Percent 100.0,
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
        MetroText.title "navigate",
        MetroText.header "move",
        pivotBlock(state, factories),
        stripBlock(state, factories),
        Container(
          FlexDirection = FlexDirection.Row,
          Width = Length.Percent 100.0,
          FlexShrink = 0.0,
          Gap = Length.Pixels 28.0
        )
          .Children(
            column("split view & tab view", splitContent(state, factories)),
            column("panorama parallax", panoramaContent factories)
          ),
        MetroText.header "arrange",
        rulesStrip(),
        Container(
          FlexDirection = FlexDirection.Row,
          Width = Length.Percent 100.0,
          FlexShrink = 0.0,
          Gap = Length.Pixels 32.0
        )
          .Children(primitivesCol state, layoutCol(state, viewboxFactory)),
        MetroText.caption state.LastAction
      )
  )

/// Creates the navigate page. The page owns the move and the arrange state.
let create() : NavigatePageCell =
  let state: NavigateState = {
    Crumbs = Content.initialCrumbs
    Tabs = Content.initialTabs
    ActiveSubTab = "overview"
    NextTabSeq = 4
    PivotSelected = "stops"
    SplitOpen = true
    SplitMode = MetroSplitViewMode.Compact
    SplitPlacement = MetroSplitViewPlacement.Start
    PinChecked = true
    ShuffleChecked = false
    ExpanderOpen = true
    GridOrientation = MetroWrapOrientation.Horizontal
    ViewboxStretch = MetroStretch.Uniform
    LastTabInput = Unchecked.defaultof<TabBarInput>
    LastBreadcrumbInput = Unchecked.defaultof<BreadcrumbInput>
    LastAction = "Ready"
    Builds = 0
  }

  let mutable self: NavigatePageCell voption = ValueNone
  let mutable splitRef: MetroSplitViewCell voption = ValueNone
  let mutable panoramaRef: MetroPanoramaCell voption = ValueNone
  let mutable viewboxRef: MetroViewboxCell voption = ValueNone

  // SplitView builds its pane inside its own Build, so selecting an item
  // inside the pane rebuilds this page explicitly via `self`.
  let onSelectSplitNav(name: string) =
    state.LastAction <- $"SplitView nav: {name}"
    self |> ValueOption.iter(fun page -> page.Rebuild())

  // Created once: every Build reuses the same factories.
  let factories: NavigateFactories = {
    Pivot =
      MetroPivot.createFactory(
        (fun id ->
          state.PivotSelected <- id
          state.LastAction <- $"Pivot: {id}"),
        fun tabId -> MetroText.body(Content.pivotTextOf tabId) :> Blob
      )
    SplitView =
      fun () ->
        let c =
          MetroSplitView.create(
            (fun isOpen ->
              state.SplitOpen <- isOpen
              state.LastAction <- $"SplitView open: {isOpen}"),
            (fun () -> buildSplitPane(state, onSelectSplitNav)),
            fun () -> buildSplitContent state
          )

        splitRef <- ValueSome c
        c
    Panorama =
      fun () ->
        let c =
          MetroPanorama.createWithBackground(
            buildPanoramaBackground,
            buildPanoramaSection
          )

        panoramaRef <- ValueSome c
        c
    TabView =
      MetroTabView.createFactory(function
        | MetroTabViewEvent.Selected id ->
          state.ActiveSubTab <- id
          state.LastAction <- $"Tab selected: {id}"
        | MetroTabViewEvent.Closed id ->
          let remaining = state.Tabs |> Array.filter(fun t -> t.Id <> id)

          if remaining.Length > 0 then
            state.Tabs <- remaining

            if state.ActiveSubTab = id then
              state.ActiveSubTab <- remaining[remaining.Length - 1].Id

            state.LastAction <- $"Tab closed: {id}"
        | MetroTabViewEvent.AddRequested ->
          let n = state.NextTabSeq
          state.NextTabSeq <- n + 1
          let id = $"tab-{n}"

          state.Tabs <-
            Array.append state.Tabs [| MetroTabView.item(id, $"Tab {n}") |]

          state.ActiveSubTab <- id
          state.LastAction <- $"Tab added: {id}")
    Breadcrumb =
      MetroBreadcrumbBar.createFactory(fun id ->
        let idxOpt = state.Crumbs |> Array.tryFindIndex(fun c -> c.Id = id)

        idxOpt
        |> Option.iter(fun idx ->
          state.Crumbs <- state.Crumbs[0..idx]
          state.LastAction <- $"Breadcrumb: {id}"))
  }

  // Created once: every Build reuses the same factory.
  let viewboxFactory() : MetroViewboxCell =
    let cell = MetroViewbox.create viewboxStamp
    viewboxRef <- ValueSome cell
    cell

  let page = {
    new NavigatePageCell() with
      override _.SplitView = splitRef
      override _.Panorama = panoramaRef
      override _.TabBarInput = state.LastTabInput
      override _.BreadcrumbInput = state.LastBreadcrumbInput
      override _.ActiveSubTab = state.ActiveSubTab
      override _.TabCount = state.Tabs.Length
      override _.LastAction = state.LastAction
      override _.ExpanderOpen = state.ExpanderOpen
      override _.GridOrientation = state.GridOrientation
      override _.ViewboxStretch = state.ViewboxStretch
      override _.ViewboxCell = viewboxRef
      override _.Builds = state.Builds

      override _.SetExpanderOpen(expanded) =
        state.ExpanderOpen <- expanded
        self |> ValueOption.iter(fun page -> page.Rebuild())

      override _.SetGridOrientation(orientation) =
        state.GridOrientation <- orientation
        self |> ValueOption.iter(fun page -> page.Rebuild())

      override _.SetViewboxStretch(stretch) =
        state.ViewboxStretch <- stretch
        self |> ValueOption.iter(fun page -> page.Rebuild())

      override _.Build() : Blob = view(state, factories, viewboxFactory)
  }

  self <- ValueSome page
  page
