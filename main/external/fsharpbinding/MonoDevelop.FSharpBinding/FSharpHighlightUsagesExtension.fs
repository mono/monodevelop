namespace MonoDevelop.FSharp

open System
open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Projects
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
            async {
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
                         return None 
            }
            |> StartAsyncAsTask token

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
                        |> Seq.map (fun symbolUse ->
                                        let start, finish = Symbol.trimSymbolRegion symbolUse fsSymbolName
                                        let startOffset = x.Editor.LocationToOffset (start.Line, start.Column+1)
                                        let endOffset = x.Editor.LocationToOffset (finish.Line, finish.Column+1)
                                        let referenceType =
                                            if symbolUse.IsFromDefinition then
                                                ReferenceUsageType.Declaration
                                            else
                                                ReferenceUsageType.Unknown
                                        new MemberReference (symbolUse, symbolUse.FileName, startOffset, endOffset-startOffset, ReferenceUsageType=referenceType))
                    | _ -> Seq.empty

                with
                | :? TaskCanceledException -> Seq.empty
                | exn -> LoggingService.LogError("Unhandled Exception in F# HighlightingUsagesExtension", exn)
                         Seq.empty

        Task.FromResult references

    override x.Dispose () =
        x.Editor.SemanticHighlighting.Dispose()
        x.Editor.SemanticHighlighting <- null
