// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore.Control

module TooltipImpl =
    let extraKeywords = ["let!";"do!";"return!";"use!";"yield!";"->";"<-";"<@";"@>";"<@@";"@@>";":>";":?>"]
    let tryKeyword col lineStr =
        maybe {let! (_col, keyword) = Parsing.findIdents col lineStr SymbolLookupKind.Simple
               let! keyword = keyword |> List.tryHead
               if PrettyNaming.KeywordNames |> List.contains keyword || extraKeywords |> List.contains keyword
               then return keyword
               else return! None }

module MDTooltip =
    let keywordToTooltip (editor:TextEditor) line col (keyword:string) =
        let startOffset = editor.LocationToOffset(line, col - keyword.Length+1)
        let endOffset = startOffset + keyword.Length
        let segment = Text.TextSegment.FromBounds(startOffset, endOffset)
        let tip = SymbolTooltips.getKeywordTooltip keyword
        TooltipItem( tip, segment :> Text.ISegment)

/// Resolves locations to tooltip items, and orchestrates their display.
type FSharpTooltipProvider() =
    inherit TooltipProvider()

    //keep the last enterNotofy handler so we can remove the handler as a new TipWindow is created
    let mutable enterNotify = None : IDisposable option
    let killTooltipWindow() = enterNotify |> Option.iter (fun en -> en.Dispose ())
    let noTooltip = Task.FromResult null
    
    override x.GetItem (editor, context, offset, cancellationToken) =
        try
            let doc = IdeApp.Workbench.ActiveDocument
            if doc = null then noTooltip else

            let file = doc.FileName.FullPath.ToString()

            if not (FileService.supportedFileName file) then noTooltip else

            let source = editor.Text
            if source = null || offset >= source.Length || offset < 0 then noTooltip else

            let line, col, lineStr = editor.GetLineInfoFromOffset offset

            if Tokens.isInvalidTipTokenAtPoint editor context offset then noTooltip else

            let tooltipComputation =
                asyncChoice {
                    try
                        LoggingService.LogDebug "TooltipProvider: Getting tool tip"

                        let parseAndCheckResults = context.TryGetAst()
                        match parseAndCheckResults with
                        | Some ast ->
                            let! symbol = ast.GetSymbolAtLocation(line, col, lineStr) |> AsyncChoice.ofOptionWith "TooltipProvider: ParseAndCheckResults not found"
                            let! signature, xmldoc, footer =
                                SymbolTooltips.getTooltipFromSymbolUse symbol
                                |> Choice.ofOptionWith (sprintf "TooltipProvider: TootipText not returned\n   %s\n   %s" lineStr (String.replicate col "-" + "^"))

                            let highlightedTip = syntaxHighlight signature, xmldoc, footer

                            //get the TextSegment the the symbols range occupies
                            let textSeg = Symbols.getTextSegment editor symbol col lineStr

                            let tooltipItem = TooltipItem(highlightedTip, textSeg)
                            return tooltipItem
                        | None ->
                            return! AsyncChoice.error "TooltipProvider: ParseAndCheckResults not found"

                    with
                    | :? TimeoutException -> return! AsyncChoice.error "TooltipProvider: timeout"
                    | ex -> return! AsyncChoice.error (sprintf "TooltipProvider: Error: %A" ex)}
                    
            match TooltipImpl.tryKeyword col lineStr with
            | Some t ->
                let keywordTip = MDTooltip.keywordToTooltip editor line col t
                Task.FromResult keywordTip
            | None ->

            Async.StartAsTask(
                async {

                    let! tooltipResult = tooltipComputation
                    match tooltipResult with
                    | Success(tip) -> return tip
                    | Operators.Error(warning) -> LoggingService.LogWarning warning
                                                  return Unchecked.defaultof<_> }, cancellationToken = cancellationToken)

        with exn ->
            LoggingService.LogError ("TooltipProvider: Error retrieving tooltip", exn)
            Task.FromResult null

    override x.CreateTooltipWindow (_editor, _context, item, _offset, _modifierState) =
        let doc = IdeApp.Workbench.ActiveDocument
        if (doc = null) then null else
            let (signature, summary, footer) = unbox item.Item
            let result = new TooltipInformationWindow(ShowArrow = true)
            let toolTipInfo = new TooltipInformation(SignatureMarkup=signature, FooterMarkup=footer)
            let formattedSummary = SymbolTooltips.formatSummary summary
            if not (String.IsNullOrWhiteSpace formattedSummary) then
                toolTipInfo.SummaryMarkup <- formattedSummary
            result.AddOverload(toolTipInfo)
            result.RepositionWindow ()
            Control.op_Implicit result

    interface IDisposable with
        member x.Dispose() = killTooltipWindow()
