namespace MonoDevelop.FSharp

open System
open System.Collections.Generic

open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.FindInFiles
open System.Linq
open ICSharpCode.NRefactory.TypeSystem
open System.IO
open MonoDevelop.Ide.TypeSystem
open ICSharpCode.NRefactory.Semantics
open Mono.TextEditor
open System.Threading
open MonoDevelop.Projects

type FSharpReferenceFinder() =
    inherit ReferenceFinder()

    override x.FindReferences(project, projectContent, files, progressMonitor, members) =
        let ref = members |> Seq.head
        match ref with
        | null -> Seq.empty
        | :? IVariable as variable ->
            let filename, line, col = variable.Region.FileName, variable.Region.BeginLine, variable.Region.BeginColumn
            let activeDoc = IdeApp.Workbench.ActiveDocument

            //save if dirty, real file refs are needed to resolve effectively
            if activeDoc.IsDirty then activeDoc.Save()

            let source = activeDoc.Editor.Text
            let lineStr = activeDoc.Editor.GetLineText(line)
            let projectFilename, files, args = MonoDevelop.getFilesAndArgsFromProject(project, IdeApp.Workspace.ActiveConfiguration)
            let references = MDLanguageService.Instance.GetReferences(projectFilename, filename, source, files, line-1, col-1, lineStr, args)
            match references with
            | Some(currentSymbolName, currentSymbolRange, references) -> 
                let memberRefs = 
                    references
                    |> Seq.map (fun (filename, range) -> NRefactory.createMemberReference(filename, range, currentSymbolName, currentSymbolRange))
                let debug = memberRefs |> Seq.toArray
                memberRefs
            | _ -> Seq.empty

        | _ -> Seq.empty
            
