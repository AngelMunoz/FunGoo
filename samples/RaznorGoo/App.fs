module GooRes.App

open System

open Goo
open Mibo.Adaptive
open GooRes.Types
open GooRes.Widgets
open GooRes.Widgets.FilePicker
open GooRes.Layout

let run() =
  Window.ConfigureApplication(
    "RaznorGoo",
    "0.1.0",
    "io.github.angelmunoz.raznor"
  )

  let musicFilters = [ "*.mp3"; "*.wav" ]
  let player = Player.create()

  // The one late binding the window forces: Root is init-only, so the
  // window is built last while the marshal below is needed first. Declared
  // up front, assigned once, read only at call time.
  let mutable gooWindow: Window voption = ValueNone

  // Foreign threads (VLC, async loads) may only marshal work to the UI
  // thread through here. What the work touches is the caller's decision.
  let inline marshal(action: unit -> unit) =
    gooWindow
    |> ValueOption.iter(fun window ->
      window.TryPost(fun () -> action()) |> ignore)

  // The picker body is still a root blob until it becomes a cell, so its
  // loads are the only remaining root rebuilds.
  let inline post(action: unit -> unit) =
    gooWindow
    |> ValueOption.iter(fun window ->
      window.TryPost(fun () ->
        action()
        window.Root.Rebuild())
      |> ignore)

  let fileSystem = FileSystem.create()

  // Construction is linear: service, environment, stateful cells, picker.
  // Nothing references a value defined after it.
  let playback, subscribePlayback = Playback.create marshal

  let env: Env = {
    PostUI = post
    Playback = playback
    FileSystem = fileSystem
  }

  let playlist =
    MediaList.create {
      songs = player.songs
      selected = player.selected
      onSelect = fun song -> Player.playSong env player song
    }

  let bottomBar = BottomBar.create env player (fun () -> playlist.Rebuild())

  let loadSongsFromFolder(dir: string) =
    async {
      let songs =
        env.FileSystem.List(dir, musicFilters)
        |> Array.filter(fun e -> not e.IsFolder)
        |> Array.map(fun e -> { Name = e.Name; Path = e.Path })

      marshal(fun () ->
        Player.replaceSongs player songs
        playlist.Rebuild())
    }
    |> Async.Start

  let filesPicked(entries: FsEntry[]) =
    Player.replaceSongs
      player
      (entries |> Array.map(fun e -> { Name = e.Name; Path = e.Path }))

    playlist.Rebuild()

  let picker =
    FilePicker.create env {
      filters = musicFilters
      startIn = Environment.GetFolderPath Environment.SpecialFolder.MyMusic
      onFilesSelected = filesPicked
      onFolderSelected = loadSongsFromFolder
    }

  let root = Layout.view picker playlist bottomBar

  let window =
    Window(Title = "RaznorGoo", Width = 720, Height = 480, Root = root)

  gooWindow <- ValueSome window

  // Subscriptions come last: every value the handlers touch exists, so no
  // handler needs a slot to reach its targets. The service delivers them
  // on the UI thread.
  subscribePlayback {
    OnPosition =
      fun percent lengthMs ->
        let length = float32 lengthMs / 1000.0f
        CVal.set length player.length
        CVal.set (percent / 100.0f * length) player.position
        bottomBar.Rebuild()
    OnState =
      fun playing ->
        CVal.set playing player.playing
        bottomBar.Rebuild()
    OnEnded =
      fun () ->
        Player.skipNext env player
        playlist.Rebuild()
    OnError = fun message -> eprintfn "playback: %s" message
  }

  window.Run()
