namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Data
open FunGoo.Children

[<Struct; NoComparison>]
type MetroLongListItem = {
  Id: string
  Label: string
  Caption: string voption
  Disabled: bool
}

[<Struct; NoComparison>]
type MetroLongListGroup = {
  Key: string
  Title: string
  Items: MetroLongListItem array
}

[<Struct; NoComparison>]
type MetroLongListRow =
  | Header of key: string * title: string * interactive: bool
  | Item of item: MetroLongListItem * selected: bool

[<Struct; NoComparison>]
type MetroLongListInput = {
  Groups: MetroLongListGroup array
  SelectedIds: string array
  Mode: MetroListSelectionMode
  JumpOpen: bool
  ShowJumpGrid: bool
  RowHeight: float
  Height: Length voption
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroLongListEvent =
  | SelectionChanged of ids: string array
  | ItemClick of id: string
  | JumpOpenChanged of isOpen: bool
  | GroupJumped of groupKey: string

[<AbstractClass>]
type MetroLongListSelectorCell() =
  inherit Cell<MetroLongListInput>()

  abstract ScrollHandle: ElementHandle

  abstract ScaleAnim: Anim<float>

  abstract OpacityAnim: Anim<float>

  abstract IsJumpOpen: bool

  abstract View: Blob

  abstract Handle: event: MetroLongListEvent -> unit

  // Gives one event to the host cell through InvokeInputCallback so the owner
  // rule rebuilds the host cell.
  member this.Raise
    (callback: Action<MetroLongListEvent>, value: MetroLongListEvent)
    =
    this.InvokeInputCallback(callback, value)

module LongListInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let hcBorder = Edges(Length.Pixels 2.0)
  let transparentBorder = Edges<Color>(Color.Transparent)

  [<Literal>]
  let defaultRowPx = 44.0

  [<Literal>]
  let headerRowPx = 32.0

  [<Literal>]
  let indicatorPx = 3.0

  [<Literal>]
  let jumpTilePx = 64.0

  let resolveRowHeight(value: float) : float =
    if Double.IsFinite value && value > 0.0 then
      value
    else
      defaultRowPx

  // Scales one scale-1.0 base height with the theme text scale. A bad scale
  // keeps the base height instead of collapsing or exploding the layout.
  let inline scaledPx(basePx: float, scale: float) : float =
    let factor = if Double.IsFinite scale && scale > 0.0 then scale else 1.0

    basePx * factor

  let headerRowKey(groupKey: string) : string = $"lls-group-{groupKey}"

  let rowKey(row: MetroLongListRow) : string =
    match row with
    | MetroLongListRow.Header(key, _, _) -> headerRowKey key
    | MetroLongListRow.Item(item, _) -> item.Id

  let isSelected(selectedIds: string array, id: string) : bool =
    if isNull selectedIds || isNull id then
      false
    else
      let mutable found = false
      let mutable i = 0

      while i < selectedIds.Length && not found do
        found <- selectedIds[i] = id
        i <- i + 1

      found

  let flattenRows(input: MetroLongListInput) : MetroLongListRow array =
    if isNull input.Groups || input.Groups.Length = 0 then
      Array.empty
    else
      let selectedIds =
        if
          input.Mode = MetroListSelectionMode.Activate
          || isNull input.SelectedIds
        then
          Array.empty
        else
          input.SelectedIds

      let useSet = selectedIds.Length > 8
      let mutable selectedSet: HashSet<string> = null

      if useSet then
        selectedSet <- HashSet<string>(selectedIds, StringComparer.Ordinal)

      let mutable rowCount = 0

      for g = 0 to input.Groups.Length - 1 do
        let group = input.Groups[g]

        if not(isNull group.Items) && group.Items.Length > 0 then
          rowCount <- rowCount + 1 + group.Items.Length

      let rows = Array.zeroCreate<MetroLongListRow> rowCount
      let mutable index = 0

      for g = 0 to input.Groups.Length - 1 do
        let group = input.Groups[g]

        if not(isNull group.Items) && group.Items.Length > 0 then
          let title = if isNull group.Title then group.Key else group.Title

          rows[index] <-
            MetroLongListRow.Header(group.Key, title, input.ShowJumpGrid)

          index <- index + 1

          for i = 0 to group.Items.Length - 1 do
            let item = group.Items[i]

            let selected =
              if isNull item.Id then false
              elif useSet then selectedSet.Contains item.Id
              else isSelected(selectedIds, item.Id)

            rows[index] <- MetroLongListRow.Item(item, selected)
            index <- index + 1

      rows

  [<Struct; NoComparison>]
  type HeaderRowArgs = {
    Key: string
    Title: string
    Interactive: bool
    RowHeight: float
  }

  [<Struct; NoComparison>]
  type ItemRowArgs = {
    Item: MetroLongListItem
    Selected: bool
    RowHeight: float
  }

  let buildHeaderRow(args: HeaderRowArgs, onOpenJump: unit -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let accent = theme.Accent.Base
    let rowId = headerRowKey args.Key

    let upper =
      (if isNull args.Title then "" else args.Title).ToUpperInvariant()

    let label =
      Text(
        Content = upper,
        Color = (if theme.HighContrast then p.Foreground else accent),
        FontFamily = theme.FontFamily,
        FontSize = Length.Pixels(13.0 * theme.TextScaleFactor),
        FontWeight = 700.0,
        LetterSpacing = Length.Pixels(1.0 * theme.TextScaleFactor),
        TextWrap = TextWrap.NoWrap
      )

    if args.Interactive then
      let focusStyle =
        Style(
          OutlineWidth = Length.Pixels 2.0,
          OutlineColor = p.Foreground,
          OutlineOffset = Length.Pixels -2.0
        )

      let activate = Action(onOpenJump)

      Button(
        Key = rowId,
        Width = Length.Percent 100.0,
        Height = Length.Pixels(scaledPx(args.RowHeight, theme.TextScaleFactor)),
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.FlexStart,
        Padding = Edges(Length.Pixels 0.0, Length.Pixels 12.0),
        BorderRadius = zeroRadius,
        BorderWidth = (if theme.HighContrast then hcBorder else zeroBorder),
        BorderColor =
          (if theme.HighContrast then
             Edges<Color>(p.Foreground)
           else
             transparentBorder),
        BoxShadows = emptyShadows,
        BackgroundColor = p.Highlight,
        HoverStyle = Style(BackgroundColor = p.Hover),
        ActiveStyle = Style(BackgroundColor = p.Active),
        FocusStyle = focusStyle,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = Cursor.Pointer,
        Focusable = true,
        TabStop = true,
        OnClick = activate,
        KeyBindings = [|
          KeyBinding(Key.Enter, KeyModifiers.None, activate)
          KeyBinding(Key.Space, KeyModifiers.None, activate)
        |],
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Button,
            Name = $"Group {upper}, open jump list"
          )
      )
        .Children(label)
    else
      Container(
        Key = rowId,
        Width = Length.Percent 100.0,
        Height = Length.Pixels(scaledPx(args.RowHeight, theme.TextScaleFactor)),
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Padding = Edges(Length.Pixels 0.0, Length.Pixels 12.0),
        BackgroundColor = p.Highlight,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Heading, Name = upper)
      )
        .Children(label)

  let buildItemRow(args: ItemRowArgs, onActivate: string -> unit) : Blob =
    let item = args.Item
    let selected = args.Selected
    let theme = Metro.read()
    let p = theme.Palette
    let accent = theme.Accent.Base
    let labelText = if isNull item.Label then "" else item.Label

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels -2.0
      )

    let disabledStyle =
      Style(
        Color = p.ForegroundDisabled,
        Opacity = (if theme.HighContrast then 1.0 else 0.4)
      )

    let row =
      Button(
        Key = item.Id,
        Width = Length.Percent 100.0,
        Height = Length.Pixels(scaledPx(args.RowHeight, theme.TextScaleFactor)),
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.FlexStart,
        JustifyContent = JustifyContent.Center,
        Gap = Length.Pixels 2.0,
        Padding = Edges(Length.Pixels 0.0, Length.Pixels 12.0),
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
        BorderStartWidth = Length.Pixels indicatorPx,
        BorderStartColor =
          (if not selected then Color.Transparent
           elif theme.HighContrast then p.Foreground
           else accent),
        BoxShadows = emptyShadows,
        BackgroundColor =
          (if not selected then Color.Transparent
           elif theme.HighContrast then p.Highlight
           else accent.WithAlpha(0.3)),
        HoverStyle = Style(BackgroundColor = p.Hover),
        ActiveStyle = Style(BackgroundColor = p.Active),
        DisabledStyle = disabledStyle,
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
            Name = labelText,
            Selected = Nullable selected
          )
      )

    if not item.Disabled then
      let activate = Action(fun () -> onActivate item.Id)
      row.OnClick <- activate

      row.KeyBindings <- [|
        KeyBinding(Key.Enter, KeyModifiers.None, activate)
        KeyBinding(Key.Space, KeyModifiers.None, activate)
      |]

    row.Add(
      Text(
        Content = labelText,
        Color = p.Foreground,
        FontFamily = theme.FontFamily,
        FontSize = Length.Pixels(14.0 * theme.TextScaleFactor),
        FontWeight = 400.0,
        TextWrap = TextWrap.NoWrap,
        TextTrimming = TextTrimming.Ellipsis
      )
    )

    item.Caption
    |> ValueOption.iter(fun caption ->
      if not(String.IsNullOrEmpty caption) then
        row.Add(
          Text(
            Content = caption,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
            FontWeight = 400.0,
            TextWrap = TextWrap.NoWrap,
            TextTrimming = TextTrimming.Ellipsis
          )
        ))

    row

  let buildJumpGrid
    (
      input: MetroLongListInput,
      onSelectGroup: string -> unit,
      onCancel: unit -> unit
    ) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent

    let groups = if isNull input.Groups then Array.empty else input.Groups

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let cancelAction = Action(onCancel)
    let mutable autoFocusPending = true

    // With no enabled tile, no tile can take the focus and the Escape binding
    // of the grid cannot run, so the grid itself takes the focus.
    let anyEnabled =
      groups
      |> Array.exists(fun group ->
        not(isNull group.Items) && group.Items.Length > 0)

    let grid =
      Container(
        Key = "lls-jump-grid",
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.Wrap,
        AlignContent = AlignContent.FlexStart,
        Gap = Length.Pixels 8.0,
        Width = Length.Percent 100.0,
        FlexGrow = 1.0,
        OverflowY = Overflow.Scroll,
        OverflowX = Overflow.Hidden,
        Padding = Edges(Length.Pixels 8.0),
        BackgroundColor = p.Background,
        Focusable = not anyEnabled,
        AutoFocus = not anyEnabled,
        KeyBindings = [|
          KeyBinding(Key.Escape, KeyModifiers.None, cancelAction)
        |],
        Accessibility =
          Accessibility(Role = AccessibilityRole.Grid, Name = "Jump to group")
      )

    for i = 0 to groups.Length - 1 do
      let group = groups[i]
      let hasItems = not(isNull group.Items) && group.Items.Length > 0
      let title = (if isNull group.Title then group.Key else group.Title)
      let upper = (if isNull title then "" else title).ToUpperInvariant()

      let tileAutoFocus = hasItems && autoFocusPending

      if tileAutoFocus then
        autoFocusPending <- false

      let tileBg =
        if not hasItems then p.BackgroundAlt
        elif theme.HighContrast then p.Highlight
        else a.Base

      let tileFg =
        if not hasItems then p.ForegroundDisabled
        elif theme.HighContrast then p.Foreground
        else a.OnAccent

      let tilePx = scaledPx(jumpTilePx, theme.TextScaleFactor)

      let tile =
        Button(
          Key = $"lls-jump-{group.Key}",
          Width = Length.Pixels tilePx,
          Height = Length.Pixels tilePx,
          FlexShrink = 0.0,
          AlignItems = AlignItems.FlexEnd,
          JustifyContent = JustifyContent.FlexStart,
          Padding = Edges(Length.Pixels 8.0),
          BorderRadius = zeroRadius,
          BorderWidth = (if theme.HighContrast then hcBorder else zeroBorder),
          BorderColor =
            (if theme.HighContrast then
               Edges<Color>(p.Foreground)
             else
               transparentBorder),
          BoxShadows = emptyShadows,
          BackgroundColor = tileBg,
          HoverStyle =
            Style(BackgroundColor = (if theme.IsDark then a.Light else a.Dark)),
          ActiveStyle = Style(BackgroundColor = a.Dark),
          FocusStyle = focusStyle,
          TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
          TransitionEasing = Easing.EaseOut,
          Cursor = (if hasItems then Cursor.Pointer else Cursor.Default),
          Focusable = hasItems,
          TabStop = hasItems,
          Disabled = not hasItems,
          AutoFocus = tileAutoFocus,
          Accessibility =
            Accessibility(Role = AccessibilityRole.GridCell, Name = upper)
        )

      if hasItems then
        let groupKey = group.Key
        let select = Action(fun () -> onSelectGroup groupKey)
        tile.OnClick <- select

        tile.KeyBindings <- [|
          KeyBinding(Key.Enter, KeyModifiers.None, select)
          KeyBinding(Key.Space, KeyModifiers.None, select)
        |]

      tile.Add(
        Text(
          Content = upper,
          Color = tileFg,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(20.0 * theme.TextScaleFactor),
          FontWeight = 300.0,
          TextWrap = TextWrap.NoWrap
        )
      )

      grid.Add(tile)

    grid

[<RequireQualifiedAccess>]
module MetroLongListSelector =

  let defaultRowHeightPx = LongListInternals.defaultRowPx

  let defaults(groups: MetroLongListGroup array) : MetroLongListInput = {
    Groups = groups
    SelectedIds = Array.empty
    Mode = MetroListSelectionMode.Single
    JumpOpen = false
    ShowJumpGrid = true
    RowHeight = defaultRowHeightPx
    Height = ValueNone
    AccessibilityName = ValueNone
  }

  let flattenRows(input: MetroLongListInput) : MetroLongListRow array =
    LongListInternals.flattenRows input

  let buildJumpGrid
    (
      input: MetroLongListInput,
      onSelectGroup: string -> unit,
      onCancel: unit -> unit
    ) : Blob =
    LongListInternals.buildJumpGrid(input, onSelectGroup, onCancel)

  type SelectorState = {
    ScrollHandle: ElementHandle
    mutable Input: MetroLongListInput
    mutable CachedRows:
      (MetroLongListGroup array *
      string array *
      MetroListSelectionMode *
      bool *
      MetroLongListRow array) voption
    mutable LastJumpOpen: bool voption
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable PendingJumpKey: string voption
    mutable Scale: Anim<float>
    mutable Opacity: Anim<float>
  }

  type SelectorContext = {
    State: SelectorState
    Raise: Action<MetroLongListEvent>
    RowKey: Func<MetroLongListRow, string>
    mutable RowBuilder: Func<MetroLongListRow, Blob>
  }

  let initialState() : SelectorState = {
    ScrollHandle = ElementHandle()
    Input = defaults Array.empty
    CachedRows = ValueNone
    LastJumpOpen = ValueNone
    LastReducedMotion = false
    ScopedReducedMotion = false
    PendingJumpKey = ValueNone
    Scale = Unchecked.defaultof<Anim<float>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  let snapToRest(state: SelectorState) : unit =
    state.Scale.Snap(1.0)
    state.Opacity.Snap(1.0)

  let rowsFor
    (state: SelectorState, input: MetroLongListInput)
    : MetroLongListRow array =
    match state.CachedRows with
    | ValueSome(groups, selected, mode, showJump, rows) when
      obj.ReferenceEquals(groups, input.Groups)
      && obj.ReferenceEquals(selected, input.SelectedIds)
      && mode = input.Mode
      && showJump = input.ShowJumpGrid
      ->
      rows
    | _ ->
      let rows = LongListInternals.flattenRows input

      state.CachedRows <-
        ValueSome(
          input.Groups,
          input.SelectedIds,
          input.Mode,
          input.ShowJumpGrid,
          rows
        )

      rows

  let requestItem(ctx: SelectorContext, id: string) : unit =
    let input = ctx.State.Input
    let selectedIds = input.SelectedIds

    match input.Mode with
    | MetroListSelectionMode.Activate ->
      ctx.Raise.Invoke(MetroLongListEvent.ItemClick id)
    | MetroListSelectionMode.Single ->
      let already =
        not(isNull selectedIds) && selectedIds.Length = 1 && selectedIds[0] = id

      if not already then
        ctx.Raise.Invoke(MetroLongListEvent.SelectionChanged [| id |])
    | MetroListSelectionMode.Multiple ->
      let selected = LongListInternals.isSelected(selectedIds, id)

      let next =
        if selected then
          if isNull selectedIds then
            Array.empty
          else
            selectedIds |> Array.filter(fun existing -> existing <> id)
        elif isNull selectedIds then
          [| id |]
        else
          Array.append selectedIds [| id |]

      ctx.Raise.Invoke(MetroLongListEvent.SelectionChanged next)

  let jumpToGroup(ctx: SelectorContext, groupKey: string) : unit =
    let state = ctx.State
    let targetKey = LongListInternals.headerRowKey groupKey
    let settle = not(state.ScopedReducedMotion || state.LastReducedMotion)

    if
      not(
        state.ScrollHandle.ScrollToItem(
          targetKey,
          ScrollAlignment.Start,
          settle
        )
      )
    then
      state.PendingJumpKey <- ValueSome targetKey

    ctx.Raise.Invoke(MetroLongListEvent.GroupJumped groupKey)

    if state.Input.JumpOpen then
      ctx.Raise.Invoke(MetroLongListEvent.JumpOpenChanged false)

  let handle(ctx: SelectorContext, event: MetroLongListEvent) : unit =
    match event with
    | MetroLongListEvent.GroupJumped groupKey -> jumpToGroup(ctx, groupKey)
    | other -> ctx.Raise.Invoke(other)

  let buildRow(ctx: SelectorContext, row: MetroLongListRow) : Blob =
    let rowHeight = LongListInternals.resolveRowHeight ctx.State.Input.RowHeight

    match row with
    | MetroLongListRow.Header(key, title, interactive) ->
      LongListInternals.buildHeaderRow(
        {
          Key = key
          Title = title
          Interactive = interactive
          RowHeight = LongListInternals.headerRowPx
        },
        fun () -> ctx.Raise.Invoke(MetroLongListEvent.JumpOpenChanged true)
      )
    | MetroLongListRow.Item(item, selected) ->
      LongListInternals.buildItemRow(
        {
          Item = item
          Selected = selected
          RowHeight = rowHeight
        },
        fun id -> requestItem(ctx, id)
      )

  let buildView(ctx: SelectorContext) : Blob =
    let state = ctx.State
    let input = state.Input
    let theme = Metro.read()

    let rowHeight =
      LongListInternals.scaledPx(
        LongListInternals.resolveRowHeight input.RowHeight,
        theme.TextScaleFactor
      )

    let jumpShown = input.JumpOpen && input.ShowJumpGrid

    let rows = rowsFor(state, input)

    let list =
      VirtualLists.Rows<MetroLongListRow>(
        rows,
        rowHeight,
        ctx.RowKey,
        ctx.RowBuilder,
        false,
        Nullable(),
        null
      )

    list.Handle <- state.ScrollHandle
    list.FlexDirection <- FlexDirection.Column
    list.FlexWrap <- FlexWrap.NoWrap
    list.OverflowX <- Overflow.Hidden
    list.Width <- Length.Percent 100.0
    list.FlexGrow <- 1.0

    input.Height
    |> ValueOption.iter(fun h ->
      list.Height <- h
      list.FlexGrow <- 0.0)

    list.Accessibility <-
      Accessibility(
        Role = AccessibilityRole.List,
        Name =
          (input.AccessibilityName
           |> ValueOption.defaultValue "Long list selector")
      )

    // The grid overlays the mounted list, so the list node keeps its scroll
    // offset while the grid is open.
    list.Visibility <-
      (if jumpShown then Visibility.Hidden else Visibility.Visible)

    state.PendingJumpKey
    |> ValueOption.iter(fun pendingKey ->
      let settle = not(state.ScopedReducedMotion || state.LastReducedMotion)

      if
        state.ScrollHandle.ScrollToItem(
          pendingKey,
          ScrollAlignment.Start,
          settle
        )
      then
        state.PendingJumpKey <- ValueNone)

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        Width = Length.Percent 100.0,
        FlexGrow = (if input.Height.IsSome then 0.0 else 1.0),
        Overflow = Overflow.Hidden,
        ScaleAnim = state.Scale,
        OpacityAnim = state.Opacity
      )

    root.Add list

    if jumpShown then
      let overlay =
        Container(
          Position = PositionType.Absolute,
          Inset = Edges(Length.Pixels 0.0),
          Overflow = Overflow.Hidden,
          BackgroundColor = theme.Palette.Background
        )

      overlay.Children(
        LongListInternals.buildJumpGrid(
          input,
          (fun groupKey -> jumpToGroup(ctx, groupKey)),
          fun () -> ctx.Raise.Invoke(MetroLongListEvent.JumpOpenChanged false)
        )
      )
      |> ignore

      root.Add overlay

    input.Height |> ValueOption.iter(fun h -> root.Height <- h)

    root :> Blob

  let inputChanged
    (ctx: SelectorContext, input: MetroLongListInput, hostReduced: bool voption)
    : unit =
    let state = ctx.State
    state.Input <- input
    let nextJump = input.JumpOpen && input.ShowJumpGrid

    let changed =
      state.LastJumpOpen
      |> ValueOption.map(fun prev -> prev <> nextJump)
      |> ValueOption.defaultValue false

    if changed then
      let reducedMotion =
        state.ScopedReducedMotion
        || ValueOption.defaultValue state.LastReducedMotion hostReduced

      if reducedMotion then
        snapToRest state
      else
        state.Scale.Play(0.96, 1.0, MetroMotion.normal)
        state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
    elif state.LastJumpOpen.IsNone then
      snapToRest state

    state.LastJumpOpen <- ValueSome nextJump

  let unmount(state: SelectorState) : unit =
    state.LastJumpOpen <- ValueNone
    state.PendingJumpKey <- ValueNone
    state.ScopedReducedMotion <- false
    snapToRest state

  let build
    (ctx: SelectorContext, input: MetroLongListInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      snapToRest state

    buildView ctx

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let create(onEvent: MetroLongListEvent -> unit) : MetroLongListSelectorCell =
    let state = initialState()
    let mutable self: MetroLongListSelectorCell voption = ValueNone
    let onEventAction = Action<MetroLongListEvent>(onEvent)

    let raise =
      Action<MetroLongListEvent>(fun event ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, event)))

    let context: SelectorContext = {
      State = state
      Raise = raise
      RowKey = Func<MetroLongListRow, string>(LongListInternals.rowKey)
      RowBuilder = Unchecked.defaultof<Func<MetroLongListRow, Blob>>
    }

    context.RowBuilder <-
      Func<MetroLongListRow, Blob>(fun row -> buildRow(context, row))

    let cell = {
      new MetroLongListSelectorCell() with
        override _.ScrollHandle = state.ScrollHandle
        override _.ScaleAnim = state.Scale
        override _.OpacityAnim = state.Opacity

        override _.IsJumpOpen = state.Input.JumpOpen && state.Input.ShowJumpGrid

        override _.View = buildView context
        override _.Handle(event) = handle(context, event)

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    state.Scale <- cell.AnimateValue(1.0)
    state.Opacity <- cell.AnimateValue(1.0)
    self <- ValueSome cell
    cell

  let createFactory
    (onEvent: MetroLongListEvent -> unit)
    : unit -> MetroLongListSelectorCell =
    fun () -> create onEvent

  let mount
    (
      key: string,
      input: MetroLongListInput,
      factory: unit -> MetroLongListSelectorCell
    ) : Blob =
    Cell.Mount<MetroLongListInput, MetroLongListSelectorCell>(
      key,
      input,
      factory
    )
