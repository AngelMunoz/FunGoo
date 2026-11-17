namespace FunGoo.Mvu

open System
open Elmish
open Goo

type MvuCell<'init, 'model, 'msg>
  (
    [<InlineIfLambda>] post: (unit -> unit) -> bool,
    stop: 'msg,
    program: Program<'init, 'model, 'msg, Blob>,
    initArg: 'init
  ) as this =
  inherit Cell()

  let mutable session: ('model * Dispatch<'msg>) voption = ValueNone

  do
    program
    |> Program.withSetState(fun model dispatch ->
      session <- ValueSome(model, dispatch)
      this.Rebuild())
    |> Program.runWithDispatch
      (fun dispatch msg -> post(fun () -> dispatch msg) |> ignore)
      initArg

  member _.Current: 'model = session |> ValueOption.get |> fst

  member _.Dispatch: Dispatch<'msg> = session |> ValueOption.get |> snd

  override _.Build() : Blob =
    let model, dispatch = session |> ValueOption.get
    Program.view program model dispatch

  interface IDisposable with
    member _.Dispose() =
      session |> ValueOption.iter(fun (_, dispatch) -> dispatch stop)
