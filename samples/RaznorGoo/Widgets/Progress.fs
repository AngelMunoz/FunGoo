module GooRes.Widgets.Progress

open Goo
open FunGoo.Children
open Mibo.Adaptive

type ProgressProps = {
  value: float32 aval
  maxValue: float32 aval
  color: Color
  trackColor: Color
  height: float
}

let inline create(p: ProgressProps) : Blob =
  let v = AVal.getValue p.value
  let m = AVal.getValue p.maxValue
  let pct = if m <= 0.0f then 0.0f else v * 100.0f / m

  Container(
    Width = Length.Percent 100,
    Height = p.height,
    BackgroundColor = p.trackColor,
    BorderRadius = 4
  )
    .Children(
      Container(
        Width = Length.Percent(float pct),
        Height = Length.Percent 100,
        BackgroundColor = p.color,
        BorderRadius = 4
      )
    )
