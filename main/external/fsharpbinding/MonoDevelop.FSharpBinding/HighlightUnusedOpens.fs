namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore.Control
open MonoDevelop
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast

module highlightUnusedOpens =
    let visitModulesAndNamespaces modulesOrNss =
        [ for moduleOrNs in modulesOrNss do

            let (SynModuleOrNamespace(_lid, _isRec, _isMod, decls, _xml, _attrs, _, _m)) = moduleOrNs

            for decl in decls do
                match decl with
                | SynModuleDecl.Open(longIdentWithDots, range) -> 
                    LoggingService.logDebug "Namespace or module: %A" longIdentWithDots.Lid
                    yield (longIdentWithDots.Lid |> List.map(fun l -> l.idText) |> String.concat "."), range
                | _ -> () ]

    let openStatements (tree: ParsedInput option) = 
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

    let entityNamespace (ent:FSharpEntity) =
        if ent.IsFSharpModule then
            [Some ent.QualifiedName; Some ent.LogicalName; Some ent.AccessPath]
        else
            [ent.Namespace; Some ent.AccessPath; getAutoOpenAccessPath ent]

    let getOffset (editor:TextEditor) (pos:Range.pos) =
        editor.LocationToOffset (pos.Line, pos.Column+1)

    let symbolIsFullyQualified (editor:TextEditor) (sym:FSharpSymbolUse) (fullName:string) =

        let startOffset = getOffset editor sym.RangeAlternate.Start
        let endOffset = getOffset editor sym.RangeAlternate.End
        let startOffset = 
            if endOffset - startOffset = fullName.Length then
                startOffset
            else
                // Entity range isn't the full longIdent range for some reason
                let fqdiff = fullName.Length - sym.Symbol.DisplayName.Length
                startOffset - fqdiff
        let text = editor.GetTextBetween (startOffset, endOffset)
        text = fullName

    let getUnusedOpens (context:DocumentContext) (editor:TextEditor) =
        let ast =
            maybe {
                let! ast = context.TryGetAst()
                let! pd = context.TryGetFSharpParsedDocument()
                return ast.ParseTree, pd
            }

        ast |> Option.bind (fun (tree, pd) ->
            let symbols = pd.AllSymbolsKeyed.Values

            let namespacesInUse =
                symbols
                |> Seq.collect (fun sym ->
                                    let isQualified = symbolIsFullyQualified editor sym
                                    match sym with
                                    | SymbolUse.Entity ent when not (isQualified ent.FullName) -> entityNamespace ent
                                    | SymbolUse.Field f when not (isQualified f.FullName) -> entityNamespace f.DeclaringEntity
                                    | SymbolUse.MemberFunctionOrValue mfv when not (isQualified mfv.FullName) -> 
                                        try
                                            entityNamespace mfv.EnclosingEntity
                                        with :? InvalidOperationException -> [None]
                                    | _ -> [None])
                |> Seq.choose id
                |> Set.ofSeq

            let filter list =
                let rec filterInner acc list (seenNamespaces: Set<string>) = 
                    let notUsed namespc =
                        not (namespacesInUse.Contains namespc) || seenNamespaces.Contains namespc

                    match list with 
                    | (namespc, range)::xs when notUsed namespc -> 
                        filterInner ((namespc, range)::acc) xs (seenNamespaces.Add namespc)
                    | (namespc, _range)::xs -> filterInner acc xs (seenNamespaces.Add namespc)
                    | [] -> acc |> List.rev
                filterInner [] list Set.empty

            let results = openStatements tree |> filter

            Some results)

    let highlightUnused (editor:TextEditor) (unusedOpenRanges: (string * Microsoft.FSharp.Compiler.Range.range) list) =

        
        unusedOpenRanges |> List.iter(fun (_, range) ->
            let startOffset = getOffset editor range.Start
            let endOffset = getOffset editor range.End

            let markers = editor.GetTextSegmentMarkersAt startOffset
            markers |> Seq.iter (fun m -> editor.RemoveMarker m |> ignore)

            let segment = new Text.TextSegment(startOffset, endOffset - startOffset)
            let marker = TextMarkerFactory.CreateGenericTextSegmentMarker(editor, TextSegmentMarkerEffect.GrayOut, segment)
            marker.IsVisible <- true
            editor.AddMarker(marker))

type HighlightUnusedOpens() =
    inherit TextEditorExtension()

    override x.Initialize() =
        x.DocumentContext.DocumentParsed.Add (fun _ -> let unused = highlightUnusedOpens.getUnusedOpens x.DocumentContext x.Editor
                                                       unused |> Option.iter(fun unused' -> highlightUnusedOpens.highlightUnused x.Editor unused'))