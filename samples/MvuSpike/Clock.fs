module MvuSpike.Clock

open System
open Elmish
open Goo
open FunGoo.Children
open FunGoo.Mvu

// One independent MVU program: model, messages, subscription, and view.
// The timer lives in this module alone, so a tick rebuilds this cell only.
type Model = { TimeText: string; Running: bool }

type Msg =
  | Tick of DateTime
  | Toggle
  | Stop


let init() : Model * Cmd<Msg> =
  { TimeText = ""; Running = false }, Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | Tick time ->
    {
      model with
          TimeText = time.ToString("HH:mm:ss")
    },
    Cmd.none
  | Toggle ->
    {
      model with
          Running = not model.Running
    },
    Cmd.none
  | Stop -> model, Cmd.none

let subscribe(model: Model) : Sub<Msg> =
  if model.Running then
    MvuSub.interval "clock" (TimeSpan.FromSeconds 1.0) (fun () ->
      Tick DateTime.Now)
  else
    Sub.none

let view (model: Model) (dispatch: Dispatch<Msg>) : Blob =
  let label = if model.Running then "Stop" else "Start"

  Container(Gap = 8)
    .Children(
      Text(Content = $"Time: {model.TimeText}"),
      Button(OnClick = fun _ -> dispatch Toggle).Children(Text(Content = label))
    )

let program: Program<unit, Model, Msg, Blob> =
  Program.mkProgram init update view
  |> Program.withSubscription subscribe
  |> Program.withTermination (fun msg -> msg = Stop) ignore
