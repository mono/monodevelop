namespace MonoDevelopTests
open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp

[<TestFixture>]
type ParsingTests() =
  let checkGetSymbol col lineStr expected =
    match Parsing.findLongIdents(col, lineStr)  with
    | Some(_colu, ident) -> ident |> should equal [expected]
    | None -> Assert.Fail "Could not find ident"
      
  let assertIdents (source: string) expected =
    let col = source.IndexOf "|"
    let source = source.Replace("|", "")
    checkGetSymbol col source expected 
    
  let assertLongIdentsAndResidue (source: string) expectedIdent expectedResidue =
    let col = source.IndexOf "|"
    let source = source.Replace("|", "")
    let ident, residue = Parsing.findLongIdentsAndResidue(col, source)
    let expectedIdent = if expectedIdent = "" then [] else [expectedIdent]
    ident |> should equal expectedIdent
    residue |> should equal expectedResidue
   
  [<TestCase("let not|backticked = ", "notbackticked")>]
  [<TestCase("open MonoDev|elop.FSharp", "MonoDevelop")>]
  [<TestCase("open MonoDevelop.FSh|arp", "MonoDevelop.FSharp")>]
  [<TestCase("open System.Te|xt.RegularExpressions", "System.Text")>]
  member x.``Find long idents``(source: string, expected) =
    assertIdents source expected
    
  [<TestCase("open MonoDevelop.FSh|", "MonoDevelop", "FSh")>]
  [<TestCase("open MonoDev|", "", "MonoDev")>]
  [<TestCase(" |  ", "", "")>]
  [<TestCase("open |  ", "", "")>]
  member x.``Find long idents and residue``(source: string, expectedIdent, expectedResidue) =
    assertLongIdentsAndResidue source expectedIdent expectedResidue
