namespace MonoDevelop.FSharp

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Projects.MSBuild
open MonoDevelop.Ide
open System.Xml
open MonoDevelop.Core.Assemblies
open ExtCore.Control

module Project =
    let FSharp3Import        = "$(MSBuildExtensionsPath32)\\..\\Microsoft SDKs\\F#\\3.0\\Framework\\v4.0\\Microsoft.FSharp.Targets"
    let FSharpImport         = @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.FSharp.Targets"

    let addConditionalTargets (msproject: MSBuildProject) =
        let p = new MSBuildPropertyGroup()
        p.SetValue("FSharpTargetsPath", FSharpImport, null, false, null)
        msproject.AddPropertyGroup(p, true, null)

        let p = new MSBuildPropertyGroup()
        p.Condition <- "'$(VisualStudioVersion)' == '10.0' OR '$(VisualStudioVersion)' == '11.0'"
        p.SetValue("FSharpTargetsPath", FSharp3Import, null, false, null)
        msproject.AddPropertyGroup(p, true, null)

type FSharpProject() as self =
    inherit DotNetProject()
    // Keep the platforms combo of CodeGenerationPanelWidget in sync with this list
    let supportedPlatforms = [| "anycpu"; "x86"; "x64"; "itanium" |]

    let oldFSharpProjectGuid   = "{4925A630-B079-445D-BCD4-3A9C94FE9307}"
    let supportedPortableProfiles = ["Profile7";"Profile47";"Profile78";"Profile259"]

    ///keyed on TargetProfile, Value: TargetFSharpCoreVersion, netcore
    let profileMap =
      Map.ofList ["Profile7",   ("3.3.1.0",   true)
                  "Profile47",  ("2.3.5.1",   false)
                  "Profile78",  ("3.78.3.1",  true)
                  "Profile259", ("3.259.3.1", true) ]

    let mutable initialisedAsPortable = false

    let invalidateProjectFile() =
        try
            if File.Exists (self.FileName.ToString()) then
                languageService.GetProjectCheckerOptions(self.FileName.ToString(), [("Configuration", IdeApp.Workspace.ActiveConfigurationId)])
                |> Option.iter(fun options ->
                    languageService.InvalidateConfiguration(options)
                    languageService.ClearProjectInfoCache())
        with ex -> LoggingService.LogError ("Could not invalidate configuration", ex)

    let invalidateFiles (args:#ProjectFileEventInfo seq) =
        for projectFileEvent in args do
            if FileService.supportedFileName (projectFileEvent.ProjectFile.FilePath.ToString()) then
                invalidateProjectFile()

    let isPortable (project:MSBuildProject) =
        project.EvaluatedProperties.Properties
        |> Seq.tryFind (fun i -> i.UnevaluatedValue.Equals(".NETPortable"))
        |> Option.isSome

    [<ProjectPathItemProperty ("TargetProfile", DefaultValue = "mscorlib")>]
    member val TargetProfile = "mscorlib" with get, set

    [<ProjectPathItemProperty ("TargetFSharpCoreVersion", DefaultValue = "")>]
    member val TargetFSharpCoreVersion = String.Empty with get, set

    override x.IsPortableLibrary = initialisedAsPortable

    override x.OnInitialize() =
        base.OnInitialize()

    override x.OnReadProject(progress, project) =
        initialisedAsPortable <- isPortable project
        base.OnReadProject(progress, project)

    override x.OnReadProjectHeader(progress, project) =
        initialisedAsPortable <- isPortable project
        base.OnReadProjectHeader(progress, project)

    override x.OnSupportsFramework (framework) =
        if isPortable self.MSBuildProject then
            framework.Id.Identifier = TargetFrameworkMoniker.ID_PORTABLE && supportedPortableProfiles |> List.exists ((=) framework.Id.Profile)
        else base.OnSupportsFramework (framework)

    override x.OnInitializeFromTemplate(createInfo, options) =
        base.OnInitializeFromTemplate(createInfo, options)
        if options.HasAttribute "FSharpPortable" then initialisedAsPortable <- true
        if options.HasAttribute "TargetProfile" then x.TargetProfile <- options.GetAttribute "TargetProfile"
        if options.HasAttribute "TargetFSharpCoreVersion" then x.TargetFSharpCoreVersion <- options.GetAttribute "TargetFSharpCoreVersion"

    override x.OnGetDefaultImports (imports) =
        base.OnGetDefaultImports (imports)

        if initialisedAsPortable then
            let fsharpPortableImport = 
                @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.Portable.FSharp.Targets"
            imports.Add(fsharpPortableImport)
        else
            Project.addConditionalTargets base.MSBuildProject
            imports.Add("$(FSharpTargetsPath)")

    override x.OnWriteProject(monitor, msproject) =
        base.OnWriteProject(monitor, msproject)
        //Fix pcl netcore and TargetFSharpCoreVersion
        let globalGroup = msproject.GetGlobalPropertyGroup()

        maybe {
            let! targetFrameworkProfile = x.TargetFramework.Id.Profile |> Option.ofString
            let! fsharpcoreversion, netcore = profileMap |> Map.tryFind targetFrameworkProfile
            do globalGroup.SetValue ("TargetFSharpCoreVersion", fsharpcoreversion, "", true)
            let targetProfile = if netcore then "netcore" else "mscorlib"
            do globalGroup.SetValue ("TargetProfile", targetProfile, "mscorlib", true) } |> ignore

        // This removes the old guid on saving the project
        let removeGuid (innerText:string) guidToRemove =
            innerText.Split ( [|';'|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun guid -> not (guid.Equals (guidToRemove, StringComparison.OrdinalIgnoreCase)))
            |> String.concat ";"

        try
            let fsimportExists =
                msproject.Imports
                |> Seq.exists (fun import -> import.Project.EndsWith("FSharp.Targets", StringComparison.OrdinalIgnoreCase))
            if fsimportExists then
                globalGroup.GetProperties()
                |> Seq.tryFind (fun p -> p.Name = "ProjectTypeGuids")
                |> Option.iter (fun currentGuids -> let newProjectTypeGuids = removeGuid currentGuids.Value oldFSharpProjectGuid
                                                    currentGuids.SetValue(newProjectTypeGuids))
        with exn -> LoggingService.LogWarning("Failed to remove old F# guid", exn)

    override x.OnCompileSources(items, config, configSel, monitor) =
        CompilerService.Compile(items, config, configSel, monitor)

    override x.OnCreateCompilationParameters(config, kind) =
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

    //override x.OnFileRenamedInProject(e)=
    //    base.OnFileRenamedInProject(e)
    //    if not self.Loading then invalidateProjectFile()

    override x.OnNameChanged(e)=
        base.OnNameChanged(e)
        if not self.Loading then invalidateProjectFile()

    override x.OnGetDefaultResourceId(projectFile) =
        projectFile.FilePath.FileName

    override x.OnDispose () =
        //if not self.Loading then invalidateProjectFile()

        // FIXME: is it correct to do it every time a project is disposed?
        //Should only be done on solution close
        //langServ.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
        base.OnDispose ()
