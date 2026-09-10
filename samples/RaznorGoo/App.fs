module GooRes.App

open System

open Goo
open GooRes.Types
open GooRes.Widgets
open GooRes.Widgets.FilePicker
open Mibo.Adaptive

let run() =
  Window.ConfigureApplication(
    "RaznorGoo",
    "0.1.0",
    "io.github.angelmunoz.raznor"
  )

  let musicFilters = [ "*.mp3"; "*.wav" ]
  let player = Player.create()

  let mutable gooWindow: Window voption = ValueNone

  let inline post(action: unit -> unit) =
    gooWindow
    |> ValueOption.iter(fun window ->
      window.TryPost(fun () ->
        action()
        window.Root.Rebuild())
      |> ignore)

  // The file system service is stateless; widgets and playlist loaders call
  // it from background threads and post the entries back to the UI.
  let fileSystem = FileSystem.create()

  // The libvlc service. Its callbacks land on the UI thread through `post`.
  // The one late-bound piece is the environment: the end-of-track handler
  // needs it, but it is built out of the playback service that owns the
  // handler.
  let mutable envSlot: Env voption = ValueNone

  let playback =
    Playback.create post {
      OnPosition =
        fun percent lengthMs ->
          let length = float32 lengthMs / 1000.0f
          CVal.set length player.length
          CVal.set (percent / 100.0f * length) player.position
      OnState = fun playing -> CVal.set playing player.playing
      OnEnded =
        fun () ->
          envSlot |> ValueOption.iter(fun env -> Player.skipNext env player)
      OnError = fun message -> eprintfn "playback: %s" message
    }

  let env: Env = {
    PostUI = post
    Playback = playback
    FileSystem = fileSystem
  }

  envSlot <- ValueSome env

  let loadSongsFromFolder(dir: string) =
    async {
      let songs =
        env.FileSystem.List(dir, musicFilters)
        |> List.filter(fun e -> not e.IsFolder)
        |> List.map(fun e -> { Name = e.Name; Path = e.Path })

      post(fun () -> Player.replaceSongs player songs)
    }
    |> Async.Start

  let filesPicked(entries: FsEntry list) =
    Player.replaceSongs
      player
      (entries |> List.map(fun e -> { Name = e.Name; Path = e.Path }))

  let picker =
    FilePicker.create env {
      filters = musicFilters
      startIn = Environment.GetFolderPath Environment.SpecialFolder.MyMusic
      onFilesSelected = filesPicked
      onFolderSelected = loadSongsFromFolder
    }

  let window =
    Window(
      Title = "RaznorGoo",
      Width = 720,
      Height = 480,
      Root = {
        new Cell() with
          override _.Build() : Blob = Shell.view env player picker
      }
    )

  gooWindow <- ValueSome window

  window.Run()
