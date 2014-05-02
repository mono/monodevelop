namespace ${Namespace}

open System.Reflection

open Android.App
open Android.OS
open Xamarin.Android.NUnitLite

[<Activity (Label = "${ProjectName}", MainLauncher = true)>]
type MainActivity () =
    inherit TestSuiteActivity ()
    
    override this.OnCreate (bundle) =
        // tests can be inside the main assembly
        this.AddTest (Assembly.GetExecutingAssembly ());
        // or in any reference assemblies
        // AddTest (typeof (Your.Library.TestClass).Assembly);

        // Once you called base.OnCreate(), you cannot add more assemblies.
        base.OnCreate (bundle)

