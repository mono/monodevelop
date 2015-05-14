namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Projects.Formats.MSBuild
open MonoDevelop.Ide
open System.Xml

type FSharpProject() as self = 
    inherit DotNetProject()
    // Keep the platforms combo of CodeGenerationPanelWidget in sync with this list
    let supportedPlatforms = [| "anycpu"; "x86"; "x64"; "itanium" |]
    let FSharp3Import          = "$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.0\\Framework\\v4.0\\Microsoft.FSharp.Targets"
    let FSharp31Import         = "$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.1\\Framework\\v4.0\\Microsoft.FSharp.Targets"
    let FSharp31PortableImport = "$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.1\\Framework\\v4.0\\Microsoft.Portable.FSharp.Targets"

    let langServ = MDLanguageService.Instance
    let mutable initialisedAsPortable = false
    
    let invalidateProjectFile() =
        try 
            if IO.File.Exists (self.FileName.ToString()) then
              let options = langServ.GetProjectCheckerOptions(self.FileName.ToString(), [("Configuration", IdeApp.Workspace.ActiveConfigurationId)])
              langServ.InvalidateConfiguration(options)
              langServ.ClearProjectInfoCache()
              ()
        with ex -> LoggingService.LogError ("Could not invalidate configuration", ex)
    
    let invalidateFiles (args:#ProjectFileEventInfo seq) =
        for projectFileEvent in args do
            if MDLanguageService.SupportedFileName (projectFileEvent.ProjectFile.FilePath.ToString()) then
                invalidateProjectFile()

    [<ProjectPathItemProperty ("TargetProfile", DefaultValue = "")>]
    member val TargetProfile = String.Empty with get, set

    [<ProjectPathItemProperty ("TargetFSharpCoreVersion", DefaultValue = "")>]
    member val TargetFSharpCoreVersion = String.Empty with get, set

    override x.OnInitialize() =
        base.OnInitialize()

    override x.OnInitializeFromTemplate(createInfo, options) =
      base.OnInitializeFromTemplate(createInfo, options)
      if options.HasAttribute "FSharpPortable" then initialisedAsPortable <- true
      if options.HasAttribute "TargetProfile" then x.TargetProfile <- options.GetAttribute "TargetProfile"
      if options.HasAttribute "TargetFSharpCoreVersion" then x.TargetFSharpCoreVersion <- options.GetAttribute "TargetFSharpCoreVersion"

    override x.OnGetDefaultImports (imports) =
        base.OnGetDefaultImports (imports)
        // By default projects use the F# 3.1 targets file unless only 3.0 is available on the machine.
        // New projects will be created with this targets file
        // If FSharp 3.1 is available, use it. If not, use 3.0
        if initialisedAsPortable then
          if MSBuildProjectService.IsTargetsAvailable(FSharp31PortableImport) then imports.Add (FSharp31PortableImport)
          else failwith "F# portable target not found"
        
        else
          if MSBuildProjectService.IsTargetsAvailable(FSharp31Import) then imports.Add (FSharp31Import)
          else imports.Add (FSharp3Import)
    
    override x.OnWriteProject(monitor, msproject) =
        base.OnWriteProject(monitor, msproject)
        // This updates the old guid to the new one on saving the project
        try 
            let fsimportExists = 
                msproject.Imports 
                |> Seq.exists (fun import -> import.Project.EndsWith("FSharp.Targets", StringComparison.OrdinalIgnoreCase))
            if fsimportExists then 
                msproject.GetGlobalPropertyGroup().GetProperties()
                |> Seq.tryFind (fun p -> p.Name = "ProjectTypeGuids")
                |> Option.iter (fun guids -> 
                       guids.Element.InnerText <- guids.Element.InnerText.Split ([| ';' |], StringSplitOptions.RemoveEmptyEntries)
                                                  |> Array.filter (fun guid -> not (guid.Equals ("{4925A630-B079-445D-BCD4-3A9C94FE9307}", StringComparison.OrdinalIgnoreCase)))
                                                  |> String.concat ";")
        with exn -> LoggingService.LogWarning("Failed to remove old F# guid", exn)
    
    override x.OnCompileSources(items, config, configSel, monitor) : BuildResult = 
        CompilerService.Compile(items, config, configSel, monitor)
    
    override x.OnCreateCompilationParameters(config : DotNetProjectConfiguration, kind : ConfigurationKind) : DotNetCompilerParameters = 
        let pars = new FSharpCompilerParameters()
        // Set up the default options
        if supportedPlatforms |> Array.exists (fun x -> x.Contains(config.Platform)) then pars.PlatformTarget <- config.Platform
        match kind with
        | ConfigurationKind.Debug -> 
            pars.AddDefineSymbol "DEBUG"
            pars.Optimize <- false
            pars.GenerateTailCalls <- false
        | ConfigurationKind.Release -> 
            pars.Optimize <- true
            pars.GenerateTailCalls <- true
        | _ -> ()
        //pars.DocumentationFile <- config.CompiledOutputName.FileNameWithoutExtension + ".xml"
        pars :> DotNetCompilerParameters
    
    override x.OnGetSupportedClrVersions() = 
        [| ClrVersion.Net_2_0; ClrVersion.Net_4_0; ClrVersion.Net_4_5; ClrVersion.Clr_2_1 |]

    override x.OnFileAddedToProject(e) =
        base.OnFileAddedToProject(e)
        if not self.Loading then invalidateFiles(e)

    override x.OnFileRemovedFromProject(e) =
        base.OnFileRemovedFromProject(e)
        if not self.Loading then invalidateFiles(e)

    override x.OnFileRenamedInProject(e) =
        base.OnFileRenamedInProject(e)
        if not self.Loading then invalidateFiles(e)

    override x.OnFilePropertyChangedInProject(e) =
        base.OnFilePropertyChangedInProject(e)
        if not self.Loading then invalidateFiles(e)

    override x.OnReferenceAddedToProject(e) =
        base.OnReferenceAddedToProject(e)
        if not self.Loading then invalidateProjectFile()

    override x.OnReferenceRemovedFromProject(e) =
        base.OnReferenceRemovedFromProject(e)
        if not self.Loading then invalidateProjectFile()

    override x.OnDispose () =
        if not self.Loading then invalidateProjectFile()

        // FIXME: is it correct to do it every time a project is disposed?
        //Should only be done on solution close
        //langServ.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
        base.OnDispose ()
