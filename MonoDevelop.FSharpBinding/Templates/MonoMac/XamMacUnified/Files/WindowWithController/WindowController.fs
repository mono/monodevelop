namespace ${Namespace}
open System
open Foundation
open AppKit

[<Register ("${Name}Controller")>]
type ${Name}Controller =

    inherit NSWindowController

    new () = { inherit NSWindowController ("${Name}") }
    new (handle : IntPtr) = { inherit NSWindowController (handle) }

    [<Export ("initWithCoder:")>]
    new (coder : NSCoder) = { inherit NSWindowController (coder) }

    override x.AwakeFromNib () =
        base.AwakeFromNib ()

    member x.Window with get () = base.Window :?> ${Name}