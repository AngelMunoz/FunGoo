module MetroGallery.ContinuumDemo

open Goo
open FunGoo.Children
open FunGoo.Metro

/// State of the demo. One mutable field per concern.
type private DemoState = {
  mutable Status: string
  // Slots for the two animation values. `create` fills them after the cell
  // exists.
  mutable Translate: Anim<Point>
  mutable Opacity: Anim<float>
}

/// The values that the functions of the demo work on.
type private DemoContext = { State: DemoState }

let private playEnter(ctx: DemoContext, theme: MetroTheme) : unit =
  let state = ctx.State

  Continuum.playEnter(
    theme,
    state.Translate,
    state.Opacity,
    Continuum.defaultEnterDelta
  )

  state.Status <- "Entered (130 px, -70 px -> 0, 0)"

let private playExit(ctx: DemoContext, theme: MetroTheme) : unit =
  let state = ctx.State

  Continuum.playExit(
    theme,
    state.Translate,
    state.Opacity,
    Continuum.defaultExitDelta
  )

  state.Status <- "Exited (0, 0 -> 225 px, 73 px)"

let private reset(state: DemoState) : unit =
  state.Translate.Snap(Point(X = 0.0, Y = 0.0))
  state.Opacity.Snap(1.0)
  state.Status <- "Reset"

/// The target of the flight. The two animation values move and fade it.
let private flightTarget(state: DemoState, theme: MetroTheme) : Blob =
  Container(
    Width = Length.Pixels 260.0,
    Padding = Edges(Length.Pixels 16.0),
    BackgroundColor = theme.Accent.Base,
    Color = theme.Accent.OnAccent,
    BorderRadius = Length.Pixels 0.0,
    BoxShadows = Array.empty,
    TranslateAnim = state.Translate,
    OpacityAnim = state.Opacity
  )
    .Children(
      MetroText.build {
        MetroText.defaults("Shared Element", MetroTextRole.Subtitle) with
            Tone = MetroTextTone.OnAccent
      },
      MetroText.build {
        MetroText.defaults(
          "Continuum exponential ease-out",
          MetroTextRole.Caption
        ) with
            Tone = MetroTextTone.OnAccent
      }
    )

let private view(ctx: DemoContext) : Blob =
  let theme = Metro.read()

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.header "continuum flight",
      MetroText.secondary ctx.State.Status,
      Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 8.0)
        .Children(
          MetroButton.accent("Play Enter", fun () -> playEnter(ctx, theme)),
          MetroButton.standard("Play Exit", fun () -> playExit(ctx, theme)),
          MetroButton.subtle("Reset", fun () -> reset ctx.State)
        ),
      Container(
        Height = Length.Pixels 104.0,
        AlignItems = AlignItems.Center,
        Overflow = Overflow.Hidden
      )
        .Children(flightTarget(ctx.State, theme))
    )

/// Creates the demo. The demo owns its two animation values and its status
/// text. Each button is a Blob of the demo, so the owner rule rebuilds it.
let create() : Cell =
  let state: DemoState = {
    Status = "Ready"
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  let ctx: DemoContext = { State = state }

  let cell = {
    new Cell() with
      override _.Build() : Blob = view ctx
  }

  // The cell exists now: Goo ties each animation to it.
  state.Translate <- cell.AnimateValue(Point(X = 0.0, Y = 0.0))
  state.Opacity <- cell.AnimateValue(1.0)
  cell
