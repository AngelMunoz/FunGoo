namespace FunGoo.Mvu

open Elmish

/// Command helpers that stock Elmish does not ship.
module MvuCmd =

  /// A command that runs a plain action and dispatches no message, for
  /// view-side effects that update must trigger.
  val ofAction: action: (unit -> unit) -> Cmd<'msg>
