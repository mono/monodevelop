namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Linq
open ExtCore.Control
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast

module highlightUnusedCode =
    let visitModulesAndNamespaces modulesOrNss =
        [ for moduleOrNs in modulesOrNss do
            let (SynModuleOrNamespace(_lid, _isRec, _isMod, decls, _xml, _attrs, _, _m)) = moduleOrNs

            for decl in decls do
                match decl with
                | SynModuleDecl.Open(longIdentWithDots, range) -> 
                    yield (longIdentWithDots.Lid |> List.map(fun l -> l.idText) |> String.concat "."), range
                | _ -> () ]

    let getOpenStatements (tree: ParsedInput option) = 
        match tree with
        | Some (ParsedInput.ImplFile(implFile)) ->
            let (ParsedImplFileInput(_fn, _script, _name, _, _, modules, _)) = implFile
            visitModulesAndNamespaces modules
        | _ -> []

    let getAutoOpenAccessPath (ent:FSharpEntity) =
        // Some.Namespace+AutoOpenedModule+Entity

        // HACK: I can't see a way to get the EnclosingEntity of an Entity
        // Some.Namespace + Some.Namespace.AutoOpenedModule are both valid
        ent.TryFullName |> Option.bind(fun _ ->
            if (not ent.IsNamespace) && ent.QualifiedName.Contains "+" then 
                Some ent.QualifiedName.[0..ent.QualifiedName.IndexOf "+" - 1]
            else
                None)

    let entityNamespace (entOpt:FSharpEntity option) =
        match entOpt with
        | Some ent ->
            if ent.IsFSharpModule then
                [Some ent.FullName; Some ent.LogicalName; Some ent.AccessPath]
            else
                [ yield ent.Namespace
                  yield Some ent.AccessPath
                  if ent.AccessPath.StartsWith "Microsoft.FSharp" then
                      yield Some (ent.AccessPath.[10..])
                  yield getAutoOpenAccessPath ent ]
        | None -> []

    let getOffset (editor:TextEditor) (pos:Range.pos) =
        editor.LocationToOffset (pos.Line, pos.Column+1)

    let textFromRange (editor:TextEditor) (range:Range.range) =
        let startOffset = getOffset editor range.Start
        let endOffset = getOffset editor range.End
        editor.GetTextBetween (startOffset, endOffset)

    let symbolIsFullyQualified (editor:TextEditor) (sym:FSharpSymbolUse) (fullName:string option) =
        match fullName with
        | Some fullName' ->
            let startOffset = getOffset editor sym.RangeAlternate.Start
            let endOffset = getOffset editor sym.RangeAlternate.End
            let startOffset =
                if endOffset - startOffset = fullName'.Length then
                    startOffset
                else
                    // Entity range isn't the full longIdent range for some reason
                    let fqdiff = fullName'.Length - sym.Symbol.DisplayName.Length
                    startOffset - fqdiff
            let text = editor.GetTextBetween (startOffset, endOffset)
            text = fullName'
        | None -> true

    let removeMarkers (editor:TextEditor) (ranges:Range.range list) =
        ranges |> List.iter(fun range ->
            let startOffset = getOffset editor range.Start
            let markers = editor.GetTextSegmentMarkersAt startOffset
            markers |> Seq.iter (fun m -> editor.RemoveMarker m |> ignore))

    let getUnusedCode (context:DocumentContext) (editor:TextEditor) =
        async {
            match context.TryGetCheckResults() with
            | Some checkResults ->
                let! opens = UnusedOpens.getUnusedOpens(checkResults, fun lineNum -> editor.GetLineText(lineNum))
                return Some opens
            | None -> return None
        }

    let highlightUnused (editor:TextEditor) (unusedOpenRanges: Range.range list) (previousUnused: Range.range list)=
        previousUnused |> removeMarkers editor

        unusedOpenRanges |> List.iter(fun range ->
            let startOffset = getOffset editor range.Start
            let markers = editor.GetTextSegmentMarkersAt startOffset |> Seq.toList
            if markers.Length = 0 then
                let endOffset = getOffset editor range.End

                let segment = new Text.TextSegment(startOffset, endOffset - startOffset)
                let marker = TextMarkerFactory.CreateGenericTextSegmentMarker(editor, TextSegmentMarkerEffect.GrayOut, segment)
                marker.IsVisible <- true

                editor.AddMarker(marker))

type HighlightUnusedCode() =
    inherit TextEditorExtension()
    let mutable previousUnused = []
    override x.Initialize() =
        let parsed = x.DocumentContext.DocumentParsed
        parsed.Add(fun _ ->
                        async {
                            let! unused = highlightUnusedCode.getUnusedCode x.DocumentContext x.Editor
                            unused |> Option.iter(fun unused' ->
                                highlightUnusedCode.highlightUnused x.Editor unused' previousUnused
                                previousUnused <- unused')
                        } |> Async.StartImmediate)
