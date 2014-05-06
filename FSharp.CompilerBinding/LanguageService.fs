namespace FSharp.CompilerBinding
open System
open System.IO
open System.Diagnostics
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

// --------------------------------------------------------------------------------------
/// Wraps the result of type-checking and provides methods for implementing
/// various IntelliSense functions (such as completion & tool tips). Provides default
/// empty/negative results if information is missing.
type ParseAndCheckResults private (infoOpt: (CheckFileResults * ParseFileResults) option) =
    let token = Parser.tagOfToken(Parser.token.IDENT("")) 

    new (checkResults, parseResults) = ParseAndCheckResults(Some (checkResults, parseResults))

    static member Empty = ParseAndCheckResults(None)

    /// Get declarations at the current location in the specified document
    /// (used to implement dot-completion in 'FSharpTextEditorCompletion.fs')
    member x.GetDeclarations(line, col, lineStr) = 
        match infoOpt with 
        | None -> None
        | Some (checkResults, parseResults) -> 
            let longName,residue = Parsing.findLongIdentsAndResidue(col, lineStr)
            Debug.WriteLine (sprintf "GetDeclarations: '%A', '%s'" longName residue)
            // Get items & generate output
            try Some (checkResults.GetDeclarationsAlternate(Some parseResults, line, col, lineStr, longName, residue, fun (_,_) -> false)
                      |> Async.RunSynchronously, residue)
            with :? TimeoutException as e -> None

    /// Get the tool-tip to be displayed at the specified offset (relatively
    /// from the beginning of the current document)
    member x.GetToolTip(line, col, lineStr) =
      async {
        match infoOpt with 
        | None -> return None
        | Some (checkResults, parseResults) -> 
        match Parsing.findLongIdents(col, lineStr) with 
        | None -> return None
        | Some(col,identIsland) ->
          let! res = checkResults.GetToolTipTextAlternate(line, col, lineStr, identIsland, token)
          Debug.WriteLine("Result: Got something, returning")
          return Some (res, (col - 10, col))
      }
    member x.GetDeclarationLocation(line, col, lineStr) =
      async {
        match infoOpt with 
        | None -> return FindDeclResult.DeclNotFound FindDeclFailureReason.Unknown
        | Some (checkResults, parseResults) -> 
        match Parsing.findLongIdents(col, lineStr) with 
        | None -> return FindDeclResult.DeclNotFound FindDeclFailureReason.Unknown
        | Some(col,identIsland) -> return! checkResults.GetDeclarationLocationAlternate(line, col, lineStr, identIsland, false)
      }
    member x.GetMethods(line, col, lineStr) =
      async { 
        match infoOpt with 
        | None -> return None
        | Some (checkResults, parseResults) -> 
        match Parsing.findLongIdentsAtGetMethodsTrigger(col, lineStr) with 
        | None -> return None
        | Some(col,identIsland) ->
            let! res = checkResults.GetMethodsAlternate(line, col, lineStr, Some identIsland)
            Debug.WriteLine("Result: Got something, returning")
            return Some (res.MethodName, res.Methods) 
      }

    member x.GetSymbol(line, col, lineStr) =
      async {
        match infoOpt with 
        | None -> return None
        | Some (checkResults, parseResults) -> 
        match Parsing.findLongIdents(col, lineStr) with 
        | None -> return None
        | Some(colu, identIsland) ->
            return! checkResults.GetSymbolUseAtLocation(line, colu, lineStr, identIsland)
      }
    member x.GetSymbolAtLocation(line, col, lineStr, identIsland) =
      async {
        match infoOpt with 
        | None -> return None
        | Some (checkResults, parseResults) -> 
            return! checkResults.GetSymbolUseAtLocation (line, col, lineStr, identIsland)
      }
    member x.GetUsesOfSymbolInFile(symbol) =
      async {
        match infoOpt with 
        | None -> return [| |]
        | Some (checkResults, parseResults) -> return! checkResults.GetUsesOfSymbolInFile(symbol)
      }
    member x.GetErrors() =
        match infoOpt with 
        | None -> None
        | Some (checkResults, parseResults) -> Some checkResults.Errors

    member x.GetNavigationItems() =
        match infoOpt with 
        | None -> [| |]
        | Some (checkResults, parseResults) -> 
           // GetNavigationItems is not 100% solid and throws occasional exceptions
            try parseResults.GetNavigationItems().Declarations
            with _ -> 
                Debug.Assert(false, "couldn't update navigation items, ignoring")  
                [| |]
    member x.ParseTree = match infoOpt with
                         | Some (check,parse) -> parse.ParseTree
                         | None -> None    

[<RequireQualifiedAccess>]
type AllowStaleResults = 
    // Allow checker results where the source doesn't even match
    | MatchingFileName
    // Allow checker results where the source matches but where the background builder may not have caught up yet after some other change
    | MatchingSource
    // Don't allow stale results
    | No

  
// --------------------------------------------------------------------------------------
// Language service 

/// Provides functionality for working with the F# interactive checker running in background
type LanguageService(dirtyNotify) =
  let tryGetSymbolRange (range: Range.range option) = 
        range |> Option.map (fun dec -> dec.FileName, ((dec.StartLine-1, dec.StartColumn), (dec.EndLine-1, dec.EndColumn)))

  /// Load times used to reset type checking properly on script/project load/unload. It just has to be unique for each project load/reload.
  /// Not yet sure if this works for scripts.
  let fakeDateTimeRepresentingTimeLoaded proj = DateTime(abs (int64 (match proj with null -> 0 | _ -> proj.GetHashCode())) % 103231L)

  // Create an instance of interactive checker. The callback is called by the F# compiler service
  // when its view of the prior-typechecking-state of the start of a file has changed, for example
  // when the background typechecker has "caught up" after some other file has been changed, 
  // and its time to re-typecheck the current file.
  let checker = 
    let checker = InteractiveChecker.Create()
    checker.BeforeBackgroundFileCheck.Add dirtyNotify
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
   
  // We use a mailbox processor to wrap requests to F.C.S. here so 
  //   (a) we can get work off the GUI thread and 
  //   (b) timeout on synchronous requests. 
  // 
  // There is already a background compilation queue+thread in F.C.S. abd many of the F.C.S. operations
  // are already asynchronous. However using direct calls to the F.C.S. API isn't quite sufficient because
  // we can't timeout, and not all F.C.S. operations are asynchronous (e.g. parsing). If F.C.S. is extended
  // so all operations are asynchronous then I believe we won't need a wrapper agent at all.
  //
  // Every request to this agent is 'PostAndReply' or 'PostAndAsyncReply'.  This means the requests are
  // a lot like a function call, except 
  //   (a) they may be asynchronous (reply is interleaved on the UI thread)
  //   (b) they may be on on a timeout (to prevent blocking the UI thread)
  //   (c) only one request is active at a time, the rest are in the queue

  let mbox = MailboxProcessor.Start(fun mbox ->
    
    async { 
       while true do
            Debug.WriteLine("Worker: Awaiting request") 
            let! (fileName, source, options, reply: AsyncReplyChannel<_> ) = mbox.Receive()
            
            let fileName = fixFileName(fileName)            
            
            Debug.WriteLine("Worker: Request received, fileName = {0}, parsing...", box fileName)
            let! parseResults = checker.ParseFileInProject(fileName, source, options) 
              
            Debug.WriteLine("Worker: Typecheck source...")
            let! checkAnswer = checker.CheckFileInProject(parseResults, fileName, 0, source,options, IsResultObsolete(fun () -> false), null )
              
            Debug.WriteLine(sprintf "Worker: Parse completed")

            // Construct new typed parse result if the task succeeded
            let results =
              match checkAnswer with
              | CheckFileAnswer.Succeeded(checkResults) ->
                  Debug.WriteLine(sprintf "LanguageService: Update typed info - HasFullTypeCheckInfo? %b" checkResults.HasFullTypeCheckInfo)
                  ParseAndCheckResults(checkResults, parseResults)
              | _ -> 
                  Debug.WriteLine("LanguageService: Update typed info - failed")
                  ParseAndCheckResults.Empty
                  
            reply.Reply results
        })

  /// Constructs options for the interactive checker for the given file in the project under the given configuration.
  member x.GetCheckerOptions(fileName, projFilename, source, files, args, targetFramework) =
    let ext = Path.GetExtension(fileName)
    let opts = 
      if (ext = ".fsx" || ext = ".fsscript") then
        // We are in a stand-alone file or we are in a project, but currently editing a script file
        x.GetScriptCheckerOptions(fileName, projFilename, source, targetFramework)
          
      // We are in a project - construct options using current properties
      else
        x.GetProjectCheckerOptions(projFilename, files, args, targetFramework)
    opts
   
  /// Constructs options for the interactive checker for the given script file in the project under the given configuration. 
  member x.GetScriptCheckerOptions(fileName, projFilename, source, targetFramework) =
    let ext = Path.GetExtension(fileName)
    let opts = 
        // We are in a stand-alone file or we are in a project, but currently editing a script file
        try 
          let fileName = fixFileName(fileName)
          Debug.WriteLine (sprintf "GetScriptCheckerOptions: Creating for stand-alone file or script: '%s'" fileName )
          let opts = checker.GetProjectOptionsFromScript(fileName, source, fakeDateTimeRepresentingTimeLoaded projFilename)
                     |> Async.RunSynchronously
          
          // The InteractiveChecker resolution sometimes doesn't include FSharp.Core and other essential assemblies, so we need to include them by hand
          if opts.ProjectOptions |> Seq.exists (fun s -> s.Contains("FSharp.Core.dll")) then opts
          else 
            // Add assemblies that may be missing in the standard assembly resolution
            Debug.WriteLine("GetScriptCheckerOptions: Adding missing core assemblies.")
            let dirs = FSharpEnvironment.getDefaultDirectories (None, targetFramework )
            {opts with ProjectOptions = [| yield! opts.ProjectOptions
                                           match FSharpEnvironment.resolveAssembly dirs "FSharp.Core" with
                                           | Some fn -> yield sprintf "-r:%s" fn
                                           | None -> Debug.WriteLine("Resolution: FSharp.Core assembly resolution failed!")
                                           match FSharpEnvironment.resolveAssembly dirs "FSharp.Compiler.Interactive.Settings" with
                                           | Some fn -> yield sprintf "-r:%s" fn
                                           | None -> Debug.WriteLine("Resolution: FSharp.Compiler.Interactive.Settings assembly resolution failed!") |]}
        with e -> failwithf "Exception when getting check options for '%s'\n.Details: %A" fileName e

    // Print contents of check option for debugging purposes
    // Debug.WriteLine(sprintf "GetScriptCheckerOptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A" 
    //                      opts.ProjectFileName opts.ProjectFileNames opts.ProjectOptions opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules)
    opts
   
  /// Constructs options for the interactive checker for a project under the given configuration. 
  member x.GetProjectCheckerOptions(projFilename, files, args, targetFramework) =
    let opts = 
      
      // We are in a project - construct options using current properties
        Debug.WriteLine (sprintf "GetProjectCheckerOptions: Creating for project '%s'" projFilename )

        {ProjectFileName = projFilename
         ProjectFileNames = files
         ProjectOptions = args
         ReferencedProjects = [| |]
         IsIncompleteTypeCheckEnvironment = false
         UseScriptResolutionRules = false   
         LoadTime = fakeDateTimeRepresentingTimeLoaded projFilename
         UnresolvedReferences = None } 

    // Print contents of check option for debugging purposes
    // Debug.WriteLine(sprintf "GetProjectCheckerOptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A" 
    //                      opts.ProjectFileName opts.ProjectFileNames opts.ProjectOptions opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules)
    opts
    
  
  /// Parses and checks the given file in the given project under the given configuration. Asynchronously
  /// returns the results of checking the file.
  member x.ParseAndCheckFileInProject(projectFilename, fileName:string, src, files, args, targetFramework, storeAst) = 
   async {
    let opts = x.GetCheckerOptions(fileName, projectFilename,  src, files , args, targetFramework)
    Debug.WriteLine(sprintf "Parsing: Trigger parse (fileName=%s)" fileName)

    // storeAst is passed from monodevelop when it finds files with the same name in other projects. 
    // It will then ask for a reparse of all files in the second project. If storeAst = false, do nothing. 
    if not storeAst then return ParseAndCheckResults.Empty else
    let! results = mbox.PostAndAsyncReply(fun r -> fileName, src, opts, r)
    Debug.WriteLine(sprintf "Worker: Starting background compilations")
    checker.StartBackgroundCompile(opts)
    return results
   }

  member x.ParseFileInProject(projectFilename, fileName:string, src, files, args, targetFramework) = 
    let opts = x.GetCheckerOptions(fileName, projectFilename, src, files, args, targetFramework)
    Debug.WriteLine(sprintf "Parsing: Get untyped parse result (fileName=%s)" fileName)
    checker.ParseFileInProject(fileName, src, opts)

  member internal x.TryGetStaleTypedParseResult(fileName:string, options, src, stale)  = 
    // Try to get recent results from the F# service
    let res = 
        match stale with 
        | AllowStaleResults.MatchingFileName -> checker.TryGetRecentTypeCheckResultsForFile(fileName, options) 
        | AllowStaleResults.MatchingSource -> checker.TryGetRecentTypeCheckResultsForFile(fileName, options, source=src) 
        | AllowStaleResults.No -> None
    match res with 
    | Some (untyped,typed,_) when typed.HasFullTypeCheckInfo  -> Some (ParseAndCheckResults(typed, untyped))
    | _ -> None

  member x.GetTypedParseResultWithTimeout(projectFilename, fileName:string, src, files, args, stale, timeout, targetFramework) = 
   async {
    let opts = x.GetCheckerOptions(fileName, projectFilename, src, files, args, targetFramework)
    Debug.WriteLine("Parsing: Get typed parse result, fileName={0}", box fileName)
    // Try to get recent results from the F# service
    match x.TryGetStaleTypedParseResult(fileName, opts, src, stale) with
    | Some _ as results ->
        Debug.WriteLine(sprintf "Parsing: using stale results")
        return results
    | None -> 
        Debug.WriteLine(sprintf "Worker: Not using stale results - trying typecheck with timeout")
        // If we didn't get a recent set of type checking results, we put in a request and wait for at most 'timeout' for a response
        return mbox.TryPostAndReply((fun reply -> (fileName, src, opts, reply)), timeout = timeout)
   }
  member x.GetTypedParseResultAsync(projectFilename, fileName:string, src, files, args, stale, targetFramework) = 
   async { 
    let opts = x.GetCheckerOptions(fileName, projectFilename, src, files, args, targetFramework)

    match x.TryGetStaleTypedParseResult(fileName, opts, src, stale)  with
    | Some results -> return results
    | None -> return! mbox.PostAndAsyncReply(fun reply -> (fileName, src, opts, reply))
   }


  /// Get all the uses of a symbol in the given file (using 'source' as the source for the file)
  member x.GetUsesOfSymbolAtLocationInFile(projectFilename, fileName, source, files, line:int, col, lineStr, args, targetFramework) =
   async { 
    match FSharp.CompilerBinding.Parsing.findLongIdents(col, lineStr) with 
    | Some(colu, identIsland) ->

        let! checkResults = x.GetTypedParseResultAsync(projectFilename, fileName, source, files, args, stale= AllowStaleResults.MatchingSource, targetFramework=targetFramework)
        let! symbolResults = checkResults.GetSymbolAtLocation(line, colu, lineStr, identIsland)
        match symbolResults with
        | Some symbolUse -> 
            let lastIdent = Seq.last identIsland
            let! refs = checkResults.GetUsesOfSymbolInFile(symbolUse.Symbol)
            return Some(lastIdent, refs)
        | None -> return None
    | None -> return None 
   }

  member x.GetUsesOfSymbolInProject(projectFilename, file, source, files, args, framework, symbol:FSharpSymbol) =
   async { 
    let projectOptions = x.GetCheckerOptions(file, projectFilename, source, files, args, framework)

    //parse and retrieve Checked Project results, this has the entity graph and errors etc
    let! projectResults = checker.ParseAndCheckProject(projectOptions) 
  
    let! refs = projectResults.GetUsesOfSymbol(symbol)
    return refs }

  member x.InvalidateConfiguration(options) = checker.InvalidateConfiguration(options)

