namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo

[<Struct; NoComparison>]
type MetroAccent = {
  Base: Color
  Light: Color
  Dark: Color
  OnAccent: Color
}

[<Struct; NoComparison>]
type MetroAccentKind =
  | Blue
  | Red
  | Orange
  | Green
  | Teal
  | Purple
  | Magenta
  | Lime
  | Brown
  | Pink
  | Mango
  | Cobalt
  | Indigo
  | Violet
  | Crimson
  | Emerald
  | Mauve
  | Sienna
  | Olive
  | Steel
  | Taupe
  | Custom of Color
  | System

[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroAccent =

  let inline linearSrgb(channel: float32) : float =
    let c = float channel

    if c <= 0.04045 then
      c / 12.92
    else
      Math.Pow((c + 0.055) / 1.055, 2.4)

  let inline relativeLuminance(color: Color) : float =
    0.2126 * linearSrgb color.R
    + 0.7152 * linearSrgb color.G
    + 0.0722 * linearSrgb color.B

  let inline contrastRatio(first: Color, second: Color) : float =
    let l1 = relativeLuminance first + 0.05
    let l2 = relativeLuminance second + 0.05
    if l1 >= l2 then l1 / l2 else l2 / l1

  let inline onAccentFor(color: Color) : Color =
    if
      contrastRatio(Color.White, color) >= contrastRatio(Color.Black, color)
    then
      Color.White
    else
      Color.Black

  let inline blend(source: Color, target: Color, amount: float32) : Color =
    Color.FromNormalized(
      source.R + (target.R - source.R) * amount,
      source.G + (target.G - source.G) * amount,
      source.B + (target.B - source.B) * amount,
      source.A
    )

  let makeNamed
    (baseHex: string, lightHex: string, darkHex: string)
    : MetroAccent =
    let baseColor = Color.Parse baseHex

    {
      Base = baseColor
      Light = Color.Parse lightHex
      Dark = Color.Parse darkHex
      OnAccent = onAccentFor baseColor
    }

  let blue = makeNamed("#0078d4", "#429ce3", "#005a9e")
  let red = makeNamed("#e51400", "#ff4438", "#b40000")
  let orange = makeNamed("#fa6800", "#ff9a3c", "#c64900")
  let green = makeNamed("#00a300", "#00d500", "#007200")
  let teal = makeNamed("#00aba9", "#00ddda", "#007a78")
  let purple = makeNamed("#a200ff", "#d04aff", "#7200ca")
  let magenta = makeNamed("#d80073", "#ff2d9b", "#a60052")
  let lime = makeNamed("#a4c400", "#d5f200", "#749200")
  let brown = makeNamed("#825a2c", "#b1834f", "#533a1c")
  let pink = makeNamed("#f472d6", "#fba0eb", "#cf4ab0")
  let mango = makeNamed("#f09609", "#ffc04a", "#be7200")
  let cobalt = makeNamed("#0050ef", "#4a8aff", "#0035a8")
  let indigo = makeNamed("#6a00ff", "#9c4aff", "#4a00c8")
  let violet = makeNamed("#aa00ff", "#c94aff", "#8200c8")
  let crimson = makeNamed("#a20025", "#d44a4a", "#720018")
  let emerald = makeNamed("#008a00", "#00c400", "#005a00")
  let mauve = makeNamed("#765f89", "#a090b0", "#543f65")
  let sienna = makeNamed("#a0522d", "#c8805c", "#783a1f")
  let olive = makeNamed("#6d8764", "#96b08a", "#4d6344")
  let steel = makeNamed("#647687", "#8a9dad", "#455a6b")
  let taupe = makeNamed("#87794e", "#b0a070", "#5f5530")

  let fromColor(color: Color) : MetroAccent = {
    Base = color
    Light = blend(color, Color.White, 0.22f)
    Dark = blend(color, Color.Black, 0.22f)
    OnAccent = onAccentFor color
  }

  let resolve
    (kind: MetroAccentKind, systemAccent: Color voption)
    : MetroAccent =
    match kind with
    | MetroAccentKind.Blue -> blue
    | MetroAccentKind.Red -> red
    | MetroAccentKind.Orange -> orange
    | MetroAccentKind.Green -> green
    | MetroAccentKind.Teal -> teal
    | MetroAccentKind.Purple -> purple
    | MetroAccentKind.Magenta -> magenta
    | MetroAccentKind.Lime -> lime
    | MetroAccentKind.Brown -> brown
    | MetroAccentKind.Pink -> pink
    | MetroAccentKind.Mango -> mango
    | MetroAccentKind.Cobalt -> cobalt
    | MetroAccentKind.Indigo -> indigo
    | MetroAccentKind.Violet -> violet
    | MetroAccentKind.Crimson -> crimson
    | MetroAccentKind.Emerald -> emerald
    | MetroAccentKind.Mauve -> mauve
    | MetroAccentKind.Sienna -> sienna
    | MetroAccentKind.Olive -> olive
    | MetroAccentKind.Steel -> steel
    | MetroAccentKind.Taupe -> taupe
    | MetroAccentKind.Custom color -> fromColor color
    | MetroAccentKind.System ->
      systemAccent |> ValueOption.map fromColor |> ValueOption.defaultValue blue

  let named: IReadOnlyList<MetroAccentKind * string * MetroAccent> =
    Array.AsReadOnly [|
      MetroAccentKind.Blue, "Blue", blue
      MetroAccentKind.Red, "Red", red
      MetroAccentKind.Orange, "Orange", orange
      MetroAccentKind.Green, "Green", green
      MetroAccentKind.Teal, "Teal", teal
      MetroAccentKind.Purple, "Purple", purple
      MetroAccentKind.Magenta, "Magenta", magenta
      MetroAccentKind.Lime, "Lime", lime
      MetroAccentKind.Brown, "Brown", brown
      MetroAccentKind.Pink, "Pink", pink
      MetroAccentKind.Mango, "Mango", mango
      MetroAccentKind.Cobalt, "Cobalt", cobalt
      MetroAccentKind.Indigo, "Indigo", indigo
      MetroAccentKind.Violet, "Violet", violet
      MetroAccentKind.Crimson, "Crimson", crimson
      MetroAccentKind.Emerald, "Emerald", emerald
      MetroAccentKind.Mauve, "Mauve", mauve
      MetroAccentKind.Sienna, "Sienna", sienna
      MetroAccentKind.Olive, "Olive", olive
      MetroAccentKind.Steel, "Steel", steel
      MetroAccentKind.Taupe, "Taupe", taupe
    |]
