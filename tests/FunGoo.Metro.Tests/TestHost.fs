module FunGoo.Metro.Tests.TestHost

open System
open Xunit
open Goo

[<assembly: CollectionBehavior(DisableTestParallelization = true)>]
do ()

/// A window host for the headless tests. It has no presentation surface.
/// <c>Wakes</c> is the number of frames that the window requested.
[<AbstractClass>]
type HeadlessWindowHost() =
  inherit EmbeddedWindowHost()

  /// The number of frames that the window requested.
  abstract Wakes: int

[<RequireQualifiedAccess>]
module HeadlessWindowHost =

  let create() : HeadlessWindowHost =
    let mutable wakes = 0

    {
      new HeadlessWindowHost() with
        override _.Wakes = wakes

        override _.RequestFrame() = wakes <- wakes + 1

        override _.LoadVulkanLibrary() =
          raise(
            NotSupportedException(
              "This headless test host has no presentation surface."
            )
          )

        override _.GetVulkanGetInstanceProcAddr() =
          raise(
            NotSupportedException(
              "This headless test host has no presentation surface."
            )
          )

        override _.UnloadVulkanLibrary() =
          raise(
            NotSupportedException(
              "This headless test host has no presentation surface."
            )
          )

        override _.GetVulkanInstanceExtensions() =
          raise(
            NotSupportedException(
              "This headless test host has no presentation surface."
            )
          )

        override _.CreateVulkanSurface
          (_instance: nativeint, _surface: byref<uint64>)
          =
          raise(
            NotSupportedException(
              "This headless test host has no presentation surface."
            )
          )

        override _.DestroyVulkanSurface
          (_instance: nativeint, _surface: uint64)
          =
          raise(
            NotSupportedException(
              "This headless test host has no presentation surface."
            )
          )
    }

/// A host of <c>width</c> by <c>height</c> logical pixels.
let createHost(width: int, height: int) : HeadlessWindowHost =
  let host = HeadlessWindowHost.create()
  host.Resize(width, height, width, height)
  host

let pumpFrames(host: EmbeddedWindowHost, count: int) : unit =
  for _ = 1 to count do
    host.RenderFrame(0.016)
