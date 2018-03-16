namespace MonoDevelop.FSharp

open System.Collections.Generic
open MonoDevelop
open MonoDevelop.Components.Commands
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open Mono.TextEditor
open ExtCore.Control
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.AstTraversal

module ExpandSelection =
    type ExpandSelectionAnnotation(editor:TextEditor) =
        let stack = new Stack<_>()
        do
            editor.CaretPositionChanged.Subscribe
                (fun _ -> editor.RemoveAnnotations<ExpandSelectionAnnotation>()
                          stack.Clear()) |> ignore

        member x.Pop() = stack.Pop()
        member x.Peek() = stack.Peek()
        member x.Push(selection) = stack.Push(selection)
        member x.Count = stack.Count

    let biggerOverlap (symbolRange, selection:Text.ISegment) =
        let startPos, endPos = symbolRange
        startPos < selection.Offset && endPos >= selection.EndOffset
        ||
        startPos <= selection.Offset && endPos > selection.EndOffset

    let inside (symbolRange, selection:Text.ISegment) =
        let symbolStart, _symbolEnd = symbolRange
        symbolStart >= selection.Offset

    let getExpandRange (editor:TextEditor, tree:ParsedInput) =
        if not editor.IsSomethingSelected then
            let line = editor.GetLine editor.CaretLine
            if editor.CaretColumn = line.LengthIncludingDelimiter || editor.CaretOffset = line.Offset then
                Some (line.Offset, line.EndOffset)
            else
                let data = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
                Some (data.FindCurrentWordStart(editor.CaretOffset), data.FindCurrentWordEnd(editor.CaretOffset))
        else
            let rangeAsOffsets(range:Range.range) =
                let startPos = editor.LocationToOffset(range.StartLine, range.StartColumn + 1)
                let endPos = editor.LocationToOffset(range.EndLine, range.EndColumn + 1)
                (startPos, endPos)

            
            let rec walker = 
                { new AstTraversal.AstVisitorBase<_>() with
                    
                    override this.VisitModuleDecl(defaultTraverse, decl) =
                        match decl with
                        | SynModuleDecl.Open(_, range) -> Some([], range)
                        | _ -> defaultTraverse(decl)

                    member this.VisitExpr(path, traverseSynExpr, defaultTraverse, expr) =
                        match expr with
                        | SynExpr.LongIdent(_,_,_,range) -> Some (path, range)
                        | SynExpr.Ident ident -> Some (path, ident.idRange)
                        | SynExpr.Const(synconst, constRange) ->
                            match synconst with
                            | SynConst.String(_str, range) -> Some (path, range)
                            | _ -> Some (path, constRange)
                        | _ ->
                            if inside(rangeAsOffsets expr.Range, editor.SelectionRange) then
                                Some (path, expr.Range)
                            else
                                defaultTraverse(expr) }

            let traversePath = AstTraversal.Traverse(mkPos editor.CaretLine (editor.CaretColumn), tree, walker)

            let rangesFromTraverse = function
                | TraverseStep.Binding binding -> [binding.RangeOfHeadPat; binding.RangeOfBindingAndRhs; binding.RangeOfBindingSansRhs]
                | TraverseStep.MatchClause synMatchClause -> [synMatchClause.Range]
                | TraverseStep.Expr synExpr -> [synExpr.Range]
                | TraverseStep.MemberDefn synMemberDefn -> [synMemberDefn.Range]
                | TraverseStep.Module synModuleDecl -> [synModuleDecl.Range]
                | TraverseStep.ModuleOrNamespace synModuleOrNamespace -> [synModuleOrNamespace.Range]
                | TraverseStep.TypeDefn synTypeDefn -> [synTypeDefn.Range]

            let selectionLineStart =
                let line = editor.GetLine editor.SelectionRegion.BeginLine
                if editor.SelectionRegion.BeginLine <> editor.SelectionRegion.EndLine then
                    line.Offset + line.GetIndentation(editor).Length, editor.SelectionRange.EndOffset
                else
                    line.Offset + line.GetIndentation(editor).Length, line.EndOffset

            let wholeDocument =
                0, editor.Length

            traversePath 
            |> Option.bind 
                (fun (traverseSteps, range) ->
                    let ranges = 
                        traverseSteps 
                        |> List.collect rangesFromTraverse

                    let offsetRanges =
                        range::ranges |> List.map rangeAsOffsets

                    let allranges =
                        [yield wholeDocument; yield selectionLineStart; yield! offsetRanges]

                    allranges
                    |> List.filter (fun range -> biggerOverlap(range, editor.SelectionRange))
                    |> List.sortBy (fun (startPos, endPos) -> endPos - startPos)
                    |> List.tryHead)

    let getSelectionAnnotation(editor:TextEditor) =
        let addAnnotation() =
            let annotation = new ExpandSelectionAnnotation(editor)
            editor.AddAnnotation (Some annotation)
            annotation

        let result = editor.Annotation<ExpandSelectionAnnotation option>() 

        result |> Option.getOrElse addAnnotation

    let makeSelection(editor:TextEditor) =
        let selection =
            maybe {
                let! ast = editor.DocumentContext.TryGetAst()
                let! tree = ast.ParseTree
                let! selection = getExpandRange(editor, tree)
                return selection
            }
        match selection with
        | Some (startPos, endPos) -> 
            let annotations = getSelectionAnnotation(editor)
            annotations.Push (startPos, endPos)
            editor.SetSelection(startPos, endPos)
        | _ -> LoggingService.logDebug "Did not find a region to expand the selection to"

type ExpandSelectionTextEditorExtension () =
    inherit Editor.Extension.TextEditorExtension ()

    let shrink(editor:TextEditor) =
        let annotations = ExpandSelection.getSelectionAnnotation(editor)
        annotations.Pop() |> ignore
        if annotations.Count > 0 then
            let startPos, endPos = annotations.Peek()
            editor.SetSelection (startPos, endPos)
        else
            editor.ClearSelection()

    override x.IsValidInContext (context) =
        context.Name <> null && FileService.supportedFileName context.Name

    [<CommandHandler ("MonoDevelop.Ide.Commands.TextEditorCommands.ExpandSelection")>]
    member x.ExpandSelection() =
        ExpandSelection.makeSelection x.Editor

    [<CommandHandler ("MonoDevelop.Ide.Commands.TextEditorCommands.ShrinkSelection")>]
    member x.ShrinkSelection() =
        shrink x.Editor
