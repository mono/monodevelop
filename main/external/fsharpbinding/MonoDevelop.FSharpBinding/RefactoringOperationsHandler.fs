namespace MonoDevelop.FSharp
open System
open System.IO
open System.Collections.Generic
open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Projects
open MonoDevelop.Refactoring
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.Ide.FindInFiles
open ExtCore.Control

module Refactoring =

    type SymbolDeclarationLocation =
    | CurrentFile
    | Projects of Project seq * isSymbolLocal:bool
    | External of string
    | Unknown

    let private performChanges (symbol:FSharpSymbolUse) (locations:array<string * Microsoft.CodeAnalysis.Text.TextSpan> ) =
        Func<_,_>(fun (renameProperties:Rename.RenameRefactoring.RenameProperties) ->
        async {
            do! Async.SwitchToContext(Runtime.MainSynchronizationContext)

            let results =
                use monitor = new ProgressMonitoring.MessageDialogProgressMonitor (true, false, false, true)
                [|
                    if renameProperties.RenameFile && symbol.IsFromType then
                        yield!
                            // TODO check .fsi file in renames?
                            Symbols.getLocationFromSymbolUse symbol
                            |> List.map (fun part -> RenameFileChange (part.FileName, renameProperties.NewName) :> Change)

                    yield!
                        locations
                        |> Array.map (fun (name, location) ->
                            TextReplaceChange (FileName = name,
                                               Offset = location.Start,
                                               RemovedChars = location.Length,
                                               InsertedText = renameProperties.NewName,
                                               Description = String.Format ("Replace '{0}' with '{1}'", symbol.Symbol.DisplayName, renameProperties.NewName))
                            :> Change) |]

            return results :> IList<Change>
        } |> StartAsyncAsTask System.Threading.CancellationToken.None)

    let getDocumentationId (symbol:FSharpSymbol) =
        match symbol with
        | :? FSharpEntity as ent ->
            ent.XmlDocSig
        | :? FSharpMemberOrFunctionOrValue as meth ->
            meth.XmlDocSig
        | _ -> ""

    let getSymbolDeclarationLocation (symbol: FSharpSymbol) (currentFile: FilePath) (solution: Solution) =
        let isPrivateToFile =
            match symbol with
            | :? FSharpMemberOrFunctionOrValue as m -> not m.IsModuleValueOrMember
            | :? FSharpEntity as m -> m.Accessibility.IsPrivate
            | :? FSharpGenericParameter -> true
            | :? FSharpUnionCase as m -> m.Accessibility.IsPrivate
            | :? FSharpField as m -> m.Accessibility.IsPrivate
            | _ -> false

        if isPrivateToFile then
            SymbolDeclarationLocation.CurrentFile
        else
            let location =
                match symbol.ImplementationLocation with
                | Some x -> Some x
                | None -> symbol.DeclarationLocation

            match location with
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
                    let allProjects = solution.GetAllProjects ()
                    match allProjects
                          |> Seq.filter (fun p -> p.Files |> Seq.exists (fun f -> f.FilePath.ToString () = filePath)) with
                    | projects when projects |> Seq.isEmpty ->
                        External (getDocumentationId symbol)
                    | projects -> SymbolDeclarationLocation.Projects (projects, symbol.IsSymbolLocalForProject)
            | None -> SymbolDeclarationLocation.Unknown


    let getBaseSymbol (symbolUse:FSharpSymbolUse) : FSharpSymbol option =
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsDispatchSlot ->
            maybe {
                let! ent =  mfv.EnclosingEntity
                let! bt = ent.BaseType
                if bt.HasTypeDefinition then
                    let baseDefs = bt.TypeDefinition.MembersFunctionsAndValues

                    //TODO check for more than one match?
                    let matches = baseDefs |> Seq.filter (fun btd -> btd.DisplayName = mfv.DisplayName) |> Seq.toList
                    assert (matches.Length <= 1)

                    //assume just the first for now
                    let! bm = baseDefs |> Seq.tryFind (fun btd -> btd.DisplayName = mfv.DisplayName)
                    return! Some (bm :> FSharpSymbol)
                else return! None }

        | :? FSharpEntity as ent ->
            match ent.BaseType with
            | Some bt when bt.HasTypeDefinition -> Some (bt.TypeDefinition :> _)
            | _ -> None
        | _ -> None

    let getSymbolAndLineInfoAtCaret (ast: ParseAndCheckResults) (editor:TextEditor) =
        let lineInfo = editor.GetLineInfoByCaretOffset ()
        // Note: this use of RunSynchronously is basically benign because GetSymbolAtLocation is "nearly always"
        // an operation that completes quickly.  Ideally it should not be asynchronous at all. This is an FCS issue.
        let symbol = ast.GetSymbolAtLocation lineInfo |> Async.RunSynchronously
        lineInfo, symbol

    /// Perform the renaming of a symbol
    let renameSymbol (editor:TextEditor, ctx:DocumentContext, lastIdent, symbol:FSharpSymbolUse) =
        // Collect the uses of the symbol across the solution.  The use of RunSynchronously  makes this a blocking UI 
        // action and will presumably cause the operation to fail with a timeout exception if it takes too long.
        let symbols =
            let activeDocFileName = editor.FileName.ToString ()
            languageService.GetUsesOfSymbolInProject (ctx.Project.FileName.ToString(), activeDocFileName, editor.Text, symbol.Symbol)
            |> (fun p -> Async.RunSynchronously(p, timeout=ServiceSettings.maximumTimeout))

        let locations =
            symbols |> Array.map (Symbols.getTextSpanTrimmed lastIdent)

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
            MessageService.ShowCustomDialog (Dialog.op_Implicit (new Rename.RenameItemDialog("Rename Item", symbol.Symbol.DisplayName, performChanges symbol locations)))
            |> ignore

    let getJumpTypePartSearchResult (location: Range.range) =

        let provider = FindInFiles.FileProvider (location.FileName)
        //let doc = TextEditorFactory.CreateNewDocument ()
        //TODO: This is unfinished...
        //(doc :> ITextDocument).Text <- provider.ReadString ()
        //let fileName, start, finish = Symbols.getTrimmedRangesForDeclarations lastIdent symbolUse

        FindInFiles.SearchResult (provider, 0, 0)

    let jumpToDocId (ctx:DocumentContext) (symbol: FSharpSymbol) =
        async {
            do! Async.SwitchToContext(Runtime.MainSynchronizationContext)
            let! jumped = RefactoringService.TryJumpToDeclarationAsync(symbol.XmlDocSig, ctx.Project)

            if not jumped then
                match symbol.Assembly.FileName with
                | Some filename -> IdeApp.ProjectOperations.JumpToMetadata(filename, symbol.XmlDocSig)
                                   |> ignore
                | None -> ()
            ()
        } |> Async.Start

    let findDeclarationSymbol documentationIdString (symbols: FSharpSymbolUse seq) =
        symbols
        |> Seq.tryFind (fun s -> s.Symbol.XmlDocSig = documentationIdString && s.IsFromDefinition)

    let jumpToDocIdInFSharp documentationIdString =
        async {
            let findById = findDeclarationSymbol documentationIdString
            let result =
                Search.getAllSymbolsInAllProjects()
                |> AsyncSeq.toSeq
                |> findById

            match result with
            | Some symbol -> let location = symbol.RangeAlternate
                             let project = Search.getAllFSharpProjects()
                                           |> Seq.find(fun p -> p.Files |> Seq.exists (fun f -> f.FilePath.ToString () = location.FileName))
                             
                             let context = System.Threading.SynchronizationContext.Current
                             do! Async.SwitchToContext(Runtime.MainSynchronizationContext)
                             IdeApp.Workbench.OpenDocument (Gui.FileOpenInformation (FilePath(location.FileName), project, Line = location.StartLine, Column = location.StartColumn + 1))
                             |> ignore
                             do! Async.SwitchToContext(context)
                             return true
            | _ -> return false
        }

    let jumpTo (editor:TextEditor, ctx:DocumentContext, symbol, location:Range.range) =
        match getSymbolDeclarationLocation symbol editor.FileName ctx.Project.ParentSolution with
        | SymbolDeclarationLocation.CurrentFile ->
            IdeApp.Workbench.OpenDocument (Gui.FileOpenInformation (FilePath(location.FileName), ctx.Project, Line = location.StartLine, Column = location.StartColumn + 1))
            |> ignore

        | SymbolDeclarationLocation.Projects (_projects, _isSymbolLocal) ->
            IdeApp.Workbench.OpenDocument (Gui.FileOpenInformation (FilePath(location.FileName), ctx.Project, Line = location.StartLine, Column = location.StartColumn + 1))
            |> ignore

        | SymbolDeclarationLocation.External _docId ->
            jumpToDocId ctx symbol

        | _ -> ()


    let jumpToDeclaration (editor:TextEditor, ctx:DocumentContext, symbolUse:FSharpSymbolUse) =
        match Symbols.getLocationFromSymbolUse symbolUse with
        | [] -> jumpToDocId ctx symbolUse.Symbol
        | [loc] -> jumpTo (editor, ctx, symbolUse.Symbol, loc)
        | locations ->
                use monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)
                for part in locations do
                    monitor.ReportResult (getJumpTypePartSearchResult (part))
                    |> ignore

    let getProjectOutputFilename config (p:Project) =
        p.GetOutputFileName(config).ToString() |> Path.GetFileNameWithoutExtension

    let getDependentProjects (project:Project) (symbolUse:FSharpSymbolUse) =
      try
          let allProjects = project.ParentSolution.GetAllProjects() |> Seq.toList
          let config = IdeApp.Workspace.ActiveConfiguration
          let currentContextProjectFilename = getProjectOutputFilename config project
          let symbolAssemblyFilename = symbolUse.Symbol.Assembly.SimpleName
          let filteredProjects =
              allProjects
              |> List.filter (fun proj ->
                match proj with
                | :? DotNetProject as dnp ->
                    let projectOutputfilename = getProjectOutputFilename config proj
                    //filter out the current project as this will be included with current dirty source file
                    if currentContextProjectFilename = projectOutputfilename then false
                    //if the symbol and project match include it
                    elif projectOutputfilename = symbolAssemblyFilename then true
                    else
                        let references = dnp.References |> Seq.toArray
                        let matchingProject =
                            references
                            |> Array.tryFind (fun rp ->
                                                let referencedFilenames =
                                                    rp.GetReferencedFileNames config
                                                    |> Array.map Path.GetFileNameWithoutExtension

                                                referencedFilenames
                                                |> Array.contains symbolAssemblyFilename)
                        matchingProject.IsSome
                | _ -> false )

          filteredProjects
          |> List.map (fun p -> p.FileName.ToString() )
      with _ -> []

    let findReferences (editor:TextEditor, ctx:DocumentContext, symbolUse:FSharpSymbolUse, lastIdent) =
        let monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)
        let findAsync = async {
            let dependentProjects = getDependentProjects ctx.Project symbolUse
           
            let! symbolrefs =
                languageService.GetUsesOfSymbolInProject(ctx.Project.FileName.ToString(), editor.FileName.ToString(), editor.Text, symbolUse.Symbol, dependentProjects)

            let distinctRefs =
                symbolrefs
                |> Array.map (Symbols.getOffsetsTrimmed lastIdent)
                |> Seq.distinct

            for (filename, startOffset, endOffset) in distinctRefs do
                let sr = SearchResult (FileProvider (filename), startOffset, endOffset-startOffset)
                monitor.ReportResult sr
        }
        let onComplete _ = monitor.Dispose()
        Async.StartWithContinuations(findAsync, onComplete, onComplete, onComplete)

    let findDerivedReferences (editor:TextEditor, ctx:DocumentContext, symbolUse:FSharpSymbolUse, lastIdent) =
        let monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)
        let findAsync = async {
            let dependentProjects = getDependentProjects ctx.Project symbolUse

            let! symbolrefs =
                languageService.GetDerivedSymbolsInProject(ctx.Project.FileName.ToString(), editor.FileName.ToString(), editor.Text, symbolUse.Symbol, dependentProjects)

            let distinctRefs =
                symbolrefs
                |> Array.map (Symbols.getOffsetsTrimmed lastIdent)
                |> Seq.distinct

            for (filename, startOffset, endOffset) in distinctRefs do
                let sr = SearchResult (FileProvider (filename), startOffset, endOffset-startOffset)
                monitor.ReportResult sr
        }
        let onComplete _ = monitor.Dispose()
        Async.StartWithContinuations(findAsync, onComplete, onComplete, onComplete)

    let findOverloads (editor:TextEditor, _ctx:DocumentContext, symbolUse:FSharpSymbolUse, _lastIdent) =
        let monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)
        let findAsync = async {
            //let dependentProjects = getDependentProjects ctx.Project symbolUse
            let overrides = languageService.GetOverridesForSymbol(symbolUse.Symbol)

            let distinctRefs =
                match overrides with
                | Some meths ->
                    //Note no trimming here as per norm, see lastident above
                    //The compiler is returning no Overloads for F# method overloads currently so expect empty results
                    meths
                    |> Seq.map (fun s -> let declLoc = s.DeclarationLocation
                                         let startOffset = editor.LocationToOffset(declLoc.StartLine, declLoc.StartColumn)
                                         let endOffset = editor.LocationToOffset(declLoc.EndLine, declLoc.EndColumn)
                                         declLoc.FileName, startOffset, endOffset)
                    |> Seq.distinct
                | _ -> Seq.empty

            for (filename, startOffset, endOffset) in distinctRefs do
                let sr = SearchResult (FileProvider (filename), startOffset, endOffset-startOffset)
                monitor.ReportResult sr
        }
        let onComplete _ = monitor.Dispose()
        Async.StartWithContinuations(findAsync, onComplete, onComplete, onComplete)

    let findExtensionMethods (editor:TextEditor, ctx:DocumentContext, symbolUse:FSharpSymbolUse, lastIdent) =
        let monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)
        let findAsync = async {
            let dependentProjects = getDependentProjects ctx.Project symbolUse

            let! symbolrefs =
                languageService.GetExtensionMethods(ctx.Project.FileName.ToString(), editor.FileName.ToString(), editor.Text, symbolUse.Symbol, dependentProjects)

            let distinctRefs =
                symbolrefs
                |> Array.map (Symbols.getOffsetsTrimmed lastIdent)
                |> Seq.distinct

            for (filename, startOffset, endOffset) in distinctRefs do
                let sr = SearchResult (FileProvider (filename), startOffset, endOffset-startOffset)
                monitor.ReportResult sr
        }
        let onComplete _ = monitor.Dispose()
        Async.StartWithContinuations(findAsync, onComplete, onComplete, onComplete)

    module Operations =
        let canRename (symbolUse:FSharpSymbolUse) fileName project =
            match getSymbolDeclarationLocation symbolUse.Symbol fileName project with
            | SymbolDeclarationLocation.External _ -> false
            | SymbolDeclarationLocation.Unknown -> false
            | _ -> true

        let canJump (_symbolUse:FSharpSymbolUse) _currentFile _solution =
            true
            //Reference:
            //For Roslyn the following symbol types *cant* be jumped to:
            //Alias, ArrayType, Assembly, DynamicType, ErrorType, NetModule, NameSpace, PointerType, PreProcessor
            //match symbolUse.Symbol with
            //| :? FSharpMemberOrFunctionOrValue
            //| :? FSharpUnionCase
            //| :? FSharpEntity
            //| :? FSharpField
            //| :? FSharpGenericParameter
            //| :? FSharpActivePatternCase
            //| :? FSharpParameter
            //| :? FSharpStaticParameter ->
            //    match getSymbolDeclarationLocation symbolUse.Symbol currentFile solution with
            //    | SymbolDeclarationLocation.External _ -> true
            //    | SymbolDeclarationLocation.Unknown -> false
            //    | _ -> true
            //| _ -> true

        let canGotoBase (symbolUse:FSharpSymbolUse) =
            match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsDispatchSlot ->
                maybe {
                    let! ent =  mfv.EnclosingEntity
                    let! bt = ent.BaseType
                    return bt.HasTypeDefinition } |> Option.getOrElse (fun () -> false)

            | :? FSharpEntity as ent ->
                match ent.BaseType with
                | Some bt -> bt.HasTypeDefinition
                | _ -> false
            | _ -> false

        let canGotoOverloads (symbolUse:FSharpSymbolUse) =
            match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                mfv.Overloads(false).IsSome
            | _ -> false

        let canFindDerived (symbolUse:FSharpSymbolUse) =
            match symbolUse.Symbol with
            | :? FSharpEntity as entity when not entity.IsFSharpModule -> true
            | :? FSharpMemberOrFunctionOrValue -> true
            | _ -> false

type CurrentRefactoringOperationsHandler() =
    inherit CommandHandler()

    let formatFileName (fileName:string) =
        if fileName |> String.isNullOrEmpty then fileName else
        let fileParts =
            fileName
            |> String.split [| Path.DirectorySeparatorChar;Path.AltDirectorySeparatorChar |]

        match fileParts with
        | [||] -> fileName
        | xs -> xs |> Array.last

    let tryGetValidDoc() =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc = null || doc.FileName = FilePath.Null || doc.ParsedDocument = null then None
        else Some doc

    override x.Run () =
        base.Run ()

    override x.Run (data) =
        let del =  data :?> Action
        if del <> null
        then del.Invoke ()

    override x.Update (ci:CommandInfo) =
        base.Update (ci)

    override x.Update (ainfo:CommandArrayInfo) =
        match tryGetValidDoc() with
        | None -> ()
        | Some doc ->
            if not (FileService.supportedFileName (doc.FileName.ToString())) then ()
            else
                match doc.TryGetAst () with
                | None -> ()
                | Some ast ->
                    match Refactoring.getSymbolAndLineInfoAtCaret ast doc.Editor with
                    | (_line, col, lineTxt), Some symbolUse ->
                        let ciset = new CommandInfoSet (Text = GettextCatalog.GetString ("Refactor"))

                        //last ident part of surrent symbol
                        let lastIdent = Symbols.lastIdent col lineTxt

                        //rename refactoring
                        let canRename = Refactoring.Operations.canRename symbolUse doc.Editor.FileName doc.Project.ParentSolution
                        if canRename then
                            let commandInfo = IdeApp.CommandService.GetCommandInfo (Commands.EditCommands.Rename)
                            commandInfo.Enabled <- true
                            ciset.CommandInfos.Add (commandInfo, Action(fun _ -> (Refactoring.renameSymbol (doc.Editor, doc, lastIdent, symbolUse))))

                        // goto to declaration
                        if Refactoring.Operations.canJump symbolUse doc.Editor.FileName doc.Project.ParentSolution then
                            let locations = Symbols.getLocationFromSymbolUse symbolUse
                            let addCommand() =
                                let commandInfo = IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration)
                                commandInfo.Enabled <- true
                                ainfo.Add (commandInfo, Action (fun _ -> Refactoring.jumpToDeclaration (doc.Editor, doc, symbolUse) ))

                            match locations with
                            | [] -> addCommand()
                            | [_location] -> addCommand()
                            | locations ->
                                let declSet = CommandInfoSet (Text = GettextCatalog.GetString ("_Go to Declaration"))
                                for location in locations do
                                    let commandText = String.Format (GettextCatalog.GetString ("{0}, Line {1}", formatFileName location.FileName, location.StartLine))
                                    declSet.CommandInfos.Add (commandText, Action (fun () -> Refactoring.jumpTo (doc.Editor, doc, symbolUse.Symbol, location)))
                                    |> ignore
                                ainfo.Add (declSet)

                        //goto base
                        if Refactoring.Operations.canGotoBase symbolUse then
                            let baseSymbol = Refactoring.getBaseSymbol symbolUse
                            match baseSymbol with
                            | Some bs ->
                                let symbol = bs
                                //let _baseIdent = symbol.DisplayName
                                let locations = Symbols.getLocationFromSymbol symbol
                                match locations with
                                | [] -> ()
                                | [location] ->
                                    let description =
                                        match symbolUse.Symbol with
                                        | :? FSharpEntity ->
                                            GettextCatalog.GetString ("Go to _Base Type")
                                        | :? FSharpMemberOrFunctionOrValue as mfv ->
                                            match mfv.EnclosingEntity with
                                            | Some ent when ent.IsInterface ->
                                                if mfv.IsProperty then GettextCatalog.GetString ("Go to _Interface Property")
                                                elif mfv.IsEvent then GettextCatalog.GetString ("Go to _Interface Event")
                                                else GettextCatalog.GetString ("Go to _Interface Method")
                                            | _ ->
                                                if mfv.IsProperty then  GettextCatalog.GetString ("Go to _Base Property")
                                                elif mfv.IsEvent then GettextCatalog.GetString ("Go to _Base Event")
                                                else GettextCatalog.GetString ("Go to _Base Method")
                                  
                                        | _-> GettextCatalog.GetString ("Go to _Base Symbol")
                                    ainfo.Add (description, Action (fun () -> Refactoring.jumpTo (doc.Editor, doc, symbol, location)))
                                    |> ignore

                                | locations ->
                                    let declSet = CommandInfoSet (Text = GettextCatalog.GetString ("Go to _Base Symbol"))
                                    for location in locations do
                                        let commandText = String.Format (GettextCatalog.GetString ("{0}, Line {1}"), formatFileName location.FileName, location.StartLine)
                                        declSet.CommandInfos.Add (commandText, Action (fun () -> Refactoring.jumpTo (doc.Editor, doc, symbol, location)))
                                        |> ignore
                                    ainfo.Add (declSet)
                            | _ -> ()
                        
                        let findReferences() =
                            match symbolUse with
                            | Val _local -> Refactoring.findReferences (doc.Editor, doc, symbolUse, lastIdent)
                            | _ -> RefactoringService.FindReferencesAsync(symbolUse.Symbol.XmlDocSig) |> ignore

                        //find references
                        let findReferencesCommand = IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences)
                        findReferencesCommand.Enabled <- true
                        ainfo.Add (findReferencesCommand, Action(fun () -> findReferences()))

                        //TODO: this one finds all overloads of a given symbol
                        //All that needs to happen here is to pass all dependent project infos
                        //ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), Action (fun () -> Refactoring.FindRefs (sym)))

                        // find derived symbols
                        if Refactoring.Operations.canFindDerived symbolUse then
                            let description =
                                match symbolUse.Symbol with
                                | :? FSharpEntity as fse when fse.IsInterface -> GettextCatalog.GetString ("Find Implementing Types")
                                | :? FSharpEntity -> GettextCatalog.GetString ("Find Derived Types")
                                | :? FSharpMemberOrFunctionOrValue as mfv ->
                                    match mfv.EnclosingEntity with
                                    | Some ent when ent.IsInterface ->
                                        GettextCatalog.GetString ("Find Implementing Symbols")
                                    | _ -> GettextCatalog.GetString ("Find overriden Symbols")
                                | _ -> GettextCatalog.GetString ("Find Derived Symbols")
                            ainfo.Add (description, Action (fun () -> Refactoring.findDerivedReferences (doc.Editor, doc, symbolUse, lastIdent))) |> ignore

                        //find overloads
                        if Refactoring.Operations.canGotoOverloads symbolUse then
                            let description = GettextCatalog.GetString ("Find Method Overloads")
                            ainfo.Add (description, Action (fun () -> Refactoring.findOverloads (doc.Editor, doc, symbolUse, lastIdent))) |> ignore

                        //find type extensions
                        if symbolUse.Symbol :? FSharpEntity then
                            let extMethodDescription = GettextCatalog.GetString ("Find Type Extensions")
                            ainfo.Add (extMethodDescription, Action (fun () -> Refactoring.findExtensionMethods (doc.Editor, doc, symbolUse, lastIdent))) |> ignore

                        if ciset.CommandInfos.Count > 0 then
                            ainfo.Add (ciset, null)
                    | _ -> ()

type FindReferencesHandler() =
    inherit CommandHandler()

    member x.Run (editor:TextEditor, ctx:DocumentContext) =
        if FileService.supportedFilePath editor.FileName then
            match ctx.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                //Is this a double check, i.e. isnt update checking can rename?
                | (_line, col, lineTxt), Some sym ->
                    let lastIdent = Symbols.lastIdent col lineTxt
                    Refactoring.findReferences (editor, ctx, sym, lastIdent)
                | _ -> ()
            | _ -> ()
    
type RenameHandler() =
    inherit CommandHandler()

    override x.Update (ci:CommandInfo) =
        let doc = IdeApp.Workbench.ActiveDocument
        let editor = doc.Editor
        //skip if theres no editor or filename
        if editor = null || editor.FileName = FilePath.Null
        then ci.Bypass <- false
        else
            if not (FileService.supportedFilePath editor.FileName) then ci.Bypass <- true
            else
                match doc.TryGetAst() with
                | Some ast ->
                    match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                    //set bypass is we cant rename
                    | _lineinfo, Some sym when not (Refactoring.Operations.canRename sym editor.FileName doc.Project.ParentSolution) ->
                        ci.Bypass <- true
                    | _lineinfo, _symbol -> ()
                //disable for no ast
                | None ->
                    ci.Bypass <- true

    member x.UpdateCommandInfo(ci:CommandInfo) = x.Update(ci)

    override x.Run (_data) =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc <> null || doc.FileName <> FilePath.Null || not (FileService.supportedFilePath doc.FileName) then
            x.Run (doc.Editor, doc)

    member x.Run (editor:TextEditor, ctx:DocumentContext) =
        if FileService.supportedFilePath editor.FileName then
            match ctx.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                //Is this a double check, i.e. isnt update checking can rename?
                | (_line, col, lineTxt), Some sym when Refactoring.Operations.canRename sym editor.FileName ctx.Project.ParentSolution ->
                    let lastIdent = Symbols.lastIdent col lineTxt
                    Refactoring.renameSymbol (editor, ctx, lastIdent, sym)
                | _ -> ()
            | None -> ()

open ExtCore
type GotoDeclarationHandler() =
    inherit CommandHandler()

    member x.UpdateCommandInfo(ci:CommandInfo) =
        x.Update(ci)

    override x.Update (ci:CommandInfo) =
        let doc = IdeApp.Workbench.ActiveDocument
        let editor = doc.Editor
        //skip if theres no editor or filename
        if editor = null || editor.FileName = FilePath.Null then ci.Bypass <- true
        elif not (FileService.supportedFilePath editor.FileName) then ci.Bypass <- true
        else
            match doc.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                //set bypass as we cant jump
                | _lineinfo, Some sym when not (Refactoring.Operations.canJump sym editor.FileName doc.Project.ParentSolution) ->
                    ci.Bypass <- true
                | _lineinfo, _symbol -> ()
            //disable for no ast
            | None -> ci.Bypass <- true

    override x.Run (_data) =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc <> null || doc.FileName <> FilePath.Null || not (FileService.supportedFilePath doc.FileName) then
            x.Run(doc.Editor, doc)

    member x.Run(editor, context:DocumentContext) =
        if FileService.supportedFileName (editor.FileName.ToString()) then
            match context.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                | (_line, _col, _lineTxt), Some symbolUse when Refactoring.Operations.canJump symbolUse editor.FileName context.Project.ParentSolution ->
                    Refactoring.jumpToDeclaration (editor, context, symbolUse)
                | _ -> ()
            | _ -> ()

type FSharpJumpToDeclarationHandler () =
    inherit JumpToDeclarationHandler ()

    override x.TryJumpToDeclarationAsync(documentationIdString, _hintProject, token) =
        async {
                // We only need to run this when the editor isn't F#
                match IdeApp.Workbench.ActiveDocument with
                | null -> return false
                | doc when FileService.supportedFileName (doc.FileName.ToString()) -> return false
                | _doc -> return! Refactoring.jumpToDocIdInFSharp documentationIdString

        }
        |> StartAsyncAsTask token

type FSharpFindReferencesProvider () =
    inherit FindReferencesProvider ()

    override x.FindReferences(documentationCommentId, _hintProject, token) =
        async {
            return
                Search.getAllSymbolsInAllProjects()
                |> AsyncSeq.toSeq
                |> Seq.toArray

                |> Array.filter (fun symbol -> symbol.Symbol.XmlDocSig = documentationCommentId)
                |> Array.map (fun symbol -> let (filename, startOffset, endOffset) = Symbols.getOffsetsTrimmed symbol.Symbol.DisplayName symbol
                                            SearchResult (FileProvider (filename), startOffset, endOffset-startOffset))
                |> Array.toSeq
        }
        |> StartAsyncAsTask token

    override x.FindAllReferences(_documentationCommentId, _hintProject, _token) =
        //TODO:
        Task.FromResult Seq.empty

type FSharpCommandsTextEditorExtension () =
    inherit Editor.Extension.TextEditorExtension ()
    static member SupportedFileExtensions =
        [".fsscript"; ".fs"; ".fsx"; ".fsi"; ".sketchfs"]

    override x.IsValidInContext (context) =
        context.Name <> null && FileService.supportedFileName context.Name

    [<CommandUpdateHandler("MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration")>]
    member x.GotoDeclarationCommand_Update(ci:CommandInfo) =
        GotoDeclarationHandler().UpdateCommandInfo (ci)

    [<CommandHandler ("MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration")>]
    member x.GotoDeclarationCommand () =
        GotoDeclarationHandler().Run(x.Editor, x.DocumentContext)

    [<CommandUpdateHandler("MonoDevelop.Ide.Commands.EditCommands.Rename")>]
    member x.RenameCommand_Update(ci:CommandInfo) =
        RenameHandler().UpdateCommandInfo (ci)

    [<CommandHandler ("MonoDevelop.Ide.Commands.EditCommands.Rename")>]
    member x.RenameCommand () =
        RenameHandler().Run (x.Editor, x.DocumentContext)

    [<CommandHandler ("MonoDevelop.Refactoring.RefactoryCommands.FindReferences")>]
    member x.FindReferences () =
        FindReferencesHandler().Run(x.Editor, x.DocumentContext)
