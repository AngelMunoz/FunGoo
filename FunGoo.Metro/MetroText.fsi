namespace FunGoo.Metro

open Goo

/// Semantic foreground tone for a <see cref="MetroText"/> element.
[<Struct>]
type MetroTextTone =
  /// Primary readable foreground (<c>Palette.Foreground</c>).
  | Primary
  /// Secondary muted foreground (<c>Palette.ForegroundSecondary</c>).
  | Secondary
  /// Disabled foreground (<c>Palette.ForegroundDisabled</c>).
  | Disabled
  /// Active theme accent foreground (<c>Accent.Base</c>).
  | Accent
  /// High-contrast text on an accent surface (<c>Accent.OnAccent</c>).
  | OnAccent
  /// Informational semantic foreground (<c>Palette.Info</c>).
  | Info
  /// Success semantic foreground (<c>Palette.Success</c>).
  | Success
  /// Warning semantic foreground (<c>Palette.Warning</c>).
  | Warning
  /// Danger/error semantic foreground (<c>Palette.Danger</c>).
  | Danger
  /// Explicit caller-supplied foreground color.
  | Custom of Color

/// Data-only properties for constructing a styled Metro <see cref="Text"/>
/// element.
[<Struct; NoComparison>]
type MetroTextProps = {
  /// Text content to render.
  Content: string
  /// Typographic role from the 10-step Metro scale.
  Role: MetroTextRole
  /// Semantic or custom foreground color tone.
  Tone: MetroTextTone
  /// Maximum rendered line count; <c>0</c> leaves line count unlimited.
  MaxLines: int
  /// Line wrapping policy.
  Wrap: TextWrap
  /// Overflow trimming policy.
  Trimming: TextTrimming
  /// Optional explicit accessibility heading level (<c>1..6</c>);
  /// <c>ValueNone</c> derives the heading level from <see cref="Role"/>.
  HeadingLevel: int voption
}

/// Metro typographic <see cref="Text"/> builders bound to the active
/// <see cref="MetroTheme"/>.
[<RequireQualifiedAccess>]
module MetroText =

  val internal resolveTone: theme: MetroTheme * tone: MetroTextTone -> Color

  val internal defaultHeadingLevel: role: MetroTextRole -> int voption

  /// Creates default <see cref="MetroTextProps"/> for
  /// <paramref name="content"/> and <paramref name="role"/> with primary tone,
  /// unlimited lines, wrapping enabled, and automatic heading semantics.
  val defaults: content: string * role: MetroTextRole -> MetroTextProps

  /// Builds a <see cref="Text"/> blob styled from the active
  /// <see cref="MetroTheme"/> and <paramref name="props"/>.
  val build: props: MetroTextProps -> Text

  /// Builds a 56 px light display headline (<see cref="MetroTextRole.Hero"/>).
  val inline hero: content: string -> Text

  /// Builds a 42 px light page title (<see cref="MetroTextRole.Title"/>).
  val inline title: content: string -> Text

  /// Builds a 28 px light section header (<see cref="MetroTextRole.Header"/>).
  val inline header: content: string -> Text

  /// Builds a 26 px light subsection header
  /// (<see cref="MetroTextRole.Subheader"/>).
  val inline subheader: content: string -> Text

  /// Builds a 20 px light subtitle (<see cref="MetroTextRole.Subtitle"/>).
  val inline subtitle: content: string -> Text

  /// Builds 14 px regular body copy (<see cref="MetroTextRole.Body"/>).
  val inline body: content: string -> Text

  /// Builds 14 px secondary muted body copy (<see cref="MetroTextRole.Body"/>
  /// with <see cref="MetroTextTone.Secondary"/>).
  val inline secondary: content: string -> Text

  /// Builds 12 px caption text (<see cref="MetroTextRole.Caption"/>).
  val inline caption: content: string -> Text

  /// Builds an 11 px bold uppercase badge label
  /// (<see cref="MetroTextRole.Badge"/>).
  val inline badge: content: string -> Text
