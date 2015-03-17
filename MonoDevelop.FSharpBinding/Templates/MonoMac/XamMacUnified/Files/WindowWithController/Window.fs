namespace ${Namespace}

open System
open Foundation
open AppKit

[<Register ("${Name}")>]
type ${Name} =
    inherit NSWindow

    new () = { inherit NSWindow () }
    new (handle : IntPtr) = { inherit NSWindow (handle) }

    [<Export ("initWithCoder:")>]
    new (coder : NSCoder) = { inherit NSWindow (coder) }

    override x.AwakeFromNib () =
        base.AwakeFromNib ()