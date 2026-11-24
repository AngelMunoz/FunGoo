module MetroGallery.GalleryStore

/// The values that the controls tab writes and that the hub reads. One
/// mutable field per concern.
type GalleryStore = {
  /// Number of taps on a tile of the hub.
  mutable TileTapCount: int
  /// Text of the last action.
  mutable LastAction: string
  /// Value of the slider of the controls tab. The hub shows it as a
  /// progress bar.
  mutable SliderValue: float
  /// Text of the display name field.
  mutable TextValue: string
  /// Text of the passcode field.
  mutable SecretValue: string
  /// <c>true</c> when the toggle switch is on and the progress ring runs.
  mutable ToggleEnabled: bool
  /// <c>true</c> when the check box is checked.
  mutable CheckAccepted: bool
  /// Identifier of the selected radio button.
  mutable RadioChoice: string
  /// <c>true</c> when the pin button is pressed.
  mutable TogglePressed: bool
}
