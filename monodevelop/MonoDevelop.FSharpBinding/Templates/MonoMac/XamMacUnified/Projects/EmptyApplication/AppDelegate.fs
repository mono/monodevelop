namespace ${Namespace}
open System
open Foundation
open AppKit

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit NSApplicationDelegate ()

    override x.DidFinishLaunching (notification) =
        let menu = new NSMenu ()
        let menuItem = new NSMenuItem ()
        menu.AddItem (menuItem)

        let appMenu = new NSMenu ()
        let quitItem =
            new NSMenuItem ("Quit " + NSProcessInfo.ProcessInfo.ProcessName, "q", fun _ _ -> NSApplication.SharedApplication.Terminate menu)
        appMenu.AddItem (quitItem)

        menuItem.Submenu <- appMenu
        NSApplication.SharedApplication.MainMenu <- menu