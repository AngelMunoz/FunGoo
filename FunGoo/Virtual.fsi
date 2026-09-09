namespace FunGoo

module Virtual =

  open System.Collections.Generic
  open Goo

  /// items must be a stable instance between builds; item keys must be non-empty and unique.
  val inline create:
    items: IReadOnlyList<'T> ->
    itemWidth: float ->
    itemHeight: float ->
    itemKey: ('T -> string) ->
    itemBuilder: ('T -> Blob) ->
      Blob

  /// Materializes items once into a ResizeArray, then delegates to create.
  val inline ofSeq:
    items: seq<'T> ->
    itemWidth: float ->
    itemHeight: float ->
    itemKey: ('T -> string) ->
    itemBuilder: ('T -> Blob) ->
      Blob

  /// Wraps count and an indexer in an IReadOnlyList, then delegates to create.
  val ofIndexed:
    count: int ->
    item: (int -> 'T) ->
    itemWidth: float ->
    itemHeight: float ->
    itemKey: ('T -> string) ->
    itemBuilder: ('T -> Blob) ->
      Blob
