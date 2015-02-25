namespace MonoDevelop.FSharp

open System
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open Mono.TextEditor

type FSharpIndentationTracker(doc:Document) = 
    let textDoc = doc.Editor.Document
    let indentSize = doc.Editor.Options.IndentationSize

    // Lines ending in these strings will be indented
    let indenters = ["=";" do";"{";"[";"[|";"->";" try"]

    let (|AddIndent|_|) (x:string) = 
        if indenters |> List.exists(x.EndsWith) then Some ()
        else None
    let (|Match|_|) (x:string) = 
        if x.EndsWith "with" && x.Contains("match ") then Some (x.LastIndexOf "match ")
        else None

    let initialWhiteSpace (s:string) offset = 
        if offset >= s.Length then 0 else
        let s = s.Substring offset
        s.Length - s.TrimStart([|' '|]).Length
                
    let rec getIndentation lineDistance (line: DocumentLine) = 
        if line = null then "" else
        match textDoc.GetLineText(line.LineNumber).TrimEnd() with
        | x when String.IsNullOrWhiteSpace(x) -> getIndentation (lineDistance + 1) line.PreviousLine
        | Match i   when lineDistance < 2 -> String(' ', i)
        | AddIndent when lineDistance < 2 -> String(' ', line.GetIndentation(textDoc).Length + indentSize)
        | _ -> line.GetIndentation textDoc
  
    let getIndentString lineNumber column =  
        let caretColumn = doc.Editor.Caret.Column
        let line = textDoc.GetLine lineNumber

        let indentation = getIndentation 0 line
        if line = null then indentation else
        // Find white space in front of the caret and strip it out
        let text = textDoc.GetLineText(line.LineNumber)
        let reIndent = column = text.Length + 1 && caretColumn = 1 
        if not reIndent then indentation else
          let indent = getIndentation 0 (line.PreviousLine)
          let initialWs = initialWhiteSpace text 0
          if initialWs >= indent.Length then indentation else
          indent.Substring(initialWhiteSpace text 0)

    interface IIndentationTracker with
        member x.GetIndentationString (lineNumber, column) =
            getIndentString lineNumber column
        member x.GetIndentationString (offset) = 
            let loc = textDoc.OffsetToLocation (offset)
            getIndentString loc.Line loc.Column
        member x.GetVirtualIndentationColumn (offset) = 
            let loc = textDoc.OffsetToLocation (offset)
            1 + (getIndentString loc.Line loc.Column).Length
        member x.GetVirtualIndentationColumn (lineNumber, column) = 
            1 + (getIndentString lineNumber column).Length
