namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct>]
type MetroToastAlignment =
  | TopCenter
  | TopEnd

[<Struct; NoComparison>]
type MetroToastItem = {
  Id: string
  Title: string voption
  Message: string
  Severity: MetroInfoBarSeverity
  ActionText: string voption
  DurationMs: float voption
  Closable: bool
}

[<Struct; NoComparison>]
type MetroToastEvent =
  | Dismissed of id: string
  | ActionInvoked of id: string

[<Struct; NoComparison>]
type MetroToastHostInput = {
  Items: MetroToastItem array
  Alignment: MetroToastAlignment
  ZIndex: int
}

[<AbstractClass>]
type MetroToastHostCell() =
  inherit Cell<MetroToastHostInput>()

  abstract ActiveCount: int

  abstract ActiveTimerCount: int

  abstract EntranceTranslate: Anim<Point>

  abstract EntranceOpacity: Anim<float>

  abstract View: Blob

  abstract Dismiss: id: string -> unit

  abstract TriggerAction: id: string -> unit

  member internal this.Raise
    (callback: Action<MetroToastEvent>, ev: MetroToastEvent)
    : unit =
    this.InvokeInputCallback(callback, ev)

  member internal this.Schedule(delayMs: float, action: Action) : WindowTimer =
    this.After(delayMs, action)

  member internal this.IsHostOpen: bool =
    not(isNull this.Host) && this.Host.IsOpen

module ToastInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPoint = Point(X = 0.0, Y = 0.0)
  let enterOffset = Point(X = 0.0, Y = -24.0)

  [<Literal>]
  let defaultDurationMs = 4000.0

  let itemsOf(input: MetroToastHostInput) : MetroToastItem array =
    if isNull input.Items then Array.empty else input.Items

[<RequireQualifiedAccess>]
module MetroToast =

  let defaultDurationMs = ToastInternals.defaultDurationMs

  let item(id: string, message: string) : MetroToastItem = {
    Id = id
    Title = ValueNone
    Message = message
    Severity = MetroInfoBarSeverity.Informational
    ActionText = ValueNone
    DurationMs = ValueSome defaultDurationMs
    Closable = true
  }

  let titledItem(id: string, title: string, message: string) : MetroToastItem = {
    item(id, message) with
        Title = ValueSome title
  }

  let hostDefaults(items: MetroToastItem array) : MetroToastHostInput = {
    Items = items
    Alignment = MetroToastAlignment.TopEnd
    ZIndex = 40
  }

  let buildCard
    (toast: MetroToastItem, onEvent: MetroToastEvent -> unit)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let stripeColor = MetroInfoBar.severityColor(toast.Severity, theme)
    let iconName = MetroInfoBar.severityIconName toast.Severity
    let outlineColor = if theme.HighContrast then p.Foreground else p.Border
    let msgText = if isNull toast.Message then "" else toast.Message
    let toastId = if isNull toast.Id then "" else toast.Id

    let a11yName =
      toast.Title
      |> ValueOption.filter(fun title -> not(String.IsNullOrWhiteSpace title))
      |> ValueOption.defaultValue msgText

    let liveMode =
      match toast.Severity with
      | MetroInfoBarSeverity.Error -> AccessibilityLive.Assertive
      | _ -> AccessibilityLive.Polite

    let bodyCol =
      Container(
        Key = $"metro-toast-body-{toastId}",
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        MinWidth = Length.Pixels 0.0,
        Gap = Length.Pixels 4.0
      )

    toast.Title
    |> ValueOption.iter(fun title ->
      if not(String.IsNullOrWhiteSpace title) then
        bodyCol.Add(
          Text(
            Content = title,
            Color = p.Foreground,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(14.0 * theme.TextScaleFactor),
            FontWeight = 600.0,
            LineHeight = 1.3
          )
        ))

    if not(String.IsNullOrEmpty msgText) then
      bodyCol.Add(
        Text(
          Content = msgText,
          Color = p.ForegroundSecondary,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
          FontWeight = 400.0,
          LineHeight = 1.4
        )
      )

    toast.ActionText
    |> ValueOption.iter(fun actionLabel ->
      if not(String.IsNullOrWhiteSpace actionLabel) then
        bodyCol.Add(
          Container(
            FlexDirection = FlexDirection.Row,
            Padding = Edges(Length.Pixels 4.0, Length.Pixels 0.0)
          )
            .Children(
              MetroButton.standard(
                actionLabel,
                fun () -> onEvent(MetroToastEvent.ActionInvoked toastId)
              )
            )
        ))

    let icon = MaterialIcons.Create(iconName, 20.0, stripeColor)
    icon.Key <- $"metro-toast-icon-{toastId}"

    let card =
      Container(
        Key = $"metro-toast-{toastId}",
        MinWidth = Length.Pixels 300.0,
        MaxWidth = Length.Pixels 400.0,
        Width = Length.Percent 100.0,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.FlexStart,
        Gap = Length.Pixels 12.0,
        Padding = Edges(Length.Pixels 12.0, Length.Pixels 16.0),
        BorderRadius = ToastInternals.zeroRadius,
        BorderWidth =
          Edges(
            Length.Pixels 4.0,
            Length.Pixels 1.0,
            Length.Pixels 1.0,
            Length.Pixels 1.0
          ),
        BorderColor =
          Edges<Color>(stripeColor, outlineColor, outlineColor, outlineColor),
        BoxShadows = ToastInternals.emptyShadows,
        BackgroundColor = p.BackgroundAlt,
        HitTestSelf = true,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Status,
            Name = a11yName,
            Live = liveMode
          )
      )
        .Children(icon, bodyCol)

    if toast.Closable then
      let dismissAction =
        Action(fun () -> onEvent(MetroToastEvent.Dismissed toastId))

      let closeBtn =
        Button(
          Key = $"metro-toast-close-{toastId}",
          Width = Length.Pixels 28.0,
          Height = Length.Pixels 28.0,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          FlexShrink = 0.0,
          Padding = Edges(Length.Pixels 0.0),
          BorderRadius = ToastInternals.zeroRadius,
          BorderWidth = ToastInternals.zeroBorder,
          BorderColor = ToastInternals.transparentBorder,
          BoxShadows = ToastInternals.emptyShadows,
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
          OnClick = dismissAction,
          KeyBindings = [|
            KeyBinding(Key.Enter, KeyModifiers.None, dismissAction)
            KeyBinding(Key.Space, KeyModifiers.None, dismissAction)
          |],
          Accessibility =
            Accessibility(Role = AccessibilityRole.Button, Name = "Close")
        )
          .Children(MaterialIcons.Create("close", 16.0, p.Foreground))

      card.Add(closeBtn)

    card :> Blob

  [<Struct; NoComparison>]
  type ScheduledTimer = {
    Timer: WindowTimer
    DurationMs: float
  }

  type HostState = {
    Timers: Dictionary<string, ScheduledTimer>
    KnownIds: HashSet<string>
    TimerDurations: Dictionary<string, float voption>
    StaleIds: ResizeArray<string>
    mutable Input: MetroToastHostInput
    mutable LastReducedMotion: bool
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
  }

  let clearAllTimers(state: HostState) : unit =
    for kv in state.Timers do
      kv.Value.Timer.Dispose()

    state.Timers.Clear()

  let syncTimers
    (
      state: HostState,
      cellOpt: MetroToastHostCell voption,
      emit: MetroToastEvent -> unit
    ) : unit =
    let items = ToastInternals.itemsOf state.Input

    // Scheduled duration per active toast id. ValueNone keeps the toast persistent.
    let durations = state.TimerDurations
    durations.Clear()

    for i = 0 to items.Length - 1 do
      let toast = items[i]

      if not(String.IsNullOrEmpty toast.Id) then
        durations[toast.Id] <-
          toast.DurationMs
          |> ValueOption.filter(fun ms -> Double.IsFinite ms && ms > 0.0)

    // Cancel timers for removed items and for stale durations.
    if state.Timers.Count > 0 then
      let toRemove = state.StaleIds
      toRemove.Clear()

      for kv in state.Timers do
        let stale =
          match durations.TryGetValue(kv.Key) with
          | true, duration -> duration <> ValueSome kv.Value.DurationMs
          | false, _ -> true

        if stale then
          kv.Value.Timer.Dispose()
          toRemove.Add kv.Key

      for i = 0 to toRemove.Count - 1 do
        state.Timers.Remove(toRemove[i]) |> ignore

    // Schedule timers only when the host window is open (hard fact 16).
    let canSchedule =
      cellOpt
      |> ValueOption.map(fun cell -> cell.IsHostOpen)
      |> ValueOption.defaultValue false

    if canSchedule then
      cellOpt
      |> ValueOption.iter(fun cell ->
        for i = 0 to items.Length - 1 do
          let toast = items[i]
          let id = toast.Id

          if
            not(String.IsNullOrEmpty id) && not(state.Timers.ContainsKey id)
          then
            durations[id]
            |> ValueOption.iter(fun ms ->
              let timer =
                cell.Schedule(
                  ms,
                  Action(fun () ->
                    state.Timers.Remove id |> ignore
                    emit(MetroToastEvent.Dismissed id))
                )

              state.Timers[id] <- { Timer = timer; DurationMs = ms }))

  let detectNewItemsAndAnimate
    (state: HostState, items: MetroToastItem array, reducedMotion: bool)
    : unit =
    let mutable hasNew = false

    for i = 0 to items.Length - 1 do
      let id = items[i].Id

      if not(String.IsNullOrEmpty id) && not(state.KnownIds.Contains id) then
        hasNew <- true

    state.KnownIds.Clear()

    for i = 0 to items.Length - 1 do
      let id = items[i].Id

      if not(String.IsNullOrEmpty id) then
        state.KnownIds.Add id |> ignore

    if hasNew then
      if reducedMotion then
        state.Translate.Snap(ToastInternals.zeroPoint)
        state.Opacity.Snap(1.0)
      else
        state.Translate.Play(
          ToastInternals.enterOffset,
          ToastInternals.zeroPoint,
          MetroMotion.slow
        )

        state.Opacity.Play(0.0, 1.0, MetroMotion.slow)

  let buildHostView(state: HostState, emit: MetroToastEvent -> unit) : Blob =
    let items = ToastInternals.itemsOf state.Input

    if items.Length = 0 then
      Container(
        Display = Display.None,
        Accessibility =
          Accessibility(Role = AccessibilityRole.None, Hidden = true)
      )
      :> Blob
    else
      let crossAlign =
        match state.Input.Alignment with
        | MetroToastAlignment.TopCenter -> AlignItems.Center
        | MetroToastAlignment.TopEnd -> AlignItems.FlexEnd

      let stack =
        Container(
          Key = "metro-toast-stack",
          Width = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          AlignItems = crossAlign,
          Gap = Length.Pixels 8.0,
          Padding = Edges(Length.Pixels 12.0),
          HitTestSelf = false,
          TranslateAnim = state.Translate,
          OpacityAnim = state.Opacity
        )

      for i = 0 to items.Length - 1 do
        stack.Add(buildCard(items[i], emit))

      let portal: Portal =
        Portal(
          Key = "metro-toast-portal",
          ZIndex = (if state.Input.ZIndex <= 0 then 40 else state.Input.ZIndex),
          Width = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          JustifyContent = JustifyContent.FlexStart,
          AlignItems = AlignItems.Stretch
        )

      portal.Add(stack)
      portal :> Blob

  let createHost(onEvent: MetroToastEvent -> unit) : MetroToastHostCell =
    let state: HostState = {
      Timers = Dictionary<string, ScheduledTimer>(StringComparer.Ordinal)
      KnownIds = HashSet<string>(StringComparer.Ordinal)
      TimerDurations = Dictionary<string, float voption>(StringComparer.Ordinal)
      StaleIds = ResizeArray<string>()
      Input = hostDefaults Array.empty
      LastReducedMotion = false
      Translate = Unchecked.defaultof<Anim<Point>>
      Opacity = Unchecked.defaultof<Anim<float>>
    }

    let mutable self: MetroToastHostCell voption = ValueNone
    let onEventAction = Action<MetroToastEvent>(onEvent)

    let emit(ev: MetroToastEvent) =
      self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, ev))

    let cell = {
      new MetroToastHostCell() with
        override _.ActiveCount = (ToastInternals.itemsOf state.Input).Length
        override _.ActiveTimerCount = state.Timers.Count
        override _.EntranceTranslate = state.Translate
        override _.EntranceOpacity = state.Opacity
        override _.View = buildHostView(state, emit)
        override _.Dismiss(id) = emit(MetroToastEvent.Dismissed id)
        override _.TriggerAction(id) = emit(MetroToastEvent.ActionInvoked id)

        override _.Mounted() = syncTimers(state, self, emit)

        override _.InputChanged(input) =
          state.Input <- input
          let items = ToastInternals.itemsOf input
          detectNewItemsAndAnimate(state, items, state.LastReducedMotion)
          syncTimers(state, self, emit)

        override _.Unmounting() =
          clearAllTimers state
          state.KnownIds.Clear()
          state.Translate.Snap(ToastInternals.zeroPoint)
          state.Opacity.Snap(1.0)

        override _.Build(input) : Blob =
          state.Input <- input
          let theme = Metro.read()
          state.LastReducedMotion <- theme.ReducedMotion

          if
            theme.ReducedMotion
            && (state.Translate.Running || state.Opacity.Running)
          then
            state.Translate.Snap(ToastInternals.zeroPoint)
            state.Opacity.Snap(1.0)

          buildHostView(state, emit)
    }

    state.Translate <- cell.AnimateValue(ToastInternals.zeroPoint)
    state.Opacity <- cell.AnimateValue(1.0)
    self <- ValueSome cell
    cell

  let createHostFactory
    (onEvent: MetroToastEvent -> unit)
    : unit -> MetroToastHostCell =
    fun () -> createHost onEvent

  let mountHost
    (
      key: string,
      input: MetroToastHostInput,
      factory: unit -> MetroToastHostCell
    ) : Blob =
    Cell.Mount<MetroToastHostInput, MetroToastHostCell>(key, input, factory)
