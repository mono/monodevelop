// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open FSharp.CompilerBinding
open Mono.TextEditor
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Resolves locations to tooltip items, and orchestrates their display.
/// We resolve language items to an NRefactory symbol.
type FSharpTooltipProvider() = 
    inherit Mono.TextEditor.TooltipProvider()

    // Keep the last result and tooltip window cached
    let mutable lastResult = None : TooltipItem option
    static let mutable lastWindow = None

    let killTooltipWindow() =
       match lastWindow with
       | Some(w:TooltipInformationWindow) -> w.Destroy()
       | None -> ()

    override x.GetItem (editor, offset) =
      try
        let fileName = IdeApp.Workbench.ActiveDocument.FileName.FullPath.ToString()
        let extEditor = editor :?> MonoDevelop.SourceEditor.ExtensibleTextEditor 
        let docText = editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else
        let projFile, files, args, framework = MonoDevelop.getCheckerArgs(extEditor.Project, fileName)
        let tyResOpt =
            MDLanguageService.Instance.GetTypedParseResultWithTimeout
                 (projFile,
                  fileName, 
                  docText, 
                  files,
                  args,
                  AllowStaleResults.MatchingSource,
                  ServiceSettings.blockingTimeout,
                  framework) |> Async.RunSynchronously
        LoggingService.LogInfo "TooltipProvider: Getting tool tip"
        match tyResOpt with
        | None -> LoggingService.LogWarning "TooltipProvider: ParseAndCheckResults not found"
                  null
        | Some tyRes ->
        // Get tool-tip from the language service
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, editor.Document)
        let tip = tyRes.GetToolTip(line, col, lineStr) |> Async.RunSynchronously
        match tip with
        | None -> LoggingService.LogWarning "TooltipProvider: TootipText not returned"
                  null
        | Some (ToolTipText(elems),_) when elems |> List.forall (function ToolTipElementNone -> true | _ -> false) -> 
            LoggingService.LogWarning "TooltipProvider: No data found"
            null
        | Some(tiptext,(col1,col2)) -> 
            LoggingService.LogInfo "TooltipProvider: Got data"
            //check to see if the last result is the same tooltipitem, if so return the previous tooltipitem
            match lastResult with
            | Some(tooltipItem) when
                tooltipItem.Item :?> ToolTipText = tiptext && 
                tooltipItem.ItemSegment = TextSegment(editor.LocationToOffset (line, col1 + 1), col2 - col1) -> tooltipItem
            //If theres no match or previous cached result generate a new tooltipitem
            | Some(_) | None -> 
                let line = editor.Document.OffsetToLineNumber offset
                let segment = TextSegment(editor.LocationToOffset (line, col1 + 1), col2 - col1)
                let tooltipItem = TooltipItem (tiptext, segment)
                lastResult <- Some(tooltipItem)
                tooltipItem
      with exn -> LoggingService.LogError ("TooltipProvider: Error retrieving tooltip", exn)
                  null

    override x.CreateTooltipWindow (editor, offset, modifierState, item) = 
        let doc = IdeApp.Workbench.ActiveDocument
        if (doc = null) then null else
        match item.Item with 
        | :? ToolTipText as titem ->
            let tooltip = TipFormatter.formatTip(titem)
            let (signature, comment) = 
                match tooltip with
                | [signature,comment] -> signature,comment
                //With multiple tips just take the head.  
                //This shouldnt happen anyway as we split them in the resolver provider
                | multiple -> multiple |> List.head
            //dont show a tooltip if there is no content
            if String.IsNullOrEmpty(signature) then null 
            else            
                let result = new TooltipInformationWindow(ShowArrow = true)
                let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
                if not (String.IsNullOrEmpty(comment)) then toolTipInfo.SummaryMarkup <- comment
                result.AddOverload(toolTipInfo)
                result.RepositionWindow ()                  
                result :> _
        | _ -> LoggingService.LogError "TooltipProvider: Type mismatch, not a FSharpLocalResolveResult"
               null
    
    override x.ShowTooltipWindow (editor, offset, modifierState, mouseX, mouseY, item) =
        match (lastResult, lastWindow) with
        | Some(lastRes), Some(lastWin) when item.Item = lastRes.Item && lastWin.IsRealized ->
            lastWin :> _                   
        | _ -> killTooltipWindow()
               match x.CreateTooltipWindow (editor, offset, modifierState, item) with
               | :? TooltipInformationWindow as tipWindow ->
                   let positionWidget = editor.TextArea
                   let region = item.ItemSegment.GetRegion(editor.Document)
                   let p1, p2 = editor.LocationToPoint(region.Begin), editor.LocationToPoint(region.End)
                   let caret = Gdk.Rectangle (int p1.X - positionWidget.Allocation.X, 
                                              int p2.Y - positionWidget.Allocation.Y, 
                                              int (p2.X - p1.X), 
                                              int editor.LineHeight)
                   //For debug this is usful for visualising the tooltip location
                   // editor.SetSelection(item.ItemSegment.Offset, item.ItemSegment.EndOffset)
               
                   tipWindow.ShowPopup(positionWidget, caret, MonoDevelop.Components.PopupPosition.Top)
                   tipWindow.EnterNotifyEvent.Add(fun _ -> editor.HideTooltip (false))
                   //cache last window shown
                   lastWindow <- Some(tipWindow)
                   lastResult <- Some(item)
                   tipWindow :> _
               | _ -> LoggingService.LogError "TooltipProvider: Type mismatch, not a TooltipInformationWindow"
                      null
            
    interface IDisposable with
        member x.Dispose() = killTooltipWindow()
