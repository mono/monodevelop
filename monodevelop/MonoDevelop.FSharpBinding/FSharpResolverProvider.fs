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
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding
open ExtCore.Control

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
                let resolveResult = ""//TODO NRefactory.createResolveResult(doc, fsSymbolUse.Symbol, lastIdent, domRegion)
                return resolveResult, domRegion }
        match Async.RunSynchronously (results, ServiceSettings.blockingTimeout) with
        | Some (res, dom) ->
            region <- dom
            //res
            null
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
