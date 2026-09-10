module GooRes.Widgets.Progress

open System
open Goo
open Goo.Widgets.Feedback
open FunGoo.Children
open FunGoo.Widgets
open Mibo.Adaptive

type ProgressProps = {
  value: float32 aval
  maxValue: float32 aval
  color: Color
  trackColor: Color
  height: float
}

// ProgressBar's root is a fixed pixel width, so the factory rebuilds the root
// at 100% width and keeps the resolved track, fill, and clipping composition.
let inline create(p: ProgressProps) : Blob =
  let v = AVal.getValue p.value
  let m = AVal.getValue p.maxValue
  let value = if m <= 0.0f then 0.0 else float v / float m

  ProgressBar(Value = value, Height = p.height)
    .trackColor(p.trackColor)
    .fillColor(p.color)
    .Build()
