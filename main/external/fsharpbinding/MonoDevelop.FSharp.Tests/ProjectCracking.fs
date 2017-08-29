namespace MonoDevelopTests
open System
open System.IO
open System.Reflection
open System.Threading.Tasks
open System.Runtime.CompilerServices

open FsUnit
open NUnit.Framework
open MonoDevelop.Core
open MonoDevelop.Core.ProgressMonitoring
open MonoDevelop.Projects
open MonoDevelop.Projects.MSBuild
open MonoDevelop.FSharp

[<TestFixture>]
module ``Project Cracking`` =
    let toTask computation : Task = Async.StartAsTask computation :> _

    let monitor = new ConsoleProgressMonitor()

    let getProjectOptions sln = async {
        let! w = Services.ProjectService.ReadWorkspaceItem (monitor, FilePath(sln)) |> Async.AwaitTask

        let s = w :?> Solution
        let fsproj = s.Items.[0] :?> FSharpProject
        do! fsproj.GetReferences()
        let opts = languageService.GetProjectOptionsFromProjectFile fsproj
        return opts.Value.OtherOptions
    }

    do
        FixtureSetup.initialiseMonoDevelop()

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Can crack Android project with explicit FSharp.Core``() = toTask <| async {
        if not MonoDevelop.Core.Platform.IsLinux then
            let directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            let sln = directoryName / "Samples" / "android" / "fsandroidnuget2.sln"
            let! opts = getProjectOptions sln
            let androidMscorlib = "MonoAndroid" / "v1.0" / "mscorlib.dll"
            let mscorlib = opts |> Array.filter(fun o -> o.EndsWith androidMscorlib)
            mscorlib.Length |> should equal 1

            let fsharpCore = opts |> Array.filter(fun o -> o.EndsWith "FSharp.Core.dll")
            fsharpCore.Length |> should equal 1
            // Should use the nuget package, not MonoAndroid FSharp.Core
            let fsharpCorePath = fsharpCore |> Array.head
            fsharpCorePath.IndexOf "FSharp.Core.4.0.0.1" |> should notEqual -1 
    }