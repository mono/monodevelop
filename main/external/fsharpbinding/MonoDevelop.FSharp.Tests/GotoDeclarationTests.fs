namespace MonoDevelopTests

open MonoDevelop.FSharp
open MonoDevelop.FSharp.MonoDevelop
open MonoDevelop.Ide.Editor
open FsUnit
open NUnit.Framework

[<TestFixture>]
type ``Go to declaration``() =
    let assertXmlDocSigDeclaration (input: string) =
        let offset = input.LastIndexOf "|"
        if offset = -1 then
            failwith "Input must contain a |"
        let input = input.Remove(offset, 1)

        let expectedDeclaration =
            if input.Contains "$" then
                Some (input.IndexOf "$")
            else
                None
        
        let input = input.Replace("$", "")
        let doc = TestHelpers.createDoc input "defined"
        let editor = doc.Editor
        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset

        let symbol =
            async {
                let! symbolUse = doc.Ast.GetSymbolAtLocation(line, col - 1, lineStr)
                let! symbols = doc.Ast.GetAllUsesOfAllSymbolsInFile()
                return Refactoring.findDeclarationSymbol symbolUse.Value.Symbol.XmlDocSig symbols.Value
            } |> Async.RunSynchronously

        match expectedDeclaration, symbol with
        | Some offset, Some sym -> 
            let range = sym.RangeAlternate
            let symOffset = doc.Editor.LocationToOffset(DocumentLocation(range.StartLine, range.StartColumn + 1))
            symOffset |> should equal offset
        | None, None -> Assert.Pass "No declaration expected"
        | None, Some s -> Assert.Fail (sprintf "Expected not to not find declaration, found %A" s)
        | Some offset, None -> Assert.Fail "Did not find declaration"

    [<Test>]
    member x.``Go to xml doc sig jumps to declaration``() =
        assertXmlDocSigDeclaration
            """
            let $mysym = 1
            printf "%D" my|sym
            """
   
    [<Test>]
    member x.``No declaration found``() =
        assertXmlDocSigDeclaration
            "System.Console.Write|Line \"something\""


