namespace FunGoo.Metro

/// Embedded Selawik typeface registration for the Metro type ramp.
[<RequireQualifiedAccess>]
module MetroFont =

  /// UI sans-serif family name served from embedded resources.
  val family: string

  /// Light weight backing the 300 roles of the Metro type ramp.
  val lightWeight: int

  /// Regular weight backing the 400 roles of the Metro type ramp.
  val regularWeight: int

  /// Bold weight backing the 700 roles of the Metro type ramp.
  val boldWeight: int

  /// Registers each embedded Selawik weight once through
  /// <c>Goo.FontSource.Register</c> and returns <c>true</c> when all
  /// three weights are registered. Returns <c>false</c> when an
  /// embedded resource is absent, without throwing.
  val ensureRegistered: unit -> bool
