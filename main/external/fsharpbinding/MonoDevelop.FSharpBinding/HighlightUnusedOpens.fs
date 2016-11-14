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

    let getUnusedOpens (context:DocumentContext) =
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
                                    match sym with
                                    | SymbolUse.Entity ent -> entityNamespace ent
                                    | SymbolUse.Field f -> entityNamespace f.DeclaringEntity
                                    | SymbolUse.MemberFunctionOrValue mfv -> 
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
        let getOffset line col = editor.LocationToOffset (line, col+1)
        
        unusedOpenRanges |> List.iter(fun (_, range) ->
            let startOffset = getOffset range.StartLine range.StartColumn
            let endOffset = getOffset range.EndLine range.EndColumn

            let markers = editor.GetTextSegmentMarkersAt startOffset
            markers |> Seq.iter (fun m -> editor.RemoveMarker m |> ignore)

            let segment = new Text.TextSegment(startOffset, endOffset - startOffset)
            let marker = TextMarkerFactory.CreateGenericTextSegmentMarker(editor, TextSegmentMarkerEffect.GrayOut, segment)
            marker.IsVisible <- true
            editor.AddMarker(marker))

type HighlightUnusedOpens() =
    inherit TextEditorExtension()

    override x.Initialize() =
        x.DocumentContext.DocumentParsed.Add (fun _ -> let unused = highlightUnusedOpens.getUnusedOpens x.DocumentContext
                                                       unused |> Option.iter(fun unused' -> highlightUnusedOpens.highlightUnused x.Editor unused'))