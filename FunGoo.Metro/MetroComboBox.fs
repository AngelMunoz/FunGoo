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

module private ComboInternals =

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

[<RequireQualifiedAccess>]
module MetroComboBox =

  let private baseProps(options: MetroComboOption array) : MetroComboBoxProps = {
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

  let toData(props: MetroComboBoxProps) : ComboBoxInput =
    ComboBoxInput(
      Items = ComboInternals.optionsOf props,
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

[<Sealed>]
type MetroComboBoxFactory(onEvent: MetroComboBoxEvent -> unit) =

  // One instance each: every input of this factory carries the same callbacks.
  // The compiler requires the constructors here (FS0002 for a typed lambda).
  let onSelect =
    Action<string>(fun id -> onEvent(MetroComboBoxEvent.Selected id))

  let onQueryChange: Action<string> =
    Action<string>(fun query -> onEvent(MetroComboBoxEvent.QueryChanged query))

  let onOpenChange: Action<bool> =
    Action<bool>(fun isOpen -> onEvent(MetroComboBoxEvent.OpenChanged isOpen))

  // The mapped options stay the same instance while the option array does.
  let mutable cachedOptions: MetroComboOption array = null
  let mutable cachedItems: ComboBoxOption array = null

  let itemsFor
    (options: MetroComboOption array, mapped: ComboBoxOption array)
    : ComboBoxOption array =
    if
      not(isNull cachedItems) && obj.ReferenceEquals(options, cachedOptions)
    then
      cachedItems
    else
      cachedOptions <- options
      cachedItems <- mapped
      mapped

  member _.CreateInput(props: MetroComboBoxProps) : ComboBoxInput =
    let data = MetroComboBox.toData props

    ComboBoxInput(
      Items = itemsFor(props.Options, data.Items),
      SelectedId = data.SelectedId,
      OnSelect = onSelect,
      Searchable = data.Searchable,
      Query = data.Query,
      OnQueryChange = (if data.Searchable then onQueryChange else null),
      Placeholder = data.Placeholder,
      SearchPlaceholder = data.SearchPlaceholder,
      EmptyText = data.EmptyText,
      Disabled = data.Disabled,
      OnOpenChange = onOpenChange,
      Width = data.Width,
      AccessibilityName = data.AccessibilityName
    )

  member this.Mount(key: string, props: MetroComboBoxProps) : Blob =
    Cell.Mount<ComboBoxInput, ComboBox>(key, this.CreateInput props)
