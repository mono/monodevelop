// --------------------------------------------------------------------------------------
// Common utilities for environment, debugging and working with project files
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open System.IO
open System.Diagnostics
open System.Reflection
open System.Globalization
open Microsoft.FSharp.Reflection
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide
open MonoDevelop.Core.Assemblies
open MonoDevelop.Core
open FSharp.CompilerBinding

module ScriptOptions =

  /// Make path absolute using the specified 'root' path if it is not already
  let makeAbsolute root (path:string) = 
    let path = path.Replace("\"","")
    if Path.IsPathRooted(path) then path
    else Path.Combine(root, path)
  
  /// Returns true if the specified file exists (and never throws exception)
  let safeExists f = 
    try File.Exists(f) with _ -> false
    
  /// Returns default directories to be used when searching for DLL files
  let getDefaultDirectories(langVersion, targetFramework) =   
    
    // Translate the target framework to an enum used by FSharp.CompilerBinding
    let fsTargetFramework = 
      if targetFramework = TargetFrameworkMoniker.NET_3_5 then FSharpTargetFramework.NET_3_5
      elif targetFramework = TargetFrameworkMoniker.NET_3_0 then FSharpTargetFramework.NET_3_0
      elif targetFramework = TargetFrameworkMoniker.NET_2_0 then FSharpTargetFramework.NET_2_0
      else FSharpTargetFramework.NET_4_0
    
    // Return all known directories
    [ // Get the location of the System DLLs
      match FSharpEnvironment.FolderOfDefaultFSharpCore(langVersion, fsTargetFramework) with 
      | Some dir -> 
          Debug.WriteLine(sprintf "Resolution: Using '%A' as the location of default FSharp.Core.dll" dir)
          yield dir
      | None -> 
          Debug.WriteLine(sprintf "Resolution: Unable to find a default location for FSharp.Core.dll")

      yield System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() 
    ]
                
  /// Resolve assembly in the specified list of directories
  let rec resolveAssembly dirs asm =
    match dirs with 
    | dir::dirs ->
        let asmPath = Path.Combine(dir, asm)
        let any = List.tryFind safeExists [ asmPath + ".dll" ]
        match any with 
        | Some(file) -> Some(file)
        | _ -> resolveAssembly dirs asm
    | [] -> None
  

// --------------------------------------------------------------------------------------
// Common utilities for working with files & extracting information from 
// MonoDevelop objects (e.g. references, project items etc.)
// --------------------------------------------------------------------------------------

module CompilerArguments = 
  /// Wraps the given string between double quotes
  let wrapFile (s:string) = if s.StartsWith "\"" then s else "\"" + s + "\""  

  /// When creating new script file on Mac, the filename we get sometimes 
  /// has a name //foo.fsx, and as a result 'Path.GetFullPath' throws in the F#
  /// language service - this fixes the issue by inventing nicer file name.
  let fixFileName path = 
    if (try Path.GetFullPath(path) |> ignore; true
        with _ -> false) then path
    else 
      let dir = 
        if Environment.OSVersion.Platform = PlatformID.Unix ||  
           Environment.OSVersion.Platform = PlatformID.MacOSX then
          Environment.GetEnvironmentVariable("HOME") 
        else
          Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")
      Path.Combine(dir, Path.GetFileName(path))
  
  /// Is the specified extension supported F# file?
  let supportedExtension ext = 
    [".fsscript"; ".fs"; ".fsx"; ".fsi"] |> Seq.exists (fun sup ->
        String.Compare(ext, sup, true) = 0)

  /// Is the specified extension used by F# script files?
  let fsharpScriptExtension ext = 
    [".fsscript"; ".fsx"] |> Seq.exists (fun sup ->
        String.Compare(ext, sup, true) = 0)

  /// Generates references for the current project & configuration as a 
  /// list of strings of the form [ "-r:<full-path>"; ... ]
  let generateReferences (items:ProjectItemCollection, langVersion, targetFramework, configSelector, shouldWrap) = 
   [ // Should we wrap references in "..."
    let wrapf = if shouldWrap then wrapFile else id
    let files = 
      [ for ref in items.GetAll<ProjectReference>() do
          for file in ref.GetReferencedFileNames(configSelector) do
            // The plain reference text "FSharp.Core" is used in Visual Studio .fsproj files.
            // On MonoDevelop+windows this is incorrectly resolved. We just skip reference text of this form,
            // and rely on the default directory search below.
            if not (file.EndsWith("FSharp.Core.dll", true, CultureInfo.InvariantCulture)) || ref.IsValid then 
                 yield file ]
    
    // If 'mscorlib.dll' and 'FSharp.Core.dll' is not in the set of references, we need to 
    // resolve it and add it. We look in the directories returned by getDefaultDirectories(),
    // where no includes are given.
    for assumedFile in ["mscorlib"; "FSharp.Core"] do 
      let coreRef = files |> List.tryFind (fun fn -> fn.EndsWith(assumedFile + ".dll", true, CultureInfo.InvariantCulture) || 
                                                     fn.EndsWith(assumedFile, true, CultureInfo.InvariantCulture))
      match coreRef with
      | None ->
        let dirs = ScriptOptions.getDefaultDirectories(langVersion, targetFramework) 
        match ScriptOptions.resolveAssembly dirs assumedFile with
        | Some fn -> yield "-r:" + wrapf(fn)
        | None -> Debug.WriteLine(sprintf "Resolution: Assembly resolution failed when trying to find default reference for '%s'!" assumedFile)
      | Some r -> 
        Debug.WriteLine(sprintf "Resolution: Found '%s' reference '%s'" assumedFile r)
      
    for file in files do 
      yield "-r:" + wrapf(file) ]


  /// Generates command line options for the compiler specified by the 
  /// F# compiler options (debugging, tail-calls etc.), custom command line
  /// parameters and assemblies referenced by the project ("-r" options)
  let generateCompilerOptions (fsconfig:FSharpCompilerParameters, reqLangVersion, targetFramework, items, configSelector, shouldWrap) =
    let dashr = generateReferences (items, reqLangVersion, targetFramework, configSelector, shouldWrap) |> Array.ofSeq
    let defines = fsconfig.DefineConstants.Split([| ';'; ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    [  yield "--noframework"
       for symbol in defines do yield "--define:" + symbol
       yield if fsconfig.DebugSymbols then  "--debug+" else  "--debug-"
       yield if fsconfig.Optimize then "--optimize+" else "--optimize-"
       yield if fsconfig.GenerateTailCalls then "--tailcalls+" else "--tailcalls-"
       // TODO: This currently ignores escaping using "..."
       for arg in fsconfig.OtherFlags.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) do
         yield arg 
       yield! dashr ] 
  

  /// Get source files of the current project (returns files that have 
  /// build action set to 'Compile', but not e.g. scripts or resources)
  let getSourceFiles (items:ProjectItemCollection) = 
    [ for file in items.GetAll<ProjectFile>() do
        if file.BuildAction = "Compile" && file.Subtype <> Subtype.Directory then 
          yield file.Name.ToString() ]

  /// Creates a relative path from one file or folder to another. 
  let makeRelativePath (root:string) (file:string) = 
    let file = Uri(file)
    let sep = Path.DirectorySeparatorChar.ToString()
    let root = Uri(if root.EndsWith(sep) then root else root + sep + "dummy" )
    root.MakeRelativeUri(file).ToString().Replace("/", sep)


  /// Create a list containing same items as the 'items' parameter that preserves
  /// the order specified by 'ordered' (and new items are at the end)  
  let getItemsInOrder root items ordered relative =
    let ordered = [| for f in ordered -> FilePath(Path.Combine(root, f)).CanonicalPath.FullPath.ToString() |]
    let itemsSet, orderedSet = set items, set ordered
    let keep = Set.intersect orderedSet itemsSet
    let ordered = ordered |> Array.filter (fun el -> keep.Contains el)
    let procf = if relative then makeRelativePath root else id
    let unorderedItems = items |> List.filter (fun el -> not (orderedSet.Contains el))
    [ for f in ordered -> procf f
      for f in unorderedItems -> procf f ]

    
  /// Generate inputs for the compiler (excluding source code!); returns list of items 
  /// containing resources (prefixed with the --resource parameter)
  let generateOtherItems (items:ProjectItemCollection) = 
    [ for file in items.GetAll<ProjectFile>() do
        match file.BuildAction with
        | _ when file.Subtype = Subtype.Directory -> ()
        | "EmbeddedResource" -> 
            let fileName = file.Name.ToString()
            let logicalResourceName = file.ProjectVirtualPath.ToString().Replace("\\",".").Replace("/",".")
            yield "--resource:" + wrapFile fileName + "," + wrapFile logicalResourceName
        | "None" | "Content" | "Compile" -> ()
        | s -> failwith("Items of type '" + s + "' not supported") ]

  let getToolPath (pathsToSearch:seq<string>) (extensions:seq<string>) (toolName:string) =
    let filesToSearch = Seq.map (fun x -> toolName + x) extensions

    let tryFindPathAndFile (filesToSearch:seq<string>) (path:string) =
      try
        let candidateFiles = Directory.GetFiles(path)

        let fileIfExists candidateFile =
          Seq.tryFind (fun x -> Path.Combine(path,x) = candidateFile) filesToSearch
        match Seq.tryPick fileIfExists candidateFiles with
          | Some x -> Some(path,x)
          | None -> None

      with
        | e -> None

    Seq.tryPick (tryFindPathAndFile filesToSearch) pathsToSearch


  let getShellToolPath (extensions:seq<string>) (toolName:string)  =
    let pathVariable = Environment.GetEnvironmentVariable("PATH")
    let searchPaths = pathVariable.Split [| IO.Path.PathSeparator  |]
    getToolPath searchPaths extensions toolName

  /// Get full path to tool
  let getEnvironmentToolPath (runtime:TargetRuntime) (framework:TargetFramework) (extensions:seq<string>) (toolName:string) =
    let pathsToSearch = runtime.GetToolsPaths(framework)
    getToolPath pathsToSearch extensions toolName

  let getDefaultTargetFramework (runtime:TargetRuntime) =
    let newest_net_framework_folder (best:TargetFramework,best_version:int[]) (candidate_framework:TargetFramework) =
      if runtime.IsInstalled(candidate_framework) && candidate_framework.Id.Identifier = TargetFrameworkMoniker.ID_NET_FRAMEWORK then
        let version = candidate_framework.Id.Version
        let parsed_version_s = (if version.[0] = 'v' then version.[1..] else version).Split('.')
        let parsed_version =
          try
            Array.map (fun x -> int x) parsed_version_s
          with
            | _ -> [| 0 |]
        let mutable level = 0
        let mutable cont = true
        let min_level = min parsed_version.Length best_version.Length
        let mutable new_best = false
        while cont && level < min_level do
          if parsed_version.[level] > best_version.[level] then
            new_best <- true
            cont <- false
          elif best_version.[level] > parsed_version.[level] then
            cont <- false
          else
            cont <- true
          level <- level + 1
        if new_best then
          (candidate_framework, parsed_version)
        else
          (best,best_version)
      else
        (best,best_version)
    let candidate_frameworks = MonoDevelop.Core.Runtime.SystemAssemblyService.GetTargetFrameworks()
    let first = Seq.head candidate_frameworks
    let best_info = Seq.fold newest_net_framework_folder (first,[| 0 |]) candidate_frameworks
    fst best_info

  let getDefaultInteractive() =

    let runtime = IdeApp.Preferences.DefaultTargetRuntime
    let framework = getDefaultTargetFramework runtime

    match getEnvironmentToolPath runtime framework [|""; ".exe"; ".bat" |] "fsharpi" with
    | Some(dir,file)-> Some(Path.Combine(dir,file))
    | None->
    match getShellToolPath [| ""; ".exe"; ".bat" |] "fsharpi" with
    | Some(dir,file)-> Some(Path.Combine(dir,file))
    | None->
    match getEnvironmentToolPath runtime framework [|""; ".exe"; ".bat" |] "fsi" with
    | Some(dir,file)-> Some(Path.Combine(dir,file))
    | None->
    match getShellToolPath [| ""; ".exe"; ".bat" |] "fsi" with
    | Some(dir,file)-> Some(Path.Combine(dir,file))
    | None-> 
    match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler(FSharpCompilerVersion.LatestKnown) with
    | Some(dir) when ScriptOptions.safeExists(Path.Combine(dir, "fsi.exe")) ->  
        Some(Path.Combine(dir,"fsi.exe"))
    | _ -> None

  let getCompilerFromEnvironment (runtime:TargetRuntime) (framework:TargetFramework) =
    match getEnvironmentToolPath runtime framework [| ""; ".exe"; ".bat" |] "fsharpc" with
    | Some(dir,file) -> Some(Path.Combine(dir,file))
    | None ->
    match getEnvironmentToolPath runtime framework [| ""; ".exe"; ".bat" |] "fsc" with
    | Some(dir,file) -> Some(Path.Combine(dir,file))
    | None -> None
    
  // Only used when xbuild support is not enabled. When xbuild is enabled, the .targets 
  // file finds FSharp.Build.dll which finds the F# compiler.
  let getDefaultFSharpCompiler() =
  
    let runtime = IdeApp.Preferences.DefaultTargetRuntime
    let framework = getDefaultTargetFramework runtime

    match getCompilerFromEnvironment runtime framework with
    | Some(result)-> Some(result)
    | None->
    match getShellToolPath [| ""; ".exe"; ".bat" |] "fsharpc" with
    | Some(dir,file) -> Some(Path.Combine(dir,file))
    | None ->
    match getShellToolPath [| ""; ".exe"; ".bat" |] "fsc" with
    | Some(dir,file) -> Some(Path.Combine(dir,file))
    | None -> 
    match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler(FSharpCompilerVersion.LatestKnown) with
    | Some(dir) when ScriptOptions.safeExists(Path.Combine(dir, "fsc.exe")) ->  
        Some(Path.Combine(dir,"fsc.exe"))
    | _ -> None

