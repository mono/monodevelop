// --------------------------------------------------------------------------------------
// Resolves locations to NRefactory Symbols and is used for Highlight usages and goto declaration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Gui.Content
open ICSharpCode.NRefactory
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding
open ExtCore.Control
open System.Collections.Immutable

///Barebones symbol
type FsharpSymbol (symbolUse:FSharpSymbolUse, editor:TextEditor) =
    interface Microsoft.CodeAnalysis.ISymbol with
        member x.Kind = Microsoft.CodeAnalysis.SymbolKind.Local
        member x.Language = "F#"
        member x.Name = symbolUse.Symbol.DisplayName
        member x.MetadataName = symbolUse.Symbol.FullName
        member x.ContainingSymbol = null //TODO
        member x.ContainingAssembly = null //TODO
        member x.ContainingModule = null //TODO
        member x.ContainingType = null ////TODO for entities or functions this will be available
        member x.ContainingNamespace = null //TODO
        member x.IsDefinition = symbolUse.IsFromDefinition
        member x.IsStatic = false //TODO
        member x.IsVirtual = false //TODO
        member x.IsOverride = false //TODO 
        member x.IsAbstract = false //TODO
        member x.IsSealed = false //TODO
        member x.IsExtern = false //TODO
        member x.IsImplicitlyDeclared = false //TODO
        member x.CanBeReferencedByName = true //TODO
        member x.Locations =
            let start = symbolUse.RangeAlternate.Start
            let finish = symbolUse.RangeAlternate.End
            let startOffset = editor.LocationToOffset (DocumentLocation (start.Line, start.Column))
            let endOffset = editor.LocationToOffset (DocumentLocation (finish.Line, finish.Column))
            ImmutableArray.ToImmutableArray
                [Microsoft.CodeAnalysis.Location.Create (null, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (startOffset, endOffset))]
        member x.DeclaringSyntaxReferences = ImmutableArray.Empty //TODO
        member x.GetAttributes () = ImmutableArray.Empty //TODO 
        member x.DeclaredAccessibility = Microsoft.CodeAnalysis.Accessibility.NotApplicable //TOdo
        member x.OriginalDefinition = null //TODO
        member x.Accept (visitor:Microsoft.CodeAnalysis.SymbolVisitor) = () //TODO
        member x.Accept<'a> (visitor: Microsoft.CodeAnalysis.SymbolVisitor<'a>) = Unchecked.defaultof<'a> 
        member x.GetDocumentationCommentId () = "" //TODO
        member x.GetDocumentationCommentXml (culture, expand, token) = "" //TODO
        member x.ToDisplayString format = symbolUse.Symbol.DisplayName //TODO format?
        member x.ToDisplayParts format = ImmutableArray.Empty //TODO
        member x.ToMinimalDisplayString (semanticModel, position, format) = "" //TODO
        member x.ToMinimalDisplayParts (semanticModel, position, format) = ImmutableArray.Empty //TODO
        member x.HasUnsupportedMetadata = false //TODO
        member x.Equals (other:Microsoft.CodeAnalysis.ISymbol) = x.Equals(other) 

/// Resolves locations to NRefactory symbols and ResolveResult objects.
type FSharpResolverProvider() =

  interface ITextEditorResolverProvider with
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:Document, offset:int, region:DocumentRegion byref): Microsoft.CodeAnalysis.ISymbol =

      try
        if doc.Editor = null then null else
        let docText = doc.Editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else

        LoggingService.LogInfo "ResolverProvider: Getting results of type checking"
        // Try to get typed result - with the specified timeout
        let projFile, files, args = MonoDevelop.getCheckerArgs(doc.Project, doc.FileName.FullPath.ToString())

        let results =
            asyncMaybe {
                let! tyRes = MDLanguageService.Instance.GetTypedParseResultAsync (projFile, doc.FileName.FullPath.ToString(), docText, files, args, AllowStaleResults.MatchingSource) |> AsyncMaybe.liftAsync
                LoggingService.LogInfo "ResolverProvider: Getting declaration location"
                // Get the declaration location from the language service
                let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, doc.Editor)
                let! fsSymbolUse = tyRes.GetSymbolAtLocation(line, col, lineStr)
                let! findDeclarationResult = tyRes.GetDeclarationLocation(line, col, lineStr) |> AsyncMaybe.liftAsync
                let domRegion =
                    match findDeclarationResult with
                    | FSharpFindDeclResult.DeclFound(m) ->
                        LoggingService.LogInfo("ResolverProvider: found, line = {0}, col = {1}, file = {2}", m.StartLine, m.StartColumn, m.FileName)
                        DocumentRegion(m.StartLine, m.EndLine, m.StartColumn+1, m.EndColumn+1)
                    | FSharpFindDeclResult.DeclNotFound(notfound) ->
                        match notfound with
                        | FSharpFindDeclFailureReason.Unknown           -> LoggingService.LogWarning "Declaration not found: Unknown"
                        | FSharpFindDeclFailureReason.NoSourceCode      -> LoggingService.LogWarning "Declaration not found: No Source Code"
                        | FSharpFindDeclFailureReason.ProvidedType(t)   -> LoggingService.LogWarning("Declaration not found: ProvidedType {0}", t)
                        | FSharpFindDeclFailureReason.ProvidedMember(m) -> LoggingService.LogWarning("Declaration not found: ProvidedMember {0}", m)
                        DocumentRegion ()               

                // This is the NRefactory symbol for the item - the Region is used for goto-definition
                let lastIdent = Symbols.lastIdent col lineStr

                return fsSymbolUse, lastIdent, domRegion }
        match Async.RunSynchronously (results, ServiceSettings.blockingTimeout) with
        | Some (symbolUse, lastIdent, dom) ->
            region <- dom
            let iSymbol = FsharpSymbol (symbolUse, doc.Editor)
            iSymbol :> _
        | _ -> null

      with exn ->
        LoggingService.LogError("ResolverProvider: Exception while retrieving resolve result", exn)
        null

    member x.GetLanguageItem(doc:Document, offset:int, _identifier:string): Microsoft.CodeAnalysis.ISymbol =
      let (result, _region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      //TODO result to ISymbol??
      null

    /// Returns string with tool-tip from 'FSharpLocalResolveResult'
    //member x.CreateTooltip(_document, _offset, _result, _errorInformation, _modifierState) = null
