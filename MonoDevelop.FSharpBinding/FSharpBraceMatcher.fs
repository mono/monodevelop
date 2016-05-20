namespace MonoDevelop.FSharp

open System
open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core.Text
open MonoDevelop.Ide.Editor
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpBraceMatcher() =
    inherit AbstractBraceMatcher()
    let defaultMatcher = new DefaultBraceMatcher()

    override x.CanHandle editor =
        FileService.supportedFileName (editor.FileName.ToString())

    override x.GetMatchingBracesAsync (editor, context, caretOffset, cancellationToken) =
        if caretOffset = -1 || caretOffset >= editor.Length then
            Task.FromResult(Nullable())
        else
        let isFsi = editor.FileName.ToString() = "__FSI__.fsx"
        match editor.GetCharAt(caretOffset), isFsi with
        | '(', false
        | ')', false ->
            let computation = async {
                let getOffset (range:Range.range) =
                    editor.LocationToOffset (range.StartLine, range.StartColumn+1)

                let projectFileName =
                    if isNull context.Project then
                        editor.FileName
                    else
                        context.Project.FileName

                let! braces = languageService.MatchingBraces(context.Name, projectFileName.ToString(), editor.Text)
                let matching = 
                    braces |> Seq.choose
                                  (fun (startRange, endRange) -> 
                                      let startOffset = getOffset startRange
                                      let endOffset = getOffset endRange
                                      match (startOffset, endOffset) with
                                      | (startOffset, endOffset) when startOffset = caretOffset 
                                          -> Some (startOffset, endOffset, true)
                                      | (startOffset, endOffset) when endOffset = caretOffset
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
        | _ -> defaultMatcher.GetMatchingBracesAsync (editor, context, caretOffset, cancellationToken)
        
