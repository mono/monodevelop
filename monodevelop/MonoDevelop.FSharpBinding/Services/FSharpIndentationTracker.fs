namespace MonoDevelop.FSharp

open System
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open Mono.TextEditor

type FSharpIndentationTracker(doc:Document) = 
    let textDoc = doc.Editor.Document
    let indentSize = doc.Editor.Options.IndentationSize

    // Lines ending in these strings will be indented
    let indenters = ["=";"do";"{";"[";"[|"]

    let (|AddIndent|_|) (x:string) = 
        if indenters |> List.exists(x.EndsWith) then Some ()
        else None
    let (|Match|_|) (x:string) = 
        if x.EndsWith "with" && x.Contains("match ") then Some (x.LastIndexOf "match ")
        else None
        
    let rec getIndentation lineDistance (line: DocumentLine) = 
        if line = null then "" else
        match textDoc.GetLineText(line.LineNumber).TrimEnd() with
        | x when String.IsNullOrWhiteSpace(x) -> getIndentation (lineDistance + 1)line.PreviousLine
        | Match i when lineDistance < 2 -> String(' ', i)
        | AddIndent -> String(' ', line.GetIndentation(textDoc).Length + indentSize)
        | _ -> line.GetIndentation textDoc

    let getIndentString lineNumber column =  
        let line = textDoc.GetLine (lineNumber);
        getIndentation 0 line

    interface IIndentationTracker with
        member x.GetIndentationString (lineNumber, column) = getIndentString lineNumber column
        member x.GetIndentationString (offset) = 
            let loc = textDoc.OffsetToLocation (offset)
            if doc.Editor.Caret.Column = 0 then "" else
            getIndentString loc.Line loc.Column
        member x.GetVirtualIndentationColumn (offset) = 
            let loc = textDoc.OffsetToLocation (offset)
            (getIndentString loc.Line loc.Column).Length
        member x.GetVirtualIndentationColumn (lineNumber, column) = (getIndentString lineNumber column).Length
