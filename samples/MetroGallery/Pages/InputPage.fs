module MetroGallery.Pages.InputPage

open System
open System.Globalization
open Elmish
open Goo
open Goo.Widgets.Inputs
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Mvu

/// Demo data of the input page. Sizes, cities, contacts, ringtones, and time
/// zones keep the pixel-equal values of the retired tabs.
module Content =

  let sizes: MetroComboOption array = [|
    {
      Id = "small"
      Label = "Small"
      Disabled = false
    }
    {
      Id = "medium"
      Label = "Medium"
      Disabled = false
    }
    {
      Id = "large"
      Label = "Large"
      Disabled = false
    }
  |]

  let cities = [|
    "Amsterdam"
    "Barcelona"
    "Berlin"
    "Lisbon"
    "London"
    "Madrid"
    "Paris"
    "Porto"
    "Rome"
  |]

  let suggestionsFor(query: string) : MetroComboOption array =
    cities
    |> Array.filter(fun city ->
      city.StartsWith(query, StringComparison.OrdinalIgnoreCase))
    |> Array.map(fun city -> {
      Id = city
      Label = city
      Disabled = false
    })

  let contactGroups: MetroLongListGroup array = [|
    {
      Key = "a"
      Title = "a"
      Items = [|
        {
          Id = "ada"
          Label = "Ada Lovelace"
          Caption = ValueSome "Analytical Engine"
          Disabled = false
        }
        {
          Id = "alan"
          Label = "Alan Turing"
          Caption = ValueSome "Computation"
          Disabled = false
        }
      |]
    }
    {
      Key = "b"
      Title = "b"
      Items = Array.empty
    }
    {
      Key = "d"
      Title = "d"
      Items = [|
        {
          Id = "don"
          Label = "Don Syme"
          Caption = ValueSome "F# Language"
          Disabled = false
        }
      |]
    }
    {
      Key = "g"
      Title = "g"
      Items = [|
        {
          Id = "grace"
          Label = "Grace Hopper"
          Caption = ValueSome "Compiler Design"
          Disabled = false
        }
      |]
    }
    {
      Key = "m"
      Title = "m"
      Items = [|
        {
          Id = "margaret"
          Label = "Margaret Hamilton"
          Caption = ValueSome "Flight Software"
          Disabled = false
        }
      |]
    }
    {
      Key = "z"
      Title = "z"
      Items = Array.empty
    }
  |]

  let ringtoneItems: MetroListPickerItem array = [|
    {
      Id = "chime"
      Label = "Metro Chime"
      IconName = ValueSome "notifications"
      Disabled = false
    }
    {
      Id = "pulse"
      Label = "Digital Pulse"
      IconName = ValueSome "graphic_eq"
      Disabled = false
    }
    {
      Id = "silent"
      Label = "Silent"
      IconName = ValueSome "notifications_off"
      Disabled = false
    }
  |]

  let timezoneItems: MetroListPickerItem array = [|
    {
      Id = "utc-8"
      Label = "Pacific Time (UTC-08)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc-7"
      Label = "Mountain Time (UTC-07)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc-5"
      Label = "Eastern Time (UTC-05)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc+0"
      Label = "London (UTC+00)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc+1"
      Label = "Lisbon / Porto (UTC+01)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
    {
      Id = "utc+9"
      Label = "Tokyo (UTC+09)"
      IconName = ValueSome "schedule"
      Disabled = false
    }
  |]

/// The model of the act section: the pin button, the ring switch, the check
/// box, the radio choice, and the last action of the section.
[<Struct; NoComparison>]
type ActModel = {
  TogglePressed: bool
  ToggleEnabled: bool
  CheckAccepted: bool
  RadioChoice: string
  LastAction: string
}

/// One message of the act section. Each control callback posts one message.
type ActMsg =
  | ActionNoted of text: string
  | PinClicked
  | RingToggled of enabled: bool
  | CheckChanged of isChecked: bool
  | RadioSelected of id: string
  | Stop

/// The act section cell: a `MvuCell` over the action controls. The headless
/// check of the gallery reads the state through these members.
[<AbstractClass>]
type ActSectionCell
  (
    post: (unit -> unit) -> bool,
    stop: ActMsg,
    program: Program<unit, ActModel, ActMsg, Blob>
  ) =
  inherit MvuCell<unit, ActModel, ActMsg>(post, stop, program, ())

  /// Text of the last action of the section.
  abstract LastAction: string

  /// The number of builds, for the headless check.
  abstract Builds: int

/// The first model of the act section.
let actInit() : ActModel * Cmd<ActMsg> =
  {
    TogglePressed = false
    ToggleEnabled = true
    CheckAccepted = true
    RadioChoice = "compact"
    LastAction = "Ready"
  },
  Cmd.none

/// Applies one message to the model of the act section.
let actUpdate(msg: ActMsg, model: ActModel) : ActModel * Cmd<ActMsg> =
  match msg with
  | ActionNoted text -> { model with LastAction = text }, Cmd.none
  | PinClicked ->
    {
      model with
          TogglePressed = not model.TogglePressed
    },
    Cmd.none
  | RingToggled enabled ->
    {
      model with
          ToggleEnabled = enabled
          LastAction = if enabled then "Ring running" else "Ring paused"
    },
    Cmd.none
  | CheckChanged isChecked ->
    {
      model with
          CheckAccepted = isChecked
          LastAction =
            if isChecked then
              "Sharp borders on"
            else
              "Sharp borders off"
    },
    Cmd.none
  | RadioSelected id ->
    {
      model with
          RadioChoice = id
          LastAction = $"Density: {id}"
    },
    Cmd.none
  | Stop -> model, Cmd.none

/// The buttons of the act section: four variants, one pin toggle button, and
/// one disabled button.
let actButtons(model: ActModel, dispatch: ActMsg -> unit) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    FlexWrap = FlexWrap.Wrap,
    Gap = Length.Pixels 12.0,
    AlignItems = AlignItems.Center
  )
    .Children(
      MetroTilt.mount(
        "act-btn-standard",
        MetroButton.standard(
          "Standard",
          fun () -> dispatch(ActionNoted "Standard clicked")
        )
      ),
      MetroTilt.mount(
        "act-btn-accent",
        MetroButton.accent(
          "Accent",
          fun () -> dispatch(ActionNoted "Accent clicked")
        )
      ),
      MetroTilt.mount(
        "act-btn-hyperlink",
        MetroButton.hyperlink(
          "Hyperlink",
          fun () -> dispatch(ActionNoted "Hyperlink clicked")
        )
      ),
      MetroTilt.mount(
        "act-btn-subtle",
        MetroButton.subtle(
          "Subtle",
          fun () -> dispatch(ActionNoted "Subtle clicked")
        )
      ),
      MetroTilt.mount(
        "act-btn-toggle",
        MetroButton.build(
          {
            MetroButton.defaults(
              if model.TogglePressed then "Pinned" else "Pin"
            ) with
                Pressed = ValueSome model.TogglePressed
          },
          fun () -> dispatch PinClicked
        )
      ),
      MetroTilt.mount(
        "act-btn-disabled",
        MetroButton.build(
          {
            MetroButton.defaults "Disabled" with
                Disabled = true
          },
          ignore
        )
      )
    )

/// The radio buttons of the density, below one caption.
let actDensity(model: ActModel, dispatch: ActMsg -> unit) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 6.0)
    .Children(
      MetroText.caption "DENSITY",
      MetroRadioButton.build(
        {
          MetroRadioButton.defaults "Compact" with
              Selected = model.RadioChoice = "compact"
        },
        fun () -> dispatch(RadioSelected "compact")
      ),
      MetroRadioButton.build(
        {
          MetroRadioButton.defaults "Comfortable" with
              Selected = model.RadioChoice = "comfortable"
        },
        fun () -> dispatch(RadioSelected "comfortable")
      ),
      MetroRadioButton.build(
        {
          MetroRadioButton.defaults "Touch" with
              Selected = model.RadioChoice = "touch"
        },
        fun () -> dispatch(RadioSelected "touch")
      )
    )

/// The toggle switch, the check box, and the density.
let actToggles(model: ActModel, dispatch: ActMsg -> unit) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    FlexWrap = FlexWrap.Wrap,
    Gap = Length.Pixels 32.0,
    AlignItems = AlignItems.FlexStart
  )
    .Children(
      MetroToggleSwitch.build(
        {
          MetroToggleSwitch.defaults model.ToggleEnabled with
              Header = ValueSome "Progress Ring"
        },
        fun value -> dispatch(RingToggled value)
      ),
      MetroCheckBox.build(
        {
          MetroCheckBox.defaults "Enable sharp borders" with
              Checked = model.CheckAccepted
        },
        fun value -> dispatch(CheckChanged value)
      ),
      actDensity(model, dispatch)
    )

/// The progress ring of the act section. It reads the toggle switch.
let actRing(model: ActModel) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    AlignItems = AlignItems.Center,
    Gap = Length.Pixels 16.0
  )
    .Children(
      Container()
        .Children(
          MetroProgressRing.mount(
            "act-ring",
            MetroProgressRing.defaults model.ToggleEnabled
          )
        ),
      MetroText.secondary(
        (if model.ToggleEnabled then "Ring active" else "Ring paused")
      )
    )

/// The tree of the act section.
let actView(model: ActModel, dispatch: ActMsg -> unit) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
    .Children(
      MetroText.subheader "act",
      actButtons(model, dispatch),
      actToggles(model, dispatch),
      actRing model,
      MetroText.caption model.LastAction
    )

/// Creates the act section. Each mount makes its own `MvuCell`, so a model
/// change rebuilds only this section.
let createActSection(post: (unit -> unit) -> bool) : ActSectionCell =
  let mutable builds = 0

  let view (model: ActModel) (dispatch: ActMsg -> unit) : Blob =
    builds <- builds + 1
    actView(model, dispatch)

  let program =
    Program.mkProgram actInit (fun msg model -> actUpdate(msg, model)) view
    |> Program.withTermination (fun msg -> msg = Stop) ignore

  let mutable self: ActSectionCell voption = ValueNone

  let current() : ActModel = (self |> ValueOption.get).Current

  let cell = {
    new ActSectionCell(post, Stop, program) with
      override _.LastAction = (current()).LastAction
      override _.Builds = builds
  }

  self <- ValueSome cell
  cell

/// The model of the enter section: the text fields, the number box, the
/// rating, the combo box, the auto-suggest box, the slider, and the last
/// action of the section. The progress bar reads the committed slider value.
[<Struct; NoComparison>]
type EnterModel = {
  TextValue: string
  SecretValue: string
  Quantity: float
  Rating: float
  SizeSelected: string
  CitySelected: string
  CityQuery: string
  SliderValue: float
  LastAction: string
}

/// One message of the enter section. The slider posts one message per change,
/// and `update` commits it only when the stepped value differs, so the page
/// does not rebuild per pixel.
type EnterMsg =
  | TextChanged of value: string
  | SecretChanged of value: string
  | Submitted of value: string
  | QuantityChanged of value: float
  | RatingChanged of value: float
  | SizeSelected of id: string
  | CityEvent of event: MetroComboBoxEvent
  | SliderChanged of value: float
  | Stop

/// The enter section cell: a `MvuCell` over the entry controls. The headless
/// check of the gallery reads the state and the widget inputs through these
/// members.
[<AbstractClass>]
type EnterSectionCell
  (
    post: (unit -> unit) -> bool,
    stop: EnterMsg,
    program: Program<unit, EnterModel, EnterMsg, Blob>
  ) =
  inherit MvuCell<unit, EnterModel, EnterMsg>(post, stop, program, ())

  /// Identifier of the selected size.
  abstract SizeSelected: string

  /// Identifier of the selected city.
  abstract CitySelected: string

  /// Text of the city entry.
  abstract CityQuery: string

  /// Committed value of the slider.
  abstract SliderValue: float

  /// Text of the last action of the section.
  abstract LastAction: string

  /// Widget input of the size combo box.
  abstract SizeInput: ComboBoxInput

  /// Widget input of the city auto-suggest box.
  abstract CityInput: ComboBoxInput

  /// The number of builds, for the headless check.
  abstract Builds: int

/// The environment of the enter program: the slider commit callback and the
/// mounted control factories. `createEnterSection` makes them once, so each
/// mount reuses its callbacks.
type EnterEnv = {
  OnSliderCommit: float -> unit
  SizeFactory: IMetroComboBoxFactory
  CityFactory: IMetroComboBoxFactory
  NumberCell: MetroNumberBoxCell
  RatingCell: MetroRatingCell
  SliderCell: MetroSliderCell
}

/// The first model of the enter section.
let enterInit() : EnterModel * Cmd<EnterMsg> =
  {
    TextValue = "Modern UI"
    SecretValue = "metro123"
    Quantity = 12.5
    Rating = 3.0
    SizeSelected = "medium"
    CitySelected = ""
    CityQuery = ""
    SliderValue = 65.0
    LastAction = "Ready"
  },
  Cmd.none

/// Applies one city event to the model of the enter section. A new query
/// starts a new search, so it clears the selection.
let applyCityEvent(model: EnterModel, event: MetroComboBoxEvent) : EnterModel =
  match event with
  | MetroComboBoxEvent.Selected id -> {
      model with
          CitySelected = id
          CityQuery = id
    }
  | MetroComboBoxEvent.QueryChanged query -> {
      model with
          CityQuery = query
          CitySelected = ""
    }
  | MetroComboBoxEvent.OpenChanged _ -> model

/// Applies one message to the model of the enter section. A changed slider
/// value commits to the shell through `OnSliderCommit`, so the hub reads the
/// live progress without owning the slider.
let enterUpdate
  (env: EnterEnv, msg: EnterMsg, model: EnterModel)
  : EnterModel * Cmd<EnterMsg> =
  match msg with
  | TextChanged value -> { model with TextValue = value }, Cmd.none
  | SecretChanged value -> { model with SecretValue = value }, Cmd.none
  | Submitted value ->
    {
      model with
          LastAction = $"Submitted '{value}'"
    },
    Cmd.none
  | QuantityChanged value -> { model with Quantity = value }, Cmd.none
  | RatingChanged value -> { model with Rating = value }, Cmd.none
  | SizeSelected id -> { model with SizeSelected = id }, Cmd.none
  | CityEvent event -> applyCityEvent(model, event), Cmd.none
  | SliderChanged value ->
    if value = model.SliderValue then
      model, Cmd.none
    else
      env.OnSliderCommit value
      { model with SliderValue = value }, Cmd.none
  | Stop -> model, Cmd.none

/// The props of the size combo box for <paramref name="model"/>.
let enterSizeProps(model: EnterModel) : MetroComboBoxProps = {
  MetroComboBox.defaults(Content.sizes, model.SizeSelected) with
      Placeholder = ValueSome "size"
      AccessibilityName = ValueSome "size"
      Width = ValueSome(Length.Pixels 200.0)
}

/// The props of the city auto-suggest box for <paramref name="model"/>.
let enterCityProps(model: EnterModel) : MetroComboBoxProps = {
  MetroComboBox.autoSuggest(
    Content.suggestionsFor model.CityQuery,
    model.CityQuery
  ) with
      SelectedId = model.CitySelected
      Placeholder = ValueSome "city"
      SearchPlaceholder = ValueSome "city"
      EmptyText = ValueSome "No city matches"
      AccessibilityName = ValueSome "city"
      Width = ValueSome(Length.Pixels 200.0)
}

/// One field of the display name and one field of the passcode.
let enterFields(model: EnterModel, dispatch: EnterMsg -> unit) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    FlexWrap = FlexWrap.Wrap,
    Gap = Length.Pixels 24.0
  )
    .Children(
      MetroTextBox.build(
        {
          MetroTextBox.defaults model.TextValue with
              Label = ValueSome "Display Name"
              Placeholder = ValueSome "Type here"
              Width = ValueSome(Length.Pixels 280.0)
        },
        (fun value -> dispatch(TextChanged value)),
        ValueSome(fun value -> dispatch(Submitted value))
      ),
      MetroTextBox.build(
        {
          MetroTextBox.defaults model.SecretValue with
              Label = ValueSome "Passcode"
              Password = true
              RevealButton = true
              Width = ValueSome(Length.Pixels 280.0)
        },
        (fun value -> dispatch(SecretChanged value)),
        ValueNone
      )
    )

/// The number box and the rating control.
let enterNumberRating(env: EnterEnv, model: EnterModel) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    Width = Length.Percent 100.0,
    FlexShrink = 0.0,
    AlignItems = AlignItems.FlexStart,
    Gap = Length.Pixels 48.0
  )
    .Children(
      env.NumberCell.Mount(
        "enter-number",
        {
          MetroNumberBox.defaults model.Quantity with
              Minimum = ValueSome 0.0
              Maximum = ValueSome 100.0
              Step = ValueSome 0.5
              Label = ValueSome "quantity"
              Width = ValueSome(Length.Pixels 200.0)
        }
      ),
      env.RatingCell.Mount(
        "enter-rating",
        {
          MetroRating.defaults model.Rating with
              AccessibilityName = ValueSome "rating"
        }
      )
    )

/// The size combo box and the city auto-suggest box.
let enterCombos(env: EnterEnv, model: EnterModel) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    Width = Length.Percent 100.0,
    FlexShrink = 0.0,
    AlignItems = AlignItems.FlexStart,
    Gap = Length.Pixels 48.0
  )
    .Children(
      env.SizeFactory.Mount("enter-combo", enterSizeProps model),
      env.CityFactory.Mount("enter-suggest", enterCityProps model)
    )

/// The slider and the progress bars. The determinate bar reads the committed
/// slider value.
let enterSliderProgress(env: EnterEnv, model: EnterModel) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    Gap = Length.Pixels 12.0,
    MaxWidth = Length.Pixels 420.0
  )
    .Children(
      Container()
        .Children(
          env.SliderCell.Mount(
            "enter-slider",
            {
              MetroSlider.defaults(model.SliderValue, 0.0, 100.0) with
                  Label = ValueSome $"Completion ({int model.SliderValue}%%)"
                  Step = 5.0
            }
          )
        ),
      MetroText.caption "DETERMINATE",
      Container()
        .Children(
          MetroProgressBar.build(
            "enter-det-progress",
            {
              MetroProgressBar.determinate model.SliderValue with
                  Maximum = 100.0
                  ShowLabel = true
            }
          )
        ),
      MetroText.caption "INDETERMINATE",
      Container()
        .Children(
          MetroProgressBar.build(
            "enter-ind-progress",
            MetroProgressBar.indeterminate
          )
        )
    )

/// The tree of the enter section.
let enterView
  (env: EnterEnv, model: EnterModel, dispatch: EnterMsg -> unit)
  : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
    .Children(
      MetroText.subheader "enter",
      enterFields(model, dispatch),
      enterNumberRating(env, model),
      enterCombos(env, model),
      enterSliderProgress(env, model),
      MetroText.caption model.LastAction
    )

/// Creates the enter section. Each mount makes its own `MvuCell`, so a model
/// change rebuilds only this section. A committed slider value reaches the
/// shell through <paramref name="onSliderCommit"/>.
let createEnterSection
  (post: (unit -> unit) -> bool, onSliderCommit: float -> unit)
  : EnterSectionCell =
  let mutable builds = 0
  let mutable dispatchSlot: EnterMsg -> unit = fun _ -> ()
  let postMsg(msg: EnterMsg) : unit = dispatchSlot msg

  // One factory or cell per control: each control has its own values, and
  // the combo events do not name the control. A selected city fills the
  // entry with its name; a new query clears the selection.
  let env: EnterEnv = {
    OnSliderCommit = onSliderCommit
    SizeFactory =
      MetroComboBox.createFactory(function
        | MetroComboBoxEvent.Selected id -> postMsg(SizeSelected id)
        | _ -> ())
    CityFactory =
      MetroComboBox.createFactory(fun event -> postMsg(CityEvent event))
    NumberCell =
      MetroNumberBox.create(fun value -> postMsg(QuantityChanged value))
    RatingCell = MetroRating.create(fun value -> postMsg(RatingChanged value))
    SliderCell = MetroSlider.create(fun value -> postMsg(SliderChanged value))
  }

  let view (model: EnterModel) (dispatch: EnterMsg -> unit) : Blob =
    builds <- builds + 1
    enterView(env, model, dispatch)

  let program =
    Program.mkProgram
      enterInit
      (fun msg model -> enterUpdate(env, msg, model))
      view
    |> Program.withTermination (fun msg -> msg = Stop) ignore

  let mutable self: EnterSectionCell voption = ValueNone

  let current() : EnterModel = (self |> ValueOption.get).Current

  let cell = {
    new EnterSectionCell(post, Stop, program) with
      override _.SizeSelected = (current()).SizeSelected
      override _.CitySelected = (current()).CitySelected
      override _.CityQuery = (current()).CityQuery
      override _.SliderValue = (current()).SliderValue
      override _.LastAction = (current()).LastAction

      override _.SizeInput =
        env.SizeFactory.CreateInput(enterSizeProps(current()))

      override _.CityInput =
        env.CityFactory.CreateInput(enterCityProps(current()))

      override _.Builds = builds
  }

  self <- ValueSome cell
  dispatchSlot <- cell.Dispatch
  cell

/// The model of the pick section: one selected date shared by the calendar
/// and the four date pickers, one selected time shared by the time pickers,
/// the clock format, the list pickers, the long list, and the last action.
[<Struct; NoComparison>]
type PickModel = {
  SelectedDate: DateOnly
  CalendarMonth: DateOnly
  DateOpen: bool
  SelectedTime: TimeOnly
  TimeOpen: bool
  UseTwelveHour: bool
  ContactSelected: string array
  JumpOpen: bool
  RingtoneId: string
  RingtoneOpen: bool
  TimezoneId: string
  TimezoneOpen: bool
  LastAction: string
}

/// One message of the pick section.
type PickMsg =
  | RollerDateChanged of date: DateOnly
  | RollerTimeChanged of time: TimeOnly
  | DatePickerEvent of event: MetroDatePickerEvent
  | TimePickerEvent of event: MetroTimePickerEvent
  | CalendarEvent of event: MetroCalendarEvent
  | DropdownEvent of event: MetroCalendarDatePickerEvent
  | ClockFormatChanged of enabled: bool
  | LongListEvent of event: MetroLongListEvent
  | InlinePickerEvent of event: MetroListPickerEvent
  | OverlayPickerEvent of event: MetroListPickerEvent
  | Stop

/// The pick section cell: a `MvuCell` over the date, time, and list pickers.
/// The headless check of the gallery reads the state and drives the cells
/// through these members.
[<AbstractClass>]
type PickSectionCell
  (
    post: (unit -> unit) -> bool,
    stop: PickMsg,
    program: Program<unit, PickModel, PickMsg, Blob>
  ) =
  inherit MvuCell<unit, PickModel, PickMsg>(post, stop, program, ())

  /// The selected date shared by the calendar and the date pickers.
  abstract SelectedDate: DateOnly

  /// The selected time shared by the time pickers.
  abstract SelectedTime: TimeOnly

  /// Text of the last action of the section.
  abstract LastAction: string

  /// Widget input of the month calendar.
  abstract CalendarInput: CalendarInput

  /// Widget input of the calendar date picker.
  abstract CalendarDatePickerInput: DatePickerInput

  /// The mounted inline date roller, for the headless check.
  abstract DateRoller: MetroDatePickerRollerCell voption

  /// The mounted inline time roller, for the headless check.
  abstract TimeRoller: MetroTimePickerRollerCell voption

  /// The mounted compact date picker, for the headless check.
  abstract DatePicker: MetroDatePickerCell voption

  /// The mounted compact time picker, for the headless check.
  abstract TimePicker: MetroTimePickerCell voption

  /// The mounted long list selector, for the headless check.
  abstract LongList: MetroLongListSelectorCell voption

  /// The mounted inline list picker, for the headless check.
  abstract InlinePicker: MetroListPickerCell voption

  /// The number of builds, for the headless check.
  abstract Builds: int

/// The factories of the pickers of the pick section. `createPickSection`
/// makes them once, so each mount reuses its callbacks.
type PickFactories = {
  DateRoller: unit -> MetroDatePickerRollerCell
  TimeRoller: unit -> MetroTimePickerRollerCell
  DatePicker: unit -> MetroDatePickerCell
  TimePicker: unit -> MetroTimePickerCell
  LongList: unit -> MetroLongListSelectorCell
  InlinePicker: unit -> MetroListPickerCell
  OverlayPicker: unit -> MetroListPickerCell
  Calendar: IMetroCalendarFactory
  CalendarDatePicker: IMetroCalendarDatePickerFactory
}

/// The first model of the pick section.
let pickInit() : PickModel * Cmd<PickMsg> =
  {
    SelectedDate = DateOnly(2026, 11, 24)
    CalendarMonth = DateOnly(2026, 11, 1)
    DateOpen = false
    SelectedTime = TimeOnly(14, 30)
    TimeOpen = false
    UseTwelveHour = false
    ContactSelected = [| "ada" |]
    JumpOpen = false
    RingtoneId = "chime"
    RingtoneOpen = false
    TimezoneId = "utc+1"
    TimezoneOpen = false
    LastAction = "Ready"
  },
  Cmd.none

/// Applies one compact date picker event to the model.
let applyPickDatePicker
  (model: PickModel, event: MetroDatePickerEvent)
  : PickModel =
  match event with
  | MetroDatePickerEvent.DateCommitted date -> {
      model with
          SelectedDate = date
          LastAction = $"Picked date: {MetroDatePicker.formatIso date}"
    }
  | MetroDatePickerEvent.OpenChanged isOpen -> { model with DateOpen = isOpen }

/// Applies one compact time picker event to the model.
let applyPickTimePicker
  (model: PickModel, event: MetroTimePickerEvent)
  : PickModel =
  let format =
    if model.UseTwelveHour then
      MetroClockFormat.TwelveHour
    else
      MetroClockFormat.TwentyFourHour

  match event with
  | MetroTimePickerEvent.TimeCommitted time -> {
      model with
          SelectedTime = time
          LastAction =
            $"Picked time: {MetroTimePickerRoller.formatTime(time, format)}"
    }
  | MetroTimePickerEvent.OpenChanged isOpen -> { model with TimeOpen = isOpen }

/// Applies one calendar event to the model.
let applyPickCalendar(model: PickModel, event: MetroCalendarEvent) : PickModel =
  match event with
  | MetroCalendarEvent.DateSelected date -> {
      model with
          SelectedDate = date
          LastAction = $"Calendar date: {MetroDatePicker.formatIso date}"
    }
  | MetroCalendarEvent.MonthChanged firstOfMonth -> {
      model with
          CalendarMonth = firstOfMonth
          LastAction =
            $"Calendar month: {MetroDatePicker.formatIso firstOfMonth}"
    }

/// Applies one calendar date picker event to the model. A cleared entry keeps
/// the selected date and notes the clear.
let applyPickDropdown
  (model: PickModel, event: MetroCalendarDatePickerEvent)
  : PickModel =
  match event with
  | MetroCalendarDatePickerEvent.ValueChanged maybeDate ->
    match maybeDate with
    | ValueSome date -> {
        model with
            SelectedDate = date
            LastAction = $"Dropdown date: {MetroDatePicker.formatIso date}"
      }
    | ValueNone -> {
        model with
            LastAction = "Dropdown date: cleared"
      }
  | MetroCalendarDatePickerEvent.OpenChanged _ -> model
  | MetroCalendarDatePickerEvent.InvalidInput raw -> {
      model with
          LastAction = $"Invalid date: {raw}"
    }

/// Applies one long list event to the model.
let applyPickLongList(model: PickModel, event: MetroLongListEvent) : PickModel =
  match event with
  | MetroLongListEvent.SelectionChanged ids ->
    let chosen = if ids.Length > 0 then ids[0] else "none"

    {
      model with
          ContactSelected = ids
          LastAction = $"Contact: {chosen}"
    }
  | MetroLongListEvent.ItemClick id -> {
      model with
          LastAction = $"Contact: {id}"
    }
  | MetroLongListEvent.JumpOpenChanged isOpen -> {
      model with
          JumpOpen = isOpen
    }
  | MetroLongListEvent.GroupJumped key -> {
      model with
          JumpOpen = false
          LastAction = $"Jumped to group {key}"
    }

/// Applies one inline picker event to the model.
let applyPickInline(model: PickModel, event: MetroListPickerEvent) : PickModel =
  match event with
  | MetroListPickerEvent.Selected id -> {
      model with
          RingtoneId = id
          LastAction = $"Ringtone: {id}"
    }
  | MetroListPickerEvent.OpenChanged isOpen -> {
      model with
          RingtoneOpen = isOpen
    }

/// Applies one overlay picker event to the model.
let applyPickOverlay
  (model: PickModel, event: MetroListPickerEvent)
  : PickModel =
  match event with
  | MetroListPickerEvent.Selected id -> {
      model with
          TimezoneId = id
          LastAction = $"Timezone: {id}"
    }
  | MetroListPickerEvent.OpenChanged isOpen -> {
      model with
          TimezoneOpen = isOpen
    }

/// Applies one message to the model of the pick section.
let pickUpdate(msg: PickMsg, model: PickModel) : PickModel * Cmd<PickMsg> =
  match msg with
  | RollerDateChanged date ->
    {
      model with
          SelectedDate = date
          LastAction = $"Roller date: {MetroDatePicker.formatIso date}"
    },
    Cmd.none
  | RollerTimeChanged time ->
    let format =
      if model.UseTwelveHour then
        MetroClockFormat.TwelveHour
      else
        MetroClockFormat.TwentyFourHour

    {
      model with
          SelectedTime = time
          LastAction =
            $"Roller time: {MetroTimePickerRoller.formatTime(time, format)}"
    },
    Cmd.none
  | DatePickerEvent event -> applyPickDatePicker(model, event), Cmd.none
  | TimePickerEvent event -> applyPickTimePicker(model, event), Cmd.none
  | CalendarEvent event -> applyPickCalendar(model, event), Cmd.none
  | DropdownEvent event -> applyPickDropdown(model, event), Cmd.none
  | ClockFormatChanged enabled ->
    {
      model with
          UseTwelveHour = enabled
          LastAction = if enabled then "12-hour format" else "24-hour format"
    },
    Cmd.none
  | LongListEvent event -> applyPickLongList(model, event), Cmd.none
  | InlinePickerEvent event -> applyPickInline(model, event), Cmd.none
  | OverlayPickerEvent event -> applyPickOverlay(model, event), Cmd.none
  | Stop -> model, Cmd.none

/// The props of the month calendar for <paramref name="model"/>. The calendar
/// leads the pick section.
let pickCalendarProps(model: PickModel) : MetroCalendarProps = {
  MetroCalendar.defaults(ValueSome model.SelectedDate) with
      Month = ValueSome model.CalendarMonth
      Today = ValueSome(DateOnly(2026, 11, 24))
      Width = ValueSome(Length.Pixels 288.0)
      AccessibilityName = ValueSome "Project schedule calendar"
}

/// The props of the calendar date picker for <paramref name="model"/>.
let pickDropdownProps(model: PickModel) : MetroCalendarDatePickerProps = {
  MetroCalendarDatePicker.defaults(ValueSome model.SelectedDate) with
      Today = ValueSome(DateOnly(2026, 11, 24))
      Placeholder =
        ValueSome(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern)
      Width = ValueSome(Length.Percent 100.0)
      AccessibilityName = ValueSome "Calendar date picker"
}

/// The props of the inline date roller for <paramref name="model"/>.
let pickDateRollerInput(model: PickModel) : MetroDatePickerRollerInput = {
  MetroDatePickerRoller.defaults model.SelectedDate with
      Label = ValueSome "INLINE DATE ROLLER"
}

/// The props of the inline time roller for <paramref name="model"/>.
let pickTimeRollerInput(model: PickModel) : MetroTimePickerRollerInput =
  let format =
    if model.UseTwelveHour then
      MetroClockFormat.TwelveHour
    else
      MetroClockFormat.TwentyFourHour

  {
    MetroTimePickerRoller.defaults model.SelectedTime with
        Format = format
        MinuteIncrement = 5
        Label = ValueSome "INLINE TIME ROLLER (5-MIN STEP)"
  }

/// The props of the compact date picker for <paramref name="model"/>.
let pickDatePickerInput(model: PickModel) : MetroDatePickerInput = {
  MetroDatePicker.defaults(ValueSome model.SelectedDate) with
      Open = model.DateOpen
      FallbackDate = model.SelectedDate
      Label = ValueSome "ROLLER DATE PICKER"
}

/// The props of the compact time picker for <paramref name="model"/>.
let pickTimePickerInput(model: PickModel) : MetroTimePickerInput =
  let format =
    if model.UseTwelveHour then
      MetroClockFormat.TwelveHour
    else
      MetroClockFormat.TwentyFourHour

  {
    MetroTimePicker.defaults(ValueSome model.SelectedTime) with
        Open = model.TimeOpen
        FallbackTime = model.SelectedTime
        Format = format
        MinuteIncrement = 5
        Label = ValueSome "ROLLER TIME PICKER"
  }

/// The props of the long list for <paramref name="model"/>.
let pickLongListProps(model: PickModel) : MetroLongListInput = {
  MetroLongListSelector.defaults Content.contactGroups with
      SelectedIds = model.ContactSelected
      JumpOpen = model.JumpOpen
      Height = ValueSome(Length.Pixels 340.0)
      AccessibilityName = ValueSome "Contacts"
}

/// The props of the inline ringtone picker for <paramref name="model"/>.
let pickInlineProps(model: PickModel) : MetroListPickerInput = {
  MetroListPicker.defaults(Content.ringtoneItems, model.RingtoneId) with
      Open = model.RingtoneOpen
      Header = ValueSome "RINGTONE (INLINE <= 5)"
}

/// The props of the overlay time zone picker for <paramref name="model"/>.
let pickOverlayProps(model: PickModel) : MetroListPickerInput = {
  MetroListPicker.defaults(Content.timezoneItems, model.TimezoneId) with
      Open = model.TimezoneOpen
      Header = ValueSome "TIME ZONE (MODAL > 5)"
      Title = ValueSome "CHOOSE TIME ZONE"
}

/// One titled group of the pick section: a title in the type ramp, then the
/// content. Whitespace groups the section; no boxes surround the groups.
let pickGroup(title: string, content: Blob) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.caption title, content)

/// The tree of the pick section. The calendar leads, then the rollers, the
/// popup pickers, the list pickers, and the long list.
let pickView
  (factories: PickFactories, model: PickModel, postMsg: PickMsg -> unit)
  : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 20.0)
    .Children(
      MetroText.subheader "pick",
      pickGroup(
        "MONTH CALENDAR",
        Container(
          FlexDirection = FlexDirection.Row,
          FlexWrap = FlexWrap.Wrap,
          Gap = Length.Pixels 24.0,
          AlignItems = AlignItems.FlexStart
        )
          .Children(
            factories.Calendar.Mount("pick-calendar", pickCalendarProps model),
            Container(
              FlexDirection = FlexDirection.Column,
              Gap = Length.Pixels 6.0,
              MaxWidth = Length.Pixels 288.0
            )
              .Children(
                MetroText.caption "CALENDAR DATE PICKER",
                factories.CalendarDatePicker.Mount(
                  "pick-calendar-date-picker",
                  pickDropdownProps model
                )
              )
          )
      ),
      pickGroup(
        "LOOPING ROLLERS",
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 18.0
        )
          .Children(
            MetroDatePickerRoller.mount(
              "pick-date-roller",
              pickDateRollerInput model,
              factories.DateRoller
            ),
            MetroTimePickerRoller.mount(
              "pick-time-roller",
              pickTimeRollerInput model,
              factories.TimeRoller
            ),
            MetroToggleSwitch.build(
              {
                MetroToggleSwitch.defaults model.UseTwelveHour with
                    Header = ValueSome "Clock format"
                    OnLabel = "12-hour (AM/PM)"
                    OffLabel = "24-hour"
              },
              fun enabled -> postMsg(ClockFormatChanged enabled)
            )
          )
      ),
      pickGroup(
        "POPUP PICKERS",
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 14.0
        )
          .Children(
            MetroDatePicker.mount(
              "pick-date-picker",
              pickDatePickerInput model,
              factories.DatePicker
            ),
            MetroTimePicker.mount(
              "pick-time-picker",
              pickTimePickerInput model,
              factories.TimePicker
            )
          )
      ),
      pickGroup(
        "LIST PICKERS",
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 14.0
        )
          .Children(
            MetroListPicker.mount(
              "pick-picker-inline",
              pickInlineProps model,
              factories.InlinePicker
            ),
            MetroListPicker.mount(
              "pick-picker-overlay",
              pickOverlayProps model,
              factories.OverlayPicker
            )
          )
      ),
      pickGroup(
        "LONG LIST SELECTOR",
        MetroLongListSelector.mount(
          "pick-long-list",
          pickLongListProps model,
          factories.LongList
        )
      ),
      MetroText.caption model.LastAction
    )

/// Creates the pick section. Each mount makes its own `MvuCell`, so a model
/// change rebuilds only this section.
let createPickSection(post: (unit -> unit) -> bool) : PickSectionCell =
  let mutable builds = 0
  let mutable dispatchSlot: PickMsg -> unit = fun _ -> ()
  let postMsg(msg: PickMsg) : unit = dispatchSlot msg

  let mutable dateRollerRef: MetroDatePickerRollerCell voption = ValueNone
  let mutable timeRollerRef: MetroTimePickerRollerCell voption = ValueNone
  let mutable datePickerRef: MetroDatePickerCell voption = ValueNone
  let mutable timePickerRef: MetroTimePickerCell voption = ValueNone
  let mutable longListRef: MetroLongListSelectorCell voption = ValueNone
  let mutable inlineRef: MetroListPickerCell voption = ValueNone

  // One factory per control: the events do not name the control, so two
  // controls never share one factory.
  let factories: PickFactories = {
    DateRoller =
      fun () ->
        let cell =
          MetroDatePickerRoller.create(fun date ->
            postMsg(RollerDateChanged date))

        dateRollerRef <- ValueSome cell
        cell
    TimeRoller =
      fun () ->
        let cell =
          MetroTimePickerRoller.create(fun time ->
            postMsg(RollerTimeChanged time))

        timeRollerRef <- ValueSome cell
        cell
    DatePicker =
      fun () ->
        let cell =
          MetroDatePicker.create(fun event -> postMsg(DatePickerEvent event))

        datePickerRef <- ValueSome cell
        cell
    TimePicker =
      fun () ->
        let cell =
          MetroTimePicker.create(fun event -> postMsg(TimePickerEvent event))

        timePickerRef <- ValueSome cell
        cell
    LongList =
      fun () ->
        let cell =
          MetroLongListSelector.create(fun event ->
            postMsg(LongListEvent event))

        longListRef <- ValueSome cell
        cell
    InlinePicker =
      fun () ->
        let cell =
          MetroListPicker.create(fun event -> postMsg(InlinePickerEvent event))

        inlineRef <- ValueSome cell
        cell
    OverlayPicker =
      fun () ->
        MetroListPicker.create(fun event -> postMsg(OverlayPickerEvent event))
    Calendar =
      MetroCalendar.createFactory(fun event -> postMsg(CalendarEvent event))
    CalendarDatePicker =
      MetroCalendarDatePicker.createFactory(fun event ->
        postMsg(DropdownEvent event))
  }

  let view (model: PickModel) (dispatch: PickMsg -> unit) : Blob =
    builds <- builds + 1
    pickView(factories, model, dispatch)

  let program =
    Program.mkProgram pickInit (fun msg model -> pickUpdate(msg, model)) view
    |> Program.withTermination (fun msg -> msg = Stop) ignore

  let mutable self: PickSectionCell voption = ValueNone

  let current() : PickModel = (self |> ValueOption.get).Current

  let cell = {
    new PickSectionCell(post, Stop, program) with
      override _.SelectedDate = (current()).SelectedDate
      override _.SelectedTime = (current()).SelectedTime
      override _.LastAction = (current()).LastAction

      override _.CalendarInput =
        factories.Calendar.CreateInput(pickCalendarProps(current()))

      override _.CalendarDatePickerInput =
        factories.CalendarDatePicker.CreateInput(pickDropdownProps(current()))

      override _.DateRoller = dateRollerRef
      override _.TimeRoller = timeRollerRef
      override _.DatePicker = datePickerRef
      override _.TimePicker = timePickerRef
      override _.LongList = longListRef
      override _.InlinePicker = inlineRef
      override _.Builds = builds
  }

  self <- ValueSome cell
  dispatchSlot <- cell.Dispatch
  cell

/// The cells of the input page: one `MvuCell` per section.
type InputPageCells = {
  Act: unit -> Cell
  Enter: unit -> Cell
  Pick: unit -> Cell
}

/// One section of the input page: a title in the type ramp, then the mounted
/// section cell. Whitespace groups the sections; no boxes surround them.
let section(title: string, key: string, factory: unit -> Cell) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.subheader title, Cell.Mount<Cell>(key, factory))

/// The input page: the page title in the largest ramp role, then the three
/// sections. Each section is its own `MvuCell`, so a model change re-renders
/// only its section.
let create(cells: InputPageCells) : Cell = {
  new Cell() with
    override _.Build() : Blob =
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
          MetroText.title "input",
          section("act", "input-act", cells.Act),
          section("enter", "input-enter", cells.Enter),
          section("pick", "input-pick", cells.Pick)
        )
}
