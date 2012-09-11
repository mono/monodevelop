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
  

// In Mono 3.0.4, this change https://github.com/mono/monodevelop/commit/4a95275cf7d0418250d87844550934b849b1153d
// appears to have busted all tooltips in editor extensions?  The call to CreateTooltip in 
//     main/src/addins/MonoDevelop.SourceEditor2/MonoDevelop.SourceEditor/LanguageItemWindow.cs
// was commented out in that change, but this means CreateTooltip is never called and no tooltips 
// are ever created. So instead we just create the Gtk window ourselves here.
#if CODE_BEFORE_WORKAROUND_FOR_MISSING_MONODEVELOP_TOOLTIPS_IN_3_0_4
type FSharpLanguageItemTooltipProvider() = 
    let p = new MonoDevelop.SourceEditor.LanguageItemTooltipProvider() 
    interface ITooltipProvider with 
    
        member x.GetItem (editor, offset) =  p.GetItem(editor,offset)
        member x.CreateTooltipWindow (editor, offset, modifierState, item) = p.CreateTooltipWindow (editor, offset, modifierState, item)
        member x.GetRequiredPosition (editor, tipWindow, requiredWidth, xalign) = p.GetRequiredPosition (editor, tipWindow, &requiredWidth, &xalign)
        member x.IsInteractive (editor, tipWindow) =  p.IsInteractive (editor, tipWindow)
#else

[<AllowNullLiteral>] 
type internal FSharpLanguageItemWindow(tooltip: string) as this = 
    inherit MonoDevelop.Components.TooltipWindow() 
    let isEmpty = System.String.IsNullOrEmpty (tooltip)|| tooltip = "?"
      
    let label = 
       if isEmpty then null else 
       new MonoDevelop.Components.FixedWidthWrapLabel (Wrap = Pango.WrapMode.WordChar,Indent = -20,BreakOnCamelCasing = true,BreakOnPunctuation = true,Markup = tooltip)

    let updateFont (label:MonoDevelop.Components.FixedWidthWrapLabel) =
      if (label <> null) then
          label.FontDescription <- MonoDevelop.Ide.Fonts.FontService.GetFontDescription ("LanguageTooltips")
        
    do 
        if not isEmpty then 
          this.BorderWidth <- 3u
          this.Add label
          updateFont label
          this.EnableTransparencyControl <- true
      
    //return the real width
    member this.SetMaxWidth (maxWidth) =
      let label = this.Child :?> MonoDevelop.Components.FixedWidthWrapLabel
      if (label = null) then 
          this.Allocation.Width 
      else
          label.MaxWidth <- maxWidth
          label.RealWidth

    override this.OnStyleSet (previous_style: Gtk.Style) =
      base.OnStyleSet (previous_style)
      updateFont (this.Child :?> MonoDevelop.Components.FixedWidthWrapLabel)

type FSharpLanguageItemTooltipProvider() = 
    let p = new MonoDevelop.SourceEditor.LanguageItemTooltipProvider() 
    interface ITooltipProvider with 
    
        member x.GetItem (editor, offset) =  p.GetItem(editor,offset)

        member x.CreateTooltipWindow (editor, offset, modifierState, item) = 
            let doc = IdeApp.Workbench.ActiveDocument
            if (doc = null) then null else
            let titem = item.Item :?> FSharpResolveResult
            let tooltip = TipFormatter.formatTipWithHeader(titem.DataTip) 
            let result = new FSharpLanguageItemWindow (tooltip)
            result :> Gtk.Window
    
        member x.GetRequiredPosition (editor, tipWindow, requiredWidth, xalign) = 
            let win = tipWindow :?> FSharpLanguageItemWindow
            requiredWidth <- win.SetMaxWidth (win.Screen.Width)
            xalign <- 0.5

        member x.IsInteractive (editor, tipWindow) =  
            false
#endif
    
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
    member x.CreateTooltip(document, offset, result, errorInformation, modifierState) : string = 
      do Debug.tracef "Resolver" "in CreteTooltip"
      match result with
      | :? FSharpResolveResult as res -> TipFormatter.formatTipWithHeader(res.DataTip)
      | _ -> null

