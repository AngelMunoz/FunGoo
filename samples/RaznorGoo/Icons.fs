module GooRes.Icons

open System
open Goo
open Goo.Svg
open Goo.Widgets.Icons

let fill = "#DCE2F0"

let document(pathData: string) : string =
  $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path fill=\"{fill}\" d=\"{pathData}\"/></svg>"

let private tint = Nullable(Color.Parse fill)

let inline private symbol(name: string) : Blob =
  MaterialIcons.Create(name, 24.0, tint) :> Blob

let play = symbol "play_arrow"

let pause = symbol "pause"

let previous = symbol "skip_previous"

let next = symbol "skip_next"

let shuffle = symbol "shuffle"

let repeat = symbol "repeat"

let repeatOne = symbol "repeat_one"

// Material Symbols ships no "repeat off" glyph, so the off state keeps its
// own parsed SVG.
let repeatOff: Blob =
  Svg
    .Parse(
      document
        "M2,5.27L3.28,4L20,20.72L18.73,22L15.73,19H7V22L3,18L7,14V17H13.73L7,10.27V11H5V8.27L2,5.27M17,13H19V17.18L17,15.18V13M17,5V2L21,6L17,10V7H8.82L6.82,5H17Z"
    )
    .Render()

let folder = symbol "folder"
