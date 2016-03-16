namespace MonoDevelopTests

open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp.MonoDevelop
open MonoDevelop.FSharp

[<TestFixture>]
type FsiTests() =

    //do
        //fsi.Initialize(null)

    [<Test>]
    member x.Init() =
        let fsi = new FSharpInteractivePad()
        fsi.SendText ";;"
        fsi.Text |> should equal ""