namespace MonoDevelop.FSharp

open Gtk

open MonoDevelop.Components
open MonoDevelop.Components.AtkCocoaHelper
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Dialogs
open MonoDevelop.Ide.Gui.OptionPanels
open MonoDevelop.Projects

type FSharpProjectCompilerOptions(project:DotNetProject) as this =
    inherit Gtk.Bin()

    let boxChild (control:obj) =
        let child = (downcast control : Box.BoxChild) 
        child.Expand <- false
        child.Fill <- false

    let combo = new DotNetCompileTargetSelector(CompileTarget = project.CompileTarget)

    do
        Stetic.BinContainer.Attach (this) |> ignore
        let label = new Label(GettextCatalog.GetString "Compile target:")
        let hbox = new HBox(Spacing = 6)
        hbox.Add label
        hbox.Add combo
        boxChild hbox.[label]
        boxChild hbox.[combo]
        this.Add hbox
        this.ShowAll()

        combo.SetCommonAccessibilityAttributes ("CodeGeneration.CompileTarget", label,
                                                 GettextCatalog.GetString ("Select the compile target for the code generation"));
    member this.CompileTarget = combo.CompileTarget

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
