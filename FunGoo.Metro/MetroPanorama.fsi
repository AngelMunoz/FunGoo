namespace FunGoo.Metro

open Goo

/// Data-only description of one section in a <see cref="MetroPanoramaCell"/>.
[<Struct; NoComparison>]
type MetroPanoramaSection = {
  /// Stable section identifier passed to the section builder.
  Id: string
  /// Section header displayed in Metro <c>Header</c> typography.
  Header: string
  /// Section width in logical pixels (defaults to <c>360.0</c>).
  Width: float
}

/// Data-only input of <see cref="MetroPanoramaCell"/>.
[<Struct; NoComparison>]
type MetroPanoramaInput = {
  /// Hero display title moving at <c>TitleParallax</c> speed.
  Title: string
  /// Ordered horizontal sections.
  Sections: MetroPanoramaSection array
  /// Parallax ratio of the optional background layer (defaults to <c>0.15</c>).
  BackgroundParallax: float
  /// Parallax ratio of the hero title layer (defaults to <c>0.35</c>).
  TitleParallax: float
  /// Horizontal gap between sections in logical pixels (defaults to <c>28.0</c>).
  SectionGap: float
  /// Optional accessible name override.
  AccessibilityName: string voption
}

/// Wide horizontal multi-layer parallax canvas inspired by the Windows Phone
/// Panorama control. Scroll updates paint-bound <see cref="Anim{Point}"/>
/// values through <c>ElementHandle.MetricsChanged</c> with zero cell rebuilds.
[<AbstractClass>]
type MetroPanoramaCell =
  inherit Cell<MetroPanoramaInput>

  internal new: unit -> MetroPanoramaCell

  /// Handle attached to the horizontal scroll viewport.
  abstract ScrollHandle: ElementHandle

  /// Paint-bound translation of the background layer.
  abstract BackgroundTranslate: Anim<Point>

  /// Paint-bound translation of the hero title layer.
  abstract TitleTranslate: Anim<Point>

/// Metro multi-layer parallax panorama builders and retained cell factories.
[<RequireQualifiedAccess>]
module MetroPanorama =

  /// Creates a panorama section with default width (<c>360.0</c> px).
  val section: id: string * header: string -> MetroPanoramaSection

  /// Creates a panorama section with explicit <paramref name="width"/>.
  val sizedSection:
    id: string * header: string * width: float -> MetroPanoramaSection

  /// Returns default panorama input with <c>0.15</c> background parallax and
  /// <c>0.35</c> title parallax.
  val defaults:
    title: string * sections: MetroPanoramaSection array -> MetroPanoramaInput

  /// Creates a <see cref="MetroPanoramaCell"/> without a custom background
  /// layer. The cell calls <paramref name="renderSection"/> inside its own
  /// <c>Build</c>. A Blob callback that <paramref name="renderSection"/>
  /// creates belongs to this cell, so the owner rule rebuilds this cell and
  /// not the host. When such a callback changes the state of the host, it must
  /// also rebuild the host.
  val create: renderSection: (string -> Blob) -> MetroPanoramaCell

  /// Creates a <see cref="MetroPanoramaCell"/> with a custom parallax
  /// <paramref name="renderBackground"/> layer behind the title and sections.
  /// The cell calls <paramref name="renderBackground"/> and
  /// <paramref name="renderSection"/> inside its own <c>Build</c>. A Blob
  /// callback that either function creates belongs to this cell, so the owner
  /// rule rebuilds this cell and not the host. When such a callback changes
  /// the state of the host, it must also rebuild the host.
  val createWithBackground:
    renderBackground: (unit -> Blob) * renderSection: (string -> Blob) ->
      MetroPanoramaCell

  /// Creates a reusable mount factory for <see cref="MetroPanoramaCell"/>.
  /// The cell calls <paramref name="renderSection"/> inside its own
  /// <c>Build</c>. A Blob callback that <paramref name="renderSection"/>
  /// creates belongs to this cell, so the owner rule rebuilds this cell and
  /// not the host. When such a callback changes the state of the host, it must
  /// also rebuild the host.
  val createFactory:
    renderSection: (string -> Blob) -> (unit -> MetroPanoramaCell)

  /// Mounts a <see cref="MetroPanoramaCell"/> under <paramref name="key"/>.
  val mount:
    key: string *
    input: MetroPanoramaInput *
    factory: (unit -> MetroPanoramaCell) ->
      Blob
