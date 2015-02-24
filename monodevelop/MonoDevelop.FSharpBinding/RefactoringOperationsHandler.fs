namespace MonoDevelop.FSharp
open System
open System.IO
open System.Collections.Generic
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Core.Text
open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Ide.Commands
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Editor
open MonoDevelop.Projects
open MonoDevelop.Refactoring
open FSharp.CompilerBinding
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

module Roslyn =

    let getSymbolLocations (s: FSharpSymbolUse) =
        [s.Symbol.DeclarationLocation; s.Symbol.SignatureLocation]
        |> List.choose id

module Refactoring =

    type SymbolDeclarationLocation =
    | CurrentFile
    | Projects of Project seq * isSymbolLocal:bool
    | External
    | Unknown      

    let performChanges (symbol:FSharpSymbolUse) (locations:array<string * Microsoft.CodeAnalysis.Text.TextSpan> ) =
        Func<_,_>(fun (renameProperties:Rename.RenameRefactoring.RenameProperties) -> 
        let results =
            use monitor = new ProgressMonitoring.MessageDialogProgressMonitor (true, false, false, true)
            [|
                if renameProperties.RenameFile && symbol.IsFromType then
                    yield!
                        // TODO check .fsi file renames?
                        Roslyn.getSymbolLocations symbol
                        |> List.map (fun part -> RenameFileChange (part.FileName, renameProperties.NewName) :> Change)
                
                yield!
                    locations
                    |> Array.map (fun (name, location) ->
                        TextReplaceChange (FileName = name,
                                           Offset = location.Start,
                                           RemovedChars = location.Length,
                                           InsertedText = renameProperties.NewName,
                                           Description = sprintf "Replace '%s' with '%s'" symbol.Symbol.DisplayName renameProperties.NewName)
                        :> Change) |]
  
        results :> IList<Change> )

    let getSymbolDeclarationLocation (symbolUse: FSharpSymbolUse) (currentFile: FilePath) (currentProject: Project) =
        let isPrivateToFile = 
            match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as m -> not m.IsModuleValueOrMember
            | :? FSharpEntity as m -> m.Accessibility.IsPrivate
            | :? FSharpGenericParameter -> true
            | :? FSharpUnionCase as m -> m.Accessibility.IsPrivate
            | :? FSharpField as m -> m.Accessibility.IsPrivate
            | _ -> false

        if isPrivateToFile then 
            SymbolDeclarationLocation.CurrentFile 
        else
            let isSymbolLocalForProject = TypedAstUtils.isSymbolLocalForProject symbolUse.Symbol 
            match Option.orElse symbolUse.Symbol.ImplementationLocation symbolUse.Symbol.DeclarationLocation with
            | Some loc ->
                let filePath = Path.GetFullPathSafe loc.FileName
                if filePath = currentFile.ToString () then //Or if script?
                    SymbolDeclarationLocation.CurrentFile
                //elif currentProject.IsForStandaloneScript then
                    // The standalone script might include other files via '#load'
                    // These files appear in project options and the standalone file 
                    // should be treated as an individual project
                //    Some (SymbolDeclarationLocation.Projects ([currentProject], isSymbolLocalForProject))
                else
                    let allProjects = currentProject.ParentSolution.GetAllProjects ()
                    match allProjects
                          |> Seq.filter (fun p -> p.Files |> Seq.exists (fun f -> f.FilePath.ToString () = filePath)) with
                    | projects when projects |> Seq.isEmpty ->
                        External
                    | projects -> SymbolDeclarationLocation.Projects (projects, isSymbolLocalForProject)
            | None -> SymbolDeclarationLocation.Unknown

    let canRename (symbolUse:FSharpSymbolUse) (editor:TextEditor) project =
        match getSymbolDeclarationLocation symbolUse editor.FileName project with
        | SymbolDeclarationLocation.External -> false
        | SymbolDeclarationLocation.Unknown -> false
        | _ -> true

    let getSymbolAndLineInfoAtCaret (ast: ParseAndCheckResults) (editor:TextEditor) =
        let lineInfo = editor.GetLineInfoByCaretOffset ()
        let symbol = ast.GetSymbolAtLocation lineInfo |> Async.RunSynchronously
        lineInfo, symbol

type FSharpRenameRefactoring(editor:TextEditor, project) =

    member x.Rename (lastIdent, symbol:FSharpSymbolUse) =         
        let symbols = 
            let activeDocFileName = editor.FileName.ToString ()
            let projectFilename, projectFiles, projectArgs = MonoDevelop.getCheckerArgs(project, activeDocFileName)
            MDLanguageService.Instance.GetUsesOfSymbolInProject (projectFilename, activeDocFileName, editor.Text, projectFiles, projectArgs, symbol.Symbol)
            |> Async.RunSynchronously

        let locations =
            symbols |> Array.map (Symbols.getTextSpan lastIdent)

        let fileLocations = 
            locations
            |> Array.map fst
            |> Array.toSet
                  
        if fileLocations.Count = 1 then
            let links = ResizeArray<TextLink> ()
            let link = TextLink ("name")

            for (_file, loc) in locations do
                let segment = Text.TextSegment (loc.Start, loc.Length)
                if (segment.Offset <= editor.CaretOffset && editor.CaretOffset <= segment.EndOffset) then
                    link.Links.Insert (0, segment)
                else
                    link.AddLink (segment)

            links.Add (link)
            editor.StartTextLinkMode (TextLinkModeOptions (links))
        else 
            MessageService.ShowCustomDialog (new Rename.RenameItemDialog ("Rename Item", symbol.Symbol.DisplayName, Refactoring.performChanges symbol locations))
            |> ignore

type FSharpRefactorCommands =
| Rename = 1

type CurrentRefactoringOperationsHandler() =
    inherit CommandHandler()

    let tryGetValidDoc =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc = null || doc.FileName = FilePath.Null || doc.ParsedDocument = null then None
        else Some doc

    override x.Run (data) =
        let del =  data :?> Action
        if del <> null
        then del.Invoke ()
        
    override x.Update (ainfo:CommandArrayInfo) =
        match tryGetValidDoc with
        | None -> ()
        | Some doc ->
        match doc.ParsedDocument.TryGetAst () with
        | None -> ()
        | Some ast ->
        match Refactoring.getSymbolAndLineInfoAtCaret ast doc.Editor with
        | (_line, col, lineTxt), Some symbol ->
            let ciset = new CommandInfoSet (Text = GettextCatalog.GetString ("Refactor"))

            if Refactoring.canRename symbol doc.Editor doc.Project then
                let commandInfo = IdeApp.CommandService.GetCommandInfo (FSharpRefactorCommands.Rename)
                commandInfo.Enabled <- true
                let lastIdent = Symbols.lastIdent col lineTxt
                ciset.CommandInfos.Add (commandInfo, Action(fun _ -> (FSharpRenameRefactoring (doc.Editor, doc.Project)).Rename (lastIdent, symbol)))

            if ciset.CommandInfos.Count > 0 then
                ainfo.Add (ciset, null)
        | _ -> ()

//            bool first = true;
//            foreach (var fix in ext.Fixes.CodeRefactoringActions) {
//                if (added & first)
//                    ciset.CommandInfos.AddSeparator ();
//                var info2 = new CommandInfo (fix.Item2.Title);
//                ciset.CommandInfos.Add (info2, new Action (new CodeActionEditorExtension.ContextActionRunner (fix.Item2, doc.Editor, doc).Run));
//                added = true;
//                first = false;
//            }
//
//            if (ciset.CommandInfos.Count > 0) {
//                ainfo.Add (ciset, null);
//                added = true;
//            }
//
//            if (IdeApp.ProjectOperations.CanJumpToDeclaration (info.Symbol) || info.Symbol == null && IdeApp.ProjectOperations.CanJumpToDeclaration (info.CandidateSymbols.FirstOrDefault ())) {
//                var type = (info.Symbol ?? info.CandidateSymbols.FirstOrDefault ()) as INamedTypeSymbol;
//                if (type != null && type.Locations.Length > 1) {
//                    var declSet = new CommandInfoSet ();
//                    declSet.Text = GettextCatalog.GetString ("_Go to Declaration");
//                    foreach (var part in type.Locations) {
//                        int line = 0;
//                        declSet.CommandInfos.Add (string.Format (GettextCatalog.GetString ("{0}, Line {1}"), FormatFileName (part.SourceTree.FilePath), line), new Action (() => IdeApp.ProjectOperations.JumpTo (type, part, doc.Project)));
//                    }
//                    ainfo.Add (declSet);
//                } else {
//                    ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration), new Action (() => GotoDeclarationHandler.JumpToDeclaration (doc, info)));
//                }
//                added = true;
//            }
//
//            if (info.DeclaredSymbol != null && GotoBaseDeclarationHandler.CanGotoBase (info.DeclaredSymbol)) {
//                ainfo.Add (GotoBaseDeclarationHandler.GetDescription (info.DeclaredSymbol), new Action (() => GotoBaseDeclarationHandler.GotoBase (doc, info.DeclaredSymbol)));
//                added = true;
//            }
//
//            if (canRename) {
//                var sym = info.Symbol ?? info.DeclaredSymbol;
//                ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new System.Action (() => FindReferencesHandler.FindRefs (sym)));
//                if (doc.HasProject) {
//                    if (Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindSimilarSymbols (sym, semanticModel.Compilation).Count () > 1)
//                        ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (() => FindAllReferencesHandler.FindRefs (info.Symbol, semanticModel.Compilation)));
//                }
//                added = true;
//            }
//            if (info.DeclaredSymbol != null) {
//                string description;
//                if (FindDerivedSymbolsHandler.CanFindDerivedSymbols (info.DeclaredSymbol, out description)) {
//                    ainfo.Add (description, new Action (() => FindDerivedSymbolsHandler.FindDerivedSymbols (info.DeclaredSymbol)));
//                    added = true;
//                }
//
//                if (FindMemberOverloadsHandler.CanFindMemberOverloads (info.DeclaredSymbol, out description)) {
//                    ainfo.Add (description, new Action (() => FindMemberOverloadsHandler.FindOverloads (info.DeclaredSymbol)));
//                    added = true;
//                }
//
//                if (FindExtensionMethodHandler.CanFindExtensionMethods (info.DeclaredSymbol, out description)) {
//                    ainfo.Add (description, new Action (() => FindExtensionMethodHandler.FindExtensionMethods (info.DeclaredSymbol)));
//                    added = true;
//                }
//            }
//        }

type RenameHandler() =
    inherit CommandHandler()

    override x.Update (ci:CommandInfo) =
        let doc = IdeApp.Workbench.ActiveDocument
        let editor = doc.Editor
        //disable if theres no editor or filename
        if editor = null || editor.FileName = FilePath.Null
        then ci.Enabled <- false
        else
            match doc.ParsedDocument.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                //set bypass is we cant rename
                | _lineinfo, Some sym when not (Refactoring.canRename sym editor doc.Project) ->
                    ci.Bypass <- true
                    ci.Enabled <- false
                | _lineinfo, _symbol -> ()
            //disable for no ast
            | None -> ci.Enabled <- false
        
    override x.Run (_data) =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc = null || doc.FileName = FilePath.Null then ()
        else x.Run (doc.Editor, doc)

    member x.Run ( editor:TextEditor, ctx:DocumentContext) =
        match ctx.ParsedDocument.TryGetAst() with
        | Some ast ->
            match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
            | (_line, col, lineTxt), Some sym when Refactoring.canRename sym editor ctx.Project ->
                let rr = FSharpRenameRefactoring(editor, ctx.Project)
                let lastIdent = Symbols.lastIdent col lineTxt
                rr.Rename (lastIdent, sym)
            | _ -> ()
        | _ -> ()
