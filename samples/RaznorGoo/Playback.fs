module GooRes.Playback

open System
open GooRes.Types
open LibVLCSharp.Shared

// The service owns one LibVLC and one MediaPlayer for the whole app. VLC
// raises its events on its own threads; every callback is handed to the app
// through `post`, so state changes and playback commands always run on the
// UI thread.
let create (post: (unit -> unit) -> unit) (events: PlaybackEvents) : IPlayback =
  Core.Initialize()

  let libvlc = new LibVLC("--no-video")
  let mediaPlayer = new MediaPlayer(libvlc)
  let mutable current: Media option = None
  let mutable lengthMs = 0L

  let inline releaseCurrent() =
    current |> Option.iter(fun media -> media.Dispose())
    current <- None

  let inline play(song: Song) : unit =
    if not(String.IsNullOrWhiteSpace song.Path) then
      releaseCurrent()

      let media = new Media(libvlc, song.Path, FromType.FromPath)
      current <- Some media
      mediaPlayer.Play media |> ignore

  mediaPlayer.LengthChanged.Add(fun args -> lengthMs <- args.Length)

  mediaPlayer.TimeChanged.Add(fun args ->
    if lengthMs > 0L then
      let percent = float32 args.Time * 100.0f / float32 lengthMs
      post(fun () -> events.OnPosition percent lengthMs))

  mediaPlayer.Playing.Add(fun _ -> post(fun () -> events.OnState true))
  mediaPlayer.Paused.Add(fun _ -> post(fun () -> events.OnState false))

  mediaPlayer.Stopped.Add(fun _ ->
    post(fun () ->
      events.OnState false
      events.OnPosition 0.0f lengthMs))

  mediaPlayer.EncounteredError.Add(fun _ ->
    post(fun () -> events.OnError "LibVLC reported a playback error"))

  mediaPlayer.EndReached.Add(fun _ ->
    post(fun () ->
      events.OnState false
      events.OnEnded()))

  {
    new IPlayback with
      member _.Play song = play song
      member _.Pause() = mediaPlayer.SetPause true
      member _.Resume() = mediaPlayer.SetPause false

      member _.Stop() =
        mediaPlayer.Stop()
        releaseCurrent()

      member _.SeekSeconds seconds =
        mediaPlayer.Time <- int64(seconds * 1000.0f)

      member _.Volume() =
        let raw = max 0 (min 100 mediaPlayer.Volume)
        float raw / 100.0

      member _.SetVolume(value: float) =
        let clamped = Math.Clamp(value, 0.0, 1.0)
        mediaPlayer.Volume <- int(round(clamped * 100.0))
  }
