module MetroGallery.Pages.MotionPage

open Goo
open FunGoo.Children
open FunGoo.Metro

/// The demo factories of the motion page. The gallery makes each demo cell
/// once, so that each mount reuses its factory.
type MotionPageCells = {
  Continuum: unit -> Cell
  Stagger: unit -> Cell
}

/// The motion page. The headless check of the gallery reads the mounted demos
/// and the shader state through these members.
[<AbstractClass>]
type MotionPageCell() =
  inherit Cell()

  /// The mounted continuum demo, for the headless check.
  abstract ContinuumDemo: Cell voption

  /// The mounted stagger demo, for the headless check.
  abstract StaggerDemo: Cell voption

  /// <c>true</c> when the compiled perspective shader is embedded.
  abstract ShaderAvailable: bool

  /// The number of builds, for the headless check.
  abstract Builds: int

/// One titled section of the page: a subheader in the type ramp, then the
/// content.
let section(title: string, content: Blob) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.subheader title, content)

/// The turnstile section: the shader demo that turns the Pivot selection in
/// 3D perspective when the compiled shader is embedded, with the 2D fallback
/// values beside it. The numbers match the retired motion tab.
let turnstile() : Blob =
  let shaderStatus =
    if MetroPerspective.isEmbeddedShaderAvailable then
      "Compiled Slang 3D perspective shader embedded"
    else
      "2D Fallback Active (compiled .goo-effect not embedded)"

  let struct (turnTransform, turnAlpha) =
    MetroFallbackMotion.turnstile2D(0.5, true)

  let struct (swivelTransform, swivelAlpha) =
    MetroFallbackMotion.swivel2D(0.5, true)

  let entering = MetroPerspective.turnstileIn 0.5
  let leaving = MetroPerspective.turnstileOut 0.5

  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 6.0)
    .Children(
      MetroText.header "perspective shader",
      MetroText.body shaderStatus,
      MetroText.secondary(
        $"Turnstile in: angleY={entering.AngleYDeg:F1}deg, "
        + $"opacity={entering.Opacity:F2}"
      ),
      MetroText.secondary(
        $"Turnstile out: angleY={leaving.AngleYDeg:F1}deg, "
        + $"opacity={leaving.Opacity:F2}"
      ),
      MetroText.secondary(
        $"Turnstile 2D: scaleX={turnTransform.ScaleX:F2}, "
        + $"opacity={turnAlpha:F2}"
      ),
      MetroText.secondary(
        $"Swivel 2D: scaleY={swivelTransform.ScaleY:F2}, "
        + $"opacity={swivelAlpha:F2}"
      )
    )

/// The page: the title, the turnstile shader demo, and the two motion demos.
/// The demos keep their sizes, order, and data.
let view(continuumFactory: unit -> Cell, staggerFactory: unit -> Cell) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 24.0,
    Padding =
      Edges(
        Length.Pixels 4.0,
        Length.Pixels 12.0,
        Length.Pixels 24.0,
        Length.Pixels 0.0
      )
  )
    .Children(
      MetroText.title "motion",
      section("PIVOT TURNSTILE", turnstile()),
      section(
        "CONTINUUM",
        Container()
          .Children(Cell.Mount<Cell>("continuum-demo", continuumFactory))
      ),
      section(
        "STAGGER",
        Container().Children(Cell.Mount<Cell>("stagger-demo", staggerFactory))
      )
    )

/// Creates the motion page. The page mounts the continuum and the stagger
/// demos and keeps them for the headless check.
let create(cells: MotionPageCells) : MotionPageCell =
  let mutable continuumRef: Cell voption = ValueNone
  let mutable staggerRef: Cell voption = ValueNone
  let mutable builds = 0

  let continuumFactory() : Cell =
    let demo = cells.Continuum()
    continuumRef <- ValueSome demo
    demo

  let staggerFactory() : Cell =
    let demo = cells.Stagger()
    staggerRef <- ValueSome demo
    demo

  {
    new MotionPageCell() with
      override _.ContinuumDemo = continuumRef
      override _.StaggerDemo = staggerRef
      override _.ShaderAvailable = MetroPerspective.isEmbeddedShaderAvailable
      override _.Builds = builds

      override _.Build() : Blob =
        builds <- builds + 1
        view(continuumFactory, staggerFactory)
  }
