module MetroGallery.Pages.HubPage

open Goo
open FunGoo.Children
open FunGoo.Metro
open MetroGallery.GalleryStore

/// The callbacks of the hub: the hub owns no shell state, so each action
/// reaches the shell through one of these callbacks.
type HubCallbacks = {
  OpenTab: string -> unit
  OpenSettings: unit -> unit
  OpenDialog: unit -> unit
}

/// The factories of the cycling tiles of the start section.
type TileFactories =
  (unit -> MetroLiveTileCell) *
  (unit -> MetroCycleTileCell) *
  (unit -> MetroFlipTileCell)

let hubSections = [|
  {
    Id = "start"
    Header = "start tiles"
    Width = 340.0
  }
  {
    Id = "status"
    Header = "at a glance"
    Width = 320.0
  }
  {
    Id = "principles"
    Header = "metro ethos"
    Width = 340.0
  }
|]

/// The layout of the start section of the hub. It holds no state. The sizes
/// follow the mount order of `startTiles`: the live tile, the cycle tile,
/// the flip tile, the mail tile, four small tiles, and the controls tile.
let tileGrid =
  TileGridLayout.create {
    CellSize = 72.0
    Gap = 8.0
    Columns = 4
    Sizes = [|
      MetroTileSize.Wide
      MetroTileSize.Medium
      MetroTileSize.Medium
      MetroTileSize.Medium
      MetroTileSize.Small
      MetroTileSize.Small
      MetroTileSize.Small
      MetroTileSize.Small
      MetroTileSize.Wide
    |]
  }

let liveFaces = [|
  {
    Title = "System Telemetry"
    Subtitle = ValueSome "Vulkan retained compositor"
    Metric = ValueSome "60 fps"
    AccentOverride = ValueNone
  }
  {
    Title = "System Telemetry"
    Subtitle = ValueSome "Zero-rebuild paint animations"
    Metric = ValueSome "0 ms"
    AccentOverride = ValueSome(Color.Rgb(0, 171, 169))
  }
  {
    Title = "System Telemetry"
    Subtitle = ValueSome "21 authentic Metro accents"
    Metric = ValueSome "21"
    AccentOverride = ValueSome(Color.Rgb(216, 0, 115))
  }
|]

let cycleFaces = [|
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
    AccentOverride = ValueSome(Color.Rgb(0, 163, 0))
  }
|]

let flipFront: MetroLiveTileFace = {
  Title = "Store"
  Subtitle = ValueSome "Tap to flip"
  Metric = ValueSome "S"
  AccentOverride = ValueNone
}

let flipBack: MetroLiveTileFace = {
  Title = "Store"
  Subtitle = ValueSome "Back again"
  Metric = ValueSome "B"
  AccentOverride = ValueSome(Color.Rgb(0, 80, 239))
}

/// Writes the action of one iconic tile of the hub. The Hub cell builds the
/// tile, so it owns the callback of the tile: the owner rule rebuilds the
/// Hub cell, and its sections read the store again.
let openTile(store: GalleryStore, tile: string) : unit =
  store.TileTapCount <- store.TileTapCount + 1
  store.LastAction <- $"Opened {tile}"

/// Increments the Mail tile count and total tile taps.
let openMailTile(store: GalleryStore) : unit =
  store.MailCount <- store.MailCount + 1
  openTile(store, "Mail tile")

/// Writes the action of the live tile. The live tile gives each click to the
/// Hub cell through `InvokeInputCallback`, so the owner rule rebuilds the
/// Hub cell.
let tapLiveTile(store: GalleryStore) : unit =
  store.TileTapCount <- store.TileTapCount + 1
  store.LastAction <- $"Live tile tapped ({store.TileTapCount})"

/// Writes the action of the cycle tile.
let tapCycleTile(store: GalleryStore) : unit =
  store.TileTapCount <- store.TileTapCount + 1
  store.LastAction <- $"Cycle tile tapped ({store.TileTapCount})"

/// Writes the action of the flip tile.
let tapFlipTile(store: GalleryStore) : unit =
  store.TileTapCount <- store.TileTapCount + 1
  store.LastAction <- $"Flip tile tapped ({store.TileTapCount})"

/// The start section of the hub: the live tile and six iconic tiles.
let startTiles
  (
    store: GalleryStore,
    cb: HubCallbacks,
    liveFactory: unit -> MetroLiveTileCell,
    cycleFactory: unit -> MetroCycleTileCell,
    flipFactory: unit -> MetroFlipTileCell
  ) : Blob =
  Container(Layout = tileGrid)
    .Children(
      MetroLiveTile.mount(
        "gallery-live-tile",
        MetroLiveTile.defaults liveFaces,
        liveFactory
      ),
      MetroCycleTile.mount(
        "gallery-cycle-tile",
        // The secondary cycler runs calmer than the telemetry live tile, so
        // the two cycling tiles do not fight on the start section. It stays
        // medium: a second wide tile pushes the small tiles out of the frame.
        {
          MetroCycleTile.defaults cycleFaces with
              IntervalMs = 6000.0
        },
        cycleFactory
      ),
      MetroFlipTile.mount(
        "gallery-flip-tile",
        MetroFlipTile.defaults(flipFront, flipBack),
        flipFactory
      ),
      MetroTilt.mount(
        "tile-mail",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Mail", "mail") with
                Count = ValueSome store.MailCount
                Size = MetroTileSize.Medium
          },
          fun () -> openMailTile store
        )
      ),
      MetroTilt.mount(
        "tile-calendar",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Calendar", "calendar_today") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(0, 163, 0))
          },
          fun () -> openTile(store, "Calendar")
        )
      ),
      MetroTilt.mount(
        "tile-music",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Music", "music_note") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(250, 104, 0))
          },
          fun () -> openTile(store, "Music")
        )
      ),
      MetroTilt.mount(
        "tile-photos",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Photos", "photo") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(162, 0, 255))
          },
          fun () -> openTile(store, "Photos")
        )
      ),
      MetroTilt.mount(
        "tile-settings",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Settings", "settings") with
                Size = MetroTileSize.Small
                AccentOverride = ValueSome(Color.Rgb(100, 118, 135))
          },
          fun () -> cb.OpenSettings()
        )
      ),
      MetroTilt.mount(
        "tile-controls",
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Explore Input", "tune") with
                Size = MetroTileSize.Wide
                AccentOverride = ValueSome(Color.Rgb(0, 80, 239))
          },
          fun () -> cb.OpenTab "input"
        )
      )
    )

/// The status section of the hub: the last action, the slider value as a
/// progress bar, and two buttons.
let statusSection(store: GalleryStore, cb: HubCallbacks) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
    .Children(
      MetroText.subtitle "Glance and go",
      MetroText.secondary
        $"Last action: {store.LastAction}. Tile taps: {store.TileTapCount}.",
      MetroText.caption "LIVE PROGRESS",
      Container()
        .Children(
          MetroProgressBar.build(
            "hub-det-bar",
            MetroProgressBar.determinate(store.SliderValue / 100.0)
          )
        ),
      Container(
        FlexDirection = FlexDirection.Row,
        Gap = Length.Pixels 12.0,
        AlignItems = AlignItems.Center
      )
        .Children(
          MetroTilt.mount(
            "hub-customize-theme",
            MetroButton.accent("Customize Theme", fun () -> cb.OpenSettings())
          ),
          MetroTilt.mount(
            "hub-show-dialog",
            MetroButton.standard("Show Dialog", fun () -> cb.OpenDialog())
          )
        )
    )

/// The principles section of the hub: two short texts.
let principlesSection() : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.subtitle "Fierce reduction",
      MetroText.body(
        "Typography, whitespace, and alignment replace "
        + "card boxes and drop shadows."
      ),
      MetroText.subtitle "Authentically digital",
      MetroText.secondary(
        "Sharp 0 px corners, 2 px borders, and fast "
        + "cubic Bezier deceleration."
      )
    )

/// Builds one section of the hub. The Hub cell calls this function inside its
/// own `Build`, so it owns the callbacks of the section.
let hubSection
  (
    store: GalleryStore,
    cb: HubCallbacks,
    tileFactories: TileFactories,
    sectionId: string
  ) : Blob =
  match sectionId with
  | "start" ->
    let liveFactory, cycleFactory, flipFactory = tileFactories
    startTiles(store, cb, liveFactory, cycleFactory, flipFactory)
  | "status" -> statusSection(store, cb)
  | _ -> principlesSection()

/// Creates the hub cell of the gallery. The cell renders each section through
/// `hubSection`.
let createHub
  (store: GalleryStore, cb: HubCallbacks, tileFactories: TileFactories)
  : MetroHubCell =
  MetroHub.create(fun sectionId ->
    hubSection(store, cb, tileFactories, sectionId))

/// Mounts the hub of the gallery: the full-bleed `MetroHub` itself.
let mountHub(hubFactory: unit -> MetroHubCell) : Blob =
  MetroHub.mount(
    "gallery-hub",
    MetroHub.defaults("metro gallery", hubSections),
    hubFactory
  )
