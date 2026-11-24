namespace FunGoo.Metro

open System
open Goo

/// Descriptor for a horizontally arranged section inside a
/// <see cref="MetroHubCell"/>.
[<Struct; NoComparison>]
type MetroHubSection = {
  /// Stable unique identifier for the section.
  Id: string
  /// Display header rendered at the top of the section column.
  Header: string
  /// Explicit section width in logical pixels so adjacent sections peek in
  /// from the right edge.
  Width: float
}

/// Data-only input snapshot for <see cref="MetroHubCell"/>.
[<Struct; NoComparison>]
type MetroHubInput = {
  /// Large parallax title displayed above the horizontal section viewport.
  Title: string
  /// Ordered horizontal hub sections.
  Sections: MetroHubSection array
  /// Multiplier applied to horizontal scroll offset for header parallax motion
  /// (defaults to <c>0.35</c>).
  ParallaxFactor: float
  /// Horizontal gap between adjacent sections in logical pixels (defaults to
  /// <c>32.0</c>).
  SectionGap: float
  /// Optional accessible name for the hub region.
  AccessibilityName: string voption
}

/// Stateful Metro Hub (Panorama) layout cell with a paint-bound parallax
/// header and horizontally scrolling peeking sections. <c>MetroHub.create</c>
/// makes the cell. The type holds the surface that hosts and tests use.
[<AbstractClass>]
type MetroHubCell =
  inherit Cell<MetroHubInput>

  /// Element handle attached to the horizontal scrolling viewport.
  abstract ScrollHandle: ElementHandle

  /// Paint-bound translation animation driving header parallax without cell
  /// rebuilds during scrolling.
  abstract HeaderTranslate: Anim<Point>

/// Construction and mounting helpers for <see cref="MetroHubCell"/>.
[<RequireQualifiedAccess>]
module MetroHub =

  /// Creates default <see cref="MetroHubInput"/> for <paramref name="title"/>
  /// and <paramref name="sections"/> with <c>0.35</c> parallax factor and
  /// <c>32.0</c> px section gap.
  val defaults: title: string * sections: MetroHubSection array -> MetroHubInput

  /// Creates a hub cell that renders each section body through
  /// <paramref name="renderSection"/>.
  /// The cell calls <paramref name="renderSection"/> inside its own
  /// <c>Build</c>. A callback that <paramref name="renderSection"/> creates
  /// belongs to this cell, so the owner rule rebuilds this cell and not the
  /// caller. Such a callback must rebuild the caller cell when it changes the
  /// state of the caller cell.
  val create: renderSection: (string -> Blob) -> MetroHubCell

  /// Creates a reusable cell factory for <see cref="MetroHubCell"/>. Cache the
  /// returned factory outside <c>Build</c>.
  val createFactory: renderSection: (string -> Blob) -> unit -> MetroHubCell

  /// Mounts a <see cref="MetroHubCell"/> under stable <paramref name="key"/>
  /// using the cached <paramref name="factory"/>.
  val mount:
    key: string * input: MetroHubInput * factory: (unit -> MetroHubCell) -> Blob
