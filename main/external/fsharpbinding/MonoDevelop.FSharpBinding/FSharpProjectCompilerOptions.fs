namespace MonoDevelop.FSharp

open Gtk

open MonoDevelop.Components
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Dialogs
open MonoDevelop.Projects

type FSharpProjectCompilerOptions(project:DotNetProject) as this =
    inherit Gtk.Bin()

    let boxChild (control:obj) =
        let child = (downcast control : Box.BoxChild) 
        child.Expand <- false
        child.Fill <- false

    let entries = 
        [| "Executable"
           "Library"
           "Executable with GUI"
           "Module" |] |> Array.map GettextCatalog.GetString

    let combo = new ComboBox(entries)

    do
        Stetic.BinContainer.Attach (this) |> ignore
        let label = new Label(GettextCatalog.GetString "Compile target:")
        let hbox = new HBox()
        combo.Active <- int project.CompileTarget
        hbox.Add label
        hbox.Add combo
        hbox.Spacing <- 6
        boxChild hbox.[label]
        boxChild hbox.[combo]
        this.Add hbox
        this.ShowAll()

    member this.CompileTarget = enum<CompileTarget> combo.Active

type FSharpProjectCompilerOptionsPanel() =
    inherit ItemOptionsPanel()

    let mutable widget:FSharpProjectCompilerOptions option = None

    override this.CreatePanelWidget() =
        let options = new FSharpProjectCompilerOptions(this.ConfiguredProject :?> _)
        widget <- Some options
        Control.op_Implicit options

    override this.ApplyChanges() =
        widget |> Option.iter(fun widget' ->
            let project = this.ConfiguredProject :?> DotNetProject
            project.CompileTarget <- widget'.CompileTarget)
