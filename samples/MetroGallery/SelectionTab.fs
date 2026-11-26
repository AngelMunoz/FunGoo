module MetroGallery.SelectionTab

open Goo
open Goo.Widgets.Data
open FunGoo.Children
open FunGoo.Metro

/// Gallery tab with a Metro tree view. The picker half of this tab moved to
/// the pick section of the input page. The tab owns its selection, and the
/// headless check of the gallery reads the values through these members.
[<AbstractClass>]
type SelectionTabCell() =
  inherit Cell()

  /// Widget input of the tree view.
  abstract TreeInput: TreeViewInput

  /// Identifiers of the selected tree nodes.
  abstract SelectedTreeIds: string array

  /// Identifiers of the expanded tree nodes.
  abstract ExpandedTreeIds: string array

  /// Text of the last action of the tab.
  abstract LastAction: string

  /// The number of builds, for the headless check.
  abstract Builds: int

/// The tree nodes of the tab.
module Content =

  let treeNodes: MetroTreeNode array = [|
    {
      MetroTreeView.branch(
        "src",
        "FunGoo.Metro",
        [|
          {
            MetroTreeView.leaf("theme", "MetroTheme.fs") with
                IconName = ValueSome "palette"
          }
          {
            MetroTreeView.leaf("pivot", "MetroPivot.fs") with
                IconName = ValueSome "tab"
          }
          {
            MetroTreeView.leaf("tree", "MetroTreeView.fs") with
                IconName = ValueSome "account_tree"
          }
        |]
      ) with
          IconName = ValueSome "folder"
    }
    {
      MetroTreeView.branch(
        "samples",
        "samples",
        [|
          {
            MetroTreeView.leaf("gallery", "MetroGallery") with
                IconName = ValueSome "rocket_launch"
          }
        |]
      ) with
          IconName = ValueSome "folder"
    }
  |]

/// State of the tab. One mutable field per concern.
type SelectionState = {
  mutable TreeSelected: string array
  mutable TreeExpanded: string array
  mutable LastTreeInput: TreeViewInput
  mutable LastAction: string
  mutable Builds: int
}

/// Applies one tree event to the state of the tab.
let applyTree(state: SelectionState, event: MetroTreeViewEvent) : unit =
  match event with
  | MetroTreeViewEvent.SelectionChanged ids ->
    state.TreeSelected <- ids
    let chosen = if ids.Length > 0 then ids[0] else "none"
    state.LastAction <- $"Tree node: {chosen}"
  | MetroTreeViewEvent.ExpandedChanged(id, expanded) ->
    state.TreeExpanded <-
      MetroTreeView.applyExpanded(state.TreeExpanded, id, expanded)

    let verb = if expanded then "Expanded" else "Collapsed"
    state.LastAction <- $"{verb} {id}"

/// The props of the tree view for <paramref name="state"/>.
let treeProps(state: SelectionState) : MetroTreeViewProps = {
  MetroTreeView.defaults Content.treeNodes with
      SelectedIds = state.TreeSelected
      ExpandedIds = state.TreeExpanded
      Height = ValueSome(Length.Pixels 240.0)
      AccessibilityName = ValueSome "Solution Explorer"
}

/// The tree of the tab.
let view(state: SelectionState, tree: IMetroTreeViewFactory) : Blob =
  state.LastTreeInput <- tree.CreateInput(treeProps state)

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 16.0
  )
    .Children(
      tree.Mount("gallery-tree-view", treeProps state),
      MetroText.caption state.LastAction
    )

/// Creates the selection tab. The tree gives each callback of its input to
/// this tab through `InvokeInputCallback`, so the owner rule rebuilds the
/// tab. Thus a handler writes the state, and no handler calls `Rebuild()`.
let create() : SelectionTabCell =
  let state: SelectionState = {
    TreeSelected = [| "pivot" |]
    TreeExpanded = [| "src" |]
    LastTreeInput = Unchecked.defaultof<TreeViewInput>
    LastAction = "Ready"
    Builds = 0
  }

  let tree = MetroTreeView.createFactory(fun ev -> applyTree(state, ev))

  {
    new SelectionTabCell() with
      override _.TreeInput = state.LastTreeInput
      override _.SelectedTreeIds = state.TreeSelected
      override _.ExpandedTreeIds = state.TreeExpanded
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, tree)
  }
