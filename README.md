<p align="center">
  <img src="fungoo.jpeg" alt="FunGoo" width="720">
</p>

# FunGoo

F# helpers and bindings for [Goo](https://github.com/obselate/goo), a retained desktop UI framework rendered with Vulkan, and for [Goo.Widgets](https://github.com/obselate/goo-widgets), its composable widget library.

Goo is authored in G#. FunGoo is a thin layer of ease-ups for consuming it from F#, without hiding the framework:

- **Children helpers** — append blobs to `Container` and `Button` in one call, from a sequence or inline.
- **Fluent setters** — chain configuration on the mutable Goo surface (`Window`, `TextEditorController`, `ShaderEffect`).
- **Interop helpers** — `voption` wrappers for `Try*` out-parameter methods, and inline conversions for `Length` and `Color`.
- **Virtual lists** — F# bindings for Goo's virtualized collections over an `IReadOnlyList`, a sequence, or a count and indexer.
- **Widgets** — generated bindings for Goo.Widgets in a separate `FunGoo.Widgets` package: set plain widget properties through constructor assignment, and nullable properties through optional-value setters.

## Getting started

Install the packages:

```sh
dotnet add package Goo
dotnet add package FunGoo
# optional: widget bindings
dotnet add package Goo.Widgets
dotnet add package FunGoo.Widgets
```

Describe your UI with plain F# values:

```fsharp
open Goo
open FunGoo.Children

let view =
  Container(Width = Length.Percent 100, Padding = 24, Gap = 12)
    .Children(
      Text(Content = "Hello from F#", FontSize = 24),
      Button(OnClick = fun _ -> printfn "clicked")
        .Children(Text(Content = "Click me"))
    )
```

Compose Goo.Widgets the same way. Constructor assignment covers plain properties and callbacks, and the generated optional-value setters take nullable properties: call them with a value to set it, or with no argument to reset it to the widget default.

```fsharp
open Goo
open Goo.Widgets.Actions
open FunGoo.Widgets

let reset =
  ActionButton(
    Label = "Reset",
    Height = 42.0,
    OnClick = fun () -> printfn "reset"
  )
    .backgroundColor(Color.Parse("#22c55e"))
    .Build()
```

See [samples/RaznorGoo](samples/RaznorGoo) for a complete media player: Goo widgets, adaptive state, LibVLCSharp audio, Material icons, and a custom file picker.

## Building

Requires the .NET 10 SDK.

```sh
dotnet tool restore
dotnet build
dotnet run --project samples/RaznorGoo
```

## License

MIT — see [LICENSE](LICENSE).
