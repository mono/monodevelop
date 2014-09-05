namespace ${Namespace}
open System
open UIKit
open Foundation

[<Register("AppDelegate")>]
type AppDelegate() = 
    inherit UIApplicationDelegate()

    override x.OnResignActivation (application) = ()
        
    // This method should be used to release shared resources and it should store the application state.
    // If your application supports background exection this method is called instead of WillTerminate
    // when the user quits.
    override x.DidEnterBackground (application) = ()
        
    // This method is called as part of the transiton from background to active state.
    override x.WillEnterForeground (application) = ()
        
    // This method is called when the application is about to terminate. Save data, if needed.
    override x.WillTerminate (application) = ()

module Main = 
    [<EntryPoint>]
    let main args = 
        NSExtension.Initialize ()
        0