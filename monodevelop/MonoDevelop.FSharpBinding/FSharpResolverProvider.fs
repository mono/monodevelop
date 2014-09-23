// --------------------------------------------------------------------------------------
// Resolves locations to NRefactory Symbols and is used for Highlight usages and goto declaration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
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
    member x.GetLanguageItem(doc:Document, offset:int, region:DomRegion byref) : ResolveResult =

      try
        if doc.Editor = null || doc.Editor.Document = null then null else
        let docText = doc.Editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else

        LoggingService.LogInfo "ResolverProvider: Getting results of type checking"
        // Try to get typed result - with the specified timeout
        let projFile, files, args, framework = MonoDevelop.getCheckerArgs(doc.Project, doc.FileName.FullPath.ToString())

        let results =
            asyncMaybe {
                let! tyRes = MDLanguageService.Instance.GetTypedParseResultWithTimeout (projFile, doc.FileName.FullPath.ToString(), docText, files, args, AllowStaleResults.MatchingSource, ServiceSettings.blockingTimeout, framework)
                LoggingService.LogInfo "ResolverProvider: Getting declaration location"
                // Get the declaration location from the language service
                let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, doc.Editor.Document)
                let! fsSymbolUse = tyRes.GetSymbol(line, col, lineStr)
                let! findDeclarationResult = tyRes.GetDeclarationLocation(line, col, lineStr) |> Async.map Some
                let domRegion =
                    match findDeclarationResult with
                    | FindDeclResult.DeclFound(m) ->
                        LoggingService.LogInfo("ResolverProvider: found, line = {0}, col = {1}, file = {2}", m.StartLine, m.StartColumn, m.FileName)
                        DomRegion(m.FileName,m.StartLine,m.StartColumn+1)
                    | FindDeclResult.DeclNotFound(notfound) ->
                        match notfound with
                        | FindDeclFailureReason.Unknown           -> LoggingService.LogWarning "Declaration not found: Unknown"
                        | FindDeclFailureReason.NoSourceCode      -> LoggingService.LogWarning "Declaration not found: No Source Code"
                        | FindDeclFailureReason.ProvidedType(t)   -> LoggingService.LogWarning("Declaration not found: ProvidedType {0}", t)
                        | FindDeclFailureReason.ProvidedMember(m) -> LoggingService.LogWarning("Declaration not found: ProvidedMember {0}", m)
                        match fsSymbolUse.Symbol.Assembly.FileName with
                        | None -> DomRegion.Empty
                        | Some filename -> DomRegion(filename, 0, 0)

                // This is the NRefactory symbol for the item - the Region is used for goto-definition
                let lastIdent = match FSharp.CompilerBinding.Parsing.findLongIdents(col, lineStr) with
                                | Some(_, identIsland) -> Seq.last identIsland
                                | None -> ""
                let resolveResult = NRefactory.createResolveResult(doc.ProjectContent, fsSymbolUse.Symbol, lastIdent, domRegion)
                return resolveResult, domRegion }
        match results |> Async.RunSynchronously with
        | Some (res, dom) ->
            region <- dom
            res
        | _ -> null

      with exn ->
        LoggingService.LogError("ResolverProvider: Exception while retrieving resolve result", exn)
        null

    member x.GetLanguageItem(doc:Document, offset:int, identifier:string) : ResolveResult =
      let (result, region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      result

    /// Returns string with tool-tip from 'FSharpLocalResolveResult'
    member x.CreateTooltip(document, offset, result, errorInformation, modifierState) = null
