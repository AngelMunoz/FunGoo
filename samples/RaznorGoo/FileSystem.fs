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

  let inline matchesExtensions(extensions: string seq, name: string) =
    Seq.isEmpty extensions
    || Seq.exists (fun ext -> name.EndsWith(ext, comparison)) extensions

  let inline entry(path: string, isFolder: bool) = {
    Name = Path.GetFileName path
    Path = path
    IsFolder = isFolder
  }

  let directories path =
    Directory.EnumerateDirectories path
    |> Seq.map(fun d -> entry(d, true))
    |> Array.ofSeq

  let files (extensions: string seq) path =
    Directory.EnumerateFiles path
    |> Seq.filter(fun f -> matchesExtensions(extensions, Path.GetFileName f))
    |> Seq.map(fun f -> entry(f, false))
    |> Array.ofSeq

  let sorted entries =
    entries |> Array.sortBy(fun e -> e.Name)

  let inline nameContains(query: string, name: string) =
    String.IsNullOrWhiteSpace query || name.Contains(query, comparison)

  {
    new IFileSystem with
      member _.List(path, extensions) =
        try
          let exts = Seq.map normalize extensions
          [| yield! sorted(directories path); yield! sorted(files exts path) |]
        with _ ->
          Array.empty

      member _.Search(path, query, extensions) =
        try
          let exts = Seq.map normalize extensions

          let matches e =
            nameContains(query, e.Name)
            && (e.IsFolder || matchesExtensions(exts, e.Name))

          let folders = directories path |> Array.filter matches
          let found = files exts path |> Array.filter matches
          [| yield! sorted folders; yield! sorted found |]
        with _ ->
          Array.empty

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
          |> sorted
        with _ ->
          Array.empty

      member _.Parent path =
        match Path.GetDirectoryName path with
        | null -> ValueNone
        | parent -> ValueSome parent
  }
