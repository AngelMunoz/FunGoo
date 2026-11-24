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
    (key: string, props: MetroListViewProps, factory: IMetroListViewFactory)
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

/// Factory for Metro grid views: a virtual wrapped grid of square tiles. Each
/// tile is a sharp button. A selected tile shows a 2 px accent border and an
/// accent tint. The host owns the selection. Create one factory per owner cell,
/// outside <c>Build</c>, so that the virtual collection keeps its key and
/// builder delegates.
type IMetroGridViewFactory =

  /// Builds one tile of edge length <paramref name="tileSize"/>. The latest
  /// <see cref="Build"/> of this factory supplies the selection behavior for
  /// the activation of the tile.
  abstract BuildItem:
    item: MetroGridViewItem * selected: bool * tileSize: float -> Blob

  /// Builds the grid for <paramref name="props"/>. The grid scrolls vertically
  /// and mounts only the tiles near the viewport. Call it inside a
  /// <c>Cell.Build</c>, because it reads the active Metro theme.
  abstract Build: props: MetroGridViewProps -> Blob

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
  type GridTile = {
    Item: MetroGridViewItem
    Selected: bool
  }

  /// State of a grid view factory. One mutable field per concern.
  type GridState = {
    mutable SelectedIds: string array
    mutable Mode: MetroListSelectionMode
    mutable TileSize: float
  }

  /// The values that the functions of a grid view factory work on.
  type GridContext = {
    State: GridState
    /// Raises the events of the grid.
    Raise: MetroListEvent -> unit
  }

  /// The key and the builder delegates of the virtual collection.
  type GridDelegates = {
    ItemKey: Func<GridTile, string>
    ItemBuilder: Func<GridTile, Blob>
  }

  let tileBorder = Edges(Length.Pixels 2.0)
  let zeroRadius = Length.Pixels 0.0

  let resolveTileSize(tileSize: float) : float =
    if Double.IsFinite tileSize && tileSize > 0.0 then
      tileSize
    else
      defaultTilePx

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

  // The latest Build of the factory supplies the selection behavior of the
  // activation of a tile.
  let request(ctx: GridContext, id: string) : unit =
    let selectedIds = ctx.State.SelectedIds

    match ctx.State.Mode with
    | MetroListSelectionMode.Activate -> ctx.Raise(MetroListEvent.ItemClick id)
    | MetroListSelectionMode.Single ->
      // A selected tile stays selected: the request raises nothing.
      let alreadySelected =
        not(isNull selectedIds) && selectedIds.Length = 1 && selectedIds[0] = id

      if not alreadySelected then
        ctx.Raise(MetroListEvent.SelectionChanged [| id |])
    | MetroListSelectionMode.Multiple ->
      let selected = not(isNull selectedIds) && isSelected(selectedIds, id)

      let next =
        if selected then
          if isNull selectedIds then
            Array.empty
          else
            selectedIds |> Array.filter(fun existing -> existing <> id)
        elif isNull selectedIds then
          [| id |]
        else
          Array.append selectedIds [| id |]

      ctx.Raise(MetroListEvent.SelectionChanged next)

  /// Builds one tile: a sharp button with the icon and the label.
  let buildTile(ctx: GridContext, tile: GridTile, tileSize: float) : Blob =
    let item = tile.Item
    let selected = tile.Selected
    let theme = Metro.read()
    let p = theme.Palette
    let accent = theme.Accent.Base
    let size = resolveTileSize tileSize
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
        BorderRadius = zeroRadius,
        BorderWidth = tileBorder,
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
        OnClick = (fun () -> request(ctx, item.Id)),
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

  /// Builds the grid. The Virtual element is its own scroll viewport, so it
  /// carries the wrap layout, the size, and the accessibility role.
  let buildGrid
    (ctx: GridContext, props: MetroGridViewProps, delegates: GridDelegates)
    : Blob =
    ctx.State.Mode <- props.Mode

    ctx.State.SelectedIds <-
      (if isNull props.SelectedIds then
         Array.empty
       else
         props.SelectedIds)

    let size = resolveTileSize props.TileSize
    ctx.State.TileSize <- size

    let grid =
      ``<Program>``.VirtualItems<GridTile>(
        tilesOf props,
        size,
        size,
        delegates.ItemKey,
        delegates.ItemBuilder
      )

    grid.FlexDirection <- FlexDirection.Row
    grid.FlexWrap <- FlexWrap.Wrap
    grid.AlignContent <- AlignContent.FlexStart
    grid.Gap <- Length.Pixels gapPx
    grid.Width <- Length.Percent 100.0

    props.Height |> ValueOption.iter(fun height -> grid.Height <- height)

    grid.Accessibility <-
      Accessibility(
        Role = AccessibilityRole.Grid,
        Name = (props.AccessibilityName |> ValueOption.defaultValue "Grid view")
      )

    grid

  let createFactory(onEvent: MetroListEvent -> unit) : IMetroGridViewFactory =
    let ctx: GridContext = {
      State = {
        SelectedIds = Array.empty
        Mode = MetroListSelectionMode.Single
        TileSize = defaultTilePx
      }
      Raise = onEvent
    }

    // One instance each: every Build of this factory gives the virtual
    // collection the same key and builder delegates.
    let delegates: GridDelegates = {
      ItemKey = Func<GridTile, string>(fun tile -> tile.Item.Id)

      ItemBuilder =
        Func<GridTile, Blob>(fun tile ->
          buildTile(ctx, tile, ctx.State.TileSize))
    }

    {
      new IMetroGridViewFactory with
        member _.BuildItem(item, selected, tileSize) =
          buildTile(ctx, { Item = item; Selected = selected }, tileSize)

        member _.Build(props) = buildGrid(ctx, props, delegates)
    }
