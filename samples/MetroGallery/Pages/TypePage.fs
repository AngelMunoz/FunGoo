module MetroGallery.Pages.TypePage

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

  let weightSentence = "The quick brown fox jumps over the lazy dog"

  let weights = [| 300; 400; 700 |]

  let tones = [|
    "Primary", MetroTextTone.Primary
    "Secondary", MetroTextTone.Secondary
    "Disabled", MetroTextTone.Disabled
    "Accent", MetroTextTone.Accent
    "OnAccent", MetroTextTone.OnAccent
    "Info", MetroTextTone.Info
    "Success", MetroTextTone.Success
    "Warning", MetroTextTone.Warning
    "Danger", MetroTextTone.Danger
  |]

/// One row of the page: the size and the weight of a text role, then a sample
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

/// One weight specimen: the same sentence set in one Selawik weight, with a
/// ramp caption naming the weight.
let weightRow(weight: int) : Blob =
  let theme = Metro.read()
  let spec = MetroTypography.spec MetroTextRole.Body

  Container(
    Key = $"typo-weight-{weight}",
    FlexDirection = FlexDirection.Column,
    Gap = Length.Pixels 2.0
  )
    .Children(
      MetroText.caption $"Selawik — weight {weight}",
      Text(
        Content = Content.weightSentence,
        FontFamily = theme.FontFamily,
        FontSize = Length.Pixels(spec.FontSize * theme.TextScaleFactor),
        FontWeight = float weight,
        LetterSpacing =
          Length.Pixels(spec.LetterSpacing * theme.TextScaleFactor),
        LineHeight = spec.LineHeight,
        Color = theme.Palette.Foreground
      )
    )

/// One tone specimen: the same sentence set in one MetroText tone, with a
/// ramp caption naming the tone.
let toneRow(name: string, tone: MetroTextTone) : Blob =
  Container(
    Key = $"typo-tone-{name.ToLowerInvariant()}",
    FlexDirection = FlexDirection.Column,
    Gap = Length.Pixels 2.0
  )
    .Children(
      MetroText.caption $"Tone — {name}",
      MetroText.build(
        {
          MetroText.defaults(Content.weightSentence, MetroTextRole.Body) with
              Tone = tone
        }
      )
    )

/// One titled section of the page: a subheader in the type ramp, then the
/// content, separated by whitespace only.
let section(title: string, content: Blob) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.subheader title, content)

/// The type page: the page title, the 10-role ramp, the Selawik weight
/// specimens, and the MetroText tone specimens, laid out with whitespace and
/// ramp captions and no card chrome.
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

  col.Add(MetroText.title "type")

  for i = 0 to Content.samples.Length - 1 do
    col.Add(sampleRow(i, Content.samples[i]))

  let weightCol =
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 14.0)

  for weight in Content.weights do
    weightCol.Add(weightRow weight)

  col.Add(section("WEIGHTS", weightCol))

  let toneCol =
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 14.0)

  for name, tone in Content.tones do
    toneCol.Add(toneRow(name, tone))

  col.Add(section("TONES", toneCol))

  col
