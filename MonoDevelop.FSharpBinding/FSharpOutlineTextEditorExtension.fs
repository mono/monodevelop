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

    override x.IsValidInContext _ =
        true
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
            //treeStore.AppendValues(TreeIter.Zero, [|"1"|]) |> ignore
            treeStore.AppendValues("1") |> ignore

            match treeView with
            | Some(treeView) -> 
                treeView.ExpandAll ()
                //treeView.Selection.SelectIter (TreeIter.Zero)
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

                    let setCellText (column: TreeViewColumn) (cellRenderer: CellRenderer) (treeModel: TreeModel) (iter: TreeIter) =
                      Console.WriteLine ("F# tree func");

                      let renderer = cellRenderer :?> CellRendererText
                      let o = treeModel.GetValue (iter, 0)
                      renderer.Text <- (string o)
                      ()

                    let refillTree _ =
                        DispatchService.AssertGuiThread ()
                        Gdk.Threads.Enter ()
                        treeStore.AppendValues("23") |> ignore
                        Gdk.Threads.Leave ()
                        ()
                    treeStore <- new TreeStore(typedefof<obj>)
                    let padTreeView = new PadTreeView(treeStore)
                    treeView <- Some padTreeView
                    let pixRenderer = new CellRendererImage ()
                    pixRenderer.Xpad <- 0u
                    pixRenderer.Ypad <- 0u

                    padTreeView.TextRenderer.Xpad <- 0u
                    padTreeView.TextRenderer.Ypad <- 0u

                    let treeCol = new TreeViewColumn ()
                    treeCol.Title <- "HI I AM A COLUMN"
                    treeCol.PackStart (pixRenderer, false)
                    // treeCol.AddAttribute (padTreeView.TextRenderer,"text",1)

                    treeCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (setCellIcon))
                    treeCol.PackStart (padTreeView.TextRenderer, true);

                    treeCol.SetCellDataFunc (padTreeView.TextRenderer, new TreeCellDataFunc (setCellText))
                    padTreeView.AppendColumn (treeCol) |> ignore

                    padTreeView.HeadersVisible <- true
                    padTreeView.Realized.Add refillTree
                    let sw = new CompactScrolledWindow()
                    //padTreeView.Model <- treeStore
                    sw.Add padTreeView
                    sw.ShowAll ()
                    sw :> Widget


        member x.GetToolbarWidgets () =
            List.empty<Widget> :> _
        member x.ReleaseOutlineWidget () =
            ()
