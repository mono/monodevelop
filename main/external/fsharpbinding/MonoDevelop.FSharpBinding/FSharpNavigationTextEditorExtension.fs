namespace MonoDevelop.FSharp

open MonoDevelop
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

        async {
            if documentContext :? FsiDocumentContext then return Seq.empty
            else
            match documentContext.ParsedDocument |> Option.tryCast<FSharpParsedDocument> with
            | Some doc ->
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
                        let text = editor.GetTextBetween(startOffset, endOffset)
                        let lastDot = text.LastIndexOf "."
                        let correctedOffset =
                            if lastDot <> -1 then
                                startOffset + lastDot + 1
                            else
                                startOffset

                        AbstractNavigationExtension.NavigationSegment(correctedOffset, endOffset - correctedOffset,
                            (fun () -> GLib.Timeout.Add (50u, fun () -> Refactoring.jumpToDeclaration(editor, documentContext, symbol)
                                                                        false) |> ignore))
                    let filterSymbols (symbol: FSharpSymbolUse) =
                        symbol.RangeAlternate.StartLine = line

                        && Refactoring.Operations.canJump symbol editor.FileName documentContext.Project.ParentSolution

                    return doc.AllSymbolsKeyed.Values
                           |> Seq.filter filterSymbols
                           |> Seq.map segmentFromSymbol

            | None -> return Seq.empty
        }
        |> StartAsyncAsTask token

