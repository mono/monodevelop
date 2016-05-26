namespace MonoDevelopTests
open FsUnit
open NUnit.Framework
open MonoDevelop.Core
open MonoDevelop.Core.ProgressMonitoring
open MonoDevelop.Projects
open MonoDevelop.Projects.MSBuild
open System
open MonoDevelop.FSharp

[<TestFixture>]
type ProjectCracking() = 
    let fsproj = "/Users/jason/src/monodevelop/main/external/fsharpbinding/MonoDevelop.FSharpBinding/MonoDevelop.FSharp.fsproj"

    [<Test;Ignore>]
    member x.``Can crack fsharpbinding project``() =
        Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", "/tmp")
        Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", "/tmp")
        Runtime.Initialize (true)
        MonoDevelop.Ide.DesktopService.Initialize()
        let sln = "/Users/jason/src/monodevelop/main/external/fsharpbinding/MonoDevelop.FSharp.sln"
        let monitor = new ConsoleProgressMonitor()
        let w = Services.ProjectService.ReadWorkspaceItem (monitor, FilePath(sln))
                |> Async.AwaitTask
                |> Async.RunSynchronously

        let s = w :?> Solution
        let fsproj = s.Items.[0] :?> DotNetProject
        let opts = languageService.GetProjectOptionsFromProjectFile fsproj
        printfn "%A" opts