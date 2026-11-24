module MetroGallery.StaggerDemo

open Goo
open FunGoo.Children
open FunGoo.Metro

module Content =

  let labels = [|
    "01  Typography-first hierarchy"
    "02  Content over chrome"
    "03  Sub-linear entrance stagger"
    "04  Direct paint-bound motion"
    "05  Zero-radius sharp geometry"
  |]

/// State of the demo. One mutable field per concern.
type DemoState = {
  // Slots for the animation values, one pair for each label. `create` fills
  // them after the cell exists.
  mutable Translates: Anim<Point> array
  mutable Opacities: Anim<float> array
}

/// The values that the functions of the demo work on.
type DemoContext = { State: DemoState }

/// Plays the entrance of each row again. The delay of a row grows with its
/// index. Under reduced motion, each row snaps to its rest.
let replay(ctx: DemoContext, theme: MetroTheme) : unit =
  let state = ctx.State

  for i = 0 to Content.labels.Length - 1 do
    if theme.ReducedMotion then
      state.Translates[i].Snap(Point(X = 0.0, Y = 0.0))
      state.Opacities[i].Snap(1.0)
    else
      let spec = MetroMotion.staggered(i, MetroMotion.normal)

      state.Translates[i]
        .Play(Point(X = 40.0, Y = 0.0), Point(X = 0.0, Y = 0.0), spec)

      state.Opacities[i].Play(0.0, 1.0, spec)

/// One row of the stagger: the label and its delay.
let row(ctx: DemoContext, index: int) : Blob =
  let delayMs = MetroMotion.staggerDelayMs index
  let theme = Metro.read()

  Container(
    Key = $"stagger-{index}",
    FlexDirection = FlexDirection.Row,
    AlignItems = AlignItems.Center,
    JustifyContent = JustifyContent.SpaceBetween,
    Padding = Edges(Length.Pixels 10.0, Length.Pixels 14.0),
    BackgroundColor = theme.Palette.BackgroundAlt,
    BorderWidth =
      Edges(
        Length.Pixels 0.0,
        Length.Pixels 0.0,
        Length.Pixels 0.0,
        Length.Pixels 4.0
      ),
    BorderColor = Edges<Color>(theme.Accent.Base),
    TranslateAnim = ctx.State.Translates[index],
    OpacityAnim = ctx.State.Opacities[index]
  )
    .Children(
      MetroText.body Content.labels[index],
      MetroText.caption $"+{int delayMs} ms"
    )

let view(ctx: DemoContext) : Blob =
  let theme = Metro.read()

  let list =
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)

  for i = 0 to Content.labels.Length - 1 do
    list.Add(row(ctx, i))

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.header "staggered entrance",
      Container(FlexDirection = FlexDirection.Row)
        .Children(
          MetroButton.standard("Replay Stagger", fun () -> replay(ctx, theme))
        ),
      list
    )

/// Creates the demo. The demo owns one pair of animation values for each row.
/// The button is a Blob of the demo, so the owner rule rebuilds it.
let create() : Cell =
  let state: DemoState = {
    Translates = Array.empty
    Opacities = Array.empty
  }

  let ctx: DemoContext = { State = state }

  let cell = {
    new Cell() with
      override _.Build() : Blob = view ctx
  }

  // The cell exists now: Goo ties each animation to it.
  state.Translates <-
    Array.init Content.labels.Length (fun _ ->
      cell.AnimateValue(Point(X = 0.0, Y = 0.0)))

  state.Opacities <-
    Array.init Content.labels.Length (fun _ -> cell.AnimateValue(1.0))

  cell
