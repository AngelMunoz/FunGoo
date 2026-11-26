module MetroGallery.Pages.MotionPage

open Goo
open FunGoo.Children
open FunGoo.Metro

/// The demo factories of the temporary motion page. A later batch replaces
/// this composition with the final single-demo rows.
type MotionPageCells = {
  Continuum: unit -> Cell
  Stagger: unit -> Cell
}

/// The temporary motion page: the page title, then the motion demos. The
/// demos keep their sizes, order, and data.
let create(cells: MotionPageCells) : Cell = {
  new Cell() with
    override _.Build() : Blob =
      let demos = MetroGallery.MotionTab.view(cells.Continuum, cells.Stagger)

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
        .Children(MetroText.title "motion", demos)
}
