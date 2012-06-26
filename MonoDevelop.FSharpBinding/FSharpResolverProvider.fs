// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System;
open MonoDevelop.Ide.Gui.Content;
open MonoDevelop.Core;
open Mono.TextEditor;
open System.Text;
open MonoDevelop.Ide.Gui;
open MonoDevelop.Ide;
open ICSharpCode.NRefactory.CSharp.Resolver;
open ICSharpCode.NRefactory.TypeSystem;
open MonoDevelop.Ide.TypeSystem;
open ICSharpCode.NRefactory.CSharp;
open System.Linq;
open System.Collections.Generic;
open ICSharpCode.NRefactory.Semantics;
open ICSharpCode.NRefactory;
open ICSharpCode.NRefactory.CSharp.TypeSystem;
open System.Threading;
open MonoDevelop.Refactoring;

/// Result of F# identifier resolution at the specified location
/// Stores the data tip that we get from the language service
/// (this is passed to MonoDevelop, which asks as about tooltip later)
//type internal FSharpResolveResult(tip:DataTipText) = 
//  inherit ResolveResult(new ResolveAtLocation().ResolveAtLocation()  
//
//  override x.GetDefinitionRegion(dom:DomRegion, memb:IMember) =
//    seq { do () }
//  member x.DataTip = tip
  

/// Implements "resolution" - looks for tool-tips at current locations
type FSharpResolverProvider() =
  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:MonoDevelop.Ide.Gui.Document, region:DomRegion, offset) : ResolveResult =
      let mutable dt = new DomRegion()
      if offset < 0 then dt <- DomRegion.Empty
      let loc = RefactoringService.GetCorrectResolveLocation(doc, doc.Editor.OffsetToLocation(offset))
      let (success, result, node) = doc.TryResolveAt(loc)
      if not success then region <- DomRegion.Empty
      null
      
    /// Whatever this is, we don't support it!  
    member x.GetLanguageItem(dom:Document, data, offset, expression) : ResolveResult =
      null

    /// Returns string with tool-tip from 'FSharpResolveResult'
    /// (which we generated in the previous method - so we simply run formatter)
    member x.CreateTooltip(dom:Document, unit, result, errorInformation, ambience, modifierState) : string = 
      match result with
      | :? FSharpResolveResult as res -> TipFormatter.formatTipWithHeader(res.DataTip)
      | _ -> null