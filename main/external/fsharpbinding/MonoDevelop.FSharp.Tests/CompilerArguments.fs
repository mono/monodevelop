namespace MonoDevelopTests
open System
open System.IO
open NUnit.Framework
open FsUnit
open System.Reflection
open MonoDevelop.Core
open MonoDevelop.FSharp
open MonoDevelop.Projects

type TestPlatform =
    | Windows = 0
    | OSX = 1
    | Linux = 2

[<TestFixture>]
type CompilerArgumentsTests() =
    let makeTestableReference (path: string) =
        let path = path.Substring(4)
        let path = path.Substring(0,path.Length - 1)
        path

    let createFSharpProject() =
        let testProject = Services.ProjectService.CreateDotNetProject ("F#")
        testProject.FileName <- FilePath "FSharpProject.fsproj"
        testProject

    member private x.``Run Only mscorlib referenced`` (assemblyName) =
        use testProject = createFSharpProject()
        let assemblyName = match assemblyName with Fqn a -> fromFqn a | File a -> a
        let _ = testProject.AddReference assemblyName
        let references =
            CompilerArguments.generateReferences(testProject, 
                                                 Some (FSharpCompilerVersion.FSharp_3_1),
                                                 FSharpTargetFramework.NET_4_5,
                                                 ConfigurationSelector.Default,
                                                 true) 

        //The two paths for mscorlib and FSharp.Core should match
        let testPaths = references |> List.map makeTestableReference
        match testPaths |> List.map Path.GetDirectoryName with
        | [one; two; three] -> ()
        | _ -> Assert.Fail(sprintf "Too many references returned %A" testPaths)

    member private x.``Run Only FSharp.Core referenced``(assemblyName) =
        use testProject = createFSharpProject()
        let assemblyName = match assemblyName with Fqn a -> fromFqn a | File a -> a
        let reference = testProject.AddReference assemblyName
        let references = 
            CompilerArguments.generateReferences(testProject,
                                                 Some (FSharpCompilerVersion.FSharp_3_1),
                                                 FSharpTargetFramework.NET_4_5,
                                                 ConfigurationSelector.Default,
                                                 false)

        references.Length |> should equal 3

        //find the mscorlib inside the FSharp.Core ref
        let mscorlibContained =
            let assemblyDef = Mono.Cecil.AssemblyDefinition.ReadAssembly(reference.HintPath.ToString())
            match assemblyDef.MainModule.AssemblyReferences |> Seq.tryFind (fun name -> name.Name = "mscorlib") with
            |Some name ->
                let resolved = assemblyDef.MainModule.AssemblyResolver.Resolve(name)
                Some(Path.neutralise resolved.MainModule.FullyQualifiedName)
            | None -> None

        //find the mscorlib from the returned references (removing unwanted chars "" / \ etc)
        let mscorlibReferenced =
            references
            |> List.tryFind (fun ref -> ref.Contains("mscorlib"))
            |> Option.map (fun r -> Path.neutralise (r.Replace("-r:", "")))

        mscorlibContained |> should equal mscorlibReferenced

    [<TestCase(TestPlatform.OSX,"/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/mscorlib.dll")>]
    [<TestCase(TestPlatform.Linux,"/usr/lib/mono/4.5/mscorlib.dll")>]
    [<TestCase(TestPlatform.OSX,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    [<TestCase(TestPlatform.Linux,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    //[<TestCase(TestPlatform.Windows,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    member x.``Only mscorlib referenced`` (platform, assemblyName:string) =
        match platform with
        | TestPlatform.Linux when Platform.IsLinux ->
            x.``Run Only mscorlib referenced``(assemblyName)
        | TestPlatform.OSX when Platform.IsMac ->
            x.``Run Only mscorlib referenced``(assemblyName)
        | TestPlatform.Windows when Platform.IsWindows -> 
            x.``Run Only mscorlib referenced``(assemblyName)
        | _ -> ()

    //[<TestCase(TestPlatform.Windows,"FSharp.Core, Version=4.4.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")>]  
    [<TestCase(TestPlatform.OSX,"FSharp.Core, Version=4.4.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")>]  
    [<TestCase(TestPlatform.OSX, "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gac/FSharp.Core/4.4.1.0__b03f5f7f11d50a3a/FSharp.Core.dll")>]
    [<TestCase(TestPlatform.Linux, "/usr/lib/mono/gac/FSharp.Core/4.4.1.0__b03f5f7f11d50a3a/FSharp.Core.dll")>] 
    [<TestCase(TestPlatform.OSX, "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/FSharp.Core.dll")>]
    member x.``Only FSharp.Core referenced`` (platform: TestPlatform, assemblyName:string) =
        match platform with
        | TestPlatform.Linux when Platform.IsLinux ->
            x.``Run Only FSharp.Core referenced``(assemblyName)
        | TestPlatform.OSX when Platform.IsMac ->
            x.``Run Only FSharp.Core referenced``(assemblyName)
        | TestPlatform.Windows when Platform.IsWindows -> 
            x.``Run Only FSharp.Core referenced``(assemblyName)
        | _ -> ()
    
    [<Test>]
    member x.``Explicit FSharp.Core and mscorlib referenced``() =
        if Platform.IsMac then
            use testProject = createFSharpProject()
            let _ = testProject.AddReference "mscorlib"
            let reference = testProject.AddReference "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/FSharp.Core.dll"
            let references =
                CompilerArguments.generateReferences(testProject,
                                                     Some (FSharpCompilerVersion.FSharp_3_1),
                                                     FSharpTargetFramework.NET_4_5,
                                                     ConfigurationSelector.Default,
                                                     true)
            let testPaths = references |> List.map makeTestableReference
            testPaths |> should contain (reference.HintPath.FullPath |> string)
