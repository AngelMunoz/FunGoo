module MetroGallery.Pages.InputPage

open Goo
open FunGoo.Children
open FunGoo.Metro

/// The tab cells of the temporary input page, in design order: act, enter,
/// and output from the controls tab, enter from the inputs tab, and pick from
/// the date-time and selection tabs. A later batch replaces this composition
/// with the single input `MvuCell`.
type InputPageCells = {
  Controls: unit -> Cell
  Inputs: unit -> Cell
  DateTime: unit -> Cell
  Selection: unit -> Cell
}

/// One captioned section of the temporary page: a caption in the type ramp,
/// then the mounted tab cell.
let section(caption: string, key: string, factory: unit -> Cell) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.caption caption, Cell.Mount<Cell>(key, factory))

/// The temporary input page: the page title, then each tab cell in design
/// order. The controls keep their sizes, order, and demo data.
let create(cells: InputPageCells) : Cell = {
  new Cell() with
    override _.Build() : Blob =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        OverflowY = Overflow.Scroll,
        OverflowX = Overflow.Hidden,
        Gap = Length.Pixels 24.0,
        Padding =
          Edges(
            Length.Pixels 4.0,
            Length.Pixels 12.0,
            Length.Pixels 24.0,
            Length.Pixels 0.0
          )
      )
        .Children(
          MetroText.title "input",
          section("ACT · ENTER · OUTPUT", "input-controls", cells.Controls),
          section("ENTER", "input-inputs", cells.Inputs),
          section("PICK", "input-datetime", cells.DateTime),
          section("PICK", "input-selection", cells.Selection)
        )
}
