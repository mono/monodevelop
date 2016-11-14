namespace MonoDevelopTests

open NUnit.Framework
open MonoDevelop.FSharp
open FsUnit

[<TestFixture>]
module ``Highlight unused opens`` =
    let assertUnusedOpens source expected =
        let doc = TestHelpers.createDoc source "defined"
        let res = highlightUnusedOpens.getUnusedOpens doc doc.Editor
        let opens = fst (res.Value |> List.unzip)
        opens |> should equal expected

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
    let ``Auto open namespace not needed for nested module``() =
        let source = 
            """
            namespace module1namespace
            [<AutoOpen>]
            module module1 =
                module module2 =
                    let x = 1
            namespace consumernamespace
            open module1namespace
            module module3 =
                let y = module2.x
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