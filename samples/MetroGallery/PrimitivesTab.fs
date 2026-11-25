module MetroGallery.PrimitivesTab

open Goo
open FunGoo.Children
open FunGoo.Metro

[<AbstractClass>]
type PrimitivesTabCell() =
  inherit Cell()

  abstract ExpanderOpen: bool
  abstract GridOrientation: MetroWrapOrientation
  abstract ViewboxStretch: MetroStretch
  abstract ViewboxCell: MetroViewboxCell voption
  abstract LastAction: string
  abstract Builds: int
  abstract SetExpanderOpen: expanded: bool -> unit
  abstract SetGridOrientation: orientation: MetroWrapOrientation -> unit
  abstract SetViewboxStretch: stretch: MetroStretch -> unit

module Assets =

  // Deterministic 48x48 geometric Metro bitmap for MetroImage showcase.
  let sampleBitmap: ImageSource =
    let w = 48
    let h = 48
    let px = Array.zeroCreate<byte>(w * h * 4)

    for y = 0 to h - 1 do
      for x = 0 to w - 1 do
        let idx = (y * w + x) * 4
        let quad = ((x / 12) + (y / 12)) % 3

        let r, g, b =
          match quad with
          | 0 -> 0uy, 120uy, 212uy
          | 1 -> 0uy, 171uy, 169uy
          | _ -> 216uy, 0uy, 115uy

        px[idx] <- r
        px[idx + 1] <- g
        px[idx + 2] <- b
        px[idx + 3] <- 255uy

    new ImageSource(w, h, px)

  let gridSpans: MetroGridSpan array = [|
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
  |]

  let gridColors: (string * Color) array = [|
    "2x2", Color.Rgb(0, 120, 212)
    "2x1", Color.Rgb(0, 163, 0)
    "1x1", Color.Rgb(240, 150, 9)
    "1x1", Color.Rgb(162, 0, 255)
    "1x2", Color.Rgb(216, 0, 115)
    "2x1", Color.Rgb(0, 171, 169)
    "1x1", Color.Rgb(100, 118, 135)
  |]

type PrimitivesState = {
  mutable ExpanderOpen: bool
  mutable GridOrientation: MetroWrapOrientation
  mutable ViewboxStretch: MetroStretch
  mutable LastAction: string
  mutable Builds: int
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

let chip(label: string) : Blob =
  let theme = Metro.read()

  MetroBorder.build(
    {
      MetroBorder.defaults() with
          Thickness = Edges(Length.Pixels 1.0)
          BorderColor = ValueSome theme.Palette.Border
          Background = ValueSome theme.Palette.BackgroundAlt
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 10.0)
    },
    MetroText.caption label
  )

let chipRounded(label: string) : Blob =
  let theme = Metro.read()

  MetroBorder.build(
    {
      MetroBorder.defaults() with
          Thickness = Edges(Length.Pixels 1.0)
          BorderColor = ValueSome theme.Palette.Border
          Background = ValueSome theme.Palette.BackgroundAlt
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 10.0)
          CornerRadius = 8.0
    },
    MetroText.caption label
  )

// The natural 200x48 stamp that both viewboxes scale. A plain function, so
// the responsive viewbox cell can rebuild it inside its own build.
// Typography alone carries the demo: no border or fill wrapper.
let viewboxStamp() : Blob =
  Container(
    Width = Length.Pixels 200.0,
    Height = Length.Pixels 48.0,
    AlignItems = AlignItems.Center,
    JustifyContent = JustifyContent.Center
  )
    .Children(
      MetroText.build {
        MetroText.defaults("METRO VIEWBOX", MetroTextRole.Subtitle) with
            Wrap = TextWrap.NoWrap
      }
    )
  :> Blob

let view
  (state: PrimitivesState, viewboxFactory: unit -> MetroViewboxCell)
  : Blob =
  let iconAndAvatarRow =
    Container(
      FlexDirection = FlexDirection.Row,
      AlignItems = AlignItems.Center,
      FlexWrap = FlexWrap.Wrap,
      Gap = Length.Pixels 14.0
    )
      .Children(
        MetroIcon.build(MetroIcon.sized("explore", MetroIconSize.Small)),
        MetroIcon.build(MetroIcon.sized("bolt", MetroIconSize.Normal)),
        MetroIcon.build {
          MetroIcon.sized("verified", MetroIconSize.Large) with
              Tone = MetroTextTone.Accent
        },
        MetroIcon.build(MetroIcon.sized("auto_awesome", MetroIconSize.XLarge)),
        MetroPersonPicture.build {
          MetroPersonPicture.defaults "Ada Lovelace" with
              AvatarSize = MetroAvatarSize.Small
              Presence = MetroPresence.Available
        },
        MetroPersonPicture.build {
          MetroPersonPicture.defaults "Grace Hopper" with
              AvatarSize = MetroAvatarSize.Normal
              Presence = MetroPresence.Busy
              AccentOverride = ValueSome(Color.Rgb(216, 0, 115))
        },
        MetroPersonPicture.build {
          MetroPersonPicture.defaults "Alan Turing" with
              AvatarSize = MetroAvatarSize.Large
              Presence = MetroPresence.Away
              AccentOverride = ValueSome(Color.Rgb(0, 163, 0))
        },
        MetroImage.build {
          MetroImage.defaults(
            ValueSome(Assets.sampleBitmap :> ImageSourceProvider)
          ) with
              Width = ValueSome(Length.Pixels 64.0)
              Height = ValueSome(Length.Pixels 64.0)
              Bordered = true
              AccessibilityLabel = ValueSome "Metro mosaic sample"
        },
        MetroImage.build {
          MetroImage.defaults ValueNone with
              Width = ValueSome(Length.Pixels 64.0)
              Height = ValueSome(Length.Pixels 64.0)
              Bordered = true
              Placeholder = ValueSome(MetroIcon.named "image")
              Fallback = ValueSome(MetroIcon.named "broken_image")
        }
      )

  let richBlock =
    MetroRichTextBlock.build(
      MetroRichTextBlock.defaults [|
        MetroRichTextBlock.bold "Fierce reduction: "
        MetroRichTextBlock.plain "combine "
        MetroRichTextBlock.italic "expressive typography"
        MetroRichTextBlock.plain ", "
        MetroRichTextBlock.accent "accent links"
        MetroRichTextBlock.plain ", and inline "
        MetroRichTextBlock.code "TextSpan[]"
        MetroRichTextBlock.plain " runs in one layout pass."
      |]
    )

  let expander =
    MetroExpander.build(
      MetroExpander.defaults(
        "Metro Design Metadata (Expander)",
        state.ExpanderOpen
      ),
      (fun next ->
        state.ExpanderOpen <- next
        state.LastAction <- $"Expander expanded: {next}"),
      MetroText.secondary
        "Sharp 0 px disclosure container preserving mounted child state while collapsed."
    )

  let primitivesCol =
    column(
      "primitives & rich text",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
        .Children(
          MetroText.caption "ICONS, PERSON PICTURES, & IMAGES",
          iconAndAvatarRow,
          MetroText.caption "RICH TEXT BLOCK",
          richBlock,
          MetroText.caption "EXPANDER",
          expander
        )
    )

  let gridItems =
    Assets.gridColors
    |> Array.mapi(fun idx (lbl, col) ->
      Container(
        Key = $"varg-tile-{idx}",
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        AlignItems = AlignItems.FlexStart,
        JustifyContent = JustifyContent.FlexEnd,
        Padding = Edges(Length.Pixels 8.0),
        BackgroundColor = col
      )
        .Children(
          MetroText.build {
            MetroText.defaults(lbl, MetroTextRole.Caption) with
                Tone = MetroTextTone.Custom(Color.Rgb(255, 255, 255))
          }
        )
      :> Blob)

  let gridArgs = {
    MetroVariableSizedWrapGrid.defaults(56.0, 48.0, Assets.gridSpans) with
        Orientation = state.GridOrientation
        Gap = 6.0
        MaximumRowsOrColumns =
          ValueSome(
            match state.GridOrientation with
            | MetroWrapOrientation.Horizontal -> 4
            | MetroWrapOrientation.Vertical -> 3
          )
  }

  let viewboxBox =
    MetroViewbox.build(
      {
        MetroViewbox.defaults(LayoutSize(260.0, 76.0), LayoutSize(200.0, 48.0)) with
            Stretch = state.ViewboxStretch
      },
      viewboxStamp()
    )

  let responsiveViewbox =
    MetroViewbox.mount(
      "primitives-viewbox-responsive",
      {
        MetroViewbox.inputDefaults(LayoutSize(200.0, 48.0)) with
            Stretch = state.ViewboxStretch
            Width = Length.Pixels 260.0
            Height = Length.Pixels 76.0
      },
      viewboxFactory
    )

  let layoutCol =
    column(
      "variable grid, wrap, & viewbox",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
        .Children(
          Container(
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Gap = Length.Pixels 8.0
          )
            .Children(
              MetroButton.standard(
                $"Grid: {state.GridOrientation}",
                fun () ->
                  state.GridOrientation <-
                    match state.GridOrientation with
                    | MetroWrapOrientation.Horizontal ->
                      MetroWrapOrientation.Vertical
                    | MetroWrapOrientation.Vertical ->
                      MetroWrapOrientation.Horizontal

                  state.LastAction <- $"Grid flow: {state.GridOrientation}"
              ),
              MetroButton.standard(
                "Uniform",
                fun () ->
                  state.ViewboxStretch <- MetroStretch.Uniform
                  state.LastAction <- "Viewbox: Uniform"
              ),
              MetroButton.standard(
                "Fill",
                fun () ->
                  state.ViewboxStretch <- MetroStretch.Fill
                  state.LastAction <- "Viewbox: Fill"
              ),
              MetroButton.standard(
                "None",
                fun () ->
                  state.ViewboxStretch <- MetroStretch.None
                  state.LastAction <- "Viewbox: None"
              )
            ),
          MetroVariableSizedWrapGrid.build(gridArgs, gridItems),
          MetroText.caption "WRAP PANEL",
          MetroWrapPanel.build(
            MetroWrapPanel.defaults(),
            [|
              chip " SHARP 0 PX "
              chip " TYPOGRAPHY "
              chip " MOTION "
              chip " CONTENT OVER CHROME "
              chipRounded " ROUNDED 8 PX "
            |]
          ),
          MetroText.caption $"VIEWBOX ({state.ViewboxStretch})",
          Container(FlexDirection = FlexDirection.Row).Children(viewboxBox),
          MetroText.caption "VIEWBOX (RESPONSIVE)",
          Container(FlexDirection = FlexDirection.Row)
            .Children(responsiveViewbox)
        )
    )

  MetroScrollViewer.build(
    MetroScrollViewer.vertical(),
    Container(
      FlexDirection = FlexDirection.Column,
      Width = Length.Percent 100.0,
      Gap = Length.Pixels 16.0
    )
      .Children(
        Container(
          FlexDirection = FlexDirection.Row,
          Width = Length.Percent 100.0,
          FlexShrink = 0.0,
          Gap = Length.Pixels 32.0
        )
          .Children(primitivesCol, layoutCol),
        MetroText.caption state.LastAction
      )
  )

let create() : PrimitivesTabCell =
  let state: PrimitivesState = {
    ExpanderOpen = true
    GridOrientation = MetroWrapOrientation.Horizontal
    ViewboxStretch = MetroStretch.Uniform
    LastAction = "Ready"
    Builds = 0
  }

  let mutable self: PrimitivesTabCell voption = ValueNone
  let mutable viewboxRef: MetroViewboxCell voption = ValueNone

  // Created once: every Build reuses the same factory.
  let viewboxFactory() : MetroViewboxCell =
    let cell = MetroViewbox.create viewboxStamp
    viewboxRef <- ValueSome cell
    cell

  let cell = {
    new PrimitivesTabCell() with
      override _.ExpanderOpen = state.ExpanderOpen
      override _.GridOrientation = state.GridOrientation
      override _.ViewboxStretch = state.ViewboxStretch
      override _.ViewboxCell = viewboxRef
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.SetExpanderOpen(expanded) =
        state.ExpanderOpen <- expanded
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.SetGridOrientation(orientation) =
        state.GridOrientation <- orientation
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.SetViewboxStretch(stretch) =
        state.ViewboxStretch <- stretch
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, viewboxFactory)
  }

  self <- ValueSome cell
  cell
