module GooRes.Player

open GooRes.Types

// The player store: one mutable field per concern, one immutable value per
// concern. Cells read it inside `Build`; writers replace whole values and
// then rebuild the cells that read the changed slice.
type PlayerState = {
  mutable Songs: Song array
  mutable Selected: int voption
  mutable Playing: bool
  mutable Position: float32
  mutable Length: float32
  mutable Loop: LoopState
}

let create() : PlayerState = {
  Songs = Array.empty
  Selected = ValueNone
  Playing = false
  Position = 0.0f
  Length = 0.0f
  Loop = LoopState.Off
}

let replaceSongs(p: PlayerState, songs: Song array) =
  p.Songs <- songs
  p.Selected <- ValueNone

let pickSong(p: PlayerState, song: Song) =
  p.Songs
  |> Array.tryFindIndex(fun s -> s = song)
  |> Option.iter(fun index -> p.Selected <- ValueSome index)

// Fisher-Yates over the playlist. The playing song keeps the selection so
// the highlight and the media bar stay on it.
let shuffle(p: PlayerState) =
  match p.Selected with
  | ValueNone -> p.Songs <- Array.randomShuffle p.Songs
  | ValueSome index ->
    let current = Array.tryItem index p.Songs
    p.Songs <- Array.randomShuffle p.Songs

    match current with
    | Some song -> pickSong(p, song)
    | None -> p.Selected <- ValueNone

let cycleLoop(p: PlayerState) =
  p.Loop <-
    match p.Loop with
    | LoopState.Off -> LoopState.All
    | LoopState.All -> LoopState.Single
    | LoopState.Single -> LoopState.Off

let playSong(env: #IProvidePlayback, p: PlayerState, song: Song) =
  pickSong(p, song)
  env.Playback.Play song
  p.Playing <- true

// Plays the playlist entry at `index`, when the list has one there.
let playAt(env: #IProvidePlayback, p: PlayerState, index: int) =
  p.Songs
  |> Array.tryItem index
  |> Option.iter(fun song -> playSong(env, p, song))

let playPause(env: #IProvidePlayback, p: PlayerState) =
  match p.Selected with
  | ValueSome index when index >= 0 ->
    if p.Playing then
      env.Playback.Pause()
      p.Playing <- false
    else
      env.Playback.Resume()
      p.Playing <- true
  | _ ->
    p.Songs |> Array.tryHead |> Option.iter(fun song -> playSong(env, p, song))

// The index of the track to play after the one at `index`, following the
// loop state: the same track under `LoopState.Single`, the following one
// under `LoopState.All` (wrapping) and `LoopState.Off` (stopping after the
// last track of the list). A missing selection starts from the first track.
let nextIndex(p: PlayerState, index: int voption) : int voption =
  let count = p.Songs.Length

  let advance(current: int) =
    match p.Loop with
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
let skipNext(env: #IProvidePlayback, p: PlayerState) =
  nextIndex(p, p.Selected) |> ValueOption.iter(fun next -> playAt(env, p, next))

// Restarts the selected track, or the first one when nothing is selected.
let skipPrevious(env: #IProvidePlayback, p: PlayerState) =
  p.Selected
  |> ValueOption.defaultValue -1
  |> max 0
  |> fun index -> playAt(env, p, index)

// Jumps the current track to `seconds` from its start. The transport hands
// over float seconds; the playback member takes float32.
let seek(env: #IProvidePlayback, seconds: float) =
  env.Playback.SeekSeconds(float32 seconds)

// Sets the playback volume. `value` runs from 0.0 (silent) through 1.0.
let setVolume(env: #IProvidePlayback, value: float) =
  env.Playback.SetVolume value
