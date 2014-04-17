// Configuration script to create
//     MonoDevelop.FSharpBinding/MonoDevelop.FSharp.mac-linux.fsproj (unix)
//     MonoDevelop.FSharpBinding/MonoDevelop.FSharp.windows.fsproj (windows)
//     MonoDevelop.FSharpBinding/FSharpBinding.addin.xml

open System
open System.Linq
open System.Collections.Generic
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions

let FSharpVersion = "3.2.29"

let UnixPaths = 
    [ "/usr/lib/monodevelop"
      "/usr/local/monodevelop/lib/monodevelop"
      "/usr/local/lib/monodevelop"
      "/Applications/MonoDevelop.app/Contents/MacOS/lib/"
      "monodevelop"
      "/opt/mono/lib/monodevelop"
      "/Applications/Xamarin Studio.app/Contents/MacOS/lib/monodevelop" ]

let WindowsPaths = 
    [ @"C:\Program Files\Xamarin Studio"
      @"C:\Program Files\MonoDevelop"
      @"C:\Program Files (x86)\Xamarin Studio"
      @"C:\Program Files (x86)\MonoDevelop" ]

let MdCheckFile = "bin/MonoDevelop.Core.dll"

let isWindows = (Path.DirectorySeparatorChar = '\\')

let GetPath (str: string list) =
    Path.GetFullPath (String.Join (Path.DirectorySeparatorChar.ToString (), str.Select(fun (s:string) -> s.Replace ('/', Path.DirectorySeparatorChar))))

let Grep (file, regex, group:string) =
    let m = Regex.Match (File.ReadAllText (GetPath [file]), regex)
    m.Groups.[group].Value

let FileReplace (file, outFile, toReplace:string, replacement:string) =
    File.WriteAllText (GetPath [outFile], File.ReadAllText(GetPath [file]).Replace(toReplace, replacement))

let Run (file, args) =
    let currentProcess = new Process ()
    currentProcess.StartInfo.FileName <- file
    currentProcess.StartInfo.Arguments <- args
    currentProcess.StartInfo.RedirectStandardOutput <- true
    currentProcess.StartInfo.UseShellExecute <- false
    currentProcess.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
    currentProcess.Start () |> ignore
    currentProcess.StandardOutput

let defaultVersion = "4.1.6"
let args = fsi.CommandLineArgs.[1..]
let searchPaths = if isWindows then WindowsPaths else UnixPaths

Console.WriteLine "MonoDevelop F# add-in configuration script"
Console.WriteLine "------------------------------------------"

if Array.exists ((=) "--help") args then
  Console.WriteLine "Options:\n"
  Console.WriteLine "--debug\n"
  Console.WriteLine "  Enable debugging of the add-in\n"
  Console.WriteLine "--prefix=PATH\n"
  Console.WriteLine "  MonoDevelop library directory. Currently searched:\n"
  for p in searchPaths do Console.WriteLine("  {0}", p)
  exit 0

let getPrefix args = 
    let tryGet (s: string) =
        match s.Split('=') with
        | [|"--prefix"; path|] -> Some path
        | _ -> None
    Array.tryPick tryGet args

let getExeVersion exe =
    let outp = Run(exe, "/?").ReadLine()
    outp.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries).Last()

// Look for the installation directory
let getMdExe mdDir =
    ["../../XamarinStudio" 
     "../../MonoDevelop"
     "bin/XamarinStudio.exe"
     "bin/MonoDevelop.exe" ]
    |> List.map (fun p -> (GetPath[mdDir;p]))
    |> List.filter (File.Exists)
    |> function 
       | exe :: _ -> Some exe
       | _ -> None
        
let getMdExeVersion mdDir =
    match getMdExe mdDir with
    | Some exe -> getExeVersion exe
    | _ -> defaultVersion

let (mdDir, mdVersion) =
    match getPrefix args with
    | Some path ->
        path, getMdExeVersion path
    | None when (File.Exists (GetPath ["../../../monodevelop.pc.in"])) -> 
        // Local MonoDevelop build directory
        let dir = GetPath [Environment.CurrentDirectory + "/../../../build"]
        let version =
            if (File.Exists (GetPath [dir;  "../../main/configure.in"])) then 
                Grep (GetPath [dir; "../../main/configure.in"], @"AC_INIT.*?(?<ver>([0-9]|\.)+)", "ver")
            else defaultVersion 
        dir, version
    | None ->
        // Using installed MonoDevelop
        let mdDirs = 
            searchPaths 
            |> List.filter (fun p -> File.Exists (GetPath [p; MdCheckFile]))
            |> List.map (fun p -> p, getMdExeVersion p)
        match mdDirs with
        | [] -> 
            printfn "No MonoDevelop libraries found. Please install MonoDevelop or use --prefix={path-to-md-libraries}" 
            exit 0
        | [dir, version] -> 
            dir, version
        | _ -> 
            printfn "Multiple MonoDevelop library directories found. Use --prefix{path-to-md-libraries} to select one.\r\nOptions: \r\n%A" mdDirs 
            exit 0

if not isWindows then
    // Update the makefile. We don't use that on windows
    FileReplace ("Makefile.orig", "Makefile", "INSERT_MDROOT", mdDir)
    FileReplace ("Makefile", "Makefile", "INSERT_MDVERSION4", mdVersion)
    FileReplace ("Makefile", "Makefile", "INSERT_VERSION", FSharpVersion)
    
Console.WriteLine ("MonoDevelop binaries found at: {0}", mdDir)
Console.WriteLine ("Detected version: {0}", mdVersion)

let tag = if isWindows then "windows" else "mac-linux"

let fsprojFile = "MonoDevelop.FSharpBinding/MonoDevelop.FSharp." + tag + ".fsproj"
let xmlFile = "MonoDevelop.FSharpBinding/FSharpBinding.addin.xml"

FileReplace ("MonoDevelop.FSharpBinding/MonoDevelop.FSharp.fsproj.orig", fsprojFile, "INSERT_FSPROJ_MDROOT", mdDir)
FileReplace (fsprojFile, fsprojFile, "INSERT_FSPROJ_MDVERSION4", mdVersion)
FileReplace (fsprojFile, fsprojFile, "INSERT_FSPROJ_MDVERSIONDEFINE", "MDVERSION_" + mdVersion.Replace(".","_"))
FileReplace (fsprojFile, fsprojFile, "INSERT_FSPROJ_MDTAG", tag)

match getMdExe mdDir with
| Some mdExe ->
    FileReplace (fsprojFile, fsprojFile, "INSERT_FSPROJ_MDEXE", mdExe)
| None -> ()

FileReplace ("MonoDevelop.FSharpBinding/FSharpBinding.addin.xml.orig", xmlFile, "INSERT_FSPROJ_VERSION", FSharpVersion)
FileReplace (xmlFile, xmlFile, "INSERT_FSPROJ_MDVERSION4", mdVersion)

if isWindows then
  FileReplace(xmlFile, xmlFile, ".dll.mdb\"", ".pdb\"")
  for config in ["Debug";"Release"] do
    System.IO.File.WriteAllText(sprintf "build-and-install-%s.bat" (config.ToLower()),
       sprintf """
@echo off
set MSBUILD=%%WINDIR%%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%%MSBUILD%% ..\FSharp.CompilerBinding\FSharp.CompilerBinding.fsproj /p:Configuration=%s
%%MSBUILD%% MonoDevelop.FSharpBinding\MonoDevelop.FSharp.windows.fsproj /p:Configuration=%s
set MDROOT="%s"
rmdir /s /q pack
mkdir pack\windows\%s
xcopy /s /I /y dependencies\AspNetMvc4 bin\windows\%s\packages\AspNetMvc4
%%MDROOT%%\bin\mdtool.exe setup pack bin\windows\%s\FSharpBinding.dll -d:pack\windows\%s
%%MDROOT%%\bin\mdtool.exe setup install -y pack\windows\%s\MonoDevelop.FSharpBinding_%s.mpack 
"""
           config config mdDir config config config config config FSharpVersion)

