namespace FunGoo.Metro

open Goo

/// Data-only input snapshot for <see cref="MetroTooltipCell"/>.
[<Struct; NoComparison>]
type MetroTooltipInput = {
  /// Target element wrapped by the tooltip trigger area.
  Target: Blob
  /// Informational text displayed inside the tooltip bubble.
  Text: string
  /// Preferred placement relative to the target
  /// (<see cref="MetroFlyoutPlacement.Top"/> by default).
  Placement: MetroFlyoutPlacement
  /// Optional controlled visibility override (<c>ValueNone</c> uses automatic
  /// hover and focus visibility). A controlled hidden state ends the current
  /// hover and focus. After a change back to <c>ValueNone</c>, the tooltip
  /// stays hidden until the pointer or the focus returns.
  Open: bool voption
  /// Hover delay in milliseconds before showing in uncontrolled mode when the
  /// host window is open (defaults to <c>400.0</c>).
  DelayMs: float
  /// Suppresses the tooltip when <c>true</c>. When the input is enabled
  /// again, the tooltip stays hidden. The pointer or the focus must return
  /// before it shows again.
  Disabled: bool
}

/// Stateful Metro tooltip cell presenting a sharp 0 px bordered popup near its
/// target via <see cref="Portal"/>. Guards window timers behind
/// <c>Window.IsOpen</c> so display-free <c>goo render</c> frames succeed.
/// <c>MetroTooltip.create</c> makes the cell.
[<AbstractClass>]
type MetroTooltipCell =
  inherit Cell<MetroTooltipInput>

  /// <c>true</c> when the tooltip bubble is currently visible.
  abstract IsVisible: bool

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

/// Builders and mounting helpers for <see cref="MetroTooltipCell"/>.
[<RequireQualifiedAccess>]
module MetroTooltip =

  /// Default hover delay (<c>400.0</c> ms).
  val defaultDelayMs: float

  /// Creates default <see cref="MetroTooltipInput"/> for
  /// <paramref name="target"/> and <paramref name="text"/> placed above the
  /// target.
  val defaults: target: Blob * text: string -> MetroTooltipInput

  /// Creates a new <see cref="MetroTooltipCell"/>.
  val create: unit -> MetroTooltipCell

  /// Mounts a <see cref="MetroTooltipCell"/> under stable <paramref name="key"/>
  /// for <paramref name="input"/>.
  val mountInput: key: string * input: MetroTooltipInput -> Blob

  /// Mounts a <see cref="MetroTooltipCell"/> under stable <paramref name="key"/>
  /// wrapping <paramref name="target"/> with <paramref name="text"/>.
  val mount: key: string * target: Blob * text: string -> Blob

  /// Mounts a <see cref="MetroTooltipCell"/> using an explicit cached
  /// <paramref name="factory"/> (for tests).
  val mountWith:
    key: string * input: MetroTooltipInput * factory: (unit -> MetroTooltipCell) ->
      Blob
