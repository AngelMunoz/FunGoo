---
name: goo-fsharp
description: Author F# applications on the Goo retained GUI framework through the FunGoo bindings. Covers the Cell/Build/Blob model, the shape of a view file with plain functions and a create function that returns an object expression, abstract cell types for public behavior, who rebuilds after a callback, the self slot and AnimateValue for an object expression, data-only props records with callbacks as parameters, closure state, stores for shared state, env capabilities with a PostUI marshaller, the composition-root skeleton, mount identity and keys, and the F#/Goo interop friction families. Use whenever you write, review, or debug F# code that opens Goo or FunGoo, makes a Cell, overrides Build, mounts a child Cell, chooses between an object expression and a class, or constructs a Window — also when the user says Goo, FunGoo, Cell, Blob, mount, rebuild, InvokeInputCallback, or Goo window.
compatibility: Requires the .NET 10 SDK and a project referencing Goo. Goo is written in G# and compiled to ordinary .NET IL; F# consumes it directly. FunGoo supplies thin generated bindings.
---

# Goo from F#

Goo is a retained GUI tree with a React-style reconciler, rendered on Vulkan, laid out with
flexbox. F# code declares trees; Goo diffs, lays out, and renders them. This skill holds the
programming model and the application architecture. For the control library, read
`goo-widgets-fsharp` after this one. A complete worked application lives in
`samples/RaznorGoo` in this repository.

## Mental model

- A component is a `Cell`. Its `Build()` returns a `Blob` tree (`Container`, `Text`, `Button`,
  `Shape`). `Build` runs only when the Cell is dirty — not every frame.
- Blobs are declarations. Goo diffs them against retained nodes, then discards them. A fresh
  tree per `Build` is the design, so declaring the tree allocates by design. Extra passes,
  closure churn, and per-row formatting are not part of that budget.
- The reconciler reuses a retained node when the kind and the key match. Unkeyed children
  match by sibling index. Never mix keyed and unkeyed siblings under one parent.
- The owner rule: after a callback runs, Goo rebuilds the cell that owns the callback. That
  is your cell for a callback on a Blob of your `Build`, and for the input callback of a
  mounted Goo.Widgets control. It is the other cell for a callback on a Blob that the other
  cell created. See "Who rebuilds after a callback".
- `Rebuild()` marks one cell dirty. It throws when called during `Build`. A second call does
  nothing while the cell is dirty.
- One process-wide UI thread, no synchronization context. Background code returns through
  `Window.Post`, or through `Cell.Post` inside a cell.
- Layout is CSS flexbox as style properties: `FlexDirection`, `Gap`, `FlexGrow`, `Padding`,
  `Overflow`. There are no constraint objects. `Style` is a write-once declaration sink: each
  init-only property pushes an entry. `BasedOn` copies another style's entries; assign it
  before overrides.

## The shape of a view file

A cell is not a class. It is a module of plain functions and one `create` function that
returns an object expression. The parameters of `create` are the inputs of the cell, and its
closure holds the state, mutable included. A test calls each plain function with no cell and
no window.

A file has four parts, in this order:

1. **Data.** The state record (one mutable field per concern) and one DU for the events.
2. **The contract, when outside code drives the state of the cell.** An abstract cell type:
   it inherits `Cell`, declares the members, and holds no logic and no state. This case is
   rare, but it exists: a shell that the composition root navigates, a page that the root
   starts, a cell that a headless check reads.
3. **Plain functions.** One transition function for the events, the view pieces, and the
   page. Each view piece takes its callback as a parameter.
4. **`create`, last.** It returns an object expression. Each member calls one plain function.

```fsharp
module App.Views.Search

open Goo
open FunGoo.Children

// 1. Data.
type SearchResult = { Id: string; Title: string }
type SearchState = { mutable Query: string; mutable Results: SearchResult array }

type SearchEvent =
    | QueryChanged of string
    | Cleared

// 2. The contract. Outside code calls `Start`.
[<AbstractClass>]
type SearchCell() =
    inherit Cell()
    abstract Start: unit -> unit

// 3. Plain functions. A test calls each one with no cell and no window.
let apply (env: #IProvideSearch, state: SearchState, event: SearchEvent) =
    match event with
    | QueryChanged query ->
        state.Query <- query
        state.Results <- env.Search.Find query
    | Cleared ->
        state.Query <- ""
        state.Results <- Array.empty

let resultRow (result: SearchResult) : Blob =
    Text(Key = $"result-{result.Id}", Content = result.Title)

let page (state: SearchState, onEvent: SearchEvent -> unit) : Blob =
    let rows = state.Results |> Array.map resultRow

    Container(FlexDirection = FlexDirection.Column, Gap = 8)
        .Children
            [|
                Text(Content = state.Query) :> Blob

                // A Blob of this cell. The owner rule rebuilds the cell after the click.
                Button(OnClick = fun () -> onEvent Cleared)
                    .Children(Text(Content = "Clear")) :> Blob

                yield! rows
            |]

// 4. `create` is last. The closure holds the state.
let create env : SearchCell =
    let state = { Query = ""; Results = Array.empty }

    // Created once.
    let onEvent (event: SearchEvent) = apply (env, state, event)

    { new SearchCell() with
        override this.Start() =
            state.Results <- env.Search.Find state.Query
            this.Rebuild()

        override _.Build() : Blob = page (state, onEvent) }
```

`IProvideSearch` is a capability (see "Services"). The mixed child list needs the array form
with `:> Blob` and `yield!`, because F# does not permit `yield!` in an argument list.

Why not a class: a class moves the logic into `let` bindings and `override` bodies that no
test reaches without the cell, and it starts with `as this`, because only a class gives
`this` to a `let` binding. "The cell inside its own callbacks" gives the same reach inside
an object expression. Do not write a named class that holds logic or state. An object
expression behaves like the type that it expresses, thus a G# base type also takes one, for
example `{ new EmbeddedWindowHost() with override _.RequestFrame() = () ... }`.

**Typed input.** A plain cell owns all its state. A typed cell receives an immutable data
snapshot from its parent and rebuilds when `EqualityComparer<'TInput>.Default` reports the
snapshot changed. This choice is independent of the shape of the file:

```fsharp
type LogPageInput = { Log: LogKind; Commits: Commit[] }   // data only

let logCell () : Cell<LogPageInput> =
    { new Cell<LogPageInput>() with
        override _.Build(input: LogPageInput) : Blob = ... }
```

Mount it with `Cell<LogPageInput>` as the second type argument and a factory:

```fsharp
Cell.Mount<LogPageInput, Cell<LogPageInput>>("page-log", input, logCell)
```

Rules:

- Keep the input a data-only record so that comparison is cheap and precise. `Build` does
  no I/O, blocks nothing, calls no `Rebuild`.
- An annotated `Blob` return upcasts a `Container`; inside child lists a derived Blob needs
  no cast at member calls.
- The form of `Cell.Mount` without a factory needs a class that F# can construct. Use the
  factory form.
- An abstract cell type is necessary in two cases only: outside code drives the state of
  the cell through members, or the cell gives a callback to its host through
  `InvokeInputCallback` (next section). An object expression cannot declare a member of
  its own (FS0767), thus that forwarding member needs the named type.

## Who rebuilds after a callback

The owner rule rebuilds one cell after a callback: the cell that owns the callback when it
runs. Goo records the owner when a cell creates the Blob that holds the callback, not when
the callback is written. A Goo.Widgets control is one exception: it hands the callback to the
host cell.

| The callback is ... | Goo rebuilds | Your cell calls `Rebuild()` |
| --- | --- | --- |
| on a Blob that your `Build` creates (`Button`, `Container`) | your cell | no |
| on the input of a mounted Goo.Widgets control (`OnChange`, `OnSelect`, `OnClick`) | your cell, because the control calls `InvokeInputCallback` | no |
| on a Blob that a different cell creates: a render function that you pass to a library cell and that the cell calls inside its `Build`; a Blob of a typed cell of yours | that cell | yes |
| a timer of your cell (`Cell.After`, `Cell.Every`) | your cell | no |
| absent: the composition root or a service handler wrote state | no cell | yes, for each cell that reads the changed state |

A state change with no rebuild is a stale screen. When the row is not clear, call `Rebuild()`
on your cell: the call does nothing when the owner rule already marked the cell dirty. The
third row causes most defects, because the callback is written in the host and owned by the
other cell. Two rules remove the cause.

A typed cell of yours that receives a callback gives it to the host through
`InvokeInputCallback`, as a Goo.Widgets control does. Then the host needs no `Rebuild()`.
`InvokeInputCallback` is a protected member, and F# does not permit a call to a protected
member from a lambda of an object expression (FS0491). Thus the abstract typed cell type
declares one forwarding member per callback shape:

```fsharp
[<AbstractClass>]
type PivotCell() =
    inherit Cell<PivotInput>()

    // Gives a callback of the host to the host cell, as a Goo.Widgets control does.
    member this.Raise(callback: Action<string>, value: string) =
        this.InvokeInputCallback(callback, value)

let create (onSelect: string -> unit) : PivotCell =
    { new PivotCell() with
        override this.Build(input: PivotInput) : Blob =
          tabButton(input, fun () -> this.Raise(onSelect, input.Id)) }
```

A callback that you write in a render function keeps the other cell as owner for the life of
that cell. When it changes the state of your cell, rebuild your cell through the self slot
(next section).

## The cell inside its own callbacks

`this` exists only in a member of the object expression. Three needs reach the cell from
outside `Build`; each has a form that keeps the object expression.

**A plain function needs the cell.** Give the function the cell as a parameter. The function
stays plain, and the object expression stays non-recursive:

```fsharp
let reload (cell: LandingCell, state: LandingState, items: Project array) =
    loaded (state, items)
    cell.Rebuild()
// Inside the object expression. `this` is the self identifier of Build.
override this.Build() : Blob =
    let onEvent = function
        | Refresh -> reload (this, state, env.Projects.List())
        | NewProject -> Navigation.go(env.Navigator, "/projects/new")

    page(palette, state, onEvent)
```

**A callback made before the cell exists needs the cell.** The callbacks of a mounted widget
input are made before the object expression exists. Keep a slot for the cell, and fill the
slot after the object expression. `Build` runs after `create` returns, thus the slot is set
in time:

```fsharp
let create (state: ZoomState) : Cell =
    let mutable self: Cell voption = ValueNone

    // Created once, before the object expression exists.
    let onEvent (event: ZoomEvent) =
        applyEvent (state, event)
        self |> ValueOption.iter(fun cell -> cell.Rebuild())

    let cell =
        { new Cell() with
            override _.Build() : Blob = zoomPage (state, onEvent) }

    self <- ValueSome cell
    cell
```

Do not bind an object expression with `let rec`: it compiles with warning FS0040. When the
cell has a start member that the composition root calls, install the rebuild function there
instead, as the `Landing` view of the Migrondi UI does.

**An animation value needs the cell.** `AnimateValue` is a member of the cell. Goo ties the
animation to that cell. Create the value after the object expression exists:

```fsharp
let create () : Cell =
    let mutable opacity = Unchecked.defaultof<Anim<float>>

    let cell =
        { new Cell() with
            override _.Build() : Blob =
                Container(OpacityAnim = opacity).Children(Text(Content = "Fade")) }

    // The cell exists now: Goo ties the animation to it.
    opacity <- cell.AnimateValue(1.0)
    cell
```

## Data in records, behavior in parameters

The central rule. Props, input, and store records carry data only — never functions.
A function field poisons record equality: two closures are never equal, so a typed-input
Cell with a callback field rebuilds on every parent build.

| Placement           | Shape                                                               | Why                                                                          |
| ------------------- | ------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Inline build helper | `let inline clickable (id, child, [<InlineIfLambda>] onClick)`      | Erases into the caller. No closure, no delegate.                             |
| View function       | `let commitCard (p: CardProps, onEvent: CardEvent -> unit) : Blob`  | Tupled, two parameters. The callback value comes from the closure of `create`. |
| `create` parameter  | `let create (onEvent: CardEvent -> unit) : Cell = ...`              | One value for the life of the cell: stable delegate identity for free.       |
| Widget input struct | `MediaTransportInput(OnPlayPause = onPlayPause)`                    | G#'s design. Keep the function outside `Build` (see `goo-widgets-fsharp`).    |

Events travel as one DU over one callback. One channel beats a delegate zoo: extensible,
no arity churn, pattern-matched handlers.

```fsharp
type CardEvent = Opened of Commit | ReplyToggled of Commit | MenuOpened of Commit

[<Struct; NoComparison>]
type CardProps = { Commit: Commit; ViewerId: string; Busy: bool }
```

- Arguments are tupled by default: `let f (a: A, b: B) = ...`. Curry only when a call site
  partially applies the function (`lines |> List.map (pad 5)`). Three parameters or fewer,
  `env` counted. Past three, move all data parameters into one record that carries the name
  of the operation (`ship` takes `ShipArgs`); `env` stays outside the record, first.
- Callbacks are F# functions and parameters, not record fields and not `Action` wrappers.
  An `inline` helper takes each callback as one `[<InlineIfLambda>]` parameter, so the
  compiler splices the body of the caller into it.
- Do not fix a data-only record by adding a callback field, even when a style guide defaults
  callbacks into record fields.
- Small short-lived props go `[<Struct; NoComparison>]`. Keep generated equality on mounted
  input records; drop it (`NoEquality`) only on records that are never compared.

## State ladder

| Tier | State kind                        | Lives in                                  | Refresh                                |
| ---- | --------------------------------- | ----------------------------------------- | -------------------------------------- |
| 1    | Ephemeral UI (open, hover, draft) | `let mutable` in the closure of `create`  | Owner rule. Write nothing.             |
| 2    | Page state, one owner             | A state record in the closure of `create` | Owner rule.                            |
| 3    | Shared domain state               | Store at the composition root             | Write the slice, rebuild reader cells. |
| 4    | Configuration                     | Validated settings capability in `env`    | Read-only.                             |

- Local mutable state is fine: a cell holds state by design, and an object expression holds
  it in the closure. State never lives in the Blob tree, which is discarded after each diff.
- A handler may write any store slice. It must rebuild every _other_ cell that reads the
  changed slice. The owner rule covers its own cell when the callback belongs to it (see
  "Who rebuilds after a callback").
- Cross-cell refresh flows through callback parameters injected at the composition root:
  `let bottomBar = BottomBar.create (env, player, fun () -> playlist.Rebuild())`. Cells
  never hold sibling references. Rebuild the smallest subtree: one mounted Cell per refresh
  region.

## Stores

One immutable value per concern, in one mutable field per concern. Data only:
`type Store = { mutable Playlist: Playlist; mutable Session: Session }`, with
`type Playlist = { Songs: Song array; Selected: SongId voption }`.

- Write by replacing, never by mutating inside the value:
  `store.Playlist <- { store.Playlist with Songs = loaded }`. The store knows no Cell, no
  Window, and no rebuild. Cells read it inside `Build`.
- All background wiring lives in the composition root subscription block, in one order:
  marshal, write, rebuild: `env.PostUI.Post(fun () -> store.Playlist <- { store.Playlist
  with Songs = songs }; mediaList.Rebuild())`.

## Services: env capabilities plus PostUI

Dependencies travel as capabilities, so that a function names what it uses and a test builds
only that:

- One **worker interface** holds the operations. One **accessor interface** named
  `IProvideX` exposes the worker as one property. Each function takes `env` first. A
  function that uses one capability annotates it
  (`env: #IProvideSearch`). A function that uses several leaves `env` unannotated, and
  inference collects the union of the capabilities that its callees name. A test env is an
  object expression with only the capabilities that the call tree names. Validated settings
  are one more capability.
- The production env is one object expression over one **bundle interface**. An object
  expression has the type of its first interface, thus a value that implements several
  capabilities one by one fails a constraint on two of them.

```fsharp
type ISearch =
    abstract Find: query: string -> SearchResult array
type IProvideSearch =
    abstract Search: ISearch
type IPostUI =
    abstract Post: (unit -> unit) -> unit
type IProvidePostUI =
    abstract PostUI: IPostUI
type IAppEnv =
    inherit IProvideSearch
    inherit IProvidePostUI
```

One Goo addition. The Window is not a service; the marshaller is. It is an interface and a
`create` function, like each other service. The composition root attaches the window after
the window exists, thus the marshaller has one member more than the capability:

```fsharp
type IUiMarshaller =
    inherit IPostUI
    abstract Attach: w: Window -> unit

module UiMarshaller =
    let create () : IUiMarshaller =
        let mutable window: Window voption = ValueNone

        let post(action: unit -> unit) =
            window
            |> ValueOption.iter(fun window ->
                window.Post(fun () -> action()) |> ignore)

        { new IUiMarshaller with
            member _.Attach(w: Window) = window <- ValueSome w
            member _.Post(action: unit -> unit) = post action }
```

- Every service event and every background task returns through `env.PostUI.Post`. The
  mutable window slot is the app's single seam, and it lives inside the marshaller and
  nowhere else. No capability exposes a `Window`, a `Cell`, or a `Blob`.
- A factory returns one interface. A tuple of `worker * looseFunction` leaks half the
  service's surface out of its contract: fold the function in as a member.
- A service that raises events on foreign threads takes the marshaller at creation and
  delivers its handlers on the UI thread. `Subscribe(handlers)` is a member of the worker
  interface; the handlers record is an argument carrier wired once at the composition root.

## Composition root

Every app reads the same, in this order:

1. `Window.ConfigureApplication(name, version, identifier)`.
2. Build the services and the marshaller (`UiMarshaller.create ()`).
3. Parse settings; stop the app on error. Build `env` (capabilities, including `IPostUI`).
4. Build the stores.
5. Create the cells: `create (env, store, callbacks)` — `env` first.
6. Build the root view Cell; mount children with stable keys.
7. Create `Window(Title = ..., Root = root)`; `marshaller.Attach(window)`.
8. Subscribe to service events: handler → `PostUI.Post` → store write → target `Rebuild()`.
9. `window.Run()`.

Step 7 resolves the one circularity: `Root` is init-only, so the window is built last and
attached to the marshaller the cells already hold.

## Mounting and keys

- Mount a child Cell by key and factory: `Cell.Mount<Cell>("playlist", playlistFactory)`.
- Identity is the declared type plus the sibling key. A changed key unmounts and remounts.
- Dynamic lists need stable keys: `Key = $"song-{song.Id}"`. Index keys break on reorder.
- Create the factory once, beside the cell it builds: `let playlistFactory () = playlist` at
  the composition root, then `Cell.Mount<Cell>("playlist", playlistFactory)` inside the
  parent `Build`. A factory written inside the parent `Build` allocates a fresh closure
  every rebuild.

## The interop boundary

F# friction families and their FunGoo answers:

| Friction                                                             | Rule                                                                                                                                                                                 |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Child collections                                                    | Use the `.Children(...)` ParamArray extension (`FunGoo.Children`); it erases to `Add` calls. Never hand-convert an F# list to `IList<Blob>`.                                         |
| Callbacks and factories                                              | Write the F# function. F# converts a function to `Action` or `Func` where a Goo API takes a delegate. Wrap the function only when the compiler reports an error: a lambda with a parameter that has no type, or a conditional with `null`. |
| `op_Implicit` (int → `Length`, `Length` → `Edges`, string → `Color`) | F# applies no implicit conversions. Use named factories `Length.Pixels`, `Length.Percent`, `Color.Rgb`, or `FunGoo` converters `lengthFromFloat`, `edgesFromFloat`, `percent 100.0`. |
| Nullable value fields and arguments                                  | Pass by name: `Padding = Nullable 20.`. Nullable struct fields take generated optional setters that accept `'T voption`.                                                             |
| `Try*` with `out`                                                    | Returns a tuple: `match handle.TryGetTextPositionAt(p, space) with true, pos -> ...`. FunGoo.Interop wraps these as `voption`.                                                       |
| Window events                                                        | Explicit accessors: `window.add_FocusChanged handler`, `window.remove_FocusChanged handler`.                                                                                         |
| Mutable class setters                                                | Fluent `FunGoo.Setters` extensions.                                                                                                                                                  |

Pass-through, no wrapper needed: constructors with named properties, CLR enums, `Color`
factories, `Cell`, `Window.Run`. In the FunGoo repo, extend the generator `codegen/Generate.fsx`,
never the generated files.

## Domain modeling and performance

- Domain types import nothing from Goo. Model routes and transitions as pure functions:
  a `Route` DU plus a total `guard` function, unit-tested with no window. Props records
  translate domain to view: keep them small, `[<Struct>]` when they are short-lived carriers.
- `voption` on the F# side. `Nullable` only at the Goo boundary.
- Bulk data: arrays with stable string keys. No `seq` materialized per build.
- Per-row builders: one pass, `inline` + `[<InlineIfLambda>]`, cached delegates, no
  formatting pipeline when the domain already holds the string.
- Budget is the measurement: `GC.GetAllocatedBytesForCurrentThread()` around one `Build`,
  asserted under a byte ceiling. Goo.Widgets ships such contract tests; hold app cells to
  the same discipline.
- `Build()` runs without a window for plain Blobs, so views test headless. A downcast
  (`:?>`) to read children of a returned Blob belongs in tests only, and a failed check
  fails with a message, not with a printed flag. A Goo.Widgets element that reads the theme
  (`VirtualItems`) needs the build context of a mounted cell: mount the cell in a headless
  `EmbeddedWindowHost` for that check.
- A check reads the state of a cell through the members of its abstract cell type, or
  through a state record that the check holds. Do not read the retained tree through
  reflection.

## Checklist for review

List each of these in the diff:

- A named class that holds logic or state (`let`, `as this`, a constructor with
  parameters), or an abstract cell type with no member that outside code drives and no
  forwarding member. A record and an interface are correct forms.
- A callback that changes the state of a cell and does not rebuild it: a callback of a
  render function that you pass to a library cell, or a delegate on a Blob of a typed cell
  of yours.
- A typed cell that calls a callback of its host without `InvokeInputCallback`.
- A `Rebuild()` in a handler of a widget input callback. The owner rule already does it.
- A function field in a props, input, or store record.
- A curried function with no partial-application call site, or an `Action` or `Func`
  wrapper that the compiler does not require.
- A delegate created inside `Build` for a mounted widget input.
- I/O inside `Build`, or a tree read, write, or `Rebuild()` on a background thread without
  `Post`.
- Keyed and unkeyed children mixed under one parent, or an F# list hand-converted for
  `Children`.
- A formatting pipeline per row in a builder.
- A cell holding a sibling cell instead of an injected callback.

Each item: fix it, or state why it stays.

## Verify

In the target project:

1. `dotnet build` — the shapes above must compile as written.
2. Call each `Build()` headless and assert on the returned Blob tree, the way
   `Goo/samples/interop/FSharp` does under `--check`.
3. Measure one `Build()` per cell with `GC.GetAllocatedBytesForCurrentThread()` in a Release
   build, and record the number next to the cell.
4. Check the form: `grep -rn "as this" <src> <samples>` returns no hit, and each
   `inherit Cell` is an abstract cell type: abstract members and forwarding members only.
