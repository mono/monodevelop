#r @"./packages/FAKE/tools/FakeLib.dll"
open Fake
open System.IO

let isWindows = (Path.DirectorySeparatorChar = '\\')
let config = "Release"

Target "Default" (fun _ ->
  MSBuildWithDefaults "Build" ["./MonoDevelop.FSharp.sln"]
  |> Log "AppBuild-Output: "
)

let mdpath = "../../build/bin/mdtool.exe"

let mdtool args =
  let result =
    if isWindows then
      Shell.Exec (mdpath, args)
    else
      Shell.Exec ("mono64", mdpath + " " + args)
  result |> ignore

let test() =
  mdtool ("run-md-tests ../../external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/" + config + "/MonoDevelop.FSharp.Tests.dll -labels")

Target "Pack" (fun _ ->
  let dir = "pack/" + config
  if Directory.Exists dir then
    Directory.Delete (dir, true)
  Directory.CreateDirectory dir |> ignore
  mdtool ("setup pack bin/FSharpBinding.dll -d:pack/" + config)
)

Target "Install" (fun _ ->
  let versionConfig = File.ReadAllLines("../../../version.config")
  let version = versionConfig.[0].Replace("Version=", "")
  mdtool ("setup install -y pack/" + config + "/MonoDevelop.FSharpBinding_" + version + ".mpack")
)

Target "BuildAndTest" (fun _ ->
  test()
)

Target "Test" (fun _ ->
  test()
)

Target "Run" (fun _ ->
  Shell.Exec ("make", "run", "../..") |> ignore
)

"Default"
  ==> "BuildAndTest"

"Default"
  ==> "Run"

"Pack"
  ==> "Install"

RunTargetOrDefault "Default"
