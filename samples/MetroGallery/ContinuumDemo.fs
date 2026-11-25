module MetroGallery.ContinuumDemo

open Goo
open FunGoo.Children
open FunGoo.Metro

/// State of the demo. One mutable field per concern.
type DemoState = {
  mutable Status: string
  // Slots for the two animation values. `create` fills them after the cell
  // exists.
  mutable Translate: Anim<Point>
  mutable Opacity: Anim<float>
}

let playEnter(state: DemoState, theme: MetroTheme) : unit =
  Continuum.playEnter {
    Theme = theme
    Translate = state.Translate
    Opacity = state.Opacity
    Delta = Continuum.defaultEnterDelta
  }

  state.Status <- "Entered (130 px, -70 px -> 0, 0)"

let playExit(state: DemoState, theme: MetroTheme) : unit =
  Continuum.playExit {
    Theme = theme
    Translate = state.Translate
    Opacity = state.Opacity
    Delta = Continuum.defaultExitDelta
  }

  state.Status <- "Exited (0, 0 -> 225 px, 73 px)"

let reset(state: DemoState) : unit =
  state.Translate.Snap(Point(X = 0.0, Y = 0.0))
  state.Opacity.Snap(1.0)
  state.Status <- "Reset"

/// The target of the flight. The two animation values move and fade it.
let flightTarget(state: DemoState, theme: MetroTheme) : Blob =
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

let view(state: DemoState) : Blob =
  let theme = Metro.read()

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.header "continuum flight",
      MetroText.secondary state.Status,
      Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 8.0)
        .Children(
          MetroTilt.mount(
            "continuum-enter",
            MetroButton.accent("Play Enter", fun () -> playEnter(state, theme))
          ),
          MetroTilt.mount(
            "continuum-exit",
            MetroButton.standard("Play Exit", fun () -> playExit(state, theme))
          ),
          MetroTilt.mount(
            "continuum-reset",
            MetroButton.subtle("Reset", fun () -> reset state)
          )
        ),
      Container(
        Height = Length.Pixels 104.0,
        AlignItems = AlignItems.Center,
        Overflow = Overflow.Hidden
      )
        .Children(flightTarget(state, theme))
    )

/// Creates the demo. The demo owns its two animation values and its status
/// text. The owner rule rebuilds the demo after each button click.
let create() : Cell =
  let state: DemoState = {
    Status = "Ready"
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  let cell = {
    new Cell() with
      override _.Build() : Blob = view state
  }

  // The cell exists now: Goo ties each animation to it.
  state.Translate <- cell.AnimateValue(Point(X = 0.0, Y = 0.0))
  state.Opacity <- cell.AnimateValue(1.0)
  cell
