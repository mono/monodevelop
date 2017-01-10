namespace MonoDevelop.FSharp
open System
open System.IO
open System.Diagnostics
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore
open ExtCore.Control
open ExtCore.Control.Collections
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Projects

module Symbol =
    /// We always know the text of the identifier that resolved to symbol.
    /// Trim the range of the referring text to only include this identifier.
    /// This means references like A.B.C are trimmed to "C".  This allows renaming to just rename "C".
    let trimSymbolRegion(symbolUse:FSharpSymbolUse) (lastIdentAtLoc:string) =
        let m = symbolUse.RangeAlternate
        let ((beginLine, beginCol), (endLine, endCol)) = ((m.StartLine, m.StartColumn), (m.EndLine, m.EndColumn))
    
        let (beginLine, beginCol) =
            if endCol >=lastIdentAtLoc.Length && (beginLine <> endLine || (endCol-beginCol) >= lastIdentAtLoc.Length) then
                (endLine,endCol-lastIdentAtLoc.Length)
            else
                (beginLine, beginCol)
        Range.mkPos beginLine beginCol, Range.mkPos endLine endCol
        //(beginLine, beginCol), (endLine, endCol)

/// Contains settings of the F# language service
module ServiceSettings =
    let internal getEnvInteger e dflt = match System.Environment.GetEnvironmentVariable(e) with null -> dflt | t -> try int t with _ -> dflt
    /// When making blocking calls from the GUI, we specify this value as the timeout, so that the GUI is not blocked forever
    let blockingTimeout = getEnvInteger "FSharpBinding_BlockingTimeout" 1000
    let maximumTimeout = getEnvInteger "FSharpBinding_MaxTimeout" 10000
    let idleBackgroundCheckTime = getEnvInteger "FSharpBinding_IdleBackgroundCheckTime" 2000
 
// --------------------------------------------------------------------------------------
/// Wraps the result of type-checking and provides methods for implementing
/// various IntelliSense functions (such as completion & tool tips).
/// Provides default empty/negative results if information is missing.
type ParseAndCheckResults (infoOpt : FSharpCheckFileResults option, parseResults : FSharpParseFileResults option) =

    /// Get declarations at the current location in the specified document and the long ident residue
    /// e.g. The incomplete ident One.Two.Th will return Th
    member x.GetDeclarations(line, col, lineStr) =
        match infoOpt, parseResults with
        | Some (checkResults), parseResults ->
            let longName,residue = Parsing.findLongIdentsAndResidue(col, lineStr)
            LoggingService.logDebug "GetDeclarations: '%A', '%s'" longName residue
            // Get items & generate output
            try
                let results =
                    Async.RunSynchronously (checkResults.GetDeclarationListInfo( parseResults, line, col, lineStr, longName, residue, fun (_,_) -> false), timeout = ServiceSettings.blockingTimeout )
                Some (results, residue)
            with :? TimeoutException -> None
        | None, _ -> None

    /// Get the symbols for declarations at the current location in the specified document and the long ident residue
    /// e.g. The incomplete ident One.Two.Th will return Th
    member x.GetDeclarationSymbols(line, col, lineStr) =
        async {
            match infoOpt, parseResults with
            | Some checkResults, parseResults ->
                  let longName,residue = Parsing.findLongIdentsAndResidue(col, lineStr)
                  LoggingService.logDebug "GetDeclarationSymbols: '%A', '%s'" longName residue
                  // Get items & generate output
                  try
                      let! results = checkResults.GetDeclarationListSymbols(parseResults, line, col, lineStr, longName, residue, fun (_,_) -> false)
                      return Some (results, residue)
                  with :? TimeoutException -> return None
            | None, _ -> return None
        }

    /// Get the tool-tip to be displayed at the specified offset (relatively
    /// from the beginning of the current document)
    member x.GetToolTip(line, col, lineStr) =
        async {
            match infoOpt with
            | Some checkResults ->
                match Parsing.findIdents col lineStr SymbolLookupKind.ByLongIdent with
                | None -> return None
                | Some(col,identIsland) ->
                    let! res = checkResults.GetToolTipTextAlternate(line, col, lineStr, identIsland, FSharpTokenTag.Identifier)
                    let! sym = checkResults.GetSymbolUseAtLocation(line, col, lineStr, identIsland)
                    LoggingService.logDebug "Result: Got something, returning"
                    return sym |> Option.bind (fun sym -> let start, finish = Symbol.trimSymbolRegion sym (Seq.last identIsland)
                                                          Some (res, (start.Column, finish.Column)))
            | None -> return None }

    member x.GetDeclarationLocation(line, col, lineStr) =
        async {
            match infoOpt with
            | Some checkResults ->
                match Parsing.findIdents col lineStr SymbolLookupKind.ByLongIdent with
                | None -> return FSharpFindDeclResult.DeclNotFound FSharpFindDeclFailureReason.Unknown
                | Some(col,identIsland) -> return! checkResults.GetDeclarationLocationAlternate(line, col, lineStr, identIsland, false)
            | None -> return FSharpFindDeclResult.DeclNotFound FSharpFindDeclFailureReason.Unknown }

    member x.GetSymbolAtLocation(line, col, lineStr) =
        async {
            match infoOpt with
            | Some (checkResults) ->
                match Parsing.findIdents col lineStr SymbolLookupKind.ByLongIdent 
                      |> Option.orTry (fun () -> Parsing.findIdents col lineStr SymbolLookupKind.Fuzzy) with
                | None -> return None
                | Some(colu, identIsland) ->
                    try
                        let! symbolUse = checkResults.GetSymbolUseAtLocation(line, colu, lineStr, identIsland)
                        return symbolUse
                    with ex ->
                        LoggingService.LogDebug("Error at: GetSymbolUseAtLocation}", ex)
                        return None
            | None -> return None }

    member x.GetMethodsAsSymbols(line, col, (lineStr:string)) =
        async {
            match infoOpt with
            | Some checkResults ->
                let lineToCaret = lineStr.[0..col-1]
                let column = lineToCaret |> Seq.tryFindIndexBack (fun c -> c <> '(' && c <> ' ')
                match column with
                | Some col ->
                    match Parsing.findIdents (col-1) lineToCaret SymbolLookupKind.ByLongIdent with
                    | None -> return None
                    | Some(colu, identIsland) ->
                        return! checkResults.GetMethodsAsSymbols(line, colu, lineToCaret, identIsland)
                | _ -> return None
            | None -> return None }

    member x.GetUsesOfSymbolInFile(symbol) =
        async {
            match infoOpt with
            | Some checkResults -> return! checkResults.GetUsesOfSymbolInFile(symbol)
            | None -> return [| |] }

    member x.GetAllUsesOfAllSymbolsInFile() =
        async {
            match infoOpt with
            | Some checkResults ->
                let! allSymbols = checkResults.GetAllUsesOfAllSymbolsInFile()
                return Some allSymbols
            | None -> return None }

    member x.PartialAssemblySignature =
        async {
            match infoOpt with
            | Some (checkResults) ->
                return Some checkResults.PartialAssemblySignature
            | None -> return None }

    member x.GetErrors() =
        match infoOpt, parseResults with
        | Some checkResults, Some parseResults ->
            checkResults.Errors
            |> Array.append parseResults.Errors
            |> Seq.distinct
        | Some checkResults, None -> checkResults.Errors |> Array.toSeq
        | None, Some parseResults -> parseResults.Errors |> Array.toSeq
        | None, None -> Seq.empty

    member x.GetNavigationItems() =
        match parseResults with
        | None -> [| |]
        | Some parseResults ->
          // GetNavigationItems is not 100% solid and throws occasional exceptions
            try parseResults.GetNavigationItems().Declarations
            with _ ->
                Debug.Assert(false, "couldn't update navigation items, ignoring")
                [| |]

    member x.ParseTree =
        match parseResults with
        | Some parseResults -> parseResults.ParseTree
        | None -> None

    member x.CheckResults = infoOpt

    member x.GetExtraColorizations() =
        match infoOpt with
        | Some checkResults -> Some(checkResults.GetExtraColorizationsAlternate())
        | None -> None

    member x.GetStringFormatterColours() =
        match infoOpt with
        | Some checkResults -> Some(checkResults.GetFormatSpecifierLocationsAndArity())
        | None -> None

[<RequireQualifiedAccess>]
type AllowStaleResults =
    // Allow checker results where the source doesn't even match
    | MatchingFileName
    // Allow checker results where the source matches but where the background builder may not have caught up yet after some other change
    | MatchingSource

//type Debug = System.Console

/// Provides functionality for working with the F# interactive checker running in background
type LanguageService(dirtyNotify) as x =

    /// Load times used to reset type checking properly on script/project load/unload. It just has to be unique for each project load/reload.
    /// Not yet sure if this works for scripts.
    let fakeDateTimeRepresentingTimeLoaded proj = DateTime(abs (int64 (match proj with null -> 0 | _ -> proj.GetHashCode())) % 103231L)
    let checkProjectResultsCache = Collections.Generic.Dictionary<string, _>()

    let projectChecked filename =
        let computation =
            async {
                let displayname = Path.GetFileName filename
                LoggingService.logDebug  "LanguageService: Project checked: %s" displayname
                if checkProjectResultsCache.ContainsKey filename then
                    LoggingService.logDebug "LanguageService: Removing project check results for: %s" displayname
                    checkProjectResultsCache.Remove(filename) |> ignore

                LoggingService.logDebug "LanguageService: Getting project checker options for: %s" displayname
                let projOptions =
                    if File.Exists filename then
                        x.GetProjectCheckerOptions(filename)
                    else
                        let trimmedfilename =
                            let finaldot = filename.LastIndexOf "."
                            if finaldot > 0 then filename.[..finaldot-1] else ""
                        if not (String.IsNullOrWhiteSpace trimmedfilename) && File.Exists trimmedfilename then
                            let source = File.ReadAllText trimmedfilename
                            x.GetScriptCheckerOptions(filename, filename, source)
                        else
                            LoggingService.logDebug "LanguageService: Could not generate project options for: %s file does not exist" filename
                            None
                match projOptions with
                | Some projOptions ->
                    LoggingService.logDebug "LanguageService: Getting CheckProjectResults for: %s" displayname
                    try
                        let! (projCheck:FSharpCheckProjectResults) = x.ParseAndCheckProject(projOptions)
                        if not projCheck.HasCriticalErrors then
                            LoggingService.logDebug "LanguageService: Adding CheckProjectResults to cache for: %s" displayname
                            checkProjectResultsCache.Add(filename, projCheck) |> ignore
                        else
                            LoggingService.logDebug "LanguageService: NOT adding CheckProjectResults to cache for: %s due to critical errors" displayname
                    with exn ->
                        LoggingService.LogDebug(sprintf "LanguageService: Error", exn)
                    | None -> () }
        Async.Start computation

    // Create an instance of interactive checker. The callback is called by the F# compiler service
    // when its view of the prior-typechecking-state of the start of a file has changed, for example
    // when the background typechecker has "caught up" after some other file has been changed,
    // and its time to re-typecheck the current file.
    let checker =
        let checker = FSharpChecker.Create()
        checker.PauseBeforeBackgroundWork <- ServiceSettings.idleBackgroundCheckTime
        checker.BeforeBackgroundFileCheck.Add dirtyNotify
#if DEBUG
        checker.FileParsed.Add (fun filename -> LoggingService.logDebug "LanguageService: File parsed: %s" filename)
        checker.FileChecked.Add (fun filename -> LoggingService.logDebug "LanguageService: File type checked: %s" filename)
#endif
        checker.ProjectChecked.Add projectChecked
        checker

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

    let projectInfoCache =
        //cache 50 project infos, then start evicting the least recently used entries
        ref (ExtCore.Caching.LruCache.create 50u)



    member x.RemoveFromProjectInfoCache(projFilename:string, ?properties) =
        let properties = defaultArg properties ["Configuration", "Debug"]
        let key = (projFilename, properties)
        LoggingService.logDebug "LanguageService: Removing %s from projectInfoCache" projFilename
        match (!projectInfoCache).TryExtract(key) with
        | Some _extractee, cache -> projectInfoCache := cache
        | None, _unchangedCache -> ()

    member x.ClearProjectInfoCache() =
        LoggingService.logDebug "LanguageService: Clearing ProjectInfoCache"
        projectInfoCache := ExtCore.Caching.LruCache.create 50u

    /// Constructs options for the interactive checker for the given file in the project under the given configuration.
    member x.GetCheckerOptions(fileName, projFilename, source) =
        let opts =
            if FileSystem.IsAScript fileName || fileName = projFilename
            // We are in a stand-alone file or we are in a project, but currently editing a script file
            then x.GetScriptCheckerOptions(fileName, projFilename, source)
            // We are in a project - construct options using current properties
            else x.GetProjectCheckerOptions(projFilename)
        opts

    /// Constructs options for the interactive checker for the given script file in the project under the given configuration.
    member x.GetScriptCheckerOptions(fileName, projFilename, source) =
        let opts =
            // We are in a stand-alone file or we are in a project, but currently editing a script file
            try
                let fileName = fixFileName(fileName)
                LoggingService.LogDebug ("LanguageService: GetScriptCheckerOptions: Creating for stand-alone file or script: {0}", fileName)
                let opts =
                  Async.RunSynchronously (checker.GetProjectOptionsFromScript(fileName, source, fakeDateTimeRepresentingTimeLoaded projFilename),
                                          timeout = ServiceSettings.maximumTimeout)

              // The InteractiveChecker resolution sometimes doesn't include FSharp.Core and other essential assemblies, so we need to include them by hand
                if opts.OtherOptions |> Seq.exists (fun s -> s.Contains("FSharp.Core.dll")) then opts
                else
                  // Add assemblies that may be missing in the standard assembly resolution
                  LoggingService.LogDebug("LanguageService: GetScriptCheckerOptions: Adding missing core assemblies.")
                  let dirs = FSharpEnvironment.getDefaultDirectories (None, FSharpTargetFramework.NET_4_5 )
                  { opts with OtherOptions = [| yield! opts.OtherOptions
                                                match FSharpEnvironment.resolveAssembly dirs "FSharp.Core" with
                                                | Some fn -> yield String.Format ("-r:{0}", fn)
                                                | None ->
                                                      LoggingService.LogDebug("LanguageService: Resolution: FSharp.Core assembly resolution failed!")
                                                      match FSharpEnvironment.resolveAssembly dirs "FSharp.Compiler.Interactive.Settings" with
                                                      | Some fn -> yield String.Format ("-r:{0}", fn)
                                                      | None -> LoggingService.LogDebug("LanguageService: Resolution: FSharp.Compiler.Interactive.Settings assembly resolution failed!") |]}
            with e -> failwithf "Exception when getting check options for '%s'\n.Details: %A" fileName e

        // Print contents of check option for debugging purposes
        // LoggingService.LogDebug(sprintf "GetScriptCheckerOptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A"
        //                      opts.ProjectFileName opts.ProjectFileNames opts.ProjectOptions opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules)
        Some opts

    member x.GetProjectOptionsFromProjectFile(project:DotNetProject) =
        let config =
            match IdeApp.Workspace with
            | null -> ConfigurationSelector.Default
            | ws ->
               match ws.ActiveConfiguration with
               | null -> ConfigurationSelector.Default
               | config -> config

        let getReferencedProjects (project:DotNetProject) =
            project.GetReferencedAssemblyProjects config
            |> Seq.filter (fun p -> p <> project && p.SupportedLanguages |> Array.contains "F#")

        let rec getOptions referencedProject =
            let projectOptions = CompilerArguments.getArgumentsFromProject referencedProject
            let referencedProjectOptions =
                referencedProject
                |> getReferencedProjects
                |> Seq.fold (fun (acc) reference ->
                                 match getOptions reference with
                                 | Some outFile, opts  -> (outFile, opts) :: acc
                                 | None,_ -> acc) ([])
                                
            (Some (referencedProject.GetOutputFileName(config).ToString()), { projectOptions with ReferencedProjects = referencedProjectOptions |> Array.ofList } )
    
        let _file, projectOptions = getOptions project
        projectOptions
                
    /// Constructs options for the interactive checker for a project under the given configuration.
    member x.GetProjectCheckerOptions(projFilename, ?properties) : FSharpProjectOptions option =
        let properties = defaultArg properties ["Configuration", "Debug"]
        let key = (projFilename, properties)

        lock projectInfoCache (fun () ->
            match (!projectInfoCache).TryFind (key) with
            | Some entry, cache ->
                LoggingService.logDebug "LanguageService: GetProjectCheckerOptions: Getting ProjectOptions from cache for:%s}" (Path.GetFileName(projFilename))
                projectInfoCache := cache
                Some entry
            | _, cache ->
                let project =
                    IdeApp.Workspace.GetAllProjects()
                    |> Seq.tryFind (fun p -> p.FileName.FullPath.ToString() = projFilename)

                match project with
                | Some proj ->
                    let opts = x.GetProjectOptionsFromProjectFile (proj :?> DotNetProject)

                    projectInfoCache := cache.Add (key, opts)
                    // Print contents of check option for debugging purposes
                    LoggingService.logDebug "GetProjectCheckerOptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A"
                        opts.ProjectFileName opts.ProjectFileNames opts.OtherOptions opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules
                    Some opts
                | None -> None)

    member x.StartBackgroundCompileOfProject (projectFilename) =
        x.GetProjectCheckerOptions(projectFilename)
        |> Option.iter(fun opts -> checker.CheckProjectInBackground(opts))

    member internal x.TryGetStaleTypedParseResult(fileName:string, options, src, stale)  =
        // Try to get recent results from the F# service
        let res =
            match stale with
            | AllowStaleResults.MatchingFileName -> checker.TryGetRecentCheckResultsForFile(fixFileName fileName, options)
            | AllowStaleResults.MatchingSource -> checker.TryGetRecentCheckResultsForFile(fixFileName fileName, options, source=src)

        match res with
        | Some (untyped,typed,_) when typed.HasFullTypeCheckInfo -> Some (ParseAndCheckResults(Some typed, Some untyped))
        | Some (untyped,_,_) -> Some (ParseAndCheckResults(None, Some untyped))
        | _ -> None

    member internal x.ParseAndCheckFile (fileName, src, version:int, options: FSharpProjectOptions option, obsoleteCheck) =
        async {
            try
                let fileName = fixFileName(fileName)
                match options with
                | Some opts ->
                    let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(fileName, version, src ,opts, obsoleteCheck, null )

                    // Construct new typed parse result if the task succeeded
                    let results =
                      match checkAnswer with
                      | FSharpCheckFileAnswer.Succeeded(checkResults) ->
                          LoggingService.logDebug "LanguageService: ParseAndCheckFile check succeeded for %s" (Path.GetFileName(fileName))
                          ParseAndCheckResults(Some checkResults, Some parseResults)
                      | FSharpCheckFileAnswer.Aborted ->
                          LoggingService.logDebug "LanguageService: ParseAndCheckFile check aborted for %s" (Path.GetFileName(fileName))
                          ParseAndCheckResults(None, Some parseResults)

                    return results
                | None -> return ParseAndCheckResults(None, None)
            with exn ->
                LoggingService.LogDebug("LanguageService agent: Exception", exn)
                return ParseAndCheckResults(None, None) }

    member x.ParseAndCheckFileInProject(projectFilename, fileName, version:int, src:string, obsoleteCheck) =
        let fileName = if Path.GetExtension fileName = ".sketchfs" then Path.ChangeExtension (fileName, ".fsx") else fileName
        let opts = x.GetCheckerOptions(fileName, projectFilename, src)
        x.ParseAndCheckFile(fileName, src, version, opts, obsoleteCheck)

    /// Parses and checks the given file in the given project under the given configuration.
    ///Asynchronously returns the results of checking the file.
    member x.GetTypedParseResultWithTimeout(projectFilename, fileName, version:int, src:string, stale, ?timeout, ?obsoleteCheck) =
        let obs = defaultArg obsoleteCheck (IsResultObsolete(fun () -> false))
        async {
            let fileName = if Path.GetExtension fileName = ".sketchfs" then Path.ChangeExtension (fileName, ".fsx") else fileName
            let options = x.GetCheckerOptions(fileName, projectFilename, src)
            match options with
            | Some opts ->
                // Try to get recent results from the F# service
                match x.TryGetStaleTypedParseResult(fileName, opts, src, stale) with
                | Some _ as results ->
                    LoggingService.logDebug "LanguageService: GetTypedParseResultWithTimeout: using stale results"
                    return results
                | None ->
                    // If we didn't get a recent set of type checking results, we put in a request and wait for at most 'timeout' for a response
                    match timeout with
                    | Some timeout ->
                        LoggingService.logDebug "LanguageService: GetTypedParseResultWithTimeout: No stale results - typechecking with timeout"
                        let! computation = Async.StartChild(x.ParseAndCheckFile(fileName, src, version, options, obs), timeout)
                        try
                            let! result = computation
                            return Some(result)
                        with
                        | :? System.TimeoutException ->
                            LoggingService.logDebug "LanguageService: GetTypedParseResultWithTimeout: No stale results - typechecking with timeout - timeout exception occured"
                            return None
                    | None ->
                          LoggingService.logDebug "LanguageService: GetTypedParseResultWithTimeout: No stale results - typechecking without timeout"
                          let! result = x.ParseAndCheckFile(fileName, src, version, options, obs)
                          return Some(result)
            | None -> return None }

    /// Returns a TypeParsedResults if available, otherwise None
    member x.GetTypedParseResultIfAvailable(projectFilename, fileName:string, src, stale) =
        let options = x.GetCheckerOptions(fileName, projectFilename, src)
        LoggingService.logDebug "LanguageService: GetTypedParseResultIfAvailable: file=%s" (Path.GetFileName(fileName))
        options |> Option.bind(fun opts -> x.TryGetStaleTypedParseResult(fileName, opts, src, stale))

    /// Get all the uses of a symbol in the given file (using 'source' as the source for the file)
    member x.GetUsesOfSymbolAtLocationInFile(projectFilename, fileName, version, source, line:int, col, lineStr) =
        asyncMaybe {
            LoggingService.logDebug "LanguageService: GetUsesOfSymbolAtLocationInFile: file:%s, line:%i, col:%i" (Path.GetFileName(fileName)) line col
            let! _colu, identIsland = Parsing.findIdents col lineStr SymbolLookupKind.ByLongIdent |> async.Return
            let! results = x.GetTypedParseResultWithTimeout(projectFilename, fileName, version, source, AllowStaleResults.MatchingSource)
            let! symbolUse = results.GetSymbolAtLocation(line, col, lineStr)
            let lastIdent = Seq.last identIsland
            let! refs = results.GetUsesOfSymbolInFile(symbolUse.Symbol) |> Async.map Some
            return (lastIdent, refs) }

    /// Get all the uses of the specified symbol in the current project and optionally all dependent projects
    member x.GetUsesOfSymbolInProject(projectFilename, file, source, symbol:FSharpSymbol, ?dependentProjects) =
        async {
            LoggingService.logDebug "LanguageService: GetUsesOfSymbolInProject: project:%s, currentFile:%s, symbol:%s" projectFilename file symbol.DisplayName

            let sourceProjectOptions = x.GetCheckerOptions(file, projectFilename, source)
            let dependentProjectsOptions = defaultArg dependentProjects [] |> List.map x.GetProjectCheckerOptions
            let! allProjectResults =
                sourceProjectOptions :: dependentProjectsOptions
                |> List.choose id
                |> Async.List.map checker.ParseAndCheckProject

            let! allSymbolUses =
                allProjectResults
                |> List.map (fun checkedProj -> checkedProj.GetUsesOfSymbol(symbol))
                |> Async.Parallel
                |> Async.map Array.concat

          return allSymbolUses }

    member x.MatchingBraces(filename, projectFilename, source) =
        let options = x.GetCheckerOptions(filename, projectFilename, source)
        match options with
        | Some opts ->
            checker.MatchBracesAlternate(filename, source, opts)
        | None -> async { return [||] }

    /// Get all symbols derived from the specified symbol in the current project and optionally all dependent projects
    member x.GetDerivedSymbolsInProject(projectFilename, file, source, symbolAtCaret:FSharpSymbol, ?dependentProjects) =
        let predicate (symbolUse: FSharpSymbolUse) =
            try
                match symbolAtCaret with
                | :? FSharpMemberOrFunctionOrValue as caretmfv ->
                    match symbolUse.Symbol with
                    | :? FSharpMemberOrFunctionOrValue as mfv ->
                        let isOverrideOrDefault = mfv.IsOverrideOrExplicitInterfaceImplementation
                        let baseTypeMatch() =
                            maybe {
                                let! ent = mfv.EnclosingEntitySafe
                                let! bt = ent.BaseType
                                let! carentEncEnt = caretmfv.EnclosingEntitySafe
                                return carentEncEnt.IsEffectivelySameAs bt.TypeDefinition }

                        let nameMatch = mfv.DisplayName = caretmfv.DisplayName
                        let parameterMatch() =
                            let both = (mfv.CurriedParameterGroups |> Seq.concat) |> Seq.zip (caretmfv.CurriedParameterGroups |> Seq.concat)
                            let allMatch = both |> Seq.forall (fun (one, two) -> one.Type = two.Type)
                            allMatch
                        let allmatch = nameMatch && isOverrideOrDefault && baseTypeMatch().IsSome && parameterMatch()
                        allmatch
                    | _ -> false
                | :? FSharpEntity as ent ->
                    match symbolUse.Symbol with
                    | :? FSharpEntity as fse ->
                        match fse.BaseType with
                        | Some basetype ->
                            basetype.TypeDefinition.IsEffectivelySameAs ent
                        | _ -> false
                    | _ -> false
                | _ -> false
            with ex ->
                false

        async {
            LoggingService.logDebug "LanguageService: GetDerivedSymbolInProject: proj:%s, file:%s, symbol:%s" projectFilename file symbolAtCaret.DisplayName
            let sourceProjectOptions = x.GetCheckerOptions(file, projectFilename, source)
            let dependentProjectsOptions = defaultArg dependentProjects [] |> List.map x.GetProjectCheckerOptions

            let! allProjectResults =
                sourceProjectOptions :: dependentProjectsOptions
                |> List.choose id
                |> Async.List.map checker.ParseAndCheckProject

            let! allSymbolUses =
                allProjectResults
                |> List.map (fun checkedProj -> checkedProj.GetAllUsesOfAllSymbols())
                |> Async.Parallel
                |> Async.map Array.concat

            let filteredSymbols = allSymbolUses |> Array.filter predicate

            return filteredSymbols }

    /// Get all overloads derived from the specified symbol in the current project
    //Currently there seems to be an issue in FCS where a methods EnclosingEntity returns the wrong type
    //The sanest option is to just use the OverloadsProperty for now.
    member x.GetOverridesForSymbol(symbolAtCaret:FSharpSymbol) =
        try
          match symbolAtCaret with
          | :? FSharpMemberOrFunctionOrValue as caretmfv ->
              caretmfv.Overloads(false)
          | _ -> None
        with _ -> None

    member x.GetExtensionMethods(projectFilename, file, source, symbolAtCaret:FSharpSymbol, ?dependentProjects) =

        let isAnAttributedExtensionMethod (symbolToCompare:FSharpMemberOrFunctionOrValue) parentEntity =
            let hasExtension = symbolToCompare.Attributes |> Seq.tryFind (fun a -> a.AttributeType.DisplayName = "ExtensionAttribute") |> Option.isSome
            if hasExtension then
                let firstCurriedParamter = symbolToCompare.CurriedParameterGroups.[0].[0]
                let sameAs = firstCurriedParamter.Type.TypeDefinition.IsEffectivelySameAs parentEntity
                sameAs
            else false

        let isAnExtensionMethod (mfv:FSharpMemberOrFunctionOrValue) (parentEntity:FSharpSymbol) =
            let isExt = mfv.IsExtensionMember
            let extslogicalEntity = mfv.LogicalEnclosingEntity
            let sameLogicalParent = parentEntity.IsEffectivelySameAs extslogicalEntity
            isExt && sameLogicalParent

        let predicate (symbolUse: FSharpSymbolUse) =
            try
                match symbolAtCaret with
                | :? FSharpEntity as caretEntity ->
                    match symbolUse.Symbol with
                    | :? FSharpMemberOrFunctionOrValue as mfv ->
                        isAnExtensionMethod mfv caretEntity || isAnAttributedExtensionMethod mfv caretEntity
                    | _ -> false
                | _ -> false
            with ex ->
                false

        async {
            LoggingService.logDebug "LanguageService: GetExtensionMethods: proj:%s, file:%s, symbol:%s" projectFilename file symbolAtCaret.DisplayName
            let sourceProjectOptions = x.GetCheckerOptions(file, projectFilename, source)
            let dependentProjectsOptions = defaultArg dependentProjects [] |> List.map x.GetProjectCheckerOptions

            let! allProjectResults =
                sourceProjectOptions :: dependentProjectsOptions
                |> List.choose id
                |> Async.List.map checker.ParseAndCheckProject

            let! allSymbolUses =
                allProjectResults
                |> List.map (fun checkedProj -> checkedProj.GetAllUsesOfAllSymbols())
                |> Async.Parallel
                |> Async.map Array.concat

            let filteredSymbols = allSymbolUses |> Array.filter predicate
            return filteredSymbols }

    member x.ParseAndCheckProject options =
        checker.ParseAndCheckProject(options)

    member x.GetCachedProjectCheckResult (project:Project) =
        //TODO clear cache on project invalidation
        //should we?  Or just wait for the checker to finish which will update it anyway.
        match checkProjectResultsCache.TryGetValue (project.FileName.ToString()) with
        | true, v -> Some v
        | false, _ -> None

    /// This function is called when the project is know to have changed for reasons not encoded in the ProjectOptions
    /// e.g. dependent references have changed
    member x.InvalidateConfiguration(options) =
        LoggingService.logDebug "LanguageService: Invalidating configuration for:%s" (Path.GetFileName(options.ProjectFileName))
        checker.InvalidateConfiguration(options)

    //flush all caches and garbage collect
    member x.ClearRootCaches() =
        LoggingService.logDebug "LanguageService: Clearing root caches and finalizing transients"
        checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
        checkProjectResultsCache.Clear()
        x.ClearProjectInfoCache()