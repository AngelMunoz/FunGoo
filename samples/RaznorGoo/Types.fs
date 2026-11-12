module GooRes.Types

open Goo

type Song = { Name: string; Path: string }

type LoopState =
  | Off
  | All
  | Single

// Handler bundle wired once at the composition root. An argument carrier,
// not a props record: it never feeds a Build or a comparison.
type PlaybackEvents = {
  OnPosition: float32 -> int64 -> unit
  OnState: bool -> unit
  OnEnded: unit -> unit
  OnError: string -> unit
}

type IPlayback =
  abstract Play: Song -> unit
  abstract Pause: unit -> unit
  abstract Resume: unit -> unit
  abstract Stop: unit -> unit
  abstract SeekSeconds: float32 -> unit
  abstract Volume: unit -> float
  abstract SetVolume: float -> unit

  // Handlers arrive on the UI thread: the service marshals internally,
  // so no subscriber can forget to.
  abstract Subscribe: handlers: PlaybackEvents -> unit

// One row of a directory listing: a sub-folder or a file.
type FsEntry = {
  Name: string
  Path: string
  IsFolder: bool
}

// System file and folder lookups. `List` returns the sub-folders of a folder
// first, then the files that match the extensions, both sorted by name; an
// empty extension list keeps every file. Extension filters accept `*.mp3`,
// `.mp3` and `mp3` forms. `Search` keeps the entries of one folder whose name
// contains the query, case-insensitive, with the same ordering; an empty
// query keeps everything. `Drives` lists the ready drives of the system.
// `Parent` gives the parent folder of a path, or none at a drive root.
type IFileSystem =
  abstract List: path: string * extensions: string seq -> FsEntry[]

  abstract Search:
    path: string * query: string * extensions: string seq -> FsEntry[]

  abstract Drives: unit -> FsEntry[]

  abstract Parent: path: string -> string voption

// Capabilities: each function names only the accessors it uses, and the
// composition root builds one object that implements them all.
type IPostUI =
  abstract Post: (unit -> unit) -> unit

type IProvidePlayback =
  abstract Playback: IPlayback

type IProvideFileSystem =
  abstract FileSystem: IFileSystem

type IProvidePostUI =
  abstract PostUI: IPostUI

// The production bundle, for the composition root only: it exists so one
// object can satisfy several capability constraints at once.
type IAppEnv =
  inherit IProvidePlayback
  inherit IProvideFileSystem
  inherit IProvidePostUI

// The one seam the window forces: `Root` is init-only, so the window is
// built last while background threads must marshal first. Attach once after
// the window exists; every `Post` before that is dropped.
type UiMarshaller() =
  let mutable window: Window voption = ValueNone

  let post(action: unit -> unit) =
    window
    |> ValueOption.iter(fun window ->
      window.TryPost(fun () -> action()) |> ignore)

  member _.Attach(w: Window) = window <- ValueSome w

  member _.Post(action: unit -> unit) = post action

  interface IPostUI with
    member _.Post action = post action
