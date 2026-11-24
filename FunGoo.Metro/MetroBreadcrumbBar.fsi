namespace FunGoo.Metro

open Goo
open Goo.Widgets.Navigation

/// Data-only description of one segment in a <see cref="MetroBreadcrumbBar"/>.
[<Struct; NoComparison>]
type MetroBreadcrumbItem = {
  /// Unique segment identifier passed to the selection handler.
  Id: string
  /// Visible segment text.
  Text: string
  /// Optional leading Material icon name.
  IconName: string voption
}

/// Data-only properties of a <see cref="MetroBreadcrumbBar"/>.
[<Struct; NoComparison>]
type MetroBreadcrumbProps = {
  /// Ordered path segments from root to current location.
  Items: MetroBreadcrumbItem array
  /// Number of leading segments kept visible when collapsing middle segments
  /// (defaults to <c>1</c>). The value is at least <c>1</c>; a smaller value
  /// gives <c>1</c>.
  KeepStart: int
  /// Number of trailing segments kept visible when collapsing middle segments
  /// (defaults to <c>2</c>). The value is at least <c>1</c>; a smaller value
  /// gives <c>1</c>.
  KeepEnd: int
  /// Optional accessible name override.
  AccessibilityName: string voption
}

/// Service factory wrapping <see cref="Breadcrumb"/> with cached
/// <see cref="BreadcrumbItem"/> arrays and cached callback delegates. Create
/// once outside <c>Build</c> per breadcrumb trail.
type IMetroBreadcrumbFactory =
  /// Builds a <see cref="BreadcrumbInput"/> snapshot with cached items and
  /// callbacks. While <c>Items</c> is the same array instance, the mapped items
  /// are the same instance also.
  abstract CreateInput: props: MetroBreadcrumbProps -> BreadcrumbInput

  /// Mounts the Metro breadcrumb bar under <paramref name="key"/>.
  abstract Mount: key: string * props: MetroBreadcrumbProps -> Blob

/// Flat Metro <c>BreadcrumbBar</c> builders and
/// <see cref="IMetroBreadcrumbFactory"/> constructors.
[<RequireQualifiedAccess>]
module MetroBreadcrumbBar =

  /// Creates a breadcrumb segment without an icon.
  val item: id: string * label: string -> MetroBreadcrumbItem

  /// Creates a breadcrumb segment with a leading Material icon.
  val iconItem:
    id: string * label: string * iconName: string -> MetroBreadcrumbItem

  /// Returns default breadcrumb properties for <paramref name="items"/>.
  val defaults: items: MetroBreadcrumbItem array -> MetroBreadcrumbProps

  /// Creates a reusable <see cref="IMetroBreadcrumbFactory"/>.
  val createFactory: onSelect: (string -> unit) -> IMetroBreadcrumbFactory
