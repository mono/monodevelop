namespace MonoDevelopTests
open NUnit.Framework
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp
open MonoDevelop.FSharp.MonoDevelop
open FsUnit

[<TestFixture>]
module TypeSignatureHelp =
    let isFSharp (symbolUse: FSharpSymbolUse)=
        match symbolUse with 
        | SymbolUse.MemberFunctionOrValue mfv ->
            signatureHelp.isFSharp mfv
        | _ -> failwith "Not a function"

    let getSignatureHelp (source: string) =
        let offset = source.IndexOf("$")
        let source = source.Replace("$", "")
        let doc = TestHelpers.createDoc source ""
        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset

        let symbolUse = doc.Ast.GetSymbolAtLocation(line, col - 1, lineStr) |> Async.RunSynchronously
        match symbolUse with
        | Some symbolUse' ->
            let res = doc.Ast.GetToolTip(line,col,lineStr) |> Async.RunSynchronously
            match res with
            | None -> failwith "did not find tooltip"
            | Some (tooltip, _) ->
                signatureHelp.extractSignature tooltip (isFSharp symbolUse')
        | None -> failwith "No symbol found at location"

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
        |> getSignatureHelp |> should equal "unit -> string"

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

    [<Test>]
    let ``Override BCL method with parameter``() =
        """
        type someType() =
            inherit System.IO.Stream()
            override this.Dis$pose(disposing) = ()
        """
        |> getSignatureHelp |> should equal "(disposing: bool) -> unit"

    [<Test>]
    let ``Override BCL method with multiple parameters``() =
        """
        open System.IO
        open System.Threading
        open System.Threading.Tasks

        type someType() =
            inherit Stream()
            override this.Cop$yToAsync(dest, bufferSize, token) = Task.FromResult 1
        """
        |> getSignatureHelp |> should equal "(destination: Stream, bufferSize: int, cancellationToken: CancellationToken) -> Task"

    [<Test>]
    let ``Tuple argument``() =
        "let so$mefunc(x:int, y:int) = ()"
        |> getSignatureHelp |> should equal "x:int * y:int -> unit"

    [<Test>]
    let ``Tuple return``() =
        "let so$mefunc(x:int, y:int) = x, y"
        |> getSignatureHelp |> should equal "x:int * y:int -> int * int"

    [<Test>]
    let ``Double backticked function``() =
        """
        let ``double b$ackticked function``() = ()
        """
        |> getSignatureHelp
        |> should equal "unit -> unit"

    [<Test>]
    let ``Nested function``() =
        """
        let someFunc() =
           let nested$Func() = ()
        """
        |> getSignatureHelp
        |> should equal "(unit -> unit)"
