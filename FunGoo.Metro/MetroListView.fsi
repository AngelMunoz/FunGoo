namespace FunGoo.Metro

open Goo
open Goo.Widgets.Data

/// One row of a Metro list.
[<Struct; NoComparison>]
type MetroListItem = {
  /// Stable unique identifier of the row.
  Id: string
  /// Primary label of the row. An empty label shows the identifier.
  Label: string
  /// Optional secondary caption under the label.
  Caption: string voption
  /// Optional count at the end of the row.
  Count: int voption
  /// Rejects interaction for this row and dims its presentation.
  Disabled: bool
}

/// Selection behavior of a Metro list.
[<Struct>]
type MetroListSelectionMode =
  /// Rows do not select. Activation raises
  /// <see cref="MetroListEvent.ItemClick"/>.
  | Activate
  /// One row selects at a time.
  | Single
  /// Any number of rows select.
  | Multiple

/// Data-only properties for a Metro list view.
[<Struct; NoComparison>]
type MetroListViewProps = {
  /// Rows in display order.
  Items: MetroListItem array
  /// Identifiers of the selected rows. <see cref="MetroListSelectionMode.Activate"/>
  /// ignores this field.
  SelectedIds: string array
  /// Selection behavior.
  Mode: MetroListSelectionMode
  /// Row height in logical pixels. A value that is not finite and positive
  /// selects <see cref="MetroListView.defaultRowHeightPx"/>.
  RowHeight: float
  /// Height of the list. <c>ValueNone</c> lets the list grow and shrink to fill
  /// the free space of its parent.
  Height: Length voption
  /// Optional accessible name of the list.
  AccessibilityName: string voption
  /// Groups rows under one non-selectable header per key when set, matching
  /// the metrino <c>group-key</c> behavior: keys sort with the current
  /// culture, each header shows its key, and a null or empty key files its
  /// row under <c>"#"</c>. <c>ValueNone</c> renders one flat row list.
  GroupKey: (MetroListItem -> string) voption
}

/// Events raised by a Metro list view.
[<Struct; NoComparison>]
type MetroListEvent =
  /// The selection changed. An empty array clears the selection.
  | SelectionChanged of ids: string array
  /// The user activated a row.
  | ItemClick of id: string

/// Factory for Metro list views. The host owns the selection: update the props
/// after each event and rebuild the owner cell. Create one factory for each
/// list view, outside <c>Build</c>. The events do not name the list view, so
/// two list views that share one factory send their events to one handler.
type IMetroListViewFactory =

  /// Returns the widget input for <paramref name="props"/>, with the callbacks
  /// and the Metro row style of this factory. While <c>Items</c> and
  /// <c>GroupKey</c> are the same instances, the mapped rows and sections
  /// are also the same instances, so an unchanged input does not rebuild
  /// the list.
  abstract CreateInput: props: MetroListViewProps -> ListViewInput

  /// Mounts a Metro list view under the stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroListViewProps -> Blob

/// Property mapping for the Metro list view, a virtual list with sharp rows,
/// a 3 px accent indicator on the start edge of each selected row, and an
/// accent tint on selected rows.
[<RequireQualifiedAccess>]
module MetroListView =

  /// Row height in logical pixels when <c>RowHeight</c> is not set.
  val defaultRowHeightPx: float

  /// Width in logical pixels of the accent indicator on a selected row.
  val indicatorWidthPx: float

  /// Creates <see cref="MetroListViewProps"/> for <paramref name="items"/> with
  /// single selection, no selection, and the default row height.
  val defaults: items: MetroListItem array -> MetroListViewProps

  /// Maps <paramref name="props"/> to the <see cref="ListViewInput"/> data,
  /// without callbacks. <see cref="IMetroListViewFactory.CreateInput"/> adds the
  /// callbacks.
  val toInput: props: MetroListViewProps -> ListViewInput

  /// Creates a factory that raises <paramref name="onEvent"/> for selection
  /// changes and row activation. One factory gives every input the same
  /// callback instances, so an unchanged input does not rebuild the list.
  val createFactory: onEvent: (MetroListEvent -> unit) -> IMetroListViewFactory
