namespace FunGoo.Metro

open Goo

/// Bordered Metro list box for a small set of choices. The box holds a
/// <see cref="IMetroListViewFactory"/> list, so the host owns the selection in
/// the same way.
[<RequireQualifiedAccess>]
module MetroListBox =

  /// Border width in logical pixels of a list box.
  val borderWidthPx: float

  /// Builds a sharp 2 px bordered box that holds the Metro list of
  /// <paramref name="factory"/> under <paramref name="key"/> with
  /// <paramref name="props"/>.
  val build:
    key: string * props: MetroListViewProps * factory: IMetroListViewFactory ->
      Blob

/// One tile of a <see cref="IMetroGridViewFactory"/> grid.
[<Struct; NoComparison>]
type MetroGridViewItem = {
  /// Stable unique identifier of the tile.
  Id: string
  /// Label of the tile.
  Label: string
  /// Optional Material symbol icon name shown above the label.
  IconName: string voption
}

/// Data-only properties for a Metro grid view.
[<Struct; NoComparison>]
type MetroGridViewProps = {
  /// Tiles in reading order.
  Items: MetroGridViewItem array
  /// Identifiers of the selected tiles.
  SelectedIds: string array
  /// Selection behavior. <see cref="MetroListSelectionMode.Activate"/> raises
  /// <see cref="MetroListEvent.ItemClick"/> only.
  Mode: MetroListSelectionMode
  /// Tile edge length in logical pixels. A value that is not finite and
  /// positive selects <see cref="MetroGridView.defaultTilePx"/>.
  TileSize: float
  /// Height of the grid. <c>ValueNone</c> lets the grid grow with its rows.
  Height: Length voption
  /// Optional accessible name of the grid.
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

/// Tile edge length and gap defaults of the Metro grid view, and the factory
/// of a grid.
[<RequireQualifiedAccess>]
module MetroGridView =

  /// Tile edge length in logical pixels when <c>TileSize</c> is not set.
  val defaultTilePx: float

  /// Gap between tiles in logical pixels.
  val gapPx: float

  /// Creates <see cref="MetroGridViewProps"/> for <paramref name="items"/> with
  /// multiple selection, no selection, and the default tile size.
  val defaults: items: MetroGridViewItem array -> MetroGridViewProps

  /// Creates a factory that raises <paramref name="onEvent"/> for selection
  /// changes and tile activation.
  val createFactory: onEvent: (MetroListEvent -> unit) -> IMetroGridViewFactory
