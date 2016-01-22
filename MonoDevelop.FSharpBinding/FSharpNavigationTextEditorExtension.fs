namespace MonoDevelop.FSharp

open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler

type FSharpNavigationTextEditorExtension() =
    inherit AbstractNavigationExtension()

    override x.RequestLinksAsync(offset, length, token) =
        // return all links for the line at offset
        let editor = base.Editor
        let doc = base.DocumentContext

        match doc.ParsedDocument.TryGetAst () with
        | None -> Task.FromResult Seq.empty
        | Some ast ->
            let getOffset(pos: Range.pos) =
                editor.LocationToOffset (DocumentLocation(pos.Line, pos.Column + 1))
            
            let (line, col, lineStr) = editor.GetLineInfoFromOffset offset
            LoggingService.LogDebug(sprintf "%d %d %d %s" length line col lineStr)
            let tokens = Lexer.tokenizeLine lineStr [||] 0 lineStr Lexer.queryLexState
            let links =
                asyncSeq {
                    for token in tokens do
                        let! symbolUse = ast.GetSymbolAtLocation (line, token.LeftColumn, lineStr)
                        match symbolUse with
                        | Some symbol when Refactoring.Operations.canJump symbol editor.FileName doc.Project.ParentSolution -> 
                            let range = symbol.RangeAlternate
                            let startOffset = getOffset range.Start
                            let endOffset = getOffset range.End
                            LoggingService.LogDebug(symbol.Symbol.DisplayName)
                            let seg = 
                                AbstractNavigationExtension.NavigationSegment(startOffset, endOffset - startOffset, 
                                    (fun () -> Refactoring.jumpToDeclaration(editor, doc, symbol)))
                            yield seg
                        | _ -> ()
                } 

            let computation = async {
                return links |> AsyncSeq.toSeq
            }
            Async.StartAsTask(computation, cancellationToken = token)