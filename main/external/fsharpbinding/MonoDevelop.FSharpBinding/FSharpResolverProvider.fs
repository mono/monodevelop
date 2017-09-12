// --------------------------------------------------------------------------------------
// Resolves locations to NRefactory Symbols and is used for Highlight usages and goto declaration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Gui.Content
open Microsoft.FSharp.Compiler.SourceCodeServices
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
                let filename =doc.FileName.FullPath.ToString()
                LoggingService.LogDebug "ResolverProvider: Getting results of type checking"
                // Try to get typed result - with the specified timeout
                let curVersion = doc.Editor.Version
                let isObsolete =
                    (fun () ->
                    let doc = IdeApp.Workbench.GetDocument(filename)
                    let newVersion = doc.Editor.Version

                    if newVersion.BelongsToSameDocumentAs(curVersion) && newVersion.CompareAge(curVersion) = 0
                    then
                        false
                    else
                        LoggingService.LogDebug ("FSharpResolverProvider: type check of {0} is obsolete, cancelled", IO.Path.GetFileName filename)
                        true )

                let results =
                    asyncMaybe {
                        let projectFile = doc.Project |> function null -> filename | project -> project.FileName.ToString()
                        let! tyRes = languageService.GetTypedParseResultWithTimeout (projectFile, filename, 0, docText, AllowStaleResults.MatchingSource, obsoleteCheck=isObsolete)
                        LoggingService.LogDebug "ResolverProvider: Getting declaration location"
                        // Get the declaration location from the language service
                        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset
                        let! fsSymbolUse = tyRes.GetSymbolAtLocation(line, col, lineStr)
                        let! findDeclarationResult = tyRes.GetDeclarationLocation(line, col, lineStr) |> Async.map Some
                        let domRegion =
                            match findDeclarationResult with
                            | FSharpFindDeclResult.DeclFound(m) ->
                                LoggingService.LogDebug("ResolverProvider: found, line = {0}, col = {1}, file = {2}", m.StartLine, m.StartColumn, m.FileName)
                                DocumentRegion(m.StartLine, m.EndLine, m.StartColumn+1, m.EndColumn+1)
                            | FSharpFindDeclResult.ExternalDecl(_assembly, _externalSymbol) ->
                                DocumentRegion()
                            | FSharpFindDeclResult.DeclNotFound(notfound) ->
                                match notfound with
                                | FSharpFindDeclFailureReason.Unknown _         -> LoggingService.LogWarning "Declaration not found: Unknown"
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

                    let roslynLocs =
                        Symbols.getTrimmedTextSpanForDeclarations lastIdent symbolUse
                        |> Seq.map (fun (fileName, ts, ls) -> Microsoft.CodeAnalysis.Location.Create(fileName, ts, ls))
                        |> System.Collections.Immutable.ImmutableArray.ToImmutableArray
                    let roslynSymbol = RoslynHelpers.FsharpSymbol (symbolUse, roslynLocs)
                    roslynSymbol :> _
                | _ -> null

            with exn ->
                LoggingService.LogError("ResolverProvider: Exception while retrieving resolve result", exn)
                null

        member x.GetLanguageItem(doc:Document, offset:int, _identifier:string): Microsoft.CodeAnalysis.ISymbol =
          LoggingService.LogDebug "ResolverProvider: GetLanguageItem"
          let (result, _region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
          result
