namespace MonoDevelop.FSharp

open System
open System.Threading.Tasks
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension

type FSharpTextPasteHandler(editor:TextEditor) =
    inherit TextPasteHandler()

    let rec getNextNonBlankLineNumber lineNumber endOffset=
        let line = editor.GetLine lineNumber
        if line.Length <> 0 || line.EndOffset > endOffset then
            lineNumber
        else
            getNextNonBlankLineNumber (lineNumber+1) endOffset

    override x.GetCopyData(offset, length) =
        // get the indent level the line was originally at
        let lineNumber = editor.OffsetToLineNumber offset
        let endOffset = offset + length

        let nonBlankLineNumber = getNextNonBlankLineNumber lineNumber endOffset

        let indent = editor.GetLineIndent nonBlankLineNumber
        [|byte indent.Length|]

    override x.PostFomatPastedText (_offset, _length) = Task.FromResult None :> Task


    override x.FormatPlainText(offset, text, copyData) =
        if editor.Options.IndentStyle = IndentStyle.Smart ||
           editor.Options.IndentStyle = IndentStyle.Virtual then
            // adjust the original indentation size
            // for the new location
            let location = editor.OffsetToLocation offset
            if location.Column > 1 then
                let getIndent (line:string) =
                    line.Length - (String.trimStart [|' '|] line).Length

                let fixIndent (line:string, indentDifference:int) =
                    if line.Length = 0 then
                        line
                    else
                        if indentDifference > 0 then
                            (String(' ', indentDifference)) + line
                        else
                            line.Substring -indentDifference

                let line = location.Line

                let insertionIndent = editor.GetLineIndent line
                let lines = String.getLines text
                let firstLine = lines.[0]
                let copyData = copyData |> Option.ofObj
                let firstLineIndent =
                    match copyData with
                    | Some data when data.Length > 0 -> int data.[0]
                    | _ -> getIndent firstLine

                let indentDifference = insertionIndent.Length - firstLineIndent
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

module indentationTracker =
    let processTextChange (editor:TextEditor) (change:Text.TextChange) =
        // When pressing Enter before the first non whitespace character,
        // we end up with leading whitespace before the new
        // newline.
        // We deal with that here as the indentation tracker
        // only deals with the line after the newline insertion.
        if String.IsNullOrWhiteSpace(change.InsertedText.Text) then
            let lineNumber = editor.OffsetToLineNumber change.Offset
            if lineNumber > 1 then
                let text = editor.GetLineText(lineNumber-1, false)
                if String.IsNullOrWhiteSpace text then
                    let previousLine = editor.GetLine(lineNumber - 1)
                    editor.RemoveText(previousLine.Offset, text.Length)

    let textChanged editor changes =
        changes |> Seq.iter (processTextChange editor)

type FSharpIndentationTracker(editor:TextEditor) =
    inherit IndentationTracker ()
    let indentSize = editor.Options.IndentationSize
    do
         editor.SetTextPasteHandler (FSharpTextPasteHandler(editor))

    // Lines ending in  these strings will be indented
    let indenters = ["=";" do"; "("; "{";"[";"[|";"->";" try"; " then"; " else"; "<-"; " lazy"; " begin"; " finally"]

    let (|AddIndent|_|) (x:string) =
        if indenters |> List.exists(x.EndsWith) then Some ()
        else None
    let (|Match|_|) (x:string) =
        if x.EndsWith "with" && x.Contains("match ") then Some (x.LastIndexOf "match ")
        else None

    let initialWhiteSpace (s:string) =
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

        let text = editor.GetLineText(line.LineNumber)

        let previousLineIndentation = getIndentation 0 (line.PreviousLine)
        let initialIndentLength = initialWhiteSpace text

        if line.Length > 0 && (not (String.IsNullOrWhiteSpace text)) && (initialIndentLength+1) >= caretColumn then
            // whitespace to the left of the caret
            // leave the indentation as-is
            String(' ', caretColumn-1)
        elif initialIndentLength >= previousLineIndentation.Length then
            indentation
        else
            previousLineIndentation.Substring(initialIndentLength)

    override x.GetIndentationString (lineNumber) =
        try
            let line = editor.GetLine (lineNumber)

            if line = null then
                ""
            else
                let indent = getIndentString lineNumber
                LoggingService.LogDebug ("FSharpIndentationTracker: indent: '{0}'", indent)
                indent
        with
        | ex -> LoggingService.LogError ("FSharpIndentationTracker", ex)
                ""

    override x.SupportedFeatures = IndentationTrackerFeatures.None ||| IndentationTrackerFeatures.CustomIndentationEngine

type IndentationTextEditorExtension() =
    inherit TextEditorExtension()
    let mutable disposable = None

    override x.Initialize() =
        disposable <-
            x.Editor.TextChanged.Subscribe(fun e -> indentationTracker.textChanged x.Editor e.TextChanges) |> Some

    override x.Dispose() =
        disposable |> Option.iter(fun d -> d.Dispose())

