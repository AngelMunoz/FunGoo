namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroPanoramaSection = {
  Id: string
  Header: string
  Width: float
}

[<Struct; NoComparison>]
type MetroPanoramaInput = {
  Title: string
  Sections: MetroPanoramaSection array
  BackgroundParallax: float
  TitleParallax: float
  SectionGap: float
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroPanoramaCell() =
  inherit Cell<MetroPanoramaInput>()

  abstract ScrollHandle: ElementHandle

  abstract BackgroundTranslate: Anim<Point>

  abstract TitleTranslate: Anim<Point>

module PanoramaInternals =

  let zeroPoint = Point(X = 0.0, Y = 0.0)

  let resolveRatio(ratio: float, fallback: float) : float =
    if Double.IsFinite ratio && ratio >= 0.0 then
      ratio
    else
      fallback

  let resolveGap(gap: float) : float =
    if Double.IsFinite gap && gap >= 0.0 then gap else 28.0

  let resolveSectionWidth(width: float) : float =
    if Double.IsFinite width && width > 0.0 then
      width
    else
      360.0

[<RequireQualifiedAccess>]
module MetroPanorama =

  let section(id: string, header: string) : MetroPanoramaSection = {
    Id = id
    Header = header
    Width = 360.0
  }

  let sizedSection
    (id: string, header: string, width: float)
    : MetroPanoramaSection = {
    Id = id
    Header = header
    Width = width
  }

  let defaults
    (title: string, sections: MetroPanoramaSection array)
    : MetroPanoramaInput = {
    Title = title
    Sections = sections
    BackgroundParallax = 0.15
    TitleParallax = 0.35
    SectionGap = 28.0
    AccessibilityName = ValueNone
  }

  type PanoramaState = {
    ScrollHandle: ElementHandle
    mutable Subscribed: bool
    mutable HostReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable ActiveBgRatio: float
    mutable ActiveTitleRatio: float
    mutable BackgroundTranslate: Anim<Point>
    mutable TitleTranslate: Anim<Point>
  }

  type PanoramaContext = {
    State: PanoramaState
    RenderBackground: (unit -> Blob) voption
    RenderSection: string -> Blob
    OnMetricsChanged: Action<ElementMetrics>
    OnWheel: Action<WheelEvent>
  }

  let isReducedMotion(state: PanoramaState) : bool =
    state.ScopedReducedMotion || state.HostReducedMotion

  let updateParallax(state: PanoramaState, scrollX: float) : unit =
    if isReducedMotion state || not(Double.IsFinite scrollX) then
      state.BackgroundTranslate.Snap(PanoramaInternals.zeroPoint)
      state.TitleTranslate.Snap(PanoramaInternals.zeroPoint)
    else
      state.BackgroundTranslate.Snap(
        Point(X = -scrollX * state.ActiveBgRatio, Y = 0.0)
      )

      state.TitleTranslate.Snap(
        Point(X = -scrollX * state.ActiveTitleRatio, Y = 0.0)
      )

  let wheelPan(state: PanoramaState, e: WheelEvent) : unit =
    let rangeX = state.ScrollHandle.ScrollRange.X

    if rangeX > 0.0 && abs e.Delta.Y > abs e.Delta.X then
      let curX = state.ScrollHandle.ScrollOffset.X
      let nextX = Math.Clamp(curX - e.Delta.Y * 48.0, 0.0, rangeX)
      state.ScrollHandle.JumpTo(x = Nullable nextX) |> ignore
      e.PreventDefault()

  let mounted(ctx: PanoramaContext) : unit =
    let state = ctx.State

    if not state.Subscribed then
      state.ScrollHandle.add_MetricsChanged ctx.OnMetricsChanged
      state.Subscribed <- true

    updateParallax(state, state.ScrollHandle.ScrollOffset.X)

  let unmounting(ctx: PanoramaContext) : unit =
    let state = ctx.State

    if state.Subscribed then
      state.ScrollHandle.remove_MetricsChanged ctx.OnMetricsChanged
      state.Subscribed <- false

    state.ScopedReducedMotion <- false
    state.BackgroundTranslate.Snap(PanoramaInternals.zeroPoint)
    state.TitleTranslate.Snap(PanoramaInternals.zeroPoint)

  let buildTree(ctx: PanoramaContext, input: MetroPanoramaInput) : Blob =
    let state = ctx.State
    let titleText = if isNull input.Title then "" else input.Title

    let sections =
      if isNull input.Sections then
        Array.empty
      else
        input.Sections

    let sectionGap = PanoramaInternals.resolveGap input.SectionGap
    let accName = input.AccessibilityName |> ValueOption.defaultValue titleText

    let root =
      Container(
        Key = "metro-panorama-root",
        Position = PositionType.Relative,
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Group, Name = accName)
      )

    ctx.RenderBackground
    |> ValueOption.iter(fun buildBg ->
      let bg = buildBg()

      if not(isNull bg) then
        let bgLayer =
          Container(
            Key = "metro-panorama-bg",
            Position = PositionType.Absolute,
            Top = Length.Pixels 0.0,
            Bottom = Length.Pixels 0.0,
            Left = Length.Pixels 0.0,
            Width = Length.Percent 140.0,
            Opacity = 0.18,
            HitTestSelf = false,
            TranslateAnim = state.BackgroundTranslate
          )
            .Children(bg)

        root.Add(bgLayer))

    let titleLayer =
      Container(
        Key = "metro-panorama-title-layer",
        FlexDirection = FlexDirection.Row,
        FlexShrink = 0.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        Padding =
          Edges(
            Length.Pixels 8.0,
            Length.Pixels 16.0,
            Length.Pixels 12.0,
            Length.Pixels 16.0
          )
      )
        .Children(
          Container(TranslateAnim = state.TitleTranslate, FlexShrink = 0.0)
            .Children(
              MetroText.build {
                MetroText.defaults(titleText, MetroTextRole.Hero) with
                    Wrap = TextWrap.NoWrap
              }
            )
        )

    let viewport =
      Container(
        Key = "metro-panorama-viewport",
        Handle = state.ScrollHandle,
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.NoWrap,
        AlignItems = AlignItems.Stretch,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Gap = Length.Pixels sectionGap,
        OverflowX = Overflow.Scroll,
        OverflowY = Overflow.Hidden,
        ScrollbarVisibilityX = ScrollbarVisibility.Hidden,
        OnWheel = ctx.OnWheel,
        Padding =
          Edges(
            Length.Pixels 0.0,
            Length.Pixels 64.0,
            Length.Pixels 8.0,
            Length.Pixels 16.0
          )
      )

    for i = 0 to sections.Length - 1 do
      let sec = sections[i]
      let secW = PanoramaInternals.resolveSectionWidth sec.Width
      let secHeader = if isNull sec.Header then "" else sec.Header

      let secNode =
        Container(
          Key = $"panorama-section-{sec.Id}",
          FlexDirection = FlexDirection.Column,
          FlexShrink = 0.0,
          Width = Length.Pixels secW,
          Gap = Length.Pixels 12.0
        )

      secNode.Add(MetroText.header secHeader)

      let section = ctx.RenderSection sec.Id

      if not(isNull section) then
        secNode.Add(section)

      viewport.Add(secNode)

    root.Children(titleLayer, viewport) :> Blob

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let createInternal
    (renderBackground: (unit -> Blob) voption, renderSection: string -> Blob)
    : MetroPanoramaCell =
    let state: PanoramaState = {
      ScrollHandle = ElementHandle()
      Subscribed = false
      HostReducedMotion = false
      ScopedReducedMotion = false
      ActiveBgRatio = 0.15
      ActiveTitleRatio = 0.35
      BackgroundTranslate = Unchecked.defaultof<Anim<Point>>
      TitleTranslate = Unchecked.defaultof<Anim<Point>>
    }

    let ctx: PanoramaContext = {
      State = state
      RenderBackground = renderBackground
      RenderSection = renderSection
      OnMetricsChanged =
        Action<ElementMetrics>(fun metrics ->
          updateParallax(state, metrics.ScrollOffset.X))
      OnWheel = Action<WheelEvent>(fun e -> wheelPan(state, e))
    }

    let cell = {
      new MetroPanoramaCell() with
        override _.ScrollHandle = state.ScrollHandle
        override _.BackgroundTranslate = state.BackgroundTranslate
        override _.TitleTranslate = state.TitleTranslate
        override _.Mounted() = mounted ctx
        override _.Unmounting() = unmounting ctx

        override this.Build(input) : Blob =
          let theme = Metro.read()

          let windowReduced =
            ValueOption.defaultValue false (reducedMotionOf this.Host)

          state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
          state.HostReducedMotion <- windowReduced

          state.ActiveBgRatio <-
            PanoramaInternals.resolveRatio(input.BackgroundParallax, 0.15)

          state.ActiveTitleRatio <-
            PanoramaInternals.resolveRatio(input.TitleParallax, 0.35)

          if theme.ReducedMotion then
            state.BackgroundTranslate.Snap(PanoramaInternals.zeroPoint)
            state.TitleTranslate.Snap(PanoramaInternals.zeroPoint)
          else
            updateParallax(state, state.ScrollHandle.ScrollOffset.X)

          buildTree(ctx, input)
    }

    state.BackgroundTranslate <- cell.AnimateValue(PanoramaInternals.zeroPoint)
    state.TitleTranslate <- cell.AnimateValue(PanoramaInternals.zeroPoint)
    cell

  let create(renderSection: string -> Blob) : MetroPanoramaCell =
    createInternal(ValueNone, renderSection)

  let createWithBackground
    (renderBackground: unit -> Blob, renderSection: string -> Blob)
    : MetroPanoramaCell =
    createInternal(ValueSome renderBackground, renderSection)

  let createFactory(renderSection: string -> Blob) : unit -> MetroPanoramaCell =
    fun () -> create renderSection

  let mount
    (key: string, input: MetroPanoramaInput, factory: unit -> MetroPanoramaCell)
    : Blob =
    Cell.Mount<MetroPanoramaInput, MetroPanoramaCell>(key, input, factory)
