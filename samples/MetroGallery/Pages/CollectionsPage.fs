module MetroGallery.Pages.CollectionsPage

open System
open Goo
open Goo.Widgets.Data
open FunGoo.Children
open FunGoo.Metro

/// Demo data of the collections page. Items, tiles, flip content, zoom
/// groups, and tree nodes keep the pixel-equal values of the retired lists,
/// flip view, zoom, and selection tabs.
module Content =

  /// Groups rows by the first letter of their label, like a metrino
  /// group-key over the row data.
  let groupOf(item: MetroListItem) : string =
    if String.IsNullOrEmpty item.Label then
      "#"
    else
      item.Label.Substring(0, 1).ToUpperInvariant()

  let items = [|
    {
      Id = "mail"
      Label = "Mail"
      Caption = ValueSome "3 new messages"
      Count = ValueSome 3
      Disabled = false
    }
    {
      Id = "photos"
      Label = "Photos"
      Caption = ValueSome "Albums and recent pictures"
      Count = ValueNone
      Disabled = false
    }
    {
      Id = "maps"
      Label = "Maps"
      Caption = ValueSome "Offline city maps"
      Count = ValueNone
      Disabled = false
    }
    {
      Id = "music"
      Label = "Music"
      Caption = ValueSome "Now playing"
      Count = ValueNone
      Disabled = false
    }
    {
      Id = "news"
      Label = "News"
      Caption = ValueSome "Headlines for you"
      Count = ValueSome 12
      Disabled = false
    }
    {
      Id = "store"
      Label = "Store"
      Caption = ValueNone
      Count = ValueNone
      Disabled = true
    }
  |]

  let choices = [|
    {
      Id = "small"
      Label = "Small"
      Caption = ValueNone
      Count = ValueNone
      Disabled = false
    }
    {
      Id = "medium"
      Label = "Medium"
      Caption = ValueNone
      Count = ValueNone
      Disabled = false
    }
    {
      Id = "large"
      Label = "Large"
      Caption = ValueNone
      Count = ValueNone
      Disabled = false
    }
  |]

  let tiles = [|
    {
      Id = "photos"
      Label = "Photos"
      IconName = ValueSome "photo"
    }
    {
      Id = "maps"
      Label = "Maps"
      IconName = ValueSome "map"
    }
    {
      Id = "music"
      Label = "Music"
      IconName = ValueSome "music_note"
    }
    {
      Id = "mail"
      Label = "Mail"
      IconName = ValueSome "mail"
    }
    {
      Id = "news"
      Label = "News"
      IconName = ValueSome "feed"
    }
    {
      Id = "games"
      Label = "Games"
      IconName = ValueSome "sports_esports"
    }
    {
      Id = "weather"
      Label = "Weather"
      IconName = ValueSome "partly_cloudy_day"
    }
    {
      Id = "store"
      Label = "Store"
      IconName = ValueSome "shopping_bag"
    }
  |]

  let flipItems = [|
    {
      Id = "one"
      AccessibilityName = "One item at a time"
    }
    {
      Id = "two"
      AccessibilityName = "Glance and go"
    }
    {
      Id = "three"
      AccessibilityName = "Authentically digital"
    }
  |]

  let flipTextOf(id: string) : string * string =
    match id with
    | "one" ->
      "One item at a time",
      "Swipe, use the arrow keys, PageUp or PageDown, or the edge buttons."
    | "two" ->
      "Glance and go",
      "The slide shows the direction of each step, so the order stays clear."
    | _ ->
      "Authentically digital",
      "Light, typography, and motion. No borders, no shadows, no chrome."

  let zoomGroups = [| "photos"; "maps"; "music" |]

  let tilesOf(group: string) : string array =
    match group with
    | "photos" -> [| "camera"; "albums"; "recent" |]
    | "maps" -> [| "offline"; "city"; "saved" |]
    | _ -> [| "now playing"; "playlists"; "albums" |]

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

/// The factories of the list collections of the page. Each collection has
/// its own factory, because each one owns its own selection.
type ListFactories = {
  Single: IMetroListViewFactory
  Multiple: IMetroListViewFactory
  Grouped: IMetroListViewFactory
  Box: IMetroListViewFactory
  Grid: IMetroGridViewFactory
}

/// State of the page. One mutable field per concern.
type CollectionsState = {
  mutable SingleSelected: string array
  mutable MultipleSelected: string array
  mutable GroupedSelected: string array
  mutable BoxSelected: string array
  mutable GridSelected: string array
  mutable FlipSelected: string
  mutable ZoomedOut: bool
  mutable ZoomAction: string
  mutable LastDetail: Blob
  mutable LastOverview: Blob
  mutable ZoomRef: MetroSemanticZoomCell voption
  mutable TreeSelected: string array
  mutable TreeExpanded: string array
  mutable LastTreeInput: TreeViewInput
  mutable TreeAction: string
  mutable LastAction: string
  mutable Builds: int
}

/// The values that the zoom render functions work on.
type ZoomContext = {
  State: CollectionsState
  /// Rebuilds this page.
  RebuildPage: unit -> unit
}

/// The data of one tile button of the zoom section.
[<Struct; NoComparison>]
type TileLabel = {
  Key: string
  Label: string
  Role: MetroTextRole
}

/// The collections page. The headless check of the gallery reads the zoom
/// and the tree state through these members.
[<AbstractClass>]
type CollectionsPageCell() =
  inherit Cell()

  /// <c>true</c> when the page asks for the zoomed-out overview.
  abstract ZoomedOut: bool

  /// Text of the last zoom action, for the headless check of the gallery.
  abstract ZoomAction: string

  /// The latest detail tree that the zoom cell built.
  abstract LastDetail: Blob

  /// The latest overview tree that the zoom cell built.
  abstract LastOverview: Blob

  /// The mounted zoom cell.
  abstract ZoomCell: MetroSemanticZoomCell voption

  /// Widget input of the tree view.
  abstract TreeInput: TreeViewInput

  /// Identifiers of the selected tree nodes.
  abstract SelectedTreeIds: string array

  /// Identifiers of the expanded tree nodes.
  abstract ExpandedTreeIds: string array

  /// The number of builds, for the headless check of the gallery.
  abstract Builds: int

/// The handler of one list collection of the page. The collection gives
/// each event to this page through `InvokeInputCallback`, so the owner rule
/// rebuilds the page. The handler writes the state and does not call
/// `Rebuild()`.
let handlerOf
  (state: CollectionsState, select: string array -> unit)
  : MetroListEvent -> unit =
  function
  | MetroListEvent.SelectionChanged ids -> select ids
  | MetroListEvent.ItemClick id -> state.LastAction <- $"Activated {id}"

/// Applies one zoom request. The zoom cell gives each request to this page
/// through `InvokeInputCallback`, so the owner rule rebuilds this page.
let applyZoom(state: CollectionsState, event: MetroSemanticZoomEvent) : unit =
  state.ZoomedOut <- (event = MetroSemanticZoomEvent.ZoomOut)

  state.ZoomAction <- (if state.ZoomedOut then "Zoomed out" else "Zoomed in")

// The zoom cell builds the detail and the overview trees inside its own
// Build, so it owns the callbacks of their tiles. The owner rule then
// rebuilds the zoom cell and not this page. For that reason a tile callback
// that changes the state of this page also rebuilds this page.
let selectZoomTile(ctx: ZoomContext, group: string, name: string) : unit =
  ctx.State.ZoomAction <- $"Selected {name} in {group}"
  ctx.RebuildPage()

let selectZoomGroup(ctx: ZoomContext, group: string) : unit =
  ctx.State.ZoomedOut <- false
  ctx.State.ZoomAction <- $"Zoomed in on {group}"
  ctx.RebuildPage()

/// Applies one tree event to the state of the page.
let applyTree(state: CollectionsState, event: MetroTreeViewEvent) : unit =
  match event with
  | MetroTreeViewEvent.SelectionChanged ids ->
    state.TreeSelected <- ids
    let chosen = if ids.Length > 0 then ids[0] else "none"
    state.TreeAction <- $"Tree node: {chosen}"
  | MetroTreeViewEvent.ExpandedChanged(id, expanded) ->
    state.TreeExpanded <-
      MetroTreeView.applyExpanded(state.TreeExpanded, id, expanded)

    let verb = if expanded then "Expanded" else "Collapsed"
    state.TreeAction <- $"{verb} {id}"

/// The props of the tree view for <paramref name="state"/>.
let treeProps(state: CollectionsState) : MetroTreeViewProps = {
  MetroTreeView.defaults Content.treeNodes with
      SelectedIds = state.TreeSelected
      ExpandedIds = state.TreeExpanded
      Height = ValueSome(Length.Pixels 240.0)
      AccessibilityName = ValueSome "Solution Explorer"
}

/// One titled section of the page: a subheader in the type ramp, then the
/// content.
let section(title: string, content: Blob) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.subheader title, content)

/// One column of the list view section: a caption above one collection.
let listColumn(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 8.0
  )
    .Children(MetroText.caption title, content)

/// The content of one item of the flip view.
let flipItemOf(id: string) : Blob =
  let theme = Metro.read()
  let title, caption = Content.flipTextOf id

  Container(
    Key = $"flip-content-{id}",
    FlexDirection = FlexDirection.Column,
    JustifyContent = JustifyContent.FlexEnd,
    Width = Length.Percent 100.0,
    Height = Length.Percent 100.0,
    Padding = Edges(Length.Pixels 24.0),
    Gap = Length.Pixels 8.0,
    BackgroundColor = theme.Palette.BackgroundAlt,
    BorderWidth =
      Edges(
        Length.Pixels 6.0,
        Length.Pixels 0.0,
        Length.Pixels 0.0,
        Length.Pixels 0.0
      ),
    BorderColor = Edges<Color>(theme.Accent.Base)
  )
    .Children(
      MetroText.build {
        MetroText.defaults(title, MetroTextRole.Subheader) with
            Wrap = TextWrap.NoWrap
      },
      MetroText.secondary caption
    )

/// One tile button of the zoom section: a sharp rectangle with one label.
let zoomTileButton(label: TileLabel, onClick: unit -> unit) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  Button(
    Key = label.Key,
    Width = Length.Pixels 232.0,
    Height = Length.Pixels 72.0,
    AlignItems = AlignItems.Center,
    JustifyContent = JustifyContent.Center,
    Padding = Edges(Length.Pixels 16.0),
    FlexShrink = 0.0,
    BorderRadius = Length.Pixels 0.0,
    BorderWidth = Edges(Length.Pixels 0.0),
    BorderColor = Edges<Color>(Color.Transparent),
    BoxShadows = Array.empty,
    BackgroundColor = p.BackgroundAlt,
    HoverStyle = Style(BackgroundColor = p.Hover),
    ActiveStyle = Style(BackgroundColor = p.Active),
    FocusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      ),
    TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
    TransitionEasing = Easing.EaseOut,
    Cursor = Cursor.Pointer,
    Focusable = true,
    TabStop = true,
    OnClick = onClick,
    KeyBindings = [|
      KeyBinding(Key.Enter, KeyModifiers.None, onClick)
      KeyBinding(Key.Space, KeyModifiers.None, onClick)
    |],
    Accessibility =
      Accessibility(Role = AccessibilityRole.Button, Name = label.Label)
  )
    .Children(
      MetroText.build {
        MetroText.defaults(label.Label, label.Role) with
            Wrap = TextWrap.NoWrap
            Trimming = TextTrimming.Ellipsis
      }
    )

/// The detail level of the zoom section: one column of tiles per group.
let zoomDetailView(ctx: ZoomContext) : Blob =
  let groupsRow =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      AlignItems = AlignItems.FlexStart,
      Gap = Length.Pixels 24.0
    )

  for group in Content.zoomGroups do
    // Every child of a column is keyed: the header text and the tiles.
    let header = MetroText.subheader group
    header.Key <- $"group-header-{group}"

    let column =
      Container(
        Key = $"group-{group}",
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 8.0
      )
        .Children(header)

    for name in Content.tilesOf group do
      column.Add(
        zoomTileButton(
          {
            Key = $"tile-{group}-{name}"
            Label = name
            Role = MetroTextRole.Body
          },
          fun () -> selectZoomTile(ctx, group, name)
        )
      )

    groupsRow.Add(column)

  ctx.State.LastDetail <- groupsRow
  groupsRow

/// The overview level of the zoom section: one tile for each group.
let zoomOverviewView(ctx: ZoomContext) : Blob =
  let overview =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      Gap = Length.Pixels 24.0
    )

  for group in Content.zoomGroups do
    overview.Add(
      zoomTileButton(
        {
          Key = $"group-tile-{group}"
          Label = group
          Role = MetroTextRole.Header
        },
        fun () -> selectZoomGroup(ctx, group)
      )
    )

  ctx.State.LastOverview <- overview
  overview

let renderZoom(ctx: ZoomContext, isZoomedOut: bool) : Blob =
  if isZoomedOut then
    zoomOverviewView ctx
  else
    zoomDetailView ctx

/// The grouped list view section.
let groupedSection(state: CollectionsState, factories: ListFactories) : Blob =
  let groupedProps = {
    MetroListView.defaults Content.items with
        GroupKey = ValueSome Content.groupOf
        SelectedIds = state.GroupedSelected
        Height = ValueSome(Length.Pixels 300.0)
  }

  section(
    "grouped list view",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary
          "The grouped list files rows under one header per initial letter.",
        factories.Grouped.Mount("gallery-list-grouped", groupedProps)
      )
  )

/// The plain list view section: single and multiple selection side by side.
let listsSection(state: CollectionsState, factories: ListFactories) : Blob =
  let singleProps = {
    MetroListView.defaults Content.items with
        SelectedIds = state.SingleSelected
        Height = ValueSome(Length.Pixels 300.0)
  }

  let multipleProps = {
    MetroListView.defaults Content.items with
        Mode = MetroListSelectionMode.Multiple
        SelectedIds = state.MultipleSelected
        Height = ValueSome(Length.Pixels 300.0)
  }

  section(
    "list view",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary
          "The host owns the selection. Each selected row shows a 3 px accent \
           indicator on its start edge and an accent tint.",
        Container(
          FlexDirection = FlexDirection.Row,
          Width = Length.Percent 100.0,
          FlexShrink = 0.0,
          Gap = Length.Pixels 24.0
        )
          .Children(
            listColumn(
              "single selection",
              factories.Single.Mount("gallery-list-single", singleProps)
            ),
            listColumn(
              "multiple selection",
              factories.Multiple.Mount("gallery-list-multiple", multipleProps)
            )
          )
      )
  )

/// The list box section.
let boxSection(state: CollectionsState, factories: ListFactories) : Blob =
  let boxProps = {
    MetroListView.defaults Content.choices with
        SelectedIds = state.BoxSelected
        Height = ValueSome(Length.Pixels 160.0)
  }

  section(
    "list box",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary "The list box adds a 2 px border.",
        MetroListBox.build("gallery-list-box", boxProps, factories.Box)
      )
  )

/// The grid view section.
let gridSection(state: CollectionsState, factories: ListFactories) : Blob =
  let gridProps = {
    MetroGridView.defaults Content.tiles with
        SelectedIds = state.GridSelected
        TileSize = 96.0
        Height = ValueSome(Length.Pixels 216.0)
        AccessibilityName = ValueSome "Apps"
  }

  section(
    "grid view",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary
          "The grid view mounts only the tiles near the viewport.",
        factories.Grid.Build gridProps
      )
  )

/// The tree view section.
let treeSection(state: CollectionsState, tree: IMetroTreeViewFactory) : Blob =
  state.LastTreeInput <- tree.CreateInput(treeProps state)

  section(
    "tree view",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        tree.Mount("gallery-tree-view", treeProps state),
        MetroText.caption state.TreeAction
      )
  )

/// The flip view section.
let flipSection
  (state: CollectionsState, flipFactory: unit -> MetroFlipViewCell)
  : Blob =
  section(
    "flip view",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary
          "One item at a time. Swipe, use Left, Right, PageUp, PageDown, Home, \
           or End, or select the edge buttons and the dots.",
        Container(
          FlexDirection = FlexDirection.Column,
          Width = Length.Percent 100.0,
          Height = Length.Pixels 360.0,
          FlexShrink = 0.0
        )
          .Children(
            MetroFlipView.mount(
              "gallery-flip",
              MetroFlipView.defaults(Content.flipItems, state.FlipSelected),
              flipFactory
            )
          )
      )
  )

/// The semantic zoom section.
let zoomSection
  (state: CollectionsState, zoomFactory: unit -> MetroSemanticZoomCell)
  : Blob =
  section(
    "semantic zoom",
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.secondary
          "The same content at two levels. Select the zoom button in the top \
           end corner. In the overview, select a group to zoom in.",
        MetroSemanticZoom.mount(
          "gallery-zoom",
          MetroSemanticZoom.defaults state.ZoomedOut,
          zoomFactory
        ),
        MetroText.caption state.ZoomAction
      )
  )

/// The tree of the page: the page title, then one titled section per
/// control. The controls keep their sizes, order, and demo data.
let view
  (
    state: CollectionsState,
    factories: ListFactories,
    flipFactory: unit -> MetroFlipViewCell,
    zoomFactory: unit -> MetroSemanticZoomCell,
    tree: IMetroTreeViewFactory
  ) : Blob =
  state.Builds <- state.Builds + 1

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 24.0,
    Padding =
      Edges(
        Length.Pixels 4.0,
        Length.Pixels 12.0,
        Length.Pixels 24.0,
        Length.Pixels 0.0
      )
  )
    .Children(
      MetroText.title "collections",
      groupedSection(state, factories),
      listsSection(state, factories),
      boxSection(state, factories),
      gridSection(state, factories),
      treeSection(state, tree),
      flipSection(state, flipFactory),
      zoomSection(state, zoomFactory),
      MetroText.caption state.LastAction
    )

/// Creates the collections page. The page owns every selection.
let create() : CollectionsPageCell =
  let state: CollectionsState = {
    SingleSelected = [| "mail" |]
    MultipleSelected = [| "photos"; "maps" |]
    GroupedSelected = [| "music" |]
    BoxSelected = [| "medium" |]
    GridSelected = [| "photos"; "maps"; "music" |]
    FlipSelected = "one"
    ZoomedOut = false
    ZoomAction = "Zoomed in"
    LastDetail = null
    LastOverview = null
    ZoomRef = ValueNone
    TreeSelected = [| "pivot" |]
    TreeExpanded = [| "src" |]
    LastTreeInput = Unchecked.defaultof<TreeViewInput>
    TreeAction = "Ready"
    LastAction = "Ready"
    Builds = 0
  }

  // The slot of the page: the callbacks of the zoom tiles are made before
  // the page exists.
  let mutable self: CollectionsPageCell voption = ValueNone

  let rebuildPage() =
    self |> ValueOption.iter(fun page -> page.Rebuild())

  let ctx: ZoomContext = {
    State = state
    RebuildPage = rebuildPage
  }

  // Created once: every Build gives each collection the same factory. Each
  // collection has its own factory, because each one owns its own selection.
  let factories: ListFactories = {
    Single =
      MetroListView.createFactory(
        handlerOf(state, fun ids -> state.SingleSelected <- ids)
      )
    Multiple =
      MetroListView.createFactory(
        handlerOf(state, fun ids -> state.MultipleSelected <- ids)
      )
    Grouped =
      MetroListView.createFactory(
        handlerOf(state, fun ids -> state.GroupedSelected <- ids)
      )
    Box =
      MetroListView.createFactory(
        handlerOf(state, fun ids -> state.BoxSelected <- ids)
      )
    Grid =
      MetroGridView.createFactory(
        handlerOf(state, fun ids -> state.GridSelected <- ids)
      )
  }

  let flipFactory =
    MetroFlipView.createFactory(
      (fun id -> state.FlipSelected <- id),
      flipItemOf
    )

  let tree = MetroTreeView.createFactory(fun ev -> applyTree(state, ev))

  let zoomFactory() : MetroSemanticZoomCell =
    let cell =
      MetroSemanticZoom.create(
        (fun event -> applyZoom(state, event)),
        fun isZoomedOut -> renderZoom(ctx, isZoomedOut)
      )

    state.ZoomRef <- ValueSome cell
    cell

  let page = {
    new CollectionsPageCell() with
      override _.ZoomedOut = state.ZoomedOut
      override _.ZoomAction = state.ZoomAction
      override _.LastDetail = state.LastDetail
      override _.LastOverview = state.LastOverview
      override _.ZoomCell = state.ZoomRef
      override _.TreeInput = state.LastTreeInput
      override _.SelectedTreeIds = state.TreeSelected
      override _.ExpandedTreeIds = state.TreeExpanded
      override _.Builds = state.Builds

      override _.Build() : Blob =
        view(state, factories, flipFactory, zoomFactory, tree)
  }

  self <- ValueSome page
  page
