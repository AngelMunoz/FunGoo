namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Icons
open Goo.Widgets.Layout
open Goo.Widgets.Navigation
open FunGoo.Children

[<Struct; NoComparison>]
type MetroMenuCommand = {
  Id: string
  Label: string
  IconName: string voption
  Shortcut: KeyBinding voption
  Children: MetroMenuEntry array
  Disabled: bool
}

and [<Struct; NoComparison>] MetroMenuEntry =
  | Command of item: MetroMenuCommand
  | Separator of id: string

[<Struct; NoComparison>]
type MetroMenuEvent =
  | ItemClicked of id: string
  | OpenChanged of isOpen: bool

[<Struct; NoComparison>]
type MetroMenuFlyoutProps = {
  Open: bool
  Anchor: ElementHandle
  Items: MetroMenuEntry array
  Placement: PopoverPlacement
  Width: Length voption
  AccessibilityName: string voption
}

type IMetroMenuFlyoutFactory =

  abstract CreateInput: props: MetroMenuFlyoutProps -> MenuInput

  abstract Mount: key: string * props: MetroMenuFlyoutProps -> Blob

[<Struct; NoComparison>]
type MetroContextMenuProps = {
  Open: bool
  Position: Point
  Items: MetroMenuEntry array
  Width: Length voption
  AccessibilityName: string voption
}

type IMetroContextMenuFactory =

  abstract CreateInput: props: MetroContextMenuProps -> MenuInput

  abstract Mount: key: string * props: MetroContextMenuProps -> Blob

module MenuInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)

  let rec mapEntry
    (icons: Dictionary<string, string>, entry: MetroMenuEntry)
    : MenuItem =
    match entry with
    | MetroMenuEntry.Separator id ->
      MenuItem(
        Id = (if String.IsNullOrEmpty id then "sep" else id),
        Separator = true
      )
    | MetroMenuEntry.Command cmd ->
      cmd.IconName
      |> ValueOption.iter(fun icon ->
        if
          not(String.IsNullOrEmpty icon) && not(String.IsNullOrEmpty cmd.Id)
        then
          icons[cmd.Id] <- icon)

      let children =
        if isNull cmd.Children || cmd.Children.Length = 0 then
          null
        else
          cmd.Children |> Array.map(fun c -> mapEntry(icons, c))

      MenuItem(
        Id = cmd.Id,
        Label = (if String.IsNullOrEmpty cmd.Label then null else cmd.Label),
        Disabled = cmd.Disabled,
        Shortcut =
          (cmd.Shortcut
           |> ValueOption.map Nullable
           |> ValueOption.defaultValue(Nullable())),
        Children = children
      )

  let mapEntries
    (entries: MetroMenuEntry array)
    : struct (MenuItem array * Dictionary<string, string>) =
    let icons = Dictionary<string, string>(StringComparer.Ordinal)

    let mapped =
      if isNull entries || entries.Length = 0 then
        Array.empty
      else
        entries |> Array.map(fun e -> mapEntry(icons, e))

    struct (mapped, icons)

  type MenuFactoryState = {
    mutable Icons: Dictionary<string, string>
    mutable Mapped: (MetroMenuEntry array * MenuItem array) voption
    mutable OnClick: Action<string>
    mutable OnOpenChange: Action<bool>
    mutable ConfigureItem: Action<MenuInput, MenuItem, Button>
    mutable ConfigurePanel: Action<MenuInput, Container>
    mutable ConfigureRoot: Action<MenuInput, Container>
  }

  let styleItem(state: MenuFactoryState, item: MenuItem, btn: Button) : unit =
    btn.BorderRadius <- zeroRadius
    btn.BoxShadows <- emptyShadows
    let mutable iconName = ""

    if not(isNull item.Id) && state.Icons.TryGetValue(item.Id, &iconName) then
      let theme = Metro.read()
      let p = theme.Palette
      let fg = if item.Disabled then p.ForegroundDisabled else p.Foreground

      let children: IList<Blob> = btn.Children

      children.Insert(0, MaterialIcons.Create(iconName, 16.0, Nullable fg))

  let stylePanel(_input: MenuInput, panel: Container) : unit =
    let theme = Metro.read()
    let p = theme.Palette
    panel.BorderRadius <- zeroRadius
    panel.BoxShadows <- emptyShadows
    panel.BorderWidth <- metroBorder

    panel.BorderColor <-
      Edges<Color>(if theme.HighContrast then p.Foreground else p.BorderStrong)

    panel.BackgroundColor <- p.BackgroundAlt

  let styleRoot(_input: MenuInput, root: Container) : unit =
    root.BorderRadius <- zeroRadius
    root.BoxShadows <- emptyShadows

  let itemsFor
    (state: MenuFactoryState, entries: MetroMenuEntry array)
    : MenuItem array =
    match state.Mapped with
    | ValueSome(cachedSource, cachedItems) when
      obj.ReferenceEquals(entries, cachedSource)
      ->
      cachedItems
    | _ ->
      let struct (mapped, icons) = mapEntries entries
      state.Icons <- icons
      state.Mapped <- ValueSome(entries, mapped)
      mapped

  let createState(onEvent: MetroMenuEvent -> unit) : MenuFactoryState =
    let state: MenuFactoryState = {
      Icons = Dictionary<string, string>(StringComparer.Ordinal)
      Mapped = ValueNone
      OnClick = Unchecked.defaultof<Action<string>>
      OnOpenChange = Unchecked.defaultof<Action<bool>>
      ConfigureItem = Unchecked.defaultof<Action<MenuInput, MenuItem, Button>>
      ConfigurePanel = Unchecked.defaultof<Action<MenuInput, Container>>
      ConfigureRoot = Unchecked.defaultof<Action<MenuInput, Container>>
    }

    state.OnClick <-
      Action<string>(fun id -> onEvent(MetroMenuEvent.ItemClicked id))

    state.OnOpenChange <-
      Action<bool>(fun isOpen -> onEvent(MetroMenuEvent.OpenChanged isOpen))

    state.ConfigureItem <-
      Action<MenuInput, MenuItem, Button>(fun _ item btn ->
        styleItem(state, item, btn))

    state.ConfigurePanel <-
      Action<MenuInput, Container>(fun input panel -> stylePanel(input, panel))

    state.ConfigureRoot <-
      Action<MenuInput, Container>(fun input root -> styleRoot(input, root))

    state

[<RequireQualifiedAccess>]
module MetroMenuFlyout =

  let item(id: string, label: string) : MetroMenuEntry =
    MetroMenuEntry.Command {
      Id = id
      Label = label
      IconName = ValueNone
      Shortcut = ValueNone
      Children = Array.empty
      Disabled = false
    }

  let iconItem(id: string, label: string, iconName: string) : MetroMenuEntry =
    MetroMenuEntry.Command {
      Id = id
      Label = label
      IconName = ValueSome iconName
      Shortcut = ValueNone
      Children = Array.empty
      Disabled = false
    }

  let submenu
    (id: string, label: string, children: MetroMenuEntry array)
    : MetroMenuEntry =
    MetroMenuEntry.Command {
      Id = id
      Label = label
      IconName = ValueNone
      Shortcut = ValueNone
      Children = (if isNull children then Array.empty else children)
      Disabled = false
    }

  let separator(id: string) : MetroMenuEntry = MetroMenuEntry.Separator id

  let defaults
    (anchor: ElementHandle, items: MetroMenuEntry array, isOpen: bool)
    : MetroMenuFlyoutProps = {
    Open = isOpen
    Anchor = anchor
    Items = items
    Placement = PopoverPlacement.BottomStart
    Width = ValueNone
    AccessibilityName = ValueNone
  }

  let mapEntries(entries: MetroMenuEntry array) : MenuItem array =
    let struct (mapped, _) = MenuInternals.mapEntries entries
    mapped

  let createInput
    (state: MenuInternals.MenuFactoryState, props: MetroMenuFlyoutProps)
    : MenuInput =
    let effectiveOpen = props.Open && not(isNull props.Anchor)

    MenuInput(
      Open = effectiveOpen,
      Anchor = props.Anchor,
      Items = MenuInternals.itemsFor(state, props.Items),
      OnClick = state.OnClick,
      OnOpenChange = state.OnOpenChange,
      Placement = props.Placement,
      Width =
        (props.Width
         |> ValueOption.map Nullable
         |> ValueOption.defaultValue(Nullable())),
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName,
      ConfigureItem = state.ConfigureItem,
      ConfigurePanel = state.ConfigurePanel,
      ConfigureRoot = state.ConfigureRoot
    )

  let createFactory(onEvent: MetroMenuEvent -> unit) : IMetroMenuFlyoutFactory =
    let state = MenuInternals.createState onEvent

    {
      new IMetroMenuFlyoutFactory with
        member _.CreateInput(props) = createInput(state, props)

        member _.Mount(key, props) =
          Cell.Mount<MenuInput, Menu>(key, createInput(state, props))
    }

[<RequireQualifiedAccess>]
module MetroContextMenu =

  let defaults
    (position: Point, items: MetroMenuEntry array, isOpen: bool)
    : MetroContextMenuProps = {
    Open = isOpen
    Position = position
    Items = items
    Width = ValueNone
    AccessibilityName = ValueSome "Context menu"
  }

  let target(child: Blob, onRequest: Point -> unit) : Blob =
    let wrapper =
      Container(
        FlexDirection = FlexDirection.Column,
        OnPointerDown =
          Action<PointerEvent>(fun e ->
            if e.Button = PointerButton.Secondary then
              onRequest e.WindowPosition)
      )

    if not(isNull child) then
      wrapper.Add(child)

    wrapper :> Blob

  let createInput
    (state: MenuInternals.MenuFactoryState, props: MetroContextMenuProps)
    : MenuInput =
    MenuInput(
      Open = props.Open,
      WindowPoint = Nullable props.Position,
      Items = MenuInternals.itemsFor(state, props.Items),
      OnClick = state.OnClick,
      OnOpenChange = state.OnOpenChange,
      Width =
        (props.Width
         |> ValueOption.map Nullable
         |> ValueOption.defaultValue(Nullable())),
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName,
      ConfigureItem = state.ConfigureItem,
      ConfigurePanel = state.ConfigurePanel,
      ConfigureRoot = state.ConfigureRoot
    )

  let createFactory
    (onEvent: MetroMenuEvent -> unit)
    : IMetroContextMenuFactory =
    let state = MenuInternals.createState onEvent

    {
      new IMetroContextMenuFactory with
        member _.CreateInput(props) = createInput(state, props)

        member _.Mount(key, props) =
          Cell.Mount<MenuInput, Menu>(key, createInput(state, props))
    }
