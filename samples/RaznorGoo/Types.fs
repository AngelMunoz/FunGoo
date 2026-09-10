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
  abstract SeekSeconds: float32 -> unit
  abstract Volume: unit -> float
  abstract SetVolume: float -> unit

type PlaybackEvents = {
  OnPosition: float32 -> int64 -> unit
  OnState: bool -> unit
  OnEnded: unit -> unit
  OnError: string -> unit
}

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
  abstract List: path: string * extensions: string list -> FsEntry list

  abstract Search:
    path: string * query: string * extensions: string list -> FsEntry list

  abstract Drives: unit -> FsEntry list

  abstract Parent: path: string -> string voption

type Env = {
  PostUI: (unit -> unit) -> unit
  Playback: IPlayback
  FileSystem: IFileSystem
}
