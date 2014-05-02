namespace ${Namespace}


open System

open Android.App
open Android.Content
open Android.Content.PM
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget

[<Activity (Label = "${ProjectName}", ConfigurationChanges=(ConfigChanges.Orientation ||| ConfigChanges.KeyboardHidden), MainLauncher = true)>]
type MainActivity () =
    inherit Activity ()

    let mutable count = 1
    let mutable view:GLView1 = null

    override x.OnCreate (bundle) =

        base.OnCreate (bundle)

        // Create our OpenGL view, and display it
        view <- new GLView1 (x)
        x.SetContentView (view)

    override x.OnPause () =
        // never forget to do this!
        base.OnPause ()
        view.Pause ()

    override x.OnResume () =
        // never forget to do this!
        base.OnResume ()
        view.Resume ()
