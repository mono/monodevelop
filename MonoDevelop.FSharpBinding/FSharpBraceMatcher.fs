namespace MonoDevelop.FSharp

open System
open MonoDevelop
open MonoDevelop.Core.Text
open MonoDevelop.Ide.Editor
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpBraceMatcher() =
    inherit AbstractBraceMatcher()
    let defaultMatcher = new DefaultBraceMatcher()

    override x.CanHandle editor =
        MDLanguageService.SupportedFileName (editor.FileName.ToString())

    override x.GetMatchingBracesAsync (editor, context, caretOffset, cancellationToken) =
        let offset = Math.Min(caretOffset, editor.Text.Length - 1)
        match editor.GetCharAt(offset) with
        | '(' | ')' ->
            match context.TryGetAst() with
            | Some _ast -> 
                let computation = async {
                    let getOffset (range:Range.range) =
                        editor.LocationToOffset (range.StartLine, range.StartColumn+1)

                    let! braces = languageService.MatchingBraces(editor.FileName.ToString(), context.Project.FileName.ToString(), editor.Text)
                    let matching = 
                        braces |> Seq.choose
                                      (fun (startRange, endRange) -> 
                                          let startOffset = getOffset startRange
                                          let endOffset = getOffset endRange
                                          match (startOffset, endOffset) with
                                          | (startOffset, endOffset) when startOffset = offset 
                                              -> Some (startOffset, endOffset, true)
                                          | (startOffset, endOffset) when endOffset = offset
                                              -> Some (startOffset, endOffset, false)
                                          | _ -> None)
                               |> Seq.tryHead

                    return
                        match matching with
                        | Some (startBrace, endBrace, isLeft) -> 
                            Nullable(new BraceMatchingResult(new TextSegment(startBrace, 1), new TextSegment(endBrace, 1), isLeft))
                        | None -> Nullable()

                }
                Async.StartAsTask (computation = computation, cancellationToken = cancellationToken)
            | None -> defaultMatcher.GetMatchingBracesAsync (editor, context, caretOffset, cancellationToken)
        | _ -> defaultMatcher.GetMatchingBracesAsync (editor, context, caretOffset, cancellationToken)
        
