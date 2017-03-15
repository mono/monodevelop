#r @"./packages/FAKE/tools/FakeLib.dll"
#r "System.Xml"
#r "System.Xml.Linq"
open Fake
open System.IO
open System
open System.Linq
open System.Xml.Linq

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

Target "GenerateFastBuildProjects" (fun _ ->
  let (/) a b = Path.Combine(a, b)
  let nsuri = "http://schemas.microsoft.com/developer/msbuild/2003"

  let absoluteFromRelative projectPath relPath =
    let projectFolder = Path.GetDirectoryName projectPath
    let full = projectFolder / relPath
    Uri(full).LocalPath |> Path.GetFullPath

  let path = __SOURCE_DIRECTORY__
  let projects =
    Directory.GetFiles(path, "*.fsproj", SearchOption.AllDirectories)
    |> Array.filter(fun p -> not (p.Contains "Samples" || p.Contains "fastbuild"))

  for projectPath in projects do
    let projectFolderUri = Uri(projectPath)
    let ns = XNamespace.Get nsuri
    let doc = XDocument.Load projectPath
    let references = doc.Descendants(ns + "ProjectReference").ToList()
    let firstOrDefault seq = Enumerable.FirstOrDefault(seq)

    for re in references do
      let inc = re.Attribute(XName.Get "Include").Value
      let fullPath = absoluteFromRelative projectPath inc
      let projectFolder = Path.GetDirectoryName fullPath
      let referenced = XDocument.Load fullPath
      let getDescendant name = (referenced.Descendants(ns + name) |> firstOrDefault).Value
      let assemblyName = getDescendant "AssemblyName"

      let outputPath =
          match Path.GetFileNameWithoutExtension(fullPath) with
          | "MonoDevelop.PackageManagement.Tests" -> @"..\..\..\..\build\tests"
          | "MonoDevelop.PackageManagement" -> @"..\..\..\build\AddIns\MonoDevelop.PackageManagement"
          | _ -> getDescendant "OutputPath"

      let outputExtension = if getDescendant "OutputType" = "Exe" then ".exe" else ".dll"
      let hintPath = projectFolder/outputPath/(assemblyName + outputExtension)

      let hintPathUri = Uri(hintPath)
      let hintPathAbsolute = hintPathUri.LocalPath |> Path.GetFullPath
      if not (File.Exists hintPathAbsolute) then
          failwithf "Did not find %s" hintPathAbsolute
      let relativeHintPath = projectFolderUri.MakeRelativeUri(hintPathUri).ToString().Replace("/", "\\")
      let replacement = XElement(ns + "Reference", XAttribute((XName.Get "Include"), assemblyName), XElement(ns + "HintPath", relativeHintPath))
      re.ReplaceWith replacement
    let fastbuildPath = projectPath.Replace(".fsproj", ".fastbuild.fsproj")
    printfn "Saving %s" fastbuildPath
    doc.Save fastbuildPath)

"Default"
  ==> "BuildAndTest"

"Default"
  ==> "Run"

"Pack"
  ==> "Install"

RunTargetOrDefault "Default"
