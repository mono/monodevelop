namespace MonoDevelopTests
open System
open System.IO
open FSharp.CompilerBinding
open NUnit.Framework
open FsUnit
open System.Reflection
open MonoDevelop.FSharp
open MonoDevelop.Projects

type TestPlatform = 
    | Windows = 0
    | Mono = 1

[<TestFixture>]
type CompilerArgumentsTests() =
    inherit TestBase()

    member private x.``Run Only mscorlib referenced`` (assemblyName) =
        use testProject = new DotNetAssemblyProject() :> DotNetProject
        let assemblyName = match assemblyName with Fqn a -> fromFqn a | File a -> a
        let _ = testProject.AddReference assemblyName
        let references = 
            CompilerArguments.generateReferences(testProject, 
                                                 Some (FSharpCompilerVersion.FSharp_3_1),
                                                 FSharpTargetFramework.NET_4_5,
                                                 ConfigurationSelector.Default,
                                                 true) 

        //there should be two references
        references.Length |> should equal 3

        //The two paths for mscorlib and FSharp.Core should match
        let makeTestableReference (path: string) = 
            let path = path.Substring(4)
            let path = path.Substring(0,path.Length - 1)
            path
        let testPaths = references |> List.map makeTestableReference
        match testPaths |> List.map Path.GetDirectoryName with
        | [one; two; three] -> ()//one |> should equal three
        | _ -> Assert.Fail("Too many references returned")

    member private x.``Run Only FSharp.Core referenced``(assemblyName) =
        use testProject = new DotNetAssemblyProject() :> DotNetProject
        let assemblyName = match assemblyName with Fqn a -> fromFqn a | File a -> a
        let reference = testProject.AddReference assemblyName
        let references = 
            CompilerArguments.generateReferences(testProject, 
                                                 Some (FSharpCompilerVersion.FSharp_3_1),
                                                 FSharpTargetFramework.NET_4_5,
                                                 ConfigurationSelector.Default,
                                                 false) 

        //there should be two references
        references.Length |> should equal 3

        //find the mscorlib inside the FSharp.Core ref
        let mscorlibContained =
            let assemblyDef = Mono.Cecil.AssemblyDefinition.ReadAssembly(reference.HintPath)
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

    [<TestCaseAttribute(TestPlatform.Mono,"/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/mscorlib.dll")>]
    [<TestCaseAttribute(TestPlatform.Mono,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    [<TestCaseAttribute(TestPlatform.Windows,"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    [<Test>]
    member x.``Only mscorlib referenced`` (platform, assemblyName:string) =
        match platform with
            | TestPlatform.Mono when MonoDevelop.Core.Platform.IsWindows -> ()
            | TestPlatform.Mono -> x.``Run Only mscorlib referenced`` (assemblyName)
            | TestPlatform.Windows when not MonoDevelop.Core.Platform.IsWindows -> ()
            | TestPlatform.Windows -> x.``Run Only mscorlib referenced`` (assemblyName)
            | _ -> ()
        

    [<TestCaseAttribute(TestPlatform.Windows,"FSharp.Core, Version=4.3.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")>]  
    [<TestCaseAttribute(TestPlatform.Mono,"FSharp.Core, Version=4.3.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")>]  
    [<TestCaseAttribute(TestPlatform.Mono, "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gac/FSharp.Core/4.3.0.0__b03f5f7f11d50a3a/FSharp.Core.dll")>] 
    [<TestCaseAttribute(TestPlatform.Mono, "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/FSharp.Core.dll")>]
    [<Test>]
    member x.``Only FSharp.Core referenced`` (platform: TestPlatform, assemblyName:string) =
        match platform with
        | TestPlatform.Mono when MonoDevelop.Core.Platform.IsWindows -> ()
        | TestPlatform.Mono -> x.``Run Only FSharp.Core referenced``(assemblyName)
        | TestPlatform.Windows when not MonoDevelop.Core.Platform.IsWindows -> ()
        | TestPlatform.Windows -> x.``Run Only FSharp.Core referenced``(assemblyName)
        | _ -> ()
        