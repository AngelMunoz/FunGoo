namespace FunGoo

/// Length and edge helpers for the Goo style surface. Goo 0.6.6 replaced
/// <c>Length.Percent</c> and the per-side border-width properties with the
/// package-level <c>Percent</c> function and grouped <c>Edges</c> values.
module Style =

  /// A length as a percentage (0-100) of the available size.
  val inline percent: value: float -> Goo.Length

  /// Edge values with only the top edge set; the other edges stay unset.
  val inline topEdge: value: float -> Goo.Edges
