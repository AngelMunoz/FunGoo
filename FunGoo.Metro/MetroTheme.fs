namespace FunGoo.Metro

open System
open System.Runtime.CompilerServices
open Goo
open Goo.Widgets

[<Struct>]
type MetroMode =
  | System
  | Light
  | Dark

[<Struct; NoComparison>]
type MetroConfig = {
  Mode: MetroMode
  Accent: MetroAccentKind
  FontFamily: string
  MonoFontFamily: string
  TileUnit: float
}

[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroConfig =

  let defaultConfig: MetroConfig = {
    Mode = MetroMode.System
    Accent = MetroAccentKind.Blue
    FontFamily = MetroFont.family
    MonoFontFamily = "monospace"
    TileUnit = 150.0
  }

[<Struct; NoComparison>]
type MetroTheme = {
  IsDark: bool
  Palette: MetroPalette
  Accent: MetroAccent
  FontFamily: string
  MonoFontFamily: string
  TileUnit: float
  ReducedMotion: bool
  HighContrast: bool
  TextScaleFactor: float
}

[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroTheme =

  // Cache Ink and Bone once so toWidgetTheme reuses immutable palette arrays
  // instead of allocating fresh arrays per call.
  let darkBase = Theme.Ink
  let lightBase = Theme.Bone
  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Nullable 0.0

  let accentTextHover(theme: MetroTheme) : Color =
    if theme.IsDark then
      theme.Accent.Light
    else
      theme.Accent.Dark

  let effectiveMs(theme: MetroTheme, durationMs: float) : float =
    if theme.ReducedMotion then 0.0 else durationMs

  let toWidgetTheme
    (theme: MetroTheme, variant: Func<bool, Theme> voption)
    : Theme =
    let p = theme.Palette
    let a = theme.Accent
    let mutable t = if theme.IsDark then darkBase else lightBase

    t.CanvasColor <- p.Background
    t.PanelColor <- p.BackgroundAlt

    t.RaisedColor <-
      if theme.IsDark then
        Color.Rgb(34, 34, 34)
      else
        Color.Rgb(236, 236, 236)

    t.OverlayColor <- p.BackgroundAlt
    t.FieldColor <- p.Background

    t.SunkenColor <-
      if theme.IsDark then
        p.Background
      else
        Color.Rgb(234, 234, 234)

    t.HoverColor <- p.Hover
    t.PressedColor <- p.Active
    t.ActiveColor <- p.Highlight
    t.HoverTextColor <- Color.Transparent

    t.BorderColor <- p.Border
    t.InputBorderColor <- p.BorderStrong
    t.StrongBorderColor <- p.BorderStrong

    t.TextColor <- p.Foreground
    t.MutedTextColor <- p.ForegroundSecondary
    t.PlaceholderColor <- p.ForegroundSecondary
    t.FaintTextColor <- p.ForegroundDisabled
    t.SubtleTextColor <- p.ForegroundSecondary
    t.DisabledTextColor <- p.ForegroundDisabled
    t.DisabledOpacity <- 0.4

    t.AccentColor <- a.Base
    t.OnAccentColor <- a.OnAccent
    t.AccentHoverColor <- if theme.IsDark then a.Light else a.Dark
    t.AccentPressedColor <- a.Dark
    t.LinkColor <- if theme.IsDark then a.Light else a.Base
    t.FocusColor <- a.Base
    t.FocusRingWidth <- 2.0
    t.FocusRingOffset <- 2.0

    t.InfoColor <- p.Info
    t.SuccessColor <- p.Success
    t.WarningColor <- p.Warning
    t.DangerColor <- p.Danger
    t.DangerFillColor <- p.DangerFill
    t.DangerHoverColor <- p.DangerHover

    t.InfoFillColor <-
      if theme.IsDark then
        Color.Rgb(12, 34, 54)
      else
        Color.Rgb(227, 241, 252)

    t.SuccessFillColor <-
      if theme.IsDark then
        Color.Rgb(11, 38, 20)
      else
        Color.Rgb(226, 245, 231)

    t.WarningFillColor <-
      if theme.IsDark then
        Color.Rgb(46, 33, 10)
      else
        Color.Rgb(253, 243, 224)

    t.DangerQuietColor <-
      if theme.IsDark then
        Color.Rgb(52, 16, 16)
      else
        Color.Rgb(252, 232, 230)

    t.ControlRadius <- zeroRadius
    t.PanelRadius <- zeroRadius
    t.RadiusSm <- zeroRadius
    t.RadiusMd <- zeroRadius
    t.RadiusLg <- zeroRadius
    t.RadiusXl <- zeroRadius

    t.ElevationSm <- BoxShadow()
    t.ElevationMd <- BoxShadow()
    t.ElevationLg <- BoxShadow()
    t.PanelShadows <- emptyShadows
    t.OverlayShadows <- emptyShadows
    t.ControlShadows <- emptyShadows
    t.FieldShadows <- emptyShadows

    t.BorderWidth <- 2.0
    t.Spacing <- 4.0
    t.ControlHeight <- 36.0
    t.ControlPadding <- 12.0
    t.ControlGap <- 8.0
    t.PanelPadding <- 16.0

    t.FontFamily <- theme.FontFamily
    t.HeadingFontFamily <- theme.FontFamily
    t.MonoFontFamily <- theme.MonoFontFamily
    t.FontSize <- 14.0
    t.FontWeight <- 400
    t.HeadingFontWeight <- 300
    t.DisplayFontSize <- 56.0
    t.H1FontSize <- 42.0
    t.H2FontSize <- 28.0
    t.H3FontSize <- 26.0
    t.H4FontSize <- 20.0
    t.H5FontSize <- 16.0
    t.H6FontSize <- 14.0
    t.SubtitleFontSize <- 20.0
    t.LabelFontSize <- 14.0
    t.CaptionFontSize <- 12.0
    t.OverlineFontSize <- 11.0

    t.TransitionMs <-
      if variant.IsSome then
        MetroTiming.fastMs
      else
        effectiveMs(theme, MetroTiming.fastMs)

    t.MotionFastMs <- MetroTiming.fastMs
    t.MotionNormalMs <- MetroTiming.normalMs
    t.MotionSlowMs <- MetroTiming.slowMs

    t.Selection <- SelectionStyle.Filled
    t.Controls <- ControlStyle.Standard
    t.Variant <- ValueOption.defaultValue null variant

    if theme.HighContrast then
      t <- t.WithHighContrast()

    if Double.IsFinite theme.TextScaleFactor && theme.TextScaleFactor > 0.0 then
      t <- t.WithTextScale theme.TextScaleFactor
      t.PlatformTextScaleFactor <- theme.TextScaleFactor

    t

[<RequireQualifiedAccess>]
module Metro =

  let toThemeMode(mode: MetroMode) : ThemeMode =
    match mode with
    | MetroMode.System -> ThemeMode.System
    | MetroMode.Light -> ThemeMode.Light
    | MetroMode.Dark -> ThemeMode.Dark

  let stripAccessibility(theme: MetroTheme) : MetroTheme = {
    theme with
        ReducedMotion = false
        HighContrast = false
        TextScaleFactor = 1.0
  }

  let resolveTheme
    (config: MetroConfig, prefs: PlatformPreferences, dark: bool voption)
    : MetroTheme =
    let isDark =
      dark
      |> ValueOption.defaultWith(fun () ->
        match config.Mode with
        | MetroMode.Dark -> true
        | MetroMode.Light -> false
        | MetroMode.System -> prefs.Theme = SystemTheme.Dark)

    let systemAccent = ValueOption.ofNullable prefs.AccentColor

    let reducedMotion =
      prefs.ReducedMotion
      |> ValueOption.ofNullable
      |> ValueOption.defaultValue false

    let highContrast =
      prefs.HighContrast
      |> ValueOption.ofNullable
      |> ValueOption.defaultValue false

    let textScale =
      prefs.TextScaleFactor
      |> ValueOption.ofNullable
      |> ValueOption.defaultValue 1.0

    {
      IsDark = isDark
      Palette = if isDark then MetroPalette.dark else MetroPalette.light
      Accent = MetroAccent.resolve(config.Accent, systemAccent)
      FontFamily = config.FontFamily
      MonoFontFamily = config.MonoFontFamily
      TileUnit = config.TileUnit
      ReducedMotion = reducedMotion
      HighContrast = highContrast
      TextScaleFactor = textScale
    }

  let metroContext = Context<MetroTheme>()

  /// The Metro attachment of one window. The module <c>Metro</c> holds one
  /// attachment for each window. <c>MetroAttachment.create</c> makes it.
  type IMetroAttachment =

    /// The configuration of the attachment.
    abstract Config: MetroConfig with get, set

    /// Publishes the Metro theme. The widget theme is attached again when
    /// <c>configChanged</c> is <c>true</c>.
    abstract Apply: prefs: PlatformPreferences * configChanged: bool -> unit

    /// Applies a change of the platform preferences.
    abstract OnPreferencesChanged: prefs: PlatformPreferences -> unit

  /// State of the attachment of one window. One mutable field per concern.
  type AttachmentState = {
    mutable Config: MetroConfig
    mutable WidgetAttached: bool
    mutable LastMode: MetroMode
    mutable LastSystemAccent: Color voption
  }

  /// The values that the functions of an attachment work on.
  type Attachment = {
    Window: Window
    State: AttachmentState
    /// Resolves the widget theme for one mode with the current configuration.
    /// One delegate for the life of the window keeps the widget theme equal
    /// across updates.
    Variant: Func<bool, Theme>
  }

  [<RequireQualifiedAccess>]
  module MetroAttachment =

    /// Resolves the widget theme for <c>dark</c>, with the current
    /// configuration and no accessibility setting.
    let variantTheme
      (window: Window, state: AttachmentState, dark: bool)
      : Theme =
      let theme =
        resolveTheme(state.Config, window.Preferences, ValueSome dark)
        |> stripAccessibility

      MetroTheme.toWidgetTheme(theme, ValueNone)

    /// Sets the mode of the widget theme when the configuration changed it.
    let syncMode(window: Window, state: AttachmentState) : unit =
      if state.LastMode <> state.Config.Mode then
        Theme.SetMode(window, toThemeMode state.Config.Mode)
        state.LastMode <- state.Config.Mode

    /// Attaches the widget theme. The first attach also sets its mode.
    let attachWidget(attachment: Attachment, resolved: MetroTheme) : unit =
      let state = attachment.State
      let theme = stripAccessibility resolved

      Theme.Attach(
        attachment.Window,
        MetroTheme.toWidgetTheme(theme, ValueSome attachment.Variant)
      )

      if state.WidgetAttached then
        syncMode(attachment.Window, state)
      else
        Theme.SetMode(attachment.Window, toThemeMode state.Config.Mode)
        state.LastMode <- state.Config.Mode

      state.WidgetAttached <- true

    /// Publishes the Metro theme, then the widget theme when necessary.
    let apply
      (attachment: Attachment, prefs: PlatformPreferences, configChanged: bool)
      : unit =
      let state = attachment.State
      let resolved = resolveTheme(state.Config, prefs, ValueNone)
      metroContext.Set(attachment.Window, resolved)
      state.LastSystemAccent <- ValueOption.ofNullable prefs.AccentColor

      if not state.WidgetAttached || configChanged then
        attachWidget(attachment, resolved)
      else
        syncMode(attachment.Window, state)

    /// Applies a change of the preferences. The widget theme is attached again
    /// only when the system accent changed, because the widget theme updates
    /// the mode and the accessibility through its variant.
    let onPreferencesChanged
      (attachment: Attachment, prefs: PlatformPreferences)
      : unit =
      let nextSystemAccent = ValueOption.ofNullable prefs.AccentColor

      let systemAccentChanged =
        attachment.State.Config.Accent = MetroAccentKind.System
        && nextSystemAccent <> attachment.State.LastSystemAccent

      apply(attachment, prefs, systemAccentChanged)

    /// Registers the embedded Selawik weights once for the life of the
    /// attachment set. The registry cache keeps the sources alive while any
    /// window uses them, so an attachment never unregisters them.
    let ensureFonts() : unit = MetroFont.ensureRegistered() |> ignore

    let create(window: Window, initialConfig: MetroConfig) : IMetroAttachment =
      ensureFonts()

      let state: AttachmentState = {
        Config = initialConfig
        WidgetAttached = false
        LastMode = initialConfig.Mode
        LastSystemAccent = ValueNone
      }

      let variant =
        Func<bool, Theme>(fun dark -> variantTheme(window, state, dark))

      let attachment: Attachment = {
        Window = window
        State = state
        Variant = variant
      }

      {
        new IMetroAttachment with
          member _.Config
            with get () = state.Config
            and set value = state.Config <- value

          member _.Apply(prefs, configChanged) =
            apply(attachment, prefs, configChanged)

          member _.OnPreferencesChanged(prefs) =
            onPreferencesChanged(attachment, prefs)
      }

  let attachments = ConditionalWeakTable<Window, IMetroAttachment>()

  let getOrCreateAttachment
    (window: Window, initialConfig: MetroConfig)
    : IMetroAttachment =
    let mutable state = Unchecked.defaultof<IMetroAttachment>

    if attachments.TryGetValue(window, &state) then
      state
    else
      let created = MetroAttachment.create(window, initialConfig)
      attachments.Add(window, created)

      window.add_PreferencesChanged(fun prefs ->
        created.OnPreferencesChanged prefs)

      created

  let attach(window: Window, config: MetroConfig) : unit =
    let state = getOrCreateAttachment(window, config)
    state.Config <- config
    state.Apply(window.Preferences, true)

  let attachDefault(window: Window) : unit =
    attach(window, MetroConfig.defaultConfig)

  let setMode(window: Window, mode: MetroMode) : unit =
    let state = getOrCreateAttachment(window, MetroConfig.defaultConfig)
    state.Config <- { state.Config with Mode = mode }
    state.Apply(window.Preferences, false)

  let setAccent(window: Window, accent: MetroAccentKind) : unit =
    let state = getOrCreateAttachment(window, MetroConfig.defaultConfig)
    state.Config <- { state.Config with Accent = accent }
    state.Apply(window.Preferences, true)

  let read() : MetroTheme =
    let window = metroContext.Window
    let mutable state = Unchecked.defaultof<IMetroAttachment>

    if not(attachments.TryGetValue(window, &state)) then
      attachDefault window

    metroContext.Read()

  let readWindow(window: Window) : MetroTheme =
    let mutable value = Unchecked.defaultof<MetroTheme>

    if not(metroContext.TryRead(window, &value)) then
      attachDefault window
      metroContext.Read(window)
    else
      value

  let scope(theme: MetroTheme, build: unit -> Blob) : Blob =
    let inherited = read()
    let inheritedWidget = Theme.Read()

    let effectiveScale =
      if
        not(Double.IsFinite theme.TextScaleFactor)
        || theme.TextScaleFactor <= 0.0
        || (theme.TextScaleFactor = 1.0 && inherited.TextScaleFactor <> 1.0)
      then
        inherited.TextScaleFactor
      else
        theme.TextScaleFactor

    let effectiveTheme = {
      theme with
          ReducedMotion = theme.ReducedMotion || inherited.ReducedMotion
          HighContrast = theme.HighContrast || inherited.HighContrast
          TextScaleFactor = effectiveScale
    }

    let mutable widgetTheme =
      MetroTheme.toWidgetTheme(effectiveTheme, ValueNone)

    // Align with inherited platform scale so ThemeScope does not double-scale.
    widgetTheme.PlatformTextScaleFactor <-
      inheritedWidget.PlatformTextScaleFactor

    let inner =
      Func<Blob>(fun () ->
        metroContext.Scope(effectiveTheme, Func<Blob>(build)))

    ThemeScope(Value = Nullable widgetTheme, Content = inner).Build()

  let canvasStyle(theme: MetroTheme) : Style =
    let body = MetroTypography.spec MetroTextRole.Body

    Style(
      BackgroundColor = theme.Palette.Background,
      Color = theme.Palette.Foreground,
      FontFamily = theme.FontFamily,
      FontSize = Length.Pixels(body.FontSize * theme.TextScaleFactor),
      FontWeight = float body.FontWeight,
      LineHeight = body.LineHeight
    )
