// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open System
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
  for fn in !! (dir + "/*.txt") ++ (dir + "/*.json") do
    let lines = File.ReadAllLines fn
    for i in [ 0 .. lines.Length - 1 ] do
      if Path.DirectorySeparatorChar = '/' then
        lines.[i] <- Regex.Replace(lines.[i],
                                   "/.*?FSharp.AutoComplete/test/(.*?(\"|$))",
                                   "<absolute path removed>/test/$1")
      else
        if Path.GetExtension fn = ".json" then
          lines.[i] <- Regex.Replace(lines.[i].Replace(@"\\", "/"),
                                     "[A-Z]:/.*?FSharp.AutoComplete/test/(.*?(\"|$))",
                                     "<absolute path removed>/test/$1")
        else
          lines.[i] <- Regex.Replace(lines.[i].Replace('\\','/'),
                                     "[A-Z]:/.*?FSharp.AutoComplete/test/(.*?(\"|$))",
                                     "<absolute path removed>/test/$1")

    // Write manually to ensure \n line endings on all platforms
    using (new StreamWriter(fn))
    <| fun f ->
        for line in lines do
          f.Write(line)
          f.Write('\n')
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


Target "BuildEmacs" (fun _ ->
  MSBuildDebug "../emacs/bin" "Build" ["./FSharp.AutoComplete.fsproj"]
  |> Log "Build-Output: "
)

module Emacs =
  let emacsd = "../emacs/"
  let srcFiles = !! (emacsd + "*.el")

  let testd = emacsd + "test/"
  let utils = !! (testd + "/test-common.el")

  let tmpd = emacsd + "tmp/"
  let bind = emacsd + "bin/"

  let exe =
    match buildServer with
    | AppVeyor -> Path.GetFullPath "../emacs-local/bin/emacs.exe"
    | _ -> @"emacs"

  let compileOpts =
    sprintf """--batch --eval "(package-initialize)" --eval "(add-to-list 'load-path \"%s\")" --eval "(setq byte-compile-error-on-warn t)" -f batch-byte-compile %s"""
      ((Path.GetFullPath emacsd).Replace(@"\", @"\\").TrimEnd('\\'))
      (String.concat " " [ for f in srcFiles do yield f ])

  let makeLoad glob =
    [ for f in glob do yield "-l " + f ]
    |> String.concat " "

Target "EmacsTest" (fun _ ->
  if not (Directory.Exists (Path.GetFullPath Emacs.tmpd)) then
    Directory.CreateDirectory Emacs.tmpd |> ignore
  let home = Environment.GetEnvironmentVariable("HOME")
  Environment.SetEnvironmentVariable("HOME", Path.GetFullPath Emacs.tmpd)

  let loadFiles = Emacs.makeLoad Emacs.utils

  let tests =
    [ yield Emacs.exe, loadFiles + " --batch -f run-fsharp-unit-tests"
      // AppVeyor doesn't currently run the integration tests
      if buildServer <> AppVeyor then
        yield Emacs.exe, loadFiles + " --batch -f run-fsharp-integration-tests"
      yield Emacs.exe, Emacs.compileOpts ]

  ProcessTestRunner.RunConsoleTests
    (fun p -> { p with WorkingDir = Emacs.emacsd })
    tests

  Environment.SetEnvironmentVariable("HOME", home)
)


Target "Test" id
Target "All" id

"RestorePackages"
  ==> "BuildUnitTest"
  ==> "UnitTest"

"RestorePackages"
  ==> "BuildDebug"
  ==> "IntegrationTest"

"RestorePackages"
  ==> "BuildEmacs"
  ==> "EmacsTest"

"EmacsTest" ==> "Test"
"UnitTest" ==> "Test"
"IntegrationTest" ==> "Test"

"BuildDebug" ==> "All"
"Test" ==> "All"

RunTargetOrDefault "BuildDebug"

