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
        let! (w:WorkspaceItem) = Services.ProjectService.ReadWorkspaceItem (monitor, FilePath(sln))

        let s = w :?> Solution
        let fsproj = s.Items.[0] :?> FSharpProject
        let config = CompilerArguments.getConfig()
        let! refs = fsproj.GetReferences (config)
        let opts = languageService.GetProjectOptionsFromProjectFile fsproj config refs
        return opts.Value.OtherOptions
    }

    do
        FixtureSetup.initialiseMonoDevelop()
