namespace fsandroidnuget2

open System

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget

[<Activity (Label = "fsandroidnuget2", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity () =
    inherit Activity ()

    let mutable count:int = 1

    override this.OnCreate (bundle) =

        base.OnCreate (bundle)

        // Set our view from the "main" layout resource
        this.SetContentView (Resource_Layout.Main)

        // Get our button from the layout resource, and attach an event to it
        let button = this.FindViewById<Button>(Resource_Id.myButton)

        let i = [|1;2;3|] |> Array.take 1 |> Array.head
        button.Click.Add (fun args -> 
            button.Text <- sprintf "%d clicks! %d" count i
            count <- count + 1
        )

