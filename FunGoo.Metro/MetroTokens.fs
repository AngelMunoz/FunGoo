namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo

[<Struct; NoComparison>]
type MetroPalette = {
  Background: Color
  BackgroundAlt: Color
  Surface: Color
  Foreground: Color
  ForegroundSecondary: Color
  ForegroundDisabled: Color
  Border: Color
  BorderStrong: Color
  Highlight: Color
  Hover: Color
  Active: Color
  Info: Color
  Success: Color
  Warning: Color
  Danger: Color
  DangerFill: Color
  DangerHover: Color
}

[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroPalette =

  let dark: MetroPalette = {
    Background = Color.Parse "#000000"
    BackgroundAlt = Color.Parse "#1a1a1a"
    Surface = Color.Rgba(255, 255, 255, 13)
    Foreground = Color.Parse "#ffffff"
    ForegroundSecondary = Color.Rgba(255, 255, 255, 153)
    ForegroundDisabled = Color.Rgba(255, 255, 255, 102)
    Border = Color.Rgba(255, 255, 255, 51)
    BorderStrong = Color.Rgba(255, 255, 255, 140)
    Highlight = Color.Rgba(255, 255, 255, 26)
    Hover = Color.Rgba(255, 255, 255, 38)
    Active = Color.Rgba(255, 255, 255, 20)
    Info = Color.Parse "#429ce3"
    Success = Color.Parse "#4cd16d"
    Warning = Color.Parse "#ffc04a"
    Danger = Color.Parse "#ff6b63"
    DangerFill = Color.Parse "#e51400"
    DangerHover = Color.Parse "#ff4438"
  }

  let light: MetroPalette = {
    Background = Color.Parse "#ffffff"
    BackgroundAlt = Color.Parse "#f3f3f3"
    Surface = Color.Rgba(0, 0, 0, 13)
    Foreground = Color.Parse "#1f1f1f"
    ForegroundSecondary = Color.Rgba(0, 0, 0, 179)
    ForegroundDisabled = Color.Rgba(0, 0, 0, 102)
    Border = Color.Rgba(0, 0, 0, 51)
    BorderStrong = Color.Rgba(0, 0, 0, 140)
    Highlight = Color.Rgba(0, 0, 0, 26)
    Hover = Color.Rgba(0, 0, 0, 20)
    Active = Color.Rgba(0, 0, 0, 38)
    Info = Color.Parse "#005a9e"
    Success = Color.Parse "#007200"
    Warning = Color.Parse "#995500"
    Danger = Color.Parse "#b40000"
    DangerFill = Color.Parse "#e51400"
    DangerHover = Color.Parse "#b40000"
  }

[<Struct>]
type MetroTextRole =
  | Hero
  | Title
  | Header
  | Subheader
  | Header2
  | Subtitle
  | Medium
  | Body
  | Caption
  | Badge

[<Struct>]
type MetroTypeSpec = {
  FontSize: float
  FontWeight: int
  LetterSpacing: float
  LineHeight: float
  Uppercase: bool
}

[<RequireQualifiedAccess>]
module MetroTypography =

  let heroSpec = {
    FontSize = 56.0
    FontWeight = 300
    LetterSpacing = -0.02 * 56.0
    LineHeight = 1.1
    Uppercase = false
  }

  let titleSpec = {
    FontSize = 42.0
    FontWeight = 300
    LetterSpacing = -0.02 * 42.0
    LineHeight = 1.1
    Uppercase = false
  }

  let headerSpec = {
    FontSize = 28.0
    FontWeight = 300
    LetterSpacing = 0.02 * 28.0
    LineHeight = 1.2
    Uppercase = false
  }

  let subheaderSpec = {
    FontSize = 26.0
    FontWeight = 300
    LetterSpacing = 0.02 * 26.0
    LineHeight = 1.2
    Uppercase = false
  }

  let header2Spec = {
    FontSize = 26.0
    FontWeight = 400
    LetterSpacing = 0.0
    LineHeight = 1.2
    Uppercase = false
  }

  let subtitleSpec = {
    FontSize = 20.0
    FontWeight = 300
    LetterSpacing = 0.02 * 20.0
    LineHeight = 1.2
    Uppercase = false
  }

  let mediumSpec = {
    FontSize = 16.0
    FontWeight = 400
    LetterSpacing = 0.02 * 16.0
    LineHeight = 1.3
    Uppercase = false
  }

  let bodySpec = {
    FontSize = 14.0
    FontWeight = 400
    LetterSpacing = 0.02 * 14.0
    LineHeight = 1.4
    Uppercase = false
  }

  let captionSpec = {
    FontSize = 12.0
    FontWeight = 400
    LetterSpacing = 0.02 * 12.0
    LineHeight = 1.3
    Uppercase = false
  }

  let badgeSpec = {
    FontSize = 11.0
    FontWeight = 700
    LetterSpacing = 0.05 * 11.0
    LineHeight = 1.2
    Uppercase = true
  }

  let spec(role: MetroTextRole) : MetroTypeSpec =
    match role with
    | MetroTextRole.Hero -> heroSpec
    | MetroTextRole.Title -> titleSpec
    | MetroTextRole.Header -> headerSpec
    | MetroTextRole.Subheader -> subheaderSpec
    | MetroTextRole.Header2 -> header2Spec
    | MetroTextRole.Subtitle -> subtitleSpec
    | MetroTextRole.Medium -> mediumSpec
    | MetroTextRole.Body -> bodySpec
    | MetroTextRole.Caption -> captionSpec
    | MetroTextRole.Badge -> badgeSpec

[<RequireQualifiedAccess>]
module MetroSpacing =
  let unit = 4.0
  let xs = 4.0
  let sm = 8.0
  let md = 12.0
  let lg = 16.0
  let xl = 24.0
  let xxl = 40.0

[<RequireQualifiedAccess>]
module MetroTiming =
  let fastMs = 167.0
  let normalMs = 250.0
  let slowMs = 333.0
  let continuumMs = 150.0
  let turnstileInMs = 500.0
  let turnstileOutMs = 400.0

  let staggerDelaysMs: IReadOnlyList<float> =
    Array.AsReadOnly [|
      0.0
      50.0
      90.0
      125.0
      155.0
      180.0
      202.0
      222.0
      240.0
      256.0
    |]
