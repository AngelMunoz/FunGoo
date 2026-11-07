namespace FunGoo

/// Bindings for Goo.Widgets virtualized collections. Goo removed its
/// package-level <c>Virtual</c> function in 0.7.22; Goo.Widgets supplies the
/// same fixed-extent collection as <c>VirtualItems</c>.
module Virtual =

  /// Builds a virtual collection from an indexable list. All items share one
  /// size; the element follows its flex direction and wrap.
  val inline create:
    items: System.Collections.Generic.IReadOnlyList<'T> ->
    itemWidth: float ->
    itemHeight: float ->
    itemKey: ('T -> string) ->
    itemBuilder: ('T -> Goo.Blob) ->
      Goo.Blob

  /// Builds a virtual collection from any sequence. The sequence is copied
  /// into a list first; rebuild the owning cell after the source changes.
  val inline ofSeq:
    items: seq<'T> ->
    itemWidth: float ->
    itemHeight: float ->
    itemKey: ('T -> string) ->
    itemBuilder: ('T -> Goo.Blob) ->
      Goo.Blob

  /// Builds a virtual collection from a count and an indexer, without
  /// materializing the items.
  val ofIndexed:
    count: int ->
    item: (int -> 'T) ->
    itemWidth: float ->
    itemHeight: float ->
    itemKey: ('T -> string) ->
    itemBuilder: ('T -> Goo.Blob) ->
      Goo.Blob
