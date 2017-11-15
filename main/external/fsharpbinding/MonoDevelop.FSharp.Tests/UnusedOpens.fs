namespace MonoDevelopTests

open NUnit.Framework
open MonoDevelop.FSharp

[<TestFixture>]
module ``Highlight unused opens`` =
    let assertUnusedOpens source expected =
        let doc = TestHelpers.createDoc source "defined"
        let res =
            highlightUnusedCode.getUnusedCode doc doc.Editor
            |> Async.RunSynchronously
        let opens = res.Value |> List.map(fun range -> highlightUnusedCode.textFromRange doc.Editor range)
        Assert.AreEqual(expected, opens, sprintf "%A" opens)

    [<Test>]
    let Simple() =
        assertUnusedOpens "open System" ["System"]

    [<Test>]
    let ``Auto open namespace not needed``() =
        let source = 
            """
            namespace module1namespace
            [<AutoOpen>]
            module module1 =
                let x = 1
            namespace consumernamespace
            open module1namespace
            module module2 =
                let y = x
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Operators``() =
        let source =
            """
            namespace n
            module Operators =
                let (+) x y = x + y
            namespace namespace1
            open n.Operators
            module module1 =
                let x = 1 + 1
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Active Patterns``() =
        let source =
            """
            namespace n
            module ActivePattern =
                let (|NotEmpty|_|) s =
                    match s with
                    | "" -> None
                    | _ -> Some NotEmpty
            namespace namespace1
            open n.ActivePattern
            module module1 =
                let s = ""
                match s with
                | NotEmpty -> Some s
                | _ -> None
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Duplicated open statements``() =
        let source = 
            """
            open System
            open System
            open System
            module module3 =
                Console.WriteLine("")
            """
        assertUnusedOpens source ["System"; "System"]

    [<Test>]
    let ``Fully qualified symbol``() =
        let source = 
            """
            open System
            module module1 =
                System.Console.WriteLine("")
            """
        assertUnusedOpens source ["System"]

    [<Test>]
    let ``Partly qualified symbol``() =
        let source = 
            """
            namespace MonoDevelop.Core.Text
            module TextSegment =
                let x=1
            namespace consumer
            open MonoDevelop.Core
            module module1 =
                let y=Text.TextSegment.x
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Module function``() =
        let source = 
            """
            namespace namespace1
            module FsUnit =
                let should()=1
            namespace namespace1
            open FsUnit
            module module1 =
                let y=should()
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Partially qualified interface``() =
        let source = 
            """
            namespace MonoDevelop.Core.Text
            type ISegment =
                abstract member Length : int
            namespace namespace1
            open MonoDevelop.Core
            module module1 =
                let someFunc(selection:Text.ISegment) = 1
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Microsoft.FSharp namespace is special``() =
        let source =
            """
            open FSharp.Reflection
            module test=FSharpType.IsFunction typeof<int> |> ignore
            """
        assertUnusedOpens source []

    [<Test>]
    let ``Type extension``() =
        let source =
            """
            namespace TypeExtension
            module ExtensionModule =
                type System.String with
                    member x.SomeExtensionMethod () = ()
            namespace namespace1
            open TypeExtension.ExtensionModule
            module myModule =
                let x = "".SomeExtensionMethod()
            """
        assertUnusedOpens source []

    [<Test>]
    let ``open module``() =
        let source =
            """
            module ElmishSample.Counter.Types

            type Model = { count: int }

            module ElmishSample.Counter.State

            open Types
            let init () = { count = 0 }
            """
        assertUnusedOpens source []