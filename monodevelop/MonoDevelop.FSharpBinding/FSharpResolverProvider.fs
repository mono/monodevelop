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

/// Result of F# identifier resolution at the specified location stores the data tip that we 
/// get from the language service (this is passed to MonoDevelop, which asks about tooltip later)
///
/// We resolve language items to an under-specified LocalResolveResult (with an additional field for
/// the data tip text). Goto-definition can operate on the LocalResolveResult.
// 
// Other interesting ResolveResult's we may one day want to return are:
// 
// -- MemberResolveResult
// -- MethodGroupResolveResult
// -- TypeResolveResult
//
// or to return complete IEntity data.
//
// Also of interest is FindReferencesHandler.FindRefs (obj), for find-all-references and renaming.

//TODO: Implement windows caching so we only rebuild a TooltipInformationWindow if it differs from the last shown one.
type FSharpLanguageItemTooltipProvider() = 
    inherit Mono.TextEditor.TooltipProvider()
    
    override x.GetItem (editor, offset) =
        let extEditor = editor :?> MonoDevelop.SourceEditor.ExtensibleTextEditor 
        let docText = editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else
        let config = IdeApp.Workspace.ActiveConfiguration
        if config = null then null else
        let tyRes = 
            LanguageService.Service.GetTypedParseResult
                 (new FilePath(editor.FileName), 
                  docText, 
                  extEditor.Project, 
                  config, 
                  allowRecentTypeCheckResults=true,
                  timeout = ServiceSettings.blockingTimeout)
        Debug.WriteLine (sprintf "TooltipProvider: Getting tool tip")
        // Get tool-tip from the language service
        let tip = tyRes.GetToolTip(offset, editor.Document)
        match tip with
        | None -> null
        | Some (ToolTipText(elems),_) when elems |> List.forall (function ToolTipElementNone -> true | _ -> false) -> 
            Debug.WriteLine (sprintf "TooltipProvider: No data found")
            null
        | Some (tiptext,(col1,col2)) -> 
            Debug.WriteLine(sprintf "TooltipProvider: Got data")
            let line = editor.Document.OffsetToLineNumber offset

            let segment = new TextSegment(editor.LocationToOffset (line, col1), col2 - col1)
            TooltipItem (tiptext, segment)

    override x.CreateTooltipWindow (editor, offset, modifierState, item : Mono.TextEditor.TooltipItem) = 
            let doc = IdeApp.Workbench.ActiveDocument
            if (doc = null) then null else
            match item.Item with 
            | :? ToolTipText as titem -> 
                let tooltip = TipFormatter.formatTipWithHeader(titem)               
                let result = new TooltipInformationWindow(ShowArrow = true)
                let toolTipInfo = new TooltipInformation(SignatureMarkup = tooltip)
                result.AddOverload(toolTipInfo)
                result.RepositionWindow ()                  
                result :> Gtk.Window
            | _ -> Debug.WriteLine("** not a FSharpLocalResolveResult!"); null
    
    override x.ShowTooltipWindow (editor, offset, modifierState, mouseX, mouseY, item) =
        let tipWindow = x.CreateTooltipWindow (editor, offset, modifierState, item) :?> TooltipInformationWindow
        if tipWindow = null then null else

        let positionWidget = editor.TextArea

        let p1 = offset |> editor.OffsetToLocation |> editor.LocationToPoint
  
        //we never set the end dom region so use a single character '1' as the end offset for now otherwise it will be negative
        let caret = new Gdk.Rectangle (p1.X - positionWidget.Allocation.X, p1.Y - positionWidget.Allocation.Y, 1, int editor.LineHeight)
        tipWindow.ShowPopup(positionWidget, caret, MonoDevelop.Components.PopupPosition.Top)
        tipWindow :> Gtk.Window

    
/// Implements "resolution" - looks for tool-tips at current locations
type FSharpResolverProvider() =
  do Debug.WriteLine (sprintf "Resolver: Creating FSharpResolverProvider")
  // TODO: ITextEditorMemberPositionProvider
  // TODO: ITextEditorExtension
  // TODO: MonoDevelop.Ide.Gui.Content.CompletionTextEditorExtension (Parameter completion etc.)
  
  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:Document, offset:int, region:DomRegion byref) : ResolveResult =

      try 
        Debug.WriteLine (sprintf "Resolver: In GetLanguageItem")
        if doc.Editor = null || doc.Editor.Document = null then null else
        let docText = doc.Editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else
        let config = IdeApp.Workspace.ActiveConfiguration
        if config = null then null else

        Debug.WriteLine(sprintf "Resolver: Getting results of type checking")
        // Try to get typed result - with the specified timeout
        let tyRes = 
            LanguageService.Service.GetTypedParseResult
                 (new FilePath(doc.Editor.FileName), 
                  docText, 
                  doc.Project, 
                  config, 
                  allowRecentTypeCheckResults=true,
                  timeout = ServiceSettings.blockingTimeout)

        Debug.WriteLine("getting declaration location...")
       
        // Get the declaration location from the language service
        let loc = tyRes.GetDeclarationLocation(offset, doc.Editor.Document)
        let reg = match loc with
                  | FindDeclResult.DeclFound((line, col), file) -> 
                       Debug.WriteLine("found, line = {0}, col = {1}, file = {2}", line, col, file)
                       DomRegion(file,line+1,col+1)
                  | FindDeclResult.DeclNotFound(notfound) -> 
                       match notfound with 
                       | FindDeclFailureReason.Unknown           -> Debug.WriteLine("DeclNotFound: Unknown")
                       | FindDeclFailureReason.NoSourceCode      -> Debug.WriteLine("DeclNotFound: No Source Code")
                       | FindDeclFailureReason.ProvidedType(t)   -> Debug.WriteLine("DeclNotFound: ProvidedType")
                       | FindDeclFailureReason.ProvidedMember(m) -> Debug.WriteLine("DeclNotFound: ProvidedMember")
                       DomRegion.Empty
        region <- reg
        // This is the NRefactory symbol for the item - the Region is used for goto-definition
        let ivar = 
            { new IVariable with 
                member x.Name = "item--item"
                member x.Region = reg
                member x.Type = (SpecialType.UnknownType :> _)
                member x.IsConst = false
                member x.ConstantValue = Unchecked.defaultof<_>
              interface ISymbol with
                member x.SymbolKind = SymbolKind.Variable 
                member x.Name = "item--item"}
                
        new LocalResolveResult(ivar) :> ResolveResult
      with exn -> 
        Debug.WriteLine (sprintf "Resolver: Exception: '%s'" (exn.ToString()))
        null

    member x.GetLanguageItem(doc:Document, offset:int, identifier:string) : ResolveResult =
      do Debug.WriteLine (sprintf "Resolver: in GetLanguageItem#2")
      let (result, region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      result

    /// Returns string with tool-tip from 'FSharpLocalResolveResult'
    member x.CreateTooltip(document, offset, result, errorInformation, modifierState) = null


