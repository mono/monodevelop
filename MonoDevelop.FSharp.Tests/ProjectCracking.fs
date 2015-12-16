namespace MonoDevelopTests
open FsUnit
open NUnit.Framework
open MonoDevelop.Core
open System

[<TestFixture>]
type ProjectCracking() = 
  [<Test>]
  member x.Can_crack_xamarin_ios_project() =
    if not Platform.IsWindows then
      let fsproj = __SOURCE_DIRECTORY__ ++ "Samples" ++ "Xamarin.iOS.fsproj"
      let fsharpProjectOptions, logs =  CompilerService.ProjectCracker.GetProjectOptionsFromProjectFile(fsproj, enableLogging=true)
      fsharpProjectOptions.ProjectFileName |> should equal fsproj
      logs.[fsproj] |> should contain "Build finished"