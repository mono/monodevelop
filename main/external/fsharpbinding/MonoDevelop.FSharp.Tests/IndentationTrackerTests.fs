namespace MonoDevelopTests
open NUnit.Framework
open MonoDevelop.FSharp
open FsUnit
open Mono.TextEditor

[<TestFixture>]
type IndentationTrackerTests() =

    let docWithCaretAt (content:string) =
        let d = TestHelpers.createDoc(content.Replace("§", "")) ""
        d.Editor.IndentationTracker <- new FSharpIndentationTracker(d.Editor)
        do match content.IndexOf('§') with
           | -1 -> ()
           | x  -> let l = d.Editor.OffsetToLocation(x)
                   d.Editor.SetCaretLocation(l.Line, l.Column)
        d

    let getIndent (content:string) =
        let doc = docWithCaretAt content
        let tracker = FSharpIndentationTracker(doc.Editor)
        let caretLine = doc.Editor.CaretLine
        tracker.GetIndentationString(caretLine).Length

    let insertEnterAtSection (text:string) =
        let idx = text.IndexOf ('§')
        let doc = new TextDocument(text.Replace("§", ""))
        use data = new TextEditorData (doc)
        data.Caret.Offset <- idx
        MiscActions.InsertNewLine(data)
        data.Document.Text

    [<Test>]
    member x.``Basic indents``() =
        let getIndent (doc:TestDocument, line:int, col) =
            doc.Editor.SetCaretLocation (2, 2)
            let column = doc.Editor.GetVirtualIndentationColumn (line)
            column

        let doc = "" |> TestHelpers.createDoc """
let a =

let b = (fun a ->

  let b = a
"""
        doc.Editor.IndentationTracker <- FSharpIndentationTracker(doc.Editor)
        getIndent (doc, 3, 1) |> should equal 5
        getIndent (doc, 5, 1) |> should equal 5
        getIndent (doc, 7, 1) |> should equal 3

    [<Test>]
    member x.``Match expression``() =
        getIndent("let m = match 123 with\n§") |> should equal 8

    [<Test>]
    member x.``If then expression``() =
        getIndent("if true then\n§") |> should equal 4

    [<Test>]
    member x.``Indented match expression``() =
        getIndent("""let m =
   match 123 with
    §""") |> should equal 3

    [<Test>]
    member x.``Enter doesnt change indentation at indent position``() =
        let input = """  let a = 123
  §let c = 321"""
        input
        |> insertEnterAtSection
        |> should equal @"  let a = 123

  let c = 321"

    [<Test>]
    member x.``Enter after equals indents``() =
        let input = """  let a = §123"""
        input
        |> insertEnterAtSection
        |> shouldEqualIgnoringLineEndings """  let a = 
  123"""
