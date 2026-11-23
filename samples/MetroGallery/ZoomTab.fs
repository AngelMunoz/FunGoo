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

  let onEvent(event: MetroSemanticZoomEvent) =
    zoomedOut <- (event = MetroSemanticZoomEvent.ZoomOut)

    lastAction <- (if zoomedOut then "Zoomed out" else "Zoomed in")

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
      let column =
        Container(
          Key = $"group-{group}",
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 8.0
        )
          .Children(MetroText.subheader group)

      for name in Content.tilesOf group do
        column.Add(
          tile(
            $"tile-{group}-{name}",
            name,
            14.0,
            // The tab authored this callback, so the owner rule rebuilds it.
            fun () -> lastAction <- $"Selected {name} in {group}"
          )
        )

      groupsRow.Add(column)

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
        tile(
          $"group-tile-{group}",
          group,
          28.0,
          // The tab authored this callback, so the owner rule rebuilds it.
          fun () ->
            zoomedOut <- false
            lastAction <- $"Zoomed in on {group}"
        )
      )

    overview

  // The zoom cell owns the callback, so the tab rebuilds itself in onEvent.
  // Created once: every Build reuses the same factory.
  let zoomFactory =
    MetroSemanticZoom.createFactory(
      onEvent,
      fun isZoomedOut ->
        if isZoomedOut then renderOverview() else renderDetail()
    )

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
