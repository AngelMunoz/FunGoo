module MetroGallery.SelectionTab

open Goo
open Goo.Widgets.Data
open FunGoo.Children
open FunGoo.Metro

[<AbstractClass>]
type SelectionTabCell() =
  inherit Cell()

  abstract LongList: MetroLongListSelectorCell voption
  abstract InlinePicker: MetroListPickerCell voption
  abstract TreeInput: TreeViewInput
  abstract SelectedTreeIds: string array
  abstract ExpandedTreeIds: string array
  abstract LastAction: string
  abstract Builds: int

module Content =

  let contactGroups: MetroLongListGroup array = [|
    {
      Key = "a"
      Title = "a"
      Items = [|
        {
          Id = "ada"
          Label = "Ada Lovelace"
          Caption = ValueSome "Analytical Engine"
          Disabled = false
        }
        {
          Id = "alan"
          Label = "Alan Turing"
          Caption = ValueSome "Computation"
          Disabled = false
        }
      |]
    }
    {
      Key = "b"
      Title = "b"
      Items = Array.empty
    }
    {
      Key = "d"
      Title = "d"
      Items = [|
        {
          Id = "don"
          Label = "Don Syme"
          Caption = ValueSome "F# Language"
          Disabled = false
        }
      |]
    }
    {
      Key = "g"
      Title = "g"
      Items = [|
        {
          Id = "grace"
          Label = "Grace Hopper"
          Caption = ValueSome "Compiler Design"
          Disabled = false
        }
      |]
    }
    {
      Key = "m"
      Title = "m"
      Items = [|
        {
          Id = "margaret"
          Label = "Margaret Hamilton"
          Caption = ValueSome "Flight Software"
          Disabled = false
        }
      |]
    }
    {
      Key = "z"
      Title = "z"
      Items = Array.empty
    }
  |]

  let ringtoneItems: MetroListPickerItem array = [|
    {
      Id = "chime"
      Label = "Metro Chime"
      IconName = ValueSome "notifications"
      Disabled = false
    }
    {
      Id = "pulse"
      Label = "Digital Pulse"
      IconName = ValueSome "graphic_eq"
      Disabled = false
    }
    {
      Id = "silent"
      Label = "Silent"
      IconName = ValueSome "notifications_off"
      Disabled = false
    }
  |]

  let timezoneItems: MetroListPickerItem array = [|
    {
      Id = "utc-8"
      Label = "Pacific Time (UTC-08)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc-7"
      Label = "Mountain Time (UTC-07)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc-5"
      Label = "Eastern Time (UTC-05)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc+0"
      Label = "London (UTC+00)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc+1"
      Label = "Lisbon / Porto (UTC+01)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc+9"
      Label = "Tokyo (UTC+09)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
  |]

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

type SelectionState = {
  mutable ContactSelected: string array
  mutable JumpOpen: bool
  mutable RingtoneId: string
  mutable RingtoneOpen: bool
  mutable TimezoneId: string
  mutable TimezoneOpen: bool
  mutable TreeSelected: string array
  mutable TreeExpanded: string array
  mutable LastTreeInput: TreeViewInput
  mutable LastAction: string
  mutable Builds: int
}

type SelectionFactories = {
  LongList: unit -> MetroLongListSelectorCell
  InlinePicker: unit -> MetroListPickerCell
  OverlayPicker: unit -> MetroListPickerCell
  Tree: IMetroTreeViewFactory
}

let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 8.0
  )
    .Children(MetroText.subheader title, content)

let applyLongList(state: SelectionState, event: MetroLongListEvent) : unit =
  match event with
  | MetroLongListEvent.SelectionChanged ids ->
    state.ContactSelected <- ids
    let chosen = if ids.Length > 0 then ids[0] else "none"
    state.LastAction <- $"Contact: {chosen}"
  | MetroLongListEvent.ItemClick id -> state.LastAction <- $"Contact: {id}"
  | MetroLongListEvent.JumpOpenChanged isOpen -> state.JumpOpen <- isOpen
  | MetroLongListEvent.GroupJumped key ->
    state.LastAction <- $"Jumped to group {key}"

let applyInlinePicker
  (state: SelectionState, event: MetroListPickerEvent)
  : unit =
  match event with
  | MetroListPickerEvent.Selected id ->
    state.RingtoneId <- id
    state.LastAction <- $"Ringtone: {id}"
  | MetroListPickerEvent.OpenChanged isOpen -> state.RingtoneOpen <- isOpen

let applyOverlayPicker
  (state: SelectionState, event: MetroListPickerEvent)
  : unit =
  match event with
  | MetroListPickerEvent.Selected id ->
    state.TimezoneId <- id
    state.LastAction <- $"Timezone: {id}"
  | MetroListPickerEvent.OpenChanged isOpen -> state.TimezoneOpen <- isOpen

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

let view(state: SelectionState, factories: SelectionFactories) : Blob =
  let longListProps = {
    MetroLongListSelector.defaults Content.contactGroups with
        SelectedIds = state.ContactSelected
        JumpOpen = state.JumpOpen
        Height = ValueSome(Length.Pixels 340.0)
        AccessibilityName = ValueSome "Contacts"
  }

  let inlinePickerProps = {
    MetroListPicker.defaults(Content.ringtoneItems, state.RingtoneId) with
        Open = state.RingtoneOpen
        Header = ValueSome "RINGTONE (INLINE <= 5)"
  }

  let overlayPickerProps = {
    MetroListPicker.defaults(Content.timezoneItems, state.TimezoneId) with
        Open = state.TimezoneOpen
        Header = ValueSome "TIME ZONE (MODAL > 5)"
        Title = ValueSome "CHOOSE TIME ZONE"
  }

  let treeProps = {
    MetroTreeView.defaults Content.treeNodes with
        SelectedIds = state.TreeSelected
        ExpandedIds = state.TreeExpanded
        Height = ValueSome(Length.Pixels 240.0)
        AccessibilityName = ValueSome "Solution Explorer"
  }

  state.LastTreeInput <- factories.Tree.CreateInput treeProps

  let pickersAndTree =
    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      FlexBasis = Length.Pixels 0.0,
      MinWidth = Length.Pixels 0.0,
      Gap = Length.Pixels 16.0
    )
      .Children(
        MetroText.subheader "list pickers & tree view",
        MetroListPicker.mount(
          "gallery-picker-inline",
          inlinePickerProps,
          factories.InlinePicker
        ),
        MetroListPicker.mount(
          "gallery-picker-overlay",
          overlayPickerProps,
          factories.OverlayPicker
        ),
        factories.Tree.Mount("gallery-tree-view", treeProps)
      )

  let mainRow =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      FlexShrink = 0.0,
      Gap = Length.Pixels 24.0
    )
      .Children(
        column(
          "long list selector",
          MetroLongListSelector.mount(
            "gallery-long-list",
            longListProps,
            factories.LongList
          )
        ),
        pickersAndTree
      )

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 16.0
  )
    .Children(
      MetroText.secondary
        "Click any uppercase group header in the long list selector to open \
         the A-Z jump grid. List pickers expand inline for up to five items \
         and open a modal sheet for longer lists.",
      mainRow,
      MetroText.caption state.LastAction
    )

let create() : SelectionTabCell =
  let state: SelectionState = {
    ContactSelected = [| "ada" |]
    JumpOpen = false
    RingtoneId = "chime"
    RingtoneOpen = false
    TimezoneId = "utc+1"
    TimezoneOpen = false
    TreeSelected = [| "pivot" |]
    TreeExpanded = [| "src" |]
    LastTreeInput = Unchecked.defaultof<TreeViewInput>
    LastAction = "Ready"
    Builds = 0
  }

  let mutable longListRef: MetroLongListSelectorCell voption = ValueNone
  let mutable inlineRef: MetroListPickerCell voption = ValueNone

  // Every control below delivers its callbacks through InvokeInputCallback,
  // so the owner rule rebuilds this tab with no Rebuild() call here.
  let factories: SelectionFactories = {
    LongList =
      fun () ->
        let cell =
          MetroLongListSelector.create(fun ev -> applyLongList(state, ev))

        longListRef <- ValueSome cell
        cell
    InlinePicker =
      fun () ->
        let cell =
          MetroListPicker.create(fun ev -> applyInlinePicker(state, ev))

        inlineRef <- ValueSome cell
        cell
    OverlayPicker =
      fun () -> MetroListPicker.create(fun ev -> applyOverlayPicker(state, ev))
    Tree = MetroTreeView.createFactory(fun ev -> applyTree(state, ev))
  }

  {
    new SelectionTabCell() with
      override _.LongList = longListRef
      override _.InlinePicker = inlineRef
      override _.TreeInput = state.LastTreeInput
      override _.SelectedTreeIds = state.TreeSelected
      override _.ExpandedTreeIds = state.TreeExpanded
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, factories)
  }
