module MetroGallery.ControlsTab

open Goo
open FunGoo.Children
open FunGoo.Metro
open MetroGallery.GalleryStore

/// The buttons: four variants, one toggle button, and one disabled button.
/// Each button is a Blob of this tab, so the owner rule rebuilds the tab.
let private buttons(store: GalleryStore) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    FlexWrap = FlexWrap.Wrap,
    Gap = Length.Pixels 12.0,
    AlignItems = AlignItems.Center
  )
    .Children(
      MetroButton.standard(
        "Standard",
        fun () -> store.LastAction <- "Standard clicked"
      ),
      MetroButton.accent(
        "Accent",
        fun () -> store.LastAction <- "Accent clicked"
      ),
      MetroButton.hyperlink(
        "Hyperlink",
        fun () -> store.LastAction <- "Hyperlink clicked"
      ),
      MetroButton.subtle(
        "Subtle",
        fun () -> store.LastAction <- "Subtle clicked"
      ),
      MetroButton.build(
        {
          MetroButton.defaults(if store.TogglePressed then "Pinned" else "Pin") with
              Pressed = ValueSome store.TogglePressed
        },
        fun () -> store.TogglePressed <- not store.TogglePressed
      ),
      MetroButton.build(
        {
          MetroButton.defaults "Disabled" with
              Disabled = true
        },
        ignore
      )
    )

/// One field of the display name and one field of the passcode.
let private fields(store: GalleryStore) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    FlexWrap = FlexWrap.Wrap,
    Gap = Length.Pixels 24.0
  )
    .Children(
      MetroTextBox.build(
        {
          MetroTextBox.defaults store.TextValue with
              Label = ValueSome "Display Name"
              Placeholder = ValueSome "Type here"
              Width = ValueSome(Length.Pixels 280.0)
        },
        (fun value -> store.TextValue <- value),
        ValueSome(fun value -> store.LastAction <- $"Submitted '{value}'")
      ),
      MetroTextBox.build(
        {
          MetroTextBox.defaults store.SecretValue with
              Label = ValueSome "Passcode"
              Password = true
              RevealButton = true
              Width = ValueSome(Length.Pixels 280.0)
        },
        (fun value -> store.SecretValue <- value),
        ValueNone
      )
    )

/// The radio buttons of the density, below one caption.
let private density(store: GalleryStore) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 6.0)
    .Children(
      MetroText.caption "DENSITY",
      MetroRadioButton.build(
        {
          Label = "Compact"
          Selected = store.RadioChoice = "compact"
          Disabled = false
        },
        fun () -> store.RadioChoice <- "compact"
      ),
      MetroRadioButton.build(
        {
          Label = "Comfortable"
          Selected = store.RadioChoice = "comfortable"
          Disabled = false
        },
        fun () -> store.RadioChoice <- "comfortable"
      ),
      MetroRadioButton.build(
        {
          Label = "Touch"
          Selected = store.RadioChoice = "touch"
          Disabled = false
        },
        fun () -> store.RadioChoice <- "touch"
      )
    )

/// The toggle switch, the check box, and the density.
let private toggles(store: GalleryStore) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    FlexWrap = FlexWrap.Wrap,
    Gap = Length.Pixels 32.0,
    AlignItems = AlignItems.FlexStart
  )
    .Children(
      MetroToggleSwitch.build(
        {
          MetroToggleSwitch.defaults store.ToggleEnabled with
              Header = ValueSome "Progress Ring"
        },
        fun value -> store.ToggleEnabled <- value
      ),
      MetroCheckBox.build(
        {
          Label = "Enable sharp borders"
          Checked = store.CheckAccepted
          Disabled = false
        },
        fun value -> store.CheckAccepted <- value
      ),
      density store
    )

/// The slider, the two progress bars, and the progress ring.
let private progress
  (store: GalleryStore, onSliderChange: float -> unit)
  : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    Gap = Length.Pixels 12.0,
    MaxWidth = Length.Pixels 420.0
  )
    .Children(
      MetroText.header "slider & progress",
      Container()
        .Children(
          MetroSlider.mount(
            "gallery-slider",
            {
              MetroSlider.defaults(store.SliderValue, 0.0, 100.0) with
                  Label = ValueSome $"Completion ({int store.SliderValue}%%)"
                  Step = 5.0
            },
            onSliderChange
          )
        ),
      MetroText.caption "DETERMINATE",
      Container()
        .Children(
          MetroProgressBar.build(
            "gallery-det-progress",
            MetroProgressBar.determinate(store.SliderValue / 100.0)
          )
        ),
      MetroText.caption "INDETERMINATE",
      Container()
        .Children(
          MetroProgressBar.build(
            "gallery-ind-progress",
            MetroProgressBar.indeterminate
          )
        ),
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 16.0
      )
        .Children(
          Container()
            .Children(
              MetroProgressRing.mount(
                "gallery-ring",
                MetroProgressRing.defaults store.ToggleEnabled
              )
            ),
          MetroText.secondary(
            (if store.ToggleEnabled then "Ring active" else "Ring paused")
          )
        )
    )

/// The controls tab: one section for each kind of control.
let view(store: GalleryStore, onSliderChange: float -> unit) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 24.0,
    Padding =
      Edges(
        Length.Pixels 4.0,
        Length.Pixels 12.0,
        Length.Pixels 24.0,
        Length.Pixels 0.0
      )
  )
    .Children(
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 10.0)
        .Children(MetroText.header "buttons", buttons store),
      fields store,
      toggles store,
      progress(store, onSliderChange)
    )

/// Creates the controls tab. The tab reads its values in
/// <paramref name="store"/>. A control writes its value into
/// <paramref name="store"/>. The slider is a mounted widget: it gives each
/// change to this tab, so the owner rule rebuilds the tab. Thus a handler
/// writes the value, and no handler calls `Rebuild()`.
let create(store: GalleryStore) : Cell =
  // Created once. F# converts the function to a delegate at each build. The
  // target of each delegate is this function value, so the slider input of
  // two builds compares equal, and an unchanged input does not rebuild the
  // slider.
  let onSliderChange(value: float) = store.SliderValue <- value

  {
    new Cell() with
      override _.Build() : Blob = view(store, onSliderChange)
  }
