module MetroGallery.ListsTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro

module private Content =

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

/// Gallery tab with one single-selection list and one multi-selection list.
/// The tab owns both selections.
[<Sealed>]
type ListsTabCell() as this =
  inherit Cell()

  let mutable singleSelected: string array = [| "mail" |]
  let mutable multipleSelected: string array = [| "photos"; "maps" |]
  let mutable lastAction = "Ready"

  let onSingle(event: MetroListEvent) =
    match event with
    | MetroListEvent.SelectionChanged ids -> singleSelected <- ids
    | MetroListEvent.ItemClick id -> lastAction <- $"Activated {id}"

    this.Rebuild()

  let onMultiple(event: MetroListEvent) =
    match event with
    | MetroListEvent.SelectionChanged ids -> multipleSelected <- ids
    | MetroListEvent.ItemClick id -> lastAction <- $"Activated {id}"

    this.Rebuild()

  // Created once: every Build reuses the same factories, so each list
  // receives the same callback instances and an unchanged input does not
  // rebuild it.
  let singleFactory = MetroListViewFactory(onSingle)
  let multipleFactory = MetroListViewFactory(onMultiple)

  let listColumn(title: string, mount: unit -> Blob) : Blob =
    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      FlexBasis = Length.Pixels 0.0,
      MinWidth = Length.Pixels 0.0,
      Gap = Length.Pixels 8.0
    )
      .Children(MetroText.subheader title, mount())

  override _.Build() : Blob =
    let singleProps = {
      MetroListView.defaults Content.items with
          SelectedIds = singleSelected
          Height = ValueSome(Length.Pixels 300.0)
    }

    let multipleProps = {
      MetroListView.defaults Content.items with
          Mode = MetroListSelectionMode.Multiple
          SelectedIds = multipleSelected
          Height = ValueSome(Length.Pixels 300.0)
    }

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
              fun () -> singleFactory.Mount("gallery-list-single", singleProps)
            ),
            listColumn(
              "multiple selection",
              fun () ->
                multipleFactory.Mount("gallery-list-multiple", multipleProps)
            )
          ),
        MetroText.caption lastAction
      )
