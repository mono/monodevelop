namespace MonoDevelop.FSharp
open MonoDevelop
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension

type FSharpNavigationTextEditorExtension() =
    inherit AbstractNavigationExtension()

    override x.RequestLinksAsync(offset, length, token) =
        let editor = base.Editor
        let doc = base.DocumentContext
        let y =
            async {
                match doc.ParsedDocument.TryGetAst () with
                | None -> return Seq.empty
                | Some ast ->
                    match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                    | (_line, col, lineTxt), Some symbolUse when Refactoring.Operations.canJump symbolUse editor.FileName doc.Project.ParentSolution ->
                        let offset = editor.LocationToOffset (DocumentLocation(symbolUse.RangeAlternate.StartLine, symbolUse.RangeAlternate.StartColumn))
                        let seg = AbstractNavigationExtension.NavigationSegment(offset, length, (fun x -> ()))
                        return seq { yield seg }
                    | _ -> return Seq.empty
            }
        y |> Async.StartAsTask
