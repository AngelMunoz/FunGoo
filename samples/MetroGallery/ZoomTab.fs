module MetroGallery.ZoomTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro

module Content =

  let groups = [| "photos"; "maps"; "music" |]

  let tilesOf(group: string) : string array =
    match group with
    | "photos" -> [| "camera"; "albums"; "recent" |]
    | "maps" -> [| "offline"; "city"; "saved" |]
    | _ -> [| "now playing"; "playlists"; "albums" |]

/// Gallery tab with a Metro SemanticZoom over three groups of tiles. The tab
/// owns the zoom state. The headless check of the gallery reads the state and
/// the trees of the zoom cell through these members.
[<AbstractClass>]
type ZoomTabCell() =
  inherit Cell()

  /// <c>true</c> when the tab state asks for the zoomed-out overview.
  abstract ZoomedOut: bool

  /// Text of the last action, for the headless check of the gallery.
  abstract LastAction: string

  /// The latest detail tree that the zoom cell built.
  abstract LastDetail: Blob

  /// The latest overview tree that the zoom cell built.
  abstract LastOverview: Blob

  /// The mounted zoom cell.
  abstract ZoomCell: MetroSemanticZoomCell voption

  /// The number of builds, for the headless check of the gallery.
  abstract Builds: int

/// State of the tab. One mutable field per concern.
type ZoomState = {
  mutable ZoomedOut: bool
  mutable LastAction: string
  // The latest trees and the zoom cell, for the headless check of the gallery.
  mutable LastDetail: Blob
  mutable LastOverview: Blob
  mutable ZoomRef: MetroSemanticZoomCell voption
  // The number of builds, for the headless check of the gallery.
  mutable Builds: int
}

/// The values that the functions of the tab work on.
type ZoomContext = {
  State: ZoomState
  /// Rebuilds this tab.
  RebuildTab: unit -> unit
}

/// The data of one tile button of the tab.
[<Struct; NoComparison>]
type TileLabel = {
  Key: string
  Label: string
  FontSize: float
}

/// Applies one zoom request. The zoom cell gives each request to this tab
/// through `InvokeInputCallback`, so the owner rule rebuilds this tab.
let applyEvent(state: ZoomState, event: MetroSemanticZoomEvent) : unit =
  state.ZoomedOut <- (event = MetroSemanticZoomEvent.ZoomOut)

  state.LastAction <- (if state.ZoomedOut then "Zoomed out" else "Zoomed in")

// The zoom cell builds the detail and the overview trees inside its own
// Build, so it owns the callbacks of their tiles. The owner rule then
// rebuilds the zoom cell and not this tab. For that reason a tile callback
// that changes the state of this tab also rebuilds this tab.
let selectTile(ctx: ZoomContext, group: string, name: string) : unit =
  ctx.State.LastAction <- $"Selected {name} in {group}"
  ctx.RebuildTab()

let selectGroup(ctx: ZoomContext, group: string) : unit =
  ctx.State.ZoomedOut <- false
  ctx.State.LastAction <- $"Zoomed in on {group}"
  ctx.RebuildTab()

/// One tile button of the tab: a sharp rectangle with one label.
let tileButton(label: TileLabel, onClick: unit -> unit) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  Button(
    Key = label.Key,
    Width = Length.Pixels 232.0,
    Height = Length.Pixels 72.0,
    AlignItems = AlignItems.Center,
    JustifyContent = JustifyContent.Center,
    Padding = Edges(Length.Pixels 16.0),
    FlexShrink = 0.0,
    BorderRadius = Length.Pixels 0.0,
    BorderWidth = Edges(Length.Pixels 0.0),
    BorderColor = Edges<Color>(Color.Transparent),
    BoxShadows = Array.empty,
    BackgroundColor = p.BackgroundAlt,
    HoverStyle = Style(BackgroundColor = p.Hover),
    ActiveStyle = Style(BackgroundColor = p.Active),
    FocusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      ),
    TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
    TransitionEasing = Easing.EaseOut,
    Cursor = Cursor.Pointer,
    Focusable = true,
    TabStop = true,
    OnClick = onClick,
    KeyBindings = [|
      KeyBinding(Key.Enter, KeyModifiers.None, onClick)
      KeyBinding(Key.Space, KeyModifiers.None, onClick)
    |],
    Accessibility =
      Accessibility(Role = AccessibilityRole.Button, Name = label.Label)
  )
    .Children(
      Text(
        Content = label.Label,
        Color = p.Foreground,
        FontFamily = theme.FontFamily,
        FontSize = Length.Pixels(label.FontSize * theme.TextScaleFactor),
        FontWeight = 300.0,
        TextWrap = TextWrap.NoWrap,
        TextTrimming = TextTrimming.Ellipsis
      )
    )

/// The detail level: one column of tiles for each group.
let detailView(ctx: ZoomContext) : Blob =
  let groupsRow =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      AlignItems = AlignItems.FlexStart,
      Gap = Length.Pixels 24.0
    )

  for group in Content.groups do
    // Every child of a column is keyed: the header text and the tiles.
    let header = MetroText.subheader group
    header.Key <- $"group-header-{group}"

    let column =
      Container(
        Key = $"group-{group}",
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 8.0
      )
        .Children(header)

    for name in Content.tilesOf group do
      column.Add(
        tileButton(
          {
            Key = $"tile-{group}-{name}"
            Label = name
            FontSize = 14.0
          },
          fun () -> selectTile(ctx, group, name)
        )
      )

    groupsRow.Add(column)

  ctx.State.LastDetail <- groupsRow
  groupsRow

/// The overview level: one tile for each group.
let overviewView(ctx: ZoomContext) : Blob =
  let overview =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      Gap = Length.Pixels 24.0
    )

  for group in Content.groups do
    overview.Add(
      tileButton(
        {
          Key = $"group-tile-{group}"
          Label = group
          FontSize = 28.0
        },
        fun () -> selectGroup(ctx, group)
      )
    )

  ctx.State.LastOverview <- overview
  overview

let render(ctx: ZoomContext, isZoomedOut: bool) : Blob =
  if isZoomedOut then overviewView ctx else detailView ctx

let view(state: ZoomState, zoomFactory: unit -> MetroSemanticZoomCell) : Blob =
  state.Builds <- state.Builds + 1

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    Overflow = Overflow.Hidden,
    Gap = Length.Pixels 16.0
  )
    .Children(
      MetroText.secondary
        "The same content at two levels. Select the zoom button in the top \
         end corner. In the overview, select a group to zoom in.",
      MetroSemanticZoom.mount(
        "gallery-zoom",
        MetroSemanticZoom.defaults state.ZoomedOut,
        zoomFactory
      ),
      MetroText.caption state.LastAction
    )

/// Creates the zoom tab. The tab owns the zoom state. The zoom cell builds
/// the two levels inside its own `Build`.
let create() : ZoomTabCell =
  let state: ZoomState = {
    ZoomedOut = false
    LastAction = "Zoomed in"
    LastDetail = null
    LastOverview = null
    ZoomRef = ValueNone
    Builds = 0
  }

  // The slot of the tab: the callbacks of the tiles are made before the tab
  // exists.
  let mutable self: ZoomTabCell voption = ValueNone

  // The zoom cell builds the tiles, so it owns their callbacks: the owner
  // rule rebuilds the zoom cell and not this tab. A tile callback that
  // changes the state of this tab rebuilds this tab also.
  let rebuildTab() =
    self |> ValueOption.iter(fun tab -> tab.Rebuild())

  let ctx: ZoomContext = {
    State = state
    RebuildTab = rebuildTab
  }

  let onEvent(event: MetroSemanticZoomEvent) = applyEvent(state, event)

  // Created once: every Build reuses the same factory.
  let zoomFactory() : MetroSemanticZoomCell =
    let cell =
      MetroSemanticZoom.create(
        onEvent,
        fun isZoomedOut -> render(ctx, isZoomedOut)
      )

    state.ZoomRef <- ValueSome cell
    cell

  let tab = {
    new ZoomTabCell() with
      override _.ZoomedOut = state.ZoomedOut
      override _.LastAction = state.LastAction
      override _.LastDetail = state.LastDetail
      override _.LastOverview = state.LastOverview
      override _.ZoomCell = state.ZoomRef
      override _.Builds = state.Builds
      override _.Build() : Blob = view(state, zoomFactory)
  }

  self <- ValueSome tab
  tab
