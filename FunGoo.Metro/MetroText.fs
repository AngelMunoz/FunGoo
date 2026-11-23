namespace FunGoo.Metro

open System
open Goo

[<Struct>]
type MetroTextTone =
  | Primary
  | Secondary
  | Disabled
  | Accent
  | OnAccent
  | Info
  | Success
  | Warning
  | Danger
  | Custom of Color

[<Struct; NoComparison>]
type MetroTextProps = {
  Content: string
  Role: MetroTextRole
  Tone: MetroTextTone
  MaxLines: int
  Wrap: TextWrap
  Trimming: TextTrimming
  HeadingLevel: int voption
}

[<RequireQualifiedAccess>]
module MetroText =

  let private resolveTone(theme: MetroTheme, tone: MetroTextTone) : Color =
    let p = theme.Palette

    match tone with
    | MetroTextTone.Primary -> p.Foreground
    | MetroTextTone.Secondary -> p.ForegroundSecondary
    | MetroTextTone.Disabled -> p.ForegroundDisabled
    | MetroTextTone.Accent -> theme.Accent.Base
    | MetroTextTone.OnAccent -> theme.Accent.OnAccent
    | MetroTextTone.Info -> p.Info
    | MetroTextTone.Success -> p.Success
    | MetroTextTone.Warning -> p.Warning
    | MetroTextTone.Danger -> p.Danger
    | MetroTextTone.Custom color -> color

  let private defaultHeadingLevel(role: MetroTextRole) : int voption =
    match role with
    | MetroTextRole.Hero
    | MetroTextRole.Title -> ValueSome 1
    | MetroTextRole.Header -> ValueSome 2
    | MetroTextRole.Subheader
    | MetroTextRole.Header2 -> ValueSome 3
    | MetroTextRole.Subtitle -> ValueSome 4
    | MetroTextRole.Medium
    | MetroTextRole.Body
    | MetroTextRole.Caption
    | MetroTextRole.Badge -> ValueNone

  let defaults(content: string, role: MetroTextRole) : MetroTextProps = {
    Content = content
    Role = role
    Tone = MetroTextTone.Primary
    MaxLines = 0
    Wrap = TextWrap.Wrap
    Trimming = TextTrimming.None
    HeadingLevel = ValueNone
  }

  let build(props: MetroTextProps) : Text =
    props.HeadingLevel
    |> ValueOption.iter(fun level ->
      if level < 1 || level > 6 then
        raise(
          ArgumentOutOfRangeException(
            "HeadingLevel",
            level,
            "Heading level must be between 1 and 6."
          )
        ))

    let theme = Metro.read()
    let spec = MetroTypography.spec props.Role
    let scale = theme.TextScaleFactor
    let content = if isNull props.Content then "" else props.Content

    let resolvedHeading =
      props.HeadingLevel
      |> ValueOption.orElseWith(fun () -> defaultHeadingLevel props.Role)

    let accessibility =
      resolvedHeading
      |> ValueOption.map(fun level ->
        Accessibility(
          Role = AccessibilityRole.Heading,
          Level = Nullable level,
          Name = content
        ))
      |> ValueOption.defaultWith(fun () ->
        Accessibility(Role = AccessibilityRole.Text, Name = content))

    Text(
      Content = content,
      FontFamily = theme.FontFamily,
      FontSize = Length.Pixels(spec.FontSize * scale),
      FontWeight = float spec.FontWeight,
      LetterSpacing = Length.Pixels(spec.LetterSpacing * scale),
      LineHeight = spec.LineHeight,
      TextTransform =
        (if spec.Uppercase then
           TextTransform.Uppercase
         else
           TextTransform.None),
      Color = resolveTone(theme, props.Tone),
      TextMaxLines = props.MaxLines,
      TextWrap = props.Wrap,
      TextTrimming = props.Trimming,
      Accessibility = accessibility
    )

  let inline hero(content: string) : Text =
    build(defaults(content, MetroTextRole.Hero))

  let inline title(content: string) : Text =
    build(defaults(content, MetroTextRole.Title))

  let inline header(content: string) : Text =
    build(defaults(content, MetroTextRole.Header))

  let inline subheader(content: string) : Text =
    build(defaults(content, MetroTextRole.Subheader))

  let inline subtitle(content: string) : Text =
    build(defaults(content, MetroTextRole.Subtitle))

  let inline body(content: string) : Text =
    build(defaults(content, MetroTextRole.Body))

  let inline secondary(content: string) : Text =
    build {
      defaults(content, MetroTextRole.Body) with
          Tone = MetroTextTone.Secondary
    }

  let inline caption(content: string) : Text =
    build(defaults(content, MetroTextRole.Caption))

  let inline badge(content: string) : Text =
    build(defaults(content, MetroTextRole.Badge))
