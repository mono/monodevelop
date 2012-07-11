// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Text
open System.Linq
open System.Collections.Generic
open System.Threading

open Mono.TextEditor

open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Refactoring

open ICSharpCode.NRefactory
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.CSharp
open ICSharpCode.NRefactory.CSharp.Resolver
open ICSharpCode.NRefactory.CSharp.TypeSystem

open Microsoft.FSharp.Compiler.SourceCodeServices

open System
open Mono.TextEditor
open MonoDevelop.Ide.TypeSystem
open ICSharpCode.NRefactory.Semantics

/// Result of F# identifier resolution at the specified location
/// Stores the data tip that we get from the language service
/// (this is passed to MonoDevelop, which asks as about tooltip later)
type internal FSharpResolveResult(tip:DataTipText) = 
  inherit ResolveResult(SpecialType.UnknownType)

  member x.DataTip = tip
  
type FSharpLanguageItemTooltipProvider() = 
  let p = new MonoDevelop.SourceEditor.LanguageItemTooltipProvider() 
  interface ITooltipProvider with 
    
    member x.GetItem (editor, offset) =  p.GetItem(editor,offset)
    member x.CreateTooltipWindow (editor, offset, modifierState, item) = p.CreateTooltipWindow (editor, offset, modifierState, item)
    member x.GetRequiredPosition (editor, tipWindow, requiredWidth, xalign) = p.GetRequiredPosition (editor, tipWindow, &requiredWidth, &xalign)
    member x.IsInteractive (editor, tipWindow) =  p.IsInteractive (editor, tipWindow)
    

/// Implements "resolution" - looks for tool-tips at current locations
type FSharpResolverProvider() =
  do Debug.tracef "Resolver" "Creating FSharpResolverProvider"
  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:MonoDevelop.Ide.Gui.Document, offset:int, region:DomRegion byref) : ResolveResult =

      try 
        Debug.tracef "Resolver" "In GetLanguageItem"
        if doc.Editor = null || doc.Editor.Document = null then null else

        let docText = doc.Editor.Text

        if docText = null || offset >= docText.Length || offset < 0 then null else

        let config = IdeApp.Workspace.ActiveConfiguration

        if config = null then null else

        let req = new FilePath(doc.Editor.FileName), docText, doc.Project, config
        
        Debug.tracef "Resolver" "Getting results of type checking"
        // Try to get typed result - with the specified timeout
        let tyRes = LanguageService.Service.GetTypedParseResult(req, timeout = ServiceSettings.blockingTimeout)
            
        Debug.tracef "Resolver" "Getting tool tip"
        // Get tool-tip from the language service
        let tip = tyRes.GetToolTip(offset, doc.Editor.Document)
        match tip with
        | DataTipText(elems) when elems |> List.forall (function DataTipElementNone -> true | _ -> false) -> 
            Debug.tracef "Resolver" "No data found"
            null
        | _ -> 
            Debug.tracef "Resolver" "Got data"
            new FSharpResolveResult(tip) :> ResolveResult
      with exn -> 
        Debug.tracef "Resolver" "Exception: '%s'" (exn.ToString())
        null

    member x.GetLanguageItem(doc:MonoDevelop.Ide.Gui.Document, offset:int, identifier:string) : ResolveResult =
      do Debug.tracef "Resolver" "in GetLanguageItem#2"
      let (result, region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      result

    /// Returns string with tool-tip from 'FSharpResolveResult'
    /// (which we generated in the previous method - so we simply run formatter)
#if MONODEVELOP_3_1_OR_GREATER
    member x.CreateTooltip(document, offset, result, errorInformation, modifierState) : string = 
#else
    member x.CreateTooltip(unit, result, errorInformation, ambience, modifierState) : string = 
#endif
      do Debug.tracef "Resolver" "in CreteTooltip"
      match result with
      | :? FSharpResolveResult as res -> TipFormatter.formatTipWithHeader(res.DataTip)
      | _ -> null
