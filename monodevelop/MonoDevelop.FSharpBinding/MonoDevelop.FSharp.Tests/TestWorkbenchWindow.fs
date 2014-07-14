namespace MonoDevelopTests
open System
open Mono.Addins
open MonoDevelop.Ide.Gui

type TestWorkbenchWindow() =
    let mutable viewContent = Unchecked.defaultof<IViewContent>
    let edc = DelegateEvent<_>()
    let closed = DelegateEvent<_>()
    let closing = DelegateEvent<_>()
    let avcc = DelegateEvent<_>()
    let viewsChanged = DelegateEvent<_>()

    member x.SetViewContent(v) = viewContent <- v
    
    interface IWorkbenchWindow with
        member x.SelectWindow () = ()
        member x.SwitchView (viewNumber:int) = ()
        member x.SwitchView (viewNumber:IAttachableViewContent) = ()
        member x.FindView<'a>() = -1
        member val Title = "" with get,set
        member val Document = null with get, set
        member val DocumentType = "" with get, set
        member val ShowNotification = false with get, set
        member x.ViewContent with get() = viewContent
        member x.ActiveViewContent with get() = viewContent :> _ and set v = viewContent <- downcast v
        member x.ExtensionContext with get() = AddinManager.AddinEngine :> _
        member x.CloseWindow (force) = true
        member x.AttachViewContent (subViewContent) = ()
        member x.InsertViewContent(index, subViewContent) = ()
        member x.SubViewContents with get() = Seq.empty
        member x.GetToolbar(targetView) = failwith "Not Implemented"

        [<CLIEvent>]
        member x.DocumentChanged = edc.Publish
        [<CLIEvent>]
        member x.Closed = closed.Publish
        [<CLIEvent>]
        member x.Closing = closing.Publish
        [<CLIEvent>]
        member x.ActiveViewContentChanged = avcc.Publish
        [<CLIEvent>]
        member x.ViewsChanged = viewsChanged.Publish