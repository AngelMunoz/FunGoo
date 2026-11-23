module MetroGallery.ZoomTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro

module private Content =

  let groups = [| "photos"; "maps"; "music" |]

  let tilesOf(group: string) : string array =
    match group with
    | "photos" -> [| "camera"; "albums"; "recent" |]
    | "maps" -> [| "offline"; "city"; "saved" |]
    | _ -> [| "now playing"; "playlists"; "albums" |]

/// Gallery tab with a Metro SemanticZoom over three groups of tiles. The tab
/// owns the zoom state.
[<Sealed>]
type ZoomTabCell() as this =
  inherit Cell()

  let mutable zoomedOut = false
  let mutable lastAction = "Zoomed in"

  // The latest trees and the zoom cell, for the headless check of the gallery.
  let mutable lastDetail: Blob = null
  let mutable lastOverview: Blob = null
  let mutable zoomRef: MetroSemanticZoomCell voption = ValueNone

  let onEvent(event: MetroSemanticZoomEvent) =
    zoomedOut <- (event = MetroSemanticZoomEvent.ZoomOut)

    lastAction <- (if zoomedOut then "Zoomed out" else "Zoomed in")

    this.Rebuild()

  // The zoom cell builds the detail and the overview trees inside its own
  // Build, so it owns the callbacks of their tiles. The owner rule then
  // rebuilds the zoom cell and not this tab. For that reason a tile callback
  // that changes the state of this tab also rebuilds this tab.
  let selectTile(group: string, name: string) =
    lastAction <- $"Selected {name} in {group}"
    this.Rebuild()

  let selectGroup(group: string) =
    zoomedOut <- false
    lastAction <- $"Zoomed in on {group}"
    this.Rebuild()

  let tile
    (key: string, label: string, fontSize: float, onClick: unit -> unit)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette

    Button(
      Key = key,
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
        Accessibility(Role = AccessibilityRole.Button, Name = label)
    )
      .Children(
        Text(
          Content = label,
          Color = p.Foreground,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(fontSize * theme.TextScaleFactor),
          FontWeight = 300.0,
          TextWrap = TextWrap.NoWrap,
          TextTrimming = TextTrimming.Ellipsis
        )
      )

  let renderDetail() : Blob =
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
          tile(
            $"tile-{group}-{name}",
            name,
            14.0,
            fun () -> selectTile(group, name)
          )
        )

      groupsRow.Add(column)

    lastDetail <- groupsRow
    groupsRow

  let renderOverview() : Blob =
    let overview =
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        Gap = Length.Pixels 24.0
      )

    for group in Content.groups do
      overview.Add(
        tile($"group-tile-{group}", group, 28.0, fun () -> selectGroup group)
      )

    lastOverview <- overview
    overview

  let render(isZoomedOut: bool) : Blob =
    if isZoomedOut then renderOverview() else renderDetail()

  // Created once: every Build reuses the same factory.
  let zoomFactory() : MetroSemanticZoomCell =
    let cell = MetroSemanticZoomCell(onEvent, render)
    zoomRef <- ValueSome cell
    cell

  /// <c>true</c> when the tab state asks for the zoomed-out overview.
  member _.ZoomedOut = zoomedOut

  /// Text of the last action, for the headless check of the gallery.
  member _.LastAction = lastAction

  /// The latest detail tree that the zoom cell built.
  member _.LastDetail = lastDetail

  /// The latest overview tree that the zoom cell built.
  member _.LastOverview = lastOverview

  /// The mounted zoom cell.
  member _.ZoomCell = zoomRef

  override _.Build() : Blob =
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
          MetroSemanticZoom.defaults zoomedOut,
          zoomFactory
        ),
        MetroText.caption lastAction
      )
