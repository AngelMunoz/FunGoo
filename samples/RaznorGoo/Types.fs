module GooRes.Types

type Song = { Name: string; Path: string }

type LoopState =
  | Off
  | All
  | Single

type IPlayback =
  abstract Play: Song -> unit
  abstract Pause: unit -> unit
  abstract Resume: unit -> unit
  abstract Stop: unit -> unit
  abstract SeekPercent: float32 -> unit

type PlaybackEvents = {
  OnPosition: float32 -> int64 -> unit
  OnState: bool -> unit
  OnEnded: unit -> unit
  OnError: string -> unit
}

type Env = {
  Post: (unit -> unit) -> unit
  Playback: IPlayback
}
