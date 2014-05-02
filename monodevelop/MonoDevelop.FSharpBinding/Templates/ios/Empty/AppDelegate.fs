namespace ${Namespace}

open System
open MonoTouch.UIKit
open MonoTouch.Foundation

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit UIApplicationDelegate ()

    let window = new UIWindow (UIScreen.MainScreen.Bounds)

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        // If you have defined a root view controller, set it here:
        // window.RootViewController <- new MyViewController ()
        window.MakeKeyAndVisible ()
        true

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main (args, null, "AppDelegate")
        0
