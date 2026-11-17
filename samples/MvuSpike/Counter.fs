module MvuSpike.Counter

open System.Threading.Tasks
open Elmish
open Goo
open FunGoo.Children

// One independent MVU program: model, messages, commands, and view.
type Model = { Count: int }

type Msg =
  | Increment
  | Reset
  | AddLater
  | Added of int
  | Stop


let init initial : Model * Cmd<Msg> = { Count = initial }, Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | Increment -> { model with Count = model.Count + 1 }, Cmd.none
  | Reset -> { model with Count = 0 }, Cmd.none
  | AddLater ->
    model,
    Cmd.OfTask.perform
      (fun () -> task {
        do! Task.Delay 500
        return 5
      })
      ()
      Added
  | Added value ->
    {
      model with
          Count = model.Count + value
    },
    Cmd.none
  | Stop -> model, Cmd.none

let view (model: Model) (dispatch: Dispatch<Msg>) : Blob =
  Container(Gap = 8)
    .Children(
      Text(Content = $"Count: {model.Count}"),
      Button(OnClick = fun _ -> dispatch Increment)
        .Children(Text(Content = "Add one")),
      Button(OnClick = fun _ -> dispatch Reset)
        .Children(Text(Content = "Reset")),
      Button(OnClick = fun _ -> dispatch AddLater)
        .Children(Text(Content = "Add five later"))
    )

let program: Program<int, Model, Msg, Blob> =
  Program.mkProgram init update view
  |> Program.withTermination (fun msg -> msg = Stop) ignore
