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

module private HubInternals =

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

[<Sealed>]
type MetroHubCell(renderSection: string -> Blob) as this =
  inherit Cell<MetroHubInput>()

  let scrollHandle = ElementHandle()
  let headerTranslate = this.AnimateValue(HubInternals.zeroPoint)

  let mutable attachedHost: Window voption = ValueNone
  let mutable subscribed = false
  let mutable lastReducedMotion = false
  let mutable scopedReducedMotion = false
  let mutable activeParallax = 0.35

  let isReducedMotion() =
    scopedReducedMotion
    || (attachedHost
        |> ValueOption.map(fun w -> Metro.readWindow(w).ReducedMotion)
        |> ValueOption.defaultValue lastReducedMotion)

  let updateParallax(scrollX: float) =
    if isReducedMotion() then
      headerTranslate.Snap(HubInternals.zeroPoint)
    else
      let x =
        if Double.IsFinite scrollX then
          -scrollX * activeParallax
        else
          0.0

      headerTranslate.Snap(Point(X = x, Y = 0.0))

  let onMetricsChanged =
    Action<ElementMetrics>(fun metrics -> updateParallax metrics.ScrollOffset.X)

  member _.ScrollHandle = scrollHandle
  member _.HeaderTranslate = headerTranslate

  override this.Mounted() =
    attachedHost <- if isNull this.Host then ValueNone else ValueSome this.Host

    if not subscribed then
      scrollHandle.add_MetricsChanged onMetricsChanged
      subscribed <- true

    updateParallax scrollHandle.ScrollOffset.X

  override _.Unmounting() =
    if subscribed then
      scrollHandle.remove_MetricsChanged onMetricsChanged
      subscribed <- false

    attachedHost <- ValueNone
    scopedReducedMotion <- false
    headerTranslate.Snap(HubInternals.zeroPoint)

  override this.Build(input: MetroHubInput) : Blob =
    let theme = Metro.read()

    let windowReduced =
      if not(isNull this.Host) then
        Metro.readWindow(this.Host).ReducedMotion
      else
        false

    scopedReducedMotion <- theme.ReducedMotion && not windowReduced
    lastReducedMotion <- theme.ReducedMotion
    activeParallax <- HubInternals.resolveParallax input.ParallaxFactor

    if theme.ReducedMotion then
      headerTranslate.Snap(HubInternals.zeroPoint)

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
          Container(TranslateAnim = headerTranslate, FlexShrink = 0.0)
            .Children(
              MetroText.build {
                MetroText.defaults(titleText, MetroTextRole.Hero) with
                    Wrap = TextWrap.NoWrap
              }
            )
        )

    let viewport =
      Container(
        Handle = scrollHandle,
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
            Length.Pixels 48.0,
            Length.Pixels 8.0,
            Length.Pixels 0.0
          )
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
          .Children(MetroText.header secHeader, renderSection section.Id)

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

[<RequireQualifiedAccess>]
module MetroHub =

  let defaults(title: string, sections: MetroHubSection array) : MetroHubInput = {
    Title = title
    Sections = sections
    ParallaxFactor = 0.35
    SectionGap = 32.0
    AccessibilityName = ValueNone
  }

  let createFactory(renderSection: string -> Blob) : Func<MetroHubCell> =
    Func<MetroHubCell>(fun () -> MetroHubCell(renderSection))

  let mount
    (key: string, input: MetroHubInput, factory: Func<MetroHubCell>)
    : Blob =
    Cell.Mount<MetroHubInput, MetroHubCell>(key, input, factory)
