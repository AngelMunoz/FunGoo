namespace FunGoo.Mvu

open System
open Elmish
open Goo

/// A Goo cell that runs a stock Elmish program and rebuilds on each model change.
///
/// The cell holds one mutable value: the latest model and dispatch pair
/// that the Elmish loop publishes through setState. setState replaces the
/// pair, then the cell rebuilds, and Build reads the pair. That field is
/// the MVU store, and it is the only reassignment in the adapter.
///
/// Dispatch serialization: every dispatch posts the message through the
/// post function given at creation (the Window.TryPost shape), so update
/// never runs on the caller thread, including inside Build. A dispatch
/// whose post call returns false (no window yet, or the window has closed)
/// drops the message and reports the drop through the program error handler.
///
/// Commands: stock Elmish Cmd values run through this same dispatch, so a
/// command result message always reaches update on the post thread. The
/// command body follows Elmish and .NET rules, not UI rules: OfAsync starts
/// on a pool thread (Async.Start), and OfTask or OfValueTask continuations
/// resume on a pool thread because the Goo UI thread carries no
/// synchronization context. Only the synchronous prefix of a command, up to
/// its first await, runs on the post thread. Keep command bodies pure off
/// the UI thread, and keep heavy synchronous work out of a command prefix.
///
/// Disposal: Goo disposes a mounted cell through DisposeMounted, which calls
/// Dispose on a cell that implements IDisposable. Dispose dispatches the
/// stop message. The program must treat that message as its termination
/// message (Program.withTermination), so the Elmish loop stops every
/// subscription and rejects later messages itself. The adapter tracks no
/// subscriptions and keeps no stop flag. A dispatch already posted when
/// disposal runs still completes, in post order before the stop message.
type MvuCell<'init, 'model, 'msg> =
  inherit Cell

  /// Creates the cell and starts the Elmish loop synchronously on the
  /// calling thread. The loop publishes the initial model and dispatch
  /// before this returns, so Build always observes both. The stop message
  /// must match the program termination predicate.
  new:
    [<InlineIfLambda>] post: ((unit -> unit) -> bool) *
    stop: 'msg *
    program: Program<'init, 'model, 'msg, Blob> *
    initArg: 'init ->
      MvuCell<'init, 'model, 'msg>

  /// The latest model published by the Elmish loop.
  member Current: 'model

  /// The loop dispatch. It only posts, so it never runs update
  /// synchronously. After termination it reaches a loop that drops it.
  member Dispatch: Dispatch<'msg>

  /// Builds the current view from the stored model and dispatch pair.
  override Build: unit -> Blob

  interface IDisposable
