namespace MonoDevelopTests
open System
open System.IO
open System.Runtime.CompilerServices
open System.Threading.Tasks
open NUnit.Framework
open FsUnit
open MonoDevelop.Core
open MonoDevelop.FSharp
open MonoDevelop.Projects

type TestPlatform =
    | Windows = 0
    | OSX = 1
    | Linux = 2

[<TestFixture>]
type CompilerArgumentsTests() =
    inherit UnitTests.TestBase ()
    let toTask computation : Task = Async.StartAsTask computation :> _

    let makeTestableReference (path: string) =
        let path = path.Substring(4)
        let path = path.Substring(0,path.Length - 1)
        path

    let createFSharpProject(name) =
        async {
            let monitor = UnitTests.Util.GetMonitor ()
            let dir = UnitTests.Util.CreateTmpDir(name)
            let testProject = Services.ProjectService.CreateDotNetProject ("F#") :?> FSharpProject
            testProject.FileName <- Path.Combine(dir, name + ".fsproj") |> FilePath

            let! _ = testProject.SaveAsync monitor |> Async.AwaitTask
            do! testProject.ReevaluateProject(monitor)
            return testProject
        }

    member private x.``Run Only mscorlib referenced`` (assemblyName) =
        async {
            use! testProject = createFSharpProject("OnlyMscorlib")
            let assemblyName = match assemblyName with Fqn a -> fromFqn a | File a -> a
            let _ = testProject.AddReference assemblyName
            let! asms = testProject.GetReferences (CompilerArguments.getConfig())
            let references =
                CompilerArguments.generateReferences(testProject, 
                                                     asms,
                                                     Some (FSharpCompilerVersion.FSharp_3_1),
                                                     FSharpTargetFramework.NET_4_5,
                                                     true) 

            //The two paths for mscorlib and FSharp.Core should match
            let testPaths = references |> List.map makeTestableReference
            match testPaths |> List.map Path.GetDirectoryName with
            | [one; two; three] -> ()
            | _ -> Assert.Fail(sprintf "Too many references returned %A" testPaths)
        }

    [<TestCase(TestPlatform.OSX,"/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/mscorlib.dll")>]
    [<TestCase(TestPlatform.Linux,"/usr/lib/mono/4.5/mscorlib.dll")>]
    [<TestCase(TestPlatform.OSX,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    [<TestCase(TestPlatform.Linux,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    //[<TestCase(TestPlatform.Windows,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    [<Test;AsyncStateMachine(typeof<Task>)>]
    member x.``Only mscorlib referenced`` (platform, assemblyName:string) =
        async {
            match platform with
            | TestPlatform.Linux when Platform.IsLinux ->
                do! x.``Run Only mscorlib referenced``(assemblyName)
            | TestPlatform.OSX when Platform.IsMac ->
                do! x.``Run Only mscorlib referenced``(assemblyName)
            | TestPlatform.Windows when Platform.IsWindows -> 
                do! x.``Run Only mscorlib referenced``(assemblyName)
            | _ -> ()
        } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    member x.``Explicit FSharp.Core and mscorlib referenced``() =
        async {
            if Platform.IsMac then
                use! testProject = createFSharpProject("MscorlibAndFSharpCore")
                let _ = testProject.AddReference "mscorlib"
                // we need to use a path to FSharp.Core.dll that exists on disk
                let fscorePath = typeof<FSharp.Core.PrintfFormat<_,_,_,_>>.Assembly.Location
                let reference = testProject.AddReference fscorePath
                let! asms = testProject.GetReferences (CompilerArguments.getConfig())
                let references =
                    CompilerArguments.generateReferences(testProject,
                                                         asms,
                                                         Some (FSharpCompilerVersion.FSharp_3_1),
                                                         FSharpTargetFramework.NET_4_5,
                                                         true)
                let testPaths = references |> List.map makeTestableReference
                testPaths |> should contain (reference.HintPath.FullPath |> string)
        } |> toTask