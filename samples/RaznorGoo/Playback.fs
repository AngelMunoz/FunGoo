module GooRes.Playback

open System
open GooRes.Types
open LibVLCSharp.Shared

// The service owns one LibVLC and one MediaPlayer for the whole app. VLC
// raises its events on its own threads; the service marshals every handler
// through `marshal`, so subscribers always run on the UI thread. The whole
// surface — control and subscription — is one interface: nothing escapes
// `create` as a loose function.
let create(marshal: (unit -> unit) -> unit) : IPlayback =
  Core.Initialize()

  let libvlc = new LibVLC("--no-video")
  let mediaPlayer = new MediaPlayer(libvlc)
  let mutable current: Media option = None
  let mutable lengthMs = 0L

  let onPosition = Event<float32 * int64>()
  let onState = Event<bool>()
  let onEnded = Event<unit>()
  let onError = Event<string>()

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
      onPosition.Trigger(percent, lengthMs))

  mediaPlayer.Playing.Add(fun _ -> onState.Trigger true)
  mediaPlayer.Paused.Add(fun _ -> onState.Trigger false)

  mediaPlayer.Stopped.Add(fun _ ->
    onState.Trigger false
    onPosition.Trigger(0.0f, lengthMs))

  mediaPlayer.EncounteredError.Add(fun _ ->
    onError.Trigger "LibVLC reported a playback error")

  mediaPlayer.EndReached.Add(fun _ -> onEnded.Trigger())

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

      member _.Subscribe(events: PlaybackEvents) =
        onPosition.Publish.Add(fun (percent, ms) ->
          marshal(fun () -> events.OnPosition percent ms))

        onState.Publish.Add(fun playing ->
          marshal(fun () -> events.OnState playing))

        onEnded.Publish.Add(fun () -> marshal(fun () -> events.OnEnded()))

        onError.Publish.Add(fun message ->
          marshal(fun () -> events.OnError message))
  }
