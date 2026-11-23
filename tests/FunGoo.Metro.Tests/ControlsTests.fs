module FunGoo.Metro.Tests.ControlsTests

open System
open System.Collections
open System.Reflection
open Xunit
open Goo
open Goo.Widgets.Inputs
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private flags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

let private entriesMethod = typeof<Style>.GetMethod("Entries", flags)

let private updateTreeMethod =
  typeof<Window>
    .GetMethod(
      "UpdateTree",
      BindingFlags.Instance ||| BindingFlags.NonPublic,
      null,
      [| typeof<float> |],
      null
    )

let private windowNodeField = typeof<Window>.GetField("node", flags)

type private StyleSample = {
  FieldName: string
  A: float32
  B: float32
  C: float32
  D: float32
  Payload: obj
}

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
          B = et.GetField("B", flags).GetValue(e) :?> float32
          C = et.GetField("C", flags).GetValue(e) :?> float32
          D = et.GetField("D", flags).GetValue(e) :?> float32
          Payload = et.GetField("Payload", flags).GetValue(e)
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

let private assertColorEqual(expected: Color, sample: StyleSample) : unit =
  Assert.Equal(expected.R, sample.A, 4)
  Assert.Equal(expected.G, sample.B, 4)
  Assert.Equal(expected.B, sample.C, 4)
  Assert.Equal(expected.A, sample.D, 4)

let private childAt(blob: Blob, index: int) : Blob =
  match blob with
  | :? Container as c ->
    let list = c.Children
    list[index]
  | :? Button as b ->
    let list = b.Children
    list[index]
  | other ->
    failwith $"Blob of type {other.GetType().Name} has no Children collection."

let private readNodeLength(node: obj, fieldName: string) : float =
  let nl = node.GetType().GetField(fieldName, flags).GetValue(node)
  let valueField = nl.GetType().GetField("Value", flags)

  let raw =
    if isNull valueField then
      nl.GetType().GetProperty("Value", flags).GetValue(nl)
    else
      valueField.GetValue(nl)

  Convert.ToDouble(raw)

let private readStyleEntriesField
  (node: obj, fieldName: string)
  : StyleSample array =
  let entries = node.GetType().GetField(fieldName, flags).GetValue(node)

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
        B = et.GetField("B", flags).GetValue(e) :?> float32
        C = et.GetField("C", flags).GetValue(e) :?> float32
        D = et.GetField("D", flags).GetValue(e) :?> float32
        Payload = et.GetField("Payload", flags).GetValue(e)
      })

let private boxShadowCount(style: Style) : int =
  let payload = requireEntry(style, "BoxShadows").Payload

  if isNull payload then
    0
  else
    payload.GetType().GetProperty("Count", flags).GetValue(payload) :?> int

let private findNodeByKey(window: Window, key: string) : obj voption =
  let rootNode = windowNodeField.GetValue(window)

  let rec visit(node: obj) : obj voption =
    if isNull node then
      ValueNone
    else
      let nt = node.GetType()

      let nodeKey =
        nt.GetField("<Key>k__BackingField", flags).GetValue(node) :?> string

      if nodeKey = key then
        ValueSome node
      else
        let children =
          nt.GetField("<Children>k__BackingField", flags).GetValue(node)
          :?> IList

        let mutable found = ValueNone

        if not(isNull children) then
          for i = 0 to children.Count - 1 do
            if found.IsNone then
              found <- visit children[i]

        found

  visit rootNode

let private pumpMotion(window: Window, count: int, dt: float) : unit =
  let args: obj array = [| box dt |]

  for _ = 1 to count do
    updateTreeMethod.Invoke(window, args) |> ignore

let private findMountedCellByTypeName
  (window: Window, typeName: string)
  : (obj * Cell) voption =
  let rootNode = windowNodeField.GetValue(window)

  let rec visit(node: obj) : (obj * Cell) voption =
    if isNull node then
      ValueNone
    else
      let nt = node.GetType()
      let fiber = nt.GetField("<Fiber>k__BackingField", flags).GetValue(node)

      if not(isNull fiber) && fiber.GetType().Name = typeName then
        ValueSome(node, fiber :?> Cell)
      else
        let children =
          nt.GetField("<Children>k__BackingField", flags).GetValue(node)
          :?> IList

        let mutable found = ValueNone

        if not(isNull children) then
          for i = 0 to children.Count - 1 do
            if found.IsNone then
              found <- visit children[i]

        found

  visit rootNode

let private runInMetroWindow
  (
    config: MetroConfig,
    prefs: PlatformPreferences voption,
    buildFn: unit -> Blob
  ) : Blob =
  use host = createHost(800, 600)
  prefs |> ValueOption.iter(fun p -> host.UpdatePreferences(p))
  let cell = CaptureCell.create(buildFn)
  let window = Window(Root = cell)
  Metro.attach(window, config)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)
  cell.Captured

[<Fact>]
let ``MetroText applies 10 roles tones uppercase headings and text scale``() =
  let roles = [|
    MetroTextRole.Hero, 56.0, 300.0, false, ValueSome 1
    MetroTextRole.Title, 42.0, 300.0, false, ValueSome 1
    MetroTextRole.Header, 28.0, 300.0, false, ValueSome 2
    MetroTextRole.Subheader, 26.0, 300.0, false, ValueSome 3
    MetroTextRole.Header2, 26.0, 400.0, false, ValueSome 3
    MetroTextRole.Subtitle, 20.0, 300.0, false, ValueSome 4
    MetroTextRole.Medium, 16.0, 400.0, false, ValueNone
    MetroTextRole.Body, 14.0, 400.0, false, ValueNone
    MetroTextRole.Caption, 12.0, 400.0, false, ValueNone
    MetroTextRole.Badge, 11.0, 700.0, true, ValueNone
  |]

  let darkConfig = {
    MetroConfig.defaultConfig with
        Mode = MetroMode.Dark
        Accent = MetroAccentKind.Blue
  }

  let scalePrefs =
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      TextScaleFactor = Nullable 1.5
    )

  let root =
    runInMetroWindow(
      darkConfig,
      ValueSome scalePrefs,
      fun () ->
        let c = Container()

        for role, _, _, _, _ in roles do
          c.Add(MetroText.build(MetroText.defaults($"Role-{role}", role)))

        // Convenience helpers
        c.Add(MetroText.hero "Hero")
        c.Add(MetroText.title "Title")
        c.Add(MetroText.header "Header")
        c.Add(MetroText.subheader "Subheader")
        c.Add(MetroText.subtitle "Subtitle")
        c.Add(MetroText.body "Body")
        c.Add(MetroText.secondary "Secondary")
        c.Add(MetroText.caption "Caption")
        c.Add(MetroText.badge "new")

        // Explicit heading override and custom tone
        c.Add(
          MetroText.build {
            MetroText.defaults("Override", MetroTextRole.Body) with
                Tone = MetroTextTone.Custom(Color.Rgb(12, 34, 56))
                HeadingLevel = ValueSome 5
                MaxLines = 2
                Wrap = TextWrap.NoWrap
                Trimming = TextTrimming.Ellipsis
          }
        )

        // Semantic tones
        let tones = [|
          MetroTextTone.Primary
          MetroTextTone.Secondary
          MetroTextTone.Disabled
          MetroTextTone.Accent
          MetroTextTone.OnAccent
          MetroTextTone.Info
          MetroTextTone.Success
          MetroTextTone.Warning
          MetroTextTone.Danger
        |]

        for tone in tones do
          c.Add(
            MetroText.build {
              MetroText.defaults("Tone", MetroTextRole.Body) with
                  Tone = tone
            }
          )

        c
    )
    :?> Container

  let children = root.Children

  for i = 0 to roles.Length - 1 do
    let role, expectedSize, expectedWeight, expectedUpper, expectedLevel =
      roles[i]

    let text = children[i] :?> Text
    let spec = MetroTypography.spec role
    let fontSize = requireEntry(text, "FontSize")
    let fontWeight = requireEntry(text, "FontWeight")
    let letterSpacing = requireEntry(text, "LetterSpacing")
    let lineHeight = requireEntry(text, "LineHeight")
    let transform = requireEntry(text, "TextTransform")

    Assert.Equal(expectedSize * 1.5, float fontSize.A, 3)
    Assert.Equal(expectedWeight, float fontWeight.A, 3)
    Assert.Equal(spec.LetterSpacing * 1.5, float letterSpacing.A, 3)
    Assert.Equal(spec.LineHeight, float lineHeight.A, 3)

    let expectedTransform =
      if expectedUpper then
        float32(int TextTransform.Uppercase)
      else
        float32(int TextTransform.None)

    Assert.Equal(expectedTransform, transform.A)

    match expectedLevel with
    | ValueSome level ->
      Assert.Equal(AccessibilityRole.Heading, text.Accessibility.Role)
      Assert.Equal(Nullable level, text.Accessibility.Level)
    | ValueNone ->
      Assert.Equal(AccessibilityRole.Text, text.Accessibility.Role)
      Assert.Equal(Nullable(), text.Accessibility.Level)

  // Secondary helper uses Body size and ForegroundSecondary color.
  let secondaryText = children[roles.Length + 6] :?> Text

  assertColorEqual(
    MetroPalette.dark.ForegroundSecondary,
    requireEntry(secondaryText, "Color")
  )

  // Badge helper is uppercase and 700 weight.
  let badgeText = children[roles.Length + 8] :?> Text
  Assert.Equal(700.0, float(requireEntry(badgeText, "FontWeight").A), 3)

  Assert.Equal(
    float32(int TextTransform.Uppercase),
    requireEntry(badgeText, "TextTransform").A
  )

  // Explicit heading override + custom color + truncation properties.
  let overrideText = children[roles.Length + 9] :?> Text
  Assert.Equal(AccessibilityRole.Heading, overrideText.Accessibility.Role)
  Assert.Equal(Nullable 5, overrideText.Accessibility.Level)
  assertColorEqual(Color.Rgb(12, 34, 56), requireEntry(overrideText, "Color"))
  Assert.Equal(2.0f, requireEntry(overrideText, "TextMaxLines").B)

  Assert.Equal(
    float32(int TextWrap.NoWrap),
    requireEntry(overrideText, "TextWrap").A
  )

  Assert.Equal(
    float32(int TextTrimming.Ellipsis),
    requireEntry(overrideText, "TextTrimming").A
  )

  // Verify semantic tones resolve to dark palette + Blue accent.
  let blue = MetroAccent.resolve(MetroAccentKind.Blue, ValueNone)
  let p = MetroPalette.dark

  let expectedColors = [|
    p.Foreground
    p.ForegroundSecondary
    p.ForegroundDisabled
    blue.Base
    blue.OnAccent
    p.Info
    p.Success
    p.Warning
    p.Danger
  |]

  for j = 0 to expectedColors.Length - 1 do
    let toneText = children[roles.Length + 10 + j] :?> Text
    assertColorEqual(expectedColors[j], requireEntry(toneText, "Color"))

  // Invalid heading levels throw ArgumentOutOfRangeException.
  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    MetroText.build {
      MetroText.defaults("BadLow", MetroTextRole.Body) with
          HeadingLevel = ValueSome 0
    }
    |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    MetroText.build {
      MetroText.defaults("BadHigh", MetroTextRole.Body) with
          HeadingLevel = ValueSome 7
    }
    |> ignore)
  |> ignore

[<Fact>]
let ``MetroButton variants enforce zero radius clicks disabled and motion``() =
  let mutable clicks = 0
  let mutable disabledClicks = 0

  let darkConfig = {
    MetroConfig.defaultConfig with
        Mode = MetroMode.Dark
        Accent = MetroAccentKind.Teal
  }

  let root =
    runInMetroWindow(
      darkConfig,
      ValueNone,
      fun () ->
        Container()
          .Children(
            MetroButton.standard("Save", fun () -> clicks <- clicks + 1),
            MetroButton.accent("Commit", fun () -> clicks <- clicks + 10),
            MetroButton.hyperlink(
              "Learn more",
              fun () -> clicks <- clicks + 100
            ),
            MetroButton.subtle("Cancel", fun () -> clicks <- clicks + 1000),
            MetroButton.build(
              {
                MetroButton.defaults("Disabled") with
                    Disabled = true
                    Pressed = ValueSome true
                    AccessibilityName = ValueSome "Disabled action"
                    MinWidth = ValueSome 120.0
              },
              fun () -> disabledClicks <- disabledClicks + 1
            ),
            MetroButton.build(
              {
                MetroButton.defaults("Pressed") with
                    Pressed = ValueSome true
              },
              fun () -> ()
            )
          )
    )
    :?> Container

  let children = root.Children
  let stdBtn = children[0] :?> Button
  let accBtn = children[1] :?> Button
  let linkBtn = children[2] :?> Button
  let subBtn = children[3] :?> Button
  let disBtn = children[4] :?> Button
  let pressedBtn = children[5] :?> Button

  let teal = MetroAccent.resolve(MetroAccentKind.Teal, ValueNone)
  let p = MetroPalette.dark

  for btn in [| stdBtn; accBtn; linkBtn; subBtn; disBtn; pressedBtn |] do
    Assert.Equal(0.0f, requireEntry(btn, "BorderRadius").A)
    Assert.Equal(0, boxShadowCount btn)
    Assert.Equal(AccessibilityRole.Button, btn.Accessibility.Role)
    Assert.Equal(2.0f, requireEntry(btn.FocusStyle, "OutlineWidth").A)
    Assert.Equal(2.0f, requireEntry(btn.FocusStyle, "OutlineOffset").A)
    assertColorEqual(p.Foreground, requireEntry(btn.FocusStyle, "OutlineColor"))

  // Standard: 2px border, transparent bg, Foreground active fill + scale.
  Assert.Equal(2.0f, requireEntry(stdBtn, "BorderTopWidth").A)
  assertColorEqual(Color.Transparent, requireEntry(stdBtn, "BackgroundColor"))

  assertColorEqual(
    p.Foreground,
    requireEntry(stdBtn.ActiveStyle, "BackgroundColor")
  )

  assertColorEqual(p.Background, requireEntry(stdBtn.ActiveStyle, "Color"))
  Assert.Equal(0.985f, requireEntry(stdBtn.ActiveStyle, "TransformScale").A, 3)
  Assert.Equal(MetroTiming.fastMs, stdBtn.TransitionMs)

  // Accent: teal Base bg + 2px border, OnAccent fg, hover Light, active Dark.
  assertColorEqual(teal.Base, requireEntry(accBtn, "BackgroundColor"))
  assertColorEqual(teal.OnAccent, requireEntry(accBtn, "Color"))
  Assert.Equal(2.0f, requireEntry(accBtn, "BorderTopWidth").A)

  assertColorEqual(
    teal.Light,
    requireEntry(accBtn.HoverStyle, "BackgroundColor")
  )

  assertColorEqual(
    teal.Dark,
    requireEntry(accBtn.ActiveStyle, "BackgroundColor")
  )

  Assert.Equal(0.985f, requireEntry(accBtn.ActiveStyle, "TransformScale").A, 3)

  // Hyperlink: 0px border, accent text, underline hover, minHeight 32.
  Assert.Equal(0.0f, requireEntry(linkBtn, "BorderTopWidth").A)
  assertColorEqual(teal.Base, requireEntry(linkBtn, "Color"))
  Assert.Equal(32.0f, requireEntry(linkBtn, "MinHeight").A)

  Assert.Equal(
    float32(int TextDecoration.Underline),
    requireEntry(linkBtn.HoverStyle, "TextDecoration").A
  )

  // Subtle: 0px border, transparent bg, hover/active neutral fills.
  Assert.Equal(0.0f, requireEntry(subBtn, "BorderTopWidth").A)
  assertColorEqual(p.Hover, requireEntry(subBtn.HoverStyle, "BackgroundColor"))

  assertColorEqual(
    p.Active,
    requireEntry(subBtn.ActiveStyle, "BackgroundColor")
  )

  // Pressed Standard uses Palette.Highlight background and Checked = True.
  assertColorEqual(p.Highlight, requireEntry(pressedBtn, "BackgroundColor"))
  Assert.Equal(AccessibilityChecked.True, pressedBtn.Accessibility.Checked)

  // OnClick and keyboard bindings fire when enabled.
  Assert.NotNull(stdBtn.OnClick)
  Assert.Equal(2, stdBtn.KeyBindings.Length)
  Assert.Equal(Key.Enter, stdBtn.KeyBindings[0].Key)
  Assert.Equal(Key.Space, stdBtn.KeyBindings[1].Key)
  stdBtn.OnClick.Invoke()
  accBtn.OnClick.Invoke()
  linkBtn.KeyBindings[0].OnPress.Invoke()
  subBtn.KeyBindings[1].OnPress.Invoke()
  Assert.Equal(1111, clicks)

  // Disabled blocks clicks/focus and sets AccessibilityName + MinWidth.
  Assert.True(disBtn.Disabled)
  Assert.False(disBtn.Focusable)
  Assert.Null(disBtn.OnClick)
  Assert.Null(disBtn.KeyBindings)
  Assert.Equal("Disabled action", disBtn.Accessibility.Name)
  Assert.Equal(120.0f, requireEntry(disBtn, "MinWidth").A)
  Assert.Equal(0.4f, requireEntry(disBtn.DisabledStyle, "Opacity").A, 3)
  assertColorEqual(p.Foreground, requireEntry(disBtn.DisabledStyle, "Color"))
  assertColorEqual(teal.OnAccent, requireEntry(accBtn.DisabledStyle, "Color"))
  Assert.Equal(0.4f, requireEntry(accBtn.DisabledStyle, "Opacity").A, 3)
  Assert.Equal(0, disabledClicks)

  // ReducedMotion + HighContrast: 0ms transition, no scale, 1.0 opacity.
  let reducedPrefs =
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true
    )

  let hcRoot =
    runInMetroWindow(
      darkConfig,
      ValueSome reducedPrefs,
      fun () ->
        Container()
          .Children(
            MetroButton.standard("HC", fun () -> ()),
            MetroButton.accent("HCAccent", fun () -> ())
          )
    )
    :?> Container

  let hcStd = childAt(hcRoot, 0) :?> Button
  let hcAcc = childAt(hcRoot, 1) :?> Button
  Assert.Equal(0.0, hcStd.TransitionMs)
  Assert.True((tryLastEntry(hcStd.ActiveStyle, "TransformScale")).IsNone)
  Assert.True((tryLastEntry(hcAcc.ActiveStyle, "TransformScale")).IsNone)
  Assert.Equal(1.0f, requireEntry(hcStd.DisabledStyle, "Opacity").A, 3)
  assertColorEqual(p.Foreground, requireEntry(hcStd.DisabledStyle, "Color"))
  assertColorEqual(p.Foreground, requireEntry(hcStd, "BorderTopColor"))
  Assert.Equal(1.0f, requireEntry(hcAcc.DisabledStyle, "Opacity").A, 3)
  assertColorEqual(teal.OnAccent, requireEntry(hcAcc.DisabledStyle, "Color"))
  assertColorEqual(p.Foreground, requireEntry(hcAcc, "BorderTopColor"))

[<Fact>]
let ``Metro inputs configure sharp Metro styling semantics and callbacks``() =
  let mutable textChanged = ""
  let mutable textSubmitted = ""
  let mutable toggleValue: bool voption = ValueNone
  let mutable checkValue: bool voption = ValueNone
  let mutable radioSelected = 0
  let sliderHandler = Action<float>(fun _ -> ())

  let darkConfig = {
    MetroConfig.defaultConfig with
        Mode = MetroMode.Dark
        Accent = MetroAccentKind.Blue
  }

  use host = createHost(800, 600)

  let capture =
    CaptureCell.create(fun () ->
      Container()
        .Children(
          MetroTextBox.build(
            {
              MetroTextBox.defaults("initial") with
                  Label = ValueSome "Username"
                  Placeholder = ValueSome "Enter name"
                  Error = ValueSome "Required"
                  Width = ValueSome(Length.Pixels 280.0)
            },
            (fun v -> textChanged <- v),
            ValueSome(fun v -> textSubmitted <- v)
          ),
          MetroToggleSwitch.build(
            {
              MetroToggleSwitch.defaults false with
                  Header = ValueSome "Wi-Fi"
            },
            fun next -> toggleValue <- ValueSome next
          ),
          MetroToggleSwitch.build(MetroToggleSwitch.defaults true, fun _ -> ()),
          MetroCheckBox.build(
            {
              Label = "Accept terms"
              Checked = false
              Disabled = false
            },
            fun next -> checkValue <- ValueSome next
          ),
          MetroRadioButton.build(
            {
              Label = "Option A"
              Selected = true
              Disabled = false
            },
            fun () -> ()
          ),
          MetroRadioButton.build(
            {
              Label = "Option B"
              Selected = false
              Disabled = false
            },
            fun () -> radioSelected <- radioSelected + 1
          ),
          Container()
            .Children(
              MetroSlider.mount(
                "volume-slider",
                {
                  MetroSlider.defaults(50.0, 0.0, 100.0) with
                      Label = ValueSome "Volume"
                      Step = 5.0
                },
                sliderHandler
              )
            )
        ))

  let window = Window(Root = capture)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let root = capture.Captured :?> Container
  let children = root.Children
  let blue = MetroAccent.resolve(MetroAccentKind.Blue, ValueNone)
  let p = MetroPalette.dark

  // 1. MetroTextBox
  let textBoxRoot = children[0] :?> Container
  Assert.Equal(280.0f, requireEntry(textBoxRoot, "Width").A)
  let entryNodeOpt = findNodeByKey(window, "entry")
  Assert.True(entryNodeOpt.IsSome)
  let entryNode = entryNodeOpt.Value

  Assert.Equal(
    0.0,
    readNodeLength(entryNode, "<BorderRadius>k__BackingField"),
    3
  )

  Assert.Equal(
    2.0,
    readNodeLength(entryNode, "<BorderTopWidth>k__BackingField"),
    3
  )

  Assert.Equal(36.0, readNodeLength(entryNode, "<Height>k__BackingField"), 3)
  let touchMount = childAt(textBoxRoot, 0)

  let touchInput =
    touchMount
      .GetType()
      .GetField("<Input>k__BackingField", flags)
      .GetValue(touchMount)

  let innerContent =
    touchInput.GetType().GetField("Content", flags).GetValue(touchInput)
    :?> Container

  let entry = childAt(innerContent, 1) :?> TextEntry
  Assert.NotNull(entry.OnChange)
  entry.OnChange.Invoke("alice")
  Assert.Equal("alice", textChanged)
  Assert.NotNull(entry.OnSubmit)
  entry.OnSubmit.Invoke("alice")
  Assert.Equal("alice", textSubmitted)

  // 2. MetroToggleSwitch (with header, unchecked -> toggles to true)
  let switchWithHeader = children[1] :?> Container
  let headerText = childAt(switchWithHeader, 0) :?> Text
  Assert.Equal("Wi-Fi", headerText.Content)
  let offSwitchBtn = childAt(switchWithHeader, 1) :?> Button
  Assert.Equal(AccessibilityRole.Switch, offSwitchBtn.Accessibility.Role)
  Assert.Equal(AccessibilityChecked.False, offSwitchBtn.Accessibility.Checked)
  Assert.Equal("Wi-Fi", offSwitchBtn.Accessibility.Name)
  Assert.Equal(2, offSwitchBtn.KeyBindings.Length)
  let offTrack = childAt(offSwitchBtn, 0) :?> Container
  let offThumb = childAt(offTrack, 0) :?> Container
  let offStateText = childAt(offSwitchBtn, 1) :?> Text
  Assert.Equal("Off", offStateText.Content)
  Assert.Equal(44.0f, requireEntry(offTrack, "Width").A)
  Assert.Equal(20.0f, requireEntry(offTrack, "Height").A)
  Assert.Equal(0.0f, requireEntry(offTrack, "BorderRadius").A)
  Assert.Equal(2.0f, requireEntry(offTrack, "BorderTopWidth").A)
  Assert.Equal(12.0f, requireEntry(offThumb, "Width").A)
  Assert.Equal(20.0f, requireEntry(offThumb, "Height").A)
  Assert.Equal(0.0f, requireEntry(offThumb, "BorderRadius").A)
  offSwitchBtn.OnClick.Invoke()
  Assert.Equal(ValueSome true, toggleValue)

  // 3. MetroToggleSwitch (checked, no header: accent fill and "On")
  let onSwitchBtn = children[2] :?> Button
  Assert.Equal(AccessibilityRole.Switch, onSwitchBtn.Accessibility.Role)
  Assert.Equal(AccessibilityChecked.True, onSwitchBtn.Accessibility.Checked)
  let onTrack = childAt(onSwitchBtn, 0) :?> Container
  let onStateText = childAt(onSwitchBtn, 1) :?> Text
  Assert.Equal("On", onStateText.Content)
  assertColorEqual(blue.Base, requireEntry(onTrack, "BackgroundColor"))

  // 4. MetroCheckBox
  let cbBtn = children[3] :?> Button
  Assert.Equal(AccessibilityRole.Checkbox, cbBtn.Accessibility.Role)
  let cbBox = childAt(cbBtn, 0) :?> Container
  Assert.Equal(0.0f, requireEntry(cbBox, "BorderRadius").A)
  Assert.Equal(2.0f, requireEntry(cbBox, "BorderTopWidth").A)
  Assert.Equal(20.0f, requireEntry(cbBox, "Width").A)
  Assert.Equal(20.0f, requireEntry(cbBox, "Height").A)
  cbBtn.OnClick.Invoke()
  Assert.Equal(ValueSome true, checkValue)

  // 5. MetroRadioButton (selected accent border + unselected fires onSelect)
  let rbSelectedBtn = children[4] :?> Button
  Assert.Equal(AccessibilityRole.Radio, rbSelectedBtn.Accessibility.Role)
  let rbSelectedCircle = childAt(rbSelectedBtn, 0) :?> Container
  Assert.Equal(2.0f, requireEntry(rbSelectedCircle, "BorderTopWidth").A)
  assertColorEqual(blue.Base, requireEntry(rbSelectedCircle, "BorderTopColor"))
  let rbUnselectedBtn = children[5] :?> Button
  Assert.Equal(AccessibilityRole.Radio, rbUnselectedBtn.Accessibility.Role)
  let rbUnselectedCircle = childAt(rbUnselectedBtn, 0) :?> Container
  Assert.Equal(2.0f, requireEntry(rbUnselectedCircle, "BorderTopWidth").A)

  assertColorEqual(
    p.BorderStrong,
    requireEntry(rbUnselectedCircle, "BorderTopColor")
  )

  rbUnselectedBtn.OnClick.Invoke()
  Assert.Equal(1, radioSelected)

  // 6. MetroSlider mounted input
  let sliderWrap = children[6] :?> Container
  let sliderBlob = childAt(sliderWrap, 0)
  Assert.Equal("volume-slider", sliderBlob.Key)

  let sliderInput =
    sliderBlob
      .GetType()
      .GetField("<Input>k__BackingField", flags)
      .GetValue(sliderBlob)
    :?> SliderInput

  Assert.Equal(50.0, sliderInput.Value)
  Assert.Equal(Nullable 0.0, sliderInput.Minimum)
  Assert.Equal(Nullable 100.0, sliderInput.Maximum)
  Assert.Equal(Nullable 5.0, sliderInput.Step)
  Assert.Equal(Nullable 0.0, sliderInput.ThumbRadius)
  Assert.Equal(Nullable 0.0, sliderInput.TrackRadius)
  Assert.Equal(Nullable 4.0, sliderInput.TrackThickness)
  Assert.Equal(Nullable 16.0, sliderInput.ThumbSize)
  Assert.Equal(Nullable 0.0, sliderInput.ThumbBorderWidth)
  Assert.Equal(Nullable blue.Base, sliderInput.FillColor)
  Assert.Equal(Nullable blue.Base, sliderInput.ThumbColor)
  Assert.Equal(Nullable p.Border, sliderInput.TrackColor)
  Assert.Same(sliderHandler, sliderInput.OnChange)

  // HighContrast overrides borders and tracks across inputs.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, HighContrast = Nullable true)
  )

  pumpFrames(host, 2)
  let hcRoot = capture.Captured :?> Container
  let hcChildren = hcRoot.Children
  let hcTb = hcChildren[0] :?> Container
  let hcTouch = childAt(hcTb, 0)

  let hcTouchInput =
    hcTouch
      .GetType()
      .GetField("<Input>k__BackingField", flags)
      .GetValue(hcTouch)

  let hcInner =
    hcTouchInput.GetType().GetField("Content", flags).GetValue(hcTouchInput)
    :?> Container

  let hcEntry = childAt(hcInner, 1) :?> TextEntry
  assertColorEqual(p.Foreground, requireEntry(hcEntry.DisabledStyle, "Color"))
  let hcEntryNode = findNodeByKey(window, "entry").Value

  let hcEntryBorder =
    hcEntryNode
      .GetType()
      .GetField("<BorderTopColor>k__BackingField", flags)
      .GetValue(hcEntryNode)
    :?> Color

  // Error field keeps Danger border; valid HighContrast field uses Foreground.
  Assert.Equal(p.Danger, hcEntryBorder)

  let hcValidTbRoot =
    runInMetroWindow(
      darkConfig,
      ValueSome(
        PlatformPreferences(
          Theme = SystemTheme.Dark,
          HighContrast = Nullable true
        )
      ),
      fun () ->
        MetroTextBox.build(MetroTextBox.defaults "valid", ignore, ValueNone)
    )
    :?> Container

  let hcValidTouch = childAt(hcValidTbRoot, 0)

  let hcValidTouchInput =
    hcValidTouch
      .GetType()
      .GetField("<Input>k__BackingField", flags)
      .GetValue(hcValidTouch)

  let hcValidInner =
    hcValidTouchInput
      .GetType()
      .GetField("Content", flags)
      .GetValue(hcValidTouchInput)
    :?> Container

  let hcValidEntry = childAt(hcValidInner, 0) :?> TextEntry
  assertColorEqual(p.Foreground, requireEntry(hcValidEntry, "BorderTopColor"))

  assertColorEqual(
    p.Foreground,
    requireEntry(hcValidEntry.DisabledStyle, "Color")
  )

  let hcRbSel = childAt(hcChildren[4] :?> Button, 0) :?> Container
  assertColorEqual(p.Foreground, requireEntry(hcRbSel, "BorderTopColor"))

  let hcSliderWrap = hcChildren[6] :?> Container
  let hcSliderBlob = childAt(hcSliderWrap, 0)

  let hcSliderInput =
    hcSliderBlob
      .GetType()
      .GetField("<Input>k__BackingField", flags)
      .GetValue(hcSliderBlob)
    :?> SliderInput

  Assert.Equal(Nullable p.Foreground, hcSliderInput.TrackColor)

[<Fact>]
let ``MetroProgressBar clamps determinate and animates without rebuilds``() =
  let darkConfig = {
    MetroConfig.defaultConfig with
        Mode = MetroMode.Dark
        Accent = MetroAccentKind.Blue
  }

  // Determinate clamping and sharp styling
  let detRoot =
    runInMetroWindow(
      darkConfig,
      ValueNone,
      fun () ->
        Container()
          .Children(
            MetroProgressBar.build("p-neg", MetroProgressBar.determinate -0.5),
            MetroProgressBar.build("p-mid", MetroProgressBar.determinate 0.45),
            MetroProgressBar.build("p-over", MetroProgressBar.determinate 1.8),
            MetroProgressBar.build(
              "p-nan",
              MetroProgressBar.determinate Double.NaN
            )
          )
    )
    :?> Container

  let expectedPcts = [| 0.0f; 45.0f; 100.0f; 0.0f |]
  let expectedNows = [| 0.0; 0.45; 1.0; 0.0 |]

  for i = 0 to 3 do
    let track = childAt(detRoot, i) :?> Container
    let fill = childAt(track, 0) :?> Container
    Assert.Equal(AccessibilityRole.ProgressBar, track.Accessibility.Role)
    Assert.Equal(Nullable expectedNows[i], track.Accessibility.Range.Now)
    Assert.Equal(0.0f, requireEntry(track, "BorderRadius").A)
    Assert.Equal(0.0f, requireEntry(fill, "BorderRadius").A)
    Assert.Equal(expectedPcts[i], requireEntry(fill, "Width").A, 3)

  // HighContrast determinate track uses BorderStrong.
  let hcDetRoot =
    runInMetroWindow(
      darkConfig,
      ValueSome(
        PlatformPreferences(
          Theme = SystemTheme.Dark,
          HighContrast = Nullable true
        )
      ),
      fun () ->
        Container()
          .Children(
            MetroProgressBar.build("p-hc", MetroProgressBar.determinate 0.5)
          )
    )
    :?> Container

  let hcDetTrack = childAt(hcDetRoot, 0) :?> Container

  assertColorEqual(
    MetroPalette.dark.BorderStrong,
    requireEntry(hcDetTrack, "BackgroundColor")
  )

  // Indeterminate mode ticks frames with zero Cell rebuilds.
  use host = createHost(640, 200)
  let mutable showBar = true

  let hostCell =
    CaptureCell.create(fun () ->
      if showBar then
        Container()
          .Children(
            MetroProgressBar.build(
              "ind-bar",
              {
                MetroProgressBar.indeterminate with
                    Width = Length.Pixels 300.0
              }
            )
          )
      else
        Container())

  let window = Window(Root = hostCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let found = findMountedCellByTypeName(window, "MetroIndeterminateBarCell")
  Assert.True(found.IsSome)
  let barNode, barCell = found.Value

  let translateAnims =
    barCell.GetType().GetField("translateAnims", flags).GetValue(barCell)
    :?> Anim<Point> array

  let opacityAnims =
    barCell.GetType().GetField("opacityAnims", flags).GetValue(barCell)
    :?> Anim<float> array

  Assert.Equal(5, translateAnims.Length)
  Assert.Equal(5, opacityAnims.Length)
  Assert.True(translateAnims[0].Running)

  let buildsBefore = hostCell.Builds

  let stylePassField =
    barNode.GetType().GetField("<StyleResolvedPass>k__BackingField", flags)

  let passBefore = stylePassField.GetValue(barNode) :?> int64
  let x0 = translateAnims[0].Value.X

  pumpMotion(window, 15, 0.03)

  let x1 = translateAnims[0].Value.X
  let passAfter = stylePassField.GetValue(barNode) :?> int64

  Assert.NotEqual(x0, x1)
  Assert.Equal(buildsBefore, hostCell.Builds)
  Assert.Equal(passBefore, passAfter)
  Assert.True(opacityAnims[0].Value > 0.0)

  // ReducedMotion stops animations and renders static indicator bar.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true
    )
  )

  pumpFrames(host, 3)

  for i = 0 to 4 do
    Assert.False(translateAnims[i].Running)
    Assert.False(opacityAnims[i].Running)

  let hcStaticBg =
    barNode
      .GetType()
      .GetField("<BackgroundColor>k__BackingField", flags)
      .GetValue(barNode)
    :?> Color

  Assert.Equal(MetroPalette.dark.BorderStrong, hcStaticBg)

  // ReducedMotion true -> false restarts indeterminate dot motion.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable false,
      HighContrast = Nullable false
    )
  )

  pumpFrames(host, 3)
  Assert.True(translateAnims[0].Running)
  let xResumed0 = translateAnims[0].Value.X
  pumpMotion(window, 10, 0.03)
  Assert.NotEqual(xResumed0, translateAnims[0].Value.X)

  // Unmounting stops all bar animations cleanly.
  showBar <- false
  hostCell.Rebuild()
  pumpFrames(host, 2)

  for i = 0 to 4 do
    Assert.False(translateAnims[i].Running)
    Assert.False(opacityAnims[i].Running)

[<Fact>]
let ``MetroProgressRing animates hides inactive and respects ReducedMotion``() =
  let darkConfig = {
    MetroConfig.defaultConfig with
        Mode = MetroMode.Dark
        Accent = MetroAccentKind.Teal
  }

  use host = createHost(400, 300)
  let mutable active = true
  let mutable size = MetroProgressRingSize.Normal
  let mutable showRing = true

  let hostCell =
    CaptureCell.create(fun () ->
      if showRing then
        Container()
          .Children(
            MetroProgressRing.mount(
              "loading-ring",
              {
                MetroProgressRing.defaults active with
                    Size = size
              }
            )
          )
      else
        Container())

  let window = Window(Root = hostCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let found = findMountedCellByTypeName(window, "MetroProgressRingCell")
  Assert.True(found.IsSome)
  let ringNode, ringCell = found.Value

  let translateAnims =
    ringCell.GetType().GetField("translateAnims", flags).GetValue(ringCell)
    :?> Anim<Point> array

  let opacityAnims =
    ringCell.GetType().GetField("opacityAnims", flags).GetValue(ringCell)
    :?> Anim<float> array

  Assert.Equal(5, translateAnims.Length)
  Assert.Equal(5, opacityAnims.Length)
  Assert.True(translateAnims[0].Running)

  let stylePassField =
    ringNode.GetType().GetField("<StyleResolvedPass>k__BackingField", flags)

  let passBefore = stylePassField.GetValue(ringNode) :?> int64
  let buildsBefore = hostCell.Builds
  let p0 = translateAnims[0].Value

  pumpMotion(window, 15, 0.03)

  let p1 = translateAnims[0].Value
  let passAfter = stylePassField.GetValue(ringNode) :?> int64

  Assert.NotEqual(p0, p1)
  Assert.Equal(buildsBefore, hostCell.Builds)
  Assert.Equal(passBefore, passAfter)

  // ReducedMotion stops ticking and renders calm static 5-dot ring.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 3)

  for i = 0 to 4 do
    Assert.False(translateAnims[i].Running)
    Assert.False(opacityAnims[i].Running)

  let staticPos0 = translateAnims[0].Value
  let staticPos1 = translateAnims[1].Value
  Assert.NotEqual(staticPos0, staticPos1)
  pumpMotion(window, 10, 0.03)
  Assert.Equal(staticPos0, translateAnims[0].Value)

  // Inactive pauses animations and hides the ring container.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable false
    )
  )

  active <- false
  size <- MetroProgressRingSize.Large
  hostCell.Rebuild()
  pumpFrames(host, 3)

  for i = 0 to 4 do
    Assert.False(translateAnims[i].Running)
    Assert.Equal(0.0, opacityAnims[i].Value)

  let baseEntries =
    readStyleEntriesField(ringNode, "<BaseStyle>k__BackingField")

  let visEntry =
    baseEntries |> Array.tryFindBack(fun s -> s.FieldName = "Visibility")

  Assert.True(visEntry.IsSome)
  Assert.Equal(float32(int Visibility.Hidden), visEntry.Value.A)

  // Verify Small / Normal / Large bounding box sizes via live rebuilds.
  let checkSize(ringSize: MetroProgressRingSize, expectedBox: float) =
    active <- true
    size <- ringSize
    hostCell.Rebuild()
    pumpFrames(host, 2)

    Assert.Equal(
      expectedBox,
      readNodeLength(ringNode, "<Width>k__BackingField"),
      3
    )

    Assert.Equal(
      expectedBox,
      readNodeLength(ringNode, "<Height>k__BackingField"),
      3
    )

  checkSize(MetroProgressRingSize.Small, 24.0)
  checkSize(MetroProgressRingSize.Normal, 36.0)
  checkSize(MetroProgressRingSize.Large, 52.0)

  // Active false -> true restarts orbiting animations and advances positions.
  Assert.True(translateAnims[0].Running)
  let pRestart0 = translateAnims[0].Value
  pumpMotion(window, 10, 0.03)
  Assert.NotEqual(pRestart0, translateAnims[0].Value)

  // Unmounting stops all ring animations cleanly.
  showRing <- false
  hostCell.Rebuild()
  pumpFrames(host, 2)

  for i = 0 to 4 do
    Assert.False(translateAnims[i].Running)
    Assert.False(opacityAnims[i].Running)

[<Fact>]
let ``Warm page of Metro controls stays within bounded allocation budget``() =
  use host = createHost(960, 720)
  let cachedSliderChange = Action<float>(fun _ -> ())
  let noopClick() = ()
  let noopText(_: string) = ()
  let noopBool(_: bool) = ()

  let buildPage() : Blob =
    Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 8.0)
      .Children(
        MetroText.title "Settings",
        MetroText.header "Appearance",
        MetroText.body "Choose your preferred Metro controls.",
        MetroText.secondary "Sharp corners, zero shadows.",
        MetroText.badge "beta",
        MetroButton.standard("Apply", noopClick),
        MetroButton.accent("Save", noopClick),
        MetroButton.hyperlink("Reset defaults", noopClick),
        MetroButton.subtle("Cancel", noopClick),
        MetroTextBox.build(
          {
            MetroTextBox.defaults "Metro" with
                Label = ValueSome "Display name"
          },
          noopText,
          ValueNone
        ),
        MetroToggleSwitch.build(MetroToggleSwitch.defaults true, noopBool),
        MetroCheckBox.build(
          {
            Label = "Enable animations"
            Checked = true
            Disabled = false
          },
          noopBool
        ),
        MetroRadioButton.build(
          {
            Label = "Dark"
            Selected = true
            Disabled = false
          },
          noopClick
        ),
        Container()
          .Children(
            MetroSlider.mount(
              "budget-slider",
              MetroSlider.defaults(0.5, 0.0, 1.0),
              cachedSliderChange
            )
          ),
        Container()
          .Children(
            MetroProgressBar.build(
              "budget-det",
              MetroProgressBar.determinate 0.65
            )
          ),
        Container()
          .Children(
            MetroProgressBar.build("budget-ind", MetroProgressBar.indeterminate)
          ),
        Container()
          .Children(
            MetroProgressRing.mount(
              "budget-ring",
              MetroProgressRing.defaults true
            )
          )
      )

  let mutable measureNext = false
  let mutable measuredBytes = -1L

  let pageCell = {
    new Cell() with
      override _.Build() : Blob =
        if measureNext then
          for _ = 1 to 4 do
            buildPage() |> ignore

          let before = GC.GetAllocatedBytesForCurrentThread()
          let tree = buildPage()
          measuredBytes <- GC.GetAllocatedBytesForCurrentThread() - before
          tree
        else
          buildPage()
  }

  let window = Window(Root = pageCell)
  Metro.attachDefault(window)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  measureNext <- true
  pageCell.Rebuild()
  pumpFrames(host, 2)

  // Full 17-control page builds well under 96 KB even in Debug.
  Assert.True(
    measuredBytes > 0L && measuredBytes < 98304L,
    $"Expected warm Metro page < 98304 bytes, actual: {measuredBytes}"
  )
