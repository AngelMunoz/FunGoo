namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Data
open Goo.Widgets.Icons
open FunGoo.Children

[<RequireQualifiedAccess>]
module MetroListBox =

  let borderWidthPx: float = 2.0

  let build
    (key: string, props: MetroListViewProps, factory: MetroListViewFactory)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette

    Container(
      FlexDirection = FlexDirection.Column,
      Width = Length.Percent 100.0,
      FlexShrink = 0.0,
      Overflow = Overflow.Hidden,
      BorderRadius = Length.Pixels 0.0,
      BorderWidth = Edges(Length.Pixels borderWidthPx),
      BorderColor =
        Edges<Color>(
          if theme.HighContrast then p.Foreground else p.BorderStrong
        ),
      BoxShadows = Array.empty
    )
      .Children(factory.Mount(key, props))

[<Struct; NoComparison>]
type MetroGridViewItem = {
  Id: string
  Label: string
  IconName: string voption
}

[<Struct; NoComparison>]
type MetroGridViewProps = {
  Items: MetroGridViewItem array
  SelectedIds: string array
  Mode: MetroListSelectionMode
  TileSize: float
  Height: Length voption
  AccessibilityName: string voption
}

[<RequireQualifiedAccess>]
module MetroGridView =

  let defaultTilePx: float = 120.0
  let gapPx: float = 8.0

  let defaults(items: MetroGridViewItem array) : MetroGridViewProps = {
    Items = items
    SelectedIds = Array.empty
    Mode = MetroListSelectionMode.Multiple
    TileSize = defaultTilePx
    Height = ValueNone
    AccessibilityName = ValueNone
  }

// The value of one tile for the virtual collection. The collection does not
// build a tile again when its value is equal, so only a tile whose selection
// changed is built again.
[<Struct; NoComparison>]
type private GridTile = {
  Item: MetroGridViewItem
  Selected: bool
}

module private GridInternals =

  let tileBorder = Edges(Length.Pixels 2.0)
  let zeroRadius = Length.Pixels 0.0

  let resolveTileSize(tileSize: float) : float =
    if Double.IsFinite tileSize && tileSize > 0.0 then
      tileSize
    else
      MetroGridView.defaultTilePx

  let isSelected(selectedIds: string array, id: string) : bool =
    if isNull selectedIds || isNull id then
      false
    else
      let mutable found = false
      let mutable i = 0

      while i < selectedIds.Length && not found do
        found <- selectedIds[i] = id
        i <- i + 1

      found

  let tilesOf(props: MetroGridViewProps) : GridTile array =
    if isNull props.Items then
      Array.empty
    else
      props.Items
      |> Array.map(fun item -> {
        Item = item
        Selected = isSelected(props.SelectedIds, item.Id)
      })


[<Sealed>]
type MetroGridViewFactory(onEvent: MetroListEvent -> unit) as this =

  // The latest Build supplies the selection behavior of a tile activation.
  let mutable currentSelected: string array = Array.empty

  let mutable currentMode: MetroListSelectionMode =
    MetroListSelectionMode.Single

  let mutable currentTileSize: float = MetroGridView.defaultTilePx

  let request(id: string) : unit =
    match currentMode with
    | MetroListSelectionMode.Activate -> onEvent(MetroListEvent.ItemClick id)
    | MetroListSelectionMode.Single ->
      if
        not(isNull currentSelected)
        && currentSelected.Length = 1
        && currentSelected[0] = id
      then
        ()
      else
        onEvent(MetroListEvent.SelectionChanged [| id |])
    | MetroListSelectionMode.Multiple ->
      let selected =
        not(isNull currentSelected)
        && GridInternals.isSelected(currentSelected, id)

      let next =
        if selected then
          if isNull currentSelected then
            Array.empty
          else
            currentSelected |> Array.filter(fun existing -> existing <> id)
        elif isNull currentSelected then
          [| id |]
        else
          Array.append currentSelected [| id |]

      onEvent(MetroListEvent.SelectionChanged next)

  // One instance each: every Build of this factory gives the virtual
  // collection the same key and builder delegates.
  let itemKey = Func<GridTile, string>(fun tile -> tile.Item.Id)

  let itemBuilder =
    Func<GridTile, Blob>(fun tile ->
      this.BuildItem(tile.Item, tile.Selected, currentTileSize))

  member _.BuildItem
    (item: MetroGridViewItem, selected: bool, tileSize: float)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let accent = theme.Accent.Base
    let size = GridInternals.resolveTileSize tileSize
    let label = if isNull item.Label then "" else item.Label

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let tile =
      Button(
        // A virtual item requires its content key to equal the item key.
        Key = item.Id,
        Width = Length.Pixels size,
        Height = Length.Pixels size,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Length.Pixels 8.0,
        Padding = Edges(Length.Pixels 8.0),
        FlexShrink = 0.0,
        Overflow = Overflow.Hidden,
        BorderRadius = GridInternals.zeroRadius,
        BorderWidth = GridInternals.tileBorder,
        BorderColor =
          Edges<Color>(
            if not selected then Color.Transparent
            elif theme.HighContrast then p.Foreground
            else accent
          ),
        BoxShadows = Array.empty,
        BackgroundColor =
          (if not selected then p.BackgroundAlt
           elif theme.HighContrast then p.Highlight
           else accent.WithAlpha(0.3)),
        Color = p.Foreground,
        HoverStyle = Style(BackgroundColor = p.Hover),
        ActiveStyle = Style(BackgroundColor = p.Active),
        FocusStyle = focusStyle,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = Cursor.Pointer,
        Focusable = true,
        TabStop = true,
        OnClick = (fun () -> request item.Id),
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.GridCell,
            Name = label,
            Selected = Nullable selected
          )
      )

    item.IconName
    |> ValueOption.iter(fun iconName ->
      tile.Add(
        MaterialIcons.Create(iconName, size * 0.28, Nullable p.Foreground)
      ))

    tile.Add(
      Text(
        Content = label,
        Color = p.Foreground,
        FontFamily = theme.FontFamily,
        FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
        FontWeight = 400.0,
        TextWrap = TextWrap.NoWrap,
        TextTrimming = TextTrimming.Ellipsis
      )
    )

    tile

  member _.Build(props: MetroGridViewProps) : Blob =
    currentMode <- props.Mode

    currentSelected <-
      (if isNull props.SelectedIds then
         Array.empty
       else
         props.SelectedIds)

    let size = GridInternals.resolveTileSize props.TileSize
    currentTileSize <- size

    // The Virtual element is its own scroll viewport, so it carries the wrap
    // layout, the size, and the accessibility role.
    let grid =
      ``<Program>``.VirtualItems<GridTile>(
        GridInternals.tilesOf props,
        size,
        size,
        itemKey,
        itemBuilder
      )

    grid.FlexDirection <- FlexDirection.Row
    grid.FlexWrap <- FlexWrap.Wrap
    grid.AlignContent <- AlignContent.FlexStart
    grid.Gap <- Length.Pixels MetroGridView.gapPx
    grid.Width <- Length.Percent 100.0

    props.Height |> ValueOption.iter(fun height -> grid.Height <- height)

    grid.Accessibility <-
      Accessibility(
        Role = AccessibilityRole.Grid,
        Name = (props.AccessibilityName |> ValueOption.defaultValue "Grid view")
      )

    grid
