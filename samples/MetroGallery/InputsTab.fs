module MetroGallery.InputsTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro
open Goo.Widgets.Inputs

module private Content =

  let sizes = [|
    {
      Id = "small"
      Label = "Small"
      Disabled = false
    }
    {
      Id = "medium"
      Label = "Medium"
      Disabled = false
    }
    {
      Id = "large"
      Label = "Large"
      Disabled = false
    }
  |]

  let cities = [|
    "Amsterdam"
    "Barcelona"
    "Berlin"
    "Lisbon"
    "London"
    "Madrid"
    "Paris"
    "Porto"
    "Rome"
  |]

  let suggestionsFor(query: string) : MetroComboOption array =
    cities
    |> Array.filter(fun city ->
      city.StartsWith(query, StringComparison.OrdinalIgnoreCase))
    |> Array.map(fun city -> {
      Id = city
      Label = city
      Disabled = false
    })

/// Gallery tab with a Metro number box, a rating control, a combo box, and an
/// auto-suggest box. The tab owns every value. The headless check of the
/// gallery reads the values and the widget inputs through these members.
[<AbstractClass>]
type InputsTabCell() =
  inherit Cell()

  /// Identifier of the selected size.
  abstract SizeSelected: string

  /// Identifier of the selected city.
  abstract CitySelected: string

  /// Text of the city entry.
  abstract CityQuery: string

  /// Widget input of the size combo box.
  abstract SizeInput: ComboBoxInput

  /// Widget input of the city auto-suggest box.
  abstract CityInput: ComboBoxInput


/// State of the tab. One mutable field per concern.
type private InputsState = {
  mutable Quantity: float
  mutable Rating: float
  mutable SizeSelected: string
  mutable CitySelected: string
  mutable CityQuery: string
}

/// The factories and the callbacks that the functions of the tab work on.
/// `create` makes them once. F# converts a function to a delegate at each
/// build, and the target of the delegate is the function value. Thus the
/// input of two builds compares equal, and an unchanged input does not
/// rebuild a widget.
type private InputsContext = {
  State: InputsState
  SizeFactory: IMetroComboBoxFactory
  CityFactory: IMetroComboBoxFactory
  OnQuantityChange: float -> unit
  OnRatingChange: float -> unit
}

let private applySize(state: InputsState, event: MetroComboBoxEvent) : unit =
  match event with
  | MetroComboBoxEvent.Selected id -> state.SizeSelected <- id
  | _ -> ()

let private applyCity(state: InputsState, event: MetroComboBoxEvent) : unit =
  match event with
  | MetroComboBoxEvent.Selected id ->
    // A selected city fills the entry with its name.
    state.CitySelected <- id
    state.CityQuery <- id
  | MetroComboBoxEvent.QueryChanged query ->
    // A new query starts a new search, so it clears the selection.
    state.CityQuery <- query
    state.CitySelected <- ""
  | MetroComboBoxEvent.OpenChanged _ -> ()

let private sizeProps(state: InputsState) : MetroComboBoxProps = {
  MetroComboBox.defaults(Content.sizes, state.SizeSelected) with
      Placeholder = ValueSome "size"
      AccessibilityName = ValueSome "size"
      Width = ValueSome(Length.Pixels 200.0)
}

let private cityProps(state: InputsState) : MetroComboBoxProps = {
  MetroComboBox.autoSuggest(
    Content.suggestionsFor state.CityQuery,
    state.CityQuery
  ) with
      SelectedId = state.CitySelected
      Placeholder = ValueSome "city"
      SearchPlaceholder = ValueSome "city"
      EmptyText = ValueSome "No city matches"
      AccessibilityName = ValueSome "city"
      Width = ValueSome(Length.Pixels 200.0)
}

/// The tree of the tab.
let private build(ctx: InputsContext) : Blob =
  let state = ctx.State

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 24.0
  )
    .Children(
      MetroText.secondary
        "The host owns each value. The number box has spin buttons and open \
         bounds. The rating control uses the accent color.",
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        AlignItems = AlignItems.FlexStart,
        Gap = Length.Pixels 48.0
      )
        .Children(
          MetroNumberBox.mount(
            "gallery-number",
            {
              MetroNumberBox.defaults state.Quantity with
                  Minimum = ValueSome 0.0
                  Maximum = ValueSome 100.0
                  Step = ValueSome 0.5
                  Label = ValueSome "quantity"
                  Width = ValueSome(Length.Pixels 200.0)
            },
            ctx.OnQuantityChange
          ),
          MetroRating.mount(
            "gallery-rating",
            {
              MetroRating.defaults state.Rating with
                  AccessibilityName = ValueSome "rating"
            },
            ctx.OnRatingChange
          )
        ),
      MetroText.secondary
        "The combo box selects one option. The auto-suggest box filters its \
         options through the query, and the host supplies the options.",
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        AlignItems = AlignItems.FlexStart,
        Gap = Length.Pixels 48.0
      )
        .Children(
          ctx.SizeFactory.Mount("gallery-combo", sizeProps state),
          ctx.CityFactory.Mount("gallery-suggest", cityProps state)
        )
    )

/// Creates the inputs tab. Each widget is a mounted Goo.Widgets control: it
/// gives each callback of its input to this tab through
/// `InvokeInputCallback`, so the owner rule rebuilds the tab. Thus a handler
/// writes the state, and no handler calls `Rebuild()`.
let create() : InputsTabCell =
  let state: InputsState = {
    Quantity = 12.5
    Rating = 3.0
    SizeSelected = "medium"
    CitySelected = ""
    CityQuery = ""
  }

  let onQuantityChange(value: float) = state.Quantity <- value

  let onRatingChange(value: float) = state.Rating <- value

  // One factory per control: each control has its own values, and the events
  // do not name the control.
  let ctx: InputsContext = {
    State = state
    SizeFactory =
      MetroComboBox.createFactory(fun event -> applySize(state, event))
    CityFactory =
      MetroComboBox.createFactory(fun event -> applyCity(state, event))
    OnQuantityChange = onQuantityChange
    OnRatingChange = onRatingChange
  }

  {
    new InputsTabCell() with
      override _.SizeSelected = state.SizeSelected
      override _.CitySelected = state.CitySelected
      override _.CityQuery = state.CityQuery
      override _.SizeInput = ctx.SizeFactory.CreateInput(sizeProps state)
      override _.CityInput = ctx.CityFactory.CreateInput(cityProps state)
      override _.Build() : Blob = build ctx
  }
