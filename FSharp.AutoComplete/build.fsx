// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open System.IO
open System.Text.RegularExpressions

Target "RestorePackages" (fun _ -> 
     "packages.config"
     |> RestorePackage (fun p ->
         { p with
             ToolPath = "../lib/nuget/NuGet.exe" })
 )

let buildDir = "./bin/Debug/"
let buildReleaseDir = "./bin/Release/"
let unitTestDir  = "./test/unit/"
let unitTestBuildDir  = unitTestDir + "build"
let integrationTestDir = "./test/integration/"

Target "BuildDebug" (fun _ ->
  MSBuildDebug buildDir "Build" ["./FSharp.AutoComplete.fsproj"]
  |> Log "Build-Output: "
)

Target "BuildRelease" (fun _ ->
  MSBuildRelease buildReleaseDir "Build" ["./FSharp.AutoComplete.fsproj"]
  |> Log "Build-Output: "
)

Target "BuildEmacs" (fun _ ->
  MSBuildDebug "../emacs/bin" "Build" ["./FSharp.AutoComplete.fsproj"]
  |> Log "Build-Output: "
)

Target "BuildUnitTest" (fun _ ->
  !! (unitTestDir + "/*/*.fsproj")
    |> MSBuildDebug unitTestBuildDir "Build"
    |> Log "TestBuild-Output: "
)

Target "UnitTest" (fun _ ->
  !! (unitTestBuildDir + "/*Tests.dll")
    |> NUnit (fun p ->
      {p with
         DisableShadowCopy = true
         Framework = "v4.0.30319"
         ToolName = "nunit-console-x86.exe"
         OutputFile = unitTestBuildDir + "/TestResults.xml"})
)

let integrationTests =
  !! (integrationTestDir + "/**/*Runner.fsx")

let runIntegrationTest (fn: string) : bool =
  let dir = Path.GetDirectoryName fn

  tracefn "Running FSIHelper '%s', '%s', '%s'"  FSIHelper.fsiPath dir fn
  let b, msgs = FSIHelper.executeFSI dir fn []
  if not b then
    for msg in msgs do
      traceError msg.Message

  // Normalize output files so that a simple
  // `git diff` will be clean if the tests passed.
  for fn in !! (dir + "/*.txt") do
    let lines = File.ReadAllLines fn
    for i in [ 0 .. lines.Length - 1 ] do
      lines.[i] <- Regex.Replace(lines.[i],
                                 "/.*?FSharp.AutoComplete/test/(.*?(\"|$))",
                                 @"<absolute path removed>/test/$1")

    File.WriteAllLines (fn, lines)
  b

Target "IntegrationTest" (fun _ ->
  let runOk =
   [ for i in integrationTests do
       yield runIntegrationTest i ]
   |> Seq.forall id
  if not runOk then
    failwith "Integration tests did not run successfully"
  else

    let ok, out, err =
      Git.CommandHelper.runGitCommand
                        "."
                        ("diff --exit-code " + integrationTestDir)
    if not ok then
      trace (toLines out)
      failwithf "Integration tests failed:\n%s" err
)

Target "Test" id
Target "All" id

"RestorePackages"
  ==> "BuildUnitTest"
  ==> "UnitTest"

"RestorePackages"
  ==> "BuildDebug"
  ==> "IntegrationTest"

"UnitTest" ==> "Test"
"IntegrationTest" ==> "Test"

"BuildDebug" ==> "All"
"Test" ==> "All"
  
RunTargetOrDefault "BuildDebug"

