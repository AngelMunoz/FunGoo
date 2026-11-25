module MetroGallery.NavigationTab

open Goo
open Goo.Widgets.Navigation
open FunGoo.Children
open FunGoo.Metro

[<AbstractClass>]
type NavigationTabCell() =
  inherit Cell()

  abstract SplitView: MetroSplitViewCell voption
  abstract Panorama: MetroPanoramaCell voption
  abstract TabBarInput: TabBarInput
  abstract BreadcrumbInput: BreadcrumbInput
  abstract SplitOpen: bool
  abstract SplitMode: MetroSplitViewMode
  abstract ActiveSubTab: string
  abstract TabCount: int
  abstract PinChecked: bool
  abstract ShuffleChecked: bool
  abstract LastAction: string
  abstract Builds: int
  abstract SetSplitOpen: isOpen: bool -> unit
  abstract SetSplitMode: mode: MetroSplitViewMode -> unit

module Content =

  let initialCrumbs: MetroBreadcrumbItem array = [|
    MetroBreadcrumbBar.iconItem("root", "Start", "home")
    MetroBreadcrumbBar.item("apps", "Collection")
    MetroBreadcrumbBar.item("media", "Music + Video")
    MetroBreadcrumbBar.item("now", "Now Playing")
  |]

  let initialTabs: MetroTabItem array = [|
    MetroTabView.pinnedItem("overview", "Overview")
    MetroTabView.iconItem("queue", "Play Queue", "queue_music")
    MetroTabView.item("lyrics", "Lyrics")
  |]

  let panoramaSections: MetroPanoramaSection array = [|
    MetroPanorama.sizedSection("featured", "featured", 240.0)
    MetroPanorama.sizedSection("recent", "recent", 240.0)
    MetroPanorama.sizedSection("artists", "artists", 240.0)
  |]

type NavigationState = {
  mutable Crumbs: MetroBreadcrumbItem array
  mutable Tabs: MetroTabItem array
  mutable ActiveSubTab: string
  mutable NextTabSeq: int
  mutable SplitOpen: bool
  mutable SplitMode: MetroSplitViewMode
  mutable SplitPlacement: MetroSplitViewPlacement
  mutable PinChecked: bool
  mutable ShuffleChecked: bool
  mutable LastTabInput: TabBarInput
  mutable LastBreadcrumbInput: BreadcrumbInput
  mutable LastAction: string
  mutable Builds: int
}

type NavigationFactories = {
  SplitView: unit -> MetroSplitViewCell
  Panorama: unit -> MetroPanoramaCell
  TabView: IMetroTabViewFactory
  Breadcrumb: IMetroBreadcrumbFactory
}

let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 12.0
  )
    .Children(MetroText.subheader title, content)

let buildSplitPane(state: NavigationState, onSelectNav: string -> unit) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    Padding = Edges(Length.Pixels 8.0),
    Gap = Length.Pixels 6.0
  )
    .Children(
      MetroButton.subtle("H  Home", fun () -> onSelectNav "Home"),
      MetroButton.subtle("L  Library", fun () -> onSelectNav "Library"),
      MetroButton.subtle("R  Radio", fun () -> onSelectNav "Radio")
    )

let buildSplitContent(state: NavigationState) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    Padding = Edges(Length.Pixels 12.0),
    Gap = Length.Pixels 6.0
  )
    .Children(
      MetroText.subtitle $"Mode: {state.SplitMode}",
      MetroText.secondary
        $"Pane open: {state.SplitOpen} ({state.SplitPlacement})"
    )

let buildPanoramaBackground() : Blob =
  let theme = Metro.read()

  Container(
    FlexDirection = FlexDirection.Row,
    AlignItems = AlignItems.Center,
    Gap = Length.Pixels 24.0,
    Padding = Edges(Length.Pixels 16.0)
  )
    .Children(
      Container(
        Width = Length.Pixels 120.0,
        Height = Length.Pixels 120.0,
        BackgroundColor = theme.Accent.Base
      ),
      Container(
        Width = Length.Pixels 180.0,
        Height = Length.Pixels 120.0,
        BackgroundColor = theme.Accent.Light
      ),
      Container(
        Width = Length.Pixels 140.0,
        Height = Length.Pixels 120.0,
        BackgroundColor = theme.Accent.Dark
      )
    )
  :> Blob

let buildPanoramaSection(secId: string) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  let copy =
    match secId with
    | "featured" -> "Background 0.15x, Hero title 0.35x, section strip 1.0x."
    | "recent" -> "Scroll horizontally with wheel or touch with zero rebuilds."
    | _ -> "Peeking adjacent sections invite horizontal exploration."

  Container(
    Padding = Edges(Length.Pixels 12.0),
    BackgroundColor = p.BackgroundAlt
  )
    .Children(MetroText.body copy)
  :> Blob

let view(state: NavigationState, factories: NavigationFactories) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  let crumbProps = MetroBreadcrumbBar.defaults state.Crumbs
  state.LastBreadcrumbInput <- factories.Breadcrumb.CreateInput crumbProps

  let tabProps = {
    MetroTabView.defaults(state.Tabs, state.ActiveSubTab) with
        ShowAddButton = true
  }

  state.LastTabInput <- factories.TabView.CreateInput tabProps

  let splitInput = {
    MetroSplitView.defaults state.SplitOpen with
        DisplayMode = state.SplitMode
        PanePlacement = state.SplitPlacement
        OpenPaneLength = 180.0
        CompactPaneLength = 44.0
  }

  let topStrip =
    Container(
      Width = Length.Percent 100.0,
      FlexDirection = FlexDirection.Row,
      AlignItems = AlignItems.Center,
      JustifyContent = JustifyContent.SpaceBetween,
      Gap = Length.Pixels 16.0,
      Padding = Edges(Length.Pixels 8.0, Length.Pixels 12.0),
      BackgroundColor = p.BackgroundAlt
    )
      .Children(
        Container(FlexGrow = 1.0, MinWidth = Length.Pixels 0.0)
          .Children(
            factories.Breadcrumb.Mount("gallery-breadcrumb", crumbProps)
          ),
        Container(
          FlexDirection = FlexDirection.Row,
          AlignItems = AlignItems.Center,
          FlexShrink = 0.0,
          Gap = Length.Pixels 4.0
        )
          .Children(
            MetroAppBarButton.build(
              MetroAppBarButton.defaults("Play", "play_arrow"),
              fun () -> state.LastAction <- "AppBar: Play"
            ),
            MetroAppBarButton.build(
              MetroAppBarButton.compact("Skip", "skip_next"),
              fun () -> state.LastAction <- "AppBar: Skip"
            ),
            MetroAppBarSeparator.standard(),
            MetroAppBarToggleButton.build(
              MetroAppBarToggleButton.defaults(
                "Pin",
                "push_pin",
                state.PinChecked
              ),
              fun next ->
                state.PinChecked <- next
                state.LastAction <- $"Pin toggled: {next}"
            ),
            MetroAppBarToggleButton.build(
              MetroAppBarToggleButton.defaults(
                "Shuffle",
                "shuffle",
                state.ShuffleChecked
              ),
              fun next ->
                state.ShuffleChecked <- next
                state.LastAction <- $"Shuffle toggled: {next}"
            )
          )
      )

  let splitAndTabsCol =
    column(
      "split view & tab view",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
        .Children(
          Container(
            Key = "gallery-split-controls",
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Gap = Length.Pixels 8.0
          )
            .Children(
              MetroButton.accent(
                (if state.SplitOpen then "Close Pane" else "Open Pane"),
                fun () -> state.SplitOpen <- not state.SplitOpen
              ),
              MetroButton.standard(
                "Compact",
                fun () -> state.SplitMode <- MetroSplitViewMode.Compact
              ),
              MetroButton.standard(
                "Inline",
                fun () -> state.SplitMode <- MetroSplitViewMode.Inline
              ),
              MetroButton.standard(
                "Overlay",
                fun () -> state.SplitMode <- MetroSplitViewMode.Overlay
              ),
              MetroButton.standard(
                "Flip Edge",
                fun () ->
                  state.SplitPlacement <-
                    match state.SplitPlacement with
                    | MetroSplitViewPlacement.Start ->
                      MetroSplitViewPlacement.End
                    | MetroSplitViewPlacement.End ->
                      MetroSplitViewPlacement.Start
              )
            ),
          Container(
            Key = "gallery-split-frame",
            Width = Length.Percent 100.0,
            Height = Length.Pixels 164.0,
            BackgroundColor = p.BackgroundAlt
          )
            .Children(
              MetroSplitView.mount(
                "gallery-split-view",
                splitInput,
                factories.SplitView
              )
            ),
          factories.TabView.Build(
            "gallery-tab-view",
            tabProps,
            fun selectedId ->
              MetroText.body
                $"Active document tab: '{selectedId}' ({state.Tabs.Length} open tabs)."
          )
        )
    )

  let panoramaCol =
    column(
      "panorama parallax",
      Container(
        Width = Length.Percent 100.0,
        Height = Length.Pixels 296.0,
        BackgroundColor = p.BackgroundAlt
      )
        .Children(
          MetroPanorama.mount(
            "gallery-panorama",
            MetroPanorama.defaults("zune media", Content.panoramaSections),
            factories.Panorama
          )
        )
    )

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 14.0
  )
    .Children(
      topStrip,
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        Gap = Length.Pixels 28.0
      )
        .Children(splitAndTabsCol, panoramaCol),
      MetroText.caption state.LastAction
    )

let create() : NavigationTabCell =
  let state: NavigationState = {
    Crumbs = Content.initialCrumbs
    Tabs = Content.initialTabs
    ActiveSubTab = "overview"
    NextTabSeq = 4
    SplitOpen = true
    SplitMode = MetroSplitViewMode.Compact
    SplitPlacement = MetroSplitViewPlacement.Start
    PinChecked = true
    ShuffleChecked = false
    LastTabInput = Unchecked.defaultof<TabBarInput>
    LastBreadcrumbInput = Unchecked.defaultof<BreadcrumbInput>
    LastAction = "Ready"
    Builds = 0
  }

  let mutable self: NavigationTabCell voption = ValueNone
  let mutable splitRef: MetroSplitViewCell voption = ValueNone
  let mutable panoramaRef: MetroPanoramaCell voption = ValueNone

  // SplitView builds its pane inside its own Build, so selecting an item inside
  // the pane rebuilds this tab explicitly via `self`.
  let onSelectSplitNav(name: string) =
    state.LastAction <- $"SplitView nav: {name}"
    self |> ValueOption.iter(fun tab -> tab.Rebuild())

  let factories: NavigationFactories = {
    SplitView =
      fun () ->
        let c =
          MetroSplitView.create(
            (fun isOpen ->
              state.SplitOpen <- isOpen
              state.LastAction <- $"SplitView open: {isOpen}"),
            (fun () -> buildSplitPane(state, onSelectSplitNav)),
            fun () -> buildSplitContent state
          )

        splitRef <- ValueSome c
        c
    Panorama =
      fun () ->
        let c =
          MetroPanorama.createWithBackground(
            buildPanoramaBackground,
            buildPanoramaSection
          )

        panoramaRef <- ValueSome c
        c
    TabView =
      MetroTabView.createFactory(function
        | MetroTabViewEvent.Selected id ->
          state.ActiveSubTab <- id
          state.LastAction <- $"Tab selected: {id}"
        | MetroTabViewEvent.Closed id ->
          let remaining = state.Tabs |> Array.filter(fun t -> t.Id <> id)

          if remaining.Length > 0 then
            state.Tabs <- remaining

            if state.ActiveSubTab = id then
              state.ActiveSubTab <- remaining[remaining.Length - 1].Id

            state.LastAction <- $"Tab closed: {id}"
        | MetroTabViewEvent.AddRequested ->
          let n = state.NextTabSeq
          state.NextTabSeq <- n + 1
          let id = $"tab-{n}"

          state.Tabs <-
            Array.append state.Tabs [| MetroTabView.item(id, $"Tab {n}") |]

          state.ActiveSubTab <- id
          state.LastAction <- $"Tab added: {id}")
    Breadcrumb =
      MetroBreadcrumbBar.createFactory(fun id ->
        let idxOpt = state.Crumbs |> Array.tryFindIndex(fun c -> c.Id = id)

        idxOpt
        |> Option.iter(fun idx ->
          state.Crumbs <- state.Crumbs[0..idx]
          state.LastAction <- $"Breadcrumb: {id}"))
  }

  let cell = {
    new NavigationTabCell() with
      override _.SplitView = splitRef
      override _.Panorama = panoramaRef
      override _.TabBarInput = state.LastTabInput
      override _.BreadcrumbInput = state.LastBreadcrumbInput
      override _.SplitOpen = state.SplitOpen
      override _.SplitMode = state.SplitMode
      override _.ActiveSubTab = state.ActiveSubTab
      override _.TabCount = state.Tabs.Length
      override _.PinChecked = state.PinChecked
      override _.ShuffleChecked = state.ShuffleChecked
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.SetSplitOpen(isOpen) =
        state.SplitOpen <- isOpen
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.SetSplitMode(mode) =
        state.SplitMode <- mode
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, factories)
  }

  self <- ValueSome cell
  cell
