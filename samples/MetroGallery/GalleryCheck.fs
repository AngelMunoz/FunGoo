module MetroGallery.GalleryCheck

open System
open System.Collections.Generic
open Goo
open FunGoo.Metro

/// Root cell of the headless check. It mounts the gallery, and it runs an
/// action of the check inside one of its builds. Inside a build, the Metro
/// theme and the view of a mounted cell are readable.
[<AbstractClass>]
type CheckRootCell() =
  inherit Cell()

  /// Runs <c>action</c> inside the next build of this cell, then renders one
  /// frame of <c>host</c>.
  abstract DuringBuild:
    host: EmbeddedWindowHost * action: (unit -> unit) -> unit

/// The request of one `DuringBuild`.
type DuringBuildRequest = {
  Host: EmbeddedWindowHost
  Action: unit -> unit
}

/// State of the root of the check. One mutable field per concern.
type RootState = {
  Gallery: Gallery.GalleryCell
  /// The action that the next build runs first, when there is one.
  mutable Pending: (unit -> unit) voption
}

/// A window host for the headless check. It has no Vulkan surface.
let headlessHost() : EmbeddedWindowHost =
  let noSurface() =
    raise(NotSupportedException("Headless check host has no Vulkan surface."))

  {
    new EmbeddedWindowHost() with
      override _.RequestFrame() = ()

      override _.LoadVulkanLibrary() = noSurface()

      override _.GetVulkanGetInstanceProcAddr() = noSurface()

      override _.UnloadVulkanLibrary() = noSurface()

      override _.GetVulkanInstanceExtensions() = noSurface()

      override _.CreateVulkanSurface
        (_instance: nativeint, _surface: byref<uint64>)
        =
        noSurface()

      override _.DestroyVulkanSurface(_instance: nativeint, _surface: uint64) =
        noSurface()
  }

module CheckRoot =

  /// The mounted gallery below the root. The action of a pending
  /// `DuringBuild` runs first.
  let build(state: RootState, galleryFactory: unit -> Cell) : Blob =
    state.Pending
    |> ValueOption.iter(fun action ->
      state.Pending <- ValueNone
      action())

    Cell.Mount<Cell>("gallery", galleryFactory)

  /// Requests one build that runs the action of the request first. The check
  /// writes no state of the root cell: it asks the root cell for one build,
  /// because no callback and no owner rule does it.
  let requestDuringBuild
    (cell: CheckRootCell, state: RootState, request: DuringBuildRequest)
    : unit =
    state.Pending <- ValueSome request.Action
    // The root cell of the check is the owner of this build.
    cell.Rebuild()
    request.Host.RenderFrame(0.016)

  let create(gallery: Gallery.GalleryCell) : CheckRootCell =
    let state: RootState = {
      Gallery = gallery
      Pending = ValueNone
    }

    let galleryFactory() : Cell = state.Gallery :> Cell

    {
      new CheckRootCell() with
        override _.Build() : Blob = build(state, galleryFactory)

        override this.DuringBuild(host, action) =
          requestDuringBuild(this, state, { Host = host; Action = action })
    }

/// Depth-first search of the Container and Button children for a keyed
/// element. It uses the public children collections only.
let rec findBlobByKey(root: Blob, key: string) : Blob voption =
  if not(isNull root) && root.Key = key then
    ValueSome root
  else
    // The type annotation selects the Children property and not the Children
    // extension method of FunGoo.Children.
    let children: IList<Blob> =
      match root with
      | :? Container as container -> container.Children
      | :? Button as button -> button.Children
      | _ -> (Array.empty<Blob> :> IList<Blob>)

    let mutable found = ValueNone
    let mutable index = 0

    while index < children.Count && found.IsNone do
      found <- findBlobByKey(children[index], key)
      index <- index + 1

    found

/// The value of a search, or a failure with <paramref name="message"/>.
let requireSome(message: string, found: 'a voption) : 'a =
  match found with
  | ValueSome value -> value
  | ValueNone -> failwith message

/// The keyed button in a tree, or a failure.
let requireButton(tree: Blob, key: string, message: string) : Button =
  match findBlobByKey(tree, key) with
  | ValueSome blob -> blob :?> Button
  | ValueNone -> failwith message

/// The headless check of the gallery. <paramref name="sceneTabs"/> are the
/// tabs that register as a named scene. <paramref name="scenes"/> makes the
/// named scenes for a getter of the window. The check returns the exit code.
let run(sceneTabs: string array, scenes: Gallery.ScenesFactory) : int =
  let mutable windowSlot: Window voption = ValueNone
  let gallery = Gallery.create((fun () -> windowSlot), Gallery.hubStart)
  let root = CheckRoot.create gallery
  use host = headlessHost()
  host.Resize(1280, 800, 1280, 800)

  let window =
    Window(
      Title = "Metro Gallery Check",
      Root = root,
      Scenes = scenes(fun () -> windowSlot)
    )

  windowSlot <- ValueSome window

  Metro.attach(window, Gallery.themeConfig)

  window.Attach(host) |> ignore

  let pump(n: int) =
    for _ = 1 to n do
      host.RenderFrame(0.016)

  pump 3

  // 0. Every gallery tab is registered as a named `goo render` scene.
  let registeredScenes =
    if isNull window.Scenes then
      Array.empty
    else
      window.Scenes.Keys |> Seq.sort |> Array.ofSeq

  if registeredScenes <> sceneTabs then
    failwith $"Expected scenes {sceneTabs}, got '{registeredScenes}'."

  // 1. Click a real Pivot tab header. The Pivot gives the selection to the
  // gallery cell through `InvokeInputCallback`, so the owner rule rebuilds
  // the gallery cell, and the posted Elmish message rebuilds it again once
  // the loop applies the message. The handler of the selection has no
  // `Rebuild()` call.
  let pivot = requireSome("Expected the Pivot to be mounted.", gallery.Pivot)

  // The view of a cell is readable only inside a build.
  let mutable pivotTree: Blob = null
  root.DuringBuild(host, fun () -> pivotTree <- pivot.View)

  let typeTab =
    requireButton(
      pivotTree,
      "pivot-tab-type",
      "Expected the Pivot tab 'pivot-tab-type'."
    )

  let buildsBefore = gallery.Builds
  typeTab.OnClick.Invoke()
  pump 2

  if gallery.ActiveTab <> "type" then
    failwith
      $"Expected tab click to switch activeTab to 'type', got '{gallery.ActiveTab}'."

  if gallery.Builds < buildsBefore + 1 then
    failwith "Expected the gallery to rebuild after a tab click."

  // 2. Exercise remaining Pivot pages across two full roundtrips to ensure
  // unmounted page cells remount cleanly with fresh instances.
  for tab in
    [|
      "input"
      "collections"
      "navigate"
      "dialogs"
      "motion"
      "type"
      "hub"
      "input"
      "collections"
      "navigate"
      "dialogs"
      "motion"
      "type"
      "hub"
    |] do
    gallery.SelectTab tab
    pump 2

    if gallery.ActiveTab <> tab then
      failwith $"Expected active tab '{tab}', got '{gallery.ActiveTab}'."

  // 3. Toggle Dark -> Light -> Dark and verify MetroTheme.
  gallery.SetMode MetroMode.Light
  pump 2

  if (Metro.readWindow window).IsDark then
    failwith "Expected Light mode after toggling theme."

  gallery.SetMode MetroMode.Dark
  pump 2
  let restoredTheme = Metro.readWindow window

  if not restoredTheme.IsDark then
    failwith "Expected Dark mode after restoring theme."

  // 4. Cycle accent swatches and verify theme accent updates.
  for kind in
    [| MetroAccentKind.Teal; MetroAccentKind.Magenta; MetroAccentKind.Blue |] do
    gallery.SetAccent kind
    pump 1
    let expected = MetroAccent.resolve(kind, ValueNone).Base
    let actual = (Metro.readWindow window).Accent.Base

    if actual <> expected then
      failwith $"Accent mismatch for {kind}."

  // 5. Open and dismiss SettingsFlyout (stateful animated cell).
  let flyout =
    requireSome(
      "Expected the SettingsFlyout to be mounted.",
      gallery.SettingsFlyout
    )

  gallery.SetSettingsOpen true
  pump 2

  if not gallery.SettingsOpen || not flyout.IsVisible then
    failwith "Expected SettingsFlyout to be open and visible."

  if not flyout.IsAnimating then
    failwith "Expected SettingsFlyout open transition to animate."

  gallery.SetSettingsOpen false
  pump 2

  if gallery.SettingsOpen then
    failwith "Expected SettingsFlyout open state to be false."

  if not flyout.IsAnimating then
    failwith "Expected SettingsFlyout close transition to animate."

  // 6. Open and dismiss ContentDialog (stateful animated cell).
  let dialog =
    requireSome(
      "Expected the ContentDialog to be mounted.",
      gallery.ContentDialog
    )

  gallery.SetDialogOpen true
  pump 2

  if not gallery.DialogOpen || not dialog.IsVisible then
    failwith "Expected ContentDialog to be open and visible."

  if not dialog.IsAnimating then
    failwith "Expected ContentDialog open transition to animate."

  gallery.SetDialogOpen false
  pump 2

  if gallery.DialogOpen then
    failwith "Expected ContentDialog open state to be false."

  if not dialog.IsAnimating then
    failwith "Expected ContentDialog close transition to animate."

  // 7. Semantic zoom: the toggle zooms out, and a group tile zooms back in.
  gallery.SelectTab "collections"
  pump 2

  let zoomTab =
    requireSome(
      "Expected the semantic zoom tab to be mounted.",
      gallery.ZoomTab
    )

  let zoomCell =
    requireSome(
      "Expected the semantic zoom cell to be mounted.",
      zoomTab.ZoomCell
    )

  zoomCell.Handle(MetroSemanticZoomEvent.ZoomOut)
  pump 2

  if not zoomTab.ZoomedOut || not zoomCell.IsZoomedOut then
    failwith "Expected the zoom toggle to zoom out."

  let groupTile =
    requireButton(
      zoomTab.LastOverview,
      "group-tile-maps",
      "Expected the group tile of 'maps'."
    )

  // The zoom cell builds the group tiles, so it owns their callbacks. The
  // tab has to zoom in by itself.
  groupTile.OnClick.Invoke()
  pump 2

  if zoomTab.ZoomedOut || zoomCell.IsZoomedOut then
    failwith "Expected a group tile to zoom in."

  if zoomTab.LastAction <> "Zoomed in on maps" then
    failwith $"Expected 'Zoomed in on maps', got '{zoomTab.LastAction}'."

  let detailTile =
    requireButton(
      zoomTab.LastDetail,
      "tile-maps-city",
      "Expected the detail tile 'city' of 'maps'."
    )

  detailTile.OnClick.Invoke()
  pump 2

  if zoomTab.LastAction <> "Selected city in maps" then
    failwith $"Expected 'Selected city in maps', got '{zoomTab.LastAction}'."

  // 8. Enter section: the size combo box and the city auto-suggest box keep
  // separate values in the section model.
  gallery.SelectTab "input"
  pump 2

  let enter =
    requireSome(
      "Expected the enter section to be mounted.",
      gallery.EnterSection
    )

  enter.CityInput.OnQueryChange.Invoke("por")
  pump 2
  enter.CityInput.OnSelect.Invoke("Porto")
  pump 2

  if enter.SizeSelected <> "medium" then
    failwith $"Expected the size to stay 'medium', got '{enter.SizeSelected}'."

  if enter.CitySelected <> "Porto" then
    failwith $"Expected the city 'Porto', got '{enter.CitySelected}'."

  enter.SizeInput.OnSelect.Invoke("large")
  pump 2

  if enter.SizeSelected <> "large" || enter.CitySelected <> "Porto" then
    failwith "Expected the size and the city to change independently."

  // A new query starts a new search and clears the city selection.
  enter.CityInput.OnQueryChange.Invoke("l")
  pump 2

  if enter.CitySelected <> "" || enter.CityQuery <> "l" then
    failwith "Expected a new query to clear the city selection."

  // 9. A tab with a control callback rebuilds with no call of `Rebuild()`.
  // The zoom cell gives each zoom request to the tab through
  // `InvokeInputCallback`, as a Goo.Widgets control gives the callback of
  // its input. The owner rule then rebuilds the tab. No handler of the zoom
  // tab calls `Rebuild()`, and no handler of the three tabs with a widget
  // input calls it. A Goo.Widgets control has no public member that starts
  // the callback of its input, thus the check drives the zoom cell.
  gallery.SelectTab "collections"
  pump 2

  let zoomTabAgain =
    requireSome(
      "Expected the semantic zoom tab to be mounted.",
      gallery.ZoomTab
    )

  let zoomCellAgain =
    requireSome(
      "Expected the semantic zoom cell to be mounted.",
      zoomTabAgain.ZoomCell
    )

  let buildsBefore = zoomTabAgain.Builds
  zoomCellAgain.Handle(MetroSemanticZoomEvent.ZoomOut)
  pump 2

  if not zoomTabAgain.ZoomedOut then
    failwith "Expected the zoom request to zoom the tab out."

  if zoomTabAgain.Builds <> buildsBefore + 1 then
    failwith
      "Expected the owner rule to rebuild the zoom tab once after a zoom request."

  // 10. Pick section: the LongListSelector jump grid and the inline list
  // picker rebuild the section through InvokeInputCallback with no manual
  // Rebuild(). The tree view stayed in the selection tab on the collections
  // page.
  gallery.SelectTab "input"
  pump 2

  let pick =
    requireSome("Expected the pick section to be mounted.", gallery.PickSection)

  let longList =
    requireSome("Expected the long list selector to be mounted.", pick.LongList)

  longList.Handle(MetroLongListEvent.JumpOpenChanged true)
  pump 2

  if not longList.IsJumpOpen then
    failwith "Expected long list jump grid to open."

  longList.Handle(MetroLongListEvent.GroupJumped "g")
  pump 2

  if longList.IsJumpOpen || pick.LastAction <> "Jumped to group g" then
    failwith
      $"Expected jump to 'g' to close jump grid, got '{pick.LastAction}'."

  let inlinePicker =
    requireSome(
      "Expected the inline list picker to be mounted.",
      pick.InlinePicker
    )

  inlinePicker.Handle(MetroListPickerEvent.OpenChanged true)
  pump 2

  if not inlinePicker.IsOpen then
    failwith "Expected inline list picker to open."

  inlinePicker.Handle(MetroListPickerEvent.Selected "pulse")
  inlinePicker.Handle(MetroListPickerEvent.OpenChanged false)
  pump 2

  if inlinePicker.IsOpen || pick.LastAction <> "Ringtone: pulse" then
    failwith
      $"Expected 'Ringtone: pulse' and closed picker, got '{pick.LastAction}'."

  // The tree view of the selection tab mounts on the collections page now.
  gallery.SelectTab "collections"
  pump 2

  let selectionTab =
    requireSome(
      "Expected the selection tab to be mounted.",
      gallery.SelectionTab
    )

  selectionTab.TreeInput.OnExpandedChange.Invoke("samples", true)
  pump 2
  selectionTab.TreeInput.OnSelectionChange.Invoke([| "gallery" |])
  pump 2

  if
    selectionTab.SelectedTreeIds <> [| "gallery" |]
    || not(selectionTab.ExpandedTreeIds |> Array.contains "samples")
  then
    failwith "Expected tree view expand and select to update tab state."

  // 11. Pick section: inline rollers and modal pickers commit to the one
  // shared date. Each widget gives its event to the section through
  // InvokeInputCallback, and the posted section message rebuilds the section.
  gallery.SelectTab "input"
  pump 2

  let pickDt =
    requireSome("Expected the pick section to be mounted.", gallery.PickSection)

  let dateRoller =
    requireSome("Expected inline date roller.", pickDt.DateRoller)

  let monthCol =
    requireSome("Expected month roller column.", dateRoller.MonthRoller)

  let dtBuildsBefore = pickDt.Builds
  monthCol.Step(1)
  pump 2

  if pickDt.SelectedDate <> DateOnly(2026, 12, 24) then
    failwith $"Expected roller date 2026-12-24, got {pickDt.SelectedDate}."

  if pickDt.Builds <= dtBuildsBefore then
    failwith
      "Expected the owner rule and the posted message to rebuild the pick section."

  let datePicker =
    requireSome("Expected compact date picker.", pickDt.DatePicker)

  datePicker.Handle(MetroDatePickerEvent.OpenChanged true)
  pump 2

  if not datePicker.IsOpen then
    failwith "Expected compact date picker modal to open."

  datePicker.Handle(MetroDatePickerEvent.DateCommitted(DateOnly(2027, 3, 15)))
  pump 2

  if datePicker.IsOpen || pickDt.SelectedDate <> DateOnly(2027, 3, 15) then
    failwith "Expected committed date 2027-03-15 and closed modal."

  pickDt.CalendarInput.OnChange.Invoke(DateOnly(2026, 11, 30))
  pump 2

  if pickDt.SelectedDate <> DateOnly(2026, 11, 30) then
    failwith "Expected Calendar selection to update to 2026-11-30."

  // A cleared entry keeps the shared date and notes the clear.
  pickDt.CalendarDatePickerInput.OnChange.Invoke(Nullable())
  pump 2

  if
    pickDt.SelectedDate <> DateOnly(2026, 11, 30)
    || pickDt.LastAction <> "Dropdown date: cleared"
  then
    failwith "Expected CalendarDatePicker clear to keep 2026-11-30."

  pickDt.CalendarDatePickerInput.OnChange.Invoke(
    Nullable(DateOnly(2026, 12, 1))
  )

  pump 2

  if pickDt.SelectedDate <> DateOnly(2026, 12, 1) then
    failwith "Expected CalendarDatePicker selection 2026-12-01."

  // 12. Overlays tab: Flyout, MessageDialog, MenuFlyout, ContextMenu, and
  // Tooltip mount and route callbacks cleanly.
  gallery.SelectTab "dialogs"
  pump 2

  let ovTab = requireSome("Expected overlays tab.", gallery.OverlaysTab)

  let tooltip = requireSome("Expected tooltip cell.", ovTab.Tooltip)

  if not tooltip.IsVisible then
    failwith "Expected pinned showcase tooltip to be visible."

  ovTab.MenuInput.OnClick.Invoke("pin")
  pump 2

  if ovTab.LastAction <> "Menu item: pin" then
    failwith $"Expected 'Menu item: pin', got '{ovTab.LastAction}'."

  ovTab.ContextMenuInput.OnClick.Invoke("inspect")
  pump 2

  if ovTab.LastAction <> "Context item: inspect" then
    failwith $"Expected 'Context item: inspect', got '{ovTab.LastAction}'."

  let msgDialog =
    requireSome("Expected message dialog cell.", ovTab.MessageDialog)

  let msgBuildsBefore = ovTab.Builds
  msgDialog.Handle(MetroMessageDialogResult.CommandInvoked "save")
  pump 2

  if
    ovTab.LastAction <> "Message command: save"
    || ovTab.Builds <> msgBuildsBefore + 1
  then
    failwith
      $"Expected 'Message command: save' in one build, got '{ovTab.LastAction}'."

  // 13. Notifications tab: InfoBar, ToastHost, and TeachingTip mount and
  // deliver callbacks cleanly.
  gallery.SelectTab "dialogs"
  pump 2

  let notifTab =
    requireSome("Expected notifications tab.", gallery.NotificationsTab)

  if notifTab.InfoOpenMask <> 0b1111 then
    failwith "Expected all four info bars open initially."

  notifTab.PushSampleToast()
  pump 2

  let toastHost = requireSome("Expected toast host cell.", notifTab.ToastHost)

  if toastHost.ActiveCount <> 1 || notifTab.ToastCount <> 1 then
    failwith "Expected 1 active toast after PushSampleToast."

  toastHost.Dismiss "toast-1"
  pump 2

  if
    toastHost.ActiveCount <> 0 || notifTab.LastAction <> "Dismissed toast-1"
  then
    failwith
      $"Expected 'Dismissed toast-1', got '{notifTab.LastAction}' (count={toastHost.ActiveCount})."

  notifTab.SetTipOpen true
  pump 2

  let tipCell = requireSome("Expected teaching tip cell.", notifTab.TeachingTip)

  if not tipCell.IsPresented then
    failwith "Expected teaching tip to be presented."

  tipCell.InvokeAction()
  pump 2

  if
    notifTab.TipOpen || notifTab.LastAction <> "Teaching tip action: Got It"
  then
    failwith
      $"Expected closed tip with 'Teaching tip action: Got It', got '{notifTab.LastAction}'."

  // 14. Navigation tab: SplitView, TabView, BreadcrumbBar, AppBar parts, and
  // Panorama mount and deliver callbacks cleanly.
  gallery.SelectTab "navigate"
  pump 2

  let navTab = requireSome("Expected navigation tab.", gallery.NavigationTab)

  navTab.TabBarInput.OnAdd.Invoke()
  pump 2

  if navTab.TabCount <> 4 || navTab.ActiveSubTab <> "tab-4" then
    failwith
      $"Expected 4 tabs and active 'tab-4', got {navTab.TabCount} and '{navTab.ActiveSubTab}'."

  navTab.TabBarInput.OnClose.Invoke "tab-4"
  pump 2

  if navTab.TabCount <> 3 then
    failwith $"Expected 3 tabs after close, got {navTab.TabCount}."

  navTab.BreadcrumbInput.OnSelect.Invoke "apps"
  pump 2

  if navTab.LastAction <> "Breadcrumb: apps" then
    failwith $"Expected 'Breadcrumb: apps', got '{navTab.LastAction}'."

  let pan = requireSome("Expected panorama cell.", navTab.Panorama)
  pan.ScrollHandle.JumpTo(x = System.Nullable 100.0) |> ignore
  pump 2

  if abs(pan.TitleTranslate.Value.X - (-35.0)) > 0.5 then
    failwith
      $"Expected Panorama title parallax -35.0, got {pan.TitleTranslate.Value.X}."

  // 15. Primitives & Layout tab: Icon, PersonPicture, Image, RichTextBlock,
  // Expander, VariableSizedWrapGrid, WrapPanel, Viewbox, and ScrollViewer
  // mount and switch modes cleanly.
  gallery.SelectTab "navigate"
  pump 2

  let primTab = requireSome("Expected primitives tab.", gallery.PrimitivesTab)

  if not primTab.ExpanderOpen then
    failwith "Expected primitives expander open initially."

  primTab.SetExpanderOpen false
  primTab.SetGridOrientation MetroWrapOrientation.Vertical
  primTab.SetViewboxStretch MetroStretch.Fill
  pump 2

  if
    primTab.ExpanderOpen
    || primTab.GridOrientation <> MetroWrapOrientation.Vertical
    || primTab.ViewboxStretch <> MetroStretch.Fill
  then
    failwith "Expected PrimitivesTab state updates to apply cleanly."

  // The responsive viewbox cell of the primitives tab mounted and scales its
  // content.
  let viewboxCell =
    requireSome("Expected the responsive viewbox cell.", primTab.ViewboxCell)

  if viewboxCell.ScaleX <= 0.0 || viewboxCell.ScaleY <= 0.0 then
    failwith
      $"Expected the responsive viewbox scale above 0.0, got {viewboxCell.ScaleX} and {viewboxCell.ScaleY}."

  printfn "MetroGallery checks passed."
  0
