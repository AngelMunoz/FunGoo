namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Data

[<Struct; NoComparison>]
type MetroListItem = {
  Id: string
  Label: string
  Caption: string voption
  Count: int voption
  Disabled: bool
}

[<Struct>]
type MetroListSelectionMode =
  | Activate
  | Single
  | Multiple

[<Struct; NoComparison>]
type MetroListViewProps = {
  Items: MetroListItem array
  SelectedIds: string array
  Mode: MetroListSelectionMode
  RowHeight: float
  Height: Length voption
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroListEvent =
  | SelectionChanged of ids: string array
  | ItemClick of id: string

/// Factory for Metro list views. The host owns the selection: update the props
/// after each event and rebuild the owner cell. Create one factory for each
/// list view, outside <c>Build</c>. The events do not name the list view, so
/// two list views that share one factory send their events to one handler.
type IMetroListViewFactory =

  /// Returns the widget input for <paramref name="props"/>, with the callbacks
  /// and the Metro row style of this factory. While <c>Items</c> is the same
  /// array instance, the mapped rows are also the same instance, so an
  /// unchanged input does not rebuild the list.
  abstract CreateInput: props: MetroListViewProps -> ListViewInput

  /// Mounts a Metro list view under the stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroListViewProps -> Blob

[<RequireQualifiedAccess>]
module MetroListView =

  let defaultRowHeightPx: float = 44.0
  let indicatorWidthPx: float = 3.0

  let defaults(items: MetroListItem array) : MetroListViewProps = {
    Items = items
    SelectedIds = Array.empty
    Mode = MetroListSelectionMode.Single
    RowHeight = defaultRowHeightPx
    Height = ValueNone
    AccessibilityName = ValueNone
  }

  let private rowsOf(props: MetroListViewProps) : ListViewItem array =
    let items = if isNull props.Items then Array.empty else props.Items

    items
    |> Array.map(fun item ->
      let caption = ValueOption.defaultValue null item.Caption

      let count =
        match item.Count with
        | ValueSome value -> Nullable value
        | ValueNone -> Nullable()

      ListViewItem(
        Id = item.Id,
        Label = (if String.IsNullOrEmpty item.Label then null else item.Label),
        Caption = caption,
        Count = count,
        Disabled = item.Disabled
      ))

  let private dataOf
    (props: MetroListViewProps, rows: ListViewItem array)
    : ListViewInput =
    let rowHeight =
      if Double.IsFinite props.RowHeight && props.RowHeight > 0.0 then
        props.RowHeight
      else
        defaultRowHeightPx

    let height =
      match props.Height with
      | ValueSome value -> Nullable value
      | ValueNone -> Nullable()

    // Activation mode keeps the selection empty, so no row stays selected.
    let selectedIds =
      if
        props.Mode = MetroListSelectionMode.Activate || isNull props.SelectedIds
      then
        Array.empty
      else
        props.SelectedIds

    ListViewInput(
      Items = rows,
      SelectedIds = selectedIds,
      Multiple = (props.Mode = MetroListSelectionMode.Multiple),
      RowHeight = Nullable rowHeight,
      Height = height,
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName
    )

  let toInput(props: MetroListViewProps) : ListViewInput =
    dataOf(props, rowsOf props)

  let private zeroRadius = Length.Pixels 0.0

  let private isSelected(selectedIds: string array, itemId: string) : bool =
    if isNull selectedIds || isNull itemId then
      false
    else
      let mutable found = false
      let mutable i = 0

      while i < selectedIds.Length && not found do
        found <- selectedIds[i] = itemId
        i <- i + 1

      found

  /// The delegates of a list view factory. `createFactory` makes them once:
  /// every input carries the same callback instances, so an unchanged input
  /// does not rebuild the list.
  type private ListViewHandlers = {
    OnSelectionChange: Action<string array>
    OnClick: Action<string>
    ConfigureRow: Action<ListViewInput, ListViewItem, Container>
    ConfigureRoot: Action<ListViewInput, Container>
  }

  /// The values that the functions of a list view factory work on. The mapped
  /// rows stay the same instance while the item array does.
  type private FactoryState = {
    Handlers: ListViewHandlers
    mutable Mapped: (MetroListItem array * ListViewItem array) voption
  }

  // ListView owns accessibility, focus, and input fields of the row. Only
  // the start border, the radius, and the background are set here. The
  // start border keeps its width on every row, so selection does not move
  // the row content.
  let private styleRow
    (input: ListViewInput, item: ListViewItem, root: Container)
    : unit =
    let theme = Metro.read()
    let selected = isSelected(input.SelectedIds, item.Id)
    root.BorderRadius <- zeroRadius
    root.BorderStartWidth <- Length.Pixels indicatorWidthPx

    if selected then
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

  let private styleRoot(_input: ListViewInput, root: Container) : unit =
    root.BorderRadius <- zeroRadius

  // The compiler requires the constructors here: a lambda does not convert to
  // a delegate in a record field (FS0002).
  let private handlersOf(onEvent: MetroListEvent -> unit) : ListViewHandlers = {
    OnSelectionChange =
      Action<string array>(fun ids ->
        onEvent(MetroListEvent.SelectionChanged ids))

    OnClick = Action<string>(fun id -> onEvent(MetroListEvent.ItemClick id))
    ConfigureRow =
      Action<ListViewInput, ListViewItem, Container>(fun input item root ->
        styleRow(input, item, root))

    ConfigureRoot =
      Action<ListViewInput, Container>(fun input root -> styleRoot(input, root))
  }

  // A factory maps an item array once. The mapped rows stay the same
  // instance while the item array does, so an unchanged input does not
  // rebuild the list.
  let private rowsFor
    (state: FactoryState, props: MetroListViewProps)
    : ListViewItem array =
    let items = props.Items

    match state.Mapped with
    | ValueSome(cachedItems, cachedRows) when
      obj.ReferenceEquals(items, cachedItems)
      ->
      cachedRows
    | _ ->
      let rows = rowsOf props
      state.Mapped <- ValueSome(items, rows)
      rows

  let private createInput
    (state: FactoryState, props: MetroListViewProps)
    : ListViewInput =
    let data = dataOf(props, rowsFor(state, props))

    ListViewInput(
      Items = data.Items,
      SelectedIds = data.SelectedIds,
      Multiple = data.Multiple,
      OnSelectionChange =
        (if props.Mode = MetroListSelectionMode.Activate then
           null
         else
           state.Handlers.OnSelectionChange),
      OnClick = state.Handlers.OnClick,
      RowHeight = data.RowHeight,
      Height = data.Height,
      AccessibilityName = data.AccessibilityName,
      ConfigureRow = state.Handlers.ConfigureRow,
      ConfigureRoot = state.Handlers.ConfigureRoot
    )

  let private mount
    (state: FactoryState, key: string, props: MetroListViewProps)
    : Blob =
    Cell.Mount<ListViewInput, ListView>(key, createInput(state, props))

  let createFactory(onEvent: MetroListEvent -> unit) : IMetroListViewFactory =
    let state: FactoryState = {
      Handlers = handlersOf onEvent
      Mapped = ValueNone
    }

    {
      new IMetroListViewFactory with
        member _.CreateInput(props) = createInput(state, props)
        member _.Mount(key, props) = mount(state, key, props)
    }
