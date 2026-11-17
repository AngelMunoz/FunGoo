namespace FunGoo.Mvu

open Elmish

module MvuCmd =

  let ofAction(action: unit -> unit) : Cmd<'msg> =
    Cmd.ofEffect(fun _ -> action())
