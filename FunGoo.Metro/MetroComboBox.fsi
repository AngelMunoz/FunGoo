namespace FunGoo.Metro

open Goo
open Goo.Widgets.Inputs

/// One option of a Metro combo box.
[<Struct; NoComparison>]
type MetroComboOption = {
  /// Stable unique identifier of the option.
  Id: string
  /// Visible label of the option. An empty label shows the identifier.
  Label: string
  /// Rejects interaction for this option and dims its presentation.
  Disabled: bool
}

/// Data-only properties for a Metro combo box and for a Metro auto-suggest box.
[<Struct; NoComparison>]
type MetroComboBoxProps = {
  /// Options in display order.
  Options: MetroComboOption array
  /// Identifier of the selected option. An empty string clears the selection.
  SelectedId: string
  /// Shows a search entry in the popup, as an auto-suggest box does.
  Searchable: bool
  /// Controlled text of the search entry. The host updates it after each
  /// <see cref="MetroComboBoxEvent.QueryChanged"/> and supplies the matching
  /// options in <c>Options</c>.
  Query: string
  /// Optional placeholder of the trigger.
  Placeholder: string voption
  /// Optional hint of the search entry.
  SearchPlaceholder: string voption
  /// Optional message when no option matches.
  EmptyText: string voption
  /// Rejects interaction.
  Disabled: bool
  /// Width of the trigger. <c>ValueNone</c> uses the widget default.
  Width: Length voption
  /// Optional accessible name.
  AccessibilityName: string voption
}

/// Events raised by a Metro combo box.
[<Struct; NoComparison>]
type MetroComboBoxEvent =
  /// The user selected the option with this identifier.
  | Selected of id: string
  /// The user changed the text of the search entry.
  | QueryChanged of query: string
  /// The user requested the open state of the popup.
  | OpenChanged of isOpen: bool

/// Property mapping for the Metro combo box and the Metro auto-suggest box on
/// <see cref="Goo.Widgets.Inputs.ComboBox"/>, under the Metro widget theme.
[<RequireQualifiedAccess>]
module MetroComboBox =

  /// Creates <see cref="MetroComboBoxProps"/> for <paramref name="options"/> and
  /// <paramref name="selectedId"/> as a combo box: single selection and no
  /// search entry.
  val defaults:
    options: MetroComboOption array * selectedId: string -> MetroComboBoxProps

  /// Creates <see cref="MetroComboBoxProps"/> for <paramref name="options"/> and
  /// <paramref name="query"/> as an auto-suggest box: the search entry is shown
  /// and the host controls the query.
  val autoSuggest:
    options: MetroComboOption array * query: string -> MetroComboBoxProps

  /// Maps <paramref name="props"/> to the widget data, without callbacks.
  val toData: props: MetroComboBoxProps -> ComboBoxInput

/// Factory for Metro combo boxes and auto-suggest boxes. The host owns the
/// selection and the query: update the props after each event and rebuild the
/// owner cell. Create one factory for each control, outside <c>Build</c>. The
/// events do not name the control, so two controls that share one factory send
/// their events to one handler. One factory gives every input the same
/// callback instances.
[<Sealed>]
type MetroComboBoxFactory =

  /// Creates a factory that raises <paramref name="onEvent"/>.
  new: onEvent: (MetroComboBoxEvent -> unit) -> MetroComboBoxFactory

  /// Returns the widget input for <paramref name="props"/>, with the callbacks
  /// of this factory. While <c>Options</c> is the same array instance, the
  /// mapped options are also the same instance.
  member CreateInput: props: MetroComboBoxProps -> ComboBoxInput

  /// Mounts the combo box under the stable <paramref name="key"/>.
  member Mount: key: string * props: MetroComboBoxProps -> Blob
