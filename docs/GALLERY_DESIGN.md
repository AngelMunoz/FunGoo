# MetroGallery — Page Organization and MVU Design

Status: implemented. The Gallery follows this map on `main`: seven Pivot pages (`hub`, `type`, `input`, `collections`, `navigate`, `dialogs`, `motion`), one file per page under `samples/MetroGallery/Pages/`, with the shell in `Gallery.fs` and `GalleryCheck` covering every control family on its new page.

Rules for the redesign, from the Metro design language: one family per page, fierce
reduction of page count, typography leads each page (one large page title, section
titles in the type ramp, no card chrome), the first page answers at a glance, and
every control appears at least once in the whole Gallery. A control may appear
again where the second use shows a different mode, state, or job (a launch button
in dialogs, a toggle as a clock-format switch). Repetition must teach a new use;
repetition as filler breaks the reduction rule.

## Current state (14 Pivot pages)

| # | Page (Pivot id) | File | Components on the page | State that crosses controls |
| --- | --- | --- | --- | --- |
| 1 | overview | `Gallery.fs` (Hub inline) | `MetroHub`, `MetroIconicTile`, `MetroLiveTile`, `MetroCycleTile`, `MetroFlipTile`, start tile grid, accent swatches, AppBar commands (theme, settings, dialog, reset, about), `MetroSettingsFlyout`, `MetroContentDialog` | `GalleryState`: ActiveTab, Mode, AccentKind, AppBarExpanded, SettingsOpen, DialogOpen |
| 2 | typography | `TypographyTab.fs` | `MetroText` in all 10 `MetroTextRole` values, `MetroTypography` ramp | none (static) |
| 3 | controls | `ControlsTab.fs` | `MetroButton`, `MetroTextBox` (text and password), `MetroCheckBox`, `MetroRadioButton`, `MetroToggleSwitch`, `MetroSlider`, `MetroProgressBar`, `MetroProgressRing`, `MetroTilt` | `GalleryStore`: SliderValue, TextValue, SecretValue, ToggleEnabled, CheckAccepted, RadioChoice, TogglePressed; the overview Hub reads SliderValue |
| 4 | flip view | `FlipViewTab.fs` | `MetroFlipView` | selected index only |
| 5 | lists | `ListsTab.fs` | `MetroListView` (plain and grouped), `MetroListBox`, `MetroGridView` | selection events only |
| 6 | selection | `SelectionTab.fs` | `MetroListPicker`, `MetroLongListSelector`, `MetroTreeView`, `MetroPivot` (demo) | selection ids only |
| 7 | zoom | `ZoomTab.fs` | `MetroSemanticZoom` with grouped tile content | zoom state only |
| 8 | inputs | `InputsTab.fs` | `MetroComboBox` (combo and auto-suggest), `MetroNumberBox`, `MetroRating` | one event channel per control |
| 9 | date & time | `DateTimeTab.fs` | `MetroCalendar`, `MetroCalendarDatePicker`, `MetroDatePicker`, `MetroDatePickerRoller`, `MetroTimePicker`, `MetroTimePickerRoller`, `MetroToggleSwitch` (format) | one selected date and time shared by calendar and pickers |
| 10 | overlays | `OverlaysTab.fs` | `MetroFlyout`, `MetroMessageDialog`, `MetroMenuFlyout`, `MetroContextMenu`, `MetroTooltip` | open surface only |
| 11 | notifications | `NotificationsTab.fs` | `MetroInfoBar`, `MetroToast` (host), `MetroTeachingTip` | toast queue with timers |
| 12 | navigation | `NavigationTab.fs` | `MetroPivot`, `MetroPanorama`, `MetroSplitView`, `MetroTabView`, `MetroBreadcrumbBar`, `MetroAppBarButton`, `MetroAppBarToggleButton`, `MetroAppBarSeparator` | selected tab and pane mode only |
| 13 | primitives | `PrimitivesTab.fs` | `MetroIcon`, `MetroBorder`, `MetroImage`, `MetroPersonPicture`, `MetroExpander`, `MetroRichTextBlock`, `MetroScrollViewer`, `MetroWrapPanel`, `MetroVariableSizedWrapGrid`, `MetroViewbox` | stretch mode and expander state only |
| 14 | motion | `MotionTab.fs` | `MetroPerspective` turnstile, `MetroFallbackMotion`, `ContinuumDemo` (flight), `StaggerDemo` (stagger), `MetroTilt` | demo-local only |

Defects of the current map:

- Input controls split across three pages: controls (3), inputs (8), and half of
  selection (6). A user hunts one family in three places.
- Collection controls split across four pages: lists (5), flip view (4), zoom (7),
  and half of selection (6).
- Date and time pickers sit apart from the other pickers in selection (6).
- `controls` and `inputs` differ by vendor history, not by kind. The names teach
  nothing.
- 14 pages exceed the Pivot glance rule: page titles peek without meaning.

## Proposed state (7 Pivot pages)

Seven pages. A page earns its place only when a user looks for its kind by name.
Pickers are input. Navigation and layout are one question: where things sit and
how the user moves between them. Dialogs and feedback are one question: how the
app interrupts. Everything else was granularity for its own sake.

| # | Page (Pivot id) | Contents (every control at least once; repeats only for a new use) | Layout on the page | State owner | MVU |
| --- | --- | --- | --- | --- | --- |
| 1 | `hub` | The page is `MetroHub` itself, full-bleed: sections "start tiles" (`MetroIconicTile`, `MetroLiveTile`, `MetroCycleTile`, `MetroFlipTile`), "at a glance", and "metro ethos" | The Hub is the desktop home pattern, so page one is both the Gallery home and the Hub showcase. The Pivot header reads `hub`, so a user who looks for the control by name finds it. The tile grid leads at glance size. Accent and mode stay in the shell AppBar and SettingsFlyout, not on this page | Shell model (tab, mode, accent) | Shell MVU (see below) |
| 2 | `type` | `MetroText` 10 roles in Selawik, weight specimens (300, 400, 700), tone variants | One column, ramp order large to small. No controls | none | No. Plain view function |
| 3 | `input` | Act: `MetroButton`, `MetroToggleSwitch`, `MetroCheckBox`, `MetroRadioButton`, `MetroProgressRing`. Enter: `MetroTextBox`, `MetroNumberBox`, `MetroComboBox`, `MetroRating`, `MetroSlider`, `MetroProgressBar`. Pick: `MetroCalendar`, `MetroCalendarDatePicker`, `MetroDatePicker`, `MetroDatePickerRoller`, `MetroTimePicker`, `MetroTimePickerRoller`, `MetroListPicker`, `MetroLongListSelector` | Three titled sections in one scrolling page: act, enter, pick. Each progress control sits with the control that feeds it: the ring in act reads the toggle, the bar in enter reads the slider. A separate output section would need state from two section models, which breaks the granular MVU rule. Calendar leads the pick section because the date pickers commit to the calendar date | One model per section. Act, enter, and pick each own one `MvuCell` | Yes, granular. Three section `MvuCell` values (see below). Slider commits dispatch on commit, not on every drag tick, so the page does not rebuild per pixel |
| 4 | `collections` | `MetroListView`, `MetroListBox`, `MetroGridView`, `MetroTreeView`, `MetroFlipView`, `MetroSemanticZoom` | One section per control, full width. Grouped ListView first (richest), then ListBox, GridView, TreeView, FlipView, SemanticZoom | Each control owns its selection in its cell closure | No. Widget factories already own events; MVU adds a rebuild hop with no reader |
| 5 | `navigate` | Move: `MetroPivot`, `MetroPanorama`, `MetroSplitView`, `MetroTabView`, `MetroBreadcrumbBar`, `MetroAppBarButton`, `MetroAppBarToggleButton`, `MetroAppBarSeparator`. Arrange: `MetroBorder`, `MetroIcon`, `MetroImage`, `MetroPersonPicture`, `MetroExpander`, `MetroRichTextBlock`, `MetroScrollViewer`, `MetroWrapPanel`, `MetroVariableSizedWrapGrid`, `MetroViewbox` | Two titled sections in one scrolling page: move, then arrange. Navigation demos keep full width; arrange demos use a two-column flow | Demo-local | No |
| 6 | `dialogs` | `MetroFlyout`, `MetroMessageDialog`, `MetroMenuFlyout`, `MetroContextMenu`, `MetroTooltip`, `MetroInfoBar`, `MetroToast`, `MetroTeachingTip`; shell `MetroContentDialog` and `MetroSettingsFlyout` stay in the shell and get one launch row here | Sections: surfaces (one launch button per anchored surface), then notices (InfoBar stack, toast host, teaching tip) | Open surface id; toast queue timers stay in `MetroToastHostCell` | No. Timers and queue are the cell job; MVU buys nothing |
| 7 | `motion` | Turnstile with the Pivot shader path and 2D fallback, `ContinuumDemo`, `StaggerDemo`, `MetroTilt` press | One demo per row, each with a reduced-motion note | Demo-local | No |

Shell (not a page): `MetroPivot` frame, `MetroAppBar` (theme, settings, dialog,
reset, about), `MetroSettingsFlyout` (accent picker, mode), `MetroContentDialog`.
The shell keeps these because every page needs them at thumb reach.

## MVU decisions

MVU enters only where one value has two or more writers or readers on one page.
`FunGoo.Mvu.MvuCell` rebuilds its page cell on each model change, so a page with
independent demos pays a full page rebuild for no gain. Plain cells stay there.

| Owner | Form | Model | Messages | Reason |
| --- | --- | --- | --- | --- |
| Gallery shell | `MvuCell` | ActiveTab, Mode, AccentKind, AppBarExpanded, SettingsOpen, DialogOpen | TabSelected, ModeChanged, AccentChanged, AppBarToggled, SettingsOpened/Closed, DialogOpened/Closed, Reset | The shell already thinks in messages (`TabSelected` exists). Accent and mode reach every page through `Metro.setMode` and `Metro.setAccent`, so one dispatch replaces six mutable fields and ad-hoc `Rebuild()` calls |
| Page `input` | `MvuCell` | TextValue, SecretValue, ToggleEnabled, CheckAccepted, RadioChoice, SliderValue, NumberValue, RatingValue, ComboSelection, SelectedDate, SelectedTime, ClockFormat | One message per control commit. Slider dispatches on commit only | Every section writes one page state and the output section reads SliderValue and ToggleEnabled. Calendar and four pickers commit to one date. This page replaces `GalleryStore`, which the overview Hub reads today; the Hub reads the shell model instead |
| Every other page | Plain cell or view function | Cell closure state | Callbacks through `InvokeInputCallback` as today | One writer and one reader per value. MVU adds ceremony only |

Cross-page rule: no page reads another page model. The old `GalleryStore` flow
(controls write, Hub reads) moves into the shell model, and the Hub page shows
the progress pair from the shell model. `GalleryStore.fs` is deleted.

## Migration rules

1. One page per commit, in the proposed order, direct to `main`. A commit moves
   the controls, renames the Pivot id and the `goo render` scene together, and
   updates `Program.fs` scene list in the same commit.
2. `GalleryCheck` steps move with their page. The check keeps one step per
   control family, not per old tab. Old scene names die with their page; no
   alias scenes stay.
3. Each moved page keeps its controls pixel-equal: same sizes, same order inside
   the page, same demo data. Only the page frame and grouping change.
4. MVU conversion is a separate commit after the page move lands, one owner per
   commit (shell, then `input`). A move and a conversion never share a commit,
   so a regression names its cause.
5. Every commit gate: `dotnet fantomas` on touched files, Release build, Metro
   tests, gallery `--check`, and one `goo render` frame per changed scene
   attached to the commit message thread for review.
6. Files: one file per page named for the page (`HubPage.fs`, `TypePage.fs`,
   `InputPage.fs`, `CollectionsPage.fs`, `NavigatePage.fs`, `DialogsPage.fs`,
   `MotionPage.fs`). `Gallery.fs` keeps the shell and the composition root only.

## What does not change

- No control changes. This work moves Gallery code only.
- The Pivot keeps 44 px targets and peeking headers. Seven pages fit the header
  pan without a scrollbar at 1200 px.
- Dark mode stays the start mode. Accent stays Blue at start.
