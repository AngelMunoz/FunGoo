namespace FunGoo.Metro

open Goo

/// Event raised by a <see cref="MetroTeachingTip"/> callout.
[<Struct; NoComparison>]
type MetroTeachingTipEvent =
  | Closed
  | ActionInvoked

/// Data-only properties of a <see cref="MetroTeachingTip"/> callout.
[<Struct; NoComparison>]
type MetroTeachingTipProps = {
  /// Whether the teaching tip is currently open.
  Open: bool
  /// Optional target <see cref="ElementHandle"/> to anchor to. When
  /// <c>ValueNone</c>, the tip presents centered near the top of the window.
  Anchor: ElementHandle voption
  /// Preferred placement relative to <c>Anchor</c>.
  Placement: MetroFlyoutPlacement
  /// Heading text displayed at the top of the callout.
  Title: string
  /// Optional secondary heading below <c>Title</c>.
  Subtitle: string voption
  /// Optional plain-text body guidance.
  Body: string voption
  /// Optional primary action button label.
  ActionText: string voption
  /// When <c>true</c>, displays a close button in the header.
  Closable: bool
  /// When <c>true</c>, Escape and outside pointer clicks dismiss the callout.
  LightDismiss: bool
  /// Minimum callout width in logical pixels (defaults to <c>220.0</c>).
  MinWidth: float
  /// Maximum callout width in logical pixels (defaults to <c>320.0</c>).
  MaxWidth: float
  /// Optional accessible label override.
  AccessibilityName: string voption
}

/// Retained Metro teaching-tip cell with a 3 px top accent border, 250 ms
/// slide + fade entrance animation, Escape/light-dismiss handling, and optional
/// rich body content.
[<AbstractClass>]
type MetroTeachingTipCell =
  inherit Cell<MetroTeachingTipProps>

  internal new: unit -> MetroTeachingTipCell

  /// <c>true</c> while the callout is open or animating closed.
  abstract IsPresented: bool

  /// Entrance/exit translation animation of the callout panel.
  abstract PanelTranslate: Anim<Point>

  /// Entrance/exit opacity animation of the callout panel.
  abstract PanelOpacity: Anim<float>

  /// Current view, for headless inspection.
  abstract View: Blob

  /// Requests dismissal through the host callback.
  abstract Close: unit -> unit

  /// Invokes the primary action through the host callback.
  abstract InvokeAction: unit -> unit

/// Sharp Metro teaching-tip callout builder and animated retained cell.
[<RequireQualifiedAccess>]
module MetroTeachingTip =

  /// Returns default non-targeted teaching-tip properties.
  val defaults: title: string * isOpen: bool -> MetroTeachingTipProps

  /// Returns teaching-tip properties anchored to <paramref name="anchor"/>.
  val anchored:
    anchor: ElementHandle * title: string * isOpen: bool ->
      MetroTeachingTipProps

  /// Builds the sharp 3 px top-accented teaching-tip panel surface inside a
  /// <c>Cell.Build</c> scope without wrapping it in a <see cref="Portal"/>.
  val buildSurface:
    props: MetroTeachingTipProps * onEvent: (MetroTeachingTipEvent -> unit) ->
      Blob

  /// Builds a stateless <see cref="Portal"/> teaching tip inside a
  /// <c>Cell.Build</c> scope.
  val build:
    props: MetroTeachingTipProps * onEvent: (MetroTeachingTipEvent -> unit) ->
      Blob

  /// Creates an animated <see cref="MetroTeachingTipCell"/> using the plain
  /// text fields in <see cref="MetroTeachingTipProps"/>.
  val create: onEvent: (MetroTeachingTipEvent -> unit) -> MetroTeachingTipCell

  /// Creates an animated <see cref="MetroTeachingTipCell"/> with an additional
  /// custom <paramref name="renderContent"/> body builder.
  val createWithContent:
    onEvent: (MetroTeachingTipEvent -> unit) * renderContent: (unit -> Blob) ->
      MetroTeachingTipCell

  /// Creates a reusable mount factory for <see cref="MetroTeachingTipCell"/>.
  val createFactory:
    onEvent: (MetroTeachingTipEvent -> unit) -> (unit -> MetroTeachingTipCell)

  /// Mounts a <see cref="MetroTeachingTipCell"/> under <paramref name="key"/>.
  val mount:
    key: string *
    props: MetroTeachingTipProps *
    factory: (unit -> MetroTeachingTipCell) ->
      Blob
