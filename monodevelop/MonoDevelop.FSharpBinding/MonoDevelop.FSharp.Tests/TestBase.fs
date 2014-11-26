namespace MonoDevelopTests
open System
open System.Reflection
open System.IO
open NUnit.Framework
open MonoDevelop.Core
open MonoDevelop.Core.Assemblies
open MonoDevelop.Ide
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Projects
open FSharp.CompilerBinding
open MonoDevelop.Ide.Gui

[<AutoOpen>]
module Path =
    let (++) (a:string) (b:string) = Path.Combine(a,b)
    ///Cleans up a path removing trailing double quotes and also consistant forward slash handling
    let neutralise (path:string) =
        Path.GetFullPath( path.TrimStart('\"').TrimEnd('\"'))


[<AutoOpen>]
module AssemblyLocation =
    let fromFqn (fqn:string) =
        let assembly = Assembly.ReflectionOnlyLoad fqn
        assembly.Location

    let (|Fqn|File|) (input:string) = 
        if input.Contains "," then Fqn(input)
        else File(input)

type Util = 
    
    static member TestsRootDir =
        let rootDir = Path.GetDirectoryName (typeof<Util>.Assembly.Location) ++ ".." ++ ".." ++ "tests"
        Path.GetFullPath (rootDir)

type TestBase() =
    static let firstRun = ref true
    do MonoDevelop.FSharp.MDLanguageService.DisableVirtualFileSystem()
    abstract member Setup: unit -> unit

    [<TestFixtureSetUp>]
    default x.Setup () =
        if !firstRun then
            let rootDir = Util.TestsRootDir
            try
                firstRun := false
                x.InternalSetup (rootDir)
            with
            | exn -> 
                // if we encounter an error, try to re create the configuration directory
                try 
                    if  Directory.Exists (rootDir) then
                        Directory.Delete (rootDir, true)
                        x.InternalSetup (rootDir)
                with
                | exn -> ()


    member x.InternalSetup (rootDir) =
        //Util.ClearTmpDir ()
        Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", rootDir)
        Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", rootDir)
        Runtime.Initialize (true)
        Gtk.Application.Init ()
        TypeSystemService.TrackFileChanges <- true
        DesktopService.Initialize ()
        Services.ProjectService.DefaultTargetFramework <- Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_5)
        
