namespace MonoDevelop.FSharp

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Projects.MSBuild
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
    let supportedPlatforms = [| "anycpu"; "x86"; "x64"; "Itanium" |]

    let oldFSharpProjectGuid   = "{4925A630-B079-445D-BCD4-3A9C94FE9307}"
    let supportedPortableProfiles = ["Profile7";"Profile47";"Profile78";"Profile259"]

    ///keyed on TargetProfile, Value: TargetFSharpCoreVersion, netcore
    let profileMap =
      Map.ofList ["Profile7",   ("3.3.1.0",   true)
                  "Profile47",  ("2.3.5.1",   false)
                  "Profile78",  ("3.78.3.1",  true)
                  "Profile259", ("3.259.3.1", true) ]

    let mutable initialisedAsPortable = false
    let mutable referencedAssemblies = None

    let isPortable (project:MSBuildProject) =
        project.EvaluatedProperties.Properties
        |> Seq.tryFind (fun i -> i.UnevaluatedValue.Equals(".NETPortable"))
        |> Option.isSome

    let fixProjectFormatForVisualStudio (project:MSBuildProject) =
        // Merge ItemGroups into one group ordered by folder name
        // so that VS for Windows can load it.
        let projectPath = project.FileName.ParentDirectory |> string

        let absolutePath path = MSBuildProjectService.FromMSBuildPath(projectPath, path)

        let directoryNameFromBuildItem (item:MSBuildItem) =
            let itemInclude = item.Include.Replace('\\', Path.DirectorySeparatorChar)
            Path.GetDirectoryName itemInclude

        let msbuildItemExistsAsFile (item:MSBuildItem) =
           let itemPath = absolutePath item.Include
           item.Name <> "ProjectReference" && File.Exists itemPath

        let groups = project.ItemGroups |> List.ofSeq
        let itemGroups =
            groups
            |> List.map  (fun group ->
                group, group.Items
                       |> Seq.filter msbuildItemExistsAsFile
                       |> List.ofSeq)
            |> List.filter (fun (_, items) -> items.Length > 0)

        let isParentDirectory folderName fileName =
            if String.isEmpty folderName then
                true
            else
                let absoluteFolder = DirectoryInfo (absolutePath folderName)
                let absoluteFile = FileInfo (absolutePath fileName)
                let rec isParentDirRec (dir:DirectoryInfo) =
                    match dir with
                    | null -> false
                    | dir when dir.FullName = absoluteFolder.FullName -> true
                    | _ -> isParentDirRec dir.Parent
                isParentDirRec absoluteFile.Directory

        let unsorted = itemGroups |> List.collect snd
        let rec splitFilesByParent (items:MSBuildItem list) parentFolder list1 list2 =
            match items with
            | h :: t ->
                if isParentDirectory parentFolder h.Include then
                    splitFilesByParent t parentFolder (h::list1) list2
                else
                    splitFilesByParent t parentFolder list1 (h::list2)
            | [] -> (list1 |> List.rev) @ (list2 |> List.rev)

        let rec orderFiles items acc lastFolder =
            match items with
            | h :: t -> 
                let newFolder = directoryNameFromBuildItem h
                if newFolder = lastFolder then
                    orderFiles t (h::acc) newFolder
                else
                    let childrenFirst = (splitFilesByParent t newFolder [] [])
                    orderFiles childrenFirst (h::acc) newFolder
            | [] -> acc |> List.rev

        let sortedItems = orderFiles unsorted [] ""
        let needsSort = 
            match itemGroups with
            | [_single, items] when items = sortedItems -> false
            | _ -> true

        if needsSort && sortedItems.Length > 0 then
            let newGroup = project.AddNewItemGroup()

            for item in sortedItems do
                project.RemoveItem(item, true)
                newGroup.AddItem item

    [<ProjectPathItemProperty ("TargetProfile", DefaultValue = "mscorlib")>]
    member val TargetProfile = "mscorlib" with get, set

    [<ProjectPathItemProperty ("TargetFSharpCoreVersion", DefaultValue = "")>]
    member val TargetFSharpCoreVersion = String.Empty with get, set

    [<ProjectPathItemProperty ("UseStandardResourceNames", DefaultValue="true")>]
    member val UseStandardResourceNames = "true" with get, set 

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
        fixProjectFormatForVisualStudio msproject
        let globalGroup = msproject.GetGlobalPropertyGroup()
        // Generate F# resource names the same way that C# does
        // See https://github.com/Microsoft/visualfsharp/pull/3352
        globalGroup.SetValue ("UseStandardResourceNames", x.UseStandardResourceNames, "false", true)

        maybe {
            //Fix pcl netcore and TargetFSharpCoreVersion
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
        CompilerService.Compile(items, config, x.ReferencedAssemblies, configSel, monitor)

    override x.OnCreateCompilationParameters(config, kind) =
        let pars = new FSharpCompilerParameters()
        config.CompilationParameters <- pars

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
        if not self.Loading then MDLanguageService.invalidateFiles e

    override x.OnFileRemovedFromProject(e) =
        base.OnFileRemovedFromProject(e)
        if not self.Loading then MDLanguageService.invalidateFiles e

    override x.OnFileRenamedInProject(e) =
        base.OnFileRenamedInProject(e)
        if not self.Loading then MDLanguageService.invalidateFiles e

    override x.OnFilePropertyChangedInProject(e) =
        base.OnFilePropertyChangedInProject(e)
        if not self.Loading then MDLanguageService.invalidateFiles e

    override x.OnReferenceAddedToProject(e) =
        base.OnReferenceAddedToProject(e)
        if not self.Loading then MDLanguageService.invalidateProjectFile self.FileName

    override x.OnReferenceRemovedFromProject(e) =
        base.OnReferenceRemovedFromProject(e)
        if not self.Loading then MDLanguageService.invalidateProjectFile self.FileName

    //override x.OnFileRenamedInProject(e)=
    //    base.OnFileRenamedInProject(e)
    //    if not self.Loading then invalidateProjectFile()

    override x.OnNameChanged(e)=
        base.OnNameChanged(e)
        if not self.Loading then MDLanguageService.invalidateProjectFile self.FileName

    override x.OnGetDefaultResourceId(projectFile) =
        projectFile.FilePath.FileName

    override x.OnModified(e) =
        base.OnModified(e)
        if not self.Loading && not self.IsReevaluating then MDLanguageService.invalidateProjectFile self.FileName

    member x.ReferencedAssemblies
        with get() =
            match referencedAssemblies with
            | Some assemblies -> assemblies
            | None ->
                let assemblies = (x.GetReferencedAssemblies (CompilerArguments.getConfig())).Result
                referencedAssemblies <- Some assemblies
                assemblies

    member x.GetOrderedReferences() =
        let references =
            let args =
                CompilerArguments.getReferencesFromProject x x.ReferencedAssemblies
                |> Seq.choose (fun ref -> if (ref.Contains "mscorlib.dll" || ref.Contains "FSharp.Core.dll")
                                          then None
                                          else
                                              let ref = ref |> String.replace "-r:" ""
                                              if File.Exists ref then Some ref
                                              else None )
                |> Seq.distinct
                |> Seq.toArray
            args

        let orderAssemblyReferences = MonoDevelop.FSharp.OrderAssemblyReferences()
        orderAssemblyReferences.Order references

    member x.GetReferences() =
        async {
            let! refs = x.GetReferencedAssemblies (CompilerArguments.getConfig()) |> Async.AwaitTask
            referencedAssemblies <- Some refs
        }

    member x.ReevaluateProject(e) =
        let task = base.OnReevaluateProject (e)

        async {
            do! task
            MDLanguageService.invalidateProjectFile self.FileName
        }

    override x.OnReevaluateProject(monitor) =
        x.ReevaluateProject monitor |> Async.startAsPlainTask

    override x.OnDispose () =
        //if not self.Loading then invalidateProjectFile()

        // FIXME: is it correct to do it every time a project is disposed?
        //Should only be done on solution close
        //langServ.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
        base.OnDispose ()

