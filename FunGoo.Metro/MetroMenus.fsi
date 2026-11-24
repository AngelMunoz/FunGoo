namespace FunGoo.Metro

open Goo
open Goo.Widgets.Layout
open Goo.Widgets.Navigation

/// Command entry inside a <see cref="MetroMenuFlyout"/> or
/// <see cref="MetroContextMenu"/>.
[<Struct; NoComparison>]
type MetroMenuCommand = {
  /// Stable unique identifier across all menu and submenu levels.
  Id: string
  /// Visible command label.
  Label: string
  /// Optional Material symbol icon name rendered before the label.
  IconName: string voption
  /// Optional display keyboard chord shown on the item.
  Shortcut: KeyBinding voption
  /// Optional nested submenu entries (empty array for leaf commands).
  Children: MetroMenuEntry array
  /// Disables activation when <c>true</c>.
  Disabled: bool
}

/// Entry inside a <see cref="MetroMenuFlyout"/> or <see cref="MetroContextMenu"/>.
and [<Struct; NoComparison>] MetroMenuEntry =
  /// Selectable command or submenu parent.
  | Command of item: MetroMenuCommand
  /// Non-interactive horizontal divider with a unique <c>id</c>.
  | Separator of id: string

/// User interaction emitted by <see cref="IMetroMenuFlyoutFactory"/> and
/// <see cref="IMetroContextMenuFactory"/>.
[<Struct; NoComparison>]
type MetroMenuEvent =
  /// A leaf command with identifier <c>id</c> was activated.
  | ItemClicked of id: string
  /// The menu requested opening or closing.
  | OpenChanged of isOpen: bool

/// Data-only properties for an anchor-triggered Metro menu flyout.
[<Struct; NoComparison>]
type MetroMenuFlyoutProps = {
  /// Displays the menu popup when <c>true</c>.
  Open: bool
  /// Mounted anchor handle to position the menu beside.
  Anchor: ElementHandle
  /// Root menu entries.
  Items: MetroMenuEntry array
  /// Preferred placement relative to <see cref="Anchor"/>
  /// (<see cref="PopoverPlacement.BottomStart"/> by default).
  Placement: PopoverPlacement
  /// Optional explicit menu width.
  Width: Length voption
  /// Optional accessible name overriding <c>"Menu"</c>.
  AccessibilityName: string voption
}

/// Factory for anchor-triggered Metro menu flyouts backed by
/// <see cref="Goo.Widgets.Navigation.Menu"/>. Create one factory per menu,
/// outside <c>Build</c>, so delegates and mapped <see cref="MenuItem"/> arrays
/// stay cached across builds.
type IMetroMenuFlyoutFactory =

  /// Returns the <see cref="MenuInput"/> for <paramref name="props"/> with
  /// cached callbacks and mapped items. While <c>Items</c> is the same array
  /// instance, the mapped items are the same instance also.
  abstract CreateInput: props: MetroMenuFlyoutProps -> MenuInput

  /// Mounts the Metro menu flyout under stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroMenuFlyoutProps -> Blob

/// Pure helpers and factory constructors for Metro menu flyouts.
[<RequireQualifiedAccess>]
module MetroMenuFlyout =

  /// Helper creating a leaf <see cref="MetroMenuEntry.Command"/>.
  val item: id: string * label: string -> MetroMenuEntry

  /// Helper creating a leaf <see cref="MetroMenuEntry.Command"/> with a
  /// Material symbol icon.
  val iconItem: id: string * label: string * iconName: string -> MetroMenuEntry

  /// Helper creating a submenu <see cref="MetroMenuEntry.Command"/>.
  val submenu:
    id: string * label: string * children: MetroMenuEntry array ->
      MetroMenuEntry

  /// Helper creating a <see cref="MetroMenuEntry.Separator"/>.
  val separator: id: string -> MetroMenuEntry

  /// Creates default <see cref="MetroMenuFlyoutProps"/> for
  /// <paramref name="anchor"/> and <paramref name="items"/>.
  val defaults:
    anchor: ElementHandle * items: MetroMenuEntry array * isOpen: bool ->
      MetroMenuFlyoutProps

  /// Purely maps <paramref name="entries"/> to <see cref="MenuItem"/> values.
  val mapEntries: entries: MetroMenuEntry array -> MenuItem array

  /// Creates an <see cref="IMetroMenuFlyoutFactory"/> delivering events to
  /// <paramref name="onEvent"/>.
  val createFactory:
    onEvent: (MetroMenuEvent -> unit) -> IMetroMenuFlyoutFactory

/// Data-only properties for a pointer-positioned Metro context menu.
[<Struct; NoComparison>]
type MetroContextMenuProps = {
  /// Displays the context menu when <c>true</c>.
  Open: bool
  /// Origin point in window logical pixels where the context menu opens.
  Position: Point
  /// Root menu entries.
  Items: MetroMenuEntry array
  /// Optional explicit menu width.
  Width: Length voption
  /// Optional accessible name overriding <c>"Context menu"</c>.
  AccessibilityName: string voption
}

/// Factory for pointer-positioned Metro context menus backed by
/// <see cref="Goo.Widgets.Navigation.Menu"/>. Create one factory per context
/// menu, outside <c>Build</c>.
type IMetroContextMenuFactory =

  /// Returns the <see cref="MenuInput"/> for <paramref name="props"/> with
  /// cached callbacks and mapped items. While <c>Items</c> is the same array
  /// instance, the mapped items are the same instance also.
  abstract CreateInput: props: MetroContextMenuProps -> MenuInput

  /// Mounts the Metro context menu under stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroContextMenuProps -> Blob

/// Pure helpers, secondary-click target wrapper, and factory constructors for
/// Metro context menus.
[<RequireQualifiedAccess>]
module MetroContextMenu =

  /// Creates default <see cref="MetroContextMenuProps"/> at
  /// <paramref name="position"/> for <paramref name="items"/>.
  val defaults:
    position: Point * items: MetroMenuEntry array * isOpen: bool ->
      MetroContextMenuProps

  /// Wraps <paramref name="child"/> in a container that invokes
  /// <paramref name="onRequest"/> with <c>WindowPosition</c> on secondary
  /// (right) pointer press.
  val target: child: Blob * onRequest: (Point -> unit) -> Blob

  /// Creates an <see cref="IMetroContextMenuFactory"/> delivering events to
  /// <paramref name="onEvent"/>.
  val createFactory:
    onEvent: (MetroMenuEvent -> unit) -> IMetroContextMenuFactory
