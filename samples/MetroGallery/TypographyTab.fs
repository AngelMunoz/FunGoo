module MetroGallery.TypographyTab

open Goo
open FunGoo.Children
open FunGoo.Metro

module Content =

  let samples = [|
    MetroTextRole.Hero, "Hero 56 px Light"
    MetroTextRole.Title, "Title 42 px Light"
    MetroTextRole.Header, "Header 28 px Light"
    MetroTextRole.Subheader, "Subheader 26 px Light"
    MetroTextRole.Header2, "Header2 26 px Regular"
    MetroTextRole.Subtitle, "Subtitle 20 px Light"
    MetroTextRole.Medium, "Medium 16 px Regular — lead copy"
    MetroTextRole.Body, "Body 14 px Regular — primary reading text"
    MetroTextRole.Caption, "Caption 12 px Regular — secondary metadata"
    MetroTextRole.Badge, "Badge 11 px Bold Uppercase"
  |]

/// One row of the tab: the size and the weight of a text role, then a sample
/// of the role.
let sampleRow(index: int, sample: MetroTextRole * string) : Blob =
  let role, sampleText = sample
  let spec = MetroTypography.spec role

  Container(
    Key = $"typo-{index}",
    FlexDirection = FlexDirection.Column,
    Gap = Length.Pixels 2.0
  )
    .Children(
      MetroText.caption
        $"{role} — {int spec.FontSize} px, weight {spec.FontWeight}",
      MetroText.build(MetroText.defaults(sampleText, role))
    )

/// The typography tab: a scrollable column with one row for each text role.
let view() : Blob =
  let col =
    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      OverflowY = Overflow.Scroll,
      OverflowX = Overflow.Hidden,
      Gap = Length.Pixels 14.0,
      Padding =
        Edges(
          Length.Pixels 4.0,
          Length.Pixels 12.0,
          Length.Pixels 24.0,
          Length.Pixels 0.0
        )
    )

  for i = 0 to Content.samples.Length - 1 do
    col.Add(sampleRow(i, Content.samples[i]))

  col
