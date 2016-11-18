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
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler
open System.Reactive.Linq
type SignatureHelpMarker(document, text, font, lineNr) =
    inherit TextLineMarker()
    static member FontScale = 0.8

    member x.Text = text

    override this.Draw(editor, g, _metrics) =
        g.SetSourceRGB(0.5, 0.5, 0.5)
        use layout = new Pango.Layout(editor.PangoContext, FontDescription=font)
        let line = editor.GetLine lineNr
        layout.SetText text
        let x = editor.ColumnToX (line, (line.GetIndentation(document).Length+1)) - editor.HAdjustment.Value + editor.TextViewMargin.XOffset + (editor.TextViewMargin.TextStartPosition |> float)
        let y = (editor.LineToY lineNr) - editor.VAdjustment.Value

        let currentPoint = g.CurrentPoint
        g.MoveTo(x, y + editor.LineHeight * (1.0 - SignatureHelpMarker.FontScale))
        g.MoveTo(x, y + 5.0)
        g.ShowLayout layout
        g.MoveTo currentPoint

    interface IExtendingTextLineMarker with
        member x.IsSpaceAbove with get() = true
        member x.GetLineHeight editor = editor.LineHeight * (1.0 + SignatureHelpMarker.FontScale)
        member x.Draw(_editor, _g, _lineNr, _lineArea) = ()

module signatureHelp =
    let getOffset (editor:TextEditor) (pos:Range.pos) =
        editor.LocationToOffset (pos.Line, pos.Column+1)

    let getUnusedOpens (markers:ConcurrentDictionary<int, SignatureHelpMarker>) (context:DocumentContext) (editor:TextEditor) (data:TextEditorData) font =
        let document = data.Document
        let ast =
            maybe {
                let! ast = context.TryGetAst()
                let! pd = context.TryGetFSharpParsedDocument()

                return ast, pd
            }
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

        ast |> Option.iter (fun (ast, pd) ->
            let symbols = pd.AllSymbolsKeyed.Values |> List.ofSeq
            let topVisibleLine = ((data.VAdjustment.Value / data.LineHeight) |> int) + 1
            let bottomVisibleLine =
                Math.Min(data.LineCount - 1,
                    topVisibleLine + ((data.VAdjustment.PageSize / data.LineHeight) |> int))

            let funs = 
                symbols
                |> List.filter(fun f -> f.RangeAlternate.StartLine >= topVisibleLine && f.RangeAlternate.EndLine <= bottomVisibleLine)
                |> List.filter(fun s -> s.IsFromDefinition)
                |> List.filter(fun s -> match s with 
                                        | SymbolUse.MemberFunctionOrValue mfv -> mfv.FullType.IsFunctionType
                                        | _ -> false)
                |> List.map(fun f -> f.RangeAlternate.StartLine, f)
                |> Map.ofList
            

            // remove any markers that are in the wrong positions
            markers.Keys
            |> Seq.filter(fun lineNumber -> lineNumber >= topVisibleLine && lineNumber <= bottomVisibleLine)
            |> Seq.iter(fun lineNr -> 
                            if not (funs.ContainsKey lineNr) then
                                document.RemoveMarker markers.[lineNr]
                                markers.TryRemove(lineNr) |> ignore)

            funs |> Map.iter(fun _l f ->
                let range = f.RangeAlternate
                let lineText = editor.GetLineText(range.StartLine)
                async {
                    let! tooltip = ast.GetToolTip(range.StartLine, range.StartColumn, lineText)
                    tooltip |> Option.iter(fun (tooltip, line) ->
                        let text = extractSignature tooltip
                        LoggingService.logDebug "Line %d - %s" line text
                        let res, marker = markers.TryGetValue range.StartLine
                        let addMarker() =
                            let newMarker = SignatureHelpMarker(document, text, font, range.StartLine)
                            markers.TryAdd (range.StartLine, newMarker) |> ignore
                            Runtime.RunInMainThread(fun () -> document.AddMarker(range.StartLine, newMarker)) |> ignore
                        if res then 
                            if marker.Text <> text then
                                document.RemoveMarker marker
                                markers.TryRemove(range.StartLine) |> ignore
                                addMarker()
                        else
                            addMarker()) 
                } |> Async.StartImmediate))

type SignatureHelp() =
    inherit TextEditorExtension()
    let mutable disposable = None : IDisposable option

    let throttle (due:TimeSpan) observable =
        Observable.Throttle(observable, due)

    override x.Initialize() =
        let data = x.Editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()

        let markers = ConcurrentDictionary<int, SignatureHelpMarker>()
        let editorFont = data.Options.Font

        let font = new Pango.FontDescription(AbsoluteSize=float(editorFont.Size) * SignatureHelpMarker.FontScale, Family=editorFont.Family)

        disposable <-
            Some
                (Observable.merge x.Editor.VAdjustmentChanged x.DocumentContext.DocumentParsed
                |> throttle (TimeSpan.FromMilliseconds 350.)
                |> Observable.subscribe (fun _ -> signatureHelp.getUnusedOpens markers x.DocumentContext x.Editor data font))

    override x.Dispose() = disposable |> Option.iter (fun en -> en.Dispose ())