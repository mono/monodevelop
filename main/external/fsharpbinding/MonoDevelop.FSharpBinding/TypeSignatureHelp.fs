namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Collections.Concurrent
open ExtCore.Control
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Components
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type SignatureHelpMarker(document, text, font, line) =
    inherit TextLineMarker()
    let mutable text' = text
    let mutable font' = font
    static let tag = obj()
    static member FontScale = 0.8
    member x.Text with get() = text' and set(value) = text' <- value
    // Font size can increase with zoom
    member x.Font with get() = font' and set(value) = font' <- value

    interface ITextLineMarker with
        member x.Line with get() = line
        member x.IsVisible with get() = true and set(_value) = ()
        member x.Tag with get() = tag and set(_value) = ()

    interface IExtendingTextLineMarker with
        member x.IsSpaceAbove with get() = true
        member x.GetLineHeight editor = editor.LineHeight * (1.0 + SignatureHelpMarker.FontScale)
        member x.Draw(editor, g, lineNr, _lineArea) =
            g.SetSourceRGB(0.5, 0.5, 0.5)
            let line = editor.GetLine lineNr

            use layout = new Pango.Layout(editor.PangoContext, FontDescription=font')
            layout.SetText text'
            let x = editor.ColumnToX (line, (line.GetIndentation(document).Length+1)) - editor.HAdjustment.Value + editor.TextViewMargin.XOffset + (editor.TextViewMargin.TextStartPosition |> float)
            let y = (editor.LineToY lineNr) - editor.VAdjustment.Value

            let currentPoint = g.CurrentPoint
            g.MoveTo(x, y + editor.LineHeight * (1.0 - SignatureHelpMarker.FontScale))
            g.ShowLayout layout
            g.MoveTo currentPoint

module signatureHelp =
    let getOffset (editor:TextEditor) (pos:Range.pos) =
        editor.LocationToOffset (pos.Line, pos.Column+1)

    let runInMainThread f = Runtime.RunInMainThread(fun () -> f() |> ignore) |> ignore
    let removeMarkers (editor:TextEditor) line =
        let markers = editor.GetLineMarkers line |> List.ofSeq
        markers
        |> List.iter(fun m -> Runtime.RunInMainThread(fun() -> editor.RemoveMarker m) |> ignore)
        markers.Length > 0

    let extractSignature (FSharpToolTipText tips) =
        let getSignature (str: string) =
            let nlpos = str.IndexOfAny([|'\r';'\n'|])
            let firstLine =
                if nlpos > 0 then str.[0..nlpos-1]
                else str
            let res = 
                if firstLine.StartsWith("type ", StringComparison.Ordinal) then
                    let index = firstLine.LastIndexOf("=", StringComparison.Ordinal)
                    if index > 0 then firstLine.[0..index-1]
                    else firstLine
                else firstLine
            let index = res.IndexOf ": "
            res.[index+2..]

        let firstResult x =
            match x with
            | FSharpToolTipElement.Single (t, _) when not (String.IsNullOrWhiteSpace t) -> Some t
            | FSharpToolTipElement.Group gs -> List.tryPick (fun (t, _) -> if not (String.IsNullOrWhiteSpace t) then Some t else None) gs
            | _ -> None

        tips
        |> Seq.sortBy (function FSharpToolTipElement.Single _ -> 0 | _ -> 1)
        |> Seq.tryPick firstResult
        |> Option.map getSignature
        |> Option.fill ""

    let displaySignatures (context:DocumentContext) (editor:TextEditor) =
        let data = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
        let editorFont = data.Options.Font
        let font = new Pango.FontDescription(AbsoluteSize=float(editorFont.Size) * SignatureHelpMarker.FontScale, Family=editorFont.Family)
        let document = data.Document

        let ast =
            maybe {
                let! ast = context.TryGetAst()
                let! pd = context.TryGetFSharpParsedDocument()

                return ast, pd
            }

        ast |> Option.iter (fun (ast, pd) ->
          if not pd.HasErrors && pd.AllSymbolsKeyed.Count > 0 then
            let symbols = pd.AllSymbolsKeyed.Values |> List.ofSeq
            let topVisibleLine = data.HeightTree.YToLineNumber data.VAdjustment.Value
            let bottomVisibleLine = 
                Math.Min(data.LineCount - 1,
                    data.HeightTree.YToLineNumber (data.VAdjustment.Value+data.VAdjustment.PageSize))

            let funs =
                symbols
                |> List.filter(fun s -> s.IsFromDefinition)
                |> List.filter(fun s -> match s with 
                                        | SymbolUse.MemberFunctionOrValue mfv -> mfv.FullType.IsFunctionType
                                        | _ -> false)
                |> List.map(fun f -> f.RangeAlternate.StartLine, f)
                |> Map.ofList

            // remove any markers that are in the wrong positions
            let lineNumbersWithMarkersToRemove =
                [topVisibleLine..bottomVisibleLine] |> List.filter(not << funs.ContainsKey)

            let removedAny =
                lineNumbersWithMarkersToRemove
                |> List.fold(fun state lineNr ->
                                let line = editor.GetLine lineNr
                                state || removeMarkers editor line) false

            if removedAny then
                runInMainThread (fun() -> document.CommitMultipleLineUpdate(topVisibleLine, bottomVisibleLine))

            let addMarker text (lineNr:int) line =
                let newMarker = SignatureHelpMarker(document, text, font, line)
                document.AddMarker(lineNr, newMarker)
                newMarker

            funs |> Map.iter(fun _l f ->
                let range = f.RangeAlternate

                let lineOption = editor.GetLine range.StartLine |> Option.ofObj
                lineOption |> Option.iter(fun line ->
                    let marker = editor.GetLineMarkers line |> Seq.tryPick(Option.tryCast<SignatureHelpMarker>)

                    let marker = marker |> Option.getOrElse(fun() -> addMarker "" range.StartLine line)
                    if range.StartLine >= topVisibleLine && range.EndLine <= bottomVisibleLine then
                        async {
                            let lineText = editor.GetLineText(range.StartLine)
                            let! tooltip = ast.GetToolTip(range.StartLine, range.StartColumn, lineText)
                            tooltip |> Option.iter(fun (tooltip, lineNr) ->
                                let text = extractSignature tooltip
                                marker.Text <- text
                                marker.Font <- font
                                document.CommitLineUpdate lineNr)
                        } |> Async.StartImmediate)))

type SignatureHelp() as x =
    inherit TextEditorExtension()
    let mutable disposables = None : IDisposable list option

    let removeAllMarkers() =
        async {
            let editor = x.Editor
            editor.GetLines() |> Seq.iter(signatureHelp.removeMarkers editor >> ignore)
            let editorData = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
            editorData.Document.CommitUpdateAll()
        } |> Async.StartImmediate
        
    override x.Initialize() =
        let displaySignatures dueMs observable =
            observable
            |> Observable.filter(fun _ -> PropertyService.Get(Settings.showTypeSignatures, true))
            |> Observable.throttle (TimeSpan.FromMilliseconds dueMs)
            |> Observable.subscribe (fun _ -> signatureHelp.displaySignatures x.DocumentContext x.Editor)

        disposables <-
            Some
                [x.Editor.VAdjustmentChanged
                |> Observable.merge x.Editor.ZoomLevelChanged
                |> displaySignatures 100.
                ;
                x.DocumentContext.DocumentParsed
                |> displaySignatures 1000.
                ;
                PropertyService.PropertyChanged
                    .Subscribe(fun p -> if p.Key = Settings.showTypeSignatures && (not (p.NewValue :?> bool)) then
                                            removeAllMarkers())]

    override x.Dispose() = disposables |> Option.iter (fun disps -> disps |> List.iter(fun disp -> disp.Dispose()))