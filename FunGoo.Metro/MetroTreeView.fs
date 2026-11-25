namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Data
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroTreeNode = {
  Id: string
  Label: string
  IconName: string voption
  Children: MetroTreeNode array
  Disabled: bool
}

[<Struct>]
type MetroTreeSelectionMode =
  | Single
  | Multiple

[<Struct; NoComparison>]
type MetroTreeViewProps = {
  Nodes: MetroTreeNode array
  SelectedIds: string array
  ExpandedIds: string array
  Mode: MetroTreeSelectionMode
  RowHeight: float
  Indent: float
  Width: Length voption
  Height: Length voption
  Disabled: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroTreeViewEvent =
  | SelectionChanged of ids: string array
  | ExpandedChanged of id: string * expanded: bool

[<AbstractClass>]
type MetroTreeViewCell() =
  inherit Cell<MetroTreeViewProps>()

  abstract ContentTranslate: Anim<Point>

  abstract ContentOpacity: Anim<float>

  abstract View: Blob

type IMetroTreeViewFactory =

  abstract CreateInput: props: MetroTreeViewProps -> TreeViewInput

  abstract Mount: key: string * props: MetroTreeViewProps -> Blob

[<RequireQualifiedAccess>]
module MetroTreeView =

  let defaultRowHeightPx: float = 36.0
  let defaultIndentPx: float = 20.0
  let indicatorWidthPx: float = 3.0
  let slideOffsetPx: float = 12.0
  let treeKey = "tree"
  let zeroRadius = Length.Pixels 0.0

  let leaf(id: string, label: string) : MetroTreeNode = {
    Id = id
    Label = label
    IconName = ValueNone
    Children = Array.empty
    Disabled = false
  }

  let branch
    (id: string, label: string, children: MetroTreeNode array)
    : MetroTreeNode = {
    Id = id
    Label = label
    IconName = ValueNone
    Children = (if isNull children then Array.empty else children)
    Disabled = false
  }

  let defaults(nodes: MetroTreeNode array) : MetroTreeViewProps = {
    Nodes = nodes
    SelectedIds = Array.empty
    ExpandedIds = Array.empty
    Mode = MetroTreeSelectionMode.Single
    RowHeight = defaultRowHeightPx
    Indent = defaultIndentPx
    Width = ValueNone
    Height = ValueNone
    Disabled = false
    AccessibilityName = ValueNone
  }

  let applyExpanded
    (expandedIds: string array, id: string, expanded: bool)
    : string array =
    let current = if isNull expandedIds then Array.empty else expandedIds

    let contains =
      let mutable found = false
      let mutable i = 0

      while i < current.Length && not found do
        found <- current[i] = id
        i <- i + 1

      found

    if expanded then
      if contains then current else Array.append current [| id |]
    elif not contains then
      current
    else
      current |> Array.filter(fun existing -> existing <> id)

  let rec mapNode
    (icons: Dictionary<string, string>, node: MetroTreeNode)
    : TreeNode =
    node.IconName
    |> ValueOption.iter(fun icon ->
      if
        not(String.IsNullOrEmpty icon) && not(String.IsNullOrEmpty node.Id)
      then
        icons[node.Id] <- icon)

    let mappedChildren =
      if isNull node.Children || node.Children.Length = 0 then
        null
      else
        node.Children |> Array.map(fun child -> mapNode(icons, child))

    TreeNode(
      Id = node.Id,
      Label = (if String.IsNullOrEmpty node.Label then null else node.Label),
      Disabled = node.Disabled,
      Children = mappedChildren
    )

  let mapNodes
    (nodes: MetroTreeNode array)
    : struct (TreeNode array * Dictionary<string, string>) =
    let icons = Dictionary<string, string>(StringComparer.Ordinal)

    let mapped =
      if isNull nodes || nodes.Length = 0 then
        Array.empty
      else
        nodes |> Array.map(fun node -> mapNode(icons, node))

    struct (mapped, icons)

  let dataOf(props: MetroTreeViewProps, nodes: TreeNode array) : TreeViewInput =
    let rowHeight =
      if Double.IsFinite props.RowHeight && props.RowHeight > 0.0 then
        props.RowHeight
      else
        defaultRowHeightPx

    let indent =
      if Double.IsFinite props.Indent && props.Indent >= 0.0 then
        props.Indent
      else
        defaultIndentPx

    let selectedIds =
      if isNull props.SelectedIds then
        Array.empty
      else
        props.SelectedIds

    let expandedIds =
      if isNull props.ExpandedIds then
        Array.empty
      else
        props.ExpandedIds

    TreeViewInput(
      Nodes = nodes,
      SelectedIds = selectedIds,
      ExpandedIds = expandedIds,
      Multiple = (props.Mode = MetroTreeSelectionMode.Multiple),
      RowHeight = Nullable rowHeight,
      Indent = Nullable indent,
      Width =
        (props.Width
         |> ValueOption.map Nullable
         |> ValueOption.defaultValue(Nullable())),
      Height =
        (props.Height
         |> ValueOption.map Nullable
         |> ValueOption.defaultValue(Nullable())),
      Disabled = props.Disabled,
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName
    )

  let toData(props: MetroTreeViewProps) : TreeViewInput =
    let struct (nodes, _) = mapNodes props.Nodes
    dataOf(props, nodes)

  type TreeHandlers = {
    OnSelectionChange: Action<string array>
    OnExpandedChange: Action<string, bool>
    ConfigureRow: Action<TreeViewInput, TreeViewRow, Container>
    ConfigureRoot: Action<TreeViewInput, Container>
    CreateContent: Func<TreeViewInput, TreeViewRow, Blob, Blob>
  }

  type FactoryState = {
    mutable Icons: Dictionary<string, string>
    mutable Mapped: (MetroTreeNode array * TreeNode array) voption
    mutable Handlers: TreeHandlers
    OnEvent: MetroTreeViewEvent -> unit
  }

  type TreeCellState = {
    mutable Input: MetroTreeViewProps
    mutable LastExpanded: string array voption
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
  }

  type TreeCellContext = {
    Shared: FactoryState
    State: TreeCellState
  }

  let styleRow
    (_input: TreeViewInput, row: TreeViewRow, root: Container)
    : unit =
    let theme = Metro.read()
    root.BorderRadius <- zeroRadius
    root.BorderStartWidth <- Length.Pixels indicatorWidthPx

    if row.Selected then
      let p = theme.Palette
      let accent = theme.Accent.Base

      root.BorderStartColor <-
        (if theme.HighContrast then p.Foreground else accent)

      root.BackgroundColor <-
        (if theme.HighContrast then
           p.Highlight
         else
           accent.WithAlpha(0.3))
    else
      root.BorderStartColor <- Color.Transparent

  let styleRoot(_input: TreeViewInput, root: Container) : unit =
    root.BorderRadius <- zeroRadius

  let decorateContent
    (state: FactoryState, row: TreeViewRow, defaultContent: Blob)
    : Blob =
    let mutable iconName = ""

    if
      not(isNull row.Node.Id) && state.Icons.TryGetValue(row.Node.Id, &iconName)
    then
      let theme = Metro.read()
      let p = theme.Palette
      let fg = if row.Disabled then p.ForegroundDisabled else p.Foreground

      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 8.0,
        MinWidth = Length.Pixels 0.0
      )
        .Children(
          MaterialIcons.Create(iconName, 16.0, Nullable fg),
          defaultContent
        )
      :> Blob
    else
      defaultContent

  let nodesFor
    (state: FactoryState, props: MetroTreeViewProps)
    : TreeNode array =
    match state.Mapped with
    | ValueSome(cachedSource, cachedNodes) when
      obj.ReferenceEquals(props.Nodes, cachedSource)
      ->
      cachedNodes
    | _ ->
      let struct (mapped, icons) = mapNodes props.Nodes
      state.Icons <- icons
      state.Mapped <- ValueSome(props.Nodes, mapped)
      mapped

  let createInput
    (state: FactoryState, props: MetroTreeViewProps)
    : TreeViewInput =
    let data = dataOf(props, nodesFor(state, props))
    let h = state.Handlers

    TreeViewInput(
      Nodes = data.Nodes,
      SelectedIds = data.SelectedIds,
      ExpandedIds = data.ExpandedIds,
      Multiple = data.Multiple,
      OnSelectionChange = h.OnSelectionChange,
      OnExpandedChange = h.OnExpandedChange,
      RowHeight = data.RowHeight,
      Indent = data.Indent,
      Width = data.Width,
      Height = data.Height,
      Disabled = data.Disabled,
      AccessibilityName = data.AccessibilityName,
      ConfigureRow = h.ConfigureRow,
      ConfigureRoot = h.ConfigureRoot,
      CreateContent = h.CreateContent
    )

  let mount
    (state: FactoryState, key: string, props: MetroTreeViewProps)
    : Blob =
    Cell.Mount<TreeViewInput, TreeView>(key, createInput(state, props))

  let freshShared(onEvent: MetroTreeViewEvent -> unit) : FactoryState =
    let shared: FactoryState = {
      Icons = Dictionary<string, string>(StringComparer.Ordinal)
      Mapped = ValueNone
      Handlers = Unchecked.defaultof<TreeHandlers>
      OnEvent = onEvent
    }

    // The compiler requires delegate constructors in record fields (FS0002).
    shared.Handlers <- {
      OnSelectionChange =
        Action<string array>(fun ids ->
          onEvent(MetroTreeViewEvent.SelectionChanged ids))
      OnExpandedChange =
        Action<string, bool>(fun id expanded ->
          onEvent(MetroTreeViewEvent.ExpandedChanged(id, expanded)))
      ConfigureRow =
        Action<TreeViewInput, TreeViewRow, Container>(fun input row root ->
          styleRow(input, row, root))
      ConfigureRoot =
        Action<TreeViewInput, Container>(fun input root ->
          styleRoot(input, root))
      CreateContent =
        Func<TreeViewInput, TreeViewRow, Blob, Blob>(fun _ row content ->
          decorateContent(shared, row, content))
    }

    shared

  let expandedOf(props: MetroTreeViewProps) : string array =
    if isNull props.ExpandedIds then
      Array.empty
    else
      props.ExpandedIds

  let sameExpanded(previous: string array, next: string array) : bool =
    if obj.ReferenceEquals(previous, next) then true
    elif previous.Length <> next.Length then false
    else (previous |> Array.sort) = (next |> Array.sort)

  let initialCellState() : TreeCellState = {
    Input = defaults(Array.empty)
    LastExpanded = ValueNone
    LastReducedMotion = false
    ScopedReducedMotion = false
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  let snapToRest(state: TreeCellState) : unit =
    state.Translate.Snap(Point(X = 0.0, Y = 0.0))
    state.Opacity.Snap(1.0)

  let playExpand(state: TreeCellState, theme: MetroTheme) : unit =
    let spec = MetroMotion.forTheme(theme, MetroMotion.fast)

    state.Translate.Play(
      Point(X = 0.0, Y = -slideOffsetPx),
      Point(X = 0.0, Y = 0.0),
      spec
    )

    state.Opacity.Play(0.0, 1.0, spec)

  let viewOf(ctx: TreeCellContext) : Blob =
    Container(
      TranslateAnim = ctx.State.Translate,
      OpacityAnim = ctx.State.Opacity
    )
      .Children(mount(ctx.Shared, treeKey, ctx.State.Input))

  let build
    (ctx: TreeCellContext, input: MetroTreeViewProps, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion
    let next = expandedOf input

    let changed =
      state.LastExpanded
      |> ValueOption.map(fun prev -> not(sameExpanded(prev, next)))
      |> ValueOption.defaultValue false

    if theme.ReducedMotion || state.LastExpanded.IsNone then
      snapToRest state
    elif changed then
      let reduced =
        state.ScopedReducedMotion
        || ValueOption.defaultValue state.LastReducedMotion hostReduced

      if reduced then
        snapToRest state
      else
        playExpand(state, theme)

    state.LastExpanded <- ValueSome next
    viewOf ctx

  let unmount(state: TreeCellState) : unit =
    state.LastExpanded <- ValueNone
    state.ScopedReducedMotion <- false
    snapToRest state

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let createWith(shared: FactoryState) : MetroTreeViewCell =
    let state = initialCellState()
    let ctx: TreeCellContext = { Shared = shared; State = state }

    let cell = {
      new MetroTreeViewCell() with
        override _.ContentTranslate = state.Translate
        override _.ContentOpacity = state.Opacity
        override _.View = viewOf ctx

        override this.Build(input) : Blob =
          build(ctx, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state
    }

    // The cell exists now: Goo ties each animation to it.
    state.Translate <- cell.AnimateValue(Point(X = 0.0, Y = 0.0))
    state.Opacity <- cell.AnimateValue(1.0)
    cell

  let mountCell
    (shared: FactoryState, key: string, props: MetroTreeViewProps)
    : Blob =
    Cell.Mount<MetroTreeViewProps, MetroTreeViewCell>(
      key,
      props,
      fun () -> createWith shared
    )

  let create(onEvent: MetroTreeViewEvent -> unit) : MetroTreeViewCell =
    createWith(freshShared onEvent)

  let createFactory
    (onEvent: MetroTreeViewEvent -> unit)
    : IMetroTreeViewFactory =
    let shared = freshShared onEvent

    {
      new IMetroTreeViewFactory with
        member _.CreateInput(props) = createInput(shared, props)
        member _.Mount(key, props) = mountCell(shared, key, props)
    }
