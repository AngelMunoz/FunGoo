module FunGoo.Metro.Tests.ThemeTests

open System
open Xunit
open Goo
open Goo.Widgets
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost

/// State of the dual theme probe. One mutable field per concern.
type private DualThemeState = {
  mutable Builds: int
  mutable MetroSnapshot: MetroTheme
  mutable WidgetSnapshot: Theme
  mutable MeasureReads: bool
  mutable ReadBytes: int64
}

/// A cell that reads the Metro theme and the widget theme in its Build. The
/// tests read the two snapshots through its members.
/// <c>DualThemeProbe.create</c> makes the cell.
[<AbstractClass>]
type private DualThemeProbe() =
  inherit Cell()

  /// The number of builds.
  abstract Builds: int

  /// The Metro theme of the last build.
  abstract MetroSnapshot: MetroTheme

  /// The widget theme of the last build.
  abstract WidgetSnapshot: Theme

  /// <c>true</c> makes the next build measure the bytes of 10000 Metro reads.
  abstract MeasureReads: bool with get, set

  /// The bytes that the last measurement allocated.
  abstract ReadBytes: int64

[<RequireQualifiedAccess>]
module private DualThemeProbe =

  /// A new state for one probe. Each probe has its own record.
  let initial() : DualThemeState = {
    Builds = 0
    MetroSnapshot = Unchecked.defaultof<MetroTheme>
    WidgetSnapshot = Unchecked.defaultof<Theme>
    MeasureReads = false
    ReadBytes = -1L
  }

  /// Measures the bytes that 10000 Metro reads allocate.
  let measureReadBytes() : int64 =
    let mutable acc = 0.0

    for _ = 1 to 10 do
      acc <- acc + Metro.read().TileUnit

    let before = GC.GetAllocatedBytesForCurrentThread()

    for _ = 1 to 10000 do
      acc <- acc + Metro.read().TileUnit

    let bytes = GC.GetAllocatedBytesForCurrentThread() - before
    Assert.True(acc > 0.0)
    bytes

  /// Reads the two themes, measures when the state asks for it, and builds.
  let buildTree(state: DualThemeState) : Blob =
    state.Builds <- state.Builds + 1
    state.MetroSnapshot <- Metro.read()
    state.WidgetSnapshot <- Theme.Read()

    if state.MeasureReads then
      state.ReadBytes <- measureReadBytes()

    Text(
      Content = "Metro probe",
      Color = state.MetroSnapshot.Palette.Foreground
    )

  let create() : DualThemeProbe =
    let state = initial()

    {
      new DualThemeProbe() with
        override _.Builds = state.Builds
        override _.MetroSnapshot = state.MetroSnapshot
        override _.WidgetSnapshot = state.WidgetSnapshot

        override _.MeasureReads
          with get () = state.MeasureReads
          and set value = state.MeasureReads <- value

        override _.ReadBytes = state.ReadBytes
        override _.Build() : Blob = buildTree state
    }

/// A host with one probe outside a Metro scope and one probe inside it.
/// <c>ScopeHostCell.create</c> makes the cell.
[<AbstractClass>]
type private ScopeHostCell() =
  inherit Cell()

  /// The probe outside the Metro scope.
  abstract Outside: DualThemeProbe

  /// The probe inside the Metro scope.
  abstract Inside: DualThemeProbe

[<RequireQualifiedAccess>]
module private ScopeHostCell =

  /// Builds the two mounts. The mount inside the scope runs in the scope.
  let buildTree
    (
      overrideTheme: MetroTheme,
      insideFactory: Func<Cell>,
      outsideFactory: Func<Cell>
    ) : Blob =
    let scoped =
      Metro.scope(
        overrideTheme,
        fun () -> Cell.Mount<Cell>("inside", insideFactory)
      )

    scoped.Key <- "metro-scope"

    Container().Children(Cell.Mount<Cell>("outside", outsideFactory), scoped)

  let create(overrideTheme: MetroTheme) : ScopeHostCell =
    let outside = DualThemeProbe.create()
    let inside = DualThemeProbe.create()
    let outsideFactory = Func<Cell>(fun () -> outside)
    let insideFactory = Func<Cell>(fun () -> inside)

    {
      new ScopeHostCell() with
        override _.Outside = outside
        override _.Inside = inside

        override _.Build() : Blob =
          buildTree(overrideTheme, insideFactory, outsideFactory)
    }

[<Fact>]
let ``All 21 named Metro accents match exact hex values and contrast``() =
  let expected = [|
    MetroAccentKind.Blue, "Blue", "#0078d4", "#429ce3", "#005a9e"
    MetroAccentKind.Red, "Red", "#e51400", "#ff4438", "#b40000"
    MetroAccentKind.Orange, "Orange", "#fa6800", "#ff9a3c", "#c64900"
    MetroAccentKind.Green, "Green", "#00a300", "#00d500", "#007200"
    MetroAccentKind.Teal, "Teal", "#00aba9", "#00ddda", "#007a78"
    MetroAccentKind.Purple, "Purple", "#a200ff", "#d04aff", "#7200ca"
    MetroAccentKind.Magenta, "Magenta", "#d80073", "#ff2d9b", "#a60052"
    MetroAccentKind.Lime, "Lime", "#a4c400", "#d5f200", "#749200"
    MetroAccentKind.Brown, "Brown", "#825a2c", "#b1834f", "#533a1c"
    MetroAccentKind.Pink, "Pink", "#f472d6", "#fba0eb", "#cf4ab0"
    MetroAccentKind.Mango, "Mango", "#f09609", "#ffc04a", "#be7200"
    MetroAccentKind.Cobalt, "Cobalt", "#0050ef", "#4a8aff", "#0035a8"
    MetroAccentKind.Indigo, "Indigo", "#6a00ff", "#9c4aff", "#4a00c8"
    MetroAccentKind.Violet, "Violet", "#aa00ff", "#c94aff", "#8200c8"
    MetroAccentKind.Crimson, "Crimson", "#a20025", "#d44a4a", "#720018"
    MetroAccentKind.Emerald, "Emerald", "#008a00", "#00c400", "#005a00"
    MetroAccentKind.Mauve, "Mauve", "#765f89", "#a090b0", "#543f65"
    MetroAccentKind.Sienna, "Sienna", "#a0522d", "#c8805c", "#783a1f"
    MetroAccentKind.Olive, "Olive", "#6d8764", "#96b08a", "#4d6344"
    MetroAccentKind.Steel, "Steel", "#647687", "#8a9dad", "#455a6b"
    MetroAccentKind.Taupe, "Taupe", "#87794e", "#b0a070", "#5f5530"
  |]

  Assert.Equal(21, MetroAccent.named.Count)

  for i = 0 to expected.Length - 1 do
    let kind, name, baseHex, lightHex, darkHex = expected[i]
    let actualKind, actualName, actual = MetroAccent.named[i]
    let resolved = MetroAccent.resolve(kind, ValueNone)

    Assert.Equal(kind, actualKind)
    Assert.Equal(name, actualName)
    Assert.Equal(actual, resolved)
    Assert.Equal(Color.Parse baseHex, resolved.Base)
    Assert.Equal(Color.Parse lightHex, resolved.Light)
    Assert.Equal(Color.Parse darkHex, resolved.Dark)

    // Metro rule: an accent surface always uses a white foreground.
    Assert.Equal(Color.White, resolved.OnAccent)

[<Fact>]
let ``MetroAccent resolve handles System fallback and Custom colors``() =
  let blue = MetroAccent.resolve(MetroAccentKind.Blue, ValueNone)
  let fallback = MetroAccent.resolve(MetroAccentKind.System, ValueNone)
  Assert.Equal(blue, fallback)

  let sysColor = Color.Rgb(20, 180, 120)

  let sysResolved =
    MetroAccent.resolve(MetroAccentKind.System, ValueSome sysColor)

  let customResolved =
    MetroAccent.resolve(MetroAccentKind.Custom sysColor, ValueNone)

  let fromColor = MetroAccent.fromColor sysColor

  Assert.Equal(fromColor, sysResolved)
  Assert.Equal(fromColor, customResolved)
  Assert.Equal(sysColor, customResolved.Base)

  Assert.True(
    customResolved.Light.R > sysColor.R && customResolved.Light.G > sysColor.G
  )

  Assert.True(
    customResolved.Dark.R < sysColor.R && customResolved.Dark.G < sysColor.G
  )

  let darkCustom = MetroAccent.fromColor(Color.Rgb(16, 24, 48))
  Assert.Equal(Color.White, darkCustom.OnAccent)

  let brightCustom = MetroAccent.fromColor(Color.Rgb(240, 230, 120))
  Assert.Equal(Color.White, brightCustom.OnAccent)

[<Fact>]
let ``Metro attach bridges sharp zero-radius flat theme to Goo Widgets``() =
  use darkHost = createHost(480, 320)
  let darkProbe = DualThemeProbe.create()
  let darkWindow = Window(Root = darkProbe)

  Metro.attach(
    darkWindow,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Teal
    }
  )

  darkWindow.Attach(darkHost) |> ignore
  pumpFrames(darkHost, 5)

  let mDark = darkProbe.MetroSnapshot
  let wDark = darkProbe.WidgetSnapshot

  Assert.True(mDark.IsDark)
  Assert.Equal(Color.Parse "#000000", mDark.Palette.Background)
  Assert.Equal(Color.Parse "#000000", wDark.CanvasColor)
  Assert.Equal(Color.Parse "#ffffff", wDark.TextColor)
  Assert.Equal(Color.Parse "#00aba9", wDark.AccentColor)

  Assert.Equal(Nullable 0.0, wDark.ControlRadius)
  Assert.Equal(Nullable 0.0, wDark.PanelRadius)
  Assert.Equal(Nullable 0.0, wDark.RadiusSm)
  Assert.Equal(Nullable 0.0, wDark.RadiusMd)
  Assert.Equal(Nullable 0.0, wDark.RadiusLg)
  Assert.Equal(Nullable 0.0, wDark.RadiusXl)

  Assert.Empty(wDark.PanelShadows)
  Assert.Empty(wDark.OverlayShadows)
  Assert.Empty(wDark.ControlShadows)
  Assert.Empty(wDark.FieldShadows)

  Assert.Equal(2.0, wDark.BorderWidth)
  Assert.Equal(MetroTiming.fastMs, wDark.TransitionMs)
  Assert.Equal(MetroTiming.fastMs, wDark.MotionFastMs)
  Assert.Equal(MetroTiming.normalMs, wDark.MotionNormalMs)
  Assert.Equal(MetroTiming.slowMs, wDark.MotionSlowMs)
  Assert.Equal(SelectionStyle.Filled, wDark.Selection)
  Assert.Equal(ControlStyle.Standard, wDark.Controls)

  use lightHost = createHost(480, 320)
  let lightProbe = DualThemeProbe.create()
  let lightWindow = Window(Root = lightProbe)

  Metro.attach(
    lightWindow,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Light
    }
  )

  lightWindow.Attach(lightHost) |> ignore
  pumpFrames(lightHost, 5)

  Assert.False(lightProbe.MetroSnapshot.IsDark)

  Assert.Equal(
    Color.Parse "#ffffff",
    lightProbe.MetroSnapshot.Palette.Background
  )

  Assert.Equal(Color.Parse "#ffffff", lightProbe.WidgetSnapshot.CanvasColor)
  Assert.Equal(Color.Parse "#1f1f1f", lightProbe.WidgetSnapshot.TextColor)

[<Fact>]
let ``Metro setMode switches Light and Dark and syncs OS preferences``() =
  use host = createHost(480, 320)
  host.UpdatePreferences(PlatformPreferences(Theme = SystemTheme.Light))

  let probe = DualThemeProbe.create()
  let window = Window(Root = probe)

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Light
          Accent = MetroAccentKind.System
    }
  )

  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  Assert.False(probe.MetroSnapshot.IsDark)
  Assert.Equal(Color.Parse "#ffffff", probe.WidgetSnapshot.CanvasColor)
  let buildsAfterLight = probe.Builds

  Metro.setMode(window, MetroMode.Dark)
  pumpFrames(host, 5)

  Assert.True(probe.Builds > buildsAfterLight)
  Assert.True(probe.MetroSnapshot.IsDark)
  Assert.Equal(Color.Parse "#000000", probe.MetroSnapshot.Palette.Background)
  Assert.Equal(Color.Parse "#000000", probe.WidgetSnapshot.CanvasColor)
  Assert.Equal(Color.Parse "#000000", Theme.Read(window).CanvasColor)

  let buildsAfterDark = probe.Builds
  Metro.setMode(window, MetroMode.Light)
  pumpFrames(host, 5)

  Assert.True(probe.Builds > buildsAfterDark)
  Assert.False(probe.MetroSnapshot.IsDark)
  Assert.Equal(Color.Parse "#ffffff", probe.WidgetSnapshot.CanvasColor)

  // Switch to System mode and verify OS preferences update both MetroTheme and Goo.Widgets.Theme.
  Metro.setMode(window, MetroMode.System)
  let osAccent = Color.Rgb(18, 142, 110)

  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      AccentColor = Nullable osAccent,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true,
      TextScaleFactor = Nullable 1.25
    )
  )

  pumpFrames(host, 5)

  Assert.True(probe.MetroSnapshot.IsDark)
  Assert.Equal(osAccent, probe.MetroSnapshot.Accent.Base)
  Assert.True(probe.MetroSnapshot.ReducedMotion)
  Assert.True(probe.MetroSnapshot.HighContrast)
  Assert.Equal(1.25, probe.MetroSnapshot.TextScaleFactor)

  Assert.Equal(
    0.0,
    MetroTheme.effectiveMs(probe.MetroSnapshot, MetroTiming.fastMs)
  )

  Assert.Equal(Color.Parse "#000000", probe.WidgetSnapshot.CanvasColor)
  Assert.Equal(0.0, probe.WidgetSnapshot.TransitionMs)

[<Fact>]
let ``Metro setAccent updates MetroTheme and Goo Widgets AccentColor``() =
  use host = createHost(480, 320)
  let probe = DualThemeProbe.create()
  let window = Window(Root = probe)

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  Assert.Equal(Color.Parse "#0078d4", probe.MetroSnapshot.Accent.Base)
  Assert.Equal(Color.Parse "#0078d4", probe.WidgetSnapshot.AccentColor)

  let buildsBefore = probe.Builds
  Metro.setAccent(window, MetroAccentKind.Crimson)
  pumpFrames(host, 5)

  Assert.True(probe.Builds > buildsBefore)
  Assert.Equal(Color.Parse "#a20025", probe.MetroSnapshot.Accent.Base)
  Assert.Equal(Color.Parse "#a20025", probe.WidgetSnapshot.AccentColor)
  Assert.Equal(Color.Parse "#a20025", Metro.readWindow(window).Accent.Base)
  Assert.Equal(Color.Parse "#a20025", Theme.Read(window).AccentColor)

[<Fact>]
let ``Metro scope overrides subtree for both Metro and Goo Widgets without affecting siblings``
  ()
  =
  use host = createHost(480, 320)
  let baseWindow = Window(Root = DualThemeProbe.create())

  Metro.attach(
    baseWindow,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  let overrideTheme = {
    Metro.readWindow baseWindow with
        IsDark = false
        Palette = MetroPalette.light
        Accent = MetroAccent.resolve(MetroAccentKind.Crimson, ValueNone)
  }

  let root = ScopeHostCell.create overrideTheme
  let window = Window(Root = root)

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  Assert.True(root.Outside.MetroSnapshot.IsDark)

  Assert.Equal(
    Color.Parse "#000000",
    root.Outside.MetroSnapshot.Palette.Background
  )

  Assert.Equal(Color.Parse "#0078d4", root.Outside.MetroSnapshot.Accent.Base)
  Assert.Equal(Color.Parse "#000000", root.Outside.WidgetSnapshot.CanvasColor)
  Assert.Equal(Color.Parse "#0078d4", root.Outside.WidgetSnapshot.AccentColor)

  Assert.False(root.Inside.MetroSnapshot.IsDark)

  Assert.Equal(
    Color.Parse "#ffffff",
    root.Inside.MetroSnapshot.Palette.Background
  )

  Assert.Equal(Color.Parse "#a20025", root.Inside.MetroSnapshot.Accent.Base)
  Assert.Equal(Color.Parse "#ffffff", root.Inside.WidgetSnapshot.CanvasColor)
  Assert.Equal(Color.Parse "#a20025", root.Inside.WidgetSnapshot.AccentColor)

[<Fact>]
let ``Warm Metro read allocates zero bytes in steady state``() =
  use host = createHost(480, 320)
  let probe = DualThemeProbe.create()
  let window = Window(Root = probe)

  Metro.attachDefault(window)
  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  probe.MeasureReads <- true
  probe.Rebuild()
  pumpFrames(host, 5)

  Assert.Equal(0L, probe.ReadBytes)

[<Fact>]
let ``Metro tokens typography spacing timing and canvasStyle match spec``() =
  let hero = MetroTypography.spec MetroTextRole.Hero
  Assert.Equal(56.0, hero.FontSize)
  Assert.Equal(300, hero.FontWeight)
  Assert.Equal(-0.02 * 56.0, hero.LetterSpacing, 6)
  Assert.Equal(1.1, hero.LineHeight)
  Assert.False(hero.Uppercase)

  let badge = MetroTypography.spec MetroTextRole.Badge
  Assert.Equal(11.0, badge.FontSize)
  Assert.Equal(700, badge.FontWeight)
  Assert.Equal(0.05 * 11.0, badge.LetterSpacing, 6)
  Assert.True(badge.Uppercase)

  Assert.Equal(4.0, MetroSpacing.unit)
  Assert.Equal(4.0, MetroSpacing.xs)
  Assert.Equal(8.0, MetroSpacing.sm)
  Assert.Equal(12.0, MetroSpacing.md)
  Assert.Equal(16.0, MetroSpacing.lg)
  Assert.Equal(24.0, MetroSpacing.xl)
  Assert.Equal(40.0, MetroSpacing.xxl)

  Assert.Equal(167.0, MetroTiming.fastMs)
  Assert.Equal(250.0, MetroTiming.normalMs)
  Assert.Equal(333.0, MetroTiming.slowMs)
  Assert.Equal(150.0, MetroTiming.continuumMs)
  Assert.Equal(500.0, MetroTiming.turnstileInMs)
  Assert.Equal(400.0, MetroTiming.turnstileOutMs)
  Assert.Equal(10, MetroTiming.staggerDelaysMs.Count)
  Assert.Equal(0.0, MetroTiming.staggerDelaysMs[0])
  Assert.Equal(256.0, MetroTiming.staggerDelaysMs[9])

  let blue = MetroAccent.resolve(MetroAccentKind.Blue, ValueNone)

  let darkTheme: MetroTheme = {
    IsDark = true
    Palette = MetroPalette.dark
    Accent = blue
    FontFamily = ""
    MonoFontFamily = "monospace"
    TileUnit = 150.0
    ReducedMotion = false
    HighContrast = false
    TextScaleFactor = 1.0
  }

  let lightTheme = {
    darkTheme with
        IsDark = false
        Palette = MetroPalette.light
  }

  Assert.Equal(blue.Light, MetroTheme.accentTextHover darkTheme)
  Assert.Equal(blue.Dark, MetroTheme.accentTextHover lightTheme)

  Assert.Equal(
    MetroTiming.normalMs,
    MetroTheme.effectiveMs(darkTheme, MetroTiming.normalMs)
  )

  Assert.Equal(
    0.0,
    MetroTheme.effectiveMs(
      { darkTheme with ReducedMotion = true },
      MetroTiming.normalMs
    )
  )

  let canvas = Metro.canvasStyle darkTheme
  Assert.NotNull(canvas)

[<Fact>]
let ``Metro scope preserves window accessibility policies in both contexts``() =
  use host = createHost(480, 320)

  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true,
      TextScaleFactor = Nullable 1.5
    )
  )

  let overrideTheme: MetroTheme = {
    IsDark = false
    Palette = MetroPalette.light
    Accent = MetroAccent.resolve(MetroAccentKind.Crimson, ValueNone)
    FontFamily = ""
    MonoFontFamily = "monospace"
    TileUnit = 150.0
    ReducedMotion = false
    HighContrast = false
    TextScaleFactor = 1.0
  }

  let root = ScopeHostCell.create overrideTheme
  let window = Window(Root = root)

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  // Inside scope inherits window ReducedMotion, HighContrast, and TextScaleFactor.
  Assert.False(root.Inside.MetroSnapshot.IsDark)
  Assert.True(root.Inside.MetroSnapshot.ReducedMotion)
  Assert.True(root.Inside.MetroSnapshot.HighContrast)
  Assert.Equal(1.5, root.Inside.MetroSnapshot.TextScaleFactor, 6)

  Assert.Equal(0.0, root.Inside.WidgetSnapshot.TransitionMs)
  Assert.Equal(1.5, root.Inside.WidgetSnapshot.TextScaleFactor, 6)
  Assert.Equal(14.0 * 1.5, root.Inside.WidgetSnapshot.FontSize, 6)

  Assert.Equal(
    root.Inside.WidgetSnapshot.TextColor,
    root.Inside.WidgetSnapshot.BorderColor
  )

  // Rebuild root and verify text scale does not compound.
  root.Rebuild()
  pumpFrames(host, 5)
  Assert.Equal(14.0 * 1.5, root.Inside.WidgetSnapshot.FontSize, 6)

[<Fact>]
let ``Metro scope explicit TextScaleFactor and HighContrast apply without compounding``
  ()
  =
  use host = createHost(480, 320)

  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable false,
      HighContrast = Nullable false,
      TextScaleFactor = Nullable 1.25
    )
  )

  let overrideTheme: MetroTheme = {
    IsDark = false
    Palette = MetroPalette.light
    Accent = MetroAccent.resolve(MetroAccentKind.Crimson, ValueNone)
    FontFamily = ""
    MonoFontFamily = "monospace"
    TileUnit = 150.0
    ReducedMotion = true
    HighContrast = true
    TextScaleFactor = 2.0
  }

  let root = ScopeHostCell.create overrideTheme
  let window = Window(Root = root)

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  // Outside keeps window preferences (no HighContrast, 1.25 scale, motion on).
  Assert.False(root.Outside.MetroSnapshot.HighContrast)
  Assert.False(root.Outside.MetroSnapshot.ReducedMotion)
  Assert.Equal(1.25, root.Outside.MetroSnapshot.TextScaleFactor, 6)
  Assert.Equal(14.0 * 1.25, root.Outside.WidgetSnapshot.FontSize, 6)
  Assert.Equal(MetroTiming.fastMs, root.Outside.WidgetSnapshot.TransitionMs)

  // Inside applies explicit 2.0 scale, HighContrast, and ReducedMotion without compounding.
  Assert.True(root.Inside.MetroSnapshot.HighContrast)
  Assert.True(root.Inside.MetroSnapshot.ReducedMotion)
  Assert.Equal(2.0, root.Inside.MetroSnapshot.TextScaleFactor, 6)
  Assert.Equal(2.0, root.Inside.WidgetSnapshot.TextScaleFactor, 6)
  Assert.Equal(14.0 * 2.0, root.Inside.WidgetSnapshot.FontSize, 6)
  Assert.Equal(0.0, root.Inside.WidgetSnapshot.TransitionMs)

  Assert.Equal(
    root.Inside.WidgetSnapshot.TextColor,
    root.Inside.WidgetSnapshot.BorderColor
  )

  root.Rebuild()
  pumpFrames(host, 5)
  Assert.Equal(14.0 * 2.0, root.Inside.WidgetSnapshot.FontSize, 6)

[<Fact>]
let ``Toggling ReducedMotion true to false restores TransitionMs on Theme Read``
  ()
  =
  use host = createHost(480, 320)

  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true,
      TextScaleFactor = Nullable 1.5
    )
  )

  let probe = DualThemeProbe.create()
  let window = Window(Root = probe)

  Metro.attach(
    window,
    {
      MetroConfig.defaultConfig with
          Mode = MetroMode.Dark
          Accent = MetroAccentKind.Blue
    }
  )

  window.Attach(host) |> ignore
  pumpFrames(host, 5)

  Assert.True(probe.MetroSnapshot.ReducedMotion)
  Assert.True(probe.MetroSnapshot.HighContrast)
  Assert.Equal(0.0, probe.WidgetSnapshot.TransitionMs)
  Assert.Equal(14.0 * 1.5, probe.WidgetSnapshot.FontSize, 6)
  Assert.Equal(probe.WidgetSnapshot.TextColor, probe.WidgetSnapshot.BorderColor)

  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable false,
      HighContrast = Nullable false,
      TextScaleFactor = Nullable 1.0
    )
  )

  pumpFrames(host, 5)

  Assert.False(probe.MetroSnapshot.ReducedMotion)
  Assert.False(probe.MetroSnapshot.HighContrast)
  Assert.Equal(1.0, probe.MetroSnapshot.TextScaleFactor, 6)
  Assert.Equal(MetroTiming.fastMs, probe.WidgetSnapshot.TransitionMs)
  Assert.Equal(MetroTiming.fastMs, Theme.Read(window).TransitionMs)
  Assert.Equal(14.0, probe.WidgetSnapshot.FontSize, 6)
  Assert.Equal(MetroPalette.dark.Border, probe.WidgetSnapshot.BorderColor)
