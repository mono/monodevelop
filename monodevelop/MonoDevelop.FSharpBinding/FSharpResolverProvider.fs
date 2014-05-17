// --------------------------------------------------------------------------------------
// Resolves locations to NRefactory Symbols and is used for Highlight usages and goto declaration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open FSharp.CompilerBinding
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open ICSharpCode.NRefactory
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Resolves locations to NRefactory symbols and ResolveResult objects.
type FSharpResolverProvider() =

  interface ITextEditorResolverProvider with
  
    /// Get tool-tip at the specified offset (from the start of the file)
    member x.GetLanguageItem(doc:Document, offset:int, region:DomRegion byref) : ResolveResult =

      try 
        LoggingService.LogInfo "ResolverProvider: In GetLanguageItem"
        if doc.Editor = null || doc.Editor.Document = null then null else
        let docText = doc.Editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else

        LoggingService.LogInfo "ResolverProvider: Getting results of type checking"
        // Try to get typed result - with the specified timeout
        let proj = doc.Project :?> MonoDevelop.Projects.DotNetProject
        let files = CompilerArguments.getSourceFiles(doc.Project.Items) |> Array.ofList
        let args = CompilerArguments.getArgumentsFromProject(proj)
        let framework = CompilerArguments.getTargetFramework(proj.TargetFramework.Id)
        let tyResOpt = 
            MDLanguageService.Instance.GetTypedParseResultWithTimeout
                 (doc.Project.FileName.ToString(),
                  doc.FileName.ToString(), 
                  docText, 
                  files, 
                  args, 
                  AllowStaleResults.MatchingSource,
                  ServiceSettings.blockingTimeout,
                  framework) |> Async.RunSynchronously

        LoggingService.LogInfo "ResolverProvider: Getting declaration location"
        match tyResOpt with
        | None -> null
        | Some tyRes ->
        // Get the declaration location from the language service
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, doc.Editor.Document)

        let fsSymbolUse = tyRes.GetSymbol(line, col, lineStr) |> Async.RunSynchronously

        match fsSymbolUse with 
        | None ->  null
        | Some fsSymbolUse ->
            let findDeclarationResult = tyRes.GetDeclarationLocation(line, col, lineStr) |> Async.RunSynchronously
            let lastIdent = 
                match FSharp.CompilerBinding.Parsing.findLongIdents(col, lineStr) with 
                | Some(_, identIsland) -> Seq.last identIsland
                | None -> ""
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
                    DomRegion.Empty
            region <- domRegion
            // This is the NRefactory symbol for the item - the Region is used for goto-definition
            let resolveResult = NRefactory.createResolveResult(doc.ProjectContent, fsSymbolUse.Symbol, lastIdent, domRegion)
            resolveResult

      with exn -> 
        LoggingService.LogError("ResolverProvider: Exception while retrieving resolve result", exn)
        null

    member x.GetLanguageItem(doc:Document, offset:int, identifier:string) : ResolveResult =
      let (result, region) = (x :> ITextEditorResolverProvider).GetLanguageItem(doc, offset)
      result

    /// Returns string with tool-tip from 'FSharpLocalResolveResult'
    member x.CreateTooltip(document, offset, result, errorInformation, modifierState) = null


