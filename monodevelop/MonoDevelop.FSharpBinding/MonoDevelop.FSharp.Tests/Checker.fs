namespace MonoDevelop.FSharp.Tests
open System
open System.IO
open FSharp.CompilerBinding
open NUnit.Framework
open FsUnit
open System.Reflection
open MonoDevelop.FSharp
open MonoDevelop.Projects

[<TestFixture>]
type CompilerArgumentsTests() =
    inherit TestBase()

    [<TestCaseAttribute("/Library/Frameworks/Mono.framework/Versions/3.4.0/lib/mono/4.5/mscorlib.dll")>]
    [<TestCaseAttribute("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" )>]
    [<Test>]
    member x.``Only mscorlib referenced`` (assemblyName:string) =

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
        references.Length |> should equal 2

        //The two paths for mscorlib and FSharp.Core should match
        match references |> List.map Path.GetDirectoryName with
        | [one; two] -> one |> should equal two
        | _ -> Assert.Fail("Too many references retuened")

    [<TestCaseAttribute("FSharp.Core, Version=4.3.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")>]  
    [<TestCaseAttribute("/Library/Frameworks/Mono.framework/Versions/3.4.0/lib/mono/gac/FSharp.Core/4.3.0.0__b03f5f7f11d50a3a/FSharp.Core.dll")>] 
    [<TestCaseAttribute("/Library/Frameworks/Mono.framework/Versions/3.4.0/lib/mono/4.5/FSharp.Core.dll")>]
    [<Test>]
    member x.``Only FSharp.Core referenced`` (assemblyName:string) =

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
        references.Length |> should equal 2

        //find the mscorlib inside the FSharp.Core ref
        let mscorlibContained =
            let assemblyDef = Mono.Cecil.AssemblyDefinition.ReadAssembly(reference.Reference)
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