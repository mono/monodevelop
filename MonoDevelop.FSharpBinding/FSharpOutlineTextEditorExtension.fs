namespace MonoDevelop.FSharp

open System
open ExtCore.Control
open Gtk
open MonoDevelop.Components.Docking
open MonoDevelop.Components
open MonoDevelop.Core
open MonoDevelop.DesignerSupport
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Ide
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpOutlineTextEditorExtension() = 
    inherit TextEditorExtension()
    let mutable treeView : PadTreeView option = None
    
    override x.Initialize() = 
        base.Initialize()
        x.DocumentContext.DocumentParsed.Add(x.UpdateDocumentOutline)
    
    override x.IsValidInContext _ = true
    
    //    IdeApp.Workbench.ActiveDocument <> null && IdeApp.Workbench.ActiveDocument.Name = x.DocumentContext.Name
    member private x.UpdateDocumentOutline _ = 
        let ast = maybe { let! context = x.DocumentContext |> Option.ofNull
                          let! parsedDocument = context.ParsedDocument |> Option.ofNull
                          let! ast = parsedDocument.Ast |> Option.tryCast<ParseAndCheckResults>
                          return ast }

        DispatchService.AssertGuiThread()
        Gdk.Threads.Enter()
        ast |> Option.iter (fun ast -> 
                   match treeView with
                   | Some(treeView) -> 
                       let treeStore = treeView.Model :?> TreeStore
                       treeStore.Clear()
                       let toplevel = ast.GetNavigationItems()
                       for item in toplevel do
                           printfn "%s %s" item.Declaration.Name (string item.Declaration.Kind)
                           let iter = treeStore.AppendValues(item.Declaration)
                           for nested in item.Nested do
                               treeStore.AppendValues(iter, [| nested |]) |> ignore
                               printfn "nested - %s %s" nested.Name (string nested.Kind)
                       treeView.ExpandAll()
                   | None -> ())
        Gdk.Threads.Leave()
    
    override x.Dispose() = 
        // more stuff here
        base.Dispose()
    interface IOutlinedDocument with
        member x.GetOutlineWidget() = 
            match treeView with
            | Some(treeView) -> treeView :> Widget
            | None -> 
                let setCellIcon (_) (cellRenderer : CellRenderer) (treeModel : TreeModel) (iter : TreeIter) = 
                    let pixRenderer = cellRenderer :?> CellRendererImage
                    let item = treeModel.GetValue(iter, 0) :?> FSharpNavigationDeclarationItem
                    pixRenderer.Image <- ImageService.GetIcon(ServiceUtils.getIcon item.Glyph, Gtk.IconSize.Menu)
                
                let setCellText (_) (cellRenderer : CellRenderer) (treeModel : TreeModel) (iter : TreeIter) = 
                    let renderer = cellRenderer :?> CellRendererText
                    let item = treeModel.GetValue(iter, 0) :?> FSharpNavigationDeclarationItem
                    renderer.Text <- item.Name
                
//                let refillTree _ = 
//                    DispatchService.AssertGuiThread()
//                    Gdk.Threads.Enter()
//                    treeStore.AppendValues("23") |> ignore
//                    Gdk.Threads.Leave()
                
                let treeStore = new TreeStore(typedefof<obj>)
                let padTreeView = new PadTreeView(treeStore)
                treeView <- Some padTreeView

                let pixRenderer = new CellRendererImage()
                pixRenderer.Xpad <- 0u
                pixRenderer.Ypad <- 0u
                padTreeView.TextRenderer.Xpad <- 0u
                padTreeView.TextRenderer.Ypad <- 0u

                let treeCol = new TreeViewColumn()
                treeCol.Title <- "HI I AM A COLUMN"
                treeCol.PackStart(pixRenderer, false)
                // treeCol.AddAttribute (padTreeView.TextRenderer,"text",1)
                treeCol.SetCellDataFunc(pixRenderer, new TreeCellDataFunc(setCellIcon))
                treeCol.PackStart(padTreeView.TextRenderer, true)
                treeCol.SetCellDataFunc(padTreeView.TextRenderer, new TreeCellDataFunc(setCellText))

                padTreeView.AppendColumn treeCol |> ignore
                padTreeView.HeadersVisible <- true
                //padTreeView.Realized.Add refillTree

                let sw = new CompactScrolledWindow()
//                padTreeView.Model <- treeStore
                sw.Add padTreeView
                sw.ShowAll()
                sw :> Widget
        
        member x.GetToolbarWidgets() = List.empty<Widget> :> _
        member x.ReleaseOutlineWidget() = ()
