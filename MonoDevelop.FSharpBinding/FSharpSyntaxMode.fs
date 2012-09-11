namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Ide
open MonoDevelop.Core
//open MonoDevelop.Projects.Dom.Parser

open Mono.TextEditor
open Mono.TextEditor.Highlighting
   
/// Implements syntax highlighting for F# sources
/// Currently, this just loads the keyword-based highlighting info from resources
type FSharpSyntaxMode() as this =
  inherit SyntaxMode()
  
  do
    let provider = new ResourceXmlProvider(typeof<FSharpSyntaxMode>.Assembly, "FSharpSyntaxMode.xml");
    use reader = provider.Open()
    let baseMode = SyntaxMode.Read(reader)
    this.rules <- new ResizeArray<_>(baseMode.Rules)
    this.keywords <- new ResizeArray<_>(baseMode.Keywords)
    this.spans <- baseMode.Spans
    this.matches <- baseMode.Matches
    this.prevMarker <- baseMode.PrevMarker
    this.SemanticRules <- new ResizeArray<_>(baseMode.SemanticRules)
    this.keywordTable <- baseMode.keywordTable
    this.properties <- baseMode.Properties

  // Do we need this? Or can we create "chunker"?
  //   
  //  override x.CreateSpanParser(doc:Document, mode:SyntaxMode, line:LineSegment, spanStack:Stack<Span>) : SyntaxMode.SpanParser =
  //    base.CreateSpanParser(doc, mode, line, spanStack)
    
 
