namespace MonoDevelop.FSharp

open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpNavigationTextEditorExtension() =
    inherit AbstractNavigationExtension()

    override x.RequestLinksAsync(offset, _length, token) =
        // return all links for the line at offset
        let editor = base.Editor
        let documentContext = base.DocumentContext

        let computation = async {
            let doc = documentContext.ParsedDocument :?> FSharpParsedDocument
            match doc.TryGetAst () with
            | None -> return Seq.empty
            | Some _ast ->
                let getOffset(pos: Range.pos) =
                    editor.LocationToOffset (DocumentLocation(pos.Line, pos.Column + 1))
                
                let line = editor.OffsetToLineNumber offset

                let segmentFromSymbol (symbol: FSharpSymbolUse) =
                    let range = symbol.RangeAlternate
                    let startOffset = getOffset range.Start
                    let endOffset = getOffset range.End
                     
                    AbstractNavigationExtension.NavigationSegment(startOffset, endOffset - startOffset, 
                        (fun () -> Refactoring.jumpToDeclaration(editor, documentContext, symbol)))

                let filterSymbols (symbol: FSharpSymbolUse) =
                    symbol.RangeAlternate.StartLine = line
                    && Refactoring.Operations.canJump symbol editor.FileName documentContext.Project.ParentSolution

                return doc.AllSymbolsKeyed.Values
                       |> Seq.filter filterSymbols
                       |> Seq.map segmentFromSymbol
        }

        Async.StartAsTask(computation, cancellationToken = token)