module MetroGallery.NumberRatingTab

open System
open Goo
open FunGoo.Children
open FunGoo.Metro

/// Gallery tab with one Metro number box and one Metro rating control. The tab
/// owns both values.
[<Sealed>]
type NumberRatingTabCell() as this =
  inherit Cell()

  let mutable quantity = 12.5
  let mutable rating = 3.0

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

  override _.Build() : Blob =
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
          )
      )
