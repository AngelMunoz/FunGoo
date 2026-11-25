module FunGoo.Metro.Tests.TileRebuildCeilingTests

open System
open System.Diagnostics
open System.Threading
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

/// Pumps frames for at least <paramref name="seconds"/> of wall-clock time,
/// so window timers fire, then returns the elapsed time.
let pumpWallClock(host: EmbeddedWindowHost, seconds: float) : TimeSpan =
  let clock = Stopwatch.StartNew()

  while clock.Elapsed.TotalSeconds < seconds do
    host.RenderFrame(0.016)
    Thread.Sleep(16)

  clock.Elapsed

/// Ceiling of builds over <paramref name="elapsed"/> for a tile cycling every
/// <paramref name="intervalMs"/>: one build for each timer advance, one for a
/// burst delivery, and one for the mount settle.
let ceilingBuilds(elapsed: TimeSpan, intervalMs: float) : int =
  int(elapsed.TotalMilliseconds / intervalMs) + 3

[<Fact>]
let ``Cycling tiles rebuild at most once per timer advance under pumped time``
  ()
  =
  let faces = [|
    {
      Title = "Build"
      Subtitle = ValueSome "Release green"
      Metric = ValueSome "0"
      AccentOverride = ValueNone
    }
    {
      Title = "Build"
      Subtitle = ValueSome "Tests passing"
      Metric = ValueSome "100%"
      AccentOverride = ValueNone
    }
  |]

  let mutable liveRef: MetroLiveTileCell voption = ValueNone
  let mutable cycleRef: MetroCycleTileCell voption = ValueNone
  let mutable flipRef: MetroFlipTileCell voption = ValueNone

  let liveFactory() =
    let cell = MetroLiveTile.create(ignore)
    liveRef <- ValueSome cell
    cell

  let cycleFactory() =
    let cell = MetroCycleTile.create(ignore)
    cycleRef <- ValueSome cell
    cell

  let flipFactory() =
    let cell = MetroFlipTile.create(ignore)
    flipRef <- ValueSome cell
    cell

  use host = createHost(800, 600)

  let capture =
    CaptureCell.create(fun () ->
      Container()
        .Children(
          MetroLiveTile.mount(
            "live",
            {
              MetroLiveTile.defaults faces with
                  IntervalMs = 300.0
            },
            liveFactory
          ),
          MetroCycleTile.mount(
            "cycle",
            {
              MetroCycleTile.defaults faces with
                  IntervalMs = 500.0
            },
            cycleFactory
          ),
          MetroFlipTile.mount(
            "flip",
            MetroFlipTile.defaults(faces[0], faces[1]),
            flipFactory
          )
        ))

  let window = Window(Root = capture)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let liveCell = liveRef |> ValueOption.get
  let cycleCell = cycleRef |> ValueOption.get
  let flipCell = flipRef |> ValueOption.get
  Assert.True(liveCell.IsTimerActive)
  Assert.True(cycleCell.IsTimerActive)

  let liveBefore = liveCell.Builds
  let cycleBefore = cycleCell.Builds
  let flipBefore = flipCell.Builds

  let elapsed = pumpWallClock(host, 1.5)

  let liveDelta = liveCell.Builds - liveBefore
  let cycleDelta = cycleCell.Builds - cycleBefore

  Assert.True(
    liveDelta <= ceilingBuilds(elapsed, 300.0),
    $"Live tile rebuilt {liveDelta} times in {elapsed.TotalSeconds:F2} s."
  )

  Assert.True(
    cycleDelta <= ceilingBuilds(elapsed, 500.0),
    $"Cycle tile rebuilt {cycleDelta} times in {elapsed.TotalSeconds:F2} s."
  )

  // The static flip tile never rebuilds without input.
  Assert.Equal(flipBefore, flipCell.Builds)

  // The window stayed open long enough to cycle: the ceiling above is not
  // vacuous.
  Assert.True(
    liveDelta >= 1,
    "Expected the live tile to advance at least once."
  )

  Assert.True(
    cycleDelta >= 1,
    "Expected the cycle tile to advance at least once."
  )
