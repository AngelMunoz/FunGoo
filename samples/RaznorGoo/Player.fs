module GooRes.Player

open System
open GooRes.Types
open Mibo.Adaptive

type PlayerState = {
  songs: clist<Song>
  selected: int cval
  playing: bool cval
  position: float32 cval
  loop: LoopState cval
  hasCurrent: bool cval
}

let create() : PlayerState = {
  songs = CList.empty<Song>
  selected = CVal.create 0
  playing = CVal.create false
  position = CVal.create 0.0f
  loop = CVal.create LoopState.Off
  hasCurrent = CVal.create false
}

let inline songCount(p: PlayerState) : int = AVal.getValue(AList.count p.songs)

let nowPlaying(p: PlayerState) : string =
  match AList.toList p.songs |> List.tryItem(AVal.getValue p.selected) with
  | Some song -> $"Playing: {song.Name}"
  | None -> "Nothing loaded"

let replaceSongs (p: PlayerState) (songs: Song list) : unit =
  p.songs.Set songs
  CVal.set 0 p.selected

let pickSong (p: PlayerState) (song: Song) : unit =
  let songs = AList.toList p.songs

  match songs |> List.tryFindIndex(fun s -> s = song) with
  | Some index -> CVal.set index p.selected
  | None -> ()

// Fisher-Yates over the playlist. The playing song keeps the selection so
// the highlight and the media bar stay on it.
let shuffle(p: PlayerState) : unit =
  let songs = AList.toList p.songs
  let current = List.tryItem (AVal.getValue p.selected) songs
  let arr = Array.ofList songs

  for i = arr.Length - 1 downto 1 do
    let j = Random.Shared.Next(i + 1)
    let swap = arr[i]
    arr[i] <- arr[j]
    arr[j] <- swap

  p.songs.Set(List.ofArray arr)

  match current with
  | Some song -> pickSong p song
  | None -> CVal.set 0 p.selected

let cycleLoop(p: PlayerState) : unit =
  let next =
    match AVal.getValue p.loop with
    | LoopState.Off -> LoopState.All
    | LoopState.All -> LoopState.Single
    | LoopState.Single -> LoopState.Off

  CVal.set next p.loop

let playSong (env: Env) (p: PlayerState) (song: Song) : unit =
  pickSong p song
  CVal.set true p.hasCurrent
  env.Playback.Play song

let playPause (env: Env) (p: PlayerState) : unit =
  if AVal.getValue p.playing then
    env.Playback.Pause()
  elif AVal.getValue p.hasCurrent then
    env.Playback.Resume()
  else
    match AList.toList p.songs with
    | [] -> ()
    | first :: _ -> playSong env p first

let skipNext (env: Env) (p: PlayerState) : unit =
  let count = songCount p

  if count > 0 then
    let index = AVal.getValue p.selected

    let next =
      match AVal.getValue p.loop with
      | LoopState.Single -> index
      | LoopState.All -> (index + 1) % count
      | LoopState.Off -> if index + 1 < count then index + 1 else index

    CVal.set next p.selected

    if AVal.getValue p.playing then
      let song = List.item next (AList.toList p.songs)
      CVal.set true p.hasCurrent
      env.Playback.Play song

let skipPrevious (env: Env) (p: PlayerState) : unit =
  let count = songCount p

  if count > 0 then
    let index = max 0 (AVal.getValue p.selected - 1)
    CVal.set index p.selected

    if AVal.getValue p.playing then
      let song = List.item index (AList.toList p.songs)
      CVal.set true p.hasCurrent
      env.Playback.Play song
