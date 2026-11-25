namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open Goo.Widgets.Layout
open FunGoo.Children

[<Struct>]
type MetroIconSize =
  | Small
  | Normal
  | Large
  | XLarge
  | Pixels of px: float

[<Struct; NoComparison>]
type MetroIconProps = {
  Glyph: string
  Size: MetroIconSize
  Tone: MetroTextTone
  ColorOverride: Color voption
}

module PrimitivesInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)

[<RequireQualifiedAccess>]
module MetroIcon =

  let pixelSize(size: MetroIconSize) : float =
    match size with
    | MetroIconSize.Small -> 16.0
    | MetroIconSize.Normal -> 20.0
    | MetroIconSize.Large -> 28.0
    | MetroIconSize.XLarge -> 40.0
    | MetroIconSize.Pixels px ->
      if Double.IsFinite px && px > 0.0 then px else 20.0

  let defaults(glyph: string) : MetroIconProps = {
    Glyph = glyph
    Size = MetroIconSize.Normal
    Tone = MetroTextTone.Primary
    ColorOverride = ValueNone
  }

  let sized(glyph: string, size: MetroIconSize) : MetroIconProps = {
    defaults glyph with
        Size = size
  }

  let build(props: MetroIconProps) : Blob =
    let theme = Metro.read()
    let px = pixelSize props.Size * theme.TextScaleFactor

    let color =
      props.ColorOverride
      |> ValueOption.defaultWith(fun () ->
        MetroText.resolveTone(theme, props.Tone))

    let name = if isNull props.Glyph then "" else props.Glyph

    if String.IsNullOrWhiteSpace name then
      Container(Width = Length.Pixels px, Height = Length.Pixels px) :> Blob
    else
      MaterialIcons.Create(name, px, color) :> Blob

  let named(glyph: string) : Blob = build(defaults glyph)

[<Struct; NoComparison>]
type MetroBorderProps = {
  Thickness: Edges
  BorderColor: Color voption
  Background: Color voption
  Padding: Edges
  CornerRadius: float
}

[<RequireQualifiedAccess>]
module MetroBorder =

  let defaults() : MetroBorderProps = {
    Thickness = Edges(Length.Pixels 2.0)
    BorderColor = ValueNone
    Background = ValueNone
    Padding = Edges(Length.Pixels 12.0)
    CornerRadius = 0.0
  }

  let card() : MetroBorderProps = {
    Thickness = Edges(Length.Pixels 1.0)
    BorderColor = ValueNone
    Background = ValueNone
    Padding = Edges(Length.Pixels 16.0)
    CornerRadius = 0.0
  }

  let build(props: MetroBorderProps, child: Blob) : Blob =
    let theme = Metro.read()
    let p = theme.Palette

    let borderCol =
      props.BorderColor
      |> ValueOption.defaultValue(
        if theme.HighContrast then p.Foreground else p.BorderStrong
      )

    let bgCol = props.Background |> ValueOption.defaultValue Color.Transparent

    let radius =
      if Double.IsFinite props.CornerRadius && props.CornerRadius > 0.0 then
        props.CornerRadius
      else
        0.0

    let box =
      Container(
        FlexDirection = FlexDirection.Column,
        Padding = props.Padding,
        BorderRadius = Length.Pixels radius,
        BorderWidth = props.Thickness,
        BorderColor = Edges<Color>(borderCol),
        BoxShadows = PrimitivesInternals.emptyShadows,
        BackgroundColor = bgCol
      )

    if not(isNull child) then
      box.Add(child)

    box :> Blob

[<Struct; NoComparison>]
type MetroExpanderProps = {
  Heading: string
  Expanded: bool
  Disabled: bool
  ContentPadding: float
  AccessibilityName: string voption
}

[<RequireQualifiedAccess>]
module MetroExpander =

  let defaults(heading: string, expanded: bool) : MetroExpanderProps = {
    Heading = heading
    Expanded = expanded
    Disabled = false
    ContentPadding = 16.0
    AccessibilityName = ValueNone
  }

  let build
    (props: MetroExpanderProps, onExpandedChange: bool -> unit, content: Blob)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let heading = if isNull props.Heading then "" else props.Heading
    let a11yName = props.AccessibilityName |> ValueOption.defaultValue heading

    let pad =
      if
        Double.IsFinite props.ContentPadding && props.ContentPadding >= 0.0
      then
        props.ContentPadding
      else
        16.0

    let disclosure =
      Disclosure(
        Label = heading,
        Content = content,
        Expanded = props.Expanded,
        Disabled = props.Disabled,
        OnExpandedChange = Action<bool>(onExpandedChange),
        AccessibilityName = a11yName,
        HeaderHeight = Nullable 44.0,
        HeaderPadding = Nullable 12.0,
        ContentPadding = Nullable pad,
        BackgroundColor = Nullable p.BackgroundAlt,
        HoverColor = Nullable p.Hover,
        TextColor = Nullable p.Foreground,
        BorderColor =
          Nullable(if theme.HighContrast then p.Foreground else p.Border),
        BorderWidth = Nullable(if theme.HighContrast then 2.0 else 1.0),
        BorderRadius = Nullable 0.0
      )

    disclosure.Build()

[<Struct; NoComparison>]
type MetroImageProps = {
  Source: ImageSourceProvider voption
  Fit: ImageFit
  Width: Length voption
  Height: Length voption
  Bordered: bool
  AccessibilityLabel: string voption
  Placeholder: Blob voption
  Fallback: Blob voption
}

[<RequireQualifiedAccess>]
module MetroImage =

  let defaults(source: ImageSourceProvider voption) : MetroImageProps = {
    Source = source
    Fit = ImageFit.Cover
    Width = ValueNone
    Height = ValueNone
    Bordered = false
    AccessibilityLabel = ValueNone
    Placeholder = ValueNone
    Fallback = ValueNone
  }

  let buildWithFallback
    (props: MetroImageProps, placeholder: Blob voption, fallback: Blob voption)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette

    let placeholderSlot = placeholder |> ValueOption.orElse props.Placeholder

    let fallbackSlot = fallback |> ValueOption.orElse props.Fallback

    let frame =
      Container(
        Overflow = Overflow.Hidden,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        BorderRadius = PrimitivesInternals.zeroRadius,
        BoxShadows = PrimitivesInternals.emptyShadows,
        BorderWidth =
          (if props.Bordered then
             Edges(Length.Pixels 1.0)
           else
             PrimitivesInternals.zeroBorder),
        BorderColor =
          (if props.Bordered then
             Edges<Color>(if theme.HighContrast then p.Foreground else p.Border)
           else
             PrimitivesInternals.transparentBorder),
        BackgroundColor = p.BackgroundAlt
      )

    props.Width |> ValueOption.iter(fun w -> frame.Width <- w)
    props.Height |> ValueOption.iter(fun h -> frame.Height <- h)

    props.AccessibilityLabel
    |> ValueOption.iter(fun lbl ->
      frame.Accessibility <-
        Accessibility(Role = AccessibilityRole.Image, Name = lbl))

    match props.Source with
    | ValueSome src when not(isNull src) ->
      let img =
        Image(
          Source = src,
          Fit = props.Fit,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0
        )

      placeholderSlot |> ValueOption.iter(fun ph -> img.Placeholder <- ph)
      fallbackSlot |> ValueOption.iter(fun fb -> img.Fallback <- fb)
      frame.Add(img)
    | _ ->
      placeholderSlot
      |> ValueOption.orElse fallbackSlot
      |> ValueOption.iter(fun fb ->
        if not(isNull fb) then
          frame.Add(fb))

    frame :> Blob

  let build(props: MetroImageProps) : Blob =
    buildWithFallback(props, ValueNone, ValueNone)

[<Struct>]
type MetroPresence =
  | NoPresence
  | Available
  | Away
  | Busy
  | Offline

[<Struct>]
type MetroAvatarSize =
  | Small
  | Normal
  | Large
  | XLarge

[<Struct; NoComparison>]
type MetroPersonPictureProps = {
  DisplayName: string
  InitialsOverride: string voption
  Source: ImageSourceProvider voption
  AvatarSize: MetroAvatarSize
  Presence: MetroPresence
  AccentOverride: Color voption
}

[<RequireQualifiedAccess>]
module MetroPersonPicture =

  let initialsOf(displayName: string) : string =
    if String.IsNullOrWhiteSpace displayName then
      ""
    else
      let parts =
        displayName.Split(
          [| ' '; '\t'; '-'; '_' |],
          StringSplitOptions.RemoveEmptyEntries
        )

      if parts.Length = 0 then
        ""
      elif parts.Length = 1 then
        let w = parts[0]
        Char.ToUpperInvariant(w[0]).ToString()
      else
        let first = Char.ToUpperInvariant(parts[0][0])
        let last = Char.ToUpperInvariant(parts[parts.Length - 1][0])
        String([| first; last |])

  let pixelSize(size: MetroAvatarSize) : float =
    match size with
    | MetroAvatarSize.Small -> 32.0
    | MetroAvatarSize.Normal -> 48.0
    | MetroAvatarSize.Large -> 64.0
    | MetroAvatarSize.XLarge -> 96.0

  let fontSizeFor(size: MetroAvatarSize) : float =
    match size with
    | MetroAvatarSize.Small -> 12.0
    | MetroAvatarSize.Normal -> 18.0
    | MetroAvatarSize.Large -> 24.0
    | MetroAvatarSize.XLarge -> 36.0

  let presenceColor(presence: MetroPresence, p: MetroPalette) : Color voption =
    match presence with
    | MetroPresence.NoPresence -> ValueNone
    | MetroPresence.Available -> ValueSome p.Success
    | MetroPresence.Away -> ValueSome p.Warning
    | MetroPresence.Busy -> ValueSome p.Danger
    | MetroPresence.Offline -> ValueSome p.ForegroundDisabled

  let defaults(displayName: string) : MetroPersonPictureProps = {
    DisplayName = displayName
    InitialsOverride = ValueNone
    Source = ValueNone
    AvatarSize = MetroAvatarSize.Normal
    Presence = MetroPresence.NoPresence
    AccentOverride = ValueNone
  }

  let build(props: MetroPersonPictureProps) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let edge = pixelSize props.AvatarSize

    let accent =
      props.AccentOverride
      |> ValueOption.map MetroAccent.fromColor
      |> ValueOption.defaultValue theme.Accent

    let name = if isNull props.DisplayName then "" else props.DisplayName
    let nameIsBlank = String.IsNullOrWhiteSpace name

    let hasImage =
      match props.Source with
      | ValueSome src when not(isNull src) -> true
      | _ -> false

    // The initials render only without an image, and the accessible name
    // uses them only for a blank name.
    let initials =
      if not hasImage || nameIsBlank then
        props.InitialsOverride
        |> ValueOption.defaultWith(fun () -> initialsOf name)
      else
        ""

    let baseLabel = if nameIsBlank then initials else name

    let a11yLabel =
      match props.Presence with
      | MetroPresence.NoPresence -> baseLabel
      | presence -> $"{baseLabel}, {presence}"

    let box =
      Container(
        Position = PositionType.Relative,
        Width = Length.Pixels edge,
        Height = Length.Pixels edge,
        FlexShrink = 0.0,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Overflow = Overflow.Hidden,
        BorderRadius = PrimitivesInternals.zeroRadius,
        BorderWidth =
          (if theme.HighContrast then
             Edges(Length.Pixels 2.0)
           else
             PrimitivesInternals.zeroBorder),
        BorderColor =
          (if theme.HighContrast then
             Edges<Color>(p.Foreground)
           else
             PrimitivesInternals.transparentBorder),
        BoxShadows = PrimitivesInternals.emptyShadows,
        BackgroundColor = accent.Base,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Image, Name = a11yLabel)
      )

    match props.Source with
    | ValueSome src when not(isNull src) ->
      box.Add(
        Image(
          Source = src,
          Fit = ImageFit.Cover,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0
        )
      )
    | _ ->
      box.Add(
        Text(
          Content = initials,
          Color = accent.OnAccent,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(fontSizeFor props.AvatarSize),
          FontWeight = 300.0,
          TextWrap = TextWrap.NoWrap
        )
      )

    presenceColor(props.Presence, p)
    |> ValueOption.iter(fun dotColor ->
      let badgeEdge = max 10.0 (Math.Round(edge * 0.25))

      box.Add(
        Container(
          Position = PositionType.Absolute,
          Right = Length.Pixels 0.0,
          Bottom = Length.Pixels 0.0,
          Width = Length.Pixels badgeEdge,
          Height = Length.Pixels badgeEdge,
          BorderRadius = PrimitivesInternals.zeroRadius,
          BorderWidth = Edges(Length.Pixels 2.0),
          BorderColor = Edges<Color>(p.Background),
          BackgroundColor = dotColor
        )
      ))

    box :> Blob

[<Struct; NoComparison>]
type MetroRichSpan = {
  RunText: string
  Bold: bool
  Italic: bool
  Underline: bool
  Monospace: bool
  Accent: bool
  Secondary: bool
}

[<Struct; NoComparison>]
type MetroRichTextProps = {
  Spans: MetroRichSpan array
  Role: MetroTextRole
  Selectable: bool
  MaxLines: int voption
  TrimEllipsis: bool
}

[<RequireQualifiedAccess>]
module MetroRichTextBlock =

  let plain(text: string) : MetroRichSpan = {
    RunText = text
    Bold = false
    Italic = false
    Underline = false
    Monospace = false
    Accent = false
    Secondary = false
  }

  let bold(text: string) : MetroRichSpan = { plain text with Bold = true }

  let italic(text: string) : MetroRichSpan = { plain text with Italic = true }

  let accent(text: string) : MetroRichSpan = {
    plain text with
        Accent = true
        Underline = true
  }

  let code(text: string) : MetroRichSpan = { plain text with Monospace = true }

  let defaults(spans: MetroRichSpan array) : MetroRichTextProps = {
    Spans = spans
    Role = MetroTextRole.Body
    Selectable = false
    MaxLines = ValueNone
    TrimEllipsis = false
  }

  // Inline styles on TextSpan must set inline text properties ONLY (never box or
  // TextTransform fields — TextPipeline.gs line 183).
  let toSpanStyle(run: MetroRichSpan, theme: MetroTheme) : Style =
    let hasCustom =
      run.Bold
      || run.Italic
      || run.Underline
      || run.Monospace
      || run.Accent
      || run.Secondary

    if not hasCustom then
      null
    else
      let st = Style()

      if run.Bold then
        st.FontWeight <- 700.0

      if run.Italic then
        st.FontStyle <- FontStyle.Italic

      if run.Underline then
        st.TextDecoration <- TextDecoration.Underline

      if run.Monospace then
        st.FontFamily <- theme.MonoFontFamily

      if run.Accent then
        st.Color <- theme.Accent.Base
      elif run.Secondary then
        st.Color <- theme.Palette.ForegroundSecondary

      st

  let build(props: MetroRichTextProps) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let spec = MetroTypography.spec props.Role
    let runs = if isNull props.Spans then Array.empty else props.Spans
    let spans = Array.zeroCreate<TextSpan> runs.Length

    for i = 0 to runs.Length - 1 do
      let r = runs[i]
      let txt = if isNull r.RunText then "" else r.RunText
      spans[i] <- TextSpan(txt, toSpanStyle(r, theme))

    let node = Text(spans)
    node.Color <- p.Foreground
    node.FontFamily <- theme.FontFamily
    node.FontSize <- Length.Pixels(spec.FontSize * theme.TextScaleFactor)
    node.FontWeight <- float spec.FontWeight
    node.LineHeight <- spec.LineHeight

    node.LetterSpacing <-
      Length.Pixels(spec.LetterSpacing * theme.TextScaleFactor)

    node.TextTransform <-
      if spec.Uppercase then
        TextTransform.Uppercase
      else
        TextTransform.None

    // The concatenated text of the runs is the accessible name.
    let name = node.Content

    node.Accessibility <-
      MetroText.defaultHeadingLevel props.Role
      |> ValueOption.map(fun level ->
        Accessibility(
          Role = AccessibilityRole.Heading,
          Level = Nullable level,
          Name = name
        ))
      |> ValueOption.defaultWith(fun () ->
        Accessibility(Role = AccessibilityRole.Text, Name = name))

    node.Selectable <- props.Selectable

    node.TextTrimming <-
      if props.TrimEllipsis then
        TextTrimming.Ellipsis
      else
        TextTrimming.None

    props.MaxLines
    |> ValueOption.iter(fun maxL ->
      if maxL > 0 then
        node.TextMaxLines <- maxL)

    node :> Blob
