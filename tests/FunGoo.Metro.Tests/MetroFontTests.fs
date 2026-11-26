module FunGoo.Metro.Tests.MetroFontTests

open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost

let private textRoot(content: string) : Cell = {
  new Cell() with
    override _.Build() : Blob = Text(Content = content) :> Blob
}

[<Fact>]
let ``Default Metro config reports the Selawik family``() =
  Assert.Equal(MetroFont.family, MetroConfig.defaultConfig.FontFamily)
  Assert.Equal("Selawik", MetroConfig.defaultConfig.FontFamily)

[<Fact>]
let ``Attaching a headless window registers the Selawik sources without error``
  ()
  =
  use host = createHost(480, 320)
  let probe = Window(Root = textRoot "font probe")

  Metro.attach(probe, MetroConfig.defaultConfig)
  probe.Attach(host) |> ignore
  pumpFrames(host, 5)

  Assert.Equal("Selawik", (Metro.readWindow probe).FontFamily)
  Assert.True(MetroFont.ensureRegistered())

[<Fact>]
let ``Caller font family override survives attach``() =
  use host = createHost(480, 320)
  let probe = Window(Root = textRoot "override probe")

  Metro.attach(
    probe,
    {
      MetroConfig.defaultConfig with
          FontFamily = "Custom Family"
    }
  )

  probe.Attach(host) |> ignore
  pumpFrames(host, 5)

  Assert.Equal("Custom Family", (Metro.readWindow probe).FontFamily)
