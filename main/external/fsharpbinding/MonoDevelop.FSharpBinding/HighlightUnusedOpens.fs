namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore.Control
open MonoDevelop
open Microsoft.FSharp.Compiler.Ast

module highlightUnusedOpens =
    let getUnusedOpens (context:DocumentContext) (editor:TextEditor) =
        let ast =
            maybe {
                let! ast = context.TryGetAst()
                let! pd = context.TryGetFSharpParsedDocument()
                return ast.ParseTree, pd
            }

        ast |> Option.iter(fun (tree, pd) ->
            let visitModulesAndNamespaces modulesOrNss =
                [ for moduleOrNs in modulesOrNss do

                    let (SynModuleOrNamespace(_lid, _isRec, _isMod, decls, _xml, _attrs, _, _m)) = moduleOrNs

                    for decl in decls do
                        match decl with
                        | SynModuleDecl.Open(longIdentWithDots, range) -> 
                            LoggingService.logDebug "Namespace or module: %A" longIdentWithDots.Lid
                            yield (longIdentWithDots.Lid |> List.map(fun l -> l.idText) |> String.concat "."), range
                        | _ -> () ]

            let openStatements = 
                match tree.Value with
                | ParsedInput.ImplFile(implFile) ->
                    let (ParsedImplFileInput(_fn, _script, _name, _, _, modules, _)) = implFile
                    visitModulesAndNamespaces modules
                | _ -> []

            let symbols = pd.AllSymbolsKeyed.Values

            let entityNamespace (ent:FSharpEntity) =
                if ent.IsFSharpModule then
                    [Some ent.QualifiedName; Some ent.LogicalName; Some ent.AccessPath]
                else
                    [ent.Namespace; Some ent.AccessPath]

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

            let getOffset line col =
                editor.LocationToOffset (line, col+1)

            openStatements
            |> List.iter(fun (namepsc,range) -> 
                let startOffset = getOffset range.StartLine range.StartColumn
                let markers = editor.GetTextSegmentMarkersAt startOffset
                markers |> Seq.iter (fun m -> editor.RemoveMarker m |> ignore)

                if not (namespacesInUse.Contains namepsc) then
                    let endOffset = getOffset range.EndLine range.EndColumn
                    let segment = new Text.TextSegment(startOffset, endOffset - startOffset)
                    let marker = TextMarkerFactory.CreateGenericTextSegmentMarker(editor, TextSegmentMarkerEffect.GrayOut, segment)
                    marker.IsVisible <- true
                    editor.AddMarker(marker))
        )

type HighlightUnusedOpens() =
    inherit TextEditorExtension()

    override x.Initialize() =
        x.DocumentContext.DocumentParsed.Add (fun _ -> highlightUnusedOpens.getUnusedOpens x.DocumentContext x.Editor)