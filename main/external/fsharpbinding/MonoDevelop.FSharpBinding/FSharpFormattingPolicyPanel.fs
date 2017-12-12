namespace MonoDevelop.FSharp

open MonoDevelop.Components
open MonoDevelop.Ide.Gui.Dialogs

type FSharpFormattingPolicyPanel() =
    inherit MimeTypePolicyOptionsPanel<FSharpFormattingPolicy>()
    let mutable policy = DefaultFSharpFormatting.policy
    let mutable panel  = None
    override __.CreatePanelWidget() =
        let widget = new FSharpFormattingPolicyPanelWidget()
        panel <- widget |> Some
        widget.Initialize()
        Control.op_Implicit widget

    override __.LoadFrom(p : FSharpFormattingPolicy) =
        let formats = p.Formats |> function null -> ResizeArray<FSharpFormattingSettings>() | _ -> ResizeArray<FSharpFormattingSettings>(p.Formats)
        policy <- { p with DefaultFormat=p.DefaultFormat; Formats=formats }
        panel |> Option.iter (fun widget -> widget.SetFormat (policy))

    override __.GetPolicy() =
        panel |> Option.iter (fun widget -> widget.CommitPendingChanges ())
        policy
