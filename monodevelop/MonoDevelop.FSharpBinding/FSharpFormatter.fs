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
open FSharp.CompilerBinding
open ExtCore.Control

type FormattingOption = 
    | Document
    | Selection of int * int

type FSharpFormatter()  = 
    inherit AbstractAdvancedFormatter()

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
                    
    let format (doc : Gui.Document option) style formatting input options =
        let isFsiFile = 
            match doc with 
            | Some d -> d.FileName.Extension.Equals(".fsi", StringComparison.OrdinalIgnoreCase)
            | _ -> false

        let config = getConfig style formatting

        match options with
        | Document ->
            let output =
                try 
                    let result =
                        trimIfNeeded input (CodeFormatter.formatSourceString isFsiFile input config)
                    //If onTheFly do the replacements in the document
                    doc |> Option.iter (fun d -> 
                        let line = d.Editor.Caret.Line
                        let col = d.Editor.Caret.Column
                        d.Editor.Document.Replace(0, input.Length, result)
                        d.Editor.SetCaretTo (line, col, false))
                    result
                with exn -> 
                    LoggingService.LogError("Error occured: {0}", exn.Message)
                    null
            output

        | Selection(fromOffset, toOffset) ->
            // Convert from offsets to line and column position
            let positions = 
                input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
                |> Seq.map (fun s -> String.length s + 1)
                |> Seq.scan (+) 0
                |> Seq.toArray
            LoggingService.LogInfo("**Fantomas**: Offsets from {0} to {1}", fromOffset, toOffset)

            let tryFormat = 
                maybe { 
                    let! startPos = offsetToPos positions (max 0 fromOffset)
                    let! endPos = offsetToPos positions (min input.Length toOffset+1)
                    let range = Range.mkRange "/tmp.fsx" startPos endPos
                    LoggingService.LogInfo("**Fantomas**: Try to format range {0}.", range)
                    let! result = 
                        try 
                            let selection = input.Substring(fromOffset, toOffset - fromOffset)
                            let result = 
                                 trimIfNeeded selection 
                                              (CodeFormatter.formatSelectionOnly isFsiFile range input config)
                            if doc.IsSome then
                                //If onTheFly do the replacements in the document
                                doc.Value.Editor.Document.Replace(fromOffset, selection.Length, result)
                                None
                            else Some(result)
                         with exn -> 
                             LoggingService.LogError("**Fantomas Error occured: {0}", exn.Message)
                             None
                    return result }

            match tryFormat with
            | Some newCode -> newCode
            | None -> null

    let formatText (doc : Gui.Document option) (policyParent : PolicyContainer) (mimeTypeInheritanceChain : string seq) input formattingOption = 
        let textStylePolicy = policyParent.Get<TextStylePolicy>(mimeTypeInheritanceChain)
        let formattingPolicy = policyParent.Get<FSharpFormattingPolicy>(mimeTypeInheritanceChain)
        format doc textStylePolicy formattingPolicy input formattingOption

    static member MimeType = "text/x-fsharp"

    override x.SupportsOnTheFlyFormatting = true
    override x.SupportsCorrectingIndent = false
    override x.CorrectIndenting(_policyParent, _mimeTypeChain, _data, _line) = raise (NotSupportedException())

    override x.OnTheFlyFormat(doc, fromOffset, toOffset) =
        let policyParent : PolicyContainer =
            match doc.Project with
            | null -> PolicyService.DefaultPolicies
            | project ->
            match project.Policies with 
            | null -> PolicyService.DefaultPolicies
            | policyBag -> policyBag  :> PolicyContainer

        let mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (FSharpFormatter.MimeType)
        let input = doc.Editor.Text
        if fromOffset = 0 && toOffset = String.length input then 
            LoggingService.LogInfo("**Fantomas**: OnTheFly Formatting document")
            formatText (Some doc) policyParent mimeTypeChain input Document
            |> ignore
        else 
            LoggingService.LogInfo("**Fantomas**: OnTheFly Formatting selection")
            formatText (Some doc) policyParent mimeTypeChain input (Selection(fromOffset, toOffset))
            |> ignore

    override x.FormatText(policyParent, mimeTypeChain, input, fromOffset, toOffset) = 
        if fromOffset = 0 && toOffset = String.length input then 
            LoggingService.LogInfo("**Fantomas**: Formatting document")
            formatText None policyParent mimeTypeChain input Document
        else 
            LoggingService.LogInfo("**Fantomas**: Formatting selection")
            formatText None policyParent mimeTypeChain input (Selection(fromOffset, toOffset))