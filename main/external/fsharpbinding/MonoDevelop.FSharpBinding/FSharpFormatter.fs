namespace MonoDevelop.FSharp

open System
open System.Diagnostics
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide
open MonoDevelop.Projects.Policies
open MonoDevelop.Ide.CodeFormatting
open Mono.TextEditor
open Fantomas
open Fantomas.FormatConfig
open Microsoft.FSharp.Compiler
open ExtCore.Control
open MonoDevelop.Core.Text

type FormattingOption =
    | Document
    | Selection of int * int

type FSharpFormatter()  =
    inherit AbstractCodeFormatter()

    let offsetToPos (positions : _ []) offset =
        let rec searchPos start finish =
            if start >= finish then None
            elif start + 1 = finish then Some(Range.mkPos (start + 1) (offset - positions.[start]))
            else
                let mid = (start + finish) / 2
                if offset = positions.[mid] then Some(Range.mkPos (mid + 1) 0)
                elif offset > positions.[mid] then searchPos mid finish
                else searchPos start mid
        searchPos 0 (positions.Length - 1)

    let getConfig (textStylePolicy:TextStylePolicy) (formattingPolicy: FSharpFormattingPolicy) =
        match textStylePolicy, formattingPolicy with
        | null, null ->
            LoggingService.LogWarning("**Fantomas**: Fall back to default config")
            FormatConfig.Default
        | null, _ ->
            let format = formattingPolicy.DefaultFormat
            { FormatConfig.Default with
                IndentOnTryWith = format.IndentOnTryWith
                ReorderOpenDeclaration = format.ReorderOpenDeclaration
                SpaceAfterComma = format.SpaceAfterComma
                SpaceAfterSemicolon = format.SpaceAfterSemicolon
                SpaceAroundDelimiter = format.SpaceAroundDelimiter
                SpaceBeforeArgument = format.SpaceBeforeArgument
                SpaceBeforeColon = format.SpaceBeforeColon
                SemicolonAtEndOfLine = false }
        | _, null ->
            { FormatConfig.Default with
                PageWidth = textStylePolicy.FileWidth
                IndentSpaceNum = textStylePolicy.IndentWidth }
        | _ ->
            let format = formattingPolicy.DefaultFormat
            { FormatConfig.Default with
                PageWidth = textStylePolicy.FileWidth
                IndentSpaceNum = textStylePolicy.IndentWidth
                IndentOnTryWith = format.IndentOnTryWith
                ReorderOpenDeclaration = format.ReorderOpenDeclaration
                SpaceAfterComma = format.SpaceAfterComma
                SpaceAfterSemicolon = format.SpaceAfterSemicolon
                SpaceAroundDelimiter = format.SpaceAroundDelimiter
                SpaceBeforeArgument = format.SpaceBeforeArgument
                SpaceBeforeColon = format.SpaceBeforeColon
                SemicolonAtEndOfLine = false }

    let trimIfNeeded (input:string) (output:string) =
        let trimLinefeed = not <|  input.EndsWith("\n")
        if trimLinefeed then output.TrimEnd('\n')
        else output

    let format (editor : Editor.TextEditor option) style formatting input options : ITextSource =
        let isFsiFile =
            match editor with
            | Some d -> d.FileName.Extension.Equals(".fsi", StringComparison.OrdinalIgnoreCase)
            | _ -> false

        let config = getConfig style formatting

        match options with
        | Document ->
            let output =
                try
                    let result = trimIfNeeded input (CodeFormatter.formatSourceString isFsiFile input config)
                    //If onTheFly do the replacements in the document
                    editor
                    |> Option.iter (fun editor ->
                        let line = editor.CaretLine
                        let col = editor.CaretColumn
                        editor.ReplaceText(0, input.Length, result)
                        editor.SetCaretLocation (line, col, false))
                    StringTextSource (result)
                with exn ->
                    LoggingService.LogError("Error occured: {0}", exn.Message)
                    StringTextSource input
            output :> ITextSource

        | Selection(fromOffset, toOffset) ->
            // Convert from offsets to line and column position
            let positions =
                input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
                |> Seq.map (fun s -> String.length s + 1)
                |> Seq.scan (+) 0
                |> Seq.toArray
            LoggingService.LogDebug("**Fantomas**: Offsets from {0} to {1}", fromOffset, toOffset)

            let tryFormat =
                maybe {
                    let! startPos = offsetToPos positions (max 0 fromOffset)
                    let! endPos = offsetToPos positions (min input.Length toOffset+1)
                    let range = Range.mkRange "/tmp.fsx" startPos endPos
                    LoggingService.LogDebug("**Fantomas**: Try to format range {0}.", range)
                    let! result =
                        try
                            let selection = input.Substring(fromOffset, toOffset - fromOffset)
                            let result = trimIfNeeded selection (CodeFormatter.formatSelectionOnly isFsiFile range input config)

                            match editor with
                            | Some editor ->
                                //If onTheFly do the replacements in the document
                                editor.ReplaceText(fromOffset, selection.Length, result)
                                None
                            | None -> Some(result)
                         with exn ->
                             LoggingService.LogError("**Fantomas Error occured: {0}", exn.Message)
                             None
                    return result }

            let formatted =
                match tryFormat with
                | Some newCode -> StringTextSource (newCode)
                | None -> StringTextSource.Empty
            formatted :> ITextSource

    let formatText (editor : Editor.TextEditor option) (policyParent : PolicyContainer) (mimeType:string) input formattingOption =
        let textStylePolicy = policyParent.Get<TextStylePolicy>(mimeType)
        let formattingPolicy = policyParent.Get<FSharpFormattingPolicy>(mimeType)
        format editor textStylePolicy formattingPolicy input formattingOption

    static member MimeType = "text/x-fsharp"

    override x.SupportsOnTheFlyFormatting = true
    override x.SupportsCorrectingIndent = false

    override x.OnTheFlyFormatImplementation(editor, context, fromOffset, toOffset) =
        let policyParent : PolicyContainer =
            match context.Project with
            | null -> PolicyService.DefaultPolicies
            | project ->
            match project.Policies with
            | null -> PolicyService.DefaultPolicies
            | policyBag -> policyBag  :> PolicyContainer

        let mimeType = DesktopService.GetMimeTypeForUri (editor.FileName.ToString())
        let input = editor.Text
        if fromOffset = 0 && toOffset = String.length input then
            formatText (Some editor) policyParent mimeType input Document
            |> ignore
        else
            formatText (Some editor) policyParent mimeType input (Selection(fromOffset, toOffset))
            |> ignore

    override x.FormatImplementation(policyParent, mimeType, input, fromOffset, toOffset) =
        if fromOffset = 0 && toOffset = String.length input.Text then
            formatText None policyParent mimeType input.Text Document
        else
            formatText None policyParent mimeType input.Text (Selection(fromOffset, toOffset))
