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

    let (|SymbolWithRegion|_|) (x:obj) =
        match x with 
        | :? IVariable as e -> Some e.Region
        | :? IEntity as e -> Some e.Region
        | _ -> None

    let (|SymbolWithFSharpSymbol|_|) (x:obj) =
        match x with 
        | :? NRefactory.IHasFSharpSymbol as e -> Some e
        | :? IMember as e -> 
            match e.UnresolvedMember with 
            | :? NRefactory.IHasFSharpSymbol as e -> Some e
            | _ -> None
        | _ -> None

    override x.FindReferences(project, projectContent, files, progressMonitor, symbols) =
      seq { 
        for symbol in symbols do 
          match symbol with
          | SymbolWithRegion(region) & SymbolWithFSharpSymbol(fsSymbol) ->
           //let filename, line, col = region.FileName, region.BeginLine, region.BeginColumn
           
           // A null filename can be present for symbols outside the solution
           //if filename <> null then 
            
            
            let activeDoc = IdeApp.Workbench.ActiveDocument
            let activeDocFileName = activeDoc.FileName.FileName

            //save if dirty, real file refs are needed to resolve effectively
            if activeDoc.IsDirty then activeDoc.Save()

            let source = activeDoc.Editor.Text
            //let lineStr = activeDoc.Editor.GetLineText(line)
            let projectFilename, files, args, framework = MonoDevelop.getCheckerArgsFromProject(project, IdeApp.Workspace.ActiveConfiguration)
            let references = 
                try Some(MDLanguageService.Instance.GetUsesOfSymbol(projectFilename, activeDocFileName, source, files, args, framework, fsSymbol.FSharpSymbol) |> Async.RunSynchronously)
                with _ -> None
            match references with
            | Some(symbolDeclLocOpt, references) -> 
                let memberRefs = [| for (filename, range) in references -> NRefactory.createMemberReference(projectContent, fsSymbol.FSharpSymbol, filename, range, fsSymbol.LastIdent, symbolDeclLocOpt) |]
                yield! memberRefs
            | None -> ()

          | _ -> () }
             
