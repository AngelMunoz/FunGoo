module FunGoo.Metro.Tests.TestSupport

open Goo

/// State of a capture cell. One mutable field per concern.
type CaptureState = {
  /// The tree of the last build. Null before the first build.
  mutable Captured: Blob
  /// The number of builds.
  mutable Builds: int
  /// The action that the next build runs first, when there is one.
  mutable DuringBuild: (unit -> unit) voption
}

/// The request of <c>CaptureCell.DuringBuild</c>.
type DuringBuildRequest = {
  Host: EmbeddedWindowHost
  Action: unit -> unit
}

/// Root cell for headless tests. <c>Build</c> runs a function inside the
/// real window build pass and keeps the returned tree in <c>Captured</c>.
/// The type holds the members that the tests read.
/// <c>CaptureCell.create</c> makes the cell.
[<AbstractClass>]
type CaptureCell() =
  inherit Cell()

  /// The tree of the last build. Null before the first build.
  abstract Captured: Blob

  /// The number of builds.
  abstract Builds: int

  /// Paint-bound translation owned by this cell, for view builders that take
  /// an <c>Anim</c>. Goo requires a paint animation to belong to the window of
  /// the element, so the owner cell must be mounted in the same window.
  abstract Track: Anim<Point>

  /// Paint-bound scalar animation at rest value 1.0, for a scale slot.
  abstract Scale: Anim<float>

  /// Paint-bound scalar animation at rest value 1.0, for an opacity slot.
  abstract Alpha: Anim<float>

  /// Runs <c>action</c> inside the next Build of this cell, where Metro theme
  /// reads and cell view builders are valid, then renders one frame.
  abstract DuringBuild:
    host: EmbeddedWindowHost * action: (unit -> unit) -> unit

[<RequireQualifiedAccess>]
module CaptureCell =

  /// A new state for one cell. Each cell has its own record.
  let initial() : CaptureState = {
    Captured = Unchecked.defaultof<Blob>
    Builds = 0
    DuringBuild = ValueNone
  }

  /// Builds the tree. The action of a pending <c>DuringBuild</c> runs first.
  let buildTree(state: CaptureState, buildFn: unit -> Blob) : Blob =
    state.Builds <- state.Builds + 1

    state.DuringBuild
    |> ValueOption.iter(fun action ->
      state.DuringBuild <- ValueNone
      action())

    let tree = buildFn()
    state.Captured <- tree
    tree

  /// Requests one build that runs the action of the request first.
  let requestDuringBuild
    (cell: CaptureCell, state: CaptureState, request: DuringBuildRequest)
    : unit =
    state.DuringBuild <- ValueSome request.Action
    cell.Rebuild()
    request.Host.RenderFrame(0.016)

  let create(buildFn: unit -> Blob) : CaptureCell =
    let state = initial()

    // Slots for the three animation values. The cell exists after the object
    // expression, and Goo ties each animation to the cell.
    let mutable track = Unchecked.defaultof<Anim<Point>>
    let mutable scale = Unchecked.defaultof<Anim<float>>
    let mutable alpha = Unchecked.defaultof<Anim<float>>

    let cell = {
      new CaptureCell() with
        override _.Captured = state.Captured
        override _.Builds = state.Builds
        override _.Track = track
        override _.Scale = scale
        override _.Alpha = alpha

        override this.DuringBuild(host, action) =
          requestDuringBuild(this, state, { Host = host; Action = action })

        override _.Build() : Blob = buildTree(state, buildFn)
    }

    track <- cell.AnimateValue(Point(X = 0.0, Y = 0.0))
    scale <- cell.AnimateValue(1.0)
    alpha <- cell.AnimateValue(1.0)
    cell

/// Number of children of a Container or a Button. Other Blob types return 0.
let childCount(blob: Blob) : int =
  match blob with
  | :? Container as container -> container.Children.Count
  | :? Button as button -> button.Children.Count
  | _ -> 0

/// Child at <c>index</c> of a Container or a Button.
let childAt(blob: Blob, index: int) : Blob =
  match blob with
  | :? Container as container -> container.Children[index]
  | :? Button as button -> button.Children[index]
  | other ->
    failwith
      $"Blob of type {other.GetType().Name} has no child at index {index}."

/// Depth-first search of the Container and Button children for a keyed element.
let rec findBlobByKey(root: Blob, key: string) : Blob voption =
  if not(isNull root) && root.Key = key then
    ValueSome root
  else
    let count = childCount root
    let mutable found = ValueNone
    let mutable index = 0

    while index < count && found.IsNone do
      found <- findBlobByKey(childAt(root, index), key)
      index <- index + 1

    found

/// Returns the keyed element, or fails with a message.
let requireBlobByKey(root: Blob, key: string) : Blob =
  match findBlobByKey(root, key) with
  | ValueSome blob -> blob
  | ValueNone -> failwith $"Expected an element with key '{key}'."

/// Returns the binding for <c>key</c> with no modifier.
let findKeyBinding(bindings: KeyBinding array, key: Key) : KeyBinding =
  bindings
  |> Array.find(fun binding ->
    binding.Key = key && binding.Modifiers = KeyModifiers.None)

/// Compares two lists with F# structural equality. xunit <c>Assert.Equal</c>
/// has ambiguous overloads for lists.
let assertListEqual(expected: 'T list, actual: 'T list) : unit =
  if expected <> actual then
    failwith $"Expected {expected}, got {actual}."

/// Dark Metro configuration for test windows.
let darkConfig: FunGoo.Metro.MetroConfig = {
  FunGoo.Metro.MetroConfig.defaultConfig with
      Mode = FunGoo.Metro.MetroMode.Dark
      Accent = FunGoo.Metro.MetroAccentKind.Blue
}
