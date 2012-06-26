// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace FSharp.MonoDevelop

open System
open MonoDevelop.Ide
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.Gui
open Microsoft.FSharp.Compiler.SourceCodeServices

open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem

/// Result of F# identifier resolution at the specified location
/// Stores the data tip that we get from the language service
/// (this is passed to MonoDevelop, which asks as about tooltip later)
type internal FSharpResolveResult(tip:DataTipText) = 
  inherit ResolveResult(SpecialType.UnknownType)  

  // override x.GetDefinitionRegion(dom:DomRegion, memb:IMember) =
  //   seq { do () }

  member x.DataTip = tip
  

/// Implements "resolution" - looks for tool-tips at current locations
type FSharpResolverProvider() =
  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    //member x.GetLanguageItem(dom:Document, data:DomRegion, offset) : ResolveResult =
    //  if offset >= data.End.Line || offset < 0 then null
    //  let loc = null 
      
    
    /// Whatever this is, we don't support it!  
    member x.GetLanguageItem(dom:Document, offset:int, expressionRegion:DomRegion byref) : ResolveResult =
      null

    member x.GetLanguageItem(dom:Document, offset:int, identifier:string) : ResolveResult =
      null

    /// Returns string with tool-tip from 'FSharpResolveResult'
    /// (which we generated in the previous method - so we simply run formatter)
    member x.CreateTooltip(unit:IParsedFile, result:ResolveResult, errorInformation, ambience, modifierState) : string = 
      match result with
      | :? FSharpResolveResult as res -> TipFormatter.formatTipWithHeader(res.DataTip)
      | _ -> null
