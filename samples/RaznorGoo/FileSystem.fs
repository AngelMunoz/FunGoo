module GooRes.FileSystem

open System
open System.IO
open GooRes.Types

// The service owns every directory and file lookup the app does. It is
// synchronous and stateless: callers run it on background threads and hand
// the results to the UI thread through `post`. Unreadable paths yield no
// entries instead of throwing, so one bad folder never breaks the picker or
// the playlist.
let create() : IFileSystem =
  let comparison = StringComparison.OrdinalIgnoreCase

  // `*.mp3`, `.mp3` and `mp3` all name the mp3 extension.
  let inline normalize(filter: string) =
    let trimmed = filter.TrimStart('*').Trim()

    if trimmed.StartsWith '.' then trimmed else $".{trimmed}"

  let inline matchesExtensions(extensions: string list, name: string) =
    List.isEmpty extensions
    || List.exists (fun ext -> name.EndsWith(ext, comparison)) extensions

  let inline entry(path: string, isFolder: bool) = {
    Name = Path.GetFileName path
    Path = path
    IsFolder = isFolder
  }

  let directories path =
    Directory.EnumerateDirectories path
    |> Seq.map(fun d -> entry(d, true))
    |> List.ofSeq

  let files (extensions: string list) path =
    Directory.EnumerateFiles path
    |> Seq.filter(fun f -> matchesExtensions(extensions, Path.GetFileName f))
    |> Seq.map(fun f -> entry(f, false))
    |> List.ofSeq

  let sorted entries = entries |> List.sortBy(fun e -> e.Name)

  let inline nameContains(query: string, name: string) =
    String.IsNullOrWhiteSpace query || name.Contains(query, comparison)

  {
    new IFileSystem with
      member _.List(path, extensions) =
        try
          let exts = List.map normalize extensions
          sorted(directories path) @ sorted(files exts path)
        with _ -> []

      member _.Search(path, query, extensions) =
        try
          let exts = List.map normalize extensions

          let matches e =
            nameContains(query, e.Name)
            && (e.IsFolder || matchesExtensions(exts, e.Name))

          let folders = directories path |> List.filter matches
          let found = files exts path |> List.filter matches
          sorted folders @ sorted found
        with _ -> []

      // Drive rows carry their name straight from DriveInfo: for a root path
      // like `C:\`, `Path.GetFileName` gives an empty string.
      member _.Drives() =
        try
          DriveInfo.GetDrives()
          |> Array.filter(fun d -> d.IsReady)
          |> Array.map(fun d -> {
            Name = d.Name
            Path = d.Name
            IsFolder = true
          })
          |> List.ofArray
          |> sorted
        with _ -> []

      member _.Parent path =
        match Path.GetDirectoryName path with
        | null -> ValueNone
        | parent -> ValueSome parent
  }
