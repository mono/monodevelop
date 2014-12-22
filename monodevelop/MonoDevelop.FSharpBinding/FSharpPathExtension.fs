namespace MonoDevelop.FSharp

open System
open System.Linq
open System.Diagnostics
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding

type FSharpPathExtension() =
    inherit TextEditorExtension()

    let pathChanged = new Event<_,_>()
    let mutable currentPath = [||]
    let mutable subscriptions = []
    member x.Document = base.Document
    member x.GetEntityMarkup(node: FSharpNavigationDeclarationItem) =
        let prefix = match node.Kind with
                     | NamespaceDecl-> "Namespace: "
                     | ModuleFileDecl -> "ModuleFile: "
                     | ExnDecl -> "Exn: "
                     | ModuleDecl -> "Module: "
                     | TypeDecl -> "Type: "
                     | MethodDecl -> "Method: "
                     | PropertyDecl -> "Property: "
                     | FieldDecl -> "Field: "
                     | OtherDecl -> "" 
        let name = node.Name.Split('.')
        if name.Length > 0 then prefix + name.Last()
        else prefix + node.Name

    override x.Initialize() =
        currentPath <- [| new PathEntry("No selection", Tag = null) |]
        let positionChanged = x.Document.Editor.Caret.PositionChanged.Subscribe(fun o e -> x.PathUpdated())
        let documentParsed  = x.Document.DocumentParsed.Subscribe(fun o e -> x.PathUpdated())
        subscriptions <- positionChanged :: documentParsed :: subscriptions
        
    member private x.PathUpdated() =
        let loc = x.Document.Editor.Caret.Location
        
        if x.Document.ParsedDocument = null ||
           IdeApp.Workbench.ActiveDocument <> x.Document then () else
        match x.Document.ParsedDocument.Ast with
        | :? ParseAndCheckResults as ast ->

            let posGt (p1Column, p1Line) (p2Column, p2Line) = 
                (p1Line > p2Line || (p1Line = p2Line && p1Column > p2Column))

            let posEq (p1Column, p1Line) (p2Column, p2Line) = 
                (p1Line = p2Line &&  p1Column = p2Column)

            let posGeq p1 p2 =
                posEq p1 p2 || posGt p1 p2

            let inside (docloc:Mono.TextEditor.DocumentLocation) (start, finish) =
                let cursor = (docloc.Column, docloc.Line)
                posGeq cursor start && posGeq finish cursor

            let toplevel = ast.GetNavigationItems()

            let topLevelTypesInsideCursor =
                toplevel |> Array.filter (fun tl -> let m = tl.Declaration.Range in inside loc ((m.StartColumn, m.StartLine),(m.EndColumn, m.EndLine)))
                         |> Array.sortBy(fun xs -> xs.Declaration.Range.StartLine)

            let newPath = ResizeArray<_>()
            for top in topLevelTypesInsideCursor do
                let name = top.Declaration.Name
                if name.Contains(".") then
                    let nameparts = name.[.. name.LastIndexOf(".")]
                    newPath.Add(PathEntry(ImageService.GetIcon(ServiceUtils.getIcon top.Declaration.Glyph, Gtk.IconSize.Menu), x.GetEntityMarkup(top.Declaration), Tag = (ast, nameparts)))
                else newPath.Add(PathEntry(ImageService.GetIcon(ServiceUtils.getIcon top.Declaration.Glyph, Gtk.IconSize.Menu), x.GetEntityMarkup(top.Declaration), Tag = ast))
            
            if topLevelTypesInsideCursor.Length > 0 then
                let lastToplevel = topLevelTypesInsideCursor.Last()
                //only first child found is returned, could there be multiple children found?
                let child = lastToplevel.Nested |> Array.tryFind (fun tl -> let m = tl.Range in inside loc ((m.StartColumn, m.StartLine),(m.EndColumn, m.EndLine)))
                let multichild = lastToplevel.Nested |> Array.filter (fun tl -> let m = tl.Range in inside loc ((m.StartColumn, m.StartLine),(m.EndColumn, m.EndLine)))

                Debug.Assert( multichild.Length <= 1, String.Format("{0} children found please investigate!", multichild.Length))
                match child with
                | Some(c) -> newPath.Add(PathEntry(ImageService.GetIcon(ServiceUtils.getIcon c.Glyph, Gtk.IconSize.Menu), x.GetEntityMarkup(c) , Tag = lastToplevel))
                | None -> newPath.Add(PathEntry("No selection", Tag = lastToplevel))

            let previousPath = currentPath
            //ensure the path has chnaged from the previous one before setting and raising event.
            let samePath = Seq.forall2 (fun (p1:PathEntry) (p2:PathEntry) -> p1.Markup = p2.Markup) previousPath newPath
            if not samePath then
                if newPath.Count = 0 then currentPath <- [|PathEntry("No selection", Tag = ast)|]
                else currentPath <- newPath.ToArray()

                //invoke pathChanged
                pathChanged.Trigger(x, DocumentPathChangedEventArgs(previousPath))
        | _ -> ()

    override x.Dispose() =
        subscriptions |> List.iter (fun s -> s.Dispose())
        subscriptions <- []

    interface IPathedDocument with
        member x.CurrentPath = currentPath
        member x.CreatePathWidget(index) =
            let path = (x :> IPathedDocument).CurrentPath
            if path = null || index < 0 || index >= path.Length then null else
            let tag = path.[index].Tag
            let window = new DropDownBoxListWindow(new FSharpDataProvider(x, tag))
            window.FixedRowHeight <- 22
            window.MaxVisibleRows <- 14
            window.SelectItem (path.[index].Tag)
            window :> _

        member x.add_PathChanged(handler) = pathChanged.Publish.AddHandler(handler)
        member x.remove_PathChanged(handler) = pathChanged.Publish.RemoveHandler(handler)


and FSharpDataProvider(ext:FSharpPathExtension, tag) =
    let memberList = ResizeArray<_>()

    let reset() =  
        memberList.Clear()
        match tag with
        | :? ParseAndCheckResults as tpr ->
            let navitems = tpr.GetNavigationItems()
            for decl in navitems do
                memberList.Add(decl.Declaration)
        | :? (ParseAndCheckResults * string) as typeAndFilter ->
            let tpr, filter = typeAndFilter 
            let navitems = tpr.GetNavigationItems()
            for decl in navitems do
                if decl.Declaration.Name.StartsWith(filter) then
                    memberList.Add(decl.Declaration)
        | :? FSharpNavigationTopLevelDeclaration as tld ->
            memberList.AddRange(tld.Nested)
        | _ -> ()

    do reset()

    interface DropDownBoxListWindow.IListDataProvider with
        member x.IconCount = memberList.Count
        member x.Reset() = reset()
        member x.GetTag (n) = memberList.[n] :> obj

        member x.ActivateItem(n) =
            let node = memberList.[n]
            let extEditor = ext.Document.GetContent<IExtensibleTextEditor>()
            if extEditor <> null then
                let (scol,sline) = node.Range.StartColumn, node.Range.StartLine
                extEditor.SetCaretTo(max 1 sline, max 1 scol, true)

        member x.GetMarkup(n) =
            let node = memberList.[n]
            ext.GetEntityMarkup (node)

        member x.GetIcon(n) =
            let node = memberList.[n]
            ImageService.GetIcon(ServiceUtils.getIcon node.Glyph, Gtk.IconSize.Menu)
