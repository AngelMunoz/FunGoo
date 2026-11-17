namespace FunGoo.Mvu

open System
open Elmish

/// Stock Elmish subscription helpers for Goo cells. Elmish itself ships
/// only Sub.none, Sub.batch, and Sub.map; these helpers cover the single
/// subscription shapes a Goo program needs, without a new runtime.
///
/// Goo events use System.Action delegates, not EventHandler delegates:
/// the contract lists Window and ElementHandle events as Action values
/// behind add_ and remove_ accessors. The action helpers below hide the
/// delegate construction that F# code would otherwise repeat at each
/// event site.
module MvuSub =

  /// One keyed subscription from a start function. The start function
  /// receives the loop dispatch and returns the disposable that stops it.
  val ofId:
    id: string -> subscribe: (Dispatch<'msg> -> IDisposable) -> Sub<'msg>

  /// A subscription that dispatches a tick message at the given interval.
  /// It uses a System.Threading.Timer on a pool thread; the message still
  /// flows through the cell post function, so update runs on the UI thread.
  /// Disposing the subscription stops the timer at once.
  val interval:
    id: string -> period: TimeSpan -> tick: (unit -> 'msg) -> Sub<'msg>

  /// A subscription over a Goo Action event. The add and remove functions
  /// take the handler, as the Window event accessors do (add_FocusChanged
  /// and remove_FocusChanged). The map function runs on the event thread;
  /// keep it pure, because the dispatch posts the message to the UI thread
  /// and update runs there. Disposing the subscription removes the handler.
  val fromActionEvent:
    id: string ->
    addHandler: (Action<'args> -> unit) ->
    removeHandler: (Action<'args> -> unit) ->
    map: ('args -> 'msg) ->
      Sub<'msg>

  /// A subscription over a Goo Action event with no arguments, such as
  /// Window DisplayChanged. Disposing the subscription removes the handler.
  val fromSignal:
    id: string ->
    addHandler: (Action -> unit) ->
    removeHandler: (Action -> unit) ->
    msg: 'msg ->
      Sub<'msg>

  /// A subscription over a .NET EventHandler event, for event sources
  /// outside Goo that use that delegate family. The map function runs on
  /// the event thread; keep it pure. Disposing the subscription removes
  /// the handler.
  val fromEventHandler:
    id: string ->
    addHandler: (EventHandler<'args> -> unit) ->
    removeHandler: (EventHandler<'args> -> unit) ->
    map: ('args -> 'msg) ->
      Sub<'msg>
