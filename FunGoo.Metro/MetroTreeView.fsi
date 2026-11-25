namespace FunGoo.Metro

open Goo
open Goo.Widgets.Data

/// Descriptor for a hierarchical node in a <see cref="MetroTreeView"/>.
[<Struct; NoComparison>]
type MetroTreeNode = {
  /// Stable unique node identifier across all levels of the tree.
  Id: string
  /// Visible node label.
  Label: string
  /// Optional Material symbol icon name rendered before the label.
  IconName: string voption
  /// Child nodes nested beneath this node (empty array for leaf nodes).
  Children: MetroTreeNode array
  /// Disables selection and expansion of this node and its descendants when
  /// <c>true</c>.
  Disabled: bool
}

/// Selection mode for <see cref="MetroTreeView"/>.
[<Struct>]
type MetroTreeSelectionMode =
  /// At most one tree node may be selected at a time.
  | Single
  /// Zero or more tree nodes may be selected simultaneously.
  | Multiple

/// Data-only properties for mounting a Metro hierarchical tree view.
[<Struct; NoComparison>]
type MetroTreeViewProps = {
  /// Root-level tree nodes.
  Nodes: MetroTreeNode array
  /// Identifiers of currently selected nodes.
  SelectedIds: string array
  /// Identifiers of currently expanded branch nodes.
  ExpandedIds: string array
  /// Selection mode (<see cref="MetroTreeSelectionMode.Single"/> or
  /// <see cref="MetroTreeSelectionMode.Multiple"/>).
  Mode: MetroTreeSelectionMode
  /// Row height in logical pixels (defaults to <c>36.0</c>).
  RowHeight: float
  /// Per-level indentation in logical pixels (defaults to <c>20.0</c>).
  Indent: float
  /// Optional explicit control width.
  Width: Length voption
  /// Optional explicit control height.
  Height: Length voption
  /// Disables all interaction when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding <c>"Tree"</c>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="IMetroTreeViewFactory"/>.
[<Struct; NoComparison>]
type MetroTreeViewEvent =
  /// The selected node identifiers changed.
  | SelectionChanged of ids: string array
  /// A branch node requested expanding or collapsing.
  | ExpandedChanged of id: string * expanded: bool

/// Stateful Metro tree view cell with an animated expand surface.
/// <c>MetroTreeView.create</c> makes the cell. The type holds the surface
/// that hosts and tests use.
[<AbstractClass>]
type MetroTreeViewCell =
  inherit Cell<MetroTreeViewProps>

  /// Current paint-bound content translation animation. A test reads it.
  abstract ContentTranslate: Anim<Point>

  /// Current paint-bound content opacity animation. A test reads it.
  abstract ContentOpacity: Anim<float>

  /// The tree of the current input: the animated surface wrapping the
  /// mounted <see cref="Goo.Widgets.Data.TreeView"/>. A test reads it, and
  /// only inside a build, where the Metro theme is readable.
  abstract View: Blob

/// Factory for Metro hierarchical tree views backed by
/// <see cref="Goo.Widgets.Data.TreeView"/>. Create one factory per tree view,
/// outside <c>Build</c>, so callback delegates and mapped
/// <see cref="TreeNode"/> arrays stay cached across parent builds.
type IMetroTreeViewFactory =

  /// Returns the <see cref="TreeViewInput"/> for <paramref name="props"/>,
  /// reusing cached callbacks and mapped <see cref="TreeNode"/> instances while
  /// <c>Nodes</c> is the same array reference.
  abstract CreateInput: props: MetroTreeViewProps -> TreeViewInput

  /// Mounts a Metro tree view under stable <paramref name="key"/>.
  /// Expand and collapse changes animate the tree container surface; see
  /// <see cref="MetroTreeView.create"/> for the motion contract.
  abstract Mount: key: string * props: MetroTreeViewProps -> Blob

/// Pure mappers and factory constructors for Metro hierarchical tree views.
[<RequireQualifiedAccess>]
module MetroTreeView =

  /// Default row height (<c>36.0</c> logical pixels).
  val defaultRowHeightPx: float

  /// Default per-level indentation (<c>20.0</c> logical pixels).
  val defaultIndentPx: float

  /// Width of the start-edge accent indicator on selected rows (<c>3.0</c> px).
  val indicatorWidthPx: float

  /// Helper creating a leaf <see cref="MetroTreeNode"/> with no children.
  val leaf: id: string * label: string -> MetroTreeNode

  /// Helper creating a branch <see cref="MetroTreeNode"/> with
  /// <paramref name="children"/>.
  val branch:
    id: string * label: string * children: MetroTreeNode array -> MetroTreeNode

  /// Creates default <see cref="MetroTreeViewProps"/> for
  /// <paramref name="nodes"/> in single-selection mode.
  val defaults: nodes: MetroTreeNode array -> MetroTreeViewProps

  /// Purely maps <paramref name="props"/> to <see cref="TreeViewInput"/> data
  /// fields without callbacks.
  val toData: props: MetroTreeViewProps -> TreeViewInput

  /// Toggles <paramref name="id"/> in <paramref name="expandedIds"/> according
  /// to <paramref name="expanded"/>, returning a new array without mutating the
  /// input array.
  val applyExpanded:
    expandedIds: string array * id: string * expanded: bool -> string array

  /// Creates an <see cref="IMetroTreeViewFactory"/> delivering events to
  /// <paramref name="onEvent"/>. Because <see cref="TreeView"/> delivers its
  /// callbacks through <c>InvokeInputCallback</c>, the owner rule rebuilds the
  /// host cell automatically.
  val createFactory:
    onEvent: (MetroTreeViewEvent -> unit) -> IMetroTreeViewFactory

  /// Creates a tree view cell delivering events to <paramref name="onEvent"/>.
  /// Because <see cref="TreeView"/> delivers its callbacks through
  /// <c>InvokeInputCallback</c>, the owner rule rebuilds the host cell
  /// automatically.
  /// When <c>ExpandedIds</c> changes, the cell plays a short vertical slide
  /// with a fade of the tree container surface at
  /// <c>MetroMotion.fast</c> through <c>MetroMotion.forTheme</c>.
  /// <see cref="TreeView"/> row hooks (<c>ConfigureRow</c>) carry no
  /// per-row animation lifecycle, so the cell animates the container
  /// surface instead of the changed region.
  /// Selection-only changes and the first mount snap to rest with no
  /// animation. Reduced motion snaps instantly with no animation.
  val create: onEvent: (MetroTreeViewEvent -> unit) -> MetroTreeViewCell
