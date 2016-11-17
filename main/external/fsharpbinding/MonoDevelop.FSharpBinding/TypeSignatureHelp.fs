namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open ExtCore.Control
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Components
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler

type SignatureHelpMarker(document, text, font, lineNr) =
    inherit TextLineMarker()
    static member FontScale = 0.8

    override this.Draw(editor, g, _metrics) =
        g.SetSourceRGB(0.5, 0.5, 0.5)
        use layout = new Pango.Layout(editor.PangoContext, FontDescription=font)
        let line = editor.GetLine lineNr
        layout.SetText text
        //let location = editor.OffsetToLocation offset
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

    let getUnusedOpens (context:DocumentContext) (editor:TextEditor) =
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

        ast |> Option.bind (fun (ast, pd) ->
            let symbols = pd.AllSymbolsKeyed.Values
            let funs = symbols |> Seq.filter(fun s -> s.IsFromDefinition) |> List.ofSeq
            let funs = funs |> List.filter(fun s -> match s with 
                                                    | SymbolUse.MemberFunctionOrValue mfv -> mfv.FullType.IsFunctionType
                                                    | _ -> false)
            let res =
                funs |> List.map(fun f ->
                    let range = f.RangeAlternate
                    let lineText = editor.GetLineText(range.StartLine)
                    ast.GetToolTip(range.StartLine, range.StartColumn, lineText)
                    )
            let (tooltips:(FSharpToolTipText * int) []) = 
                res 
                |> Async.Parallel 
                |> Async.RunSynchronously
                |> Array.choose id

            let res = tooltips |> Array.map(fun (t, line) -> extractSignature t, line)
            Some res)

    let markers = ResizeArray<SignatureHelpMarker>()
    let highlightUnused (doc:TextDocument) font (unusedOpenRanges: (string * int) []) =

        markers |> Seq.iter(fun m -> doc.RemoveMarker m)
        markers.Clear()
        unusedOpenRanges |> Array.iter(fun (text, line) ->
            let marker = SignatureHelpMarker(doc, text, font, line)
            markers.Add marker
            doc.AddMarker(line, marker))

type SignatureHelp() =
    inherit TextEditorExtension()

    override x.Initialize() =
        let data = x.Editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
        let textDocument = data.Document
        let editorFont = data.Options.Font
        let font = new Pango.FontDescription(AbsoluteSize=float(editorFont.Size) * SignatureHelpMarker.FontScale, Family=editorFont.Family)
        x.DocumentContext.DocumentParsed.Add (fun _ -> let unused = signatureHelp.getUnusedOpens x.DocumentContext x.Editor
                                                       unused |> Option.iter(fun unused' -> signatureHelp.highlightUnused textDocument font unused'))// TypeSignatureHelp.fs
