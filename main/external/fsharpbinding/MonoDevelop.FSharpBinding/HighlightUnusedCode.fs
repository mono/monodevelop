namespace MonoDevelop.FSharp

open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler

module highlightUnusedCode =
    let getOffset (editor:TextEditor) (pos:Range.pos) =
        editor.LocationToOffset (pos.Line, pos.Column+1)

    let removeMarkers (editor:TextEditor) (ranges:Range.range list) =
        Runtime.RunInMainThread(fun () ->
            ranges 
            |> List.iter(fun range ->
                            let startOffset = getOffset editor range.Start
                            let markers = editor.GetTextSegmentMarkersAt startOffset
                            markers |> Seq.iter (fun m -> editor.RemoveMarker m |> ignore))) |> ignore

    let getUnusedCode (context:DocumentContext) (editor:TextEditor) =
        async {
            match context.TryGetCheckResults() with
            | Some checkResults ->
                let! opens = UnusedOpens.getUnusedOpens(checkResults, fun lineNum ->
                    let line = editor.GetLine lineNum
                    if not(isNull line) then
                        editor.GetTextAt line
                    else
                        "")
                return Some opens
            | None -> return None
        }

    let highlightUnused (editor:TextEditor) (unusedOpenRanges: Range.range list) (previousUnused: Range.range list)=
        previousUnused |> removeMarkers editor
        Runtime.RunInMainThread(fun () ->
            unusedOpenRanges |> List.iter(fun range ->
                let startOffset = getOffset editor range.Start
                let markers = editor.GetTextSegmentMarkersAt startOffset |> Seq.toList
                if markers.Length = 0 then
                    let endOffset = getOffset editor range.End

                    let segment = new Text.TextSegment(startOffset, endOffset - startOffset)
                    let marker = TextMarkerFactory.CreateGenericTextSegmentMarker(editor, TextSegmentMarkerEffect.GrayOut, segment)
                    marker.IsVisible <- true
                    editor.AddMarker(marker))) |> ignore

type HighlightUnusedCode() =
    inherit TextEditorExtension()
    let mutable previousUnused = []
    let mutable parsedSubscription = None

    override x.Initialize() =
        parsedSubscription <-
            x.DocumentContext.DocumentParsed.Subscribe
                (fun _ ->
                    async {
                        let! unused = highlightUnusedCode.getUnusedCode x.DocumentContext x.Editor
                        unused |> Option.iter(fun unused' ->
                            highlightUnusedCode.highlightUnused x.Editor unused' previousUnused
                            previousUnused <- unused')
                    } |> Async.StartAndLogException) |> Some

    override x.Dispose() =
        parsedSubscription |> Option.iter(fun sub -> sub.Dispose())