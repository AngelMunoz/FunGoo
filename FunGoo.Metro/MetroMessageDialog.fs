namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroDialogCommand = {
  Id: string
  Label: string
  IsPrimary: bool
}

[<Struct; NoComparison>]
type MetroMessageDialogResult =
  | CommandInvoked of id: string
  | Dismissed

[<Struct; NoComparison>]
type MetroMessageDialogProps = {
  Open: bool
  Title: string voption
  Message: string
  Commands: MetroDialogCommand array
  Dismissible: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type DialogRenderArgs = {
  Props: MetroMessageDialogProps
  Scale: Anim<float> voption
  Backdrop: Anim<float> voption
}

[<AbstractClass>]
type MetroMessageDialogCell() =
  inherit Cell<MetroMessageDialogProps>()

  abstract IsVisible: bool

  abstract IsAnimating: bool

  abstract BackdropOpacity: Anim<float>

  abstract View: Blob

  abstract Handle: result: MetroMessageDialogResult -> unit

  // Gives one dialog result to the host cell through InvokeInputCallback.
  member this.Raise
    (
      callback: Action<MetroMessageDialogResult>,
      result: MetroMessageDialogResult
    ) =
    this.InvokeInputCallback(callback, result)

module MessageDialogInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0

  let defaultCommands = [|
    {
      Id = "ok"
      Label = "OK"
      IsPrimary = true
    }
  |]

  let resolveCommands
    (commands: MetroDialogCommand array)
    : MetroDialogCommand array =
    if isNull commands || commands.Length = 0 then
      defaultCommands
    elif commands.Length <= 3 then
      commands
    else
      Array.sub commands 0 3

  let buildContent
    (args: DialogRenderArgs, onResult: MetroMessageDialogResult -> unit)
    : struct (Blob * Blob) =
    let props = args.Props
    let theme = Metro.read()
    let p = theme.Palette
    let commands = resolveCommands props.Commands
    let msg = if isNull props.Message then "" else props.Message

    let accName =
      props.AccessibilityName
      |> ValueOption.orElse props.Title
      |> ValueOption.defaultValue "Message dialog"

    let footer =
      Container(
        FlexDirection = FlexDirection.Row,
        JustifyContent = JustifyContent.FlexEnd,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 12.0,
        Width = Length.Percent 100.0
      )

    for i = 0 to commands.Length - 1 do
      let cmd = commands[i]
      let cmdId = cmd.Id

      let btnProps = {
        MetroButton.defaults cmd.Label with
            Variant =
              if cmd.IsPrimary then
                MetroButtonVariant.Accent
              else
                MetroButtonVariant.Standard
            MinWidth = ValueSome 96.0
      }

      let btn =
        MetroButton.build(
          btnProps,
          fun () -> onResult(MetroMessageDialogResult.CommandInvoked cmdId)
        )

      btn.Key <- $"msg-dialog-cmd-{cmdId}"
      footer.Add(btn)

    let inner =
      Container(
        FlexDirection = FlexDirection.Column,
        Width = Length.Percent 100.0,
        MaxWidth = Length.Pixels 560.0,
        Gap = Length.Pixels 16.0
      )

    props.Title
    |> ValueOption.iter(fun title ->
      if not(String.IsNullOrWhiteSpace title) then
        inner.Add(MetroText.subheader title))

    inner.Add(MetroText.body msg)
    inner.Add(footer)

    let band =
      Container(
        Key = "msg-dialog-band",
        Width = Length.Percent 100.0,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Padding = Edges(Length.Pixels 24.0, Length.Pixels 32.0),
        BorderRadius = zeroRadius,
        BorderWidth =
          Edges(
            Length.Pixels 2.0,
            Length.Pixels 0.0,
            Length.Pixels 2.0,
            Length.Pixels 0.0
          ),
        BorderColor =
          Edges<Color>(
            (if theme.HighContrast then
               p.Foreground
             else
               theme.Accent.Base),
            Color.Transparent,
            (if theme.HighContrast then
               p.Foreground
             else
               theme.Accent.Base),
            Color.Transparent
          ),
        BoxShadows = emptyShadows,
        BackgroundColor = p.Background,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Dialog,
            Name = accName,
            Modal = Nullable true
          )
      )
        .Children(inner)

    args.Scale |> ValueOption.iter(fun s -> band.ScaleAnim <- s)

    let primaryCmd =
      commands
      |> Array.tryFind(fun c -> c.IsPrimary)
      |> Option.defaultValue commands[0]

    let enterAction =
      Action(fun () ->
        onResult(MetroMessageDialogResult.CommandInvoked primaryCmd.Id))

    let bindings = [| KeyBinding(Key.Enter, KeyModifiers.None, enterAction) |]

    band.KeyBindings <- bindings

    let scrim =
      if theme.IsDark then
        Color.Black.WithAlpha(0.65)
      else
        Color.Black.WithAlpha(0.45)

    let backdrop =
      Container(
        Key = "msg-dialog-backdrop",
        Position = PositionType.Absolute,
        Inset = Length.Pixels 0.0,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        BackgroundColor = scrim
      )

    if props.Dismissible then
      backdrop.OnPointerDown <-
        Action<PointerEvent>(fun _ ->
          onResult MetroMessageDialogResult.Dismissed)

    args.Backdrop |> ValueOption.iter(fun b -> backdrop.OpacityAnim <- b)

    struct (backdrop :> Blob, band :> Blob)

  let buildPortal
    (args: DialogRenderArgs, onResult: MetroMessageDialogResult -> unit)
    : Blob =
    let props = args.Props
    let struct (backdrop, band) = buildContent(args, onResult)

    let portal =
      Portal(
        Key = "msg-dialog-portal",
        FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
        Scrim = Nullable Color.Transparent,
        ZIndex = 50,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.Center,
        AlignItems = AlignItems.Stretch
      )

    if props.Dismissible then
      portal.OnDismiss <-
        Action<PopupDismissReason>(fun _ ->
          onResult MetroMessageDialogResult.Dismissed)

    portal.Add(backdrop)
    portal.Add(band)
    portal :> Blob

[<RequireQualifiedAccess>]
module MetroMessageDialog =

  let primaryCommand(id: string, label: string) : MetroDialogCommand = {
    Id = id
    Label = label
    IsPrimary = true
  }

  let command(id: string, label: string) : MetroDialogCommand = {
    Id = id
    Label = label
    IsPrimary = false
  }

  let defaults
    (title: string voption, message: string, isOpen: bool)
    : MetroMessageDialogProps = {
    Open = isOpen
    Title = title
    Message = message
    Commands = MessageDialogInternals.defaultCommands
    Dismissible = true
    AccessibilityName = ValueNone
  }

  let buildContent
    (args: DialogRenderArgs, onResult: MetroMessageDialogResult -> unit)
    : struct (Blob * Blob) =
    MessageDialogInternals.buildContent(args, onResult)

  let build
    (props: MetroMessageDialogProps, onResult: MetroMessageDialogResult -> unit)
    : Blob =
    if not props.Open then
      Container(Key = "msg-dialog-closed", Display = Display.None) :> Blob
    else
      MessageDialogInternals.buildPortal(
        {
          Props = props
          Scale = ValueNone
          Backdrop = ValueNone
        },
        onResult
      )

  type DialogState = {
    mutable Props: MetroMessageDialogProps
    mutable LastOpen: bool voption
    mutable Closing: bool
    mutable Requested: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable Scale: Anim<float>
    mutable Backdrop: Anim<float>
  }

  type DialogContext = {
    State: DialogState
    Raise: Action<MetroMessageDialogResult>
  }

  let initialState() : DialogState = {
    Props = defaults(ValueNone, "", false)
    LastOpen = ValueNone
    Closing = false
    Requested = false
    LastReducedMotion = false
    ScopedReducedMotion = false
    Scale = Unchecked.defaultof<Anim<float>>
    Backdrop = Unchecked.defaultof<Anim<float>>
  }

  let snapOpen(state: DialogState) : unit =
    state.Closing <- false
    state.Scale.Snap(1.0)
    state.Backdrop.Snap(1.0)

  let snapClosed(state: DialogState) : unit =
    state.Closing <- false
    state.Scale.Snap(0.95)
    state.Backdrop.Snap(0.0)

  let isVisible(state: DialogState) : bool = state.Props.Open || state.Closing

  let view(ctx: DialogContext) : Blob =
    let state = ctx.State

    if not(isVisible state) then
      Container(Key = "msg-dialog-closed", Display = Display.None) :> Blob
    else
      MessageDialogInternals.buildPortal(
        {
          Props = state.Props
          Scale = ValueSome state.Scale
          Backdrop = ValueSome state.Backdrop
        },
        fun result -> ctx.Raise.Invoke(result)
      )

  let inputChanged
    (
      ctx: DialogContext,
      input: MetroMessageDialogProps,
      hostReduced: bool voption
    ) : unit =
    let state = ctx.State
    state.Props <- input
    state.Requested <- false

    let reduced =
      state.ScopedReducedMotion
      || ValueOption.defaultValue state.LastReducedMotion hostReduced

    let changed =
      state.LastOpen
      |> ValueOption.map(fun prev -> prev <> input.Open)
      |> ValueOption.defaultValue false

    if changed then
      if input.Open then
        let wasClosing = state.Closing
        state.Closing <- false

        if reduced then
          snapOpen state
        elif wasClosing then
          // A reopen during the close animation continues from the current
          // value; Play would snap to the start of the fade.
          state.Scale.To(1.0, MetroContentDialog.transitionSpec)
          state.Backdrop.To(1.0, MetroContentDialog.transitionSpec)
        else
          state.Scale.Play(0.95, 1.0, MetroContentDialog.transitionSpec)
          state.Backdrop.Play(0.0, 1.0, MetroContentDialog.transitionSpec)
      elif reduced then
        snapClosed state
      else
        state.Closing <- true
        state.Scale.To(0.95, MetroContentDialog.transitionSpec)
        state.Backdrop.To(0.0, MetroContentDialog.transitionSpec)
    elif state.LastOpen.IsNone then
      if input.Open then snapOpen state else snapClosed state

    state.LastOpen <- ValueSome input.Open

  let unmount(state: DialogState) : unit =
    state.LastOpen <- ValueNone
    state.ScopedReducedMotion <- false
    snapClosed state

  let buildCell
    (
      ctx: DialogContext,
      input: MetroMessageDialogProps,
      hostReduced: bool voption
    ) : Blob =
    let state = ctx.State
    state.Props <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      if input.Open then snapOpen state else snapClosed state
    elif state.LastOpen.IsNone then
      inputChanged(ctx, input, hostReduced)

    view ctx

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let create
    (onResult: MetroMessageDialogResult -> unit)
    : MetroMessageDialogCell =
    let state = initialState()
    let mutable self: MetroMessageDialogCell voption = ValueNone
    let onResultAction = Action<MetroMessageDialogResult>(onResult)

    let raiseResult(result: MetroMessageDialogResult) =
      if not state.Closing && not state.Requested then
        state.Requested <- true

        self |> ValueOption.iter(fun cell -> cell.Raise(onResultAction, result))

    let raise = Action<MetroMessageDialogResult>(raiseResult)

    let context: DialogContext = { State = state; Raise = raise }

    let cell = {
      new MetroMessageDialogCell() with
        override _.IsVisible = isVisible state

        override _.IsAnimating = state.Scale.Running || state.Backdrop.Running

        override _.BackdropOpacity = state.Backdrop
        override _.View = view context
        override _.Handle(result) = raiseResult result

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          buildCell(context, input, reducedMotionOf this.Host)
    }

    state.Scale <- cell.AnimateValue(0.95)
    state.Backdrop <- cell.AnimateValue(0.0)

    state.Backdrop.add_Completed(
      Action<MotionCompletionReason>(fun _ ->
        if not state.Props.Open && state.Closing then
          state.Closing <- false
          cell.Rebuild())
    )

    self <- ValueSome cell
    cell

  let createFactory
    (onResult: MetroMessageDialogResult -> unit)
    : unit -> MetroMessageDialogCell =
    fun () -> create onResult

  let mount
    (
      key: string,
      input: MetroMessageDialogProps,
      factory: unit -> MetroMessageDialogCell
    ) : Blob =
    Cell.Mount<MetroMessageDialogProps, MetroMessageDialogCell>(
      key,
      input,
      factory
    )
