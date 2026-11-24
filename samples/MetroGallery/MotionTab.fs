module MetroGallery.MotionTab

open Goo
open FunGoo.Children
open FunGoo.Metro

/// The motion tab: the two motion demos, then the status of the perspective
/// shader. <paramref name="continuumFactory"/> and
/// <paramref name="staggerFactory"/> make the cells of the demos. Make each
/// factory once, in the cell that mounts the tab.
let view(continuumFactory: unit -> Cell, staggerFactory: unit -> Cell) : Blob =
  let shaderStatus =
    if MetroPerspective.isEmbeddedShaderAvailable then
      "Compiled Slang 3D perspective shader embedded"
    else
      "2D Fallback Active (compiled .goo-effect not embedded)"

  let struct (turnTransform, turnAlpha) =
    MetroFallbackMotion.turnstile2D(0.5, true)

  let struct (swivelTransform, swivelAlpha) =
    MetroFallbackMotion.swivel2D(0.5, true)

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
      Container().Children(Cell.Mount<Cell>("continuum-demo", continuumFactory)),
      Container().Children(Cell.Mount<Cell>("stagger-demo", staggerFactory)),
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 6.0)
        .Children(
          MetroText.header "perspective shader",
          MetroText.body shaderStatus,
          MetroText.secondary(
            $"Turnstile 2D: scaleX={turnTransform.ScaleX:F2}, "
            + $"opacity={turnAlpha:F2}"
          ),
          MetroText.secondary(
            $"Swivel 2D: scaleY={swivelTransform.ScaleY:F2}, "
            + $"opacity={swivelAlpha:F2}"
          )
        )
    )
