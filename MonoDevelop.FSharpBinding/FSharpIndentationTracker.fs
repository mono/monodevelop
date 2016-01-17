namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension

type FSharpTextPasteHandler(editor:TextEditor) =
    inherit TextPasteHandler()
    override x.GetCopyData(_offset, _length) = [||]
    override x.PostFomatPastedText (_offset, _length) = ()

    override x.FormatPlainText(offset, text, _copyData) =
        if editor.Options.IndentStyle = IndentStyle.Smart ||
           editor.Options.IndentStyle = IndentStyle.Virtual then
            let location = editor.OffsetToLocation offset
            if location.Column > 1 then
                let getIndent (line:string) =
                    line.Length - (String.trimStart [|' '|] line).Length

                let fixIndent (line:string, indentDifference:int) =
                    if indentDifference > 0 then
                        (String(' ', indentDifference)) + line
                    else
                        line.Substring -indentDifference

                let line = location.Line

                let indent = editor.GetLineIndent line
                let lines = String.getLines text
                let firstLine = lines.[0]
                let indentDifference = indent.Length - getIndent firstLine
                let remainingLines = lines |> Seq.skip (1) 
                                           |> Seq.map(fun line -> fixIndent(line, indentDifference))
                let lines = remainingLines
                            |> Seq.append (seq [(String.trimStart [|' '|] firstLine)])
                let res = String.Join (editor.Options.DefaultEolMarker, lines)
                res
            else
                text
        else
            text

type FSharpIndentationTracker(editor:TextEditor) = 
    inherit IndentationTracker ()
    let indentSize = editor.Options.IndentationSize
    do
         editor.SetTextPasteHandler (FSharpTextPasteHandler(editor))

    // Lines ending in  these strings will be indented
    let indenters = ["=";" do"; "("; "{";"[";"[|";"->";" try"; " then"; " else"; "("]

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

        match editor.GetLineText(line.LineNumber).TrimEnd() with
        | x when String.IsNullOrWhiteSpace(x) -> getIndentation (lineDistance + 1) line.PreviousLine
        | Match i   when lineDistance < 2 -> String(' ', i)
        | AddIndent when lineDistance < 2 -> String(' ', line.GetIndentation(editor).Length + indentSize)
        | _ -> line.GetIndentation editor
  
    let getIndentString lineNumber =  
        let caretColumn = editor.CaretColumn
        let line = editor.GetLine lineNumber

        let indentation = getIndentation 0 line
        if line = null then indentation else
        // Find white space in front of the caret and strip it out
        let text = editor.GetLineText(line.LineNumber)
        //TODO using 0 instead of column, which we dont have now
        let reIndent = 0 = text.Length + 1 && caretColumn = 1 
        if not reIndent then indentation else
          let indent = getIndentation 0 (line.PreviousLine)
          let initialWs = initialWhiteSpace text 0
          if initialWs >= indent.Length then indentation else
          indent.Substring(initialWhiteSpace text 0)

    override x.GetIndentationString (lineNumber) =
        try
            let line = editor.GetLine (lineNumber)
            let indent =
              if line = null then "" else
                getIndentString lineNumber
            LoggingService.LogInfo ("FSharpIndentationTracker: indent: '{0}'", indent)
            indent
        with
        | ex ->   LoggingService.LogError ("FSharpIndentationTracker", ex)
                  ""

    override x.SupportedFeatures = IndentatitonTrackerFeatures.None
       
