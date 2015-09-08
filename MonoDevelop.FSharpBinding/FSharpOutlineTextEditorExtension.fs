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

type FSharpOutlineTextEditorExtension() =
    inherit TextEditorExtension()

    let mutable treeStore : TreeStore = null
    let mutable treeView : PadTreeView option = None

    override x.Initialize () =
        base.Initialize ()
//        let documentParsed  = x.DocumentContext.DocumentParsed.Subscribe(fun o e -> x.PathUpdated())
        x.DocumentContext.DocumentParsed.Add(x.UpdateDocumentOutline)

    //override x.IsValidInContext _ =
    //    IdeApp.Workbench.ActiveDocument <> null && IdeApp.Workbench.ActiveDocument.Name = x.DocumentContext.Name

    member private x.UpdateDocumentOutline _ =
        let ast = 
            maybe {let! context = x.DocumentContext |> Option.ofNull
                   let! parsedDocument = context.ParsedDocument |> Option.ofNull
                   let! ast = parsedDocument.Ast |> Option.tryCast<ParseAndCheckResults>
                   return ast}

        DispatchService.AssertGuiThread ()
        Gdk.Threads.Enter ()
        ast |> Option.iter (fun ast ->
            treeStore.AppendValues("2") |> ignore

            match treeView with
            | Some(treeView) -> treeView.ExpandAll ()
            | None -> ()
            ()
        )
        Gdk.Threads.Leave ()
 
        ()

    override x.Dispose () =
      // more stuff here
        base.Dispose ()

    interface IOutlinedDocument with
        member x.GetOutlineWidget () =
            match treeView with
                | Some(treeView) -> treeView :> Widget
                | None ->
                    let setCellIcon (column: TreeViewColumn) (cellRenderer: CellRenderer) (treeModel: TreeModel) (iter: TreeIter) =
                        ()

                    let refillTree _ = 
                        DispatchService.AssertGuiThread ()
                        Gdk.Threads.Enter ()
                        treeStore.AppendValues("1") |> ignore
                        Gdk.Threads.Leave ()
                        ()
                    let treeStore = new TreeStore(typedefof<Object>)
                    let padTreeView = new PadTreeView(treeStore)
                    let treeView = Some padTreeView
                    let pixRenderer = new CellRendererImage ()
                    pixRenderer.Xpad <- 0u
                    pixRenderer.Ypad <- 0u

                    padTreeView.TextRenderer.Xpad <- 0u
                    padTreeView.TextRenderer.Ypad <- 0u

                    let treeCol = new TreeViewColumn ()
                    treeCol.PackStart (pixRenderer, false)

                    treeCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (setCellIcon))
                    treeCol.PackStart (padTreeView.TextRenderer, true);

                    treeCol.SetCellDataFunc (padTreeView.TextRenderer, new TreeCellDataFunc (setCellIcon))
                    padTreeView.AppendColumn (treeCol) |> ignore

                    padTreeView.HeadersVisible <- false
                    padTreeView.Realized.Add (refillTree)
                    let sw = new CompactScrolledWindow()
                    sw.Add padTreeView
                    sw.ShowAll ()
                    sw :> Widget
        

        member x.GetToolbarWidgets () =
            List.empty<Widget> :> _
        member x.ReleaseOutlineWidget () =
            ()
        
