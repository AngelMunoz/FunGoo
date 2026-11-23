module MetroGallery.InputsTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro

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
/// auto-suggest box. The tab owns every value.
[<Sealed>]
type InputsTabCell() as this =
  inherit Cell()

  let mutable quantity = 12.5
  let mutable rating = 3.0
  let mutable sizeSelected = "medium"
  let mutable citySelected = ""
  let mutable cityQuery = ""

  // Created once: every Build gives the widgets the same callback instances,
  // so an unchanged input does not rebuild them. The widgets own these
  // callbacks, so the tab rebuilds itself.
  let onQuantityChange =
    Action<float>(fun value ->
      quantity <- value
      this.Rebuild())

  let onRatingChange =
    Action<float>(fun value ->
      rating <- value
      this.Rebuild())

  // Each control has its own handler, because each one owns a different value.
  let onSize(event: MetroComboBoxEvent) =
    match event with
    | MetroComboBoxEvent.Selected id -> sizeSelected <- id
    | _ -> ()

    this.Rebuild()

  let onCity(event: MetroComboBoxEvent) =
    match event with
    | MetroComboBoxEvent.Selected id ->
      // A selected city fills the entry with its name.
      citySelected <- id
      cityQuery <- id
    | MetroComboBoxEvent.QueryChanged query ->
      // A new query starts a new search, so it clears the selection.
      cityQuery <- query
      citySelected <- ""
    | MetroComboBoxEvent.OpenChanged _ -> ()

    this.Rebuild()

  // One factory per control: each factory holds its own callback set.
  let sizeFactory = MetroComboBoxFactory(onSize)
  let cityFactory = MetroComboBoxFactory(onCity)

  let sizeProps() : MetroComboBoxProps = {
    MetroComboBox.defaults(Content.sizes, sizeSelected) with
        Placeholder = ValueSome "size"
        AccessibilityName = ValueSome "size"
        Width = ValueSome(Length.Pixels 200.0)
  }

  let cityProps() : MetroComboBoxProps = {
    MetroComboBox.autoSuggest(Content.suggestionsFor cityQuery, cityQuery) with
        SelectedId = citySelected
        Placeholder = ValueSome "city"
        SearchPlaceholder = ValueSome "city"
        EmptyText = ValueSome "No city matches"
        AccessibilityName = ValueSome "city"
        Width = ValueSome(Length.Pixels 200.0)
  }

  /// Identifier of the selected size, for the headless check of the gallery.
  member _.SizeSelected = sizeSelected

  /// Identifier of the selected city, for the headless check of the gallery.
  member _.CitySelected = citySelected

  /// Text of the city entry, for the headless check of the gallery.
  member _.CityQuery = cityQuery

  /// Widget input of the size combo box, for the headless check.
  member _.SizeInput = sizeFactory.CreateInput(sizeProps())

  /// Widget input of the city auto-suggest box, for the headless check.
  member _.CityInput = cityFactory.CreateInput(cityProps())

  override _.Build() : Blob =
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
                MetroNumberBox.defaults quantity with
                    Minimum = ValueSome 0.0
                    Maximum = ValueSome 100.0
                    Step = ValueSome 0.5
                    Label = ValueSome "quantity"
                    Width = ValueSome(Length.Pixels 200.0)
              },
              onQuantityChange
            ),
            MetroRating.mount(
              "gallery-rating",
              {
                MetroRating.defaults rating with
                    AccessibilityName = ValueSome "rating"
              },
              onRatingChange
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
            sizeFactory.Mount("gallery-combo", sizeProps()),
            cityFactory.Mount("gallery-suggest", cityProps())
          )
      )
