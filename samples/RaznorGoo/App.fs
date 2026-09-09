module GooRes.App

open System
open System.IO

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

  let mutable shellCell: Cell voption = ValueNone
  let mutable gooWindow: Window voption = ValueNone

  let inline post(action: unit -> unit) =
    gooWindow
    |> ValueOption.iter(fun window ->
      shellCell
      |> ValueOption.iter(fun shell ->
        window.TryPost(fun () ->
          action()
          shell.Rebuild())
        |> ignore))

  // The libvlc service. Its callbacks land on the UI thread through `post`;
  // the one late-bound piece is the service itself, for auto-advance at the
  // end of a track.
  let mutable playbackSlot: IPlayback voption = ValueNone

  let playback =
    Playback.live post {
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

  let env: Env = { Post = post; Playback = playback }

  let loadSongsFromFolder(dir: string) =
    async {
      let files =
        Array.ofList musicFilters
        |> Array.collect(fun pattern -> Directory.GetFiles(dir, pattern))
        |> Array.distinct
        |> Array.sort
        |> Array.map(fun f -> { Name = Path.GetFileName f; Path = f })

      post(fun () -> Player.replaceSongs player (List.ofArray files))
    }
    |> Async.Start

  let filesPicked(paths: string list) =
    Player.replaceSongs
      player
      (paths |> List.map(fun f -> { Name = Path.GetFileName f; Path = f }))

  let picker =
    FilePicker.create {
      filters = musicFilters
      postUI = post
      onFilesSelected = filesPicked
      onFolderSelected = loadSongsFromFolder
    }

  let root = {
    new Cell() with
      override _.Build() : Blob = Shell.view env player picker
  }

  let window =
    Window(Title = "RaznorGoo", Width = 720, Height = 480, Root = root)

  shellCell <- ValueSome root
  gooWindow <- ValueSome window

  window.Run()
