namespace MonoDevelop.FSharp

open System
open System.Threading.Tasks
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Projects
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.TypeSystem.Implementation
open Microsoft.FSharp.Compiler.SourceCodeServices

/// MD/XS extension for highlighting the usages of a symbol within the current buffer.
type HighlightUsagesExtension() =
    inherit AbstractUsagesExtension<(string * FSharpSymbolUse []) option>()

    override x.Initialize() =
        base.Initialize ()
        let syntaxMode = new FSharpSyntaxMode (x.Editor, x.DocumentContext)
        x.Editor.SemanticHighlighting <- syntaxMode

    override x.ResolveAsync (token) =
        match IdeApp.Workbench.ActiveDocument with
        | null -> Task.FromResult(None)
        | doc when doc.FileName = FilePath.Null || doc.FileName <> x.Editor.FileName || x.DocumentContext.ParsedDocument = null -> Task.FromResult(None)
        | _doc ->
            LoggingService.LogDebug("HighlightUsagesExtension: ResolveAsync starting on {0}", x.DocumentContext.Name |> IO.Path.GetFileName )
            Async.StartAsTask (
                cancellationToken = token,
                computation = async {
                try
                    let line, col, lineStr = x.Editor.GetLineInfoByCaretOffset ()
                    let currentFile = x.DocumentContext.Name
                    let source = x.Editor.Text
                    let projectFile = x.DocumentContext.Project |> function null -> currentFile | project -> project.FileName.ToString()
                    let! symbolReferences = languageService.GetUsesOfSymbolAtLocationInFile (projectFile, currentFile, 0, source, line, col, lineStr)
                    return symbolReferences
                with
                | :? TaskCanceledException -> return None
                | exn -> LoggingService.LogError("Unhandled Exception in F# HighlightingUsagesExtension", exn)
                         return None })

    override x.GetReferencesAsync(resolveResult, token) =
        let references =
            if token.IsCancellationRequested then Seq.empty else

                try
                    match resolveResult with
                    | Some(fsSymbolName, references) ->
                        LoggingService.LogDebug("HighlightUsagesExtension: GetReferences starting on {0}", x.DocumentContext.Name |> IO.Path.GetFileName)
                        //TODO: Can we use the DisplayName from the symbol rather than the last element in ident islands?
                        // If we could then we could remove the Parsing.findLongIdents in GetUsesOfSymbolAtLocationInFile.
                        references
                        |> Seq.map (fun symbolUse -> NRefactory.createMemberReference(x.Editor, symbolUse, fsSymbolName))
                    | _ -> Seq.empty

                with
                | :? TaskCanceledException -> Seq.empty
                | exn -> LoggingService.LogError("Unhandled Exception in F# HighlightingUsagesExtension", exn)
                         Seq.empty

        Task.FromResult references

    override x.Dispose () =
        x.Editor.SemanticHighlighting.Dispose()
        x.Editor.SemanticHighlighting <- null
