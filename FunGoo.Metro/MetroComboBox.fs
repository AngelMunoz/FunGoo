namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Inputs

[<Struct; NoComparison>]
type MetroComboOption = {
  Id: string
  Label: string
  Disabled: bool
}

[<Struct; NoComparison>]
type MetroComboBoxProps = {
  Options: MetroComboOption array
  SelectedId: string
  Searchable: bool
  Query: string
  Placeholder: string voption
  SearchPlaceholder: string voption
  EmptyText: string voption
  Disabled: bool
  Width: Length voption
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroComboBoxEvent =
  | Selected of id: string
  | QueryChanged of query: string
  | OpenChanged of isOpen: bool

/// Factory for Metro combo boxes and auto-suggest boxes. The host owns the
/// selection and the query: update the props after each event and rebuild the
/// owner cell. Create one factory for each control, outside <c>Build</c>. The
/// events do not name the control, so two controls that share one factory send
/// their events to one handler. One factory gives every input the same
/// callback instances.
type IMetroComboBoxFactory =

  /// Returns the widget input for <paramref name="props"/>, with the callbacks
  /// of this factory. While <c>Options</c> is the same array instance, the
  /// mapped options are also the same instance.
  abstract CreateInput: props: MetroComboBoxProps -> ComboBoxInput

  /// Mounts the combo box under the stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroComboBoxProps -> Blob

[<RequireQualifiedAccess>]
module MetroComboBox =

  let baseProps(options: MetroComboOption array) : MetroComboBoxProps = {
    Options = options
    SelectedId = ""
    Searchable = false
    Query = ""
    Placeholder = ValueNone
    SearchPlaceholder = ValueNone
    EmptyText = ValueNone
    Disabled = false
    Width = ValueNone
    AccessibilityName = ValueNone
  }

  let defaults
    (options: MetroComboOption array, selectedId: string)
    : MetroComboBoxProps = {
    baseProps options with
        SelectedId = selectedId
  }

  let autoSuggest
    (options: MetroComboOption array, query: string)
    : MetroComboBoxProps = {
    baseProps options with
        Searchable = true
        Query = (if isNull query then "" else query)
  }

  let optionsOf(props: MetroComboBoxProps) : ComboBoxOption array =
    if isNull props.Options then
      Array.empty
    else
      props.Options
      |> Array.map(fun option ->
        ComboBoxOption(
          Id = option.Id,
          Label =
            (if String.IsNullOrEmpty option.Label then
               null
             else
               option.Label),
          Disabled = option.Disabled
        ))

  let dataOf
    (props: MetroComboBoxProps, items: ComboBoxOption array)
    : ComboBoxInput =
    ComboBoxInput(
      Items = items,
      SelectedId =
        (if String.IsNullOrEmpty props.SelectedId then
           null
         else
           props.SelectedId),
      Searchable = props.Searchable,
      // A combo box without the search entry leaves the query to the widget.
      Query = (if props.Searchable then props.Query else null),
      Placeholder = ValueOption.defaultValue null props.Placeholder,
      SearchPlaceholder = ValueOption.defaultValue null props.SearchPlaceholder,
      EmptyText = ValueOption.defaultValue null props.EmptyText,
      Disabled = props.Disabled,
      Width =
        (match props.Width with
         | ValueSome width -> Nullable width
         | ValueNone -> Nullable()),
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName
    )

  let toData(props: MetroComboBoxProps) : ComboBoxInput =
    dataOf(props, optionsOf props)

  /// The delegates of a combo box factory. `createFactory` makes them once:
  /// every input carries the same callback instances, so an unchanged input
  /// does not rebuild the control.
  type ComboHandlers = {
    OnSelect: Action<string>
    OnQueryChange: Action<string>
    OnOpenChange: Action<bool>
  }

  /// The values that the functions of a combo box factory work on. The mapped
  /// options stay the same instance while the option array does.
  type FactoryState = {
    Handlers: ComboHandlers
    mutable Mapped: (MetroComboOption array * ComboBoxOption array) voption
  }

  // The compiler requires the constructors here: a lambda does not convert to
  // a delegate in a record field (FS0002).
  let handlersOf(onEvent: MetroComboBoxEvent -> unit) : ComboHandlers = {
    OnSelect = Action<string>(fun id -> onEvent(MetroComboBoxEvent.Selected id))

    OnQueryChange =
      Action<string>(fun query ->
        onEvent(MetroComboBoxEvent.QueryChanged query))

    OnOpenChange =
      Action<bool>(fun isOpen -> onEvent(MetroComboBoxEvent.OpenChanged isOpen))
  }

  // A factory maps an option array once. The mapped options stay the same
  // instance while the option array does.
  let itemsFor
    (state: FactoryState, props: MetroComboBoxProps)
    : ComboBoxOption array =
    let options = props.Options

    match state.Mapped with
    | ValueSome(cachedOptions, cachedItems) when
      obj.ReferenceEquals(options, cachedOptions)
      ->
      cachedItems
    | _ ->
      let items = optionsOf props
      state.Mapped <- ValueSome(options, items)
      items

  let createInput
    (state: FactoryState, props: MetroComboBoxProps)
    : ComboBoxInput =
    let data = dataOf(props, itemsFor(state, props))

    ComboBoxInput(
      Items = data.Items,
      SelectedId = data.SelectedId,
      OnSelect = state.Handlers.OnSelect,
      Searchable = data.Searchable,
      Query = data.Query,
      OnQueryChange =
        (if data.Searchable then
           state.Handlers.OnQueryChange
         else
           null),
      Placeholder = data.Placeholder,
      SearchPlaceholder = data.SearchPlaceholder,
      EmptyText = data.EmptyText,
      Disabled = data.Disabled,
      OnOpenChange = state.Handlers.OnOpenChange,
      Width = data.Width,
      AccessibilityName = data.AccessibilityName
    )

  let mount
    (state: FactoryState, key: string, props: MetroComboBoxProps)
    : Blob =
    Cell.Mount<ComboBoxInput, ComboBox>(key, createInput(state, props))

  let createFactory
    (onEvent: MetroComboBoxEvent -> unit)
    : IMetroComboBoxFactory =
    let state: FactoryState = {
      Handlers = handlersOf onEvent
      Mapped = ValueNone
    }

    {
      new IMetroComboBoxFactory with
        member _.CreateInput(props) = createInput(state, props)
        member _.Mount(key, props) = mount(state, key, props)
    }
