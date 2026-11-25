module FunGoo.Metro.Tests.PrimitivesAndLayoutTests

open System
open System.Diagnostics
open System.Reflection
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

// Packs the way `MetroVariableSizedWrapGrid.pack` packs, but the scan of
// each item starts at row 0 and no start hint is used. It is the reference
// for the scan start of `pack` and reads only the public arguments and
// types of `pack`.
let private referencePack
  (args: MetroVariableGridArgs, count: int, available: LayoutSize)
  : struct (MetroVariableSizedWrapGrid.PlacedCell array * int * int) =
  if count <= 0 then
    struct (Array.empty, 0, 0)
  else
    let cellW =
      if Double.IsFinite args.ItemWidth && args.ItemWidth > 0.0 then
        args.ItemWidth
      else
        64.0

    let cellH =
      if Double.IsFinite args.ItemHeight && args.ItemHeight > 0.0 then
        args.ItemHeight
      else
        64.0

    let gap =
      if Double.IsFinite args.Gap && args.Gap >= 0.0 then
        args.Gap
      else
        8.0

    let spans = if isNull args.Spans then Array.empty else args.Spans
    let isHoriz = args.Orientation = MetroWrapOrientation.Horizontal

    let availLimit =
      if isHoriz then
        if Double.IsFinite available.Width && available.Width > 0.0 then
          max 1 (int(Math.Floor((available.Width + gap) / (cellW + gap))))
        else
          4
      elif Double.IsFinite available.Height && available.Height > 0.0 then
        max 1 (int(Math.Floor((available.Height + gap) / (cellH + gap))))
      else
        4

    let primaryLimit =
      args.MaximumRowsOrColumns
      |> ValueOption.bind(fun m ->
        if m >= 1 then ValueSome(min m 1024) else ValueNone)
      |> ValueOption.map(fun m ->
        if
          (isHoriz && Double.IsFinite available.Width && available.Width > 0.0)
          || (not isHoriz
              && Double.IsFinite available.Height
              && available.Height > 0.0)
        then
          min m availLimit
        else
          m)
      |> ValueOption.defaultValue availLimit

    let mutable rows = 1

    for i = 0 to count - 1 do
      let raw =
        if i < spans.Length then
          spans[i]
        else
          { ColSpan = 1; RowSpan = 1 }

      let span = if isHoriz then raw.RowSpan else raw.ColSpan
      rows <- rows + (min 1024 (max 1 span))

    let buffer = Array.zeroCreate<bool>(primaryLimit * rows)
    let placed = Array.zeroCreate<MetroVariableSizedWrapGrid.PlacedCell> count
    let mutable maxCol = 0
    let mutable maxRow = 0

    for i = 0 to count - 1 do
      let raw =
        if i < spans.Length then
          spans[i]
        else
          { ColSpan = 1; RowSpan = 1 }

      let cs = min 1024 (max 1 raw.ColSpan)
      let rs = min 1024 (max 1 raw.RowSpan)

      let priSpan, secSpan =
        if isHoriz then
          min primaryLimit cs, rs
        else
          min primaryLimit rs, cs

      let canFit(sec: int, pri: int) : bool =
        let mutable ok = true
        let mutable s = sec

        while ok && s < sec + secSpan do
          let mutable p = pri

          while ok && p < pri + priSpan do
            if buffer[s * primaryLimit + p] then
              ok <- false

            p <- p + 1

          s <- s + 1

        ok

      let mutable sec = 0
      let mutable chosenPri = -1

      while chosenPri < 0 do
        let mutable p = 0

        while chosenPri < 0 && p <= primaryLimit - priSpan do
          if canFit(sec, p) then
            chosenPri <- p

          p <- p + 1

        if chosenPri < 0 then
          sec <- sec + 1

      for s = sec to sec + secSpan - 1 do
        for p = chosenPri to chosenPri + priSpan - 1 do
          buffer[s * primaryLimit + p] <- true

      let col, row, finalCs, finalRs =
        if isHoriz then
          chosenPri, sec, priSpan, secSpan
        else
          sec, chosenPri, secSpan, priSpan

      placed[i] <- {
        Col = col
        Row = row
        ColSpan = finalCs
        RowSpan = finalRs
      }

      if col + finalCs > maxCol then
        maxCol <- col + finalCs

      if row + finalRs > maxRow then
        maxRow <- row + finalRs

    struct (placed, maxCol, maxRow)

// Compares the cells of `pack` with the cells of `referencePack` and fails
// with the first difference.
let private assertSamePlacement
  (args: MetroVariableGridArgs, count: int, available: LayoutSize)
  : unit =
  let struct (placed, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(args, count, available)

  let struct (expected, expCols, expRows) =
    referencePack(args, count, available)

  if usedCols <> expCols || usedRows <> expRows then
    failwith
      $"Expected {expCols} used columns and {expRows} used rows, got {usedCols} and {usedRows}."

  if placed.Length <> expected.Length then
    failwith $"Expected {expected.Length} placed cells, got {placed.Length}."

  for i = 0 to expected.Length - 1 do
    let cell = placed[i]
    let want = expected[i]

    if
      cell.Col <> want.Col
      || cell.Row <> want.Row
      || cell.ColSpan <> want.ColSpan
      || cell.RowSpan <> want.RowSpan
    then
      failwith
        $"Item {i}: expected col {want.Col}, row {want.Row}, span {want.ColSpan}x{want.RowSpan}, got col {cell.Col}, row {cell.Row}, span {cell.ColSpan}x{cell.RowSpan}."

[<Fact>]
let ``MetroPersonPicture initialsOf extracts uppercase initials cleanly``() =
  Assert.Equal("AL", MetroPersonPicture.initialsOf "Ada Lovelace")
  Assert.Equal("GH", MetroPersonPicture.initialsOf "  grace   hopper  ")
  Assert.Equal("J", MetroPersonPicture.initialsOf "jean")
  Assert.Equal("MS", MetroPersonPicture.initialsOf "Mary-Jane Watson Smith")
  Assert.Equal("", MetroPersonPicture.initialsOf "   ")
  Assert.Equal("", MetroPersonPicture.initialsOf null)


let private flags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

let private entriesMethod = typeof<Style>.GetMethod("Entries", flags)

type private StyleSample = { FieldName: string; A: float32 }

let private readEntries(style: Style) : StyleSample array =
  if isNull style then
    Array.empty
  else
    let entries = entriesMethod.Invoke(style, null)

    if isNull entries then
      Array.empty
    else
      let entriesType = entries.GetType()

      let count =
        entriesType.GetProperty("Count", flags).GetValue(entries) :?> int

      let atMethod = entriesType.GetMethod("At", flags)

      Array.init count (fun i ->
        let e = atMethod.Invoke(entries, [| box i |])
        let et = e.GetType()

        {
          FieldName = et.GetField("Field", flags).GetValue(e).ToString()
          A = et.GetField("A", flags).GetValue(e) :?> float32
        })

let private tryLastEntry
  (style: Style, fieldName: string)
  : StyleSample voption =
  let entries = readEntries style
  let mutable found = ValueNone

  for i = entries.Length - 1 downto 0 do
    if found.IsNone && entries[i].FieldName = fieldName then
      found <- ValueSome entries[i]

  found

let private requireEntry(style: Style, fieldName: string) : StyleSample =
  match tryLastEntry(style, fieldName) with
  | ValueSome s -> s
  | ValueNone -> failwith $"Expected style entry '{fieldName}' to be present."

[<Fact>]
let ``MetroPrimitives build Icon, Border, Expander, Image fallback, PersonPicture, and RichTextBlock without errors``
  ()
  =
  use host = createHost(600, 500)
  let mutable expanded = false

  let root =
    CaptureCell.create(fun () ->
      let exp =
        MetroExpander.build(
          MetroExpander.defaults("Details", expanded),
          (fun next -> expanded <- next),
          MetroText.body "Hidden body"
        )

      let rich =
        MetroRichTextBlock.build {
          MetroRichTextBlock.defaults [|
            MetroRichTextBlock.bold "Bold "
            MetroRichTextBlock.italic "Italic "
            MetroRichTextBlock.accent "Link "
            MetroRichTextBlock.code "let x = 1"
          |] with
              Selectable = true
              MaxLines = ValueSome 2
              TrimEllipsis = true
        }

      let fallbackImg =
        MetroImage.buildWithFallback(
          {
            MetroImage.defaults ValueNone with
                Width = ValueSome(Length.Pixels 48.0)
                Height = ValueSome(Length.Pixels 48.0)
                Bordered = true
          },
          ValueNone,
          ValueSome(
            Container(Key = "img-fallback-slot", Width = Length.Pixels 16.0)
            :> Blob
          )
        )

      MetroBorder.build(
        MetroBorder.defaults(),
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 8.0
        )
          .Children(
            MetroIcon.sized("bolt", MetroIconSize.Large) |> MetroIcon.build,
            MetroPersonPicture.build {
              MetroPersonPicture.defaults "Ada Lovelace" with
                  Presence = MetroPresence.Available
            },
            rich,
            fallbackImg,
            exp
          )
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  Assert.True(findBlobByKey(root.Captured, "img-fallback-slot").IsSome)

  let headerBtn = requireBlobByKey(root.Captured, "header") :?> Button
  headerBtn.OnClick.Invoke()
  Assert.True expanded

[<Fact>]
let ``MetroVariableSizedWrapGrid packs multi-span cells in Horizontal and Vertical orientations``
  ()
  =
  use host = createHost(500, 500)
  let h0 = ElementHandle()
  let h1 = ElementHandle()
  let h2 = ElementHandle()
  let mutable orientation = MetroWrapOrientation.Horizontal

  let spans = [|
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
  |]

  let root =
    CaptureCell.create(fun () ->
      let args = {
        MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spans) with
            Orientation = orientation
            Gap = 10.0
            MaximumRowsOrColumns = ValueSome 4
      }

      MetroVariableSizedWrapGrid.build(
        args,
        [|
          Container(Handle = h0) :> Blob
          Container(Handle = h1) :> Blob
          Container(Handle = h2) :> Blob
        |]
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // Horizontal 4-column grid (50x40 cells, 10 px gap):
  // Item 0 (2x2) -> (0, 0) size 110x90
  // Item 1 (2x1) -> col 2, row 0 -> X = 2 * 60 = 120, Y = 0, size 110x40
  // Item 2 (1x1) -> col 2, row 1 -> X = 120, Y = 1 * 50 = 50, size 50x40
  Assert.Equal(0.0, h0.BorderBox.X, 1)
  Assert.Equal(0.0, h0.BorderBox.Y, 1)
  Assert.Equal(110.0, h0.BorderBox.Width, 1)
  Assert.Equal(90.0, h0.BorderBox.Height, 1)

  Assert.Equal(120.0, h1.BorderBox.X, 1)
  Assert.Equal(0.0, h1.BorderBox.Y, 1)
  Assert.Equal(110.0, h1.BorderBox.Width, 1)
  Assert.Equal(40.0, h1.BorderBox.Height, 1)

  Assert.Equal(120.0, h2.BorderBox.X, 1)
  Assert.Equal(50.0, h2.BorderBox.Y, 1)

  // Switch to Vertical 4-row column-first Start-screen packing:
  // Item 0 (2x2) -> col 0, row 0
  // Item 1 (2x1) -> col 0, row 2 -> X = 0, Y = 2 * 50 = 100
  // Item 2 (1x1) -> col 0, row 3 -> X = 0, Y = 3 * 50 = 150
  orientation <- MetroWrapOrientation.Vertical
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.Equal(0.0, h1.BorderBox.X, 1)
  Assert.Equal(100.0, h1.BorderBox.Y, 1)
  Assert.Equal(0.0, h2.BorderBox.X, 1)
  Assert.Equal(150.0, h2.BorderBox.Y, 1)

[<Fact>]
let ``MetroViewbox computeScale honors Stretch and StretchDirection modes and MetroViewboxCell measures viewport``
  ()
  =
  let baseProps =
    MetroViewbox.defaults(LayoutSize(200.0, 100.0), LayoutSize(100.0, 100.0))

  let struct (ux, uy) = MetroViewbox.computeScale baseProps
  Assert.Equal(1.0, ux, 3)
  Assert.Equal(1.0, uy, 3)

  let struct (fx, fy) =
    MetroViewbox.computeScale {
      baseProps with
          Stretch = MetroStretch.Fill
    }

  Assert.Equal(2.0, fx, 3)
  Assert.Equal(1.0, fy, 3)

  let struct (ufx, ufy) =
    MetroViewbox.computeScale {
      baseProps with
          Stretch = MetroStretch.UniformToFill
    }

  Assert.Equal(2.0, ufx, 3)
  Assert.Equal(2.0, ufy, 3)

  let struct (dx, dy) =
    MetroViewbox.computeScale {
      baseProps with
          Stretch = MetroStretch.UniformToFill
          StretchDirection = MetroStretchDirection.DownOnly
    }

  Assert.Equal(1.0, dx, 3)
  Assert.Equal(1.0, dy, 3)

  use host = createHost(300, 150)
  let mutable vbRef: MetroViewboxCell voption = ValueNone

  let factory() =
    let c =
      MetroViewbox.create(fun () ->
        Container(Width = Length.Pixels 100.0, Height = Length.Pixels 50.0)
        :> Blob)

    vbRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroViewbox.mount(
        "vb",
        MetroViewbox.inputDefaults(LayoutSize(100.0, 50.0)),
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 4)

  let vb =
    match vbRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroViewboxCell."

  Assert.Equal(3.0, vb.ScaleX, 2)
  Assert.Equal(3.0, vb.ScaleY, 2)

  // Resize the host. The viewport handle reports the new 600x300 box through
  // MetricsChanged, and the cell rebuilds. Uniform: min(600/100, 300/50) =
  // 6.0.
  host.Resize(600, 300, 600, 300)
  pumpFrames(host, 4)

  Assert.Equal(6.0, vb.ScaleX, 2)
  Assert.Equal(6.0, vb.ScaleY, 2)

[<Fact>]
let ``non-finite sizes give the default scale``() =
  let props = {
    MetroViewbox.defaults(LayoutSize(200.0, 100.0), LayoutSize(100.0, 100.0)) with
        ViewportWidth = Double.NaN
        ViewportHeight = Double.NaN
        ContentWidth = Double.NaN
        ContentHeight = Double.NaN
  }

  let struct (sx, sy) = MetroViewbox.computeScale props
  Assert.Equal(1.0, sx, 3)
  Assert.Equal(1.0, sy, 3)

// Ceiling in bytes for one pack call of 2,000 items. Two times the measured
// 48,736 B in Release on 2026-11-24, rounded up to a multiple of 1,024.
#if RELEASE
let packAllocationCeiling = 98304L
#else
// Two times the measured 160,776 B in Debug on 2026-11-24, rounded up to a
// multiple of 1,024.
let packAllocationCeiling = 322560L
#endif

[<Fact>]
let ``pack of a large grid allocates below its ceiling``() =
  let spans =
    Array.init 2000 (fun i ->
      if i % 2 = 0 then
        MetroVariableSizedWrapGrid.span(2, 2)
      else
        MetroVariableSizedWrapGrid.span(1, 1))

  let args = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spans) with
        MaximumRowsOrColumns = ValueSome 4
  }

  let count = 2000
  let available = LayoutSize(1000.0, Double.PositiveInfinity)

  // Warm up the first call of the seam before the measurement.
  MetroVariableSizedWrapGrid.pack(args, count, available) |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
  let _ = MetroVariableSizedWrapGrid.pack(args, count, available)
  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes

  Assert.True(
    allocated <= packAllocationCeiling,
    $"Expected <= {packAllocationCeiling} B, allocated {allocated} B."
  )

[<Fact>]
let ``pack gives the expected cells``() =
  let spans = [|
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
  |]

  let args = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spans) with
        MaximumRowsOrColumns = ValueSome 4
  }

  let struct (placed, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(
      args,
      6,
      LayoutSize(1000.0, Double.PositiveInfinity)
    )

  let expected = [|
    struct (0, 0, 2, 2)
    struct (2, 0, 2, 1)
    struct (2, 1, 1, 1)
    struct (3, 1, 1, 1)
    struct (0, 2, 2, 1)
    struct (2, 2, 1, 1)
  |]

  for i = 0 to expected.Length - 1 do
    let struct (col, row, colSpan, rowSpan) = expected[i]
    let cell = placed[i]

    Assert.Equal(col, cell.Col)
    Assert.Equal(row, cell.Row)
    Assert.Equal(colSpan, cell.ColSpan)
    Assert.Equal(rowSpan, cell.RowSpan)

  Assert.Equal(4, usedCols)
  Assert.Equal(3, usedRows)

[<Fact>]
let ``a very large span and cap are limited``() =
  // One item with a 1,000,000 x 1,000,000 span and a cap of 1,000,000: the
  // span and the cap are clamped to 1024 and the pack throws no exception.
  let spans = [| MetroVariableSizedWrapGrid.span(1000000, 1000000) |]

  let args = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spans) with
        MaximumRowsOrColumns = ValueSome 1000000
  }

  let struct (placed, _, _) =
    MetroVariableSizedWrapGrid.pack(
      args,
      1,
      LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
    )

  Assert.Equal(1024, placed[0].ColSpan)
  Assert.Equal(1024, placed[0].RowSpan)

  // A cap of 1,000,000 is clamped: 10 items of 1x1 use 10 columns.
  let args = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, [||]) with
        MaximumRowsOrColumns = ValueSome 1000000
  }

  let struct (_, usedCols, _) =
    MetroVariableSizedWrapGrid.pack(
      args,
      10,
      LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
    )

  Assert.Equal(10, usedCols)

[<Fact>]
let ``a tall grid with one column puts each item on its own row``() =
  let args = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, [||]) with
        MaximumRowsOrColumns = ValueSome 1
  }

  let struct (placed, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(
      args,
      2000,
      LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
    )

  for i = 0 to 1999 do
    let cell = placed[i]

    if
      cell.Col <> 0 || cell.Row <> i || cell.ColSpan <> 1 || cell.RowSpan <> 1
    then
      failwith $"Item {i} is not at column 0, row {i}, with a 1x1 span."

  Assert.Equal(1, usedCols)
  Assert.Equal(2000, usedRows)

[<Fact>]
let ``a tiny item width gives a bounded column count``() =
  // An item width of 1.0e-9 against 1000 px gives a ratio far above the
  // count of items: the computed limit is the count.
  let args = {
    MetroVariableSizedWrapGrid.defaults(1.0e-9, 64.0, [||]) with
        Gap = 0.0
  }

  let struct (_, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(args, 10, LayoutSize(1000.0, 1000.0))

  Assert.True(
    usedCols <= 10,
    $"Expected at most 10 used columns, got {usedCols}."
  )

  Assert.Equal(1, usedRows)

#if RELEASE
let tinyWidthPackCeiling = 65536L
#else
let tinyWidthPackCeiling = 65536L
#endif

[<Fact>]
let ``a tiny item width allocates a bounded pack buffer``() =
  // One 1x1 item with an item width of 1.0e-9 against 1000 px: the raw
  // ratio is 1.0e12, capped to 65,536, while the summed primary span is 1.
  // The old limit allocated bool[65536] + int[65536] (about 328 KB) here.
  let args = {
    MetroVariableSizedWrapGrid.defaults(1.0e-9, 64.0, [||]) with
        Gap = 0.0
  }

  let available = LayoutSize(1000.0, 1000.0)

  // Warm up the first call before the measurement.
  MetroVariableSizedWrapGrid.pack(args, 1, available) |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()

  let struct (placed, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(args, 1, available)

  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes

  Assert.Equal(1, usedCols)
  Assert.Equal(1, usedRows)
  Assert.Equal(0, placed[0].Col)
  Assert.Equal(0, placed[0].Row)

  Assert.True(
    allocated <= tinyWidthPackCeiling,
    $"Expected <= {tinyWidthPackCeiling} B, allocated {allocated} B."
  )

[<Fact>]
let ``a rich text block with the Badge role has the badge transform and a header has the heading role``
  ()
  =
  use host = createHost(300, 200)
  let mutable badge: Blob = null
  let mutable header: Blob = null

  let root =
    CaptureCell.create(fun () ->
      badge <-
        MetroRichTextBlock.build {
          MetroRichTextBlock.defaults [|
            MetroRichTextBlock.plain "badge text"
          |] with
              Role = MetroTextRole.Badge
        }

      header <-
        MetroRichTextBlock.build {
          MetroRichTextBlock.defaults [|
            MetroRichTextBlock.plain "Section title"
          |] with
              Role = MetroTextRole.Header
        }

      Container().Children(badge, header) :> Blob)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let badgeText = badge :?> Text
  let headerText = header :?> Text

  // The Badge role declares TextTransform.Uppercase, so both casings of the
  // text measure as the display text.
  Assert.Equal(
    window.MeasureText("BADGE TEXT", badgeText),
    window.MeasureText("badge text", badgeText),
    3
  )

  // The Header role declares Heading accessibility with the concatenated
  // text of the runs as the name.
  Assert.Equal(AccessibilityRole.Heading, headerText.Accessibility.Role)
  Assert.Equal(Nullable 2, headerText.Accessibility.Level)
  Assert.Equal("Section title", headerText.Accessibility.Name)

[<Fact>]
let ``a NaN fallback and a very small content give a finite scale``() =
  // A content of 1.0e-40 against a 200x100 viewport gives the bounded 1.0
  // scale instead of an unbounded division result.
  let props =
    MetroViewbox.defaults(
      LayoutSize(200.0, 100.0),
      LayoutSize(1.0e-40, 1.0e-40)
    )

  let struct (sx, sy) = MetroViewbox.computeScale props
  Assert.Equal(1.0, sx, 3)
  Assert.Equal(1.0, sy, 3)

  // A mounted responsive viewbox renders with a NaN fallback size and with a
  // very small fallback size, and each build throws no exception. The child
  // has no size, so the fallback content size stays in use.
  let mountWith(fallbackW: float, fallbackH: float) : MetroViewboxCell =
    use host = createHost(300, 150)
    let mutable vbRef: MetroViewboxCell voption = ValueNone

    let factory() =
      let c = MetroViewbox.create(fun () -> Container() :> Blob)
      vbRef <- ValueSome c
      c

    let root =
      CaptureCell.create(fun () ->
        MetroViewbox.mount(
          "vb",
          {
            MetroViewbox.inputDefaults(LayoutSize(100.0, 50.0)) with
                FallbackContentWidth = fallbackW
                FallbackContentHeight = fallbackH
          },
          factory
        ))

    let window = Window(Root = root)
    Metro.attach(window, darkConfig)
    window.Attach(host) |> ignore
    pumpFrames(host, 4)

    match vbRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroViewboxCell."

  let nanBox = mountWith(Double.NaN, Double.NaN)

  Assert.True(
    Double.IsFinite nanBox.ScaleX,
    $"Expected a finite scale, got {nanBox.ScaleX}."
  )

  Assert.True(
    Double.IsFinite nanBox.ScaleY,
    $"Expected a finite scale, got {nanBox.ScaleY}."
  )

  let tinyBox = mountWith(1.0e-40, 1.0e-40)
  Assert.Equal(1.0, tinyBox.ScaleX, 3)
  Assert.Equal(1.0, tinyBox.ScaleY, 3)

[<Fact>]
let ``the scan start gives the same cells as a scan from row zero``() =
  // A 4-column grid with mixed spans.
  let spans4 = [|
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 1)
  |]

  let args4 = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spans4) with
        MaximumRowsOrColumns = ValueSome 4
  }

  assertSamePlacement(args4, 6, LayoutSize(1000.0, Double.PositiveInfinity))

  // A 3-column grid.
  let spans3 = [|
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(2, 1)
    MetroVariableSizedWrapGrid.span(1, 2)
    MetroVariableSizedWrapGrid.span(3, 1)
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(1, 3)
    MetroVariableSizedWrapGrid.span(2, 1)
  |]

  let args3 = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spans3) with
        MaximumRowsOrColumns = ValueSome 3
  }

  assertSamePlacement(
    args3,
    8,
    LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
  )

  // A one-column grid with 50 items.
  let args1 = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, [||]) with
        MaximumRowsOrColumns = ValueSome 1
  }

  let struct (placed, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(
      args1,
      50,
      LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
    )

  Assert.Equal(1, usedCols)
  Assert.Equal(50, usedRows)

  for i = 0 to 49 do
    let cell = placed[i]
    Assert.Equal(0, cell.Col)
    Assert.Equal(i, cell.Row)
    Assert.Equal(1, cell.ColSpan)
    Assert.Equal(1, cell.RowSpan)

  assertSamePlacement(
    args1,
    50,
    LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
  )

  // A grid with the spans 1x1, 2x2, 3x1, and 1x3.
  let mixed = [|
    MetroVariableSizedWrapGrid.span(1, 1)
    MetroVariableSizedWrapGrid.span(2, 2)
    MetroVariableSizedWrapGrid.span(3, 1)
    MetroVariableSizedWrapGrid.span(1, 3)
  |]

  let spansMixed = Array.init 8 (fun i -> mixed[i % mixed.Length])

  let argsMixed = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, spansMixed) with
        MaximumRowsOrColumns = ValueSome 5
  }

  assertSamePlacement(
    argsMixed,
    8,
    LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
  )

[<Fact>]
let ``a tall grid packs in a short time``() =
  let args = {
    MetroVariableSizedWrapGrid.defaults(50.0, 40.0, [||]) with
        MaximumRowsOrColumns = ValueSome 1
  }

  let count = 16000
  let available = LayoutSize(Double.PositiveInfinity, Double.PositiveInfinity)
  let sw = Stopwatch.StartNew()

  let struct (placed, usedCols, usedRows) =
    MetroVariableSizedWrapGrid.pack(args, count, available)

  sw.Stop()
  Assert.Equal(1, usedCols)
  Assert.Equal(16000, usedRows)
  Assert.Equal(count, placed.Length)

  Assert.True(
    sw.ElapsedMilliseconds < 500L,
    $"Expected pack of {count} items below 500 ms, took {sw.ElapsedMilliseconds} ms."
  )

[<Fact>]
let ``a person picture with an image and a name builds no initials``() =
  use host = createHost(300, 200)
  let px = Array.zeroCreate<byte>(2 * 2 * 4)

  for i = 0 to px.Length - 1 do
    px[i] <- 200uy

  use source = new ImageSource(2, 2, px)

  let root =
    CaptureCell.create(fun () ->
      MetroPersonPicture.build {
        MetroPersonPicture.defaults "Ada Lovelace" with
            Source = ValueSome(source :> ImageSourceProvider)
      })

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // The build succeeds and the accessible name is the display name. The
  // initials are not observable here: the test cannot count the calls of a
  // private function.
  Assert.Equal("Ada Lovelace", root.Captured.Accessibility.Name)
  Assert.Equal(AccessibilityRole.Image, root.Captured.Accessibility.Role)

[<Fact>]
let ``a huge content size gives a finite result``() =
  let props =
    MetroViewbox.defaults(
      LayoutSize(100.0, 100.0),
      LayoutSize(1.0e300, 1.0e300)
    )

  let struct (sx, sy) = MetroViewbox.computeScale props
  Assert.True(Double.IsFinite sx, $"Expected a finite scale, got {sx}.")
  Assert.True(Double.IsFinite sy, $"Expected a finite scale, got {sy}.")

  // The stateless build accepts the size without an exception.
  MetroViewbox.build(props, Container() :> Blob) |> ignore

  // The mounted responsive viewbox builds with a huge fallback content size
  // and gives finite metrics.
  use host = createHost(100, 100)
  let mutable vbRef: MetroViewboxCell voption = ValueNone

  let factory() =
    let c = MetroViewbox.create(fun () -> Container() :> Blob)
    vbRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroViewbox.mount(
        "vb",
        {
          MetroViewbox.inputDefaults(LayoutSize(100.0, 100.0)) with
              FallbackContentWidth = 1.0e300
              FallbackContentHeight = 1.0e300
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 4)

  let vb =
    match vbRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroViewboxCell."

  Assert.True(
    Double.IsFinite vb.ScaleX,
    $"Expected a finite scale, got {vb.ScaleX}."
  )

  Assert.True(
    Double.IsFinite vb.ScaleY,
    $"Expected a finite scale, got {vb.ScaleY}."
  )

  Assert.True(Double.IsFinite vb.ViewportHandle.BorderBox.Width)
  Assert.True(Double.IsFinite vb.ViewportHandle.BorderBox.Height)
  Assert.True(Double.IsFinite vb.ContentHandle.BorderBox.Width)
  Assert.True(Double.IsFinite vb.ContentHandle.BorderBox.Height)

[<Fact>]
let ``a person picture with a presence states it in the accessible name``() =
  use host = createHost(300, 200)

  let root =
    CaptureCell.create(fun () ->
      MetroPersonPicture.build {
        MetroPersonPicture.defaults "Ada Lovelace" with
            Presence = MetroPresence.Available
      })

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let name = root.Captured.Accessibility.Name

  Assert.True(
    name.EndsWith(", Available"),
    $"Expected an accessible name ending with ', Available', got '{name}'."
  )

[<Fact>]
let ``build skips a null item``() =
  let args = MetroVariableSizedWrapGrid.defaults(50.0, 40.0, [||])

  let tree =
    MetroVariableSizedWrapGrid.build(
      args,
      [| Container() :> Blob; null; Container() :> Blob |]
    )

  Assert.Equal(2, childCount tree)

[<Fact>]
let ``a child with a percentage size has no size in the responsive viewbox``() =
  use host = createHost(300, 150)
  let h = ElementHandle()
  let mutable vbRef: MetroViewboxCell voption = ValueNone

  let factory() =
    let c =
      MetroViewbox.create(fun () ->
        Container(
          Handle = h,
          Width = Length.Percent 50.0,
          Height = Length.Percent 50.0
        )
        :> Blob)

    vbRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroViewbox.mount(
        "vb",
        MetroViewbox.inputDefaults(LayoutSize(100.0, 50.0)),
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 4)

  match vbRef with
  | ValueSome _ -> ()
  | ValueNone -> failwith "Expected MetroViewboxCell."

  // The percentage child has no natural size: the limit that the
  // MetroLayout signature states.
  Assert.Equal(0.0, h.BorderBox.Width, 3)
  Assert.Equal(0.0, h.BorderBox.Height, 3)

[<Fact>]
let ``MetroBorder CornerRadius applies and sanitizes to sharp``() =
  Assert.Equal(0.0, MetroBorder.defaults().CornerRadius)
  Assert.Equal(0.0, MetroBorder.card().CornerRadius)

  use host = createHost(400, 200)

  let root =
    CaptureCell.create(fun () ->
      Container()
        .Children(
          MetroBorder.build(
            {
              MetroBorder.defaults() with
                  CornerRadius = 6.0
            },
            Container(Key = "radius-child") :> Blob
          ),
          MetroBorder.build(
            {
              MetroBorder.defaults() with
                  CornerRadius = -4.0
            },
            Container(Key = "neg-child") :> Blob
          ),
          MetroBorder.build(
            {
              MetroBorder.defaults() with
                  CornerRadius = Double.NaN
            },
            Container(Key = "nan-child") :> Blob
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let rounded = childAt(root.Captured, 0) :?> Container
  let negative = childAt(root.Captured, 1) :?> Container
  let nan = childAt(root.Captured, 2) :?> Container

  Assert.Equal(6.0f, requireEntry(rounded, "BorderRadius").A, 3)
  Assert.Equal(0.0f, requireEntry(negative, "BorderRadius").A, 3)
  Assert.Equal(0.0f, requireEntry(nan, "BorderRadius").A, 3)

[<Fact>]
let ``MetroImage renders Placeholder for no source and keeps present sources``
  ()
  =
  let imageDefaults = MetroImage.defaults ValueNone
  Assert.True(imageDefaults.Placeholder.IsNone)
  Assert.True(imageDefaults.Fallback.IsNone)
  Assert.False(imageDefaults.Bordered)

  use host = createHost(400, 200)
  let px = Array.zeroCreate<byte>(2 * 2 * 4)

  for i = 0 to px.Length - 1 do
    px[i] <- 200uy

  use source = new ImageSource(2, 2, px)

  let root =
    CaptureCell.create(fun () ->
      Container()
        .Children(
          MetroImage.build {
            MetroImage.defaults ValueNone with
                Placeholder = ValueSome(Container(Key = "img-ph") :> Blob)
                Fallback = ValueSome(Container(Key = "img-fb") :> Blob)
          },
          MetroImage.build {
            MetroImage.defaults ValueNone with
                Fallback = ValueSome(Container(Key = "img-fb-only") :> Blob)
          },
          MetroImage.build {
            MetroImage.defaults(ValueSome(source :> ImageSourceProvider)) with
                Placeholder = ValueSome(Container(Key = "img-src-ph") :> Blob)
                Fallback = ValueSome(Container(Key = "img-src-fb") :> Blob)
          },
          MetroImage.build {
            MetroImage.defaults ValueNone with
                Width = ValueSome(Length.Pixels 48.0)
                Height = ValueSome(Length.Pixels 48.0)
          }
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // An empty source renders the placeholder, not the fallback.
  Assert.True(findBlobByKey(root.Captured, "img-ph").IsSome)
  Assert.True(findBlobByKey(root.Captured, "img-fb").IsNone)

  // The fallback is the empty-source content when no placeholder is set.
  Assert.True(findBlobByKey(root.Captured, "img-fb-only").IsSome)

  // A present source renders the image element, which owns the wired
  // placeholder and fallback slots for its own load states.
  let loaded = childAt(root.Captured, 2) :?> Container
  Assert.Equal(1, childCount loaded)
  Assert.IsType<Image>(childAt(loaded, 0)) |> ignore
  Assert.True(findBlobByKey(root.Captured, "img-src-ph").IsNone)
  Assert.True(findBlobByKey(root.Captured, "img-src-fb").IsNone)

  // Neither slot leaves an empty frame.
  let empty = childAt(root.Captured, 3) :?> Container
  Assert.Equal(0, childCount empty)
