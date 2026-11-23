namespace FunGoo.Metro

open System
open Goo

/// Canonical Metro Start tile footprint presets expressed in grid cell spans.
[<Struct>]
type MetroTileSize =
  /// 1x1 cell square tile.
  | Small
  /// 2x2 cell square tile.
  | Medium
  /// 4x2 cell wide rectangular tile.
  | Wide
  /// 4x4 cell large square tile.
  | Large

/// Grid span and pixel geometry calculations for Metro tiles.
[<RequireQualifiedAccess>]
module MetroTileMetrics =

  /// Returns the <c>(columns, rows)</c> cell span for <paramref name="size"/>.
  val cellSpans: size: MetroTileSize -> struct (int * int)

  /// Computes the logical pixel <c>(width, height)</c> for
  /// <paramref name="size"/> given a single cell edge length and inter-cell
  /// gutter.
  val pixelSize:
    cellSize: float * gap: float * size: MetroTileSize -> struct (float * float)

/// Immutable Metro tile grid bin-packing <see cref="LayoutAlgorithm"/> placing
/// mixed-size tiles without overlap and with exact gutters.
[<Sealed>]
type TileGridLayout =
  interface LayoutAlgorithm

  /// Creates an immutable tile grid layout with logical
  /// <paramref name="cellSize"/>, inter-cell <paramref name="gap"/>, grid
  /// <paramref name="columns"/>, and per-child tile <paramref name="sizes"/>.
  new:
    cellSize: float * gap: float * columns: int * sizes: MetroTileSize array ->
      TileGridLayout

  /// Resolved single-cell edge length in logical pixels.
  member CellSize: float

  /// Resolved gutter width between adjacent cells in logical pixels.
  member Gap: float

  /// Resolved column count of the tile grid.
  member Columns: int

/// Data-only properties for a static iconic Metro tile with optional count
/// badge.
[<Struct; NoComparison>]
type MetroIconicTileProps = {
  /// Bottom-left tile title label.
  Title: string
  /// Material symbol icon name rendered in the center of the tile.
  IconName: string
  /// Optional numeric badge displayed beside the tile icon.
  Count: int voption
  /// Tile footprint preset (defaults to <see cref="MetroTileSize.Medium"/>).
  Size: MetroTileSize
  /// Single-cell edge length in logical pixels (defaults to <c>72.0</c>).
  CellSize: float
  /// Gutter width between cells in logical pixels (defaults to <c>8.0</c>).
  Gap: float
  /// Optional accent color overriding <see cref="MetroTheme.Accent"/>.
  AccentOverride: Color voption
  /// Disables pointer and keyboard activation when <c>true</c>.
  Disabled: bool
}

/// Square, solid-fill Metro iconic tile builders.
[<RequireQualifiedAccess>]
module MetroIconicTile =

  /// Creates default <see cref="MetroIconicTileProps"/> for
  /// <paramref name="title"/> and <paramref name="iconName"/> using medium size
  /// (<c>CellSize = 72.0</c>, <c>Gap = 8.0</c>).
  val defaults: title: string * iconName: string -> MetroIconicTileProps

  /// Builds an iconic Metro tile <see cref="Blob"/> invoking
  /// <paramref name="onClick"/> on pointer or keyboard activation.
  val build: props: MetroIconicTileProps * onClick: (unit -> unit) -> Blob

/// Content definition for a single face of a cycling Metro live tile.
[<Struct; NoComparison>]
type MetroLiveTileFace = {
  /// Primary title rendered at the bottom-left of the tile face.
  Title: string
  /// Optional secondary descriptive line displayed in the tile body.
  Subtitle: string voption
  /// Optional glanceable metric value rendered prominently above the title.
  Metric: string voption
  /// Optional accent color overriding <see cref="MetroTheme.Accent"/> for this
  /// face.
  AccentOverride: Color voption
}

/// Data-only input snapshot for <see cref="MetroLiveTileCell"/>.
[<Struct; NoComparison>]
type MetroLiveTileInput = {
  /// Ordered faces cycled by the live tile.
  Faces: MetroLiveTileFace array
  /// Tile footprint preset (defaults to <see cref="MetroTileSize.Wide"/>).
  Size: MetroTileSize
  /// Single-cell edge length in logical pixels (defaults to <c>72.0</c>).
  CellSize: float
  /// Gutter width between cells in logical pixels (defaults to <c>8.0</c>).
  Gap: float
  /// Cycle interval in milliseconds between face transitions (defaults to
  /// <c>4500.0</c>).
  IntervalMs: float
  /// Disables pointer and keyboard activation when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding the active face title.
  AccessibilityName: string voption
}

/// Stateful cycling Metro live tile cell driven by <c>Cell.Every</c> and
/// paint-bound slide/fade animations, honoring reduced motion.
[<Sealed>]
type MetroLiveTileCell =
  inherit Cell<MetroLiveTileInput>

  /// Creates a live tile cell invoking <paramref name="onClick"/> on
  /// activation.
  new: onClick: (unit -> unit) -> MetroLiveTileCell

  /// Zero-based index of the currently displayed face.
  member ActiveFaceIndex: int

  /// <c>true</c> while an automatic face-cycling timer is active.
  member IsTimerActive: bool

  /// Paint-bound translation animation for face entrance motion.
  member FaceTranslate: Anim<Point>

  /// Paint-bound opacity animation for face entrance motion.
  member FaceOpacity: Anim<float>

  /// Starts face cycling when mounted with multiple faces and standard motion.
  override Mounted: unit -> unit

  /// Updates face bounds and cycle timer when input changes.
  override InputChanged: input: MetroLiveTileInput -> unit

  /// Cancels any active cycle timer and snaps animations on unmount.
  override Unmounting: unit -> unit

  /// Builds the live tile container and active face presentation.
  override Build: input: MetroLiveTileInput -> Blob

/// Construction and mounting helpers for <see cref="MetroLiveTileCell"/>.
[<RequireQualifiedAccess>]
module MetroLiveTile =

  /// Creates default <see cref="MetroLiveTileInput"/> for
  /// <paramref name="faces"/> using wide size, <c>72.0</c> px cell unit,
  /// <c>8.0</c> px gap, and <c>4500.0</c> ms interval.
  val defaults: faces: MetroLiveTileFace array -> MetroLiveTileInput

  /// Creates a reusable cell factory for <see cref="MetroLiveTileCell"/>. Cache
  /// the returned factory outside <c>Build</c>.
  val createFactory: onClick: (unit -> unit) -> Func<MetroLiveTileCell>

  /// Mounts a <see cref="MetroLiveTileCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string * input: MetroLiveTileInput * factory: Func<MetroLiveTileCell> ->
      Blob
