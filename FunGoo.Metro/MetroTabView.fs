namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Icons
open Goo.Widgets.Navigation
open FunGoo.Children

[<Struct; NoComparison>]
type MetroTabItem = {
  Id: string
  Header: string
  IconName: string voption
  Closable: bool
  Disabled: bool
}

[<Struct; NoComparison>]
type MetroTabViewEvent =
  | Selected of id: string
  | Closed of id: string
  | AddRequested

[<Struct; NoComparison>]
type MetroTabViewProps = {
  Items: MetroTabItem array
  SelectedId: string
  ShowAddButton: bool
  AccessibilityName: string voption
}

type IMetroTabViewFactory =
  abstract CreateInput: props: MetroTabViewProps -> TabBarInput

  abstract Build:
    key: string * props: MetroTabViewProps * renderPanel: (string -> Blob) ->
      Blob

module TabViewInternals =

  let zeroRadius = Length.Pixels 0.0
  let emptyShadows: BoxShadow array = Array.empty

  let newTabBar() : TabBar = new TabBar()

  let mapItems
    (source: MetroTabItem array, iconsById: Dictionary<string, string>)
    : NavigationItem array * string array * bool =
    iconsById.Clear()

    if isNull source || source.Length = 0 then
      Array.empty, Array.empty, false
    else
      let nav = Array.zeroCreate<NavigationItem> source.Length
      let pinned = ResizeArray<string>()
      let mutable anyClosable = false

      for i = 0 to source.Length - 1 do
        let t = source[i]

        t.IconName
        |> ValueOption.iter(fun name ->
          if
            not(String.IsNullOrWhiteSpace name)
            && not(String.IsNullOrEmpty t.Id)
          then
            iconsById[t.Id] <- name)

        nav[i] <-
          NavigationItem(Id = t.Id, Label = t.Header, Disabled = t.Disabled)

        if t.Closable then anyClosable <- true else pinned.Add t.Id

      nav, pinned.ToArray(), anyClosable

[<RequireQualifiedAccess>]
module MetroTabView =

  let item(id: string, header: string) : MetroTabItem = {
    Id = id
    Header = header
    IconName = ValueNone
    Closable = true
    Disabled = false
  }

  let pinnedItem(id: string, header: string) : MetroTabItem = {
    item(id, header) with
        Closable = false
  }

  let iconItem(id: string, header: string, iconName: string) : MetroTabItem = {
    item(id, header) with
        IconName = ValueSome iconName
  }

  let defaults
    (items: MetroTabItem array, selectedId: string)
    : MetroTabViewProps = {
    Items = items
    SelectedId = selectedId
    ShowAddButton = false
    AccessibilityName = ValueNone
  }

  type CacheState = {
    IconsById: Dictionary<string, string>
    mutable SourceItems: MetroTabItem array
    mutable MappedItems: NavigationItem array
    mutable PinnedIds: string array
    mutable AnyClosable: bool
  }

  let createFactory(onEvent: MetroTabViewEvent -> unit) : IMetroTabViewFactory =
    let cache: CacheState = {
      IconsById = Dictionary<string, string>(StringComparer.Ordinal)
      SourceItems = null
      MappedItems = Array.empty
      PinnedIds = Array.empty
      AnyClosable = false
    }

    let onSelect =
      Action<string>(fun id -> onEvent(MetroTabViewEvent.Selected id))

    let onClose = Action<string>(fun id -> onEvent(MetroTabViewEvent.Closed id))
    let onAdd = Action(fun () -> onEvent MetroTabViewEvent.AddRequested)

    // Builds fresh icon Blobs inside TabBar.Build so icons sit beside the label
    // and never reuse a frozen Blob across TabBar rebuilds.
    let configureItem =
      Action<TabBarInput, NavigationItem, Button>(fun _ navItem btn ->
        btn.BorderRadius <- TabViewInternals.zeroRadius
        btn.BoxShadows <- TabViewInternals.emptyShadows

        if not(isNull navItem.Id) then
          match cache.IconsById.TryGetValue navItem.Id with
          | true, iconName ->
            btn.Children.Insert(0, MaterialIcons.Create(iconName, 16.0))
          | false, _ -> ())

    let createInput(props: MetroTabViewProps) : TabBarInput =
      if not(Object.ReferenceEquals(cache.SourceItems, props.Items)) then
        let nav, pinned, anyClosable =
          TabViewInternals.mapItems(props.Items, cache.IconsById)

        cache.SourceItems <- props.Items
        cache.MappedItems <- nav
        cache.PinnedIds <- pinned
        cache.AnyClosable <- anyClosable

      let a11yName = props.AccessibilityName |> ValueOption.toObj

      TabBarInput(
        Items = cache.MappedItems,
        SelectedId = props.SelectedId,
        OnSelect = onSelect,
        OnClose = (if cache.AnyClosable then onClose else null),
        OnAdd = (if props.ShowAddButton then onAdd else null),
        PinnedIds = cache.PinnedIds,
        ShowIndicator = Nullable true,
        ItemHeight = Nullable 40.0,
        AccessibilityName = a11yName,
        ConfigureItem = configureItem
      )

    let mountStrip(key: string, props: MetroTabViewProps) : Blob =
      let input = createInput props
      Cell.Mount<TabBarInput, TabBar>(key, input, TabViewInternals.newTabBar)

    {
      new IMetroTabViewFactory with
        member _.CreateInput(props) = createInput props

        member _.Build(key, props, renderPanel) =
          let theme = Metro.read()
          let p = theme.Palette
          let strip = mountStrip(key, props)

          // createInput has synced the cache with props.Items one call above,
          // so one loop over the mapped items finds the selected header.
          let mutable selectedHeader = ""
          let mutable i = 0

          while i < cache.MappedItems.Length
                && cache.MappedItems[i].Id <> props.SelectedId do
            i <- i + 1

          if i < cache.MappedItems.Length then
            selectedHeader <- cache.MappedItems[i].Label

          let panel =
            Container(
              Key = $"{key}-panel",
              Width = Length.Percent 100.0,
              FlexDirection = FlexDirection.Column,
              FlexGrow = 1.0,
              Padding = Edges(Length.Pixels 12.0, Length.Pixels 0.0),
              BorderWidth =
                Edges(
                  Length.Pixels 2.0,
                  Length.Pixels 0.0,
                  Length.Pixels 0.0,
                  Length.Pixels 0.0
                ),
              BorderColor = Edges<Color>(p.Border),
              Accessibility =
                Accessibility(
                  Role = AccessibilityRole.TabPanel,
                  Name = selectedHeader
                )
            )

          if not(String.IsNullOrEmpty props.SelectedId) then
            let content = renderPanel props.SelectedId

            if not(isNull content) then
              panel.Add(content)

          Container(
            Key = $"{key}-root",
            Width = Length.Percent 100.0,
            FlexDirection = FlexDirection.Column
          )
            .Children(strip, panel)
          :> Blob
    }
