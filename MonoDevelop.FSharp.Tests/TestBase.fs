namespace MonoDevelopTests
open System
open System.Reflection
open System.IO
open System.Threading
open NUnit.Framework
open MonoDevelop.Core
open MonoDevelop.Core.Assemblies
open MonoDevelop.Core.Logging
open MonoDevelop.Ide
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui

module FsUnit =

    open System.Diagnostics
    open NUnit.Framework
    open NUnit.Framework.Constraints

    [<DebuggerNonUserCode>]
    let should (f : 'a -> #Constraint) x (y : obj) =
        let c = f x
        let y =
            match y with
            | :? (unit -> unit) -> box (new TestDelegate(y :?> unit -> unit))
            | _                 -> y
        Assert.That(y, c)

    let equal x = new EqualConstraint(x)

    // like "should equal", but validates same-type
    let shouldEqual (x: 'a) (y: 'a) = Assert.AreEqual(x, y, sprintf "Expected: %A\nActual: %A" x y)

    let notEqual x = new NotConstraint(new EqualConstraint(x))

    let contain x = new ContainsConstraint(x)

    let haveLength n = Has.Length.EqualTo(n)

    let haveCount n = Has.Count.EqualTo(n)

    let endWith (s:string) = new EndsWithConstraint(s)

    let startWith (s:string) = new StartsWithConstraint(s)

    let be = id

    let Null = new NullConstraint()

    let Empty = new EmptyConstraint()

    let EmptyString = new EmptyStringConstraint()

    let NullOrEmptyString = new NullOrEmptyStringConstraint()

    let True = new TrueConstraint()

    let False = new FalseConstraint()

    let sameAs x = new SameAsConstraint(x)

    let throw = Throws.TypeOf


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

type TestLogger() = 
    // logger with instant flush for testing
    let logPath = Path.Combine(Util.TestsRootDir, "nunit.log")
    static let monitor = new Object()
    interface ILogger with
        member x.EnabledLevel = EnabledLoggingLevel.All
        member x.Name = "TestLogger"
        member x.Log (level, message) =
            lock monitor (fun() -> File.AppendAllText (logPath, message + "\n"))
            
        
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
                | exn -> printfn "%A" exn


    member x.InternalSetup (rootDir) =

        // Set a synchronization context for the main gtk thread
        SynchronizationContext.SetSynchronizationContext (new GtkSynchronizationContext ())
        Runtime.MainSynchronizationContext <- SynchronizationContext.Current

        //Util.ClearTmpDir ()
        let logger = new FileLogger (Path.Combine(Util.TestsRootDir, "nunit.log")) 
        logger.EnabledLevel <- EnabledLoggingLevel.All
        MonoDevelop.Core.LoggingService.AddLogger(new TestLogger())


        Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", rootDir)
        Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", rootDir)
        Environment.SetEnvironmentVariable ("MONODEVELOP_CONSOLE_LOG_LEVEL", "Debug")
        Environment.SetEnvironmentVariable ("MONODEVELOP_LOGGING_PAD_LEVEL", "Debug")

        Runtime.Initialize (true)
        Xwt.Application.Initialize ()
        Gtk.Application.Init ()

        MonoDevelop.Ide.TypeSystem.TypeSystemService.TrackFileChanges <- true
        DesktopService.Initialize ()
        Services.ProjectService.DefaultTargetFramework <- Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_5)
        
