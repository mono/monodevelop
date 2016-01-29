namespace MonoDevelopTests
open NUnit.Framework
open FsUnit
open MonoDevelop.Core
open MonoDevelop.FSharp
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Projects
open System
open System.IO

[<TestFixture>]
type ProjectTests() =

    [<Test>]
    member this.Can_reorder_nodes() =
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
                """<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <Compile Include="test2.fs" />
    <Compile Include="test1.fs" />
  </ItemGroup>
</Project>"""
            newXml |> should equal expected

    [<Test>]
    member this.``Adds desktop conditional FSharp targets``() =
        let project = Services.ProjectService.CreateDotNetProject ("F#") :?> FSharpProject
        Project.addConditionalTargets (project.MSBuildProject, false)
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
    member this.``Adds portable FSharp targets``() =
        let project = Services.ProjectService.CreateDotNetProject ("F#") :?> FSharpProject
        Project.addConditionalTargets (project.MSBuildProject, true)
        let s = project.MSBuildProject.SaveToString()
        s |> shouldEqualIgnoringLineEndings
            """<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup />
  <PropertyGroup>
    <FSharpTargetsPath>$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.Portable.FSharp.Targets</FSharpTargetsPath>
  </PropertyGroup>
</Project>"""
