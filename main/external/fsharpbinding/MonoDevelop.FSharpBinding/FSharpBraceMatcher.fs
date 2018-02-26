namespace MonoDevelop.FSharp

open System
open System.Threading.Tasks
open MonoDevelop.Core.Text
open MonoDevelop.Ide.Editor
open Microsoft.FSharp.Compiler

module braceMatcher =
    let noMatch = Nullable()

    let getMatchingBraces (editor:IReadonlyTextDocument) (context:DocumentContext) caretOffset =
        async {
            let getOffset (range:Range.range) =
                editor.LocationToOffset (range.StartLine, range.StartColumn+1)

            let projectFileName =
                if isNull context.Project then
                    editor.FileName
                else
                    context.Project.FileName
            let location = editor.OffsetToLocation caretOffset
            let range = Range.mkRange (context.Name) (Range.mkPos location.Line (location.Column-1)) (Range.mkPos location.Line location.Column)
            let! braces = languageService.MatchingBraces(context.Name, projectFileName.ToString(), editor.Text)
            let matching = 
                braces 
                |> Seq.tryPick
                    (function 
                    | (startRange, endRange) when startRange = range ->
                        Some (startRange, endRange, true)
                    | (startRange, endRange) when endRange = range ->
                        Some (startRange, endRange, false)
                    | _ -> None)

            return
                match matching with
                | Some (startBrace, endBrace, isLeft) ->
                    let startOffset = getOffset startBrace
                    let endOffset = getOffset endBrace
                    Nullable(new BraceMatchingResult(new TextSegment(startOffset, 1), new TextSegment(endOffset, 1), isLeft))
                | None -> noMatch
        }

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
            braceMatcher.getMatchingBraces editor context caretOffset
            |> StartAsyncAsTask cancellationToken
        | _ -> defaultMatcher.GetMatchingBracesAsync (editor, context, caretOffset, cancellationToken)
