namespace ${Namespace}

open System
open MonoTouch.UIKit
open MonoTouch.Foundation

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit UIApplicationDelegate ()

    override val Window = null with get, set

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        this.Window <- new UIWindow (UIScreen.MainScreen.Bounds)
        // If you have defined a root view controller, set it here:
        // this.Window.RootViewController <- new MyViewController ()
        this.Window.MakeKeyAndVisible ()
        true

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main (args, null, "AppDelegate")
        0
