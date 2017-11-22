namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Linq
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore.Control

type FSharpPathExtension() as x =
    inherit TextEditorExtension()

    let pathChanged = new Event<_,_>()
    let mutable currentPath = [||]
    let mutable subscriptions = ResizeArray<IDisposable>()
    let mutable caretSubscription = None
    let ownerProjects = ResizeArray<DotNetProject>()

    let textChanging _args =
        //remove caret subscription
        match caretSubscription with
        | Some (s:IDisposable) ->
            s.Dispose()
            caretSubscription <- None
        | None -> ()

    let caretPositionChanged _args =
        x.Update()

    let subscribeCaretChange() =
        match caretSubscription with
        | Some _ -> ()
        | None -> caretSubscription <- Some(x.Editor.CaretPositionChanged.Subscribe(caretPositionChanged))

    let docParsed _args =
        subscribeCaretChange()

    let handleStartupProjectChanged = EventHandler(fun sender args ->
        // If the startup project changes, and the new startup project is an owner of this document, then attach the document to that project
        sender
        |> Option.tryCast<Solution>
        |> Option.map (fun sol -> sol.StartupItem)
        |> Option.tryCast<DotNetProject>
        |> Option.bind (Option.condition ownerProjects.Contains)
        |> Option.iter x.DocumentContext.AttachToProject)

    let getSolutions() =
        ownerProjects |> Seq.choose (fun p -> p.ParentSolution |> Option.ofNull) |> Seq.distinct

    let untrackStartupProjectChanges () =
        getSolutions()
        |> Seq.choose (fun s -> s.StartupItemChanged |> Option.ofNull)
        |> Seq.iter (fun e -> e.RemoveHandler handleStartupProjectChanged)

    let trackStartupProjectChanges() =
        getSolutions()
        |> Seq.choose (fun s -> s.StartupItemChanged |> Option.ofNull)
        |> Seq.iter (fun e -> e.AddHandler handleStartupProjectChanged)

    let setOwnerProjects (projects) =
        untrackStartupProjectChanges ()
        ownerProjects.Clear()
        ownerProjects.AddRange projects
        trackStartupProjectChanges ()

    let updateOwnerProjects (allProjects:DotNetProject seq) =
        let projects = HashSet(allProjects |> Seq.filter (fun p -> p.IsFileInProject (x.DocumentContext.Name)))
        if not (projects.SetEquals ownerProjects) then
            setOwnerProjects (projects |> Seq.sortBy(fun p -> p.Name))
            x.DocumentContext.Project
            |> Option.ofNull
            |> Option.filter (fun _ -> ownerProjects.Count > 0)
            |> Option.iter (fun project ->
                let parentSln =
                    match project with
                    | :? DotNetProject as dnp ->
                        //if the ownerprojects does NOT contain this project then find a default
                        if not (ownerProjects.Contains dnp) then Some project.ParentSolution else None
                          // If the project for the document is NOT a DotNetProject but there
                          // is a project containing this file in the current solution, then use that project
                    | _ -> Some project.ParentSolution

                parentSln
                |> Option.bind (fun s -> x.FindBestDefaultProject(s))
                |> Option.iter x.DocumentContext.AttachToProject)

    let resetOwnerProject() =
        if ownerProjects.Count > 0 then
            x.FindBestDefaultProject () |> Option.iter x.DocumentContext.AttachToProject

    let updateOwnerProjectsWithReset () =
        IdeApp.Workspace
        |> Option.ofObj
        |> Option.iter(fun w -> updateOwnerProjects (w.GetAllItems<DotNetProject> ()))

        x.DocumentContext
        |> Option.ofObj
        |> Option.iter (fun context ->
            match context.Project with
            | null -> resetOwnerProject ()
            | _ -> ())

    let projectChanged _args =
        updateOwnerProjectsWithReset()
        caretPositionChanged ()

    let workspaceItemLoaded (e:WorkspaceItemEventArgs) =
        updateOwnerProjects (e.Item.GetAllItems<DotNetProject>())

    let removeOwnerProject(project:DotNetProject) =
        untrackStartupProjectChanges ()
        ownerProjects.Remove (project) |> ignore
        trackStartupProjectChanges ()

    let workspaceItemUnloaded (e:WorkspaceItemEventArgs) =
        if ownerProjects.Count = 0 then
            ownerProjects.Clear()
            x.DocumentContext.AttachToProject (null)
        else
            for p in e.Item.GetAllItems<DotNetProject> () do
                removeOwnerProject (p)

    let activeConfigurationChanged _args =
        // If the current configuration changes and the project to which this document is bound is disabled in the
        // new configuration, try to find another project
        match x.DocumentContext.Project with
        | null -> ()
        | project when project.ParentSolution = IdeApp.ProjectOperations.CurrentSelectedSolution ->
            match project.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) with
            | null -> ()
            | conf when not (conf.BuildEnabledForItem (project)) -> resetOwnerProject ()
            | _ -> ()
        | _ -> ()

    member x.OwnerProjects = ownerProjects

    member x.FindBestDefaultProject (?solution : Solution) =
        // The best candidate to be selected as default project for this document is the startup project.
        // If the startup project is not an owner, pick any project that is not disabled in the current configuration.
        let solution = defaultArg solution IdeApp.ProjectOperations.CurrentSelectedSolution

        let best =
            let sameParentSlnAndBuilt =
                ownerProjects
                |> Seq.filter (fun p -> let solutionMatch = p.ParentSolution = solution
                                        let config = p.ParentSolution.GetConfiguration(IdeApp.Workspace.ActiveConfiguration)
                                        let buildEnabled = config.BuildEnabledForItem(p)
                                        solutionMatch && buildEnabled && p.LanguageName = "F#")

            sameParentSlnAndBuilt
            |> Seq.tryFind(fun p -> p = (p.ParentSolution.StartupItem :?> DotNetProject))
            |> Option.coalesce (sameParentSlnAndBuilt |> Seq.tryHead)

        best
        |> Option.coalesce (ownerProjects |> Seq.tryFind (fun p -> p.ParentSolution = solution))
        |> Option.coalesce (ownerProjects |> Seq.tryHead)


    member x.Document = base.Editor
    member x.GetEntityMarkup(node: FSharpNavigationDeclarationItem) =
        let name = node.Name.Split('.')
        if name.Length > 0 then name.Last()
        else node.Name

    override x.Initialize() =
        currentPath <- [| new PathEntry("No selection", Tag = null) |]
        // Delay the execution of UpdateOwnerProjects since it may end calling DocumentContext.AttachToProject,
        // which shouldn't be called while the extension chain is being initialized.
        Gtk.Application.Invoke (fun _ _ -> updateOwnerProjectsWithReset()
                                           caretPositionChanged() )

        let workspace = IdeApp.Workspace |> Option.ofObj
        subscriptions.AddRange
            [ yield x.Editor.TextChanging.Subscribe(textChanging)
              yield x.DocumentContext.DocumentParsed.Subscribe(docParsed)
              if workspace.IsSome then
                  let ws = workspace.Value
                  yield ws.FileAddedToProject.Subscribe(projectChanged)
                  yield ws.FileRemovedFromProject.Subscribe(projectChanged)
                  yield ws.ItemAddedToSolution.Subscribe(projectChanged)
                  yield ws.WorkspaceItemUnloaded.Subscribe(workspaceItemUnloaded)
                  yield ws.WorkspaceItemLoaded.Subscribe(workspaceItemLoaded)
                  yield ws.ActiveConfigurationChanged.Subscribe(activeConfigurationChanged) ]
        subscribeCaretChange()

    member private x.Update() =
        match IdeApp.Workbench with
        | null -> ()
        | _ ->
        match IdeApp.Workbench.ActiveDocument with
        | null -> ()
        | context when context.Name <> x.DocumentContext.Name -> ()
        | _ ->
      
        let ast =
            maybe { let! context = x.DocumentContext |> Option.ofNull
                    let! parsedDocument = context.ParsedDocument |> Option.ofNull
                    let! ast = parsedDocument.Ast |> Option.tryCast<ParseAndCheckResults>
                    return ast }
      
        let caretLocation = x.Editor.CaretLocation
        ast |> Option.iter (fun ast ->
            let posGt (p1Column, p1Line) (p2Column, p2Line) =
                (p1Line > p2Line || (p1Line = p2Line && p1Column > p2Column))
      
            let posEq (p1Column, p1Line) (p2Column, p2Line) =
                (p1Line = p2Line &&  p1Column = p2Column)
      
            let posGeq p1 p2 =
                posEq p1 p2 || posGt p1 p2
      
            let isInside (docloc:Editor.DocumentLocation) (start, finish) =
                let cursor = (docloc.Column, docloc.Line)
                posGeq cursor start && posGeq finish cursor
      
            let toplevel = ast.GetNavigationItems()
      
            let topLevelTypesInsideCursor =
                toplevel
                |> Array.filter (fun tl -> let range = tl.Declaration.Range
                                           isInside caretLocation ((range.StartColumn, range.StartLine),(range.EndColumn, range.EndLine)))
                |> Array.sortBy(fun xs -> xs.Declaration.Range.StartLine)
      
            let newPath = ResizeArray<_>()
      
            if ownerProjects.Count > 1 then
                let p = x.DocumentContext.Project
                newPath.Add (new PathEntry(icon = ImageService.GetIcon(p.StockIcon.Name, Gtk.IconSize.Menu),
                                           markup = GLib.Markup.EscapeText (p.Name),
                                           Tag=p))
      
            for top in topLevelTypesInsideCursor do
                let name = top.Declaration.Name
                let navitems =
                    if name.Contains(".") then
                        let nameparts = name.[.. name.LastIndexOf(".")]
                        toplevel |> Array.filter (fun decl -> decl.Declaration.Name.StartsWith(nameparts))
                    else toplevel
      
                newPath.Add(new PathEntry(icon = ImageService.GetIcon(ServiceUtils.getIcon top.Declaration, Gtk.IconSize.Menu),
                                          markup =x.GetEntityMarkup(top.Declaration),
                                          Tag = navitems))
      
            if topLevelTypesInsideCursor.Length > 0 then
                let lastToplevel = topLevelTypesInsideCursor.Last()
                //only first child found is returned, could there be multiple children found?
                let child =
                    lastToplevel.Nested
                    |> Array.tryFind (fun tl -> let range = tl.Range
                                                isInside caretLocation ((range.StartColumn, range.StartLine),(range.EndColumn, range.EndLine)))
                match child with
                | Some(c) -> newPath.Add(new PathEntry(icon = ImageService.GetIcon(ServiceUtils.getIcon c, Gtk.IconSize.Menu),
                                                       markup = x.GetEntityMarkup(c),
                                                       Tag = lastToplevel))
                | None -> newPath.Add(new PathEntry("No selection", Tag = lastToplevel))
      
            let previousPath = currentPath
            //ensure the path has changed from the previous one before setting and raising event.
            let samePath = Seq.forall2 (fun (p1:PathEntry) (p2:PathEntry) -> p1.Markup = p2.Markup) previousPath newPath
            if not samePath then
                if newPath.Count = 0 then currentPath <- [|new PathEntry("No selection", Tag = null)|]
                else currentPath <- newPath.ToArray()
      
                //invoke pathChanged
                pathChanged.Trigger(x, DocumentPathChangedEventArgs(previousPath)))

    override x.Dispose() =
        for disposable in subscriptions do disposable.Dispose()
        subscriptions.Clear()
        caretSubscription |> Option.iter (fun d -> d.Dispose())

    interface IPathedDocument with
        member x.CurrentPath = currentPath
        member x.CreatePathWidget(index) =
            let path = (x :> IPathedDocument).CurrentPath
            if path = null || index < 0 || index >= path.Length then null else
            let tag = path.[index].Tag
            let window = new DropDownBoxListWindow(FSharpDataProvider(x, tag), FixedRowHeight=22, MaxVisibleRows=14)
            window.SelectItem (path.[index].Tag)
            Control.op_Implicit window

        member x.add_PathChanged(handler) = pathChanged.Publish.AddHandler(handler)
        member x.remove_PathChanged(handler) = pathChanged.Publish.RemoveHandler(handler)


and FSharpDataProvider(ext:FSharpPathExtension, tag) =
    let memberList = ResizeArray<_>()

    let reset() =
        memberList.Clear()
        match tag with
        | :? array<FSharpNavigationTopLevelDeclaration> as navitems ->
            for decl in navitems do
                memberList.Add(decl.Declaration)
        | :? FSharpNavigationTopLevelDeclaration as tld ->
            memberList.AddRange(tld.Nested)
        | _ -> ()

    do reset()

    interface DropDownBoxListWindow.IListDataProvider with
        member x.IconCount =
            if tag :? DotNetProject then ext.OwnerProjects.Count
            else memberList.Count

        member x.Reset() = reset()

        member x.GetTag (n) =
            if tag :? DotNetProject then ext.OwnerProjects.[n] :> obj
            else memberList.[n] :> obj

        member x.ActivateItem(n) =
            if tag :? DotNetProject then
                ext.DocumentContext.AttachToProject (ext.OwnerProjects.[n])
            else
                let node = memberList.[n]
                let extEditor = ext.DocumentContext.GetContent<Editor.TextEditor>()
                if extEditor <> null then
                    let (scol,sline) = node.Range.StartColumn+1, node.Range.StartLine
                    extEditor.SetCaretLocation(max 1 sline, max 1 scol, true)

        member x.GetMarkup(n) =
            if tag :? DotNetProject then
                GLib.Markup.EscapeText (ext.OwnerProjects.[n].Name)
            else
                let node = memberList.[n]
                ext.GetEntityMarkup (node)

        member x.GetIcon(n) =
            if tag :? DotNetProject then
                ImageService.GetIcon(ext.OwnerProjects.[n].StockIcon.Name, Gtk.IconSize.Menu)
            else
                let node = memberList.[n]
                ImageService.GetIcon(ServiceUtils.getIcon node, Gtk.IconSize.Menu)
