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

// --------------------------------------------------------------------------------------
// Common utilities for working with files & extracting information from 
// MonoDevelop objects (e.g. references, project items etc.)
// --------------------------------------------------------------------------------------

module CompilerArguments = 

  /// Wraps the given string between double quotes
  let wrapFile (s:string) = if s.StartsWith "\"" then s else "\"" + s + "\"" 
  
  // Translate the target framework to an enum used by FSharp.CompilerBinding
  let getTargetFramework (targetFramework:TargetFrameworkMoniker) = 
      if targetFramework = TargetFrameworkMoniker.NET_3_5 then FSharpTargetFramework.NET_3_5
      elif targetFramework = TargetFrameworkMoniker.NET_3_0 then FSharpTargetFramework.NET_3_0
      elif targetFramework = TargetFrameworkMoniker.NET_2_0 then FSharpTargetFramework.NET_2_0
      elif targetFramework = TargetFrameworkMoniker.NET_4_0 then FSharpTargetFramework.NET_4_0
      elif targetFramework = TargetFrameworkMoniker.NET_4_5 then FSharpTargetFramework.NET_4_5
      else FSharpTargetFramework.NET_4_5
  
  module Project =
                                    
      let isPortable (project: DotNetProject) =
        not (String.IsNullOrEmpty project.TargetFramework.Id.Profile)
      
      let getPortableReferences (project: DotNetProject) configSelector = 
        let portableReferences =
            // create a new target framework  moniker, the default one is incorrect for portable unless the project type is PortableDotnetProject
            // which has the default moniker profile of ".NETPortable" rather than ".NETFramework".  We cant use a PortableDotnetProject as this 
            // requires adding a guid flavour, which breaks compatiability with VS until the MD project system is refined to support projects the way VS does.
            let frameworkMoniker = TargetFrameworkMoniker (TargetFrameworkMoniker.ID_PORTABLE, project.TargetFramework.Id.Version, project.TargetFramework.Id.Profile)
            let assemblyDirectoryName = frameworkMoniker.GetAssemblyDirectoryName()

            project.TargetRuntime.GetReferenceFrameworkDirectories() 
            |> Seq.tryFind (fun fd ->  Directory.Exists(fd.Combine([|TargetFrameworkMoniker.ID_PORTABLE|]).ToString()))
            |> function
               | Some fd -> Directory.EnumerateFiles(Path.Combine(fd.ToString(), assemblyDirectoryName), "*.dll")
               | None -> Seq.empty

        project.GetReferencedAssemblies(configSelector) 
        |> Seq.append portableReferences
        |> set 
        |> Set.map ((+) "-r:")
        |> Set.toList

  module ReferenceResolution =

    let tryGetDefaultReference langVersion targetFramework filename (extrapath: string option) =
          let dirs = 
            match extrapath with
            | Some path -> path :: FSharpEnvironment.getDefaultDirectories(langVersion, targetFramework)
            | None -> FSharpEnvironment.getDefaultDirectories(langVersion, targetFramework)
          FSharpEnvironment.resolveAssembly dirs filename

    let tryGetReferenceFromAssembly (assemblyRef:string) (refToFind:string) =
        let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(assemblyRef)
        assembly.MainModule.AssemblyReferences
        |> Seq.tryFind (fun name -> name.Name = refToFind)
        |> Option.bind (fun assemblyNameRef -> let resolved = Mono.Cecil.DefaultAssemblyResolver().Resolve(assemblyNameRef)
                                               Some (resolved.MainModule.FullyQualifiedName))

  let resolutionFailedMessage = sprintf "Resolution: Assembly resolution failed when trying to find default reference for: %s"
       
  /// Generates references for the current project & configuration as a 
  /// list of strings of the form [ "-r:<full-path>"; ... ]
  let generateReferences (project: DotNetProject, langVersion, targetFramework, configSelector, shouldWrap) = 
   if Project.isPortable project then
        Project.getPortableReferences project configSelector 
   else
       let wrapf = if shouldWrap then wrapFile else id
       
       [
        let refs =  project.GetReferencedAssemblies(configSelector) 
        let projectReferences =
            refs
            // The unversioned reference text "FSharp.Core" is used in Visual Studio .fsproj files.  This can sometimes be 
            // incorrectly resolved so we just skip this simple reference form and rely on the default directory search below.
            |> Seq.filter (fun (ref: string) -> not (ref.EndsWith("FSharp.Core")))
            |> set
             
        let find assemblyName=
            projectReferences
            |> Seq.tryFind (fun fn -> fn.EndsWith(assemblyName + ".dll", true, CultureInfo.InvariantCulture) 
                                      || fn.EndsWith(assemblyName, true, CultureInfo.InvariantCulture))
             
        // If 'mscorlib.dll' or 'FSharp.Core.dll' is not in the set of references, we try to resolve and add them. 
        match find "FSharp.Core", find "mscorlib" with
        | None, Some mscorlib ->
            // if mscorlib is founbd without FSharp.Core yield fsharp.core in the same base dir as mscorlib
            // falling back to one of the default directories
            let extraPath = Some (Path.GetDirectoryName (mscorlib))
            match ReferenceResolution.tryGetDefaultReference langVersion targetFramework "FSharp.Core" extraPath with
            | Some ref -> yield "-r:" + wrapf(ref)
            | None -> LoggingService.LogWarning(resolutionFailedMessage "FSharp.Core")

        | Some fsharpCore, None ->
            // If FSharp.Core is found without mscorlib yield an mscorlib thats referenced from FSharp.core
            match ReferenceResolution.tryGetReferenceFromAssembly fsharpCore "mscorlib" with
            | Some resolved -> yield "-r:" + wrapf(resolved)
            | None -> LoggingService.LogWarning(resolutionFailedMessage "mscorlib")

        | None, None ->
            // If neither are found yield the default fsharp.core and mscorlib
            match ReferenceResolution.tryGetDefaultReference langVersion targetFramework "FSharp.Core" None with
            | Some ref -> yield "-r:" + wrapf(ref)
            | None -> LoggingService.LogWarning(resolutionFailedMessage "FSharp.Core")

            match ReferenceResolution.tryGetDefaultReference langVersion targetFramework "mscorlib" None with
            | Some ref -> yield "-r:" + wrapf(ref)
            | None -> LoggingService.LogWarning(resolutionFailedMessage "mscorlib")
        | _ -> () // found them both, no action needed
                  
        for file in projectReferences do 
          yield "-r:" + wrapf(file) ]

  let getCurrentConfigurationOrDefault (proj:Project) =
     match IdeApp.Workspace with
     | ws when ws <> null && ws.ActiveConfiguration <> null -> ws.ActiveConfiguration
     | _ -> ConfigurationSelector.Default

  let generateDebug (config:FSharpCompilerParameters) =
      match config.DebugSymbols, config.DebugType with
      | true, typ ->
        match typ with
        | "full" -> "--debug:full"
        | "pdbonly" -> "--debug:pdbonly"
        | _ -> "--debug+"
      | false, _ -> "--debug-"

  /// Generates command line options for the compiler specified by the 
  /// F# compiler options (debugging, tail-calls etc.), custom command line
  /// parameters and assemblies referenced by the project ("-r" options)
  let generateCompilerOptions (project: DotNetProject, fsconfig:FSharpCompilerParameters, reqLangVersion, targetFramework, configSelector, shouldWrap) =
    let dashr = generateReferences (project, reqLangVersion, targetFramework, configSelector, shouldWrap) |> Array.ofSeq
    let defines = fsconfig.DefineConstants.Split([| ';'; ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    let currentProjectConfig = getCurrentConfigurationOrDefault project
    let outputFilename = project.GetOutputFileName(currentProjectConfig).ToString ()
    [  yield "--noframework"
       yield "-o:" + outputFilename
       for symbol in defines do yield "--define:" + symbol
       yield generateDebug fsconfig
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
        | _ -> ()] // failwith("Items of type '" + s + "' not supported") ]

  let private getToolPath (pathsToSearch:seq<string>) (extensions:seq<string>) (toolName:string) =
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

  let private getShellToolPath (extensions:seq<string>) (toolName:string)  =
    let pathVariable = Environment.GetEnvironmentVariable("PATH")
    let searchPaths = pathVariable.Split [| IO.Path.PathSeparator  |]
    getToolPath searchPaths extensions toolName

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
    match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler None with
    | Some(dir) when FSharpEnvironment.safeExists(Path.Combine(dir, "fsi.exe")) ->  
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
    match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler None with
    | Some(dir) when FSharpEnvironment.safeExists(Path.Combine(dir, "fsc.exe")) ->  
        Some(Path.Combine(dir,"fsc.exe"))
    | _ -> None

  let getArgumentsFromProject (proj:DotNetProject) =
        let config = getCurrentConfigurationOrDefault proj
        let projConfig = proj.GetConfiguration(config) :?> DotNetProjectConfiguration
        let fsconfig = projConfig.CompilationParameters :?> FSharpCompilerParameters
        generateCompilerOptions (proj, fsconfig, None, getTargetFramework projConfig.TargetFramework.Id, config, false) |> Array.ofList

  let getDefineSymbols (fileName:string) (project: Project option) =
    [if (fileName.EndsWith(".fsx") || fileName.EndsWith(".fsscript"))
     then yield "INTERACTIVE"
     else yield "COMPILED"
    
     let workspace = IdeApp.Workspace |> Option.ofNull
     let configuration =
        match workspace, project with
         | None, Some proj ->
             //as there is no workspace use the default configuration for the project
             Some (proj.GetConfiguration(proj.DefaultConfiguration.Selector))
         | Some workspace, Some project ->
             Some (project.GetConfiguration(workspace.ActiveConfiguration))
         | _ -> None

     match configuration with
     | Some config  ->
         match config with
         | :? DotNetProjectConfiguration as config -> yield! config.GetDefineSymbols() 
         | _ -> ()
     | None -> () ]
