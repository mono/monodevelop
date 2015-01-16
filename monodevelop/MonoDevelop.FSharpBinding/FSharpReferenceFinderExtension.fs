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

/// Top-level 'ReferenceFinder' extension, referenced in FSharpBinding.addin.xml.orig
type FSharpReferenceFinder() =
    inherit ReferenceFinder()

    /// Detect a symbol that has some region information
    let (|SymbolWithRegion|_|) (x:obj) =
        match x with 
        | :? IVariable as e -> Some e.Region
        | :? IEntity as e -> Some e.Region
        | _ -> None

    /// Detect a symbol that has come from the F# resolver and/or reference finder.
    let (|SymbolWithFSharpInfo|_|) (x:obj) =
        match x with 
        | :? NRefactory.IHasFSharpSymbol as e -> Some e
        | :? IMember as e -> 
            match e.UnresolvedMember with 
            | :? NRefactory.IHasFSharpSymbol as e -> Some e
            | _ -> None
        | _ -> None

    override x.FindReferences(project, projectContent, _files, _progressMonitor, symbols) =
      // If the breakpoint is not triggered by a 'Find References' action,
      // then it probably means the inferred set of files to search for a symbol has not been correctly determined
      // by MD/XS.  The logic of 'what to search' used by XS is quite convoluted and depends on properties of
      // the symbol, e.g. accessibility, whether it is an IVariable, IEntity, etc.
      System.Diagnostics.Debug.WriteLine("Finding references...")
      seq { 
        for symbol in symbols do 
          match symbol with
          | SymbolWithRegion(_region) & SymbolWithFSharpInfo(fsSymbol) ->
            
            // Get the active document, but only to 
            //   (a) determine if this is a script
            //   (b) find the active project confifuration. 
            // TODO: we should instead enumerate the items in 'files'?
            let activeDoc = IdeApp.Workbench.ActiveDocument
            let activeDocFileName = activeDoc.FileName.FullPath.ToString()

            // Get the source, but only in order to infer the project options for a script.
            let activeDocSource = activeDoc.Editor.Text
            
            let projectFilename, projectFiles, projectArgs = MonoDevelop.getCheckerArgs(project, activeDocFileName)
            let references = 
                try Some(MDLanguageService.Instance.GetUsesOfSymbolInProject(projectFilename, activeDocFileName, activeDocSource, projectFiles, projectArgs, fsSymbol.FSharpSymbol) 
                    |> Async.RunSynchronously)
                with _ -> None

            match references with
            | Some(references) -> 
                let memberRefs = [| for symbolUse in references -> 
                                        let text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText(symbolUse.FileName)
                                        NRefactory.createMemberReference(projectContent, symbolUse, symbolUse.FileName, text, fsSymbol.LastIdent) |]
                yield! memberRefs
            | None -> ()

          | _ -> () }
             
