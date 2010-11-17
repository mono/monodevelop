// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace FSharp.MonoDevelop

open System
open MonoDevelop.Ide
open MonoDevelop.Core
open MonoDevelop.Projects.Dom
open MonoDevelop.Projects.Dom.Parser
open MonoDevelop.Ide.Gui.Content
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Result of F# identifier resolution at the specified location
/// Stores the data tip that we get from the language service
/// (this is passed to MonoDevelop, which asks as about tooltip later)
type internal FSharpResolveResult(tip:DataTipText) = 
  inherit ResolveResult(ResolvedExpression = new ExpressionResult(""))
  override x.CreateResolveResult(dom:ProjectDom, memb:IMember) =
    seq { do () }
  member x.DataTip = tip


/// Implements "resolution" - looks for tool-tips at current locations
type FSharpResolverProvider() =
  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(dom:ProjectDom, data, offset) : ResolveResult =
      Debug.tracef "Gui" "[Gui] Trying to get tooltip"
      if offset >= data.Document.Text.Length || offset < 0 then null else
      try 
        Debug.tracef "Gui" "[Gui] Trying to get tooltip"
        let config = IdeApp.Workspace.ActiveConfiguration
        let req = new FilePath(data.Document.FileName), data.Document.Text, dom, config
        
        // Try to get typed result - with the specified timeout
        let tyRes = 
          LanguageService.Service.GetTypedParseResult
            (req, timeout = ServiceSettings.blockingTimeout)
            
        // Get tool-tip from the language service
        let tip = tyRes.GetToolTip(offset, data.Document)
        match tip with
        | DataTipText(elems) 
            when elems |> List.forall (function 
              DataTipElementNone -> true | _ -> false) -> 
            System.Diagnostics.Debug.WriteLine("[F#] [Gui] No data found")
            null
        | _ -> 
            new FSharpResolveResult(tip) :> ResolveResult
      with :? System.TimeoutException -> null
    
    /// Whatever this is, we don't support it!  
    member x.GetLanguageItem(dom:ProjectDom, data, offset, expression) : ResolveResult =
      null

    /// Retursn string with tool-tip from 'FSharpResolveResult'
    /// (which we generated in the previous method - so we simply run formatter)
    member x.CreateTooltip(dom:ProjectDom, unit, result, errorInformation, ambience, modifierState) : string = 
      match result with
      | :? FSharpResolveResult as res -> TipFormatter.formatTipWithHeader(res.DataTip)
      | _ -> null