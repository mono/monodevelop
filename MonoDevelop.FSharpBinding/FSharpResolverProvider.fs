// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System;
open System.Text;
open System.Linq;
open System.Collections.Generic;
open System.Threading;

open Mono.TextEditor;

open MonoDevelop.Core;
open MonoDevelop.CSharp;
open MonoDevelop.Ide;
open MonoDevelop.Ide.Gui;
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.TypeSystem;
open MonoDevelop.Refactoring;

open ICSharpCode.NRefactory;
open ICSharpCode.NRefactory.Semantics;
open ICSharpCode.NRefactory.TypeSystem;
open ICSharpCode.NRefactory.CSharp;
open ICSharpCode.NRefactory.CSharp.Resolver;
open ICSharpCode.NRefactory.CSharp.TypeSystem;

open Microsoft.FSharp.Compiler.SourceCodeServices

/// Result of F# identifier resolution at the specified location
/// Stores the data tip that we get from the language service
/// (this is passed to MonoDevelop, which asks as about tooltip later)
type internal FSharpResolveResult(tip:DataTipText) = 
  inherit ResolveResult(SpecialType.UnknownType)

  member x.DataTip = tip
  

/// Implements "resolution" - looks for tool-tips at current locations
type FSharpResolverProvider() =
  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:MonoDevelop.Ide.Gui.Document, offset:int, region:DomRegion byref) : ResolveResult =
      let mutable dt = new DomRegion()
      if offset < 0 then dt <- DomRegion.Empty
      let loc = RefactoringService.GetCorrectResolveLocation(doc, doc.Editor.OffsetToLocation(offset))
      let (success, result:ResolveResult, node:AstNode) = doc.TryResolveAt(loc)
      if not success then
        dt <- DomRegion.Empty
        null
      else
        dt <- new DomRegion(node.StartLocation, node.EndLocation)
        result

    member x.GetLanguageItem(doc:MonoDevelop.Ide.Gui.Document, offset:int, identifier:string) : ResolveResult =
      let (result, region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      result

    /// Returns string with tool-tip from 'FSharpResolveResult'
    /// (which we generated in the previous method - so we simply run formatter)
    member x.CreateTooltip(unit, result, errorInformation, ambience, modifierState) : string = 
      // With monoDevelop head, get error that "result" has type "int"
      null
      //match result with
      //| :? FSharpResolveResult as res -> TipFormatter.formatTipWithHeader(res.DataTip)
      //| _ -> null
