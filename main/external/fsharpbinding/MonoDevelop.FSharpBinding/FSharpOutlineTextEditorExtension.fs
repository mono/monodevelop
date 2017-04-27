namespace MonoDevelop.FSharp

open System
open Gtk
open MonoDevelop
open MonoDevelop.Components
open MonoDevelop.Core
open MonoDevelop.DesignerSupport
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Ide
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpOutlineTextEditorExtension() as x =
    inherit TextEditorExtension()
    let mutable treeView : PadTreeView option = None
    let mutable refreshingOutline : bool = false
    let mutable timerId : uint32 = 0u
    let mutable handler : IDisposable = null

    let refillTree() =
        match treeView with
        | Some(treeView) ->

            Runtime.AssertMainThread()
            refreshingOutline <- false

            if treeView.IsRealized then
                x.DocumentContext.TryGetAst() |> Option.iter (fun ast ->
                    let treeStore = treeView.Model :?> TreeStore
                    treeStore.Clear()
                    let toplevel = ast.GetNavigationItems()
                                   |> Array.sortBy(fun xs -> xs.Declaration.Range.StartLine)

                    for item in toplevel do
                        let iter = treeStore.AppendValues([| item.Declaration |])
                        let children = item.Nested
                                       |> Array.sortBy(fun xs -> xs.Range.StartLine)

                        for nested in children do
                            treeStore.AppendValues(iter, [| nested |]) |> ignore

                    treeView.ExpandAll())
                Gdk.Threads.Leave()
                timerId <- 0u
        | None -> ()

        refreshingOutline <- false
        false

    member private x.updateDocumentOutline _ =
        if not refreshingOutline then
            refreshingOutline <- true
            timerId <- GLib.Timeout.Add (1000u, (fun _ -> refillTree()))

    override x.Initialize() =
        base.Initialize()
        handler <- x.DocumentContext.DocumentParsed.Subscribe(fun o e -> x.updateDocumentOutline())

    override x.Dispose() =
        handler.Dispose()
        if timerId > 0u then
            GLib.Source.Remove timerId |> ignore
        timerId <- 0u
        base.Dispose()

    override x.IsValidInContext context =
        LanguageBindingService.GetBindingPerFileName (context.Name) <> null;

    interface IOutlinedDocument with
        member x.GetOutlineWidget() =
            match treeView with
            | Some(treeView) -> treeView :> Widget
            | None ->
                let treeStore = new TreeStore(typedefof<obj>)
                let padTreeView = new PadTreeView(treeStore, HeadersVisible = true)

                let setCellIcon _column (cellRenderer : CellRenderer) (treeModel : TreeModel) (iter : TreeIter) =
                    let pixRenderer = cellRenderer :?> CellRendererImage
                    treeModel.GetValue(iter, 0)
                    |> Option.tryCast<FSharpNavigationDeclarationItem[]>
                    |> Option.iter(fun item ->
                        pixRenderer.Image <- ImageService.GetIcon(ServiceUtils.getIcon item.[0], Gtk.IconSize.Menu))

                let setCellText _column (cellRenderer : CellRenderer) (treeModel : TreeModel) (iter : TreeIter) =
                    let renderer = cellRenderer :?> CellRendererText
                    treeModel.GetValue(iter, 0)
                    |> Option.tryCast<FSharpNavigationDeclarationItem[]>
                    |> Option.iter(fun item -> renderer.Text <- item.[0].Name)

                let jumpToDeclaration focus =
                    let iter : TreeIter ref = ref Unchecked.defaultof<_>
                    if padTreeView.Selection.GetSelected(iter) then
                        padTreeView.Model.GetValue(!iter, 0)
                        |> Option.tryCast<FSharpNavigationDeclarationItem[]>
                        |> Option.iter(fun item ->
                            let node = item.[0]
                            let (scol,sline) = node.Range.StartColumn+1, node.Range.StartLine
                            IdeApp.Workbench.OpenDocument (x.Editor.FileName, null, max 1 sline, max 1 scol) |> ignore)

                    if focus then
                        x.Editor.GrabFocus()

                treeView <- Some padTreeView

                let pixRenderer = new CellRendererImage(Xpad = 0u, Ypad = 0u)
                padTreeView.TextRenderer.Xpad <- 0u
                padTreeView.TextRenderer.Ypad <- 0u

                let treeCol = new TreeViewColumn()
                treeCol.PackStart(pixRenderer, false)
                treeCol.SetCellDataFunc(pixRenderer, new TreeCellDataFunc(setCellIcon))
                treeCol.PackStart(padTreeView.TextRenderer, true)
                treeCol.SetCellDataFunc(padTreeView.TextRenderer, new TreeCellDataFunc(setCellText))

                padTreeView.AppendColumn treeCol |> ignore
                padTreeView.Realized.Add(fun _ -> refillTree |> ignore)
                padTreeView.Selection.Changed.Subscribe(fun _ -> jumpToDeclaration false) |> ignore
                padTreeView.RowActivated.Subscribe(fun _ -> jumpToDeclaration true) |> ignore

                let sw = new CompactScrolledWindow()
                sw.Add padTreeView
                sw.ShowAll()
                sw :> Widget

        member x.GetToolbarWidgets() = [] :> _

        member x.ReleaseOutlineWidget() =
            treeView |> Option.iter(fun tv -> Option.tryCast<ScrolledWindow>(tv.Parent) 
                                              |> Option.iter (fun sw -> sw.Destroy())

                                              match tv.Model with
                                              :? TreeStore as ts -> ts.Dispose()
                                              | _ -> ())
            treeView <- None
