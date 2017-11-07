﻿namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open ExtCore.Control
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Components
open MonoDevelop.Components.Commands
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type SignatureHelpMarker(document, text, font, line, isFromFSharpType) =
    inherit TextLineMarker()
    let mutable text' = text
    let mutable font' = font
    static let tag = obj()
    static member FontScale = 0.8
    member x.Text with get() = text' and set(value) = text' <- value
    // Font size can increase with zoom
    member x.Font with get() = font' and set(value) = font' <- value
    member x.IsFromFSharpType = isFromFSharpType

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
            g.MoveTo(x, y + editor.LineHeight * (1.0 - SignatureHelpMarker.FontScale) - 2.0)
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

    let extractSignature (FSharpToolTipText tips) isFromFSharpType =
        let getSignature (str: string) =
            let nlpos = str.IndexOfAny [|'\r';'\n'|]
            let nlpos = if nlpos > 0 then nlpos else str.Length
            if not isFromFSharpType then
                let parensPos = str.IndexOf '(' 
                if parensPos > 0 then
                    // BCL tupled arguments method
                    let str = 
                        str.[parensPos .. nlpos-1]
                        |> String.replace "()" "unit"
                    let lastColon = str.LastIndexOf ':'
                    sprintf "%s->%s" str.[0 .. lastColon-1] str.[lastColon+1 .. str.Length-1]
                else
                    let index = str.IndexOf ": "
                    str.[index+2 .. nlpos-1]
            else
                let index = str.IndexOf ": "
                str.[index+2 .. nlpos-1]

        let firstResult x =
            match x with
            | FSharpToolTipElement.Group gs -> gs |> List.tryPick (fun data -> if not (String.IsNullOrWhiteSpace data.MainDescription) then Some data.MainDescription else None)
            | _ -> None

        tips
        |> Seq.tryPick firstResult
        |> Option.map getSignature
        |> Option.fill ""

    let isFSharp (mfv: FSharpMemberOrFunctionOrValue) =
        let typeDefinitionSafe (t:FSharpType) =
            match t.HasTypeDefinition with
            | true -> Some t.TypeDefinition
            | false -> None

        let baseType =
            match mfv.IsOverrideOrExplicitInterfaceImplementation, mfv.IsExplicitInterfaceImplementation with
            | true, false ->
                mfv.EnclosingEntity
                |> Option.bind(fun ent -> ent.BaseType)
                |> Option.bind typeDefinitionSafe
            | true, true ->
                mfv.ImplementedAbstractSignatures
                |> Seq.tryHead
                |> Option.map(fun s -> s.DeclaringType)
                |> Option.bind typeDefinitionSafe
            | _ -> mfv.EnclosingEntity

        baseType
        |> Option.map(fun ent -> ent.IsFSharp)
        |> Option.defaultValue true

    let getFunctionInformation (symbolUse:FSharpSymbolUse) =
        match symbolUse with 
        | SymbolUse.MemberFunctionOrValue mfv when symbolUse.IsFromDefinition ->
            match mfv.FullTypeSafe with
            | Some t when t.IsFunctionType ->
                 Some (symbolUse.RangeAlternate.StartLine, (symbolUse, isFSharp mfv))
            | _ -> None
        | _ -> None

    let displaySignatures (context:DocumentContext) (editor:TextEditor) recalculate =
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
          if pd.AllSymbolsKeyed.Count > 0 then
            let symbols = pd.AllSymbolsKeyed.Values |> List.ofSeq
            let topVisibleLine = data.HeightTree.YToLineNumber data.VAdjustment.Value
            let bottomVisibleLine = 
                Math.Min(data.LineCount,
                    data.HeightTree.YToLineNumber (data.VAdjustment.Value+data.VAdjustment.PageSize))

            let funs =
                symbols
                |> List.choose getFunctionInformation
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

            let addMarker text (lineNr:int) line isFromFSharpType =
                let newMarker = SignatureHelpMarker(document, text, font, line, isFromFSharpType)
                runInMainThread(fun() -> document.AddMarker(lineNr, newMarker))
                newMarker

            funs |> Map.iter(fun _l (f, isFSharp) ->
                let range = f.RangeAlternate

                let lineOption = editor.GetLine range.StartLine |> Option.ofObj
                lineOption |> Option.iter(fun line ->
                    let marker = editor.GetLineMarkers line |> Seq.tryPick(Option.tryCast<SignatureHelpMarker>)

                    match marker, recalculate with
                    | Some marker', false when marker'.Text <> "" -> ()
                    | _ -> 
                        let marker = marker |> Option.getOrElse(fun() -> addMarker "" range.StartLine line isFSharp)
                        if range.StartLine >= topVisibleLine && range.EndLine <= bottomVisibleLine then
                            async {
                                let lineText = editor.GetLineText(range.StartLine)
                                let! tooltip = ast.GetToolTip(range.StartLine, range.StartColumn, lineText)
                                tooltip |> Option.iter(fun (tooltip, lineNr) ->
                                    let text = extractSignature tooltip marker.IsFromFSharpType
                                    marker.Text <- text
                                    marker.Font <- font
                                    runInMainThread (fun() -> document.CommitLineUpdate lineNr))
                            } |> Async.StartImmediate)))

type SignatureHelp() as x =
    inherit TextEditorExtension()
    let mutable disposables = []

    let removeAllMarkers() =
        async {
            let editor = x.Editor
            editor.GetLines() |> Seq.iter(signatureHelp.removeMarkers editor >> ignore)
            let editorData = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
            signatureHelp.runInMainThread (fun() -> editorData.Document.CommitUpdateAll())
        } |> Async.StartImmediate
   
    [<CommandHandler("MonoDevelop.FSharp.SignatureHelp.Toggle")>]
    member x.SignatureHelpToggle() =
        let current = PropertyService.Get(Settings.showTypeSignatures, false)
        PropertyService.Set(Settings.showTypeSignatures, not current)

    override x.Initialize() =
        let displaySignatures dueMs recalculate observable =
            observable
            |> Observable.filter(fun _ -> PropertyService.Get(Settings.showTypeSignatures, false))
            |> Observable.throttle (TimeSpan.FromMilliseconds dueMs)
            |> Observable.subscribe (fun _ -> signatureHelp.displaySignatures x.DocumentContext x.Editor recalculate)
        
        let resetSignatures dueMs recalculate observable =
            removeAllMarkers()
            displaySignatures dueMs recalculate observable
    
        disposables <-
            [ x.Editor.VAdjustmentChanged
              |> displaySignatures 100. false

              x.Editor.ZoomLevelChanged
              |> resetSignatures 100. true

              x.DocumentContext.DocumentParsed
              |> displaySignatures 1000. true

              PropertyService.PropertyChanged
                  .Subscribe(fun p -> if p.Key = Settings.showTypeSignatures then
                                              match (p.NewValue :?> bool) with
                                              | true -> signatureHelp.displaySignatures x.DocumentContext x.Editor true
                                              | false -> removeAllMarkers())]

    override x.Dispose() = disposables |> List.iter(fun disp -> disp.Dispose())