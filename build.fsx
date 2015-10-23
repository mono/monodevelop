#r @"FakeLib.dll"
open Fake

Target "Build" (fun _ ->
  MSBuildWithDefaults "Build" ["./MonoDevelop.FSharp.mac-linux.sln"]
  |> Log "AppBuild-Output: "
)

let test() =
  Shell.Exec ("mono", "../../build/bin/mdtool.exe run-md-tests ../../external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Debug/MonoDevelop.FSharp.Tests.dll -labels") |> ignore

Target "BuildAndTest" (fun _ ->
  test()
)

Target "Test" (fun _ ->
  test()
)

Target "Run" (fun _ ->
  Shell.Exec ("make", "run", "../..") |> ignore
)

"Build"
  ==> "BuildAndTest"

"Build"
  ==> "Run"

RunTargetOrDefault "Build"
