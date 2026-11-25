module MetroGallery.ListsTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro

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

/// The factories of the five collections of the tab. `create` makes them
/// once, so each collection receives the same callback instances and an
/// unchanged input does not rebuild it.
type ListFactories = {
  Single: IMetroListViewFactory
  Multiple: IMetroListViewFactory
  Grouped: IMetroListViewFactory
  Box: IMetroListViewFactory
  Grid: IMetroGridViewFactory
}

/// State of the tab. One mutable field per concern.
type ListsState = {
  mutable SingleSelected: string array
  mutable MultipleSelected: string array
  mutable GroupedSelected: string array
  mutable BoxSelected: string array
  mutable GridSelected: string array
  mutable LastAction: string
}

/// The handler of one collection of the tab. The collection gives each event
/// to this tab through `InvokeInputCallback`, so the owner rule rebuilds the
/// tab. The handler writes the state and does not call `Rebuild()`.
let handlerOf
  (state: ListsState, select: string array -> unit)
  : MetroListEvent -> unit =
  function
  | MetroListEvent.SelectionChanged ids -> select ids
  | MetroListEvent.ItemClick id -> state.LastAction <- $"Activated {id}"

/// One column of the tab: a subheader above one collection.
let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 8.0
  )
    .Children(MetroText.subheader title, content)

let view(state: ListsState, factories: ListFactories) : Blob =
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

  let groupedProps = {
    MetroListView.defaults Content.items with
        GroupKey = ValueSome Content.groupOf
        SelectedIds = state.GroupedSelected
        Height = ValueSome(Length.Pixels 300.0)
  }

  let boxProps = {
    MetroListView.defaults Content.choices with
        SelectedIds = state.BoxSelected
        Height = ValueSome(Length.Pixels 160.0)
  }

  let gridProps = {
    MetroGridView.defaults Content.tiles with
        SelectedIds = state.GridSelected
        TileSize = 96.0
        Height = ValueSome(Length.Pixels 216.0)
        AccessibilityName = ValueSome "Apps"
  }

  let listsRow =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      FlexShrink = 0.0,
      Gap = Length.Pixels 24.0
    )
      .Children(
        column(
          "single selection",
          factories.Single.Mount("gallery-list-single", singleProps)
        ),
        column(
          "multiple selection",
          factories.Multiple.Mount("gallery-list-multiple", multipleProps)
        ),
        column(
          "grouped",
          factories.Grouped.Mount("gallery-list-grouped", groupedProps)
        )
      )

  let collectionsRow =
    Container(
      FlexDirection = FlexDirection.Row,
      Width = Length.Percent 100.0,
      FlexShrink = 0.0,
      Gap = Length.Pixels 24.0
    )
      .Children(
        column(
          "list box",
          MetroListBox.build("gallery-list-box", boxProps, factories.Box)
        ),
        column("grid view", factories.Grid.Build gridProps)
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
        "The host owns the selection. Each selected row shows a 3 px accent \
         indicator on its start edge and an accent tint. The grouped list \
         files rows under one header per initial letter. The list box adds \
         a 2 px border. The grid view mounts only the tiles near the viewport.",
      listsRow,
      collectionsRow,
      MetroText.caption state.LastAction
    )

/// Creates the lists tab with three Metro lists, one list box, and one grid
/// view. The tab owns every selection.
let create() : Cell =
  let state: ListsState = {
    SingleSelected = [| "mail" |]
    MultipleSelected = [| "photos"; "maps" |]
    GroupedSelected = [| "music" |]
    BoxSelected = [| "medium" |]
    GridSelected = [| "photos"; "maps"; "music" |]
    LastAction = "Ready"
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

  {
    new Cell() with
      override _.Build() : Blob = view(state, factories)
  }
