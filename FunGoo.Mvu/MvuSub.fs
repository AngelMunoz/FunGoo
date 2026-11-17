namespace FunGoo.Mvu

open System
open System.Threading
open Elmish

module MvuSub =

  let ofId (id: string) (subscribe: Dispatch<'msg> -> IDisposable) : Sub<'msg> =
    List.singleton([ id ], subscribe)

  let interval
    (id: string)
    (period: TimeSpan)
    (tick: unit -> 'msg)
    : Sub<'msg> =
    let subscribe(dispatch: Dispatch<'msg>) : IDisposable =
      new Timer(TimerCallback(fun _ -> dispatch(tick())), null, period, period)

    ofId id subscribe

  let fromActionEvent
    (id: string)
    (addHandler: Action<'args> -> unit)
    (removeHandler: Action<'args> -> unit)
    (map: 'args -> 'msg)
    : Sub<'msg> =
    let subscribe(dispatch: Dispatch<'msg>) : IDisposable =
      let handler = Action<'args>(fun args -> dispatch(map args))
      addHandler handler

      {
        new IDisposable with
          member _.Dispose() = removeHandler handler
      }

    ofId id subscribe

  let fromSignal
    (id: string)
    (addHandler: Action -> unit)
    (removeHandler: Action -> unit)
    (msg: 'msg)
    : Sub<'msg> =
    let subscribe(dispatch: Dispatch<'msg>) : IDisposable =
      let handler = Action(fun () -> dispatch msg)
      addHandler handler

      {
        new IDisposable with
          member _.Dispose() = removeHandler handler
      }

    ofId id subscribe

  let fromEventHandler
    (id: string)
    (addHandler: EventHandler<'args> -> unit)
    (removeHandler: EventHandler<'args> -> unit)
    (map: 'args -> 'msg)
    : Sub<'msg> =
    let subscribe(dispatch: Dispatch<'msg>) : IDisposable =
      let handler = EventHandler<'args>(fun _ args -> dispatch(map args))

      addHandler handler

      {
        new IDisposable with
          member _.Dispose() = removeHandler handler
      }

    ofId id subscribe
