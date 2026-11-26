namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroHubSection = {
  Id: string
  Header: string
  Width: float
}

[<Struct; NoComparison>]
type MetroHubInput = {
  Title: string
  Sections: MetroHubSection array
  ParallaxFactor: float
  SectionGap: float
  AccessibilityName: string voption
}

module HubInternals =

  let zeroPoint = Point(X = 0.0, Y = 0.0)

  let resolveParallax(factor: float) : float =
    if Double.IsFinite factor && factor >= 0.0 then
      factor
    else
      0.35

  let resolveGap(gap: float) : float =
    if Double.IsFinite gap && gap >= 0.0 then gap else 32.0

  let resolveSectionWidth(width: float) : float =
    if Double.IsFinite width && width > 0.0 then
      width
    else
      320.0

[<AbstractClass>]
type MetroHubCell() =
  inherit Cell<MetroHubInput>()

  abstract ScrollHandle: ElementHandle

  abstract HeaderTranslate: Anim<Point>

[<RequireQualifiedAccess>]
module MetroHub =

  let defaults(title: string, sections: MetroHubSection array) : MetroHubInput = {
    Title = title
    Sections = sections
    ParallaxFactor = 0.35
    SectionGap = 32.0
    AccessibilityName = ValueNone
  }

  /// State of a Hub cell. One mutable field per concern.
  type HubState = {
    ScrollHandle: ElementHandle
    mutable AttachedHost: Window voption
    mutable Subscribed: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable ActiveParallax: float
    // A slot for the animation value. `create` fills it after the cell exists.
    mutable HeaderTranslate: Anim<Point>
  }

  /// The values that the functions of a Hub cell work on.
  type HubContext = {
    State: HubState
    RenderSection: string -> Blob
    /// One delegate for the life of the cell. The handle subscribes to it.
    OnMetricsChanged: Action<ElementMetrics>
  }

  let initialState() : HubState = {
    ScrollHandle = ElementHandle()
    AttachedHost = ValueNone
    Subscribed = false
    LastReducedMotion = false
    ScopedReducedMotion = false
    ActiveParallax = 0.35
    HeaderTranslate = Unchecked.defaultof<Anim<Point>>
  }

  /// <c>true</c> when a scope or the host window asks for reduced motion.
  let isReducedMotion(state: HubState) : bool =
    state.ScopedReducedMotion
    || (state.AttachedHost
        |> ValueOption.map(fun w -> Metro.readWindow(w).ReducedMotion)
        |> ValueOption.defaultValue state.LastReducedMotion)

  /// Moves the header against the scroll offset. The animation is
  /// paint-bound, thus the scroll causes no cell rebuild.
  let updateParallax(state: HubState, scrollX: float) : unit =
    if isReducedMotion state then
      state.HeaderTranslate.Snap(HubInternals.zeroPoint)
    else
      let x =
        if Double.IsFinite scrollX then
          -scrollX * state.ActiveParallax
        else
          0.0

      state.HeaderTranslate.Snap(Point(X = x, Y = 0.0))

  /// Subscribes the handle to its scroll metrics when the cell mounts.
  let mounted(ctx: HubContext, host: Window voption) : unit =
    let state = ctx.State
    state.AttachedHost <- host

    if not state.Subscribed then
      state.ScrollHandle.add_MetricsChanged ctx.OnMetricsChanged
      state.Subscribed <- true

    updateParallax(state, state.ScrollHandle.ScrollOffset.X)

  /// Unsubscribes the handle and puts the header at rest.
  let unmounting(ctx: HubContext) : unit =
    let state = ctx.State

    if state.Subscribed then
      state.ScrollHandle.remove_MetricsChanged ctx.OnMetricsChanged
      state.Subscribed <- false

    state.AttachedHost <- ValueNone
    state.ScopedReducedMotion <- false
    state.HeaderTranslate.Snap(HubInternals.zeroPoint)

  let buildTree
    (ctx: HubContext, input: MetroHubInput, theme: MetroTheme)
    : Blob =
    let titleText = if isNull input.Title then "" else input.Title

    let sections =
      if isNull input.Sections then
        Array.empty
      else
        input.Sections

    let sectionGap = HubInternals.resolveGap input.SectionGap

    let accName = input.AccessibilityName |> ValueOption.defaultValue titleText

    let headerLayer =
      Container(
        FlexDirection = FlexDirection.Row,
        FlexShrink = 0.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        Padding =
          Edges(
            Length.Pixels 0.0,
            Length.Pixels 0.0,
            Length.Pixels 12.0,
            Length.Pixels 0.0
          )
      )
        .Children(
          Container(TranslateAnim = ctx.State.HeaderTranslate, FlexShrink = 0.0)
            .Children(
              MetroText.build {
                MetroText.defaults(titleText, MetroTextRole.Hero) with
                    Wrap = TextWrap.NoWrap
              }
            )
        )

    let viewport =
      Container(
        Handle = ctx.State.ScrollHandle,
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.NoWrap,
        AlignItems = AlignItems.Stretch,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Gap = Length.Pixels sectionGap,
        OverflowX = Overflow.Scroll,
        OverflowY = Overflow.Hidden,
        ScrollbarVisibilityX = ScrollbarVisibility.Auto,
        Padding =
          Edges(
            Length.Pixels 0.0,
            Length.Pixels 0.0,
            Length.Pixels 8.0,
            Length.Pixels 0.0
          ),
        PaddingEnd = Length.Pixels 48.0
      )

    for i = 0 to sections.Length - 1 do
      let section = sections[i]
      let secWidth = HubInternals.resolveSectionWidth section.Width
      let secHeader = if isNull section.Header then "" else section.Header

      let sectionNode =
        Container(
          Key = $"hub-section-{section.Id}",
          FlexDirection = FlexDirection.Column,
          FlexShrink = 0.0,
          Width = Length.Pixels secWidth,
          Gap = Length.Pixels 12.0
        )
          .Children(MetroText.header secHeader, ctx.RenderSection section.Id)

      viewport.Add(sectionNode)

    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      Width = Length.Percent 100.0,
      Overflow = Overflow.Hidden,
      Accessibility =
        Accessibility(Role = AccessibilityRole.Group, Name = accName)
    )
      .Children(headerLayer, viewport)

  /// Reads the theme and the parallax factor, then builds the tree.
  let build
    (ctx: HubContext, input: MetroHubInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion
    state.ActiveParallax <- HubInternals.resolveParallax input.ParallaxFactor

    if theme.ReducedMotion then
      state.HeaderTranslate.Snap(HubInternals.zeroPoint)

    buildTree(ctx, input, theme)

  /// The reduced motion preference of the host window. <c>ValueNone</c> when
  /// the cell has no window.
  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  /// The window of a cell as a value option.
  let hostOf(host: Window) : Window voption =
    if isNull host then ValueNone else ValueSome host

  let create(renderSection: string -> Blob) : MetroHubCell =
    let state = initialState()

    let onMetricsChanged =
      Action<ElementMetrics>(fun metrics ->
        updateParallax(state, metrics.ScrollOffset.X))

    let context: HubContext = {
      State = state
      RenderSection = renderSection
      OnMetricsChanged = onMetricsChanged
    }

    let cell = {
      new MetroHubCell() with
        override _.ScrollHandle = state.ScrollHandle
        override _.HeaderTranslate = state.HeaderTranslate

        override this.Mounted() = mounted(context, hostOf this.Host)

        override _.Unmounting() = unmounting context

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    // The cell exists now: Goo ties the animation to it.
    state.HeaderTranslate <- cell.AnimateValue(HubInternals.zeroPoint)
    cell

  let createFactory(renderSection: string -> Blob) : unit -> MetroHubCell =
    fun () -> create renderSection

  let mount
    (key: string, input: MetroHubInput, factory: unit -> MetroHubCell)
    : Blob =
    Cell.Mount<MetroHubInput, MetroHubCell>(key, input, factory)
