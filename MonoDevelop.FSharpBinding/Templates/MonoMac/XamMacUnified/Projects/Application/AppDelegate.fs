namespace ${Namespace}
open System
open Foundation
open AppKit

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit NSApplicationDelegate ()

    override x.DidFinishLaunching (notification) =
        let mainWindowController = new MainWindowController ()
        mainWindowController.Window.MakeKeyAndOrderFront (x)