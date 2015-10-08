#r @"FakeLib.dll"
open Fake

Target "Build" (fun _ ->
  MSBuildWithDefaults "Build" ["./MonoDevelop.FSharp.mac-linux.sln"]
  |> Log "AppBuild-Output: "
)

Target "Test" (fun _ ->
  Shell.Exec ("mono", "../../build/bin/mdtool.exe run-md-tests ../../external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Debug/MonoDevelop.FSharp.Tests.dll") |> ignore
)

Target "Run" (fun _ ->
  Shell.Exec ("make", "run", "../..") |> ignore
)

"Build"
  ==> "Test"

"Build"
  ==> "Run"

RunTargetOrDefault "Build"
