module MetroGallery.Pages.CollectionsPage

open Goo
open FunGoo.Children
open FunGoo.Metro

/// The tab cells of the temporary collections page. The tree view moved here
/// from the selection tab when its picker half joined the input page. A later
/// batch replaces this composition with the final grouped sections.
type CollectionsPageCells = {
  Lists: unit -> Cell
  FlipView: unit -> Cell
  Zoom: unit -> Cell
  Selection: unit -> Cell
}

/// One captioned section of the temporary page: a caption in the type ramp,
/// then the mounted tab cell.
let section(caption: string, key: string, factory: unit -> Cell) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.caption caption, Cell.Mount<Cell>(key, factory))

/// The temporary collections page: the page title, then each tab cell. The
/// controls keep their sizes, order, and demo data.
let create(cells: CollectionsPageCells) : Cell = {
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
          MetroText.title "collections",
          section("LISTS", "collections-lists", cells.Lists),
          section("FLIP VIEW", "collections-flip-view", cells.FlipView),
          section("TREE VIEW", "collections-tree", cells.Selection),
          section("SEMANTIC ZOOM", "collections-zoom", cells.Zoom)
        )
}
