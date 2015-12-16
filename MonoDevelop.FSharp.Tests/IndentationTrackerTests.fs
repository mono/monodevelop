namespace MonoDevelopTests
open NUnit.Framework
open MonoDevelop.FSharp
open FsUnit

[<TestFixture>]
type IndentationTrackerTests() =
    let content = """
let a = 

let b = (fun a ->

  let b = a
"""

    let docWithCaret (content:string) = 
        let d = TestHelpers.createDoc(content.Replace("§", "")) ""
        d.Editor.SetIndentationTracker (FSharpIndentationTracker(d.Editor))
        do match content.IndexOf('§') with
           | -1 -> ()
           | x  -> let l = d.Editor.OffsetToLocation(x)
                   d.Editor.SetCaretLocation(l.Line, l.Column)
        d

    let getIndent (content:string) =
      let doc = docWithCaret content
      let tracker = FSharpIndentationTracker(doc.Editor)
      let caretLine = doc.Editor.CaretLine
      tracker.GetIndentationString(caretLine).Length + 1

    [<Test>]
    member x.BasicIndents() =
      let getIndent (doc:TestDocument, line:int, col) =
          doc.Editor.SetCaretLocation (2, 2)
          let column = doc.Editor.GetVirtualIndentationColumn (line)
          column

      let doc = TestHelpers.createDoc(content) ""
      doc.Editor.SetIndentationTracker (FSharpIndentationTracker(doc.Editor))
      getIndent (doc, 3, 1) |> should equal 5
      getIndent (doc, 5, 1) |> should equal 5
      getIndent (doc, 7, 1) |> should equal 3

    [<Test>]
    member x.MatchExpression() =
        getIndent("let m = match 123 with\n§") |> should equal 9

    [<Test>]
    member x.IndentedMatchExpression() =
        getIndent("""let m =
   match 123 with
    §""") |> should equal 4
  
    [<Test>]
    [<Ignore("InsertAtCaret doesn't simulate what happens when you press enter in MD, so this test currently fails")>]
    member x.EnterDoesntChangeIndentationAtIndentPosition() =
        let doc = docWithCaret("""  let a = 123
  §let b = 321""")
        doc.Editor.InsertAtCaret("\n")
        doc.Editor.Text 
        |> should equal @"  let a = 123

  let b = 321"

    [<Test>]
    [<Ignore("InsertAtCaret doesn't properly simulate what happens when you press enter in MD, so this test currently fails")>]
    member x.EnterAfterEqualsIndents() =
        let doc = docWithCaret """  let a = §123"""
        doc.Editor.InsertAtCaret("\n")
        doc.Editor.Text 
        |> should equal "  let a = 123\n      123"
