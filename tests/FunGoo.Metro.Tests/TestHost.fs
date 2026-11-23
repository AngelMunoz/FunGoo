module FunGoo.Metro.Tests.TestHost

open System
open Xunit
open Goo

[<assembly: CollectionBehavior(DisableTestParallelization = true)>]
do ()

type HeadlessWindowHost() =
  inherit EmbeddedWindowHost()

  let mutable wakes = 0

  member _.Wakes = wakes

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

  override _.DestroyVulkanSurface(_instance: nativeint, _surface: uint64) =
    raise(
      NotSupportedException(
        "This headless test host has no presentation surface."
      )
    )

let createHost(width: int, height: int) : HeadlessWindowHost =
  let host = new HeadlessWindowHost()
  host.Resize(width, height, width, height)
  host

let pumpFrames(host: EmbeddedWindowHost, count: int) : unit =
  for _ = 1 to count do
    host.RenderFrame(0.016)
