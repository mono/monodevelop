// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.IO
open FSharp.CompilerBinding
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Extensions
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
        let activeDoc = IdeApp.Workbench.ActiveDocument
        if activeDoc = null then null else

        let fileName = activeDoc.FileName.FullPath.ToString()
        
        let supported = MDLanguageService.SupportedFileName (fileName)
        if supported <> true then null else

        let docText = editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else

        let line, col, lineStr = editor.GetLineInfoFromOffset offset

        let result =
            //operate on available results no async gettypeparse results is available quick enough
            let parseAndCheckResults = MDLanguageService.Instance.GetTypedParseResultIfAvailable (context.Project.FileName.ToString(), fileName, docText, AllowStaleResults.MatchingSource)
            Async.RunSynchronously (
                async {
                    try
                        LoggingService.LogInfo "TooltipProvider: Getting tool tip"
                        let! symbol = parseAndCheckResults.GetSymbolAtLocation(line, col, lineStr)
                        //Hack: Because FCS does not always contain XmlDocSigs for tooltips we have to have to currently use the old tooltips
                        // to extract the signature, this is only limited in that it deals with only a single tooltip in a group/list
                        // This should be fine as there are issues with generic tooltip xmldocs anyway
                        // e.g. generics such as Dictionary<'a,'b>.Contains currently dont work.
                        let! tip = parseAndCheckResults.GetToolTip(line, col, lineStr)
                        //we create the backupSig as lazily as possible we could put the async call in here but I was worried about GC retension.
                        let backupSig = 
                            lazy
                                match tip with
                                | Some (FSharpToolTipText (first :: _remainder), (_startCol,_endCol)) ->
                                    match first with
                                    | FSharpToolTipElement.Single (_name, FSharpXmlDoc.XmlDocFileSignature(key, file)) -> Some (file, key)
                                    | FSharpToolTipElement.Group ((_name, FSharpXmlDoc.XmlDocFileSignature (key, file)) :: _remainder)  -> Some (file, key)
                                    | FSharpToolTipElement.CompositionError error ->
                                        LoggingService.LogError (sprintf "TooltipProvider: Composition error: %s" error)
                                        None
                                    | _ -> None
                                | _ -> None
                        
                        // As the new tooltips are unfinished we match ToolTip here to use the new tooltips and anything else to run through the old tooltip system
                        // In the section above we return EmptyTip for any tooltips symbols that have not yet ben finished
                        match symbol with
                        | Some s -> 
                             let tt = SymbolTooltips.getTooltipFromSymbolUse s backupSig
                             match tt with
                             | ToolTip(signature, summary) ->
                                 //get the TextSegment the the symbols range occupies
                                 let textSeg = Symbols.getTextSegment editor symbol.Value col lineStr
                                 let tooltipItem = TooltipItem((signature, summary), textSeg)
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
        //At the moment as the new tooltips are unfinished we have two types here
        // ToolTipText for the old tooltips and (string * XmlDoc) for the new tooltips
        match item.Item with 
        | :? FSharpToolTipText as titem ->
            let tooltip = TooltipFormatting.formatTip(titem)
            let (signature, comment) = 
                match tooltip with
                | [signature,comment] -> signature,comment
                //With multiple tips just take the head.  
                //This shouldnt happen anyway as we split them in the resolver provider
                | multiple -> multiple |> List.head
            //dont show a tooltip if there is no content
            if String.IsNullOrEmpty(signature) then null 
            else            
                let result = new MonoDevelop.Ide.CodeCompletion.TooltipInformationWindow(ShowArrow = true)
                let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
                if not (String.IsNullOrEmpty(comment)) then toolTipInfo.SummaryMarkup <- comment
                result.AddOverload(toolTipInfo)
                result.RepositionWindow ()                  
                new Control (result)

        | :? (string * XmlDoc) as tip -> 
            let signature, xmldoc = tip
            let result = new TooltipInformationWindow(ShowArrow = true)
            let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
            match xmldoc with
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

        | _ -> LoggingService.LogError "TooltipProvider: Type mismatch, not a FSharpLocalResolveResult"
               null
            
    interface IDisposable with
        member x.Dispose() = killTooltipWindow()
