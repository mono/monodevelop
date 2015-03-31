namespace MonoDevelop.FSharp
open System
open System.IO
open System.Collections.Generic
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Projects
open MonoDevelop.Refactoring
open FSharp.CompilerBinding
open Microsoft.FSharp.Compiler.SourceCodeServices

module Refactoring =

    type SymbolDeclarationLocation =
    | CurrentFile
    | Projects of Project seq * isSymbolLocal:bool
    | External of string
    | Unknown      

    let performChanges (symbol:FSharpSymbolUse) (locations:array<string * Microsoft.CodeAnalysis.Text.TextSpan> ) =
        Func<_,_>(fun (renameProperties:Rename.RenameRefactoring.RenameProperties) -> 
        let results =
            use monitor = new ProgressMonitoring.MessageDialogProgressMonitor (true, false, false, true)
            [|
                if renameProperties.RenameFile && symbol.IsFromType then
                    yield!
                        // TODO check .fsi file in renames?
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

    let getDocumentationId (symbolUse:FSharpSymbolUse) =
        let id = ""
        id

    let getSymbolDeclarationLocation (symbolUse: FSharpSymbolUse) (currentFile: FilePath) (solution: Solution) =
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
                    let allProjects = solution.GetAllProjects ()
                    match allProjects
                          |> Seq.filter (fun p -> p.Files |> Seq.exists (fun f -> f.FilePath.ToString () = filePath)) with
                    | projects when projects |> Seq.isEmpty ->
                        External (getDocumentationId symbolUse)
                    | projects -> SymbolDeclarationLocation.Projects (projects, isSymbolLocalForProject)
            | None -> SymbolDeclarationLocation.Unknown

    let canRename (symbolUse:FSharpSymbolUse) fileName project =
        match getSymbolDeclarationLocation symbolUse fileName project with
        | SymbolDeclarationLocation.External _ -> false
        | SymbolDeclarationLocation.Unknown -> false
        | _ -> true

    let canJump (symbolUse:FSharpSymbolUse) currentFile solution =
        //Reference:
        //For Roslyn the following symbol types *cant* be jumped to: 
        //Alias, ArrayType, Assembly, DynamicType, ErrorType, NetModule, NameSpace, PointerType, PreProcessor
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue
        | :? FSharpUnionCase
        | :? FSharpEntity
        | :? FSharpField
        | :? FSharpGenericParameter
        | :? FSharpActivePatternCase
        | :? FSharpParameter
        | :? FSharpStaticParameter ->
            match getSymbolDeclarationLocation symbolUse currentFile solution with
            | SymbolDeclarationLocation.External _ -> true
            | SymbolDeclarationLocation.Unknown -> false
            | _ -> true

        //The following are represented by a basic FSharpSymbol, do we want to have goto on these?
        // ImplicitOp, ILField , FakeInterfaceCtor, NewDef

        // These cases cover unreachable cases
        // CustomOperation, UnqualifiedType, ModuleOrNamespaces, Property, MethodGroup, CtorGroup

        // These cases cover misc. corned cases (non-symbol types)
        // Types, DelegateCtor
        | _ -> false
        
    let canGotoBase (symbolUse:FSharpSymbolUse) =
        match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsDispatchSlot -> true
            | :? FSharpUnionCase as unioncase -> true
            | :? FSharpActivePatternCase as apc -> true
            | :? FSharpEntity as ent when ent.BaseType.IsSome -> true
            //| :? FSharpField 
            //| :? FSharpGenericParameter
            //| :? FSharpParameter
            //| :? FSharpStaticParameter ->
            | _ -> false
    
    let getSymbolAndLineInfoAtCaret (ast: ParseAndCheckResults) (editor:TextEditor) =
        let lineInfo = editor.GetLineInfoByCaretOffset ()
        let symbol = ast.GetSymbolAtLocation lineInfo |> Async.RunSynchronously
        lineInfo, symbol

type FSharpRefactoring(editor:TextEditor, ctx:DocumentContext) =

    member x.Rename (lastIdent, symbol:FSharpSymbolUse) =         
        let symbols = 
            let activeDocFileName = editor.FileName.ToString ()
            let projectFilename, projectFiles, projectArgs = MonoDevelop.getCheckerArgs(ctx.Project, activeDocFileName)
            MDLanguageService.Instance.GetUsesOfSymbolInProject (projectFilename, activeDocFileName, editor.Text, projectFiles, projectArgs, symbol.Symbol)
            |> Async.RunSynchronously

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
            MessageService.ShowCustomDialog (new Rename.RenameItemDialog ("Rename Item", symbol.Symbol.DisplayName, Refactoring.performChanges symbol locations))
            |> ignore

        member x.GetJumpTypePartSearchResult (lastIdent, symbolUse:FSharpSymbolUse, location: Microsoft.FSharp.Compiler.Range.range) =
        
            let provider = MonoDevelop.Ide.FindInFiles.FileProvider (location.FileName)
            let doc = TextEditorFactory.CreateNewDocument ()
            //TODO: This is unfinished...
            //(doc :> ITextDocument).Text <- provider.ReadString ()
            //let fileName, start, finish = Symbols.getTrimmedRangesForDeclarations lastIdent symbolUse
                        
            MonoDevelop.Ide.FindInFiles.SearchResult (provider, 0, 0)
        

        member x.JumpTo (lastIdent:string, symbolUse, location:Microsoft.FSharp.Compiler.Range.range) =
            match Refactoring.getSymbolDeclarationLocation symbolUse editor.FileName ctx.Project.ParentSolution with
            | Refactoring.SymbolDeclarationLocation.CurrentFile ->
                IdeApp.Workbench.OpenDocument (MonoDevelop.Ide.Gui.FileOpenInformation (FilePath(location.FileName), ctx.Project, Line = location.StartLine, Column = location.StartColumn))
                |> ignore
                
            | Refactoring.SymbolDeclarationLocation.Projects (_projects, isSymbolLocal) when isSymbolLocal ->
                IdeApp.Workbench.OpenDocument (MonoDevelop.Ide.Gui.FileOpenInformation (FilePath(location.FileName), ctx.Project, Line = location.StartLine, Column = location.StartColumn))
                |> ignore

            | Refactoring.SymbolDeclarationLocation.External docId ->
                if (editor <> null) then
                    editor.RunWhenLoaded (fun () ->
                        //this is the assembly browser widget
                        let handler = editor.GetContent<MonoDevelop.Ide.Gui.Content.IOpenNamedElementHandler> ()
                        if handler <> null then handler.Open (docId))
            | _ -> ()    

                
        member x.JumpToDeclaration (lastIdent:string, symbolUse:FSharpSymbolUse, location) =
            match Roslyn.getSymbolLocations symbolUse with
            | [] -> ()
            | [loc] -> x.JumpTo (lastIdent, symbolUse, loc)
            | locations ->
                    use monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)
                    for part in locations do
                        ignore <| monitor.ReportResult (x.GetJumpTypePartSearchResult (lastIdent, symbolUse, part))

type CurrentRefactoringOperationsHandler() =
    inherit CommandHandler()

    let formatFileName (fileName:string) =
        if fileName |> String.isNullOrEmpty then fileName else
        let fileParts =
            fileName
            |> String.split [|Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar|]

        match fileParts with
        | [||] -> fileName
        | xs -> xs |> Array.last

    let tryGetValidDoc =
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
        match tryGetValidDoc with
        | None -> ()
        | Some doc ->
            if not (MDLanguageService.SupportedFileName (doc.FileName.ToString())) then ()
            else
                match doc.ParsedDocument.TryGetAst () with
                | None -> ()
                | Some ast ->
                    match Refactoring.getSymbolAndLineInfoAtCaret ast doc.Editor with
                    | (_line, col, lineTxt), Some symbolUse ->
                        let ciset = new CommandInfoSet (Text = GettextCatalog.GetString ("Refactor"))
            
                        //last ident part of surrent symbol
                        let lastIdent = Symbols.lastIdent col lineTxt
            
                        //rename refactoring
                        let canRename = Refactoring.canRename symbolUse doc.Editor.FileName doc.Project.ParentSolution
                        if canRename then
                            let commandInfo = IdeApp.CommandService.GetCommandInfo (Commands.EditCommands.Rename)
                            commandInfo.Enabled <- true
            
                            ciset.CommandInfos.Add (commandInfo, Action(fun _ -> (FSharpRefactoring (doc.Editor, doc)).Rename (lastIdent, symbolUse)))
            
                        // jump to declaration
                        if Refactoring.canJump symbolUse doc.Editor.FileName doc.Project.ParentSolution then
                            let locations = Roslyn.getSymbolLocations symbolUse
                            match locations with
                            | [] -> ()
                            | [location] ->
                                ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration),
                                                                                 Action (fun () -> FSharpRefactoring(doc.Editor, doc).JumpToDeclaration (lastIdent, symbolUse, location) ))
                            | locations ->
                                let declSet = CommandInfoSet (Text = GettextCatalog.GetString ("_Go to Declaration"))
                                for location in locations do
                  
                                    let commandText = String.Format (GettextCatalog.GetString ("{0}, Line {1}"), formatFileName location.FileName, location.StartLine)
                                    declSet.CommandInfos.Add (commandText, Action (fun () -> FSharpRefactoring(doc.Editor, doc).JumpTo (lastIdent, symbolUse, location)))
                                    |> ignore
                                ainfo.Add (declSet)
                        
                        //TODO:goto base, find references, find derived symbols, find overloads, find extension methods, find type extensions
            
                        if ciset.CommandInfos.Count > 0 then
                            ainfo.Add (ciset, null)
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
            if not (MDLanguageService.SupportedFilePath editor.FileName) then ci.Bypass <- true
            else
                match doc.ParsedDocument.TryGetAst() with
                | Some ast ->
                    match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                    //set bypass is we cant rename
                    | _lineinfo, Some sym when not (Refactoring.canRename sym editor.FileName doc.Project.ParentSolution) ->
                        ci.Bypass <- true
                    | _lineinfo, _symbol -> ()
                //disable for no ast
                | None ->
                    ci.Bypass <- true

    member x.UpdateCommandInfo(ci:CommandInfo) = x.Update(ci)
        
    override x.Run (_data) =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc <> null || doc.FileName <> FilePath.Null || not (MDLanguageService.SupportedFilePath doc.FileName) then
            x.Run (doc.Editor, doc)

    member x.Run ( editor:TextEditor, ctx:DocumentContext) =
        if MDLanguageService.SupportedFilePath editor.FileName then 
            match ctx.ParsedDocument.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                //Is this a double check, i.e. isnt update checking can rename?
                | (_line, col, lineTxt), Some sym when Refactoring.canRename sym editor.FileName ctx.Project.ParentSolution ->
                    let rr = FSharpRefactoring(editor, ctx)
                    let lastIdent = Symbols.lastIdent col lineTxt
                    rr.Rename (lastIdent, sym)
                | _ -> ()
            | _ -> ()


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
        elif not (MDLanguageService.SupportedFilePath editor.FileName) then ci.Bypass <- true
        else
            match doc.ParsedDocument.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                //set bypass as we cant jump
                | _lineinfo, Some sym when not (Refactoring.canJump sym editor.FileName doc.Project.ParentSolution) ->
                    ci.Bypass <- true
                | _lineinfo, _symbol -> ()
            //disable for no ast
            | None -> ci.Bypass <- true

    override x.Run (_data) =
        let doc = IdeApp.Workbench.ActiveDocument
        if doc <> null || doc.FileName <> FilePath.Null || not (MDLanguageService.SupportedFilePath doc.FileName) then
            x.Run(doc.Editor, doc)
            
    member x.Run(editor, context:DocumentContext) =
        if MDLanguageService.SupportedFileName (editor.FileName.ToString()) then
            match context.ParsedDocument.TryGetAst() with
            | Some ast ->
                match Refactoring.getSymbolAndLineInfoAtCaret ast editor with
                | (_line, col, lineTxt), Some symbolUse when Refactoring.canJump symbolUse editor.FileName context.Project.ParentSolution ->
                        let lastIdent = Symbols.lastIdent col lineTxt
                        match Roslyn.getSymbolLocations symbolUse with
                        //We only jump to the first declaration location with this command handler, which is invoked via Cmd D
                        //We could intelligently swich by jumping to the second location if we are already at the first
                        //We could also provide a selection like context menu does.  For now though, we mirror C#
                        | first :: _ -> FSharpRefactoring(editor, context).JumpToDeclaration (lastIdent, symbolUse, first)
                        | [] -> ()
                | _ -> ()
            | _ -> ()

type FSharpCommandsTextEditorExtension () =
    inherit MonoDevelop.Ide.Editor.Extension.TextEditorExtension ()

    override x.IsValidInContext (context) =
        context.Name <> null && MDLanguageService.SupportedFileName context.Name

    [<CommandUpdateHandler(MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration)>]
    member x.GotoDeclarationCommand_Update(ci:CommandInfo) =
        GotoDeclarationHandler().UpdateCommandInfo (ci)
    
    [<CommandHandler (MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration)>]
    member x.GotoDeclarationCommand () =
        GotoDeclarationHandler().Run(x.Editor, x.DocumentContext)
        
    [<CommandUpdateHandler(MonoDevelop.Ide.Commands.EditCommands.Rename)>]
    member x.RenameCommand_Update(ci:CommandInfo) =
        RenameHandler().UpdateCommandInfo (ci)
    
    [<CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Rename)>]
    member x.RenameCommand () =
        RenameHandler().Run (x.Editor, x.DocumentContext)