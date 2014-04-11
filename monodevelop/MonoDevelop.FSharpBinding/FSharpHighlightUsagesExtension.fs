namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Linq
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open MonoDevelop.SourceEditor.QuickTasks
open ICSharpCode.NRefactory
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.TypeSystem.Implementation
open Cairo

open Microsoft.FSharp.Compiler.SourceCodeServices

/// Used by HighlightUsagesExtension.
type UsageSegment( usageType: ReferenceUsageType, offset, length) =
    let textSegment = TextSegment (offset, length)
    member x.UsageType = usageType
    member x.TextSegment = textSegment

/// Used by HighlightUsagesExtension.
type UsageMarker() =
    inherit TextLineMarker()

    let usages = List<UsageSegment>()

    let getFromTo( editor:TextEditor,  metrics:LineMetrics,  markerStart:int,  markerEnd:int) =
        let from, too =
            if markerStart < metrics.TextStartOffset && metrics.TextEndOffset < markerEnd then
                metrics.TextRenderStartPosition, metrics.TextRenderEndPosition

            else 
                let start = if metrics.TextStartOffset < markerStart then markerStart else metrics.TextStartOffset
                let end' = if metrics.TextEndOffset < markerEnd then metrics.TextEndOffset else markerEnd
                let curIndex = ref 0u
                let byteIndex = ref 0u
                TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, uint32 (start - metrics.TextStartOffset), curIndex, byteIndex) |> ignore
                let x_pos = (metrics.Layout.Layout.IndexToPos (int !byteIndex)).X
                let from = metrics.TextRenderStartPosition + (float x_pos) / Pango.Scale.PangoScale

                TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, uint32 (end' - metrics.TextStartOffset), curIndex, byteIndex) |> ignore
                let tox_pos = (metrics.Layout.Layout.IndexToPos (int !byteIndex)).X
                let too = metrics.TextRenderStartPosition + (float tox_pos / Pango.Scale.PangoScale)
                from, too

        max from editor.TextViewMargin.XOffset, max too editor.TextViewMargin.XOffset
                
    override x.DrawBackground(editor, context, y, linemetrics) =
        //exit clauses
        if linemetrics.SelectionStart >= 0
           || editor.CurrentMode :? TextLinkEditMode 
           || editor.TextViewMargin.SearchResultMatchCount > 0
           || usages |> Seq.exists (fun u -> u.TextSegment.EndOffset < linemetrics.TextStartOffset 
                                             || u.TextSegment.Offset > linemetrics.TextEndOffset) then false else
        
        for usage in usages do    
            let markerStart = usage.TextSegment.Offset
            let markerEnd = usage.TextSegment.EndOffset
            let from, to' = getFromTo (editor, linemetrics, markerStart, markerEnd)

            if from < to' then

                let colorStyle = 
                    if usage.UsageType = ReferenceUsageType.Write then editor.ColorStyle.ChangingUsagesRectangle
                    else editor.ColorStyle.UsagesRectangle

                use linearGradient = new LinearGradient (from + 1.0, y + 1.0, to' , y + editor.LineHeight)
                linearGradient.AddColorStop(0.0, colorStyle.Color) |> ignore
                linearGradient.AddColorStop(1.0, colorStyle.SecondColor) |> ignore
                context.SetSource(linearGradient)
                context.RoundedRectangle (from + 0.5, y + 1.5, to' - from - 1.0, editor.LineHeight - 2.0, editor.LineHeight / 4.0)
                context.FillPreserve()

                context.SetSourceColor (colorStyle.BorderColor)
                context.Stroke()
        true

    member x.Usages = usages
            
    member x.Contains(offset) =
        usages.Any (fun u -> u.TextSegment.Offset <= offset && offset <= u.TextSegment.EndOffset)


/// MD/XS extension for highlighting the usages of a symbol within the current buffer.
type HighlightUsagesExtension() as this =
    inherit TextEditorExtension()
        
    let usagesUpdated = Event<_,_>()
    let mutable textEditorData = Unchecked.defaultof<TextEditorData>
    let markers = Dictionary<int, UsageMarker>()
    let popupTimer = ref 0u
    let usages = List<DocumentLocation>()
    let usagesSegments = List<UsageSegment>();
    let mutable doc = Unchecked.defaultof<Gui.Document>
    let mutable cts = new Threading.CancellationTokenSource()

    let cancelHighlight() =
        cts.Cancel()
        cts.Dispose()
        cts <- new Threading.CancellationTokenSource()

    let removeMarkers(updateLine) =
        if markers.Count <> 0 then
            textEditorData.Parent.TextViewMargin.AlphaBlendSearchResults <- false
            for kv in markers do
                textEditorData.Document.RemoveMarker (kv.Value, true)
            markers.Clear()

    let removeTimer() =
        if !popupTimer <> 0u then
            GLib.Source.Remove(!popupTimer) |> ignore
            popupTimer := 0u

    let getMarker (line) =
        match markers.TryGetValue(line) with
        | true, usageMarker -> usageMarker
        | false, _ ->  let result = UsageMarker()
                       textEditorData.Document.AddMarker(line, result)
                       markers.Add (line, result)
                       result

    //XS specific
    let showReferences(references:MemberReference seq) =
        removeMarkers (false)
        let lineNumbers =  HashSet<int> ()
        usages.Clear ()
        usagesSegments.Clear()

        let editor = textEditorData.Parent
        if editor <> null && editor.TextViewMargin <> null then
            if references <> null then
                let mutable alphaBlend = false
                for r in references do
                    let marker = getMarker(r.Region.BeginLine)
                
                    usages.Add(DocumentLocation.op_Implicit r.Region.Begin)

                    let offset = r.Offset;
                    let endOffset = offset + r.Length;
                    if (not alphaBlend && editor.TextViewMargin.SearchResults.Any (fun sr -> sr.Contains (offset) || sr.Contains (endOffset) || offset < sr.Offset && sr.EndOffset < endOffset)) then 
                        editor.TextViewMargin.AlphaBlendSearchResults <- true
                        alphaBlend <- true

                    usagesSegments.Add (UsageSegment (r.ReferenceUsageType, offset, endOffset - offset))
                    marker.Usages.Add (UsageSegment (r.ReferenceUsageType, offset, endOffset - offset))
                    lineNumbers.Add (r.Region.BeginLine) |> ignore

            for line in lineNumbers do
                textEditorData.Document.CommitLineUpdate(line)
            usagesSegments.Sort(Comparison(fun (x:UsageSegment) y -> x.TextSegment.Offset.CompareTo (y.TextSegment.Offset)))
        
        usagesUpdated.Trigger(this, EventArgs.Empty)

    let ShowHighlightAsync(projectContent, projectFilename, currentFile, source, files, line, col, lineStr, args, framework) =
        let token = cts.Token
        let asyncOperation = 
            async{let! symbolReferences =
                    MDLanguageService.Instance.GetUsesOfSymbolAtLocationInFile(projectFilename, currentFile, source, files, line, col, lineStr, args, framework)

                  match symbolReferences with
                  | Some(fsSymbolName, references) -> 
                      let memberReferences =
                          [| for symbolUse in references do
                              //We only want symbol refs from the current file as we are highlighting text
                              if symbolUse.FileName = currentFile then 
                                  yield NRefactory.createMemberReference(projectContent, symbolUse, currentFile, source, fsSymbolName) |]
                      if not token.IsCancellationRequested then
                          Gtk.Application.Invoke(fun _ _ -> showReferences(memberReferences))
                  | _ -> () }
        Async.Start(asyncOperation, token)

    let delayedHighight =
        GLib.TimeoutHandler(fun _ -> 
                                try
                                    try
                                        //find usages is based on symbols found in phsical files so a dirty file here will be invalid
                                        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(textEditorData.Caret.Offset, doc.Editor.Document)
                                        let currentFile = FilePath(textEditorData.FileName).ToString()

                                        let projectFilename, files, args, framework = MonoDevelop.getCheckerArgsFromProject(doc.Project :?> DotNetProject, IdeApp.Workspace.ActiveConfiguration)

                                        //cancel any current highlights
                                        cancelHighlight()
                                        ShowHighlightAsync(doc.ProjectContent, projectFilename, currentFile, textEditorData.Text, files, line, col, lineStr, args, framework)

                                    with exn -> LoggingService.LogError("Unhandled Exception in F# HighlightingUsagesExtension", exn)

                                finally popupTimer := 0u
                                false )

    let caretPositionChanged =
        EventHandler<_>
            (fun s dl -> let isHighlighted = 
                             SourceEditor.DefaultSourceEditorOptions.Instance.EnableHighlightUsages
                          || SourceEditor.DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting
                         let selectionContainsCaret = textEditorData.IsSomethingSelected && markers.Values.Any(fun m -> m.Contains(textEditorData.Caret.Offset))
                         if isHighlighted && (textEditorData.IsSomethingSelected || not selectionContainsCaret) then
                             removeMarkers (textEditorData.IsSomethingSelected)
                             removeTimer()
                             if not textEditorData.IsSomethingSelected then
                                 popupTimer := GLib.Timeout.Add(1000u, delayedHighight))

    let documentTextReplaced = EventHandler<_>(fun _ _ -> removeMarkers(false))
    let documentSelectionChanged = EventHandler(fun _ _ -> removeMarkers(false))

    override x.Initialize () =
        base.Initialize ()
        doc <- base.Document
        textEditorData <- base.Document.Editor
        textEditorData.Caret.PositionChanged.AddHandler caretPositionChanged
        textEditorData.Document.TextReplaced.AddHandler documentTextReplaced
        textEditorData.SelectionChanged.AddHandler documentSelectionChanged

    override x.Dispose() =
        base.Dispose ()
        textEditorData.SelectionChanged.RemoveHandler documentSelectionChanged
        textEditorData.Caret.PositionChanged.RemoveHandler caretPositionChanged
        textEditorData.Document.TextReplaced.RemoveHandler documentTextReplaced
        removeTimer()
        cancelHighlight()
    
    //These three members will be used by 'move to next reference'
    member x.IsTimerOnQueue = 
        !popupTimer <> 0u

    member x.ForceUpdate () =
        removeTimer()
        delayedHighight.Invoke()

    member x.UsagesSegments = 
        usagesSegments

    interface IUsageProvider with
        member x.Usages = usages.AsEnumerable()
        member x.add_UsagesUpdated(handler)    = usagesUpdated.Publish.AddHandler(handler)
        member x.remove_UsagesUpdated(handler) = usagesUpdated.Publish.RemoveHandler(handler)

    
