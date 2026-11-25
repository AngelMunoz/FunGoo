namespace FunGoo.Metro

open Goo

/// Horizontal alignment of a <see cref="MetroToastHostCell"/> overlay inside
/// the host window.
[<Struct>]
type MetroToastAlignment =
  | TopCenter
  | TopEnd

/// Data-only description of a single Metro toast notification.
[<Struct; NoComparison>]
type MetroToastItem = {
  /// Stable identifier of the toast.
  Id: string
  /// Optional bold heading displayed above the message.
  Title: string voption
  /// Notification message body.
  Message: string
  /// Severity controlling the 4 px top bar and icon tint.
  Severity: MetroInfoBarSeverity
  /// Optional action button label.
  ActionText: string voption
  /// Optional auto-dismiss duration in milliseconds. <c>ValueNone</c> or
  /// non-positive durations keep the toast persistent until dismissed.
  /// The host removes the item from its input when it receives
  /// <c>Dismissed</c>. A new item must not reuse the id of an old item.
  DurationMs: float voption
  /// When <c>true</c>, renders a manual close button.
  Closable: bool
}

/// Event emitted by a Metro toast card or toast host.
[<Struct; NoComparison>]
type MetroToastEvent =
  | Dismissed of id: string
  | ActionInvoked of id: string

/// Input of <see cref="MetroToastHostCell"/>.
[<Struct; NoComparison>]
type MetroToastHostInput = {
  /// Active toast stack rendered from top to bottom.
  Items: MetroToastItem array
  /// Horizontal stack alignment inside the window overlay.
  Alignment: MetroToastAlignment
  /// Overlay z-index (defaults to <c>40</c>).
  ZIndex: int
}

/// Retained Metro toast host cell presenting stacked toast notifications in a
/// window <see cref="Portal"/> with slide-down entrance animations and
/// <c>Window.IsOpen</c>-guarded auto-dismiss timers.
[<AbstractClass>]
type MetroToastHostCell =
  inherit Cell<MetroToastHostInput>

  internal new: unit -> MetroToastHostCell

  /// Number of toasts currently rendered by the host.
  abstract ActiveCount: int

  /// Number of active auto-dismiss timers currently scheduled.
  abstract ActiveTimerCount: int

  /// Entrance translation animation of the toast stack.
  abstract EntranceTranslate: Anim<Point>

  /// Entrance opacity animation of the toast stack.
  abstract EntranceOpacity: Anim<float>

  /// Current view, for headless inspection.
  abstract View: Blob

  /// Programmatically dismisses the toast with <paramref name="id"/> through
  /// the host callback.
  abstract Dismiss: id: string -> unit

  /// Programmatically invokes the action of toast <paramref name="id"/>
  /// through the host callback.
  abstract TriggerAction: id: string -> unit

/// Metro toast notification card builder and retained <see cref="Portal"/>
/// toast host.
[<RequireQualifiedAccess>]
module MetroToast =

  /// Default auto-dismiss duration in milliseconds (<c>4000.0</c>).
  val defaultDurationMs: float

  /// Creates an informational toast item with default auto-dismiss.
  val item: id: string * message: string -> MetroToastItem

  /// Creates an informational toast item with <paramref name="title"/> and
  /// <paramref name="message"/>.
  val titledItem: id: string * title: string * message: string -> MetroToastItem

  /// Returns default host input aligned to <see cref="MetroToastAlignment.TopEnd"/>.
  val hostDefaults: items: MetroToastItem array -> MetroToastHostInput

  /// Builds a single sharp Metro toast card inside a <c>Cell.Build</c> scope.
  val buildCard:
    toast: MetroToastItem * onEvent: (MetroToastEvent -> unit) -> Blob

  /// Creates a retained <see cref="MetroToastHostCell"/> that reports
  /// dismissals and actions to its host through <c>InvokeInputCallback</c>.
  val createHost: onEvent: (MetroToastEvent -> unit) -> MetroToastHostCell

  /// Creates a reusable mount factory for <see cref="MetroToastHostCell"/>.
  val createHostFactory:
    onEvent: (MetroToastEvent -> unit) -> (unit -> MetroToastHostCell)

  /// Mounts a <see cref="MetroToastHostCell"/> under <paramref name="key"/>.
  val mountHost:
    key: string *
    input: MetroToastHostInput *
    factory: (unit -> MetroToastHostCell) ->
      Blob
