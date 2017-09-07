namespace MonoDevelopTests
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.FSharp.MonoDevelop
open FsUnit

[<TestFixture>]
module TypeSignatureHelp =
    let getSignatureHelp (source: string) =
        let offset = source.IndexOf("$")
        let source = source.Replace("$", "")

        let doc = TestHelpers.createDoc source ""
        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset

        let res = doc.Ast.GetToolTip(line,col,lineStr) |> Async.RunSynchronously
        match res with
        | None -> failwith "did not find tooltip"
        | Some (tooltip, _) ->
            signatureHelp.extractSignature tooltip

    [<Test>]
    let ``Function signature without parameters``() =
        getSignatureHelp "let so$mefunc() = ()" |> should equal "unit -> unit"

    [<Test>]
    let ``Add function signature``() =
        getSignatureHelp "let so$mefunc x y = x + y" |> should equal "x:int -> y:int -> int"

    [<Test>]
    let ``Function signature with generic parameter``() =
        getSignatureHelp "let so$mefunc x = x" |> should equal "x:'a -> 'a"

    [<Test>]
    let ``Property signature``() =
        """
        type someType() =
            member this.intProp$erty = 1
        """
        |> getSignatureHelp |> should equal "int"

    [<Test>]
    let ``Override signature``() =
        """
        type someType() =
            override this.ToStr$ing() = ""
        """
        |> getSignatureHelp |> should equal "string"

    [<Test>]
    let ``Override with generic parameter``() =
        """
        type baseType() =
            default this.Member(x) = 0

        type someType() =
            inherit baseType()
            override this.Mem$ber(x) = 1
        """
        |> getSignatureHelp |> should equal "x:'a -> int"

    [<Test>]
    let ``Override with int parameter``() =
        """
        type baseType() =
            default this.Member(x:int) = 0

        type someType() =
            inherit baseType()
            override this.Mem$ber(x:int) = 1
        """
        |> getSignatureHelp |> should equal "x:int -> int"