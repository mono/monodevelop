namespace MonoDevelop.FSharp

open Gtk
open MonoDevelop.Core
open MonoDevelop.Components.PropertyGrid

// Handwritten GUI, feel free to edit

[<System.ComponentModel.ToolboxItem(true)>]
type FSharpFormattingPolicyPanelWidget() =
    inherit Gtk.Bin()

    let store = new ListStore (typedefof<string>, typedefof<FSharpFormattingSettings>)

    let mutable policy = DefaultFSharpFormatting.policy
    let mutable vbox2 : Gtk.VBox = null
    let mutable hbox1 : Gtk.HBox = null
    let mutable boxScopes : Gtk.VBox = null
    let mutable GtkScrolledWindow : Gtk.ScrolledWindow = null
    let mutable listView : Gtk.TreeView = null
    let mutable hbox2 : Gtk.HBox = null
    let mutable vbox4 : Gtk.VBox = null
    let mutable tableScopes : Gtk.Table = null
    let mutable propertyGrid : PropertyGrid = null

    let getName format =
        if format = policy.DefaultFormat then
            GettextCatalog.GetString ("Default")
        else
            let i = policy.Formats.IndexOf (format) + 1
            GettextCatalog.GetString ("Format #{0}", i)

    let updateCurrentName() =
        let it : TreeIter ref = ref Unchecked.defaultof<_>
        match listView.Selection.GetSelected(it) with
        | true -> 
             let s = store.GetValue(!it, 1) :?> FSharpFormattingSettings
             store.SetValue (!it, 0, getName s)
        | false -> ()

    let fillFormat so =
        match so with
        | Some format ->
            propertyGrid.CurrentObject <- format
        | None -> ()
        updateCurrentName()
        propertyGrid.Sensitive <- so.IsSome

    let handleListViewSelectionChanged _ =
        let it : TreeIter ref = ref Unchecked.defaultof<_>
        match listView.Selection.GetSelected(it) with
        | true -> 
            let format = store.GetValue(!it, 1) :?> FSharpFormattingSettings
            fillFormat(Some format) 
        | _ -> 
            fillFormat(None)

    let appendSettings format =
        store.AppendValues(getName format, format) |> ignore

    let update() =
        store.Clear()
        appendSettings(policy.DefaultFormat)
        for s in policy.Formats do
            appendSettings s

    member private this.Build() =
        Stetic.Gui.Initialize(this)
        // Widget MonoDevelop.Xml.Formatting.XmlFormattingPolicyPanelWidget
        Stetic.BinContainer.Attach (this) |> ignore
        this.Name <- "MonoDevelop.FSharp.FSharpFormattingPolicyPanelWidget"
        // Container child MonoDevelop.Xml.Formatting.XmlFormattingPolicyPanelWidget.Gtk.Container+ContainerChild
        vbox2 <- new Gtk.VBox()
        vbox2.Name <- "vbox2"
        vbox2.Spacing <- 6
        // Container child vbox2.Gtk.Box+BoxChild
        hbox1 <- new Gtk.HBox()
        hbox1.Name <- "hbox1"
        hbox1.Spacing <- 6
        // Container child hbox1.Gtk.Box+BoxChild
        boxScopes <- new Gtk.VBox()
        boxScopes.Name <- "boxScopes"
        boxScopes.Spacing <- 6
        // Container child boxScopes.Gtk.Box+BoxChild
        GtkScrolledWindow <- new Gtk.ScrolledWindow()
        GtkScrolledWindow.Name <- "GtkScrolledWindow"
        GtkScrolledWindow.ShadowType <- ShadowType.In
        // Container child GtkScrolledWindow.Gtk.Container+ContainerChild
        listView <- new Gtk.TreeView()
        listView.CanFocus <- true
        listView.Name <- "listView"
        listView.HeadersVisible <- false
        GtkScrolledWindow.Add(listView)
        boxScopes.Add(GtkScrolledWindow)
        let w2 = boxScopes.[GtkScrolledWindow] :?> Gtk.Box.BoxChild
        w2.Position <- 0
        // Container child boxScopes.Gtk.Box+BoxChild
        hbox2 <- new Gtk.HBox()
        hbox2.Name <- "hbox2"
        hbox2.Spacing <- 6
        boxScopes.Add(hbox2)
        let w5 = boxScopes.[hbox2] :?> Gtk.Box.BoxChild
        w5.Position <- 1
        w5.Expand <- false
        w5.Fill <- false
        hbox1.Add(boxScopes)
        let w6 = hbox1.[boxScopes] :?> Gtk.Box.BoxChild
        w6.Position <- 0
        w6.Expand <- false
        w6.Fill <- false

        // Container child hbox1.Gtk.Box+BoxChild
        vbox4 <- new Gtk.VBox()
        vbox4.Name <- "vbox4"
        vbox4.Spacing <- 6

        // Container child vbox4.Gtk.Box+BoxChild
        tableScopes <- new Gtk.Table(uint32 3, uint32 3, false)
        tableScopes.Name <- "tableScopes"
        tableScopes.RowSpacing <- uint32 6
        tableScopes.ColumnSpacing <- uint32 6
        vbox4.Add(tableScopes)
        let w8 = vbox4.[tableScopes] :?> Gtk.Box.BoxChild
        w8.Position <- 1
        w8.Expand <- false
        w8.Fill <- false
        // Container child vbox4.Gtk.Box+BoxChild
        propertyGrid <- new PropertyGrid()
        propertyGrid.Name <- "propertyGrid"
        propertyGrid.ShowToolbar <- false
        propertyGrid.ShowHelp <- false
        vbox4.Add (propertyGrid)
        let w9 = vbox4.[propertyGrid] :?> Gtk.Box.BoxChild
        w9.Position <- 2
        hbox1.Add(vbox4)
        let w10 = hbox1.[vbox4] :?> Gtk.Box.BoxChild
        w10.Position <- 1

        vbox2.Add(hbox1)
        let w11 = vbox2.[hbox1] :?> Gtk.Box.BoxChild
        w11.Position <- 0

        this.Add(vbox2)
        if this.Child <> null then
           this.Child.ShowAll()
        boxScopes.Hide()

    member this.Initialize() =
        this.Build()

        propertyGrid.ShowToolbar <- false
        propertyGrid.ShadowType <- ShadowType.In

        listView.Model <- store
        listView.Selection.Changed.Add(handleListViewSelectionChanged)

    member __.CommitPendingChanges() = 
        propertyGrid.CommitPendingChanges()

    member __.SetFormat(p : FSharpFormattingPolicy) =
        policy <- p
        update()
        match store.GetIterFirst() with
        | true, it ->
            listView.Selection.SelectIter(it)
        | _ -> ()