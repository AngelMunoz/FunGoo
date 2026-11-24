namespace FunGoo.Metro

open System
open Goo

/// Command button descriptor for <see cref="MetroMessageDialogCell"/>.
[<Struct; NoComparison>]
type MetroDialogCommand = {
  /// Stable command identifier emitted when the user activates this button.
  /// Unique inside <c>Commands</c>: Goo throws when two sibling buttons share
  /// a key.
  Id: string
  /// Visible button caption.
  Label: string
  /// Styles the button as an accent primary command when <c>true</c>.
  IsPrimary: bool
}

/// Resolution emitted by <see cref="MetroMessageDialogCell"/>.
[<Struct; NoComparison>]
type MetroMessageDialogResult =
  /// One of the dialog's <see cref="MetroDialogCommand"/> buttons was invoked.
  | CommandInvoked of id: string
  /// Dismissed via Escape or backdrop click when <c>Dismissible</c> is
  /// <c>true</c>.
  | Dismissed

/// Data-only properties for a Metro modal message dialog.
[<Struct; NoComparison>]
type MetroMessageDialogProps = {
  /// Displays the message dialog when <c>true</c>.
  Open: bool
  /// Optional heading text rendered above the message body.
  Title: string voption
  /// Message body text rendered inside the modal band.
  Message: string
  /// Up to three action commands displayed at the end of the footer row
  /// (defaults to a single primary <c>"ok"</c> command when empty).
  Commands: MetroDialogCommand array
  /// Allows dismissing via Escape or backdrop click when <c>true</c>.
  Dismissible: bool
  /// Optional accessible name overriding <see cref="Title"/>.
  AccessibilityName: string voption
}

/// Render arguments for the <see cref="MetroMessageDialog"/> builders: the
/// props plus the optional paint-bound animations of an open dialog.
[<Struct; NoComparison>]
type DialogRenderArgs = {
  /// Properties of the dialog.
  Props: MetroMessageDialogProps
  /// Optional paint-bound scale animation of the dialog band.
  Scale: Anim<float> voption
  /// Optional paint-bound opacity animation of the backdrop scrim.
  Backdrop: Anim<float> voption
}

/// Stateful animated Metro message dialog cell presented via modal
/// <see cref="Portal"/> over 280 ms. <c>MetroMessageDialog.create</c> makes
/// the cell.
[<AbstractClass>]
type MetroMessageDialogCell =
  inherit Cell<MetroMessageDialogProps>

  /// <c>true</c> while the dialog portal is mounted (open or running its close
  /// animation).
  abstract IsVisible: bool

  /// <c>true</c> while the open or close transition is running.
  abstract IsAnimating: bool

  /// Paint-bound opacity animation of the backdrop scrim. A test reads it.
  abstract BackdropOpacity: Anim<float>

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

  /// Drives one dialog result through the same callback path as buttons,
  /// Escape, and backdrop clicks.
  abstract Handle: result: MetroMessageDialogResult -> unit

  /// Gives one dialog result to the host cell through
  /// <c>InvokeInputCallback</c>, so the owner rule rebuilds the host cell.
  member Raise:
    callback: Action<MetroMessageDialogResult> *
    result: MetroMessageDialogResult ->
      unit

/// Stateless builders and stateful animated cell constructors for Metro
/// message dialogs.
[<RequireQualifiedAccess>]
module MetroMessageDialog =

  /// Helper creating a primary accent <see cref="MetroDialogCommand"/>.
  val primaryCommand: id: string * label: string -> MetroDialogCommand

  /// Helper creating a standard secondary <see cref="MetroDialogCommand"/>.
  val command: id: string * label: string -> MetroDialogCommand

  /// Creates default <see cref="MetroMessageDialogProps"/> with a single
  /// <c>"OK"</c> primary command and dismissal enabled.
  val defaults:
    title: string voption * message: string * isOpen: bool ->
      MetroMessageDialogProps

  /// Builds the content of the dialog portal for an open dialog. Returns the
  /// dimmed backdrop and the message band with its commands as two separate
  /// blobs; the band is not inside the backdrop. The caller adds the backdrop
  /// first and the band second to its <see cref="Portal"/>. Call it inside a
  /// build where the Metro theme is readable. A test reads it.
  val buildContent:
    args: DialogRenderArgs * onResult: (MetroMessageDialogResult -> unit) ->
      struct (Blob * Blob)

  /// Builds a stateless Metro message dialog <see cref="Blob"/>.
  val build:
    props: MetroMessageDialogProps *
    onResult: (MetroMessageDialogResult -> unit) ->
      Blob

  /// Creates an animated <see cref="MetroMessageDialogCell"/> delivering
  /// results to <paramref name="onResult"/> through <c>InvokeInputCallback</c>.
  val create:
    onResult: (MetroMessageDialogResult -> unit) -> MetroMessageDialogCell

  /// Creates a reusable cell factory for <see cref="MetroMessageDialogCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onResult: (MetroMessageDialogResult -> unit) ->
    unit ->
      MetroMessageDialogCell

  /// Mounts a <see cref="MetroMessageDialogCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroMessageDialogProps *
    factory: (unit -> MetroMessageDialogCell) ->
      Blob
