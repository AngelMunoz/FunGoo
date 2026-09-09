module GooRes.Widgets.MediaMenu

open Goo
open GooRes.Icons
open GooRes.Types
open FunGoo.Children
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
  (icon: VectorAsset)
  ([<InlineIfLambda>] onClick: unit -> unit)
  =
  Button(OnClick = fun _ -> onClick())
    .Children(Container(Width = 24, Height = 24).Children(icon.Render()))

let inline loopIcon(loop: LoopState) : VectorAsset =
  match loop with
  | LoopState.Off -> repeatOff
  | LoopState.All -> repeat
  | LoopState.Single -> repeatOne

let inline create(p: MediaMenuProps) : Blob =
  let playing = AVal.getValue p.isPlaying
  let loop = AVal.getValue p.loop

  Container(
    FlexDirection = FlexDirection.Row,
    Gap = 8,
    AlignItems = AlignItems.Center
  )
    .Children(
      button previous p.onPrevious,
      button (if playing then pause else play) p.onPlayPause,
      button next p.onNext,
      button shuffle p.onShuffle,
      button (loopIcon loop) p.onLoop
    )
