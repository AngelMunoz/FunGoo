module GooRes.Player

open System
open GooRes.Types
open Mibo.Adaptive

type PlayerState = {
  songs: clist<Song>
  selected: int voption cval
  playing: bool cval
  position: float32 cval
  length: float32 cval
  loop: LoopState cval
  nowPlaying: string aval
}

let inline create() : PlayerState =
  let songs = CList.empty<Song>
  let selected = CVal.create ValueNone

  let nowPlaying =
    selected
    |> AVal.bind(fun index ->
      index
      |> ValueOption.map(fun index -> AList.tryAt index songs)
      |> ValueOption.defaultValue(AVal.constant ValueNone))
    |> AVal.map(fun songOpt ->
      match songOpt with
      | ValueSome song -> $"Playing: {song.Name}"
      | ValueNone -> "Nothing loaded")

  {
    songs = songs
    selected = selected
    playing = CVal.create false
    position = CVal.create 0.0f
    length = CVal.create 0.0f
    loop = CVal.create LoopState.Off
    nowPlaying = nowPlaying
  }

let inline songCount(p: PlayerState) : int = AVal.getValue(AList.count p.songs)

let replaceSongs (p: PlayerState) (songs: Song[]) : unit =
  p.songs.Set songs
  CVal.set ValueNone p.selected

let pickSong (p: PlayerState) (song: Song) : unit =
  let songs = p.songs |> AList.toList

  songs
  |> List.tryFindIndex(fun s -> s = song)
  |> Option.iter(fun index -> CVal.set (ValueSome index) p.selected)

// Fisher-Yates over the playlist. The playing song keeps the selection so
// the highlight and the media bar stay on it.
let shuffle(p: PlayerState) : unit =
  let songs = AList.toArray p.songs

  match AVal.getValue p.selected with
  | ValueNone -> songs |> Array.randomShuffle |> p.songs.Set
  | ValueSome index ->
    let current = Array.tryItem index songs
    songs |> Array.randomShuffle |> p.songs.Set

    match current with
    | Some song -> pickSong p song
    | None -> CVal.set ValueNone p.selected



let cycleLoop(p: PlayerState) : unit =
  let next =
    match AVal.getValue p.loop with
    | LoopState.Off -> LoopState.All
    | LoopState.All -> LoopState.Single
    | LoopState.Single -> LoopState.Off

  CVal.set next p.loop

let playSong (env: Env) (p: PlayerState) (song: Song) : unit =
  pickSong p song
  env.Playback.Play song
  p.playing.Set true

// Plays the playlist entry at `index`, when the list has one there.
let playAt (env: Env) (p: PlayerState) (index: int) : unit =
  AList.tryAt index p.songs
  |> AVal.getValue
  |> ValueOption.iter(fun song -> playSong env p song)

let playPause (env: Env) (p: PlayerState) : unit =
  let selected = AVal.getValue p.selected

  if AVal.getValue p.playing then
    env.Playback.Pause()
    p.playing.Set false
  elif selected.IsSome && selected.Value >= 0 then
    env.Playback.Resume()
    p.playing.Set true
  else
    AList.tryFirst p.songs
    |> AVal.getValue
    |> ValueOption.iter(fun song -> playSong env p song)

// The index of the track to play after the one at `index`, following the
// loop state: the same track under `LoopState.Single`, the following one
// under `LoopState.All` (wrapping) and `LoopState.Off` (stopping after the
// last track of the list). A missing selection starts from the first track.
let nextIndex (p: PlayerState) (index: int voption) : int voption =
  let count = songCount p

  let advance(current: int) =
    match AVal.getValue p.loop with
    | LoopState.Single -> ValueSome current
    | LoopState.All -> ValueSome((current + 1) % count)
    | LoopState.Off when current + 1 < count -> ValueSome(current + 1)
    | LoopState.Off -> ValueNone

  if count = 0 then
    ValueNone
  else
    index |> ValueOption.map advance |> ValueOption.defaultValue(ValueSome 0)

// Plays the next track of the playlist under the loop state. The `Next`
// button and the end-of-track auto-advance both use it.
let skipNext (env: Env) (p: PlayerState) : unit =
  AVal.getValue p.selected
  |> nextIndex p
  |> ValueOption.iter(fun next -> playAt env p next)

// Restarts the selected track, or the first one when nothing is selected.
let skipPrevious (env: Env) (p: PlayerState) : unit =
  AVal.getValue p.selected
  |> ValueOption.defaultValue -1
  |> max 0
  |> playAt env p

// Jumps the current track to `seconds` from its start. The transport hands
// over float seconds; the playback member takes float32.
let seek (env: Env) (seconds: float) : unit =
  env.Playback.SeekSeconds(float32 seconds)

// Sets the playback volume. `value` runs from 0.0 (silent) through 1.0.
let setVolume (env: Env) (value: float) : unit = env.Playback.SetVolume value
