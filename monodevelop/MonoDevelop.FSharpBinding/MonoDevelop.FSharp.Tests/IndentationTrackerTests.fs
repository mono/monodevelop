namespace MonoDevelopTests
open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open FSharp.CompilerBinding
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger

[<TestFixture>]
type IndentationTrackerTests() =
    inherit TestBase()
    let mutable doc = Unchecked.defaultof<Document>

    let content = """
let a = 

let b = (fun a ->

  let b = a
"""

    let docWithCaret (content:string) = 
        let d = fst(TestHelpers.createDoc(content.Replace("§", "")) [])
        do match content.IndexOf('§') with
           | -1 -> ()
           | x  -> let l = d.Editor.OffsetToLocation(x)
                   d.Editor.SetCaretTo(l.Line, l.Column)
        d

    let getBasicOffset expr =
        let startOffset = content.IndexOf (expr, StringComparison.Ordinal)
        startOffset + (expr.Length / 2)

    let getIndent (doc:Document, content:string, line, col) =
        doc.Editor.SetCaretTo(2, 2)
        let column = doc.Editor.IndentationTracker.GetVirtualIndentationColumn(line, col)
        column

    [<TestFixtureSetUp>]
    override x.Setup() =
        base.Setup()
        doc <- fst(TestHelpers.createDoc(content) [])

    
    [<Test>]
    member x.``Basic Indents``() =
       // let basicOffset = getBasicOffset (localVariable)
        getIndent (doc, content, 3, 1) |> should equal 5
        getIndent (doc, content, 5, 1) |> should equal 5
        getIndent (doc, content, 7, 1) |> should equal 3


    [<Test>]
    member x.MatchExpression() =
        let doc = docWithCaret("""let m = match 123 with§""")
        doc.Editor.GetVirtualIndentationColumn(9)

        |> should equal 9

    [<Test>]
    [<Ignore("InsertAtCaret doesn't simulate what happens when you press enter in MD, so this test currently fails")>]
    member x.EnterDoesntChangeIndentationAtIndentPosition() =
        let doc = docWithCaret("""  let a = 123
  §let b = 321""")
        doc.Editor.InsertAtCaret("\n")
        doc.Editor.Document.Text 
        |> should equal @"  let a = 123

  let b = 321"

    [<Test>]
    member x.EnterDoesntChangeIndentationAtStartOfLine() =
        let doc = docWithCaret("""  let a = 123
§  let b = 321""")
        doc.Editor.InsertAtCaret("\n")
        doc.Editor.Document.Text 
        |> should equal @"  let a = 123

  let b = 321"

    [<Test>]
    [<Ignore("InsertAtCaret doesn't properly simulate what happens when you press enter in MD, so this test currently fails")>]
    member x.EnterAfterEqualsIndents() =
        let doc = docWithCaret """  let a = §123"""
        doc.Editor.InsertAtCaret("\n")
        doc.Editor.Document.Text 
        |> should equal "  let a = 123\n      123"




   

