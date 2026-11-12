module GooRes.App

open System

open Goo
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

  // Services and the marshaller come first: the cells receive env, and the
  // window attaches last because Root is init-only.
  let marshaller = UiMarshaller()
  let fileSystem = FileSystem.create()

  // Construction is linear: service, environment, store, cells, picker.
  // Nothing references a value defined after it.
  let playback = Playback.create marshaller.Post

  let env = {
    new IAppEnv with
      member _.Playback = playback
      member _.FileSystem = fileSystem
      member _.PostUI = marshaller :> IPostUI
  }

  let player = Player.create()

  let playlist =
    MediaList.create(player, fun song -> Player.playSong(env, player, song))

  let bottomBar = BottomBar.create(env, player, fun () -> playlist.Rebuild())

  let musicFilters = [ "*.mp3"; "*.wav" ]

  let loadSongsFromFolder(dir: string) =
    async {
      let songs =
        env.FileSystem.List(dir, musicFilters)
        |> Array.filter(fun e -> not e.IsFolder)
        |> Array.map(fun e -> { Name = e.Name; Path = e.Path })

      env.PostUI.Post(fun () ->
        Player.replaceSongs(player, songs)
        playlist.Rebuild())
    }
    |> Async.Start

  let filesPicked(entries: FsEntry[]) =
    Player.replaceSongs(
      player,
      entries |> Array.map(fun e -> { Name = e.Name; Path = e.Path })
    )

    playlist.Rebuild()

  let picker =
    FilePicker.create(
      env,
      {
        filters = musicFilters
        startIn = Environment.GetFolderPath Environment.SpecialFolder.MyMusic
      },
      filesPicked,
      loadSongsFromFolder
    )

  let root = Layout.view(picker, playlist, bottomBar)

  let window =
    Window(Title = "RaznorGoo", Width = 720, Height = 480, Root = root)

  marshaller.Attach(window)

  // Subscriptions come last: every value the handlers touch exists, so no
  // handler needs a slot to reach its targets. The service delivers them
  // on the UI thread, and the wiring goes through the capability — the
  // service contract, not a function kept beside it.
  env.Playback.Subscribe {
    OnPosition =
      fun percent lengthMs ->
        let length = float32 lengthMs / 1000.0f
        player.Length <- length
        player.Position <- percent / 100.0f * length
        bottomBar.Rebuild()
    OnState =
      fun playing ->
        player.Playing <- playing
        bottomBar.Rebuild()
    OnEnded =
      fun () ->
        Player.skipNext(env, player)
        playlist.Rebuild()
    OnError = fun message -> eprintfn "playback: %s" message
  }

  window.Run()
