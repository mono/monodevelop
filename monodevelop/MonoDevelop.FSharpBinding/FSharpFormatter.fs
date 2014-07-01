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

type FormattingOption  = 
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
                
    let format (doc : Gui.Document) (textStylePolicy : TextStylePolicy) (formattingPolicy : FSharpFormattingPolicy) input formattingOption =
        let isFsiFile = 
            if doc = null then false
            else doc.FileName.Extension.Equals(".fsi", StringComparison.OrdinalIgnoreCase)

        let config = 
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
                    SemicolonAtEndOfLine = format.SemicolonAtEndOfLine }
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
                    SemicolonAtEndOfLine = format.SemicolonAtEndOfLine }
        LoggingService.LogInfo("**Fantomas**: Read config - \n{0}", sprintf "%A" config)

        match formattingOption with
        | Document -> 
            let output = 
                try CodeFormatter.formatSourceString isFsiFile input config
                with :? FormatException as ex -> 
                    LoggingService.LogError("Error occurs: {0}", ex.Message)
                    input
            output

        | Selection(fromOffset, toOffset) ->
            // Convert from offsets to line and column position
            let positions = 
                input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
                |> Seq.map (fun s -> String.length s + 1)
                |> Seq.scan (+) 0
                |> Seq.toArray
            LoggingService.LogInfo("**Fantomas**: Offsets from {0} to {1}", fromOffset, toOffset)

            let formatted = 
                maybe { 
                    let! startPos = offsetToPos positions (max 0 fromOffset)
                    let! endPos = offsetToPos positions (min input.Length toOffset)
                    let range = Range.mkRange "/tmp.fsx" startPos endPos
                    let selectedText = doc.Editor.GetTextBetween(fromOffset, toOffset)
                    let trimLinefeed = not <| selectedText.EndsWith("\n")
                    LoggingService.LogInfo("**Fantomas**: Try to format range {0}.", range)
                    let! result = try 
                                      let result = CodeFormatter.formatSelectionOnly isFsiFile range input config
                                      Some(if trimLinefeed then result.TrimEnd('\n')
                                           else result)
                                  with :? FormatException as ex -> 
                                      LoggingService.LogError("**Fantomas Error occured: {0}", ex.Message)
                                      None
                    return result }

            match formatted with
            | Some newCode -> newCode
            | None -> null

    let formatText (doc : Gui.Document) (policyParent : PolicyContainer) (mimeTypeInheritanceChain : string seq) (input : string) formattingOption = 
        let textStylePolicy = policyParent.Get<TextStylePolicy>(mimeTypeInheritanceChain)
        let formattingPolicy = policyParent.Get<FSharpFormattingPolicy>(mimeTypeInheritanceChain)
        format doc textStylePolicy formattingPolicy input formattingOption

    override x.SupportsOnTheFlyFormatting = false
    override x.SupportsCorrectingIndent = false
    override x.CorrectIndenting(policyParent, mimeTypeChain, data, line) = raise (NotSupportedException())
    override x.OnTheFlyFormat(doc, startOffset, endOffset) = raise (NotSupportedException())

    override x.FormatText(policyParent, mimeTypeInheritanceChain, input, fromOffset, toOffset) = 
        let doc = IdeApp.Workbench.ActiveDocument
        if fromOffset = 0 && toOffset = String.length input then 
            LoggingService.LogInfo("**Fantomas**: Formatting document")
            formatText doc policyParent mimeTypeInheritanceChain input Document
        else 
            LoggingService.LogInfo("**Fantomas**: Formatting selection")
            formatText doc policyParent mimeTypeInheritanceChain input (Selection(fromOffset, toOffset))
