namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Data
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroListPickerItem = {
  Id: string
  Label: string
  IconName: string voption
  Disabled: bool
}

[<Struct>]
type MetroListPickerMode =
  | Auto
  | Inline
  | Overlay

[<Struct; NoComparison>]
type MetroListPickerInput = {
  Items: MetroListPickerItem array
  SelectedId: string
  Open: bool
  Mode: MetroListPickerMode
  Header: string voption
  Title: string voption
  Placeholder: string voption
  Disabled: bool
  Width: Length voption
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroListPickerEvent =
  | Selected of id: string
  | OpenChanged of isOpen: bool

[<Struct; NoComparison>]
type MetroListPickerState = {
  Input: MetroListPickerInput
  ResolvedMode: MetroListPickerMode
  PanelTranslate: Anim<Point>
  PanelOpacity: Anim<float>
  SheetHandle: ElementHandle
}

[<AbstractClass>]
type MetroListPickerCell() =
  inherit Cell<MetroListPickerInput>()

  abstract IsOpen: bool

  abstract ResolvedMode: MetroListPickerMode

  abstract SheetHandle: ElementHandle

  abstract View: Blob

  abstract Handle: event: MetroListPickerEvent -> unit

  // Gives one event to the host cell through InvokeInputCallback so the owner
  // rule rebuilds the host cell.
  member this.Raise
    (callback: Action<MetroListPickerEvent>, value: MetroListPickerEvent)
    =
    this.InvokeInputCallback(callback, value)

module ListPickerInternals =

  // One Overlay virtual row: the option with its resolved selection flag.
  // The virtual list diffs these immutable values, so a SelectedId change
  // makes a new array and the changed rows rebuild.
  [<Struct; NoComparison>]
  type OverlayOptionRow = {
    Item: MetroListPickerItem
    Selected: bool
  }

  // The rows and the key and builder delegates of the Overlay virtual list.
  // create makes the delegates once per cell so every build reuses them.
  [<Struct; NoComparison>]
  type OverlayRows = {
    Rows: OverlayOptionRow array
    Key: Func<OverlayOptionRow, string>
    Builder: Func<OverlayOptionRow, Blob>
  }

  [<Struct; NoComparison>]
  type OptionRowArgs = {
    Key: string
    Item: MetroListPickerItem
    Selected: bool
  }

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  [<Literal>]
  let inlineLimit = 5

  let resolveMode(input: MetroListPickerInput) : MetroListPickerMode =
    match input.Mode with
    | MetroListPickerMode.Inline -> MetroListPickerMode.Inline
    | MetroListPickerMode.Overlay -> MetroListPickerMode.Overlay
    | MetroListPickerMode.Auto ->
      let count = if isNull input.Items then 0 else input.Items.Length

      if count <= inlineLimit then
        MetroListPickerMode.Inline
      else
        MetroListPickerMode.Overlay

  let findSelected
    (items: MetroListPickerItem array, selectedId: string)
    : MetroListPickerItem voption =
    if isNull items || String.IsNullOrEmpty selectedId then
      ValueNone
    else
      let mutable found = ValueNone
      let mutable i = 0

      while i < items.Length && found.IsNone do
        if items[i].Id = selectedId then
          found <- ValueSome items[i]

        i <- i + 1

      found

  let buildOptionRow(args: OptionRowArgs, onPick: string -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    let item = args.Item
    let selected = args.Selected
    let label = if isNull item.Label then item.Id else item.Label

    let fg =
      if item.Disabled then
        p.ForegroundDisabled
      elif selected then
        (if theme.HighContrast then p.Foreground else a.OnAccent)
      else
        p.Foreground

    let bg =
      if not selected then Color.Transparent
      elif theme.HighContrast then p.Highlight
      else a.Base

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels -2.0
      )

    let btn =
      Button(
        Key = args.Key,
        Width = Length.Percent 100.0,
        MinHeight = Length.Pixels 40.0,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Length.Pixels 8.0,
        Padding = Edges(Length.Pixels 8.0, Length.Pixels 12.0),
        BorderRadius = zeroRadius,
        BorderWidth =
          Edges(
            Length.Pixels 0.0,
            Length.Pixels 0.0,
            Length.Pixels 1.0,
            Length.Pixels 0.0
          ),
        BorderColor =
          Edges<Color>(
            Color.Transparent,
            Color.Transparent,
            p.Border,
            Color.Transparent
          ),
        BoxShadows = emptyShadows,
        BackgroundColor = bg,
        HoverStyle =
          Style(
            BackgroundColor =
              (if selected then
                 (if theme.IsDark then a.Light else a.Dark)
               else
                 p.Hover)
          ),
        ActiveStyle = Style(BackgroundColor = a.Dark),
        FocusStyle = focusStyle,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if item.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not item.Disabled,
        TabStop = not item.Disabled,
        Disabled = item.Disabled,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.ListItem,
            Name = label,
            Selected = Nullable selected
          )
      )

    if not item.Disabled then
      let itemId = item.Id
      let pick = Action(fun () -> onPick itemId)
      btn.OnClick <- pick

      btn.KeyBindings <- [|
        KeyBinding(Key.Enter, KeyModifiers.None, pick)
        KeyBinding(Key.Space, KeyModifiers.None, pick)
      |]

    let leading =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 8.0,
        FlexGrow = 1.0,
        MinWidth = Length.Pixels 0.0
      )

    item.IconName
    |> ValueOption.iter(fun iconName ->
      leading.Add(MaterialIcons.Create(iconName, 18.0, Nullable fg)))

    leading.Add(
      Text(
        Content = label,
        Color = fg,
        FontFamily = theme.FontFamily,
        FontSize =
          Length.Pixels(
            MetroTypography.size MetroTextRole.Body * theme.TextScaleFactor
          ),
        FontWeight = (if selected then 600.0 else 400.0),
        TextWrap = TextWrap.NoWrap,
        TextTrimming = TextTrimming.Ellipsis
      )
    )

    btn.Add(leading)

    if selected then
      btn.Add(MaterialIcons.Create("check", 16.0, Nullable fg))

    btn

  // One Overlay option row is exactly 40 px tall: the option button declares
  // MinHeight 40 px and its content never exceeds it.
  let overlayRowPx = 40.0

  let overlayRowKey(row: OverlayOptionRow) : string = row.Item.Id

  // Maps the options to immutable virtual rows carrying the selection flag.
  // A SelectedId change makes a new array, so the virtual list rebuilds the
  // changed rows even when the Items array is unchanged.
  let overlayRowsFor
    (items: MetroListPickerItem array, selectedId: string)
    : OverlayOptionRow array =
    let source = if isNull items then Array.empty else items

    source
    |> Array.map(fun item -> {
      Item = item
      Selected = item.Id = selectedId
    })

  // The key of the content of a virtual item must equal the item key (hard
  // fact 13), so an Overlay row uses item.Id as its key.
  let overlayRow(row: OverlayOptionRow, pick: string -> unit) : Blob =
    buildOptionRow(
      {
        Key = row.Item.Id
        Item = row.Item
        Selected = row.Selected
      },
      pick
    )

  // The content of the Overlay portal: the dimmed backdrop with the sheet,
  // the header, the close button, and the option list.
  let buildOverlay
    (
      state: MetroListPickerState,
      onEvent: MetroListPickerEvent -> unit,
      rows: OverlayRows
    ) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let input = state.Input

    let accName =
      input.AccessibilityName
      |> ValueOption.orElse input.Header
      |> ValueOption.orElse input.Title
      |> ValueOption.defaultValue "List picker"

    let dismissAction =
      Action(fun () -> onEvent(MetroListPickerEvent.OpenChanged false))

    let titleText =
      input.Title
      |> ValueOption.orElse input.Header
      |> ValueOption.defaultValue "CHOOSE AN ITEM"

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let headerBar =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        Padding = Edges(Length.Pixels 12.0, Length.Pixels 16.0),
        BorderWidth =
          Edges(
            Length.Pixels 0.0,
            Length.Pixels 0.0,
            Length.Pixels 2.0,
            Length.Pixels 0.0
          ),
        BorderColor =
          Edges<Color>(
            Color.Transparent,
            Color.Transparent,
            p.BorderStrong,
            Color.Transparent
          )
      )
        .Children(
          Text(
            Key = "list-picker-overlay-title",
            Content = titleText,
            Color = p.Foreground,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Medium
                * theme.TextScaleFactor
              ),
            FontWeight = 600.0
          ),
          Button(
            Key = "list-picker-close",
            Width = Length.Pixels 36.0,
            Height = Length.Pixels 36.0,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            BorderRadius = zeroRadius,
            BorderWidth = zeroBorder,
            BorderColor = transparentBorder,
            BoxShadows = emptyShadows,
            BackgroundColor = Color.Transparent,
            HoverStyle = Style(BackgroundColor = p.Hover),
            ActiveStyle = Style(BackgroundColor = p.Active),
            FocusStyle = focusStyle,
            Cursor = Cursor.Pointer,
            Focusable = true,
            TabStop = true,
            OnClick = dismissAction,
            Accessibility =
              Accessibility(Role = AccessibilityRole.Button, Name = "Close")
          )
            .Children(
              MaterialIcons.Create("close", 20.0, Nullable p.Foreground)
            )
        )

    // The Overlay mode serves long lists, so its options render through a
    // virtual list. One option row is exactly 40 px tall.
    let list =
      VirtualLists.Rows<OverlayOptionRow>(
        rows.Rows,
        overlayRowPx,
        rows.Key,
        rows.Builder,
        false,
        Nullable(),
        null
      )

    list.Width <- Length.Percent 100.0

    list.Height <-
      Length.Pixels(min 360.0 (float rows.Rows.Length * overlayRowPx))

    list.FlexShrink <- 1.0
    list.MinHeight <- Length.Pixels 0.0
    list.FlexDirection <- FlexDirection.Column
    list.FlexWrap <- FlexWrap.NoWrap
    list.OverflowX <- Overflow.Hidden

    list.Accessibility <-
      Accessibility(Role = AccessibilityRole.List, Name = accName)

    let sheet =
      Container(
        Key = "list-picker-overlay-sheet",
        FlexDirection = FlexDirection.Column,
        Width = Length.Percent 100.0,
        MaxWidth = Length.Pixels 560.0,
        MaxHeight = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        Handle = state.SheetHandle,
        BorderRadius = zeroRadius,
        BorderWidth = metroBorder,
        BorderColor =
          Edges<Color>(
            if theme.HighContrast then p.Foreground else p.BorderStrong
          ),
        BackgroundColor = p.Background,
        TranslateAnim = state.PanelTranslate,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Dialog, Name = titleText)
      )
        .Children(headerBar, list :> Blob)

    let backdrop =
      Container(
        Key = "list-picker-backdrop",
        Position = PositionType.Absolute,
        Inset = Length.Pixels 0.0,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.FlexEnd,
        BackgroundColor = Color.Black.WithAlpha(0.7),
        OpacityAnim = state.PanelOpacity,
        KeyBindings = [|
          KeyBinding(Key.Escape, KeyModifiers.None, dismissAction)
        |]
      )
        .Children(sheet)

    backdrop

  let buildView
    (
      state: MetroListPickerState,
      onEvent: MetroListPickerEvent -> unit,
      rows: OverlayRows
    ) : Blob =
    if isNull state.PanelTranslate || isNull state.PanelOpacity then
      raise(ArgumentNullException(nameof state))

    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    let input = state.Input
    let items = if isNull input.Items then Array.empty else input.Items
    let selectedItem = findSelected(items, input.SelectedId)
    let isOpen = input.Open && not input.Disabled

    let displayLabel =
      selectedItem
      |> ValueOption.map(fun item ->
        if isNull item.Label then item.Id else item.Label)
      |> ValueOption.orElse input.Placeholder
      |> ValueOption.defaultValue "Select..."

    let isPlaceholder = selectedItem.IsNone

    let accName =
      input.AccessibilityName
      |> ValueOption.orElse input.Header
      |> ValueOption.orElse input.Title
      |> ValueOption.defaultValue "List picker"

    let borderColor =
      if theme.HighContrast then p.Foreground
      elif isOpen then a.Base
      else p.BorderStrong

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let trigger =
      Button(
        Key = "list-picker-trigger",
        Width = Length.Percent 100.0,
        MinHeight = Length.Pixels 38.0,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Length.Pixels 8.0,
        Padding = Edges(Length.Pixels 6.0, Length.Pixels 12.0),
        BorderRadius = zeroRadius,
        BorderWidth = metroBorder,
        BorderColor = Edges<Color>(borderColor),
        BoxShadows = emptyShadows,
        BackgroundColor = p.BackgroundAlt,
        HoverStyle = Style(BackgroundColor = p.Hover),
        ActiveStyle = Style(BackgroundColor = p.Active),
        FocusStyle = focusStyle,
        DisabledStyle =
          Style(Opacity = (if theme.HighContrast then 1.0 else 0.4)),
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if input.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not input.Disabled,
        TabStop = not input.Disabled,
        Disabled = input.Disabled,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.ComboBox,
            Name = accName,
            Expanded = Nullable isOpen
          )
      )

    if not input.Disabled then
      let toggle =
        Action(fun () -> onEvent(MetroListPickerEvent.OpenChanged(not isOpen)))

      trigger.OnClick <- toggle

      // The inline panel is a sibling of the focused trigger, and Goo tests a
      // key binding on the focused node and its ancestors only.
      let inlinePanelOpen =
        isOpen && state.ResolvedMode <> MetroListPickerMode.Overlay

      if inlinePanelOpen then
        let cancel =
          Action(fun () -> onEvent(MetroListPickerEvent.OpenChanged false))

        trigger.KeyBindings <- [|
          KeyBinding(Key.Enter, KeyModifiers.None, toggle)
          KeyBinding(Key.Space, KeyModifiers.None, toggle)
          KeyBinding(Key.Escape, KeyModifiers.None, cancel)
        |]
      else
        trigger.KeyBindings <- [|
          KeyBinding(Key.Enter, KeyModifiers.None, toggle)
          KeyBinding(Key.Space, KeyModifiers.None, toggle)
        |]

    let triggerLeading =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 8.0,
        FlexGrow = 1.0,
        MinWidth = Length.Pixels 0.0
      )

    selectedItem
    |> ValueOption.bind(fun item -> item.IconName)
    |> ValueOption.iter(fun iconName ->
      triggerLeading.Add(
        MaterialIcons.Create(iconName, 18.0, Nullable p.Foreground)
      ))

    triggerLeading.Add(
      Text(
        Content = displayLabel,
        Color =
          (if isPlaceholder then
             p.ForegroundSecondary
           else
             p.Foreground),
        FontFamily = theme.FontFamily,
        FontSize =
          Length.Pixels(
            MetroTypography.size MetroTextRole.Body * theme.TextScaleFactor
          ),
        FontWeight = 400.0,
        TextWrap = TextWrap.NoWrap,
        TextTrimming = TextTrimming.Ellipsis
      )
    )

    let chevronIcon = if isOpen then "expand_less" else "expand_more"

    trigger.Children(
      triggerLeading,
      MaterialIcons.Create(chevronIcon, 18.0, Nullable p.ForegroundSecondary)
    )
    |> ignore

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 6.0,
        Width = (input.Width |> ValueOption.defaultValue(Length.Percent 100.0))
      )

    input.Header
    |> ValueOption.iter(fun hdr ->
      if not(String.IsNullOrWhiteSpace hdr) then
        root.Add(
          Text(
            Key = "list-picker-header",
            Content = hdr,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Caption
                * theme.TextScaleFactor
              ),
            FontWeight = 600.0
          )
        ))

    root.Add(trigger)

    let pickItem(id: string) =
      onEvent(MetroListPickerEvent.Selected id)
      onEvent(MetroListPickerEvent.OpenChanged false)

    if isOpen then
      match state.ResolvedMode with
      | MetroListPickerMode.Inline
      | MetroListPickerMode.Auto ->
        let cancelInline =
          Action(fun () -> onEvent(MetroListPickerEvent.OpenChanged false))

        let inlinePanel =
          Container(
            Key = "list-picker-inline-panel",
            FlexDirection = FlexDirection.Column,
            Width = Length.Percent 100.0,
            BorderRadius = zeroRadius,
            BorderWidth = metroBorder,
            BorderColor = Edges<Color>(a.Base),
            BackgroundColor = p.BackgroundAlt,
            TranslateAnim = state.PanelTranslate,
            OpacityAnim = state.PanelOpacity,
            KeyBindings = [|
              KeyBinding(Key.Escape, KeyModifiers.None, cancelInline)
            |],
            Accessibility =
              Accessibility(Role = AccessibilityRole.List, Name = accName)
          )

        for i = 0 to items.Length - 1 do
          let item = items[i]

          inlinePanel.Add(
            buildOptionRow(
              {
                Key = $"list-picker-item-{item.Id}"
                Item = item
                Selected = item.Id = input.SelectedId
              },
              pickItem
            )
          )

        root.Add(inlinePanel)
      | MetroListPickerMode.Overlay ->
        let portal =
          Portal(
            Key = "list-picker-portal",
            ZIndex = 50,
            FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
            OnDismiss =
              Action<PopupDismissReason>(fun _ ->
                onEvent(MetroListPickerEvent.OpenChanged false))
          )

        portal.Add(buildOverlay(state, onEvent, rows))
        root.Add(portal)

    root

[<RequireQualifiedAccess>]
module MetroListPicker =

  let inlineThreshold = ListPickerInternals.inlineLimit

  let defaults
    (items: MetroListPickerItem array, selectedId: string)
    : MetroListPickerInput = {
    Items = items
    SelectedId = selectedId
    Open = false
    Mode = MetroListPickerMode.Auto
    Header = ValueNone
    Title = ValueNone
    Placeholder = ValueNone
    Disabled = false
    Width = ValueNone
    AccessibilityName = ValueNone
  }

  let resolveMode(input: MetroListPickerInput) : MetroListPickerMode =
    ListPickerInternals.resolveMode input

  // The public builders are stateless view seams, so they make their rows
  // and row delegates per call. The mounted cell caches its rows and reuses
  // the delegates of create.
  let overlayRowsOf
    (state: MetroListPickerState, onEvent: MetroListPickerEvent -> unit)
    : ListPickerInternals.OverlayRows =
    let pickItem(id: string) =
      onEvent(MetroListPickerEvent.Selected id)
      onEvent(MetroListPickerEvent.OpenChanged false)

    {
      ListPickerInternals.OverlayRows.Rows =
        ListPickerInternals.overlayRowsFor(
          state.Input.Items,
          state.Input.SelectedId
        )
      ListPickerInternals.OverlayRows.Key =
        Func<ListPickerInternals.OverlayOptionRow, string>(
          ListPickerInternals.overlayRowKey
        )
      ListPickerInternals.OverlayRows.Builder =
        Func<ListPickerInternals.OverlayOptionRow, Blob>(fun row ->
          ListPickerInternals.overlayRow(row, pickItem))
    }

  let buildOverlayRow
    (
      item: MetroListPickerItem,
      selectedId: string,
      onEvent: MetroListPickerEvent -> unit
    ) : Blob =
    let pickItem(id: string) =
      onEvent(MetroListPickerEvent.Selected id)
      onEvent(MetroListPickerEvent.OpenChanged false)

    ListPickerInternals.overlayRow(
      {
        Item = item
        Selected = item.Id = selectedId
      },
      pickItem
    )

  let buildOverlay
    (state: MetroListPickerState, onEvent: MetroListPickerEvent -> unit)
    : Blob =
    ListPickerInternals.buildOverlay(
      state,
      onEvent,
      overlayRowsOf(state, onEvent)
    )

  let buildView
    (state: MetroListPickerState, onEvent: MetroListPickerEvent -> unit)
    : Blob =
    ListPickerInternals.buildView(state, onEvent, overlayRowsOf(state, onEvent))

  type PickerInternalState = {
    mutable Input: MetroListPickerInput
    mutable CachedRows:
      (MetroListPickerItem array *
      string *
      ListPickerInternals.OverlayOptionRow array) voption
    mutable LastOpen: bool voption
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
    mutable SheetHandle: ElementHandle
  }

  type PickerContext = {
    State: PickerInternalState
    Raise: Action<MetroListPickerEvent>
    RowKey: Func<ListPickerInternals.OverlayOptionRow, string>
    RowBuilder: Func<ListPickerInternals.OverlayOptionRow, Blob>
  }

  let initialState() : PickerInternalState = {
    Input = defaults(Array.empty, "")
    CachedRows = ValueNone
    LastOpen = ValueNone
    LastReducedMotion = false
    ScopedReducedMotion = false
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
    SheetHandle = ElementHandle()
  }

  // The Overlay virtual rows of the input, cached on the Items reference and
  // the SelectedId value like MetroLongListSelector caches its rows.
  let rowsFor
    (state: PickerInternalState, input: MetroListPickerInput)
    : ListPickerInternals.OverlayOptionRow array =
    match state.CachedRows with
    | ValueSome(items, selectedId, rows) when
      obj.ReferenceEquals(items, input.Items) && selectedId = input.SelectedId
      ->
      rows
    | _ ->
      let rows =
        ListPickerInternals.overlayRowsFor(input.Items, input.SelectedId)

      state.CachedRows <- ValueSome(input.Items, input.SelectedId, rows)
      rows

  let currentState(ctx: PickerContext) : MetroListPickerState = {
    Input = ctx.State.Input
    ResolvedMode = resolveMode ctx.State.Input
    PanelTranslate = ctx.State.Translate
    PanelOpacity = ctx.State.Opacity
    SheetHandle = ctx.State.SheetHandle
  }

  let snapToRest(state: PickerInternalState) : unit =
    state.Translate.Snap(ListPickerInternals.zeroPoint)
    state.Opacity.Snap(1.0)

  let handle(ctx: PickerContext, event: MetroListPickerEvent) : unit =
    ctx.Raise.Invoke(event)

  let view(ctx: PickerContext) : Blob =
    let rows = {
      ListPickerInternals.OverlayRows.Rows = rowsFor(ctx.State, ctx.State.Input)
      ListPickerInternals.OverlayRows.Key = ctx.RowKey
      ListPickerInternals.OverlayRows.Builder = ctx.RowBuilder
    }

    ListPickerInternals.buildView(
      currentState ctx,
      (fun event -> handle(ctx, event)),
      rows
    )

  let inputChanged
    (ctx: PickerContext, input: MetroListPickerInput, hostReduced: bool voption)
    : unit =
    let state = ctx.State
    state.Input <- input
    let nextOpen = input.Open && not input.Disabled

    let changed =
      state.LastOpen
      |> ValueOption.map(fun prev -> prev <> nextOpen)
      |> ValueOption.defaultValue false

    if changed && nextOpen then
      let reducedMotion =
        state.ScopedReducedMotion
        || ValueOption.defaultValue state.LastReducedMotion hostReduced

      if reducedMotion then
        snapToRest state
      else
        let startY =
          if resolveMode input = MetroListPickerMode.Overlay then
            40.0
          else
            -8.0

        state.Translate.Play(
          Point(X = 0.0, Y = startY),
          ListPickerInternals.zeroPoint,
          MetroMotion.normal
        )

        state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
    elif state.LastOpen.IsNone || not nextOpen then
      snapToRest state

    state.LastOpen <- ValueSome nextOpen

  let unmount(state: PickerInternalState) : unit =
    state.LastOpen <- ValueNone
    state.ScopedReducedMotion <- false
    snapToRest state

  let build
    (ctx: PickerContext, input: MetroListPickerInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      snapToRest state

    view ctx

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let create(onEvent: MetroListPickerEvent -> unit) : MetroListPickerCell =
    let state = initialState()
    let mutable self: MetroListPickerCell voption = ValueNone
    let onEventAction = Action<MetroListPickerEvent>(onEvent)

    let raise =
      Action<MetroListPickerEvent>(fun event ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, event)))

    let pickItem(id: string) =
      raise.Invoke(MetroListPickerEvent.Selected id)
      raise.Invoke(MetroListPickerEvent.OpenChanged false)

    // The row delegates of the Overlay virtual list are made once per cell,
    // before the object expression, so every build reuses them. The rows
    // carry the selection flag, so the builder reads no changing state.
    let context: PickerContext = {
      State = state
      Raise = raise
      RowKey =
        Func<ListPickerInternals.OverlayOptionRow, string>(
          ListPickerInternals.overlayRowKey
        )
      RowBuilder =
        Func<ListPickerInternals.OverlayOptionRow, Blob>(fun row ->
          ListPickerInternals.overlayRow(row, pickItem))
    }

    let cell = {
      new MetroListPickerCell() with
        override _.IsOpen = state.Input.Open && not state.Input.Disabled
        override _.ResolvedMode = resolveMode state.Input
        override _.SheetHandle = state.SheetHandle
        override _.View = view context
        override _.Handle(event) = handle(context, event)

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    state.Translate <- cell.AnimateValue(ListPickerInternals.zeroPoint)
    state.Opacity <- cell.AnimateValue(1.0)
    self <- ValueSome cell
    cell

  let createFactory
    (onEvent: MetroListPickerEvent -> unit)
    : unit -> MetroListPickerCell =
    fun () -> create onEvent

  let mount
    (
      key: string,
      input: MetroListPickerInput,
      factory: unit -> MetroListPickerCell
    ) : Blob =
    Cell.Mount<MetroListPickerInput, MetroListPickerCell>(key, input, factory)
