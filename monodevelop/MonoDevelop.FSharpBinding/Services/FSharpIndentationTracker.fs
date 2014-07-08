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
        let trimmed = x.TrimEnd() 
        if indenters |> List.exists(trimmed.EndsWith) then Some indentSize
        else None
        
    let rec getIndentation (line: DocumentLine) = 
         if line = null then "" else
         match textDoc.GetLineText(line.LineNumber) with
         | x when String.IsNullOrWhiteSpace(x) -> getIndentation line.PreviousLine
         | AddIndent i -> String(' ', line.GetIndentation(textDoc).Length + i)
         | _ -> line.GetIndentation textDoc

    let getIndentString lineNumber column =  
         let line = textDoc.GetLine (lineNumber);
         getIndentation line

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
