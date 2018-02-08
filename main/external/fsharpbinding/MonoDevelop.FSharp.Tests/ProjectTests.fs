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
    member this.``Doesn't change forward slashes to backslashes ``() =
        toTask <| async {
            if not MonoDevelop.Core.Platform.IsWindows then
                let xml =
                    """
                    <Project Sdk="Microsoft.NET.Sdk.Web">
                      <PropertyGroup>
                        <TargetFramework>netcoreapp2.0</TargetFramework>
                        <MvcRazorCompileOnPublish>true</MvcRazorCompileOnPublish>
                      </PropertyGroup>
                      <ItemGroup>
                        <Compile Include="Controllers/HomeController.fs" />
                        <Compile Include="Models/ErrorViewModel.fs" />
                        <Compile Include="Startup.fs" />
                        <Compile Include="Program.fs" />
                      </ItemGroup>
                    </Project>"""

                let path = Path.GetTempPath() / (Guid.NewGuid().ToString())
                Directory.CreateDirectory path |> ignore
                let projectPath = path / "slashes.fsproj"
                printfn "%s" path
                File.WriteAllText (projectPath, xml)
                let! (solutionItem:SolutionItem) = Services.ProjectService.ReadSolutionItem(monitor, projectPath) 
                let project = solutionItem :?> DotNetProject
                project.FileName <- FilePath(projectPath)
                project.AddFile(path / "Controllers\\HomeController.fs", "Compile") |> ignore
                do! project.SaveAsync(monitor)
                project.Dispose()
                let xml = File.ReadAllText projectPath
                printfn "%s" xml
                let expected =
                    """
                    <Project Sdk="Microsoft.NET.Sdk.Web">
                      <PropertyGroup>
                        <TargetFramework>netcoreapp2.0</TargetFramework>
                        <MvcRazorCompileOnPublish>true</MvcRazorCompileOnPublish>
                      </PropertyGroup>
                      <ItemGroup>
                        <Compile Include="Controllers/HomeController.fs" />
                        <Compile Include="Models/ErrorViewModel.fs" />
                        <Compile Include="Startup.fs" />
                        <Compile Include="Program.fs" />
                      </ItemGroup>
                    </Project>"""
                xml |> should equal expected
        }

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

                do! project.SaveAsync(monitor)
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
    member this.``Adds two image assets``() =
        toTask <| async {
            if not MonoDevelop.Core.Platform.IsWindows then
                let path = Path.GetTempPath()
                let projectPath = path + Guid.NewGuid().ToString() + ".fsproj"
                let project = Services.ProjectService.CreateDotNetProject ("F#")
                project.FileName <- FilePath(projectPath)

                let addFile path buildAction =
                    Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
                    File.Create(path).Dispose()
                    project.AddFile(path, buildAction) |> ignore

                addFile (path/"Assets.xcassets"/"test1.fs") "Compile"
                addFile (path/"Assets.xcassets"/"test1.png") "ImageAsset"
                do! project.SaveAsync(monitor)
                addFile (path/"Assets.xcassets"/"test2.png") "ImageAsset"
                do! project.SaveAsync(monitor)

                let x = File.ReadAllText projectPath
                printfn "%s" x
                let groups = project.MSBuildProject.ItemGroups |> List.ofSeq
                groups.Length |> should equal 1
                let includes =
                    groups.[0].Items
                    |> Seq.map (fun item -> item.Include)
                    |> List.ofSeq
                let expected =
                    ["Assets.xcassets\\test1.fs"
                     "Assets.xcassets\\test1.png"
                     "Assets.xcassets\\test2.png"]
                Assert.AreEqual(expected, includes, sprintf "%A" includes)

        }

    [<Test;AsyncStateMachine(typeof<Task>)>]
    member this.``Maintains three item groups``() =
        toTask <| async {
            if not MonoDevelop.Core.Platform.IsWindows then
                let project = Services.ProjectService.CreateDotNetProject ("F#")

                let path = Path.GetTempPath() / (Guid.NewGuid().ToString())
                Directory.CreateDirectory(path) |> ignore
                let addFile buildAction buildInclude =
                    project.AddFile(path/buildInclude, buildAction) |> ignore

                addFile "Reference" "System"
                addFile "Folder" "Resources"
                addFile "Compile" "Source.fs"
                addFile "None" "SomeImage.bmp"

                let projectPath = path / Guid.NewGuid().ToString() + ".fsproj"
                project.FileName <- FilePath(projectPath)
                do! project.SaveAsync(monitor)
                let xml = File.ReadAllText projectPath
                printfn "%s" xml
                let groups = project.MSBuildProject.ItemGroups |> List.ofSeq
                groups.Length |> should equal 3 // References, Folders, Rest
        }

    [<Test;AsyncStateMachine(typeof<Task>)>]
        member this.``Adds new image assets``() =
            toTask <| async {
                if not MonoDevelop.Core.Platform.IsWindows then
                    let xml =
                        """
                        <Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                          <PropertyGroup>
                            <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
                            <Platform Condition=" '$(Platform)' == '' ">iPhoneSimulator</Platform>
                            <ProjectGuid>{A23F6451-2157-4673-8C0D-F1DE17D00D32}</ProjectGuid>
                            <ProjectTypeGuids>{FEACFBD2-3405-455C-9665-78FE426C6842};{F2A71F9B-5D33-465A-A702-920D77279786}</ProjectTypeGuids>
                            <OutputType>Exe</OutputType>
                            <RootNamespace>fsr</RootNamespace>
                            <AssemblyName>fsr</AssemblyName>
                            <IPhoneResourcePrefix>Resources</IPhoneResourcePrefix>
                          </PropertyGroup>
                          <ItemGroup>
                            <Reference Include="System" />
                            <Reference Include="System.Xml" />
                            <Reference Include="System.Core" />
                            <Reference Include="mscorlib" />
                            <Reference Include="FSharp.Core" />
                            <Reference Include="Xamarin.iOS" />
                          </ItemGroup>
                          <ItemGroup>
                            <Folder Include="Resources\" />
                          </ItemGroup>
                          <ItemGroup>
                            <ImageAsset Include="Assets.xcassets\AppIcon.appiconset\Icon-App-29x29%401x.png" />
                            <ImageAsset Include="Assets.xcassets\Contents.json" />
                            <InterfaceDefinition Include="LaunchScreen.storyboard" />
                            <InterfaceDefinition Include="Main.storyboard" />
                            <None Include="Entitlements.plist" />
                            <None Include="Info.plist" />
                            <Compile Include="Main.fs" />
                            <Compile Include="ViewController.fs" />
                            <Compile Include="AppDelegate.fs" />
                            <ImageAsset Include="Assets.xcassets\AppIcon.appiconset\Icon-App-76x76%402x.png" />
                          </ItemGroup>
                          <Import Project="$(MSBuildExtensionsPath)\Xamarin\iOS\Xamarin.iOS.FSharp.targets" />
                        </Project>
                        """

                    let path = Path.GetTempPath() / (Guid.NewGuid().ToString())
                    Directory.CreateDirectory path |> ignore
                    let projectPath = path / "ImageAssets.fsproj"
                    printfn "%s" path
                    File.WriteAllText (projectPath, xml)
                    let project = Services.ProjectService.CreateDotNetProject ("F#")
                    let! (solutionItem:SolutionItem) = Services.ProjectService.ReadSolutionItem(monitor, projectPath) 
                    let project = solutionItem :?> Project
                    project.FileName <- FilePath(projectPath)

                    let addFile filePath buildAction =
                        project.AddFile(path / filePath, buildAction) |> ignore

                    addFile ("Assets.xcassets"/"AppIcon.appiconset"/"Icon-App-test1.png") "ImageAsset"
                    do! project.SaveAsync(monitor)
                    addFile ("Assets.xcassets"/"AppIcon.appiconset"/"Icon-App-test2.png") "ImageAsset"
                    do! project.SaveAsync(monitor)
                    let groups = project.MSBuildProject.ItemGroups |> List.ofSeq
                    groups.Length |> should equal 3 // References, Folders, Rest
                    let includes =
                        groups.[2].Items
                        |> Seq.map (fun item -> item.Include)
                        |> List.ofSeq
                        |> List.filter(fun i -> i.StartsWith("Assets.xcassets\\AppIcon.appiconset"))
                        |> List.sort

                    let expected =
                        ["Assets.xcassets\\AppIcon.appiconset\\Icon-App-29x29%401x.png"
                         "Assets.xcassets\\AppIcon.appiconset\\Icon-App-76x76%402x.png"
                         "Assets.xcassets\\AppIcon.appiconset\\Icon-App-test1.png"
                         "Assets.xcassets\\AppIcon.appiconset\\Icon-App-test2.png"]
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

                do! project.SaveAsync(monitor)
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
