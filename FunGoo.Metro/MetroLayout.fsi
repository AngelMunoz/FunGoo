namespace FunGoo.Metro

open Goo

/// Scroll axis behavior of a <see cref="MetroScrollViewer"/>.
[<Struct>]
type MetroScrollMode =
  /// Disables scrolling and clips overflowing content.
  | Disabled
  /// Enables scrolling and shows the scrollbar automatically.
  | Auto
  /// Enables pan/wheel scrolling while hiding the scrollbar chrome.
  | HiddenScrollbar

/// Data-only properties of a <see cref="MetroScrollViewer"/>.
[<Struct; NoComparison>]
type MetroScrollViewerProps = {
  /// Horizontal scroll policy.
  HorizontalMode: MetroScrollMode
  /// Vertical scroll policy.
  VerticalMode: MetroScrollMode
  /// Inner viewport padding.
  Padding: Edges
  /// Optional <see cref="ElementHandle"/> attached to the scroll viewport.
  Handle: ElementHandle voption
}

/// Metro scroll viewport wrapper over <c>Goo.Container</c>.
[<RequireQualifiedAccess>]
module MetroScrollViewer =

  /// Returns vertical-only scroll viewer properties.
  val vertical: unit -> MetroScrollViewerProps

  /// Returns horizontal-only scroll viewer properties.
  val horizontal: unit -> MetroScrollViewerProps

  /// Returns bi-directional scroll viewer properties.
  val both: unit -> MetroScrollViewerProps

  /// Builds the scroll viewer wrapping <paramref name="content"/>.
  val build: props: MetroScrollViewerProps * content: Blob -> Blob

/// Flow orientation of a <see cref="MetroWrapPanel"/> or
/// <see cref="MetroVariableSizedWrapGrid"/>.
[<Struct>]
type MetroWrapOrientation =
  /// Places items left-to-right (start-to-end), wrapping onto new rows.
  | Horizontal
  /// Places items top-to-bottom, wrapping onto new columns.
  | Vertical

/// Data-only properties of a <see cref="MetroWrapPanel"/>.
[<Struct; NoComparison>]
type MetroWrapPanelProps = {
  /// Primary wrapping axis.
  WrapOrientation: MetroWrapOrientation
  /// Vertical gap between rows in logical pixels.
  RowGap: float
  /// Horizontal gap between columns in logical pixels.
  ColumnGap: float
}

/// Metro flowing wrap container built on <c>FlexWrap.Wrap</c>.
[<RequireQualifiedAccess>]
module MetroWrapPanel =

  /// Returns default horizontal wrap-panel properties with <c>8 px</c> gaps.
  val defaults: unit -> MetroWrapPanelProps

  /// Returns wrap-panel properties with uniform <paramref name="gap"/>.
  val withGap:
    orientation: MetroWrapOrientation * gap: float -> MetroWrapPanelProps

  /// Builds the wrap panel containing <paramref name="items"/>.
  val build: props: MetroWrapPanelProps * items: Blob array -> Blob

/// Column and row cell span of a child inside a
/// <see cref="MetroVariableSizedWrapGrid"/>.
[<Struct; NoComparison>]
type MetroGridSpan = {
  /// Number of grid columns occupied (<c>&gt;= 1</c>).
  ColSpan: int
  /// Number of grid rows occupied (<c>&gt;= 1</c>).
  RowSpan: int
}

/// Data-only arguments of <see cref="MetroVariableSizedWrapGrid"/>.
[<Struct; NoComparison>]
type MetroVariableGridArgs = {
  /// Primary packing flow (<c>Horizontal</c> row-first or <c>Vertical</c>
  /// column-first).
  Orientation: MetroWrapOrientation
  /// Single-cell width in logical pixels. A value above 1.0e6 gives the
  /// 64 px default.
  ItemWidth: float
  /// Single-cell height in logical pixels. A value above 1.0e6 gives the
  /// 64 px default.
  ItemHeight: float
  /// Gutter between cells in logical pixels.
  Gap: float
  /// Optional hard cap on columns (in <c>Horizontal</c> mode) or rows (in
  /// <c>Vertical</c> mode). A value above 1024 gives 1024. When the main
  /// axis has no bound and this value is <c>ValueNone</c>, the grid uses 4
  /// columns in the <c>Horizontal</c> mode and 4 rows in the
  /// <c>Vertical</c> mode.
  MaximumRowsOrColumns: int voption
  /// Per-item cell spans indexed by item position; out-of-range items
  /// default to <c>1x1</c>. A null item in <c>build</c> is skipped with its
  /// span, so each kept child keeps the span of its own item. A span above
  /// 1024 gives 1024.
  Spans: MetroGridSpan array
}

/// Custom <see cref="LayoutAlgorithm"/> packing variable-span Metro tiles into
/// uniform grid cells in either row-major or column-major Start-screen order.
[<RequireQualifiedAccess>]
module MetroVariableSizedWrapGrid =

  /// Placement of one child of a variable grid.
  [<Struct; NoComparison>]
  type PlacedCell = {
    /// Zero-based grid column of the cell.
    Col: int
    /// Zero-based grid row of the cell.
    Row: int
    /// Number of occupied grid columns.
    ColSpan: int
    /// Number of occupied grid rows.
    RowSpan: int
  }

  /// Packs <paramref name="count"/> items into a grid and returns the
  /// placement of each item and the used columns and rows. Pure, and a test
  /// reads this function.
  val pack:
    args: MetroVariableGridArgs * count: int * available: LayoutSize ->
      struct (PlacedCell array * int * int)

  /// Returns a <c>1x1</c> cell span.
  val unitSpan: MetroGridSpan

  /// Creates a cell span of <paramref name="colSpan"/> by
  /// <paramref name="rowSpan"/>.
  val span: colSpan: int * rowSpan: int -> MetroGridSpan

  /// Returns default horizontal variable-grid arguments with <c>8 px</c> gap.
  val defaults:
    itemWidth: float * itemHeight: float * spans: MetroGridSpan array ->
      MetroVariableGridArgs

  /// Creates an immutable <see cref="LayoutAlgorithm"/> for
  /// <paramref name="args"/>.
  val createLayout: args: MetroVariableGridArgs -> LayoutAlgorithm

  /// Builds a container arranged by <see cref="createLayout"/>.
  val build: args: MetroVariableGridArgs * items: Blob array -> Blob

/// Scaling mode of a <see cref="MetroViewbox"/>.
[<Struct>]
type MetroStretch =
  /// Preserves natural <c>1.0</c> scale on both axes.
  | None
  /// Scales X and Y independently to fill the viewport.
  | Fill
  /// Scales uniformly to fit inside the viewport without clipping.
  | Uniform
  /// Scales uniformly to cover the entire viewport.
  | UniformToFill

/// Scale clamping direction of a <see cref="MetroViewbox"/>.
[<Struct>]
type MetroStretchDirection =
  /// Allows scaling both above and below <c>1.0</c>.
  | Both
  /// Allows scaling only at or above <c>1.0</c>.
  | UpOnly
  /// Allows scaling only at or below <c>1.0</c>.
  | DownOnly

/// Data-only properties of a stateless <see cref="MetroViewbox"/> with known
/// viewport and natural content dimensions.
[<Struct; NoComparison>]
type MetroViewboxProps = {
  /// Aspect-ratio stretch mode.
  Stretch: MetroStretch
  /// Scale clamping policy.
  StretchDirection: MetroStretchDirection
  /// Target viewport width in logical pixels.
  ViewportWidth: float
  /// Target viewport height in logical pixels.
  ViewportHeight: float
  /// Natural unscaled content width in logical pixels.
  ContentWidth: float
  /// Natural unscaled content height in logical pixels.
  ContentHeight: float
}

/// Data-only input of a responsive <see cref="MetroViewboxCell"/>.
[<Struct; NoComparison>]
type MetroViewboxInput = {
  /// Aspect-ratio stretch mode.
  Stretch: MetroStretch
  /// Scale clamping policy.
  StretchDirection: MetroStretchDirection
  /// Container width (defaults to <c>Length.Percent 100.0</c>).
  Width: Length
  /// Container height (defaults to <c>Length.Percent 100.0</c>).
  Height: Length
  /// Fallback natural content width before first measurement.
  FallbackContentWidth: float
  /// Fallback natural content height before first measurement.
  FallbackContentHeight: float
}

/// Retained responsive viewbox cell that measures its viewport and child box
/// via <see cref="ElementHandle"/> metrics and updates its 2D scale transform.
/// A change of the viewport size or of the content size rebuilds the cell.
/// The content must have a natural size. A child with a percentage size has
/// no natural size and measures as zero here. Use the stateless
/// <c>MetroViewbox.build</c> with an explicit content size for such content.
[<AbstractClass>]
type MetroViewboxCell =
  inherit Cell<MetroViewboxInput>

  internal new: unit -> MetroViewboxCell

  /// Handle attached to the outer viewport box.
  abstract ViewportHandle: ElementHandle

  /// Handle attached to the scaled content wrapper.
  abstract ContentHandle: ElementHandle

  /// Current horizontal scale factor.
  abstract ScaleX: float

  /// Current vertical scale factor.
  abstract ScaleY: float

/// Metro <c>Viewbox</c> scale calculator, stateless builder, and responsive
/// retained cell.
[<RequireQualifiedAccess>]
module MetroViewbox =

  /// Computes <c>(scaleX, scaleY)</c> for <paramref name="props"/>.
  val computeScale: props: MetroViewboxProps -> struct (float * float)

  /// Returns default <see cref="MetroStretch.Uniform"/> stateless viewbox
  /// properties for a viewport and natural content size.
  val defaults:
    viewportSize: LayoutSize * contentSize: LayoutSize -> MetroViewboxProps

  /// Builds a stateless scaled viewbox wrapping <paramref name="content"/>.
  val build: props: MetroViewboxProps * content: Blob -> Blob

  /// Returns default responsive <see cref="MetroViewboxInput"/>.
  val inputDefaults: fallbackContentSize: LayoutSize -> MetroViewboxInput

  /// Creates a responsive <see cref="MetroViewboxCell"/> rendering
  /// <paramref name="renderContent"/>.
  val create: renderContent: (unit -> Blob) -> MetroViewboxCell

  /// Mounts a <see cref="MetroViewboxCell"/> under <paramref name="key"/>.
  val mount:
    key: string * input: MetroViewboxInput * factory: (unit -> MetroViewboxCell) ->
      Blob
