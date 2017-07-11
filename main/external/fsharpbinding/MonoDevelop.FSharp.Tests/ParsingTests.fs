namespace MonoDevelopTests
open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp.Shared

[<TestFixture>]
type ParsingTests() =
    let checkGetSymbol col lineStr expected expectedColumn =
        let expected = if expected = "" then [] else expected.Split '.' |> Array.toList
        match Parsing.findIdents col lineStr SymbolLookupKind.ByLongIdent
              |> Option.orTry (fun () -> Parsing.findIdents col lineStr SymbolLookupKind.Fuzzy) with
        | Some(colu, ident) -> ident |> should equal expected
                               colu |> should equal expectedColumn
        | None -> Assert.Fail "Could not find ident"

    let assertIdents (source: string) expected expectedColumn =
        let col = source.IndexOf "|"
        let source = source.Replace("|", "")
        checkGetSymbol col source expected expectedColumn

    let assertLongIdentsAndResidue (source: string) expectedIdent expectedResidue =
        let col = source.IndexOf "|"
        let source = source.Replace("|", "")
        let ident, residue = Parsing.findLongIdentsAndResidue(col, source)
        let expectedIdent = if expectedIdent = "" then [] else expectedIdent.Split '.'
                                                               |> Array.toList
        ident |> should equal expectedIdent
        residue |> should equal expectedResidue

    [<TestCase("let not|backticked = ", "notbackticked", 17)>]
    [<TestCase("open MonoDev|elop.FSharp", "MonoDevelop", 16)>]
    [<TestCase("open MonoDevelop.FSh|arp", "MonoDevelop.FSharp", 23)>]
    [<TestCase("open System.Te|xt.RegularExpressions", "System.Text", 16)>]
    member x.``Find long idents``(source: string, expected, expectedColumn) =
        assertIdents source expected expectedColumn

    [<TestCase("open MonoDevelop.FSh|arp", "MonoDevelop", "FSh")>]
    [<TestCase("open MonoDevelop.|", "MonoDevelop", "")>]
    [<TestCase("open MonoDevelop.FSharp|", "MonoDevelop", "FSharp")>]
    [<TestCase("open MonoDevelop.FSharp.|", "MonoDevelop.FSharp", "")>]
    [<TestCase("open MonoDev|", "", "MonoDev")>]
    [<TestCase(" |  ", "", "")>]
    [<TestCase("open |  ", "", "")>]
    [<TestCase("#load|", "", "#load")>]
    [<TestCase("#lo|", "", "#lo")>]
    member x.``Find long idents and residue``(source: string, expectedIdent, expectedResidue) =
        assertLongIdentsAndResidue source expectedIdent expectedResidue
    
    [<Test>]
    member x.``Find custom operator``() =
        let source = "let ( >|.> ) a b = a + b"
        assertIdents source ">.>" 9
