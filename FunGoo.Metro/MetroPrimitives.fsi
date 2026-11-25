namespace FunGoo.Metro

open Goo

/// Size ramp of a <see cref="MetroIcon"/>.
[<Struct>]
type MetroIconSize =
  /// 16 px glyph.
  | Small
  /// 20 px glyph.
  | Normal
  /// 28 px glyph.
  | Large
  /// 40 px glyph.
  | XLarge
  /// Custom glyph size in logical pixels.
  | Pixels of px: float

/// Data-only properties of a <see cref="MetroIcon"/>.
[<Struct; NoComparison>]
type MetroIconProps = {
  /// Material Symbols glyph name.
  Glyph: string
  /// Logical glyph size.
  Size: MetroIconSize
  /// Semantic foreground tone when <c>ColorOverride</c> is <c>ValueNone</c>.
  Tone: MetroTextTone
  /// Optional explicit icon color.
  ColorOverride: Color voption
}

/// Metro icon primitive backed by <c>Goo.Widgets.Icons.MaterialIcons</c>.
[<RequireQualifiedAccess>]
module MetroIcon =

  /// Resolves the logical pixel edge length of <paramref name="size"/>.
  val pixelSize: size: MetroIconSize -> float

  /// Returns default normal-sized primary icon properties.
  val defaults: glyph: string -> MetroIconProps

  /// Returns icon properties for <paramref name="glyph"/> at
  /// <paramref name="size"/>.
  val sized: glyph: string * size: MetroIconSize -> MetroIconProps

  /// Builds the icon inside a <c>Cell.Build</c> scope.
  val build: props: MetroIconProps -> Blob

  /// Builds a default 20 px primary icon for <paramref name="glyph"/>.
  val named: glyph: string -> Blob

/// Data-only properties of a sharp <see cref="MetroBorder"/> container.
[<Struct; NoComparison>]
type MetroBorderProps = {
  /// Per-edge border widths (defaults to <c>2 px</c> on all edges).
  Thickness: Edges
  /// Optional explicit border color; <c>ValueNone</c> uses
  /// <c>Palette.BorderStrong</c>.
  BorderColor: Color voption
  /// Optional explicit background fill; <c>ValueNone</c> uses transparent.
  Background: Color voption
  /// Inner padding around the child content.
  Padding: Edges
  /// Corner radius in logical pixels (defaults to <c>0.0</c> sharp Metro).
  /// Non-finite and negative values build as <c>0.0</c>.
  CornerRadius: float
}

/// Sharp <c>0 px</c> Metro border and surface frame.
[<RequireQualifiedAccess>]
module MetroBorder =

  /// Returns default <c>2 px</c> bordered container properties.
  val defaults: unit -> MetroBorderProps

  /// Returns subtle <c>1 px</c> card surface properties. <c>card</c> sets no
  /// background: the caller sets the background.
  val card: unit -> MetroBorderProps

  /// Wraps <paramref name="child"/> in a sharp Metro border container inside a
  /// <c>Cell.Build</c> scope.
  val build: props: MetroBorderProps * child: Blob -> Blob

/// Data-only properties of a collapsible <see cref="MetroExpander"/> section.
[<Struct; NoComparison>]
type MetroExpanderProps = {
  /// Header text displayed beside the disclosure chevron.
  Heading: string
  /// Controlled expanded state.
  Expanded: bool
  /// When <c>true</c>, disables header toggling.
  Disabled: bool
  /// Inner body padding in logical pixels (defaults to <c>16.0</c>).
  ContentPadding: float
  /// Optional accessible header name override.
  AccessibilityName: string voption
}

/// Sharp <c>0 px</c> Metro collapsible disclosure section wrapping
/// <c>Goo.Widgets.Layout.Disclosure</c>.
[<RequireQualifiedAccess>]
module MetroExpander =

  /// Returns default expander properties for <paramref name="heading"/> and
  /// <paramref name="expanded"/>.
  val defaults: heading: string * expanded: bool -> MetroExpanderProps

  /// Builds the collapsible expander inside a <c>Cell.Build</c> scope.
  val build:
    props: MetroExpanderProps * onExpandedChange: (bool -> unit) * content: Blob ->
      Blob

/// Data-only properties of a sharp <see cref="MetroImage"/>.
[<Struct; NoComparison>]
type MetroImageProps = {
  /// Optional bitmap image source provider.
  Source: ImageSourceProvider voption
  /// Aspect-ratio fit mode inside the image box.
  Fit: ImageFit
  /// Optional explicit width.
  Width: Length voption
  /// Optional explicit height.
  Height: Length voption
  /// When <c>true</c>, draws a sharp <c>1 px</c> Metro border around the frame.
  Bordered: bool
  /// Optional accessible description of the image.
  AccessibilityLabel: string voption
  /// Content rendered when <c>Source</c> is <c>ValueNone</c>, and passed to
  /// the image element as its loading content for a present source.
  Placeholder: Blob voption
  /// Content the image element shows when a present source provider yields
  /// no image. A load failure is only observable by the image element, so
  /// the Metro layer keeps no loading state: <c>Fallback</c> is wired to the
  /// element slot, and renders as the empty-source content when
  /// <c>Placeholder</c> is <c>ValueNone</c>.
  Fallback: Blob voption
}

/// Sharp <c>0 px</c> Metro image primitive with placeholder and fallback slots.
[<RequireQualifiedAccess>]
module MetroImage =

  /// Returns default image properties with <see cref="ImageFit.Cover"/>.
  val defaults: source: ImageSourceProvider voption -> MetroImageProps

  /// Builds the Metro image frame inside a <c>Cell.Build</c> scope.
  val build: props: MetroImageProps -> Blob

  /// Builds the Metro image frame with optional <paramref name="placeholder"/>
  /// and <paramref name="fallback"/> elements. Explicit arguments win over
  /// the matching <c>MetroImageProps</c> slots; when <c>Source</c> is
  /// <c>ValueNone</c>, the placeholder renders, else the fallback renders as
  /// the empty-source content.
  val buildWithFallback:
    props: MetroImageProps * placeholder: Blob voption * fallback: Blob voption ->
      Blob

/// Presence badge status of a <see cref="MetroPersonPicture"/>.
[<Struct>]
type MetroPresence =
  | NoPresence
  | Available
  | Away
  | Busy
  | Offline

/// Size preset of a <see cref="MetroPersonPicture"/>.
[<Struct>]
type MetroAvatarSize =
  /// 32 px square avatar.
  | Small
  /// 48 px square avatar.
  | Normal
  /// 64 px square avatar.
  | Large
  /// 96 px square avatar.
  | XLarge

/// Data-only properties of a sharp square <see cref="MetroPersonPicture"/>.
[<Struct; NoComparison>]
type MetroPersonPictureProps = {
  /// Full display name used for accessibility and automatic initials.
  DisplayName: string
  /// Optional explicit initials overriding automatic extraction.
  InitialsOverride: string voption
  /// Optional profile bitmap source.
  Source: ImageSourceProvider voption
  /// Avatar edge size preset.
  AvatarSize: MetroAvatarSize
  /// Optional corner presence badge.
  Presence: MetroPresence
  /// Optional background tile color override when displaying initials.
  AccentOverride: Color voption
}

/// Authentic square Metro avatar displaying a profile image or light-weight
/// uppercase initials with an optional presence corner badge. The font size of
/// the initials follows the avatar size and not <c>TextScaleFactor</c>,
/// because the avatar box has a fixed size.
[<RequireQualifiedAccess>]
module MetroPersonPicture =

  /// Extracts up to two uppercase initials from <paramref name="displayName"/>.
  val initialsOf: displayName: string -> string

  /// Resolves the logical pixel edge length of <paramref name="size"/>.
  val pixelSize: size: MetroAvatarSize -> float

  /// Returns default 48 px square avatar properties.
  val defaults: displayName: string -> MetroPersonPictureProps

  /// Builds the avatar inside a <c>Cell.Build</c> scope.
  val build: props: MetroPersonPictureProps -> Blob

/// Data-only inline styled text run in a <see cref="MetroRichTextBlock"/>.
[<Struct; NoComparison>]
type MetroRichSpan = {
  /// Displayed span text.
  RunText: string
  /// Renders with bold weight (<c>700</c>).
  Bold: bool
  /// Renders with italic slant.
  Italic: bool
  /// Renders with underline decoration.
  Underline: bool
  /// Renders in the theme monospace font family.
  Monospace: bool
  /// Colors the span with the active Metro accent color.
  Accent: bool
  /// Colors the span with the secondary muted foreground color.
  Secondary: bool
}

/// Data-only properties of a <see cref="MetroRichTextBlock"/>.
[<Struct; NoComparison>]
type MetroRichTextProps = {
  /// Ordered inline runs concatenated into one <see cref="Text"/> element.
  Spans: MetroRichSpan array
  /// Base Metro typographic role.
  Role: MetroTextRole
  /// Enables pointer text selection and copy when <c>true</c>.
  Selectable: bool
  /// Optional maximum line count before clamping.
  MaxLines: int voption
  /// Trims overflowing text with an ellipsis when <c>true</c>.
  TrimEllipsis: bool
}

/// Rich multi-span Metro text block built on <c>Goo.Text(TextSpan[])</c>.
[<RequireQualifiedAccess>]
module MetroRichTextBlock =

  /// Creates an unstyled plain text span.
  val plain: text: string -> MetroRichSpan

  /// Creates a bold text span.
  val bold: text: string -> MetroRichSpan

  /// Creates an italic text span.
  val italic: text: string -> MetroRichSpan

  /// Creates an underlined accent-colored text span.
  val accent: text: string -> MetroRichSpan

  /// Creates a monospace inline-code text span.
  val code: text: string -> MetroRichSpan

  /// Returns default <see cref="MetroTextRole.Body"/> rich-text properties.
  val defaults: spans: MetroRichSpan array -> MetroRichTextProps

  /// Builds the rich text block inside a <c>Cell.Build</c> scope.
  val build: props: MetroRichTextProps -> Blob
