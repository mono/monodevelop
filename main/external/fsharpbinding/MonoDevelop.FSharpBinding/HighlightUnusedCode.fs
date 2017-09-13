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
        match tree.Value with
        | ParsedInput.ImplFile(implFile) ->
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
        let ast =
            maybe {
                let! ast = context.TryGetAst()
                let! pd = context.TryGetFSharpParsedDocument()
                return ast, pd
            }

        ast |> Option.bind (fun (ast, pd) ->
            let errors = ast.GetErrors()
            if errors.Any() then
                None
            else
                let tree = ast.ParseTree
                let symbols = pd.AllSymbolsKeyed.Values

                let getPartNamespace (sym:FSharpSymbolUse) (fullName:string option) =
                    // given a symbol range such as `Text.ISegment` and a full name
                    // of `MonoDevelop.Core.Text.ISegment`, return `MonoDevelop.Core`
                    fullName |> Option.bind(fun fullName ->
                        let length = sym.RangeAlternate.EndColumn - sym.RangeAlternate.StartColumn
                        let lengthDiff = fullName.Length - length - 2
                        Some fullName.[0..lengthDiff])

                let getPossibleNamespaces sym =
                    let isQualified = symbolIsFullyQualified editor sym
                    match sym with
                    | SymbolUse.Entity ent when not (isQualified ent.TryFullName) ->
                        getPartNamespace sym ent.TryFullName :: entityNamespace (Some ent)
                    | SymbolUse.Field f when not (isQualified (Some f.FullName)) -> 
                        getPartNamespace sym (Some f.FullName) :: entityNamespace (Some f.DeclaringEntity)
                    | SymbolUse.MemberFunctionOrValue mfv when not (isQualified (Some mfv.FullName)) -> 
                        getPartNamespace sym (Some mfv.FullName) :: entityNamespace mfv.EnclosingEntity
                    | SymbolUse.Operator op when not (isQualified (Some op.FullName)) ->
                        getPartNamespace sym (Some op.FullName) :: entityNamespace op.EnclosingEntity
                    | SymbolUse.ActivePattern ap when not (isQualified (Some ap.FullName)) ->
                        getPartNamespace sym (Some ap.FullName) :: entityNamespace ap.EnclosingEntity
                    | SymbolUse.ActivePatternCase apc when not (isQualified (Some apc.FullName)) ->
                        getPartNamespace sym (Some apc.FullName) :: entityNamespace apc.Group.EnclosingEntity
                    | SymbolUse.UnionCase uc when not (isQualified (Some uc.FullName)) ->
                        getPartNamespace sym (Some uc.FullName) :: entityNamespace (Some uc.ReturnType.TypeDefinition)
                    | SymbolUse.Parameter p when not (isQualified (Some p.FullName)) ->
                        getPartNamespace sym (Some p.FullName) :: entityNamespace (Some p.Type.TypeDefinition)
                    | _ -> [None]

                let namespacesInUse =
                    symbols
                    |> Seq.filter (fun s -> not s.IsFromDefinition)
                    |> Seq.collect getPossibleNamespaces
                    |> Seq.choose id
                    |> Set.ofSeq

                let filter list: (string * Range.range) list =
                    let rec filterInner acc list (seenNamespaces: Set<string>) = 
                        let notUsed namespc =
                            not (namespacesInUse.Contains namespc) || seenNamespaces.Contains namespc

                        match list with 
                        | (namespc, range)::xs when notUsed namespc -> 
                            filterInner ((namespc, range)::acc) xs (seenNamespaces.Add namespc)
                        | (namespc, _)::xs ->
                            filterInner acc xs (seenNamespaces.Add namespc)
                        | [] -> acc |> List.rev
                    filterInner [] list Set.empty

                let openStatements = getOpenStatements tree
                openStatements |> List.map snd |> removeMarkers editor

                let results =
                    let opens = (openStatements |> filter) |> List.map snd
                    opens |> List.append (pd.UnusedCodeRanges |> Option.fill [])

                Some results)

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
                        let unused = highlightUnusedCode.getUnusedCode x.DocumentContext x.Editor

                        unused |> Option.iter(fun unused' ->
                        highlightUnusedCode.highlightUnused x.Editor unused' previousUnused
                        previousUnused <- unused'))
