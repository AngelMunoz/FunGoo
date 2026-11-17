module MvuSpike.App

open Goo
open FunGoo.Children
open FunGoo.Mvu

// Composition root, in order: window slot, cells, composition, window.
// The window attaches last because Root is init-only, and the cells post
// through the slot until the window exists.
let run() =
  Window.ConfigureApplication("MvuSpike", "0.1.0", "io.github.fungoo.mvuspike")

  let mutable target: Window voption = ValueNone

  let post action =
    target
    |> ValueOption.map(fun window -> window.TryPost(fun () -> action()))
    |> ValueOption.defaultValue false

  let counter =
    new MvuCell<int, Counter.Model, Counter.Msg>(
      post,
      Counter.Stop,
      Counter.program,
      10
    )

  let clock =
    new MvuCell<unit, Clock.Model, Clock.Msg>(
      post,
      Clock.Stop,
      Clock.program,
      ()
    )

  // The composition cell owns no model. It mounts the MVU cells through
  // factories made once, beside the cells they return, so a child rebuild
  // never runs this cell build and one child never touches the other.
  let counterFactory = fun () -> counter
  let clockFactory = fun () -> clock

  let root = {
    new Cell() with
      override _.Build() : Blob =
        Container(Padding = 24, Gap = 12, FlexDirection = FlexDirection.Column)
          .Children(
            Text("Goo Plus MVU", AlignSelf = AlignSelf.Center),
            Cell.Mount(counterFactory, "counter"),
            Cell.Mount(clockFactory, "clock")
          )
  }

  let window =
    Window(Title = "MvuSpike", Width = 640, Height = 480, Root = root)

  target <- ValueSome window
  window.Run()
