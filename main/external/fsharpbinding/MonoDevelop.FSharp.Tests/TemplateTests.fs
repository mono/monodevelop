﻿namespace MonoDevelopTests
open System
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading.Tasks
open FsUnit
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.Core
open MonoDevelop.Core.ProgressMonitoring
open MonoDevelop.FSharp
open MonoDevelop.Ide
open MonoDevelop.Ide.Projects
open MonoDevelop.Ide.Templates
open MonoDevelop.PackageManagement.Tests.Helpers
open MonoDevelop.Projects
open MonoDevelop.Projects.MSBuild
open NUnit.Framework

[<TestFixture>]
type ``Template tests``() =
    let toTask computation : Task = Async.StartAsTask computation :> _

    let monitor = new ConsoleProgressMonitor()
    do
        FixtureSetup.initialiseMonoDevelop()
        let getField name =
            typeof<IdeApp>.GetField(name, BindingFlags.NonPublic ||| BindingFlags.Static)

        let workspace = getField "workspace"
        workspace.SetValue(null, new RootWorkspace())
        let workbench = getField "workbench"
        workbench.SetValue(null, new MonoDevelop.Ide.Gui.Workbench())

    let templateService = TemplatingService()
    let templateMatch (template:SolutionTemplate) = 
        template.IsMatch (SolutionTemplateVisibility.All)

    let predicate = new Predicate<SolutionTemplate>(templateMatch)

    let rec flattenCategories (category: TemplateCategory) = 
        seq {
            yield category
            yield! category.Categories |> Seq.collect flattenCategories
        }

    let solutionTemplates =
        templateService.GetProjectTemplateCategories (predicate)
        |> Seq.collect flattenCategories
        |> Seq.collect(fun c -> c.Templates)
        |> Seq.choose(fun s -> s.GetTemplate("F#") |> Option.ofObj)
        |> Seq.filter(fun t -> t.Id.IndexOf("SharedAssets") = -1) // shared assets projects can't be built standalone
        |> List.ofSeq

    let templatesDir = FilePath(".").FullPath.ToString() / "buildtemplates"

    let test (tt:string) =
        if not MonoDevelop.Core.Platform.IsMac then
            Assert.Ignore ()
        //if tt = "FSharpPortableLibrary" then
            //Assert.Ignore ("A platform service implementation has not been found")
        toTask <| async {
            let projectTemplate = ProjectTemplate.ProjectTemplates |> Seq.find (fun t -> t.Id = tt)
            let dir = FilePath (templatesDir/projectTemplate.Id)
            dir.Delete()
            Directory.CreateDirectory (dir |> string) |> ignore
            let cinfo = new ProjectCreateInformation (ProjectBasePath = dir, ProjectName = tt, SolutionName = tt, SolutionPath = dir)
            cinfo.Parameters.["CreateSharedAssetsProject"] <- "False"
            cinfo.Parameters.["CreatePortableDotNetProject"] <- "True"
            cinfo.Parameters.["CreateMonoTouchProject"] <- "True"
            cinfo.Parameters.["UseXamarinAndroidSupportv7AppCompat"] <- "True"
            cinfo.Parameters.["CreateAndroidProject"] <- "True"
            cinfo.Parameters.["UseUniversal"] <- "True"
            cinfo.Parameters.["UseIPad"] <- "False"
            cinfo.Parameters.["UseIPhone"] <- "False"
            cinfo.Parameters.["CreateiOSUITest"] <- "False"
            cinfo.Parameters.["CreateAndroidUITest"] <- "False"
            cinfo.Parameters.["MinimumOSVersion"] <- "10.0"
            cinfo.Parameters.["AppIdentifier"] <- tt
            cinfo.Parameters.["AndroidMinSdkVersionAttribute"] <- "android:minSdkVersion=\"10\""
            cinfo.Parameters.["AndroidThemeAttribute"] <- ""
            cinfo.Parameters.["TargetFrameworkVersion"] <- "MonoAndroid,Version=v7.0"
            use sln = projectTemplate.CreateWorkspaceItem (cinfo) :?> Solution

            let createTemplate (template:SolutionTemplate) =
                let config = NewProjectConfiguration(
                                CreateSolution = false,
                                ProjectName = tt,
                                SolutionName = tt,
                                Location = (dir |> string)
                             )

                templateService.ProcessTemplate(template, config, sln.RootFolder)

            let folder = new SolutionFolder()
            let solutionTemplate =
                solutionTemplates 
                |> Seq.find(fun t -> t.Id = tt)

            let projects = sln.Items |> Seq.filter(fun i -> i :? DotNetProject) |> Seq.cast<DotNetProject> |> List.ofSeq

            // Save solution before installing NuGet packages to prevent any Imports from being added
            // in the wrong place. Android projects now use the Xamarin.Build.Download NuGet package which
            // will add its own .props Import at the top of the project file. Saving the project the first time
            // after installing this NuGet package results in the Xamarin.Android.FSharp.targets Import being
            // added at the top of the project which causes a compile error about the OutputType not being defined.
            // This is because the Import is grouped with the Xamarin.Build.Download .props Import which is inserted
            // at the top of the project file.
            do! sln.SaveAsync(monitor)
            do! NuGetPackageInstaller.InstallPackages (sln, projectTemplate.PackageReferencesForCreatedProjects)

            let getErrorsForProject (projects: DotNetProject list) =
                asyncSeq {
                    let ctx = TargetEvaluationContext (LogVerbosity=MSBuildVerbosity.Diagnostic)
                    let! result = sln.Build(monitor, sln.DefaultConfigurationSelector, ctx) |> Async.AwaitTask
                    match tt, result.HasWarnings, result.HasErrors with
                    | "Xamarin.tvOS.FSharp.SingleViewApp", _, false //MTOUCH : warning MT0094: Both profiling (--profiling) and incremental builds (--fastdev) is not supported when building for tvOS. Incremental builds have ben disabled.]
                    | _, false, false ->
                        // xbuild worked, now check for editor squiggles
                        for project in projects do
                            let checker = FSharpChecker.Create()
                            let! refs = project.GetReferencedAssemblies (CompilerArguments.getConfig()) |> Async.AwaitTask

                            let projectOptions = languageService.GetProjectOptionsFromProjectFile (project, refs)

                            let! checkResult = checker.ParseAndCheckProject projectOptions.Value
                            for error in checkResult.Errors do
                                yield "Editor error", error.FileName, error.Message
                    | _ ->
                        for error in result.Errors do
                            if not error.IsWarning then
                                yield "Build error", error.FileName, error.ErrorText
                }

            let errors = getErrorsForProject projects |> AsyncSeq.toSeq |> List.ofSeq
            match errors with
            | [] -> Assert.Pass()
            | errors -> Assert.Fail (sprintf "%A" errors)
        }
    [<TestFixtureSetUp>]
    member x.Setup() =
        let config = """
<configuration>  
  <config>
    <add key="repositoryPath" value="packages" />
  </config>
</configuration>"""
        if not (Directory.Exists templatesDir) then
            Directory.CreateDirectory templatesDir |> ignore
        let configFileName = templatesDir/"NuGet.Config"
        File.WriteAllText (configFileName, config, Text.Encoding.UTF8)

    [<Test>]
    member x.``FSharp portable project``() =
        let name = "FSharpPortableLibrary"
        let projectTemplate = ProjectTemplate.ProjectTemplates |> Seq.find (fun t -> t.Id = name)
        let dir = FilePath (templatesDir/"fsportable")
        dir.Delete()
        let cinfo = new ProjectCreateInformation (ProjectBasePath = dir, ProjectName = name, SolutionName = name, SolutionPath = dir)
        let sln = projectTemplate.CreateWorkspaceItem (cinfo) :?> Solution
        let proj = sln.Items.[0] :?> FSharpProject
        proj.IsPortableLibrary |> should equal true

    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Forms FSharp FormsApp``()= test "Xamarin.Forms.FSharp.FormsApp"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``FSharpPortableLibrary``()= test "FSharpPortableLibrary"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Forms FSharp ClassLibrary``()= test "Xamarin.Forms.FSharp.ClassLibrary"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Forms FSharp UITestApp-Mac``()= test "Xamarin.Forms.FSharp.UITestApp-Mac"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin iOS FSharp SingleViewApp``()= test "Xamarin.iOS.FSharp.SingleViewApp"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin iOS FSharp ClassLibrary``()= test "Xamarin.iOS.FSharp.ClassLibrary"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin iOS FSharp UnitTestsApp``()= test "Xamarin.iOS.FSharp.UnitTestsApp"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Android FSharp AndroidApp``()= test "Xamarin.Android.FSharp.AndroidApp"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Android FSharp OpenGLGame``()= test "Xamarin.Android.FSharp.OpenGLGame"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Android FSharp ClassLibrary``()= test "Xamarin.Android.FSharp.ClassLibrary"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Android FSharp UnitTestApp``()= test "Xamarin.Android.FSharp.UnitTestApp"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Mac FSharp CocoaApp-XIB``()= test "Xamarin.Mac.FSharp.CocoaApp-XIB"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin Mac FSharp ClassLibrary``()= test "Xamarin.Mac.FSharp.ClassLibrary"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``Xamarin tvOS FSharp SingleViewApp``()= test "Xamarin.tvOS.FSharp.SingleViewApp"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``MonoDevelop FSharp ConsoleProject``()= test "MonoDevelop.FSharp.ConsoleProject"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``FSharpGtkProject``()= test "FSharpGtkProject"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``MonoDevelop FSharp LibraryProject``()= test "MonoDevelop.FSharp.LibraryProject"
    [<Test;AsyncStateMachine(typeof<Task>)>]member x.``FSharpNUnitLibraryProject``()= test "FSharpNUnitLibraryProject"
