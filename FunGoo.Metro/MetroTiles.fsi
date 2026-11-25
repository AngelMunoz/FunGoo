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

/// The arguments of a tile grid layout.
[<NoComparison; NoEquality>]
type TileGridArgs = {
  /// Single-cell edge length in logical pixels. A value that is not finite or
  /// not positive gives <c>72.0</c>.
  CellSize: float
  /// Gutter width between cells in logical pixels. A value that is not finite
  /// or is negative gives <c>8.0</c>.
  Gap: float
  /// Column count of the grid. A value below <c>1</c> gives <c>4</c>.
  Columns: int
  /// The tile size of each child. A child past the end of the array is
  /// <see cref="MetroTileSize.Medium"/>.
  Sizes: MetroTileSize array
}

/// The resolved values of a tile grid layout.
[<NoComparison; NoEquality>]
type TileGridSpec = {
  /// The single-cell edge length in logical pixels.
  CellSize: float
  /// The gutter width between cells in logical pixels.
  Gap: float
  /// The column count of the grid.
  Columns: int
  /// A copy of the tile size of each child.
  Sizes: MetroTileSize array
}

/// Immutable Metro tile grid bin-packing <see cref="LayoutAlgorithm"/> placing
/// mixed-size tiles without overlap and with exact gutters.
[<RequireQualifiedAccess>]
module TileGridLayout =

  /// Resolves the arguments of a tile grid layout into the values that the
  /// layout uses, with a copy of the tile sizes.
  val resolve: args: TileGridArgs -> TileGridSpec

  /// Creates an immutable tile grid layout for
  /// <see cref="TileGridArgs.CellSize"/>, <see cref="TileGridArgs.Gap"/>,
  /// <see cref="TileGridArgs.Columns"/>, and <see cref="TileGridArgs.Sizes"/>.
  val create: args: TileGridArgs -> LayoutAlgorithm

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
/// <c>MetroLiveTile.create</c> makes the cell. The type holds the surface that
/// hosts and tests use.
[<AbstractClass>]
type MetroLiveTileCell =
  inherit Cell<MetroLiveTileInput>

  /// Zero-based index of the currently displayed face. A test reads it.
  abstract ActiveFaceIndex: int

  /// <c>true</c> while an automatic face-cycling timer is active. A host window
  /// that is not open, such as a <c>goo render</c> frame, never starts the timer.
  abstract IsTimerActive: bool

  /// The tree of the current input. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

  /// Current press-tilt state and animation handles. A test reads it.
  abstract TiltState: MetroTiltState

  /// The number of builds of this live tile cell. A test reads it.
  abstract Builds: int

  /// Gives one click of this cell to the host cell, as a Goo.Widgets control
  /// does. The owner rule then rebuilds the host cell, so a host does not
  /// call <c>Rebuild</c> on itself in the click callback.
  member Raise: callback: Action -> unit

/// Construction and mounting helpers for <see cref="MetroLiveTileCell"/>.
[<RequireQualifiedAccess>]
module MetroLiveTile =

  /// Creates default <see cref="MetroLiveTileInput"/> for
  /// <paramref name="faces"/> using wide size, <c>72.0</c> px cell unit,
  /// <c>8.0</c> px gap, and <c>4500.0</c> ms interval.
  val defaults: faces: MetroLiveTileFace array -> MetroLiveTileInput

  /// Creates a live tile cell. The cell gives each activation to
  /// <paramref name="onClick"/>, and the owner rule then rebuilds the host
  /// cell.
  val create: onClick: (unit -> unit) -> MetroLiveTileCell

  /// Creates a live tile cell with an explicit optional
  /// <see cref="ShaderEffect"/> for press tilt. Tests use this to exercise
  /// both 3D and 2D tilt paths.
  val createWithEffect:
    effect: ShaderEffect voption * onClick: (unit -> unit) -> MetroLiveTileCell

  /// Creates a reusable cell factory for <see cref="MetroLiveTileCell"/>. Cache
  /// the returned factory outside <c>Build</c>.
  val createFactory: onClick: (unit -> unit) -> unit -> MetroLiveTileCell

  /// Mounts a <see cref="MetroLiveTileCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroLiveTileInput *
    factory: (unit -> MetroLiveTileCell) ->
      Blob

/// Data-only input snapshot for <see cref="MetroCycleTileCell"/>.
[<Struct; NoComparison>]
type MetroCycleTileInput = {
  /// Ordered faces cycled by the cycle tile.
  Faces: MetroLiveTileFace array
  /// Tile footprint preset (defaults to <see cref="MetroTileSize.Medium"/>).
  Size: MetroTileSize
  /// Single-cell edge length in logical pixels (defaults to <c>72.0</c>).
  CellSize: float
  /// Gutter width between cells in logical pixels (defaults to <c>8.0</c>).
  Gap: float
  /// Cycle interval in milliseconds between face transitions (defaults to
  /// <c>3000.0</c>).
  IntervalMs: float
  /// Disables pointer and keyboard activation when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding the active face title.
  AccessibilityName: string voption
}

/// Stateful cycling Metro cycle tile cell driven by <c>Cell.Every</c> and
/// paint-bound slide/fade animations, honoring reduced motion.
/// <c>MetroCycleTile.create</c> makes the cell. The type holds the surface
/// that hosts and tests use.
[<AbstractClass>]
type MetroCycleTileCell =
  inherit Cell<MetroCycleTileInput>

  /// Zero-based index of the currently displayed face. A test reads it.
  abstract ActiveFaceIndex: int

  /// <c>true</c> while an automatic face-cycling timer is active. A host
  /// window that is not open, such as a <c>goo render</c> frame, never starts
  /// the timer.
  abstract IsTimerActive: bool

  /// The tree of the current input. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

  /// Current press-tilt state and animation handles. A test reads it.
  abstract TiltState: MetroTiltState

  /// The number of builds of this cycle tile cell. A test reads it.
  abstract Builds: int

  /// Gives one click of this cell to the host cell, as a Goo.Widgets control
  /// does. The owner rule then rebuilds the host cell, so a host does not
  /// call <c>Rebuild</c> on itself in the click callback.
  member Raise: callback: Action -> unit

/// Construction and mounting helpers for <see cref="MetroCycleTileCell"/>.
[<RequireQualifiedAccess>]
module MetroCycleTile =

  /// Creates default <see cref="MetroCycleTileInput"/> for
  /// <paramref name="faces"/> using medium size, <c>72.0</c> px cell unit,
  /// <c>8.0</c> px gap, and <c>3000.0</c> ms interval.
  val defaults: faces: MetroLiveTileFace array -> MetroCycleTileInput

  /// Creates a cycle tile cell. The cell gives each activation to
  /// <paramref name="onClick"/>, and the owner rule then rebuilds the host
  /// cell.
  val create: onClick: (unit -> unit) -> MetroCycleTileCell

  /// Creates a cycle tile cell with an explicit optional
  /// <see cref="ShaderEffect"/> for press tilt. Tests use this to exercise
  /// both 3D and 2D tilt paths.
  val createWithEffect:
    effect: ShaderEffect voption * onClick: (unit -> unit) -> MetroCycleTileCell

  /// Creates a reusable cell factory for <see cref="MetroCycleTileCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory: onClick: (unit -> unit) -> unit -> MetroCycleTileCell

  /// Mounts a <see cref="MetroCycleTileCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroCycleTileInput *
    factory: (unit -> MetroCycleTileCell) ->
      Blob

/// Data-only input snapshot for <see cref="MetroFlipTileCell"/>.
[<Struct; NoComparison>]
type MetroFlipTileInput = {
  /// Face displayed while the tile is not flipped.
  Front: MetroLiveTileFace
  /// Face displayed while the tile is flipped.
  Back: MetroLiveTileFace
  /// Tile footprint preset (defaults to <see cref="MetroTileSize.Medium"/>).
  Size: MetroTileSize
  /// Single-cell edge length in logical pixels (defaults to <c>72.0</c>).
  CellSize: float
  /// Gutter width between cells in logical pixels (defaults to <c>8.0</c>).
  Gap: float
  /// <c>true</c> selects the back face. The cell flips when this value
  /// changes, and a click flips the tile.
  Flipped: bool
  /// Disables pointer and keyboard activation when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding the displayed face title.
  AccessibilityName: string voption
}

/// Stateful Metro flip tile cell showing one of two faces with a tile flip
/// motion, honoring reduced motion with a snap. <c>MetroFlipTile.create</c>
/// makes the cell. The type holds the surface that hosts and tests use.
[<AbstractClass>]
type MetroFlipTileCell =
  inherit Cell<MetroFlipTileInput>

  /// <c>true</c> when the back face is displayed. A test reads it.
  abstract Flipped: bool

  /// The tree of the current input. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

  /// Current press-tilt state and animation handles. A test reads it.
  abstract TiltState: MetroTiltState

  /// The number of builds of this flip tile cell. A test reads it.
  abstract Builds: int

  /// Gives one click of this cell to the host cell, as a Goo.Widgets control
  /// does. The owner rule then rebuilds the host cell, so a host does not
  /// call <c>Rebuild</c> on itself in the click callback.
  member Raise: callback: Action -> unit

/// Construction and mounting helpers for <see cref="MetroFlipTileCell"/>.
[<RequireQualifiedAccess>]
module MetroFlipTile =

  /// Creates default <see cref="MetroFlipTileInput"/> for
  /// <paramref name="front"/> and <paramref name="back"/> using medium size,
  /// <c>72.0</c> px cell unit, and <c>8.0</c> px gap.
  val defaults:
    front: MetroLiveTileFace * back: MetroLiveTileFace -> MetroFlipTileInput

  /// Creates a flip tile cell. The cell flips on each activation and gives
  /// the activation to <paramref name="onClick"/>, and the owner rule then
  /// rebuilds the host cell.
  val create: onClick: (unit -> unit) -> MetroFlipTileCell

  /// Creates a flip tile cell with an explicit optional
  /// <see cref="ShaderEffect"/> for press tilt. Tests use this to exercise
  /// both 3D and 2D tilt paths.
  val createWithEffect:
    effect: ShaderEffect voption * onClick: (unit -> unit) -> MetroFlipTileCell

  /// Creates a reusable cell factory for <see cref="MetroFlipTileCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory: onClick: (unit -> unit) -> unit -> MetroFlipTileCell

  /// Mounts a <see cref="MetroFlipTileCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroFlipTileInput *
    factory: (unit -> MetroFlipTileCell) ->
      Blob
