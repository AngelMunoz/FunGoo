module MetroGallery.FlipViewTab

open Goo
open FunGoo.Children
open FunGoo.Metro

module private Content =

  let items = [|
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

  let textOf(id: string) : string * string =
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

/// Gallery tab that shows a Metro FlipView with three content items.
[<Sealed>]
type FlipViewTabCell() =
  inherit Cell()

  let mutable selectedId = "one"

  // The flip view gives each selection to this tab through
  // InvokeInputCallback, so the owner rule rebuilds this tab.
  let onSelect(id: string) = selectedId <- id

  let renderItem(id: string) : Blob =
    let theme = Metro.read()
    let title, caption = Content.textOf id

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

  // Created once: every Build reuses the same factory.
  let flipFactory = MetroFlipView.createFactory(onSelect, renderItem)

  override _.Build() : Blob =
    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      Width = Length.Percent 100.0,
      Overflow = Overflow.Hidden,
      Gap = Length.Pixels 16.0
    )
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
              MetroFlipView.defaults(Content.items, selectedId),
              flipFactory
            )
          )
      )
