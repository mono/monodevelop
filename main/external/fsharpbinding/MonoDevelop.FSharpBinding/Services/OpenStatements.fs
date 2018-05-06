namespace MonoDevelop.FSharp

open System
open MonoDevelop.Ide.Editor
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

module openStatements =
    let addOpenStatement (editor:TextEditor) (parseTree:ParsedInput) (fullName:string) =
        let lineNumber = editor.CaretLine - 1
        let insertionPoint = OpenStatementInsertionPoint.Nearest
        let idents = fullName.Split '.'
        match ParsedInput.tryFindNearestPointToInsertOpenDeclaration lineNumber parseTree idents insertionPoint with
        | Some context ->
            let getLineText = fun lineNumber -> editor.GetLineText(lineNumber+1, false)
            let pos = context |> ParsedInput.adjustInsertionPoint getLineText

            let lineText = editor.GetLineText(pos.Line - 1, false)
            let column =
                if (lineText.TrimStart ' ').StartsWith "module" then
                    let line = editor.GetLine(pos.Line - 1)
                    let indentation = line.GetIndentation(editor.CreateDocumentSnapshot())
                    editor.Options.IndentationSize + indentation.Length
                else
                    //FCS assumes indent of 4 - correct for user specified indent length
                    pos.Column - 4 + editor.Options.IndentationSize
            let openPrefix = String(' ', column) + "open "
            let textToInsert = openPrefix + fullName

            let line = pos.Line
            let lineToInsert =
                seq { line - 1 .. -1 .. 1 }
                |> Seq.takeWhile (fun i ->
                    let lineText = editor.GetLineText(i, false)
                    lineText.StartsWith(openPrefix) &&
                        (textToInsert < lineText))// || isSystem && not (lineText.StartsWith("open System")))) // todo: System<smth> namespaces
                |> Seq.tryLast
                |> Option.defaultValue line

            let prevLineEndOffset =
                if lineToInsert > 1 then
                    (editor.GetLine (lineToInsert - 1)).EndOffsetIncludingDelimiter
                else 0

            editor.InsertText(prevLineEndOffset, textToInsert + "\n")
        | None -> ()

    let rec visitDecls decls currentLine =
        [ for decl in decls do
            match decl with
            | SynModuleDecl.Open(longIdentWithDots, range) -> 
                yield (longIdentWithDots.Lid |> List.map(fun l -> l.idText) |> String.concat "."), range
            | SynModuleDecl.NestedModule (_, _, decls , _, range) ->
                if range.StartLine < currentLine && range.EndLine >= currentLine then
                    yield! visitDecls decls currentLine
            | _ -> () ]

    let private visitModulesAndNamespaces modulesOrNss currentLine =
        [ for moduleOrNs in modulesOrNss do
            let (SynModuleOrNamespace(_lid, _isRec, _isMod, decls, _xml, _attrs, _, _m)) = moduleOrNs
            yield! visitDecls decls currentLine ]

    let getOpenStatements (tree: ParsedInput option) currentLine = 
        match tree with
        | Some (ParsedInput.ImplFile(implFile)) ->
            let (ParsedImplFileInput(_fn, _script, _name, _, _, modules, _)) = implFile
            visitModulesAndNamespaces modules currentLine
        | _ -> []
