namespace MonoDevelopTests
open System.Text.RegularExpressions
open System.Threading
open NUnit.Framework
open FsUnit
open MonoDevelop.Ide.Editor
open MonoDevelop.FSharp.MonoDevelop
open MonoDevelop.FSharp
open ExtCore
open ExtCore.Control
open ExtCore.Control.Collections

[<TestFixture>]
type ``Expandselection``() =
    let rec getSelections(doc:TestDocument, selections:list<_>) =
        let selectionOpt = ExpandSelection.getExpandRange (doc.Editor, doc.Ast.ParseTree.Value)
        match selectionOpt with
        | None -> selections
        | Some selection -> 
            let start, finish = selection
            doc.Editor.SetSelection(start, finish)
            getSelections(doc, selection::selections)

    let assertExpansion (source:string) =
        let caret = "$"
        let mutable source = source
        let original = source
        for i in [1..9] do
            source <- source.Replace(i.ToString(), "")
        let offset = source.IndexOf caret
        if offset = -1 then
            failwith "Did not find caret in the source"
        source <- source.Replace(caret, "")
        let doc = TestHelpers.createDoc source ""
        doc.Editor.CaretOffset <- offset

        let selections = getSelections(doc, [])
                         |> List.rev
                         |> List.indexed
        
        for selection in selections do
            let index, (start, finish) = selection
            source <- source.Insert(start, (index+1) |> string)
            source <- source.Insert(finish+index*2+1, (index+1) |> string)
        source <- source.Insert(offset + selections.Length, caret)
        let failMessage = sprintf "%A\nExpected\n%s\n\nActual\n%s\n\n" doc.Ast.ParseTree.Value original source
        Assert.AreEqual(original, source, failMessage)
        

    static member testCases = 
        [|
            """6type 5Math() =
                  43let 21ad$d1 x y2 = 3
                      x + y4
                  let subtract x y = x - y56"""

            """7let 6math =
                  54let 3add x y = 
                      21$x1 + y234
                  let subtract x y = x - y567"""

            "3open 2FSharp.Compiler.1Se$rvice123"

            """3match expr with
               2| SynExpr.1Long$Ident1(_,_,_,range) -> Some (path, range)23"""

            """9let 8whatever() =
                  76let 5selections = 4getSelections(doc, [])
                                        3|> 21Li$st1.rev23456

                  for x in selections do
                      printf "%A" x789"""

            """76let 5selections = 4getSelections(doc, [])
                               3|> 21Li$st1.rev23456

               let x = 'c'7"""


            """4let 3x = 2new Dictionary<1str$ing1>()234"""

            """4match x with
               3| 2Some s -> 
                  1tr$ue123
               | None -> false4"""

            """32Async.1Sta$rt12(computation, token)3"""

            """4let 3s = 2"some long 1s$tring1"234"""

            """4let 3source = 21Fi$le1.ReadAllText trimmedfilename234"""

            """3let 21ast$ring1 = "astring"23"""

            """21le$t1 astring = "astring"2"""

            """6let 5search term =
               4token.Cancel()
               3token <- 2new 1Cancell$ationTokenSource1()23456"""
            
            """6let 5result = 4SearchResult3(2FileProvider(m.Groups.["filename"].Value), 1offs$et1, text.Length2)3456"""
        |]

    [<Test>]
    [<TestCaseSource("testCases")>]
    member x.``Expand selection repeatedly``(source) =
        assertExpansion source
