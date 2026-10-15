namespace FunGoo

module Style =

  open Goo
  open FunGoo.Interop

  let inline percent(value: float) : Length = ``<Program>``.Percent(value)

  // The four-argument constructor marks every edge as set; an unset Length
  // clears a side, so a default Length keeps right, bottom, and left empty.
  let inline topEdge(value: float) : Edges =
    Edges(
      lengthFromFloat value,
      Unchecked.defaultof<Length>,
      Unchecked.defaultof<Length>,
      Unchecked.defaultof<Length>
    )
