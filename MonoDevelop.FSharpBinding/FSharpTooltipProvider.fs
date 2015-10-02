// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.IO
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Extensions
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp.Symbols
open ExtCore.Control
open Symbols

/// Resolves locations to tooltip items, and orchestrates their display.
type FSharpTooltipProvider() = 
    inherit TooltipProvider()

    //keep the last enterNotofy handler so we can remove the handler as a new TipWindow is created
    let mutable enterNotify = None : IDisposable option

    let killTooltipWindow() =
       enterNotify |> Option.iter (fun en -> en.Dispose ())

    override x.GetItem (editor, context, offset) =
      try
        let doc = IdeApp.Workbench.ActiveDocument
        if doc = null then null else

        let fileName = doc.FileName.FullPath.ToString()
        let projectFileName = context.Project |> function null -> fileName | project -> project.FileName.ToString()
        
        if not (MDLanguageService.SupportedFileName fileName) then null else

        let docText = editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else

        let line, col, lineStr = editor.GetLineInfoFromOffset offset

        let caretToken = 
            maybe {
              let! pd = Option.tryCast<FSharpParsedDocument> context.ParsedDocument
              let! tokens = pd.Tokens
              let (Tokens.TokenisedLine(_lineNumber, _offset, lineTokens, _state)) = tokens.[line-1]
              return! lineTokens |> List.tryFind (fun t -> col >= t.LeftColumn && col <= t.RightColumn) }

        let isTokenInvalid = 
          match caretToken with
          | Some token -> token.ColorClass = FSharpTokenColorKind.Comment ||
                          token.ColorClass = FSharpTokenColorKind.String ||
                          token.ColorClass = FSharpTokenColorKind.Text
          | None -> true

        if isTokenInvalid then null else 
        
        let tryKeyword =
           let ident = Parsing.findLongIdents(col, lineStr)
           match ident with
           | Some (col, identIsland) ->
             match identIsland with
             | [single] when PrettyNaming.KeywordNames |> List.contains single ->
               let startOffset = editor.LocationToOffset(line, col - single.Length+1)
               let endOffset = startOffset + single.Length
               let segment = Text.TextSegment.FromBounds(startOffset, endOffset)
               let tip = SymbolTooltips.getKeywordTooltip single
               Some (TooltipItem( tip, segment :> Text.ISegment))
             | _ -> None
           | None -> None

        let result =
            match tryKeyword with
            | Some keyword -> Tooltip keyword
            | None ->
            //operate on available results no async gettypeparse results is available quick enough
            let parseAndCheckResults = MDLanguageService.Instance.GetTypedParseResultIfAvailable (projectFileName, fileName, docText, AllowStaleResults.MatchingSource)
            Async.RunSynchronously (
                async {
                    try 
                        LoggingService.LogDebug "TooltipProvider: Getting tool tip"
                        let! symbol = parseAndCheckResults.GetSymbolAtLocation(line, col, lineStr)

                        // As the new tooltips are unfinished we match ToolTip here to use the new tooltips and anything else to run through the old tooltip system
                        // In the section above we return EmptyTip for any tooltips symbols that have not yet ben finished
                        match symbol with
                        | Some s -> 
                             let tt = SymbolTooltips.getTooltipFromSymbolUse s
                             match tt with
                             | ToolTip _ as tip ->
                                 //get the TextSegment the the symbols range occupies
                                 let textSeg = Symbols.getTextSegment editor symbol.Value col lineStr
                                 let tooltipItem = TooltipItem(tip, textSeg)
                                 return Tooltip tooltipItem
                             | EmptyTip -> return ParseAndCheckNotFound //TODO Support non symbol tooltips?
                        | None -> return ParseAndCheckNotFound
                    with
                    | :? TimeoutException -> return ParseAndCheckNotFound
                    | ex ->
                        LoggingService.LogError ("TooltipProvider: unexpected exception", ex)
                        return ParseAndCheckNotFound},
                ServiceSettings.blockingTimeout)
        match result with
        | ParseAndCheckNotFound -> LoggingService.LogWarning "TooltipProvider: ParseAndCheckResults not found"; null
        | NoToolTipText -> LoggingService.LogWarning "TooltipProvider: TootipText not returned"; null
        | NoToolTipData -> LoggingService.LogWarning "TooltipProvider: No data found"; null
        | Tooltip t -> t
       
      with exn ->
          LoggingService.LogError ("TooltipProvider: Error retrieving tooltip", exn)
          null

    override x.CreateTooltipWindow (_editor, _context, item, _offset, _modifierState) = 
        let doc = IdeApp.Workbench.ActiveDocument
        if (doc = null) then null else
        match unbox item.Item with 
        | ToolTips.ToolTip(signature, summary) -> 
            let result = new TooltipInformationWindow(ShowArrow = true)
            let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
            match summary with
            | Full(summary) -> toolTipInfo.SummaryMarkup <- summary
            | Lookup(key, potentialFilename) ->
                let summary = 
                    maybe {let! filename = potentialFilename
                           let! markup = TooltipXmlDoc.findDocForEntity(filename, key)
                           let summary = TooltipsXml.getTooltipSummary Styles.simpleMarkup markup
                           return summary}
                summary |> Option.iter (fun summary -> toolTipInfo.SummaryMarkup <- summary)
            | EmptyDoc -> ()
            result.AddOverload(toolTipInfo)
            result.RepositionWindow ()         
            new Control(result)
        | _ -> LoggingService.LogError "TooltipProvider: Type mismatch"
               null
            
    interface IDisposable with
        member x.Dispose() = killTooltipWindow()
