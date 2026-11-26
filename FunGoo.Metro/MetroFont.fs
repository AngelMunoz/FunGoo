namespace FunGoo.Metro

open System
open System.Reflection
open Goo

[<RequireQualifiedAccess>]
module MetroFont =

  let family = "Selawik"
  let lightWeight = 300
  let regularWeight = 400
  let boldWeight = 700

  let private resources: (string * int) array = [|
    "FunGoo.Metro.Fonts.selawkl.ttf", lightWeight
    "FunGoo.Metro.Fonts.selawk.ttf", regularWeight
    "FunGoo.Metro.Fonts.selawkb.ttf", boldWeight
  |]

  let private gate = obj()
  let mutable private ready = false
  let mutable private held: FontSource array = Array.empty

  let private tryLoadBytes(name: string) : byte array voption =
    let assembly = typeof<MetroTextRole>.Assembly
    use stream = assembly.GetManifestResourceStream(name)

    if isNull stream then
      ValueNone
    else
      let bytes = Array.zeroCreate<byte>(int stream.Length)
      let mutable offset = 0

      while offset < bytes.Length do
        let read = stream.Read(bytes, offset, bytes.Length - offset)

        if read = 0 then
          offset <- bytes.Length
        else
          offset <- offset + read

      ValueSome bytes

  let ensureRegistered() : bool =
    lock gate (fun () ->
      if ready then
        true
      else
        let loaded =
          resources
          |> Array.choose(fun (name, weight) ->
            tryLoadBytes name
            |> ValueOption.map(fun bytes -> bytes, weight)
            |> ValueOption.toOption)

        if loaded.Length <> resources.Length then
          false
        else
          // Registers each source in order. When `Register` throws, every
          // source made in this attempt is disposed (`Dispose` unregisters),
          // so no half-registered set leaks and a later call retries cleanly.
          let created = ResizeArray<FontSource>(resources.Length)

          try
            for bytes, weight in loaded do
              let source = new FontSource(family, weight, false, bytes)
              created.Add(source)
              source.Register()

            held <- created.ToArray()
            ready <- true
            true
          with _ ->
            for source in created do
              try
                source.Dispose()
              with _ ->
                ()

            held <- Array.empty
            ready <- false
            false)
