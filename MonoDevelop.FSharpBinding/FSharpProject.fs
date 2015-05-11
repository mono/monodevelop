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
    
    let langServ = MDLanguageService.Instance
    
    let invalidateProjectFile() =
        try
            let options = langServ.GetProjectCheckerOptions(self.FileName.ToString(), [("Configuration", IdeApp.Workspace.ActiveConfigurationId)])
            langServ.InvalidateConfiguration(options)
            langServ.ClearProjectInfoCache()
        with ex -> LoggingService.LogError ("Could not invalidate configuration", ex)
    
    let invalidateFiles (args:#ProjectFileEventInfo seq) =
        for projectFileEvent in args do
            if MDLanguageService.SupportedFileName (projectFileEvent.ProjectFile.FilePath.ToString()) then
                invalidateProjectFile()

    override x.OnInitialize() = 
        base.OnInitialize()

    override x.OnGetDefaultImports (imports) = 
        base.OnGetDefaultImports (imports);
        // By default projects use the F# 3.1 targets file unless only 3.0 is available on the machine.
        // New projects will be created with this targets file
        // If FSharp 3.1 is available, use it. If not, use 3.0
        if MSBuildProjectService.IsTargetsAvailable("$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.1\\Framework\\v4.0\\Microsoft.FSharp.Targets") then
            imports.Add ("$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.1\\Framework\\v4.0\\Microsoft.FSharp.Targets");
        else
            imports.Add ("$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.0\\Framework\\v4.0\\Microsoft.FSharp.Targets");
    
    override x.OnWriteProject(monitor, msproject) = 
        base.OnWriteProject(monitor, msproject)
        // This updates the old guid to the new one on saving the project
        try 
            let fsimportExists = 
                msproject.Imports 
                |> Seq.exists 
                       (fun import -> import.Project.EndsWith("FSharp.Targets", StringComparison.OrdinalIgnoreCase))
            if fsimportExists then 
                msproject.GetGlobalPropertyGroup().GetProperties()
                |> Seq.tryFind (fun p -> p.Name = "ProjectTypeGuids")
                |> Option.iter (fun guids -> 
                       guids.Element.InnerText <- guids.Element.InnerText.Split
                                                      ([| ';' |], StringSplitOptions.RemoveEmptyEntries)
                                                  |> Array.filter 
                                                         (fun guid -> 
                                                         not 
                                                             (guid.Equals
                                                                  ("{4925A630-B079-445D-BCD4-3A9C94FE9307}", 
                                                                   StringComparison.OrdinalIgnoreCase)))
                                                  |> String.concat ";")
        with exn -> LoggingService.LogWarning("Failed to remove old F# guid", exn)
    
    override x.OnCompileSources(items, config, configSel, monitor) : BuildResult = 
        CompilerService.Compile(items, config, configSel, monitor)
    
    override x.OnCreateCompilationParameters(options : XmlElement) : DotNetCompilerParameters = 
        let pars = new FSharpCompilerParameters()
        // Set up the default options
        if options <> null then 
            let platform = options.GetAttribute("Platform")
            if (supportedPlatforms |> Array.exists (fun x -> x.Contains(platform))) then pars.PlatformTarget <- platform
            let debugAtt = options.GetAttribute("DefineDebug")
            if (System.String.Compare("True", debugAtt, StringComparison.OrdinalIgnoreCase) = 0) then 
                pars.AddDefineSymbol "DEBUG"
                pars.ParentConfiguration.DebugSymbols <- true
                pars.Optimize <- false
                pars.GenerateTailCalls <- false
            let releaseAtt = options.GetAttribute("Release")
            if (System.String.Compare("True", releaseAtt, StringComparison.OrdinalIgnoreCase) = 0) then 
                pars.ParentConfiguration.DebugSymbols <- false
                pars.Optimize <- true
                pars.GenerateTailCalls <- true
        // TODO: set up the documentation file to be AssemblyName.xml by default (but how do we get AssemblyName here?)
        // pars.DocumentationFile <- ""
        //    System.IO.Path.GetFileNameWithoutExtension(config.CompiledOutputName.ToString())+".xml" 
        pars :> DotNetCompilerParameters
    
    override x.OnGetSupportedClrVersions() = 
        [| ClrVersion.Net_2_0; ClrVersion.Net_4_0; ClrVersion.Net_4_5; ClrVersion.Clr_2_1 |]

    override x.OnFileAddedToProject(e) =
        base.OnFileAddedToProject(e)
        invalidateFiles(e)

    override x.OnFileRemovedFromProject(e) =
        base.OnFileRemovedFromProject(e)
        invalidateFiles(e)

    override x.OnFileRenamedInProject(e) =
        base.OnFileRenamedInProject(e)
        invalidateFiles(e)

    override x.OnFilePropertyChangedInProject(e) =
        base.OnFilePropertyChangedInProject(e)
        invalidateFiles(e)

    override x.OnReferenceAddedToProject(e) =
        base.OnReferenceAddedToProject(e)
        invalidateProjectFile()

    override x.OnReferenceRemovedFromProject(e) =
        base.OnReferenceRemovedFromProject(e)
        invalidateProjectFile()

    override x.OnDispose () =
        invalidateProjectFile()

        // FIXME: is it correct to do it every time a project is disposed?
        langServ.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
        base.OnDispose ()
