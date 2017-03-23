namespace MonoDevelop.FSharp

open MonoDevelop.Components
open MonoDevelop.Ide.Gui.Dialogs

type FSharpFormattingPolicyPanel() =
    inherit MimeTypePolicyOptionsPanel<FSharpFormattingPolicy>()
    let mutable policy = DefaultFSharpFormatting.policy
    let mutable panel = new FSharpFormattingPolicyPanelWidget()
    override __.CreatePanelWidget() =
        panel <- new FSharpFormattingPolicyPanelWidget()
        panel.Initialize()
        Control.op_Implicit panel

    override __.LoadFrom(p : FSharpFormattingPolicy) =
        let formats = p.Formats |> function null -> ResizeArray<FSharpFormattingSettings>() | _ -> ResizeArray<FSharpFormattingSettings>(p.Formats)
        policy <- { p with DefaultFormat=p.DefaultFormat; Formats=formats }
        panel.SetFormat(policy)

    override __.GetPolicy() =
        panel.CommitPendingChanges ()
        policy
