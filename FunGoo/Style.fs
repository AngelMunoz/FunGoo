namespace FunGoo

module Style =

  open Goo
  open FunGoo.Interop

  // Goo 0.8.0 restored named Length factories for CLR callers.
  let inline percent(value: float) : Length = Length.Percent(value)

  // The four-argument constructor marks every edge as set; an unset Length
  // clears a side, so a default Length keeps right, bottom, and left empty.
  let inline topEdge(value: float) : Edges =
    Edges(
      lengthFromFloat value,
      Unchecked.defaultof<Length>,
      Unchecked.defaultof<Length>,
      Unchecked.defaultof<Length>
    )
