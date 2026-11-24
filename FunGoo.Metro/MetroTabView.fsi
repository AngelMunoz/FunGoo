namespace FunGoo.Metro

open Goo
open Goo.Widgets.Navigation

/// Data-only description of one tab in a <see cref="MetroTabView"/>.
[<Struct; NoComparison>]
type MetroTabItem = {
  /// Unique tab identifier.
  Id: string
  /// Visible tab header text.
  Header: string
  /// Optional leading Material icon name.
  IconName: string voption
  /// When <c>true</c>, renders a close button on the tab when closing is
  /// enabled.
  Closable: bool
  /// When <c>true</c>, disables tab selection.
  Disabled: bool
}

/// Event emitted by a <see cref="IMetroTabViewFactory"/>.
[<Struct; NoComparison>]
type MetroTabViewEvent =
  | Selected of id: string
  | Closed of id: string
  | AddRequested

/// Data-only properties of a <see cref="MetroTabView"/>.
[<Struct; NoComparison>]
type MetroTabViewProps = {
  /// Ordered tab entries.
  Items: MetroTabItem array
  /// Currently selected tab identifier.
  SelectedId: string
  /// When <c>true</c>, displays a trailing <c>+</c> add-tab button.
  ShowAddButton: bool
  /// Optional accessible name for the tab list.
  AccessibilityName: string voption
}

/// Service factory wrapping <see cref="TabBar"/> with flat Metro <c>0 px</c>
/// styling, cached <see cref="NavigationItem"/> arrays, and cached callback
/// delegates. Create once outside <c>Build</c> per tab strip.
type IMetroTabViewFactory =
  /// Builds a <see cref="TabBarInput"/> snapshot with cached items and
  /// callbacks. While <c>Items</c> is the same array instance, the mapped items
  /// are the same instance also.
  abstract CreateInput: props: MetroTabViewProps -> TabBarInput

  /// Mounts the Metro tab strip under <paramref name="key"/> and pairs it with
  /// the active tab panel built by <paramref name="renderPanel"/>.
  abstract Build:
    key: string * props: MetroTabViewProps * renderPanel: (string -> Blob) ->
      Blob

/// Flat Metro <c>TabView</c> builders and <see cref="IMetroTabViewFactory"/>
/// constructors.
[<RequireQualifiedAccess>]
module MetroTabView =

  /// Creates a closable tab item.
  val item: id: string * header: string -> MetroTabItem

  /// Creates a non-closable (pinned) tab item.
  val pinnedItem: id: string * header: string -> MetroTabItem

  /// Creates a closable tab item with a leading Material icon.
  val iconItem: id: string * header: string * iconName: string -> MetroTabItem

  /// Returns default tab-view properties for <paramref name="items"/> and
  /// <paramref name="selectedId"/>.
  val defaults:
    items: MetroTabItem array * selectedId: string -> MetroTabViewProps

  /// Creates a reusable <see cref="IMetroTabViewFactory"/>.
  val createFactory:
    onEvent: (MetroTabViewEvent -> unit) -> IMetroTabViewFactory
