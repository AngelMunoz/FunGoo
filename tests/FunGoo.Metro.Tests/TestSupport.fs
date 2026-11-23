module FunGoo.Metro.Tests.TestSupport

open Goo

/// Root cell for headless tests. <c>Build</c> runs <c>buildFn</c> inside the
/// real window build pass and keeps the returned tree in <c>Captured</c>.
type CaptureCell(buildFn: unit -> Blob) as this =
  inherit Cell()

  let track = this.AnimateValue(Point(X = 0.0, Y = 0.0))
  let mutable captured: Blob = null
  let mutable duringBuild: (unit -> unit) voption = ValueNone

  member _.Captured = captured

  /// Paint-bound translation owned by this cell, for view builders that take
  /// an <c>Anim</c>. Goo requires a paint animation to belong to the window of
  /// the element, so the owner cell must be mounted in the same window.
  member _.Track = track

  /// Runs <c>action</c> inside the next Build of this cell, where Metro theme
  /// reads and cell view builders are valid, then renders one frame.
  member this.DuringBuild(host: EmbeddedWindowHost, action: unit -> unit) =
    duringBuild <- ValueSome action
    this.Rebuild()
    host.RenderFrame(0.016)

  override _.Build() : Blob =
    duringBuild
    |> ValueOption.iter(fun action ->
      duringBuild <- ValueNone
      action())

    let tree = buildFn()
    captured <- tree
    tree

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
