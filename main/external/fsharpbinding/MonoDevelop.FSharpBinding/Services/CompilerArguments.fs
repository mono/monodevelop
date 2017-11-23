// --------------------------------------------------------------------------------------
// Common utilities for environment, debugging and working with project files
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open System.IO
open System.Reflection
open System.Globalization
open System.Runtime.Versioning
open MonoDevelop.Projects
open MonoDevelop.Ide
open MonoDevelop.Core.Assemblies
open MonoDevelop.Core
open ExtCore
open ExtCore.Control
open Microsoft.FSharp.Compiler.SourceCodeServices

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
      elif targetFramework = TargetFrameworkMoniker.NET_4_6 then FSharpTargetFramework.NET_4_6
      elif targetFramework = TargetFrameworkMoniker.NET_4_6_1 then FSharpTargetFramework.NET_4_6_1
      elif targetFramework = TargetFrameworkMoniker.NET_4_6_2 then FSharpTargetFramework.NET_4_6_2
      else FSharpTargetFramework.NET_4_6_2

  module Project =
      ///Use the IdeApp.Workspace active configuration failing back to proj.DefaultConfiguration then ConfigurationSelector.Default
      let getCurrentConfigurationOrDefault (proj:Project) =
          match IdeApp.Workspace with
          | ws when ws <> null && ws.ActiveConfiguration <> null -> ws.ActiveConfiguration
          | _ -> if proj <> null then proj.DefaultConfiguration.Selector
                 else ConfigurationSelector.Default

      let isPortable (project: DotNetProject) =
          not (String.IsNullOrEmpty project.TargetFramework.Id.Profile)
      
      let isDotNetCoreProject (project:DotNetProject) =
          let properties = project.MSBuildProject.EvaluatedProperties
          properties.HasProperty ("TargetFramework") || properties.HasProperty ("TargetFrameworks")

      let isOrReferencesPortableProject (project: DotNetProject) =
          isPortable project ||
          project.GetReferencedAssemblyProjects(getCurrentConfigurationOrDefault project)
          |> Seq.exists isPortable

      let getAssemblyLocations (reference:ProjectReference) =
          let tryGetFromHintPath() =
              if reference.HintPath.IsNotNull then
                  let path = reference.HintPath.FullPath |> string
                  let path = path.Replace("/Library/Frameworks/Mono.framework/External",
                                          "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono")
                  if File.Exists path then
                      [path]
                  else
                      // try and resolve from GAC
                      [reference.HintPath.FileName]
              else
                  []

          match reference.ReferenceType with
          | ReferenceType.Assembly ->
              tryGetFromHintPath()
          | ReferenceType.Package ->
              if isNull reference.Package then
                  tryGetFromHintPath()
              else
                  if reference.Include <> "System" then
                      let assembly =
                           reference.Package.Assemblies
                           |> Seq.tryFind (fun a -> a.Name = reference.Include || a.FullName = reference.Include)
                      match assembly with
                      | Some asm -> [asm.Location]
                      | None -> []
                  else
                      []

          | ReferenceType.Project ->
              let referencedProject = reference.Project :?> DotNetProject
              let reference =
                  referencedProject.GetReferencedAssemblyProjects (getCurrentConfigurationOrDefault referencedProject)
                  |> Seq.tryFind(fun p -> p.Name = reference.Reference)

              match reference with
                  | Some ref ->
                      let output = ref.GetOutputFileName(getCurrentConfigurationOrDefault ref)
                      [output.FullPath.ToString()]
                  | _ -> []
          | _ -> []

      let getDefaultTargetFramework (runtime:TargetRuntime) =
          let newest_net_framework_folder (best:TargetFramework,best_version:int[]) (candidate_framework:TargetFramework) =
              if runtime.IsInstalled(candidate_framework) && candidate_framework.Id.Identifier = TargetFrameworkMoniker.ID_NET_FRAMEWORK then
                  let version = candidate_framework.Id.Version
                  let parsed_version_s = (if version.[0] = 'v' then version.[1..] else version).Split('.')
                  let parsed_version =
                      try
                          Array.map int parsed_version_s
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

      let portableReferences (project: DotNetProject) =
          // create a new target framework  moniker, the default one is incorrect for portable unless the project type is PortableDotnetProject
          // which has the default moniker profile of ".NETPortable" rather than ".NETFramework".  We cant use a PortableDotnetProject as this
          // requires adding a guid flavour, which breaks compatiability with VS until the MD project system is refined to support projects the way VS does.
          let frameworkMoniker = TargetFrameworkMoniker (TargetFrameworkMoniker.ID_PORTABLE, project.TargetFramework.Id.Version, project.TargetFramework.Id.Profile)
          let assemblyDirectoryName = frameworkMoniker.GetAssemblyDirectoryName()
          project.TargetRuntime.GetReferenceFrameworkDirectories()
          |> Seq.tryFind (fun fd -> Directory.Exists(fd.Combine([|TargetFrameworkMoniker.ID_PORTABLE|]).ToString()))
          |> function
             | Some fd -> Directory.EnumerateFiles(Path.Combine(fd.ToString(), assemblyDirectoryName), "*.dll")
             | None -> Seq.empty

      let getPortableReferences (project: DotNetProject) =
          project.References
          |> Seq.collect getAssemblyLocations
          |> Seq.append (portableReferences project)
          |> set
          |> Set.toList

  module ReferenceResolution =
    
    let tryGetDefaultReference langVersion targetFramework filename (extrapath: string option) =
        let dirs =
            match extrapath with
            | Some path -> path :: FSharpEnvironment.getDefaultDirectories(langVersion, targetFramework)
            | None -> FSharpEnvironment.getDefaultDirectories(langVersion, targetFramework)
        FSharpEnvironment.resolveAssembly dirs filename

  let resolutionFailedMessage (n:string) = String.Format ("Resolution: Assembly resolution failed when trying to find default reference for: {0}", n)
  /// Generates references for the current project & configuration as a
  /// list of strings of the form [ "-r:<full-path>"; ... ]
  let generateReferences (project: DotNetProject, projectAssemblyReferences: AssemblyReference seq, langVersion, targetFramework, configSelector, shouldWrap) =
   if Project.isPortable project then
       [for ref in Project.getPortableReferences project do
            yield "-r:" + ref]
   else
       let isAssemblyPortable path =
           try
               let assembly = Assembly.ReflectionOnlyLoadFrom path

               let referencesSystemRuntime() =
                   assembly.GetReferencedAssemblies()
                   |> Seq.exists (fun a -> a.Name = "System.Runtime")

               let hasTargetFrameworkProfile() =
                   try
                       assembly.GetCustomAttributes(true)
                       |> Seq.tryFind (fun a ->
                              match a with
                              | :? TargetFrameworkAttribute as attr ->
                                   let fn = new FrameworkName(attr.FrameworkName)
                                   not (fn.Profile = "")
                              | _ -> false)
                       |> Option.isSome
                   with
                   | :? IOException -> true
                   | _e -> false

               referencesSystemRuntime() || hasTargetFrameworkProfile()
           with
           | _e -> false

       let needsFacades () =
           let referencedAssemblyProjects = project.GetReferencedAssemblyProjects configSelector

           match referencedAssemblyProjects |> Seq.tryFind Project.isPortable with
           | Some _ -> true
           | None -> project.References
                     |> Seq.filter (fun r -> r.ReferenceType = ReferenceType.Assembly)
                     |> Seq.collect Project.getAssemblyLocations
                     |> Seq.tryFind isAssemblyPortable
                     |> Option.isSome

       let wrapf = if shouldWrap then wrapFile else id

       let getReferencedAssemblies (project:DotNetProject) =
            let hasExplicitFSharpCore =
                project.References |> Seq.exists (fun r -> r.Include = "FSharp.Core")

            LoggingService.logDebug "Fetching referenced assemblies for %s " project.Name

            if hasExplicitFSharpCore then
                projectAssemblyReferences |> Seq.filter (fun r -> not (r.FilePath.ToString().EndsWith "FSharp.Core.dll"))
            else
                projectAssemblyReferences

       [
        let portableRefs =
            if needsFacades() then
                project.TargetRuntime.FindFacadeAssembliesForPCL project.TargetFramework
                |> Seq.filter (fun r -> not (r.EndsWith("mscorlib.dll"))
                                        && not (r.EndsWith("FSharp.Core.dll")))
            else
                Seq.empty

        let getAbsolutePath (ref:AssemblyReference) =
            let assemblyPath = ref.FilePath
            if assemblyPath.IsAbsolute then
                assemblyPath |> string
            else
                let s = Path.Combine(project.FileName.ParentDirectory.ToString(), assemblyPath.ToString())
                Path.GetFullPath s

        let projectReferences =
            project.References
            |> Seq.collect Project.getAssemblyLocations
            |> Seq.append portableRefs

            |> Seq.append (getReferencedAssemblies project |> Seq.map getAbsolutePath)
            |> Seq.distinct

        let find assemblyName=
            projectReferences
            |> Seq.tryFind (fun fn -> fn.EndsWith(assemblyName + ".dll", true, CultureInfo.InvariantCulture)
                                      || fn.EndsWith(assemblyName, true, CultureInfo.InvariantCulture))


        // If 'mscorlib.dll' or 'FSharp.Core.dll' is not in the set of references, we try to resolve and add them.
        match find "FSharp.Core", find "mscorlib", Project.isDotNetCoreProject project with
        | None, Some mscorlib, false ->
            // if mscorlib is found without FSharp.Core yield fsharp.core in the same base dir as mscorlib
            // falling back to one of the default directories
            let extraPath = Some (Path.GetDirectoryName (mscorlib))
            match ReferenceResolution.tryGetDefaultReference langVersion targetFramework "FSharp.Core" extraPath with
            | Some ref -> yield "-r:" + wrapf(ref)
            | None -> LoggingService.LogWarning(resolutionFailedMessage "FSharp.Core")
        | None, None, false ->
            // If neither are found yield the default fsharp.core
            match ReferenceResolution.tryGetDefaultReference langVersion targetFramework "FSharp.Core" None with
            | Some ref -> yield "-r:" + wrapf(ref)
            | None -> LoggingService.LogWarning(resolutionFailedMessage "FSharp.Core")
        | _ -> () // found them both, no action needed

        for file in projectReferences do
            yield "-r:" + wrapf(file) ]

  let generateDebug (config:FSharpCompilerParameters) =
      match config.ParentConfiguration.DebugSymbols, config.ParentConfiguration.DebugType with
      | true, typ ->
          match typ with
          | "full" -> "--debug:full"
          | "pdbonly" -> "--debug:pdbonly"
          | _ -> "--debug+"
      | false, _ -> "--debug-"

  let getSharedAssetFilesFromReferences (project:DotNetProject) =
      project.References
      |> Seq.filter (fun r -> r.ExtendedProperties.Contains("MSBuild.SharedAssetsProject"))
      |> Seq.collect (fun r -> (r.ResolveProject project.ParentSolution).Files)
      |> Seq.map (fun f -> f.FilePath)
      |> Set.ofSeq

  let getCompiledFiles project =
      let sharedAssetFiles = getSharedAssetFilesFromReferences project

      project.Files
      // Shared Asset files need to be referenced first
      |> Seq.sortByDescending (fun f -> sharedAssetFiles.Contains f.FilePath)
      |> Seq.filter(fun f -> f.FilePath.Extension = ".fs")
      |> Seq.map(fun f -> f.Name)
      |> Seq.distinct

  /// Generates command line options for the compiler specified by the
  /// F# compiler options (debugging, tail-calls etc.), custom command line
  /// parameters and assemblies referenced by the project ("-r" options)
  let generateCompilerOptions (project:DotNetProject, projectAssemblyReferences: AssemblyReference seq, fsconfig:FSharpCompilerParameters, reqLangVersion, targetFramework, configSelector, shouldWrap) =
    let dashr = generateReferences (project, projectAssemblyReferences, reqLangVersion, targetFramework, configSelector, shouldWrap) |> Array.ofSeq

    let splitByChars (chars: char array) (s:string) =
        s.Split(chars, StringSplitOptions.RemoveEmptyEntries)

    let defines = fsconfig.GetDefineSymbols()
    [
       yield "--simpleresolution"
       yield "--noframework"
       let outputFile = project.GetOutputFileName(configSelector).ToString()
       if not (String.IsNullOrWhiteSpace outputFile) then 
           yield "--out:" + outputFile
       if Project.isPortable project || Project.isDotNetCoreProject project then
           yield "--targetprofile:netcore"
       if not (String.IsNullOrWhiteSpace fsconfig.PlatformTarget) then
           yield "--platform:" + fsconfig.PlatformTarget
       yield "--fullpaths"
       yield "--flaterrors"
       for symbol in defines do yield "--define:" + symbol
       yield if fsconfig.HasDefineSymbol "DEBUG" then  "--debug+" else  "--debug-"
       yield if fsconfig.Optimize then "--optimize+" else "--optimize-"
       yield if fsconfig.GenerateTailCalls then "--tailcalls+" else "--tailcalls-"
       if not (String.IsNullOrWhiteSpace fsconfig.DebugType) then
           yield sprintf "--debug:%s" fsconfig.DebugType
       yield match project.CompileTarget with
             | CompileTarget.Library -> "--target:library"
             | CompileTarget.Module -> "--target:module"
             | _ -> "--target:exe"
       yield if fsconfig.TreatWarningsAsErrors then "--warnaserror+" else "--warnaserror-"
       yield sprintf "--warn:%d" fsconfig.WarningLevel
       if not (String.IsNullOrWhiteSpace fsconfig.NoWarn) then
           for arg in fsconfig.NoWarn |> splitByChars [|';'; ','|] do
               yield "--nowarn:" + arg
       // TODO: This currently ignores escaping using "..."
       for arg in fsconfig.OtherFlags |> splitByChars [|' '|] do
         yield arg
       yield! dashr
       yield! (getCompiledFiles project)]

  let generateProjectOptions (project:DotNetProject, projectAssemblyReferences: AssemblyReference seq, fsconfig:FSharpCompilerParameters, reqLangVersion, targetFramework, configSelector, shouldWrap) =
    let compilerOptions = generateCompilerOptions (project, projectAssemblyReferences, fsconfig, reqLangVersion, targetFramework, configSelector, shouldWrap) |> Array.ofSeq
    let loadedTimeStamp =  DateTime.MaxValue // Not 'now', we don't want to force reloading
    { ProjectFileName = project.FileName.FullPath.ToString()
      SourceFiles = [| |]
      Stamp = None
      OtherOptions = compilerOptions
      ReferencedProjects = [| |]
      IsIncompleteTypeCheckEnvironment = false
      UseScriptResolutionRules = false
      LoadTime = loadedTimeStamp
      UnresolvedReferences = None
      OriginalLoadReferences = []
      ExtraProjectInfo = None }

  /// Get source files of the current project (returns files that have
  /// build action set to 'Compile', but not e.g. scripts or resources)
  let getSourceFiles (items:ProjectItemCollection) =
      [ for file in items.GetAll<ProjectFile>() do
            if file.BuildAction = "Compile" && file.Subtype <> Subtype.Directory then
                yield file.FilePath.FullPath.ToString() ]


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

  let private getShellToolPath (extensions:seq<string>) (toolName:string)  =
    let pathVariable = Environment.GetEnvironmentVariable("PATH")
    let searchPaths = pathVariable.Split [| IO.Path.PathSeparator  |]
    getToolPath searchPaths extensions toolName

  let getDefaultInteractive() =

      let runtime = IdeApp.Preferences.DefaultTargetRuntime.Value
      let framework = Project.getDefaultTargetFramework runtime

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

      let runtime = IdeApp.Preferences.DefaultTargetRuntime.Value
      let framework = Project.getDefaultTargetFramework runtime

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

  let getDefineSymbols (fileName:string) (project: Project) =
      [if FileSystem.IsAScript fileName
       then yield! ["INTERACTIVE";"EDITING"]
       else yield! ["COMPILED";"EDITING"]

       let configuration =
           match IdeApp.Workspace |> Option.ofNull, project |> Option.ofNull with
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

  let getConfig() =
      match MonoDevelop.Ide.IdeApp.Workspace with
            | ws when ws <> null && ws.ActiveConfiguration <> null -> ws.ActiveConfiguration
            | _ -> MonoDevelop.Projects.ConfigurationSelector.Default

  let getArgumentsFromProject (proj:DotNetProject) (referencedAssemblies) =
        maybe {
            let config = getConfig()
            let! projConfig = proj.GetConfiguration(config) |> Option.tryCast<DotNetProjectConfiguration>
            let! fsconfig = projConfig.CompilationParameters |> Option.tryCast<FSharpCompilerParameters>
            return generateProjectOptions (proj, referencedAssemblies, fsconfig, None, getTargetFramework projConfig.TargetFramework.Id, config, false)
        }

  let getReferencesFromProject (proj:DotNetProject) referencedAssemblies =
        let config = getConfig()
        let projConfig = proj.GetConfiguration(config) :?> DotNetProjectConfiguration
        generateReferences(proj, referencedAssemblies, None, getTargetFramework projConfig.TargetFramework.Id, config, false)

