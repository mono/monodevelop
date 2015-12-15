namespace MonoDevelopTests
open System.Text.RegularExpressions
open NUnit.Framework
open FsUnit
open MonoDevelop.Core
open MonoDevelop.FSharp
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Projects
open MonoDevelop.Projects.MSBuild
open System
open System.IO

[<TestFixture>]
type TestProjectNodeCommandHandler() = 

    [<Test>]
    member this.Can_reorder_nodes() =
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
