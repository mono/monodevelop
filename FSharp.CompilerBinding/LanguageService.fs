namespace FSharp.CompilerBinding
open System
open System.IO
open System.Diagnostics
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

// --------------------------------------------------------------------------------------
/// Wraps the result of type-checking and provides methods for implementing
/// various IntelliSense functions (such as completion & tool tips)
type TypedParseResult(info:CheckFileResults, untyped : ParseFileResults) =
    let token = Parser.tagOfToken(Parser.token.IDENT("")) 

    /// Get declarations at the current location in the specified document
    /// (used to implement dot-completion in 'FSharpTextEditorCompletion.fs')
    member x.GetDeclarations(line, col, lineStr) = 
        match Parsing.findLongIdentsAndResidue(col, lineStr) with
        | None -> None
        | Some (longName, residue) ->
            Debug.WriteLine (sprintf "GetDeclarations: '%A', '%s'" longName residue)
            // Get items & generate output
            try Some (info.GetDeclarations(None, line, col, lineStr, longName, residue, fun (_,_) -> false)
                      |> Async.RunSynchronously, residue)
            with :? TimeoutException as e -> None

    /// Get the tool-tip to be displayed at the specified offset (relatively
    /// from the beginning of the current document)
    member x.GetToolTip(line, col, lineStr) : Option<ToolTipText * (int * int)> =
        match Parsing.findLongIdents(col, lineStr) with 
        | None -> None
        | Some(col,identIsland) ->
          let res = info.GetToolTipText(line, col, lineStr, identIsland, token)
          Debug.WriteLine("Result: Got something, returning")
          Some (res, (col - (Seq.last identIsland).Length, col))

    member x.GetDeclarationLocation(line, col, lineStr) =
        match Parsing.findLongIdents(col, lineStr) with 
        | None -> FindDeclResult.DeclNotFound FindDeclFailureReason.Unknown
        | Some(col,identIsland) ->
            let res = info.GetDeclarationLocation(line, col, lineStr, identIsland, true)
            Debug.WriteLine("Result: Got something, returning")
            res 

    member x.GetMethods(line, col, lineStr) =
        match Parsing.findLongIdentsAtGetMethodsTrigger(col, lineStr) with 
        | None -> None
        | Some(col,identIsland) ->
            let res = info.GetMethods(line, col, lineStr, Some identIsland)
            Debug.WriteLine("Result: Got something, returning")
            Some (res.MethodName, res.Methods) 

    member x.GetSymbol(line, col, lineStr) =
        match Parsing.findLongIdents(col, lineStr) with 
        | Some(colu, identIsland) ->
            //get symbol at location
            //Note we advance the caret to 'colu' ** due to GetSymbolAtLocation only working at the beginning/end **
            info.GetSymbolAtLocation(line, colu, lineStr, identIsland)
        | None -> None

    member x.CheckFileResults = info        
    member x.ParseFileResults = untyped 

// --------------------------------------------------------------------------------------
/// Represents request send to the background worker
/// We need information about the current file and project (options)
type internal ParseRequest (file:string, source:string, options:ProjectOptions, fullCompile:bool, afterCompleteTypeCheckCallback: (string * ErrorInfo [] -> unit) option) =
  member x.File  = file
  member x.Source = source
  member x.Options = options
  member x.StartFullCompile = fullCompile
  /// A callback that gets called asynchronously on a background thread after a full, complete and accurate typecheck of a file has finally completed.
  member x.AfterCompleteTypeCheckCallback = afterCompleteTypeCheckCallback
  
// --------------------------------------------------------------------------------------
// Language service - is a mailbox processor that deals with requests from the user
// interface - mainly to trigger background parsing or get current parsing results
// All processing in the mailbox is quick - however, if we don't have required info
// we post ourselves a message that will be handled when the info becomes available

type internal LanguageServiceMessage = 
  // Trigger parse request in ParserWorker
  | TriggerRequest of ParseRequest
  // Request for information - when we receive this, we parse and reply when information become available
  | UpdateAndGetTypedInfo of ParseRequest * AsyncReplyChannel<TypedParseResult>
  | GetTypedInfoDone of AsyncReplyChannel<TypedParseResult>


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
   
  // Mailbox of this 'LanguageService'
  let mbox = MailboxProcessor.Start(fun mbox ->
    
    // Tail-recursive loop that remembers the current state
    // (untyped and typed parse results)
    let rec loop typedInfo =
      mbox.Scan(fun msg ->
        Debug.WriteLine(sprintf "Worker: Checking message %s" (msg.GetType().Name))
        match msg, typedInfo with 
        | TriggerRequest(info), _ -> Some <| async {
          let newTypedInfo = 
           try
            Debug.WriteLine("Worker: TriggerRequest")
            let fileName = info.File        
            Debug.WriteLine("Worker: Request parse received")
            // Run the untyped parsing of the file and report result...
            Debug.WriteLine("Worker: Untyped parse...")
            let untypedInfo = try checker.ParseFileInProject(fileName, info.Source, info.Options) 
                              with e -> Debug.WriteLine(sprintf "Worker: Error in UntypedParse: %s" (e.ToString()))
                                        reraise ()
              
            // Now run the type-checking
            let fileName = fixFileName(fileName)
            Debug.WriteLine("Worker: Typecheck source...")
            let updatedTyped = try checker.CheckFileInProjectIfReady( untypedInfo, fileName, 0, info.Source,info.Options, IsResultObsolete(fun () -> false), null )
                               with e -> Debug.WriteLine(sprintf "Worker: Error in TypeCheckSource: %s" (e.ToString()))
                                         reraise ()
              
            // If this is 'full' request, then start background compilations too
            if info.StartFullCompile then
                Debug.WriteLine(sprintf "Worker: Starting background compilations")
                checker.StartBackgroundCompile(info.Options)
            Debug.WriteLine(sprintf "Worker: Parse completed")

            let file = info.File

            // Construct new typed parse result if the task succeeded
            let newTypedInfo =
              match updatedTyped with
              | Some(CheckFileAnswer.Succeeded(results)) ->
                  // Handle errors on the GUI thread
                  Debug.WriteLine(sprintf "LanguageService: Update typed info - HasFullTypeCheckInfo? %b" results.HasFullTypeCheckInfo)
                  match info.AfterCompleteTypeCheckCallback with 
                  | None -> ()
                  | Some cb -> 
                      Debug.WriteLine (sprintf "Errors: Got update for: %s" (Path.GetFileName(file)))
                      cb(file, results.Errors)

                  match results.HasFullTypeCheckInfo with
                  | true -> Some(TypedParseResult(results, untypedInfo))
                  | _ -> typedInfo
              | _ -> 
                  Debug.WriteLine("LanguageService: Update typed info - failed")
                  typedInfo
            newTypedInfo
           with e -> 
            Debug.WriteLine (sprintf "Errors: Got unexpected background error: %s" (e.ToString()))
            typedInfo
          return! loop newTypedInfo }

        
        // When we receive request for information and we don't have it we trigger a 
        // parse request and then send ourselves a message, so that we can reply later
        | UpdateAndGetTypedInfo(req, reply), _ -> Some <| async { 
            Debug.WriteLine ("LanguageService: UpdateAndGetTypedInfo")
            mbox.Post(TriggerRequest(req))
            mbox.Post(GetTypedInfoDone(reply)) 
            return! loop typedInfo }
                    
        | GetTypedInfoDone(reply), (Some typedRes) -> Some <| async {
            Debug.WriteLine (sprintf "LanguageService: GetTypedInfoDone")
            reply.Reply(typedRes)
            return! loop typedInfo }

        // We didn't have information to reply to a request - keep waiting for results!
        // The caller will probably timeout.
        | GetTypedInfoDone _, None -> 
            Debug.WriteLine("Worker: No match found for the message, leaving in queue until info is available")
            None )
        
    // Start looping with no initial information        
    loop None)

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
          
          // The InteractiveChecker resolution sometimes doesn't include FSharp.Core and other essential assemblies, so we need to include them by hand
          if opts.ProjectOptions |> Seq.exists (fun s -> s.Contains("FSharp.Core.dll")) then opts
          else 
            // Add assemblies that may be missing in the standard assembly resolution
            Debug.WriteLine("GetScriptCheckerOptions: Adding missing core assemblies.")
            let dirs = FSharpEnvironment.getDefaultDirectories (FSharpCompilerVersion.LatestKnown, targetFramework )
            {opts with ProjectOptions = [| yield! opts.ProjectOptions
                                           match FSharpEnvironment.resolveAssembly dirs "FSharp.Core" with
                                           | Some fn -> yield sprintf "-r:%s" fn
                                           | None -> Debug.WriteLine("Resolution: FSharp.Core assembly resolution failed!")
                                           match FSharpEnvironment.resolveAssembly dirs "FSharp.Compiler.Interactive.Settings" with
                                           | Some fn -> yield sprintf "-r:%s" fn
                                           | None -> Debug.WriteLine("Resolution: FSharp.Compiler.Interactive.Settings assembly resolution failed!") |]}
        with e -> failwithf "Exception when getting check options for '%s'\n.Details: %A" fileName e

    // Print contents of check option for debugging purposes
    Debug.WriteLine(sprintf "GetScriptCheckerOptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A" 
                         opts.ProjectFileName opts.ProjectFileNames opts.ProjectOptions opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules)
    opts
   
  /// Constructs options for the interactive checker for a project under the given configuration. 
  member x.GetProjectCheckerOptions(projFilename, files, args, targetFramework) =
    let opts = 
      
      // We are in a project - construct options using current properties
        Debug.WriteLine (sprintf "GetProjectCheckerOptions: Creating for project '%s'" projFilename )

        {ProjectFileName = projFilename
         ProjectFileNames = files
         ProjectOptions = args
         IsIncompleteTypeCheckEnvironment = false
         UseScriptResolutionRules = false   
         LoadTime = fakeDateTimeRepresentingTimeLoaded projFilename
         UnresolvedReferences = None } 

    // Print contents of check option for debugging purposes
    Debug.WriteLine(sprintf "GetProjectCheckerOptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A" 
                         opts.ProjectFileName opts.ProjectFileNames opts.ProjectOptions opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules)
    opts
    
  
  /// Parses and type-checks the given file in the given project under the given configuration. The callback
  /// is called after the complete typecheck has been performed.
  member x.TriggerParse(projectFilename, fileName:string, src, files, args, targetFramework, afterCompleteTypeCheckCallback) = 
    let opts = x.GetCheckerOptions(fileName, projectFilename,  src, files , args, targetFramework)
    Debug.WriteLine(sprintf "Parsing: Trigger parse (fileName=%s)" fileName)
    mbox.Post(TriggerRequest(ParseRequest(fileName, src, opts, true, Some afterCompleteTypeCheckCallback)))

  member x.GetUntypedParseResult(projectFilename, fileName:string, src, files, args, targetFramework) = 
        let opts = x.GetCheckerOptions(fileName, projectFilename, src, files, args, targetFramework)
        Debug.WriteLine(sprintf "Parsing: Get untyped parse result (fileName=%s)" fileName)
        let req = ParseRequest(fileName, src, opts, false, None)
        checker.ParseFileInProject(fileName, src, opts)

  member x.GetTypedParseResult(projectFilename, fileName:string, src, files, args, allowRecentTypeCheckResults, timeout, targetFramework)  : TypedParseResult = 
    let opts = x.GetCheckerOptions(fileName, projectFilename, src, files, args, targetFramework)
    Debug.WriteLine("Parsing: Get typed parse result, fileName={0}", fileName)
    let req = ParseRequest(fileName, src, opts, false, None)
    // Try to get recent results from the F# service
    match checker.TryGetRecentTypeCheckResultsForFile(fileName, req.Options) with
    | Some(untyped, typed, _) when typed.HasFullTypeCheckInfo && allowRecentTypeCheckResults ->
        Debug.WriteLine(sprintf "Worker: Quick parse completed - success")
        TypedParseResult(typed, untyped)
    | _ -> Debug.WriteLine(sprintf "Worker: No TryGetRecentTypeCheckResultsForFile - trying typecheck with timeout")
           // If we didn't get a recent set of type checking results, we put in a request and wait for at most 'timeout' for a response
           mbox.PostAndReply((fun repl -> UpdateAndGetTypedInfo(req, repl)), timeout = timeout)

  member x.GetUsesOfSymbolAtLocation(projectFilename, file, source, files, line:int, col, lineStr, args, framework) =
   async { 
    let projectOptions = x.GetCheckerOptions(file, projectFilename, source, files, args, framework)

    //parse and retrieve Checked Project results, this has the entity graph and errors etc
    let! projectResults = checker.ParseAndCheckProject(projectOptions) 
  
    //get the parse results for the current file
    let! backgroundParseResults, backgroundTypedParse = //Note this operates on the file system so the file needs to be current
        checker.GetBackgroundCheckResultsForFileInProject(file, projectOptions) 

    match FSharp.CompilerBinding.Parsing.findLongIdents(col, lineStr) with 
    | Some(colu, identIsland) ->
        //get symbol at location
        //Note we advance the caret to 'colu' ** due to GetSymbolAtLocation only working at the beginning/end **
        match backgroundTypedParse.GetSymbolAtLocation(line, colu, lineStr, identIsland) with
        | Some(symbol) ->
            let lastIdent = Seq.last identIsland
            let refs = projectResults.GetUsesOfSymbol(symbol)
            return Some(lastIdent, refs)
        | _ -> return None
    | _ -> return None }

  member x.GetUsesOfSymbol(projectFilename, file, source, files, args, framework, symbol:FSharpSymbol) =
   async { 
    let projectOptions = x.GetCheckerOptions(file, projectFilename, source, files, args, framework)

    //parse and retrieve Checked Project results, this has the entity graph and errors etc
    let! projectResults = checker.ParseAndCheckProject(projectOptions) 
  
    let refs = projectResults.GetUsesOfSymbol(symbol)
    return refs }

  member x.Checker = checker

