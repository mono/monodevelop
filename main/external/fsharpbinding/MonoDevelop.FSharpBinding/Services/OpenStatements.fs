namespace MonoDevelop.FSharp

open System
open MonoDevelop.Ide.Editor
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

module openStatements =
    let addOpenStatement (editor:TextEditor) (parseTree:ParsedInput) (fullName:string) =
        let lineNumber = editor.CaretLine
        let insertionPoint = OpenStatementInsertionPoint.Nearest
        let idents = fullName.Split '.'
        match ParsedInput.tryFindNearestPointToInsertOpenDeclaration lineNumber parseTree idents insertionPoint with
        | Some context ->
            let getLineText = fun lineNumber -> editor.GetLineText(lineNumber, false)
            let pos = context |> ParsedInput.adjustInsertionPoint getLineText

            let isSystem = fullName.StartsWith("System.")
            let openPrefix = String(' ', pos.Column) + "open "
            let textToInsert = openPrefix + fullName

            let line = pos.Line
            let lineToInsert =
                seq { line - 1 .. -1 .. 1 }
                |> Seq.takeWhile (fun i ->
                    let lineText = editor.GetLineText(i, false)
                    lineText.StartsWith(openPrefix) &&
                    (textToInsert < lineText || isSystem && not (lineText.StartsWith("open System")))) // todo: System<smth> namespaces
                |> Seq.tryLast
                |> Option.defaultValue line

            // add empty line after all open expressions if needed
            let insertEmptyLine =
                editor.GetLineText(line, false)
                |> String.IsNullOrWhiteSpace
                |> not

            let prevLineEndOffset =
                if lineToInsert > 0 then
                    (editor.GetLine (lineToInsert - 1 |> min 1)).EndOffsetIncludingDelimiter
                else 0

            editor.InsertText(prevLineEndOffset, textToInsert + "\n" + (if insertEmptyLine then "\n" else ""))
        | None -> ()

    let private visitModulesAndNamespaces modulesOrNss =
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
