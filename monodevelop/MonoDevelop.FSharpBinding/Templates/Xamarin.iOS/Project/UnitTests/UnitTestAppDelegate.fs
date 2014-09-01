namespace ${Namespace}

open System
open UIKit
open Foundation
open MonoTouch.NUnit.UI

/// The UIApplicationDelegate for the application. This class is responsible for launching the User Interface of
// the application, as well as listening (and optionally responding) to application events from iOS.
[<Register("UnitTestAppDelegate")>]
type UnitTestAppDelegate() = 
    inherit UIApplicationDelegate()

    let mutable window = null
    let mutable runner = null

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        //create a new window instance based on the screen size
        window <- new UIWindow (UIScreen.MainScreen.Bounds)
        runner <- TouchRunner (window)

        //register every tests included in the main application/assembly
        runner.Add (System.Reflection.Assembly.GetExecutingAssembly ())
        window.RootViewController <- new UINavigationController (runner.GetViewController ())

        //make the window visible
        window.MakeKeyAndVisible ()
        true

module Main = 
    [<EntryPoint>]
    let main args = 
        UIApplication.Main(args, null, "UnitTestAppDelegate")
        0
