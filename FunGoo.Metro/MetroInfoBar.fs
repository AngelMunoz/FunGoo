namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct>]
type MetroInfoBarSeverity =
  | Informational
  | Success
  | Warning
  | Error

[<Struct; NoComparison>]
type MetroInfoBarEvent =
  | Closed
  | ActionInvoked

[<Struct; NoComparison>]
type MetroInfoBarProps = {
  Severity: MetroInfoBarSeverity
  Title: string voption
  Message: string
  ActionText: string voption
  Closable: bool
  Open: bool
  AccessibilityName: string voption
}

module InfoBarInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)

  let successColor = Color.Rgb(0, 163, 0)
  let warningColor = Color.Rgb(240, 150, 9)
  let errorColor = Color.Rgb(229, 20, 0)

[<RequireQualifiedAccess>]
module MetroInfoBar =

  let severityColor(severity: MetroInfoBarSeverity, theme: MetroTheme) : Color =
    match severity with
    | MetroInfoBarSeverity.Informational -> theme.Accent.Base
    | MetroInfoBarSeverity.Success -> InfoBarInternals.successColor
    | MetroInfoBarSeverity.Warning -> InfoBarInternals.warningColor
    | MetroInfoBarSeverity.Error -> InfoBarInternals.errorColor

  let severityIconName(severity: MetroInfoBarSeverity) : string =
    match severity with
    | MetroInfoBarSeverity.Informational -> "info"
    | MetroInfoBarSeverity.Success -> "check_circle"
    | MetroInfoBarSeverity.Warning -> "warning"
    | MetroInfoBarSeverity.Error -> "error"

  let defaults(message: string) : MetroInfoBarProps = {
    Severity = MetroInfoBarSeverity.Informational
    Title = ValueNone
    Message = message
    ActionText = ValueNone
    Closable = true
    Open = true
    AccessibilityName = ValueNone
  }

  let withSeverity
    (severity: MetroInfoBarSeverity, title: string, message: string)
    : MetroInfoBarProps = {
    defaults message with
        Severity = severity
        Title = ValueSome title
  }

  let informational(title: string, message: string) : MetroInfoBarProps =
    withSeverity(MetroInfoBarSeverity.Informational, title, message)

  let success(title: string, message: string) : MetroInfoBarProps =
    withSeverity(MetroInfoBarSeverity.Success, title, message)

  let warning(title: string, message: string) : MetroInfoBarProps =
    withSeverity(MetroInfoBarSeverity.Warning, title, message)

  let error(title: string, message: string) : MetroInfoBarProps =
    withSeverity(MetroInfoBarSeverity.Error, title, message)

  let build
    (props: MetroInfoBarProps, onEvent: MetroInfoBarEvent -> unit)
    : Blob =
    if not props.Open then
      Container(
        Display = Display.None,
        Accessibility =
          Accessibility(Role = AccessibilityRole.None, Hidden = true)
      )
      :> Blob
    else
      let theme = Metro.read()
      let p = theme.Palette
      let stripeColor = severityColor(props.Severity, theme)
      let iconName = severityIconName props.Severity
      let msgText = if isNull props.Message then "" else props.Message

      let titleName =
        props.Title
        |> ValueOption.filter(fun title -> not(String.IsNullOrWhiteSpace title))

      let a11yName =
        props.AccessibilityName
        |> ValueOption.filter(fun name -> not(String.IsNullOrWhiteSpace name))
        |> ValueOption.orElse titleName
        |> ValueOption.defaultValue msgText

      let liveMode =
        match props.Severity with
        | MetroInfoBarSeverity.Error -> AccessibilityLive.Assertive
        | _ -> AccessibilityLive.Polite

      let contentCol =
        Container(
          Key = "metro-infobar-content",
          FlexDirection = FlexDirection.Column,
          FlexGrow = 1.0,
          MinWidth = Length.Pixels 0.0,
          Gap = Length.Pixels 2.0
        )

      props.Title
      |> ValueOption.iter(fun title ->
        if not(String.IsNullOrWhiteSpace title) then
          contentCol.Add(
            Text(
              Content = title,
              Color = p.Foreground,
              FontFamily = theme.FontFamily,
              FontSize =
                Length.Pixels(
                  MetroTypography.size MetroTextRole.Body
                  * theme.TextScaleFactor
                ),
              FontWeight = 600.0,
              LineHeight = 1.3
            )
          ))

      if not(String.IsNullOrEmpty msgText) then
        contentCol.Add(
          Text(
            Content = msgText,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Caption
                * theme.TextScaleFactor
              ),
            FontWeight = 400.0,
            LineHeight = 1.4
          )
        )

      let icon = MaterialIcons.Create(iconName, 20.0, stripeColor)
      icon.Key <- "metro-infobar-icon"

      let row =
        Container(
          Width = Length.Percent 100.0,
          FlexDirection = FlexDirection.Row,
          AlignItems = AlignItems.Center,
          Gap = Length.Pixels 12.0,
          Padding = Edges(Length.Pixels 12.0, Length.Pixels 16.0),
          BorderRadius = InfoBarInternals.zeroRadius,
          BorderWidth =
            (if theme.HighContrast then
               Edges(Length.Pixels 1.0)
             else
               InfoBarInternals.zeroBorder),
          BorderColor =
            (if theme.HighContrast then
               Edges<Color>(p.Foreground)
             else
               InfoBarInternals.transparentBorder),
          // Logical start stripe flips automatically in RTL layouts.
          BorderStartWidth = Length.Pixels 4.0,
          BorderStartColor = stripeColor,
          BoxShadows = InfoBarInternals.emptyShadows,
          BackgroundColor = p.BackgroundAlt,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.Alert,
              Name = a11yName,
              Live = liveMode
            )
        )
          .Children(icon, contentCol)

      props.ActionText
      |> ValueOption.iter(fun actionLabel ->
        if not(String.IsNullOrWhiteSpace actionLabel) then
          let actionBtn =
            MetroButton.standard(
              actionLabel,
              fun () -> onEvent MetroInfoBarEvent.ActionInvoked
            )

          actionBtn.Key <- "metro-infobar-action"
          row.Add actionBtn)

      if props.Closable then
        let closeAction = Action(fun () -> onEvent MetroInfoBarEvent.Closed)

        let closeBtn =
          Button(
            Key = "metro-infobar-close",
            Width = Length.Pixels 32.0,
            Height = Length.Pixels 32.0,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            FlexShrink = 0.0,
            Padding = Edges(Length.Pixels 0.0),
            BorderRadius = InfoBarInternals.zeroRadius,
            BorderWidth = InfoBarInternals.zeroBorder,
            BorderColor = InfoBarInternals.transparentBorder,
            BoxShadows = InfoBarInternals.emptyShadows,
            BackgroundColor = Color.Transparent,
            HoverStyle = Style(BackgroundColor = p.Hover),
            ActiveStyle = Style(BackgroundColor = p.Active),
            FocusStyle =
              Style(
                OutlineWidth = Length.Pixels 2.0,
                OutlineColor = p.Foreground,
                OutlineOffset = Length.Pixels 1.0
              ),
            Cursor = Cursor.Pointer,
            Focusable = true,
            TabStop = true,
            OnClick = closeAction,
            KeyBindings = [|
              KeyBinding(Key.Enter, KeyModifiers.None, closeAction)
              KeyBinding(Key.Space, KeyModifiers.None, closeAction)
            |],
            Accessibility =
              Accessibility(Role = AccessibilityRole.Button, Name = "Close")
          )
            .Children(MaterialIcons.Create("close", 16.0, p.Foreground))

        row.Add(closeBtn)

      row :> Blob
