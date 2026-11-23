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

module private ListInternals =

  let zeroRadius = Length.Pixels 0.0

  let isSelected(selectedIds: string array, itemId: string) : bool =
    if isNull selectedIds || isNull itemId then
      false
    else
      let mutable found = false
      let mutable i = 0

      while i < selectedIds.Length && not found do
        found <- selectedIds[i] = itemId
        i <- i + 1

      found

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

  let toInput(props: MetroListViewProps) : ListViewInput =
    let items = if isNull props.Items then Array.empty else props.Items

    let rows =
      items
      |> Array.map(fun item ->
        let caption = ValueOption.defaultValue null item.Caption

        let count =
          match item.Count with
          | ValueSome value -> Nullable value
          | ValueNone -> Nullable()

        ListViewItem(
          Id = item.Id,
          Label =
            (if String.IsNullOrEmpty item.Label then null else item.Label),
          Caption = caption,
          Count = count,
          Disabled = item.Disabled
        ))

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

[<Sealed>]
type MetroListViewFactory(onEvent: MetroListEvent -> unit) =

  // One instance each: every input of this factory carries the same callbacks.
  let onSelectionChange =
    Action<string array>(fun ids ->
      onEvent(MetroListEvent.SelectionChanged ids))

  let onClick = Action<string>(fun id -> onEvent(MetroListEvent.ItemClick id))

  let configureRow =
    Action<ListViewInput, ListViewItem, Container>(fun input item root ->
      let theme = Metro.read()
      let selected = ListInternals.isSelected(input.SelectedIds, item.Id)

      // ListView owns accessibility, focus, and input fields of the row. Only
      // the start border, the radius, and the background are set here. The
      // start border keeps its width on every row, so selection does not move
      // the row content.
      root.BorderRadius <- ListInternals.zeroRadius

      root.BorderStartWidth <- Length.Pixels MetroListView.indicatorWidthPx

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
        root.BorderStartColor <- Color.Transparent)

  let configureRoot =
    Action<ListViewInput, Container>(fun _ root ->
      root.BorderRadius <- ListInternals.zeroRadius)

  // The mapped rows stay the same instance while the item array does, so an
  // unchanged input does not rebuild the list.
  let mutable cachedItems: MetroListItem array = null
  let mutable cachedRows: ListViewItem array = null

  let rowsFor
    (items: MetroListItem array, mapped: ListViewItem array)
    : ListViewItem array =
    if not(isNull cachedRows) && obj.ReferenceEquals(items, cachedItems) then
      cachedRows
    else
      cachedItems <- items
      cachedRows <- mapped
      mapped

  member _.CreateInput(props: MetroListViewProps) : ListViewInput =
    let data = MetroListView.toInput props

    ListViewInput(
      Items = rowsFor(props.Items, data.Items),
      SelectedIds = data.SelectedIds,
      Multiple = data.Multiple,
      OnSelectionChange =
        (if props.Mode = MetroListSelectionMode.Activate then
           null
         else
           onSelectionChange),
      OnClick = onClick,
      RowHeight = data.RowHeight,
      Height = data.Height,
      AccessibilityName = data.AccessibilityName,
      ConfigureRow = configureRow,
      ConfigureRoot = configureRoot
    )

  member this.Mount(key: string, props: MetroListViewProps) : Blob =
    Cell.Mount<ListViewInput, ListView>(key, this.CreateInput props)
