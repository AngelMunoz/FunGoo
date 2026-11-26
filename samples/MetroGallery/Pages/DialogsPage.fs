module MetroGallery.Pages.DialogsPage

open Goo
open FunGoo.Children
open FunGoo.Metro

/// The tab cells of the temporary dialogs page: surfaces, then notices. The
/// shell `MetroContentDialog` and `MetroSettingsFlyout` stay in the shell and
/// get one launch row here in a later batch. A later batch replaces this
/// composition with the final sections.
type DialogsPageCells = {
  Overlays: unit -> Cell
  Notifications: unit -> Cell
}

/// One captioned section of the temporary page: a caption in the type ramp,
/// then the mounted tab cell.
let section(caption: string, key: string, factory: unit -> Cell) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.caption caption, Cell.Mount<Cell>(key, factory))

/// The temporary dialogs page: the page title, then each tab cell. The
/// controls keep their sizes, order, and demo data.
let create(cells: DialogsPageCells) : Cell = {
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
          MetroText.title "dialogs",
          section("SURFACES", "dialogs-overlays", cells.Overlays),
          section("NOTICES", "dialogs-notifications", cells.Notifications)
        )
}
