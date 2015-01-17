// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open System
open System.IO
open System.Text.RegularExpressions

Target "RestorePackages" (fun _ -> 
     "packages.config"
     |> RestorePackage (fun p ->
         { p with
             ToolPath = "..\lib\nuget\NuGet.exe" })
 )

let vimInstallDir = Environment.ExpandEnvironmentVariables( "%HOMEDRIVE%%HOMEPATH%\\vimfiles\\bundle\\fsharpbinding-vim")

let vimBinDir =  @"ftplugin\bin"
let ftpluginDir =  __SOURCE_DIRECTORY__ @@ "ftplugin"
let autoloadDir =  __SOURCE_DIRECTORY__ @@ "autoload"
let syntaxDir =  __SOURCE_DIRECTORY__ @@ "syntax"
let ftdetectDir =  __SOURCE_DIRECTORY__ @@ "ftdetect"
let syntaxCheckersDir =  __SOURCE_DIRECTORY__ @@ "syntax_checkers"

Target "BuildVim" (fun _ ->
  CreateDir vimBinDir
  MSBuildRelease vimBinDir "Build" [__SOURCE_DIRECTORY__ @@ @"..\FSharp.AutoComplete\FSharp.AutoComplete.fsproj"]
  |> Log "Build-Output: "
)

Target "Install" (fun _ ->
    DeleteDir vimInstallDir
    CreateDir vimInstallDir
    CopyDir (vimInstallDir @@ "ftplugin") ftpluginDir (fun _ -> true)
    CopyDir (vimInstallDir @@ "autoload") autoloadDir (fun _ -> true)
    CopyDir (vimInstallDir @@ "syntax") syntaxDir (fun _ -> true)
    CopyDir (vimInstallDir @@ "syntax_checkers") syntaxCheckersDir (fun _ -> true)
    CopyDir (vimInstallDir @@ "ftdetect") ftdetectDir (fun _ -> true)
    )

Target "Clean" (fun _ ->
  CleanDirs [ vimBinDir; vimInstallDir ])

Target "All" id

"BuildVim"
    ==> "Install"
    ==> "All"

RunTargetOrDefault "All"
