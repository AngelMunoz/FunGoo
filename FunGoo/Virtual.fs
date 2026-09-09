namespace FunGoo

module Virtual =

  open System.Collections.Generic
  open Goo

  let inline create
    (items: IReadOnlyList<'T>)
    (itemWidth: float)
    (itemHeight: float)
    (itemKey: 'T -> string)
    (itemBuilder: 'T -> Blob)
    : Blob =
    Goo.``<Program>``.Virtual<'T>(
      items,
      itemWidth,
      itemHeight,
      itemKey,
      itemBuilder
    )

  let inline ofSeq
    (items: seq<'T>)
    (itemWidth: float)
    (itemHeight: float)
    (itemKey: 'T -> string)
    (itemBuilder: 'T -> Blob)
    : Blob =
    let snapshot = ResizeArray items
    create snapshot itemWidth itemHeight itemKey itemBuilder

  let ofIndexed
    (count: int)
    (item: int -> 'T)
    (itemWidth: float)
    (itemHeight: float)
    (itemKey: 'T -> string)
    (itemBuilder: 'T -> Blob)
    : Blob =
    let view = {
      new IReadOnlyList<'T> with
        member _.Count = count

        member _.Item
          with get index = item index

        member _.GetEnumerator() : IEnumerator<'T> =
          (Seq.init count item).GetEnumerator()

        member _.GetEnumerator() : System.Collections.IEnumerator =
          (Seq.init count item).GetEnumerator()
          :> System.Collections.IEnumerator
    }

    create view itemWidth itemHeight itemKey itemBuilder
