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

  // The libvlc service. Its callbacks land on the UI thread through `post`;
  // the one late-bound piece is the service itself, for auto-advance at the
  // end of a track.
  let mutable playbackSlot: IPlayback voption = ValueNone

  let playback =
    Playback.create post {
      OnPosition = fun percent _lengthMs -> CVal.set percent player.position
      OnState = fun playing -> CVal.set playing player.playing
      OnEnded =
        fun () ->
          let count = Player.songCount player
          let songs = AList.toList player.songs
          let index = AVal.getValue player.selected

          let playAt next =
            let song = List.item next songs
            Player.pickSong player song
            CVal.set true player.hasCurrent

            playbackSlot |> ValueOption.iter(fun pb -> pb.Play song)

          if count > 0 then
            match AVal.getValue player.loop with
            | LoopState.Single -> playAt index
            | LoopState.All -> playAt((index + 1) % count)
            | LoopState.Off when index + 1 < count -> playAt(index + 1)
            | _ -> ()
      OnError = fun message -> eprintfn "playback: %s" message
    }

  playbackSlot <- ValueSome playback

  // The file system service is stateless; widgets and playlist loaders call
  // it from background threads and post the entries back to the UI.
  let fileSystem = FileSystem.create()

  let env: Env = {
    PostUI = post
    Playback = playback
    FileSystem = fileSystem
  }

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
