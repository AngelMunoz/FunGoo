module MetroGallery.Program

open System
open System.Collections.Generic
open Goo
open FunGoo.Metro

/// Gallery tabs that also register as named `goo render` scenes, in name order.
let sceneTabs = [|
  "controls"
  "flip-view"
  "inputs"
  "lists"
  "motion"
  "overview"
  "selection"
  "typography"
  "zoom"
|]

/// Maps the window theme preference onto a Metro appearance mode.
let sceneMode(window: Window) : MetroMode =
  if window.Preferences.Theme = SystemTheme.Dark then
    MetroMode.Dark
  else
    MetroMode.Light

/// Named gallery states for `goo render --scene <name>`. A scene starts on its
/// tab and follows the `--theme` preference in the Metro palette and the
/// gallery mode state.
let galleryScenes: Gallery.ScenesFactory =
  fun getWindow ->
    let scenes = Dictionary<string, Func<Cell>>()

    for tab in sceneTabs do
      scenes.[tab] <-
        fun () ->
          let mutable mode = MetroMode.Dark

          getWindow()
          |> ValueOption.iter(fun window ->
            mode <- sceneMode window
            Metro.setMode(window, mode))

          Gallery.create(getWindow, { Tab = tab; Mode = mode }) :> Cell

    scenes :> IReadOnlyDictionary<string, Func<Cell>>

[<EntryPoint>]
let main(args: string array) : int =
  if args |> Array.exists(fun arg -> arg = "--check") then
    GalleryCheck.run(sceneTabs, galleryScenes)
  else
    Application.Configure(
      "MetroGallery",
      "0.1.0",
      "io.github.fungoo.metrogallery"
    )

    let mutable windowSlot: Window voption = ValueNone

    let gallery = Gallery.create((fun () -> windowSlot), Gallery.overviewStart)

    let window =
      Window(
        Title = "Metro Gallery",
        Size = LayoutSize(1200, 800),
        Root = gallery,
        Scenes = galleryScenes(fun () -> windowSlot)
      )

    windowSlot <- ValueSome window

    Metro.attach(window, Gallery.themeConfig)
    Application.Run window
    0
