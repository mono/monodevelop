// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Diagnostics
open System.Text
open System.Linq
open System.Collections.Generic
open System.Threading
open FSharp.CompilerBinding

open Mono.TextEditor
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Refactoring

open ICSharpCode.NRefactory
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem

open Microsoft.FSharp.Compiler.SourceCodeServices

/// Resolves locations to tooltip items, and orchestrates their display.
///
/// We resolve language items to an NRefactory symbol.
type FSharpLanguageItemTooltipProvider() = 
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
        let extEditor = editor :?> MonoDevelop.SourceEditor.ExtensibleTextEditor 
        let docText = editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else
        let config = IdeApp.Workspace.ActiveConfiguration
        if config = null then null else
        let proj = extEditor.Project :?> MonoDevelop.Projects.DotNetProject
        let files = CompilerArguments.getSourceFiles(extEditor.Project.Items) |> Array.ofList
        let args = CompilerArguments.getArgumentsFromProject(proj, config)
        let framework = CompilerArguments.getTargetFramework(proj.TargetFramework.Id)
        let tyResOpt =
            MDLanguageService.Instance.GetTypedParseResultWithTimeout
                 (extEditor.Project.FileName.ToString(),
                  editor.FileName, 
                  docText, 
                  files,
                  args,
                  AllowStaleResults.MatchingSource,
                  ServiceSettings.blockingTimeout,
                  framework) |> Async.RunSynchronously
        Debug.WriteLine (sprintf "TooltipProvider: Getting tool tip")
        match tyResOpt with
        | None -> null
        | Some tyRes ->
        // Get tool-tip from the language service
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, editor.Document)
        let tip = tyRes.GetToolTip(line, col, lineStr) |> Async.RunSynchronously
        match tip with
        | None -> null
        | Some (ToolTipText(elems),_) when elems |> List.forall (function ToolTipElementNone -> true | _ -> false) -> 
            Debug.WriteLine ("TooltipProvider: No data found")
            null
        | Some(tiptext,(col1,col2)) -> 
            Debug.WriteLine("TooltipProvider: Got data")
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
      with x -> null

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
                | multiple -> multiple |> List.head |> (fun (signature,comment) -> signature,comment)
            //dont show a tooltip if there is no content
            if String.IsNullOrEmpty(signature) then null 
            else            
                let result = new TooltipInformationWindow(ShowArrow = true)
                let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
                if not (String.IsNullOrEmpty(comment)) then toolTipInfo.SummaryMarkup <- comment
                result.AddOverload(toolTipInfo)
                result.RepositionWindow ()                  
                result :> _
        | _ -> Debug.WriteLine("** not a FSharpLocalResolveResult!")
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
               | _ -> null
            
    interface IDisposable with
        member x.Dispose() = killTooltipWindow()

/// Resolves locations to NRefactory symbols and ResolveResult objects.
type FSharpResolverProvider() =
  do Debug.WriteLine ("Resolver: Creating FSharpResolverProvider")

  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:Document, offset:int, region:DomRegion byref) : ResolveResult =

      try 
        Debug.WriteLine ("Resolver: In GetLanguageItem")
        if doc.Editor = null || doc.Editor.Document = null then null else
        let docText = doc.Editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else
        let config = IdeApp.Workspace.ActiveConfiguration
        if config = null then null else

        Debug.WriteLine("Resolver: Getting results of type checking")
        // Try to get typed result - with the specified timeout
        let proj = doc.Project :?> MonoDevelop.Projects.DotNetProject
        let files = CompilerArguments.getSourceFiles(doc.Project.Items) |> Array.ofList
        let args = CompilerArguments.getArgumentsFromProject(proj, config)
        let framework = CompilerArguments.getTargetFramework(proj.TargetFramework.Id)
        let tyResOpt = 
            MDLanguageService.Instance.GetTypedParseResultWithTimeout
                 (doc.Project.FileName.ToString(),
                  doc.Editor.FileName, 
                  docText, 
                  files, 
                  args, 
                  AllowStaleResults.MatchingSource,
                  ServiceSettings.blockingTimeout,
                  framework) |> Async.RunSynchronously

        Debug.WriteLine("getting declaration location...")
        match tyResOpt with
        | None -> null
        | Some tyRes ->
        // Get the declaration location from the language service
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(doc.Editor.Caret.Offset, doc.Editor.Document)
        let loc = tyRes.GetDeclarationLocation(line, col, lineStr) |> Async.RunSynchronously
        let lastIdent = 
            match FSharp.CompilerBinding.Parsing.findLongIdents(col, lineStr) with 
            | Some(_, identIsland) -> Seq.last identIsland
            | None -> ""

        let fsSymbolOpt = tyRes.GetSymbol(line, col, lineStr) |> Async.RunSynchronously

        match fsSymbolOpt with 
        | None ->  null
        | Some fsSymbol -> 
            let reg = 
                match loc with
                | FindDeclResult.DeclFound(m) -> 
                    Debug.WriteLine("found, line = {0}, col = {1}, file = {2}", m.StartLine, m.StartColumn, m.FileName)
                    DomRegion(m.FileName,m.StartLine,m.StartColumn+1)
                | FindDeclResult.DeclNotFound(notfound) -> 
                    match notfound with 
                    | FindDeclFailureReason.Unknown           -> Debug.WriteLine("DeclNotFound: Unknown")
                    | FindDeclFailureReason.NoSourceCode      -> Debug.WriteLine("DeclNotFound: No Source Code")
                    | FindDeclFailureReason.ProvidedType(t)   -> Debug.WriteLine("DeclNotFound: ProvidedType")
                    | FindDeclFailureReason.ProvidedMember(m) -> Debug.WriteLine("DeclNotFound: ProvidedMember")
                    DomRegion.Empty
            region <- reg
            // This is the NRefactory symbol for the item - the Region is used for goto-definition
            let resolveResult = NRefactory.createResolveResult(doc.ProjectContent, fsSymbol, lastIdent, reg)
            resolveResult

      with exn -> 
        Debug.WriteLine (sprintf "Resolver: Exception: '%s'" (exn.ToString()))
        null

    member x.GetLanguageItem(doc:Document, offset:int, identifier:string) : ResolveResult =
      do Debug.WriteLine (sprintf "Resolver: in GetLanguageItem#2")
      let (result, region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      result

    /// Returns string with tool-tip from 'FSharpLocalResolveResult'
    member x.CreateTooltip(document, offset, result, errorInformation, modifierState) = null


