module GooRes.Widgets.MediaMenu

open Goo
open Goo.Widgets.Actions
open GooRes.Icons
open GooRes.Types
open FunGoo.Children
open FunGoo.Widgets
open Mibo.Adaptive

type MediaMenuProps = {
  isPlaying: bool aval
  loop: LoopState aval
  onPlayPause: unit -> unit
  onNext: unit -> unit
  onPrevious: unit -> unit
  onShuffle: unit -> unit
  onLoop: unit -> unit
}

let inline button
  (icon: Blob)
  (name: string)
  ([<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  IconButton(
    Icon = icon,
    AccessibilityName = name,
    OnClick = fun _ -> onClick()
  )
    .Build()

let inline loopButton
  (loop: LoopState)
  ([<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  let icon =
    match loop with
    | LoopState.Off -> repeatOff
    | LoopState.All -> repeat
    | LoopState.Single -> repeatOne

  IconButton(
    Icon = icon,
    AccessibilityName = "Loop",
    Active = (loop <> LoopState.Off),
    OnClick = fun _ -> onClick()
  )
    .Build()

let inline create(p: MediaMenuProps) : Blob =
  let playing = AVal.getValue p.isPlaying
  let loop = AVal.getValue p.loop

  Container(
    FlexDirection = FlexDirection.Row,
    Gap = 8,
    AlignItems = AlignItems.Center
  )
    .Children(
      button previous "Previous" p.onPrevious,
      button
        (if playing then pause else play)
        (if playing then "Pause" else "Play")
        p.onPlayPause,
      button next "Next" p.onNext,
      button shuffle "Shuffle" p.onShuffle,
      loopButton loop p.onLoop
    )
