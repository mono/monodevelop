// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
namespace MonoDevelop.FSharp

open System
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp
open MonoDevelop
open MonoDevelop.Ide.Editor
open MonoDevelop.Refactoring

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal InsertContext =
    /// Corrects insertion line number based on kind of scope and text surrounding the insertion point.
    let adjustInsertionPoint (text: string) ctx  =

        let sourceText = String.getLines text
        let getLineStr line = sourceText.[line].ToString().Trim()
        let line =
            match ctx.ScopeKind with
            | ScopeKind.TopModule ->
                if ctx.Pos.Line > 1 then
                    // it's an implicit module without any open declarations    
                    let line = getLineStr (ctx.Pos.Line - 2)
                    let isImpliciteTopLevelModule = not (line.StartsWith "module" && not (line.EndsWith "="))

                    if isImpliciteTopLevelModule then 
                        let rec skipComments (line:string) count =
                            if line.StartsWith ("//") then 
                                let count = count + 1
                                skipComments (getLineStr count) count
                            else count
                        skipComments (getLineStr 0) 0
                    else ctx.Pos.Line - 1
                else 0
            | ScopeKind.Namespace ->
                // for namespaces the start line is start line of the first nested entity
                if ctx.Pos.Line > 1 then
                    [0..ctx.Pos.Line - 1]
                    |> List.mapi (fun i line -> i, getLineStr line)
                    |> List.tryPick (fun (i, lineStr) -> 
                        if lineStr.StartsWith "namespace" then Some i
                        else None)
                    |> function
                        | Some line -> line + 1
                        | None -> ctx.Pos.Line - 1
                else 1  
            | _ -> ctx.Pos.Line - 1

        if line = 0 then 0 
        else   
            let lengthOfLines = sourceText |> Array.take line |> String.concat "\n" |> sprintf "%s\n"
            lengthOfLines.Length


    /// <summary>
    /// Creates a TextReplaceChange with open declaration at an appropriate offset
    /// </summary>
    /// <param name="sourceText">SourceText.</param>
    /// <param name="ctx">Insertion context. Typically returned from tryGetInsertionContext</param>
    /// <param name="ns">Namespace to open.</param>
    let insertOpenDeclartionWithEditor (editor:TextEditor) (ctx) displayText = 
        let activeDocFileName = editor.FileName.ToString ()
        let offset = adjustInsertionPoint editor.Text ctx

        TextReplaceChange (FileName = activeDocFileName,
                           Offset = offset,
                           RemovedChars = 0,
                           InsertedText = displayText,
                           Description = String.Format ("Insert open declartion '{0}''", displayText))  :> Change
        |> Array.singleton

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal FSharpAddOpenCodeFixProvider = 

    let getSuggestions editor monitor (candidates: (Entity * InsertContext) list) =
        candidates
        |> Seq.choose (fun (entity, ctx) -> entity.Namespace |> Option.map (fun ns -> ns, entity.Name, ctx))
        |> Seq.groupBy (fun (ns, _, _) -> ns)
        |> Seq.map (fun (ns, xs) -> 
            ns, 
            xs 
            |> Seq.map (fun (_, name, ctx) -> name, ctx) 
            |> Seq.distinctBy (fun (name, _) -> name)
            |> Seq.sort
            |> Seq.toArray)
        |> Seq.map (fun (ns, names) ->
            let multipleNames = names |> Array.length > 1
            names |> Seq.map (fun (name, ctx) -> ns, name, ctx, multipleNames))
        |> Seq.concat
        |> Seq.map (fun (ns, name, ctx, multipleNames) -> 

            let displayText = "open " + ns + if multipleNames then " (" + name + ")" else "" + Environment.NewLine

            displayText, 
                fun () -> 
                    let changes = InsertContext.insertOpenDeclartionWithEditor editor ctx displayText
                    RefactoringService.AcceptChanges(monitor, changes)
            )

    let getCodeFixesAsync (editor:TextEditor) (assemblyContentProvider: AssemblyContentProvider) monitor 
            (ast:ParseAndCheckResults) (unresolvedIdentRange:Range.range) = 
        asyncMaybe {
            let! parsedInput = ast.ParseTree
            let! checkResults = ast.CheckResults

            let isAttribute = UntypedParseImpl.GetEntityKind(unresolvedIdentRange.Start, parsedInput) = Some EntityKind.Attribute

            let entities =
                assemblyContentProvider.GetAllEntitiesInProjectAndReferencedAssemblies checkResults
                |> List.collect (fun e -> 
                     [ yield e.TopRequireQualifiedAccessParent, e.AutoOpenParent, e.Namespace, e.CleanedIdents
                       if isAttribute then
                           let lastIdent = e.CleanedIdents.[e.CleanedIdents.Length - 1]
                           if lastIdent.EndsWith "Attribute" && e.Kind LookupType.Precise = EntityKind.Attribute then
                               yield 
                                   e.TopRequireQualifiedAccessParent, 
                                   e.AutoOpenParent,
                                   e.Namespace,
                                   e.CleanedIdents 
                                   |> Array.replace (e.CleanedIdents.Length - 1) (lastIdent.Substring(0, lastIdent.Length - 9)) ])

            let idents = ParsedInput.getLongIdents (Some parsedInput)

            let longIdent = ParsedInput.getLongIdentAt parsedInput unresolvedIdentRange.End
            
            let! maybeUnresolvedIdents =
                longIdent 
                |> Option.map (fun longIdent ->
                    longIdent
                    |> List.map (fun ident ->
                        { Ident = ident.idText
                          Resolved = false }) // not (ident.idRange = unresolvedIdentRange) })
                    |> List.toArray)
            let createEntity = ParsedInput.tryFindInsertionContext unresolvedIdentRange.StartLine parsedInput maybeUnresolvedIdents
            let candidates = entities |> Seq.map createEntity |> Seq.concat |> Seq.toList
            return getSuggestions editor monitor candidates |> Seq.toList
        }


