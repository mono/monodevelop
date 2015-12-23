namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension

type FSharpIndentationTracker(data:TextEditor) = 
    inherit IndentationTracker ()
    let indentSize = data.Options.IndentationSize

    // Lines ending in  these strings will be indented
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
                
    let rec getIndentation lineDistance (line: IDocumentLine) = 
        if line = null then "" else

        match data.GetLineText(line.LineNumber).TrimEnd() with
        | x when String.IsNullOrWhiteSpace(x) -> getIndentation (lineDistance + 1) line.PreviousLine
        | Match i   when lineDistance < 2 -> String(' ', i)
        | AddIndent when lineDistance < 2 -> String(' ', line.GetIndentation(data).Length + indentSize)
        | _ -> line.GetIndentation data
  
    let getIndentString lineNumber =  
        let caretColumn = data.CaretColumn
        let line = data.GetLine lineNumber

        let indentation = getIndentation 0 line
        if line = null then indentation else
        // Find white space in front of the caret and strip it out
        let text = data.GetLineText(line.LineNumber)
        //TODO using 0 instead of column, which we dont have now
        let reIndent = 0 = text.Length + 1 && caretColumn = 1 
        if not reIndent then indentation else
          let indent = getIndentation 0 (line.PreviousLine)
          let initialWs = initialWhiteSpace text 0
          if initialWs >= indent.Length then indentation else
          indent.Substring(initialWhiteSpace text 0)

    override x.GetIndentationString (lineNumber) =

        try
            let line = data.GetLine (lineNumber)
            let indent =
              if line = null then "" else
                getIndentString lineNumber
            LoggingService.LogInfo ("FSharpIndentationTracker: indent: '{0}'", indent)
            indent
        with
        | ex ->   LoggingService.LogError ("FSharpIndentationTracker", ex)
                  ""

    override x.SupportedFeatures = IndentatitonTrackerFeatures.None
       
