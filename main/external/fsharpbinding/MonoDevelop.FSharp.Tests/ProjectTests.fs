namespace MonoDevelopTests
open NUnit.Framework
open FsUnit
open MonoDevelop.Core
open MonoDevelop.Core.ProgressMonitoring
open MonoDevelop.FSharp
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Projects
open MonoDevelop.Projects.SharedAssetsProjects
open System
open System.IO
open System.Threading.Tasks
open System.Runtime.CompilerServices
[<TestFixture>]
type ProjectTests() =
    let monitor = new ConsoleProgressMonitor()
    let toTask computation : Task = Async.StartAsTask computation :> _
    let (/) a b = Path.Combine (a, b)

    [<Test>]
    member this.``Can reorder nodes``() =
        if not MonoDevelop.Core.Platform.IsWindows then
            let xml =
                """
                <Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """
            let path = Path.GetTempPath() + Guid.NewGuid().ToString() + ".fsproj"
            File.WriteAllText (path, xml)
            let project = Services.ProjectService.CreateDotNetProject ("F#")
            project.FileName <- new FilePath(path)
            let movingNode = project.AddFile("test1.fs")
            let moveToNode = project.AddFile("test2.fs")

            let fsp = new FSharpProjectNodeCommandHandler()
            fsp.MoveNodes moveToNode movingNode DropPosition.After
          
            let newXml = File.ReadAllText path
            let expected =
                """<Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <Compile Include="test2.fs" />
    <Compile Include="test1.fs" />
  </ItemGroup>
</Project>"""
            newXml |> should equal expected

    [<Test>]
    member this.``Can reorder dotnet core project files``() =
        if not MonoDevelop.Core.Platform.IsWindows then
            let xml =
                """
                <Project Sdk="FSharp.NET.Sdk;Microsoft.NET.Sdk">
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """
            let path = Path.GetTempPath() + Guid.NewGuid().ToString() + ".fsproj"
            File.WriteAllText (path, xml)
            let project = Services.ProjectService.CreateDotNetProject ("F#")
            project.FileName <- new FilePath(path)
            let movingNode = project.AddFile("test1.fs")
            let moveToNode = project.AddFile("test2.fs")

            let fsp = new FSharpProjectNodeCommandHandler()
            fsp.MoveNodes moveToNode movingNode DropPosition.After
          
            let newXml = File.ReadAllText path
            let expected =
                """<Project Sdk="FSharp.NET.Sdk;Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="test2.fs" />
    <Compile Include="test1.fs" />
  </ItemGroup>
</Project>"""

            newXml |> should equal expected

    [<Test;AsyncStateMachine(typeof<Task>)>]
    member this.``Orders and groups files correctly for Visual Studio``() =
        toTask <| async {
            if not MonoDevelop.Core.Platform.IsWindows then
                let path = Path.GetTempPath()
                let projectPath = path + Guid.NewGuid().ToString() + ".fsproj"
                let project = Services.ProjectService.CreateDotNetProject ("F#")
                project.FileName <- FilePath(projectPath)
                let files =
                    [ path / "MainActivity.fs", "Compile"
                      path / "Properties" / "AssemblyInfo.fs", "Compile"
                      path / "Resources" / "AboutResources.txt", "None"
                      path / "Properties" / "AndroidManifest.xml", "None"
                      path / "Properties" / "9.txt", "None"
                      path / "Properties" / "8.txt", "None"
                      path / "Properties" / "7.txt", "None" ]

                files |> List.iter(fun (path, buildAction) ->
                                        Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
                                        File.Create(path).Dispose()
                                        project.AddFile(path, buildAction) |> ignore)

                do! project.SaveAsync(monitor) |> Async.AwaitTask
                let groups = project.MSBuildProject.ItemGroups |> List.ofSeq
                groups.Length |> should equal 1
                let includes =
                    groups.[0].Items
                    |> Seq.map (fun item -> item.Include)
                    |> List.ofSeq
                let expected =
                    ["MainActivity.fs"
                     "Properties\\AssemblyInfo.fs"
                     "Properties\\AndroidManifest.xml"
                     "Properties\\9.txt"
                     "Properties\\8.txt"
                     "Properties\\7.txt"
                     "Resources\\AboutResources.txt"]
                Assert.AreEqual(expected, includes, sprintf "%A" includes)

        }

    [<Test;AsyncStateMachine(typeof<Task>)>]
    member this.``Orders by folders first found``() =
        toTask <| async {
            if not MonoDevelop.Core.Platform.IsWindows then
                let path = Path.GetTempPath()
                let projectPath = path + Guid.NewGuid().ToString() + ".fsproj"
                let project = Services.ProjectService.CreateDotNetProject ("F#")
                project.FileName <- FilePath(projectPath)
                let files =
                    [ path / "Properties" / "AndroidManifest.xml", "None"
                      path / "Services" / "Parser.fs", "Compile"
                      path / "MainActivity.fs", "Compile"
                      path / "Properties" / "AssemblyInfo.fs", "Compile" ]

                files |> List.iter(fun (path, buildAction) ->
                                        Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
                                        File.Create(path).Dispose()
                                        project.AddFile(path, buildAction) |> ignore)

                do! project.SaveAsync(monitor) |> Async.AwaitTask
                let groups = project.MSBuildProject.ItemGroups |> List.ofSeq
                groups.Length |> should equal 1
                let includes =
                    groups.[0].Items
                    |> Seq.map (fun item -> item.Include)
                    |> List.ofSeq
                let expected =
                    ["Properties\\AndroidManifest.xml"
                     "Properties\\AssemblyInfo.fs"
                     "Services\\Parser.fs"
                     "MainActivity.fs"]
                Assert.AreEqual(expected, includes, sprintf "%A" includes)
        }

    [<Test>]
    member this.``Adds desktop conditional FSharp targets``() =
        if not MonoDevelop.Core.Platform.IsWindows then
            let project = Services.ProjectService.CreateDotNetProject ("F#") :?> FSharpProject
            Project.addConditionalTargets project.MSBuildProject
            let s = project.MSBuildProject.SaveToString()
            s |> shouldEqualIgnoringLineEndings
                """<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup />
  <PropertyGroup>
    <FSharpTargetsPath>$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.FSharp.Targets</FSharpTargetsPath>
  </PropertyGroup>
  <PropertyGroup Condition="'$(VisualStudioVersion)' == '10.0' OR '$(VisualStudioVersion)' == '11.0'">
    <FSharpTargetsPath>$(MSBuildExtensionsPath32)\..\Microsoft SDKs\F#\3.0\Framework\v4.0\Microsoft.FSharp.Targets</FSharpTargetsPath>
  </PropertyGroup>
</Project>"""

    [<Test>]
    member this.``Adds shared project files first``() =
        let sol = new Solution ()
        let shared = new SharedAssetsProject ("F#")
        shared.AddFile ("Shared1.fs") |> ignore
        shared.AddFile ("Shared2.fs") |> ignore
        sol.RootFolder.AddItem (shared)

        // Reference to shared is added before adding project to solution
        let main = Services.ProjectService.CreateDotNetProject ("F#")
        main.AddFile ("File1.fs") |> ignore
        main.References.Add (ProjectReference.CreateProjectReference (shared))
        sol.RootFolder.AddItem (main)

        let files = CompilerArguments.getCompiledFiles main
                    |> Seq.map(fun f -> FilePath(f).FileName)

        Assert.AreEqual (seq ["Shared1.fs"; "Shared2.fs"; "File1.fs"], files)
