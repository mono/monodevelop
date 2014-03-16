// --------------------------------------------------------------------------------------
// (c) Tomas Petricek, http://tomasp.net/blog
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open System
open System.IO

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding

open Newtonsoft.Json
open Newtonsoft.Json.Converters

module FsParser = Microsoft.FSharp.Compiler.Parser

// --------------------------------------------------------------------------------------
// IntelliSense agent - provides easier an access to F# IntelliSense service
// We're using a simple agent, because requests should be done from a single thread
// --------------------------------------------------------------------------------------


/// The possible types of output
type OutputMode =
  | Json
  | Text

/// Represents information needed to call the F# IntelliSense service
/// (including project/script options, file name and source)
type internal RequestOptions(opts, file, src) =
  member x.Options : ProjectOptions = opts
  member x.FileName : string = file
  member x.Source : string = src
  member x.WithSource(source) =
    RequestOptions(opts, file, source)

  override x.ToString() =
    sprintf "FileName: '%s'\nSource length: '%d'\nOptions: %s, %A, %A, %b, %b"
      x.FileName x.Source.Length x.Options.ProjectFileName x.Options.ProjectFileNames
      x.Options.ProjectOptions x.Options.IsIncompleteTypeCheckEnvironment
      x.Options.UseScriptResolutionRules


/// Message type that is used by 'IntelliSenseAgent'
type internal IntelliSenseAgentMessage =
  | TriggerParseRequest of RequestOptions * bool
  | GetTypeCheckInfo of RequestOptions * int option * AsyncReplyChannel<CheckFileResults option>
  | GetErrors of AsyncReplyChannel<ErrorInfo[]>
  | GetDeclarationsMessage of RequestOptions * AsyncReplyChannel<TopLevelDeclaration[]>

type ResponseMsg<'T> =
  {
    Kind: string
    Data: 'T
  }

type Location =
  {
    File: string
    Line: int
    Column: int
  }

type SeverityConverter() =
  inherit JsonConverter()

  override x.CanConvert(t:System.Type) = t = typeof<Severity>
 
  override x.WriteJson(writer, value, serializer) =
    match value :?> Severity with
    | Severity.Error -> serializer.Serialize(writer, "Error")
    | Severity.Warning -> serializer.Serialize(writer, "Warning")
 
  override x.ReadJson(reader, t, _, serializer) =
    raise (System.NotSupportedException())

  override x.CanRead = false
  override x.CanWrite = true

type RangeConverter() =
  inherit JsonConverter()

  override x.CanConvert(t:System.Type) = t = typeof<Range.range>

  override x.WriteJson(writer, value, serializer) =
    let range = value :?> Range.range
    serializer.Serialize(writer, (range.Start, range.End))

  override x.ReadJson(reader, t, _, serializer) =
    raise (System.NotSupportedException())

  override x.CanRead = false
  override x.CanWrite = true

/// Provides an easy access to F# IntelliSense service
type internal IntelliSenseAgent() =

  /// Create an F# IntelliSense service
  let checker = InteractiveChecker.Create()

  /// Creates an empty "Identifier" token (we need it when getting ToolTip)
  let identToken = FsParser.tagOfToken(FsParser.token.IDENT(""))

  /// Calls F# IntelliSense service repeatedly in an (asynchronous) loop
  /// until the type check request succeeds
  let rec waitForTypeCheck(opts:RequestOptions, untypedInfo) = async {
    let info =
      checker.CheckFileInProjectIfReady
        ( untypedInfo, opts.FileName, identToken, opts.Source,
          opts.Options, IsResultObsolete(fun () -> false), null)
    match info with
    | Some(CheckFileAnswer.Succeeded(res)) when res.HasFullTypeCheckInfo ->
        return res, res.Errors
    | _ ->
        do! Async.Sleep(200)
        return! waitForTypeCheck(opts, untypedInfo) }

  /// Start the agent - the agent remembers some state
  /// (currently just a list of errors from the last parse)
  let agent = MailboxProcessor.Start(fun agent ->
    let rec loop errors = async {
      let! msg = agent.Receive()
      match msg with
      | TriggerParseRequest(opts, full) ->
          // Start parsing and update errors with the new ones
          let! untypedInfo = checker.ParseFileInProject(opts.FileName, opts.Source, opts.Options)
          let res =
            checker.CheckFileInProjectIfReady
              ( untypedInfo, opts.FileName, 0, opts.Source,
                opts.Options, IsResultObsolete(fun () -> false), null)
          let errors =
            match res with
            | Some(CheckFileAnswer.Succeeded(res)) -> res.Errors
            | _ -> errors
          // Start full background parsing if requested..
          if full then checker.StartBackgroundCompile(opts.Options)
          return! loop errors

      | GetDeclarationsMessage(opts, repl) ->
          let! untypedInfo = checker.ParseFileInProject(opts.FileName, opts.Source, opts.Options)
          repl.Reply(untypedInfo.GetNavigationItems().Declarations)
          return! loop errors

      | GetTypeCheckInfo(opts, timeout, reply) ->
          // Try to get information for the IntelliSense (in the specified time)
          let! untypedInfo = checker.ParseFileInProject(opts.FileName, opts.Source, opts.Options)
          try
            let res, errors =
              Async.RunSynchronously
                (waitForTypeCheck(opts, untypedInfo), ?timeout = timeout)
            reply.Reply(Some(res))
            return! loop errors
          with
            | :? OperationCanceledException
            | :? TimeoutException ->
                    reply.Reply(None)
                    return! loop errors

      | GetErrors(reply) ->
          // Return an array with errors that were reported last time
          reply.Reply(errors)
          return! loop errors }
    loop [||] )

  do agent.Error.Add(fun exn -> printfn "Background compiler agent died:\n%A\n" exn
                                exit 1)      

  // Copy-paste from monodevelop binding LanguageService.fs and modified
  member x.GetCheckerOptions(fileName, source, proj:Option<ProjectParser.ProjectResolver>) =
    let ext = Path.GetExtension(fileName)
    let opts =
      match proj, ext with
      | None, _
      | Some _, ".fsx"
      | Some _, ".fsscript" ->

        // We are in a stand-alone file or we are in a project,
        // but currently editing a script file
        checker.GetProjectOptionsFromScript(fileName, source, System.DateTime.Now)

          // The InteractiveChecker resolution sometimes doesn't
          // include FSharp.Core and other essential assemblies, so we may
          // need to bring over some more code from the monodevelop binding to
          // handle that situation.


      // We are in a project - construct options using current properties
      | Some proj, _ ->
        let projFile = ProjectParser.getFileName proj
        let files = ProjectParser.getFiles proj
        let args = ProjectParser.getOptions proj

        { ProjectFileName = projFile
          ProjectFileNames = files
          ProjectOptions = args
          ReferencedProjects = [| |]
          IsIncompleteTypeCheckEnvironment = false
          UseScriptResolutionRules = false
          LoadTime = ProjectParser.getLoadTime proj
          UnresolvedReferences = None }

    // Print contents of check option for debugging purposes
    // Debug.print "Checkoptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A"
    //                      opts.ProjectFileName
    //                      opts.ProjectFileNames
    //                      opts.ProjectOptions
    //                      opts.IsIncompleteTypeCheckEnvironment
    //                      opts.UseScriptResolutionRules
    opts

  /// Get errors from the last parse request
  member x.GetErrors() =
    agent.PostAndReply(GetErrors)

  /// Get declarations from the last parse request
  member x.GetDeclarations(opts) =
    agent.PostAndReply(fun repl -> GetDeclarationsMessage(opts, repl))

  /// Trigger background parse request
  member x.TriggerParseRequest(opts, full) =
    agent.Post(TriggerParseRequest(opts, full))

  /// Fetch latest type check information, possibly with a background check if needed
  member x.GetTypeCheckInfo(opts : RequestOptions, time) : Option<CheckFileResults> =
    // First check if cached results are available
    let checkres = checker.TryGetRecentTypeCheckResultsForFile(opts.FileName, opts.Options)
    match checkres with
    | Some(untyped, typed, _) when typed.HasFullTypeCheckInfo ->
      Debug.print "Worker: Quick parse completed - success"
      Some typed
    | _ ->
      // Otherwise try to get type information & run the request
      agent.PostAndReply(fun r -> GetTypeCheckInfo(opts, time, r))

  /// Invokes dot-completion request. Returns possible completions
  /// and current residue.
  member x.DoCompletion(opts : RequestOptions, line, column, lineStr, time) : Option<DeclarationSet * String> =
    Option.bind (fun (longName, residue) ->
      Option.bind (fun (info: CheckFileResults) ->
      try
        Some (info.GetDeclarations(None, line, column, lineStr, longName, residue, fun (_,_) -> false)
              |> Async.RunSynchronously, residue)
      with :? System.TimeoutException as e ->
                 None) (x.GetTypeCheckInfo(opts, time))
      ) (Parsing.findLongIdentsAndResidue (column, lineStr))

  /// Gets ToolTip for the specified location (and prints it to the output)
  member x.GetToolTip(opts, line, column, lineStr, time) : Option<ToolTipText> =

    Option.bind (fun (col',identIsland) ->
      Option.map (fun (info:CheckFileResults) ->
        // Assume that we are inside identifier (F# services can also handle
        // case when we're in a string in '#r "Foo.dll"' but we don't do that)
        info.GetToolTipText(line,col', lineStr, identIsland, identToken)
        ) (x.GetTypeCheckInfo(opts, time))
      ) (Parsing.findLongIdents(column, lineStr))

  /// Finds the point of declaration of the symbol at pos
  /// and writes information to the standard output
  member x.FindDeclaration(opts : RequestOptions, line, column, lineStr, time) =

    Option.bind (fun (col',identIsland) ->
      Option.map (fun (info:CheckFileResults) ->
        // Assume that we are inside identifier (F# services can also handle
        // case when we're in a string in '#r "Foo.dll"' but we don't do that)
        info.GetDeclarationLocation(line,col', lineStr, identIsland, true)
        ) (x.GetTypeCheckInfo(opts, time))
      ) (Parsing.findLongIdents(column, lineStr))



// --------------------------------------------------------------------------------------
// Utilities for parsing & processing command line input
// --------------------------------------------------------------------------------------

module internal CommandInput =
  open Parser

  let helpText = @"
    Supported commands
    ==================
    help
      - display this help message
    quit
      - quit the program
    errors
      - get error messages reported by last parse
    declarations ""filename""
      - get information about top-level declarations in a file with location
    parse ""<filename>"" [full]
      - trigger (full) background parse request; should be
        followed by content of a file (ended with <<EOF>>)
    completion ""<filename>"" <line> <col> [timeout]
      - trigger completion request for the specified location
    helptext <candidate>
      - fetch type signature for specified completion candidate
        (from last completion request). Only use in JSON mode.
    tooltip ""<filename>"" <line> <col> [timeout]
      - get tool tip for the specified location
    finddecl ""<filename>"" <line> <col> [timeout]
      - find the point of declaration of the symbol at specified location
    project ""<filename>""
      - associates the current session with the specified project
    outputmode {json,text}
      - switches the output format. json offers richer data
        for some commands
    compilerlocation
      - prints the best guess for the location of fsc and fsi
        (or fsharpc and fsharpi on unix)
    "
    
  let outputText = @"
    Output format
    =============

    Messages are in one of the following three forms:

    1. INFO: text
       <<EOF>>

       A single line with a free text field. Returns information.

    2. ERROR: text
       <<EOF>>

       A single line with a free text field. An error has occurred.

    3. DATA: word
       text
       <<EOF>>

       Some data, where 'word' (in [a-z]+) indicates the type of data
       followed by some lines of free text, terminated by the special
       string <<EOF>>"

  // The types of commands that need position information
  type PosCommand =
    | Completion
    | ToolTip
    | FindDeclaration

  // Command that can be entered on the command-line
  type Command =
    | PosCommand of PosCommand * string * int * int * int option
    | HelpText of string
    | Declarations of string
    | GetErrors
    | Parse of string * bool
    | Error of string
    | Project of string
    | OutputMode of OutputMode
    | CompilerLocation
    | Help
    | Quit

  /// Parse 'help' command
  let help = string "help" |> Parser.map (fun _ -> Help)

  /// Parse 'quit' command
  let quit = string "quit" |> Parser.map (fun _ -> Quit)

  /// Parse 'declarations' command
  let declarations = parser {
    let! _ = string "declarations "
    let! _ = char '"'
    let! filename = some (sat ((<>) '"')) |> Parser.map String.ofSeq
    let! _ = char '"'
    return Declarations(filename) }

  /// Parse 'errors' command
  let errors = string "errors" |> Parser.map (fun _ -> GetErrors)

  /// Parse 'outputmode' command
  let outputmode = parser {
    let! _ = string "outputmode "
    let! mode = (parser { let! _ = string "json"
                          return Json }) <|>
                (parser { let! _ = string "text"
                          return Text })
    return OutputMode mode }

  /// Parse 'project' command
  let project = parser {
    let! _ = string "project "
    let! _ = char '"'
    let! filename = some (sat ((<>) '"')) |> Parser.map String.ofSeq
    let! _ = char '"'
    return Project(filename) }

  /// Read multi-line input as a list of strings
  let rec readInput input =
    let str = Console.ReadLine()
    if str = "<<EOF>>" then List.rev input
    else readInput (str::input)

  // Parse 'parse "<filename>" [full]' command
  let parse = parser {
    let! _ = string "parse "
    let! _ = char '"'
    let! filename = some (sat ((<>) '"')) |> Parser.map String.ofSeq
    let! _ = char '"'
    let! _ = many (string " ")
    let! full = (parser { let! _ = string "full"
                          return true }) <|>
                (parser { return false })
    return Parse (filename, full) }

  // Parse 'completion "<filename>" <line> <col> [timeout]' command
  let completionTipOrDecl = parser {
    let! f = (string "completion " |> Parser.map (fun _ -> Completion)) <|>
             (string "tooltip " |> Parser.map (fun _ -> ToolTip)) <|>
             (string "finddecl " |> Parser.map (fun _ -> FindDeclaration))
    let! _ = char '"'
    let! filename = some (sat ((<>) '"')) |> Parser.map String.ofSeq
    let! _ = char '"'
    let! _ = many (string " ")
    let! line = some digit |> Parser.map (String.ofSeq >> int)
    let! _ = many (string " ")
    let! col = some digit |> Parser.map (String.ofSeq >> int)
    let! timeout =
      (parser { let! _ = some (string " ")
                return! some digit |> Parser.map (String.ofSeq >> int >> Some) }) <|>
      (parser { return None })
    return PosCommand(f, filename, line, col, timeout) }

  let helptext = parser {
      let! _ = string "helptext"
      let! _ = some (string " ")
      let! sym = many (sat (fun _ -> true)) |> Parser.map String.ofSeq
      return HelpText sym
    }

  let compilerlocation = parser {
    let! _ = string "compilerlocation"
    return CompilerLocation
    }

  // Parses always and returns default error message
  let error = parser { return Error("Unknown command or wrong arguments") }

  // Parase any of the supported commands
  let parseCommand =
    function
    | null -> Quit
    | input ->
      let reader = Parsing.createForwardStringReader input 0
      let cmds = compilerlocation <|> errors <|> outputmode <|> helptext <|> help <|> declarations <|> parse <|> project <|> completionTipOrDecl <|> quit <|> error
      reader |> Parsing.getFirst cmds

// --------------------------------------------------------------------------------------
// Main application command-line loop
// --------------------------------------------------------------------------------------

/// Represents current state
type internal State =
  {
    Files : Map<string,string[]>
    Project : Option<ProjectParser.ProjectResolver>
    OutputMode : OutputMode
    HelpText : Map<String, ToolTipText>
  }

/// Contains main loop of the application
module internal Main =
  open CommandInput

  let initialState = { Files = Map.empty; Project = None; OutputMode = Text; HelpText = Map.empty }

  // Main agent that handles IntelliSense requests
  let agent = new IntelliSenseAgent()

  let jsonConverters = [| new SeverityConverter() :> JsonConverter; new RangeConverter() :> JsonConverter |]

  let rec main (state:State) : int =

    let prAsJson o = Console.WriteLine (JsonConvert.SerializeObject(o, jsonConverters))

    let printMsg ty s =
      match state.OutputMode with
      | Text -> printfn "%s: %s\n<<EOF>>" ty s
      | Json -> prAsJson { Kind = ty; Data = s }

    let parsed file =
      let ok = Map.containsKey file state.Files
      if not ok then printMsg "ERROR" (sprintf "File '%s' not parsed" file)
      ok

    /// Is the specified position consistent with internal state of file?
    let posok file line col =
      let lines = state.Files.[file]
      let ok = line < lines.Length && line >= 0 &&
               col <= lines.[line].Length && col >= 0
      if not ok then printMsg "ERROR" "Position is out of range"
      ok

    let getoptions file =
      let text = String.concat "\n" state.Files.[file]
      RequestOptions(agent.GetCheckerOptions(file, text, state.Project),
                     file,
                     text)

    // Debug.print "main state is:\nproject: %b\nfiles: %A\nmode: %A"
    //             (Option.isSome state.Project)
    //             (Map.fold (fun ks k _ -> k::ks) [] state.Files)
    //             state.OutputMode
    match parseCommand(Console.ReadLine()) with
    | GetErrors ->
        let errs = agent.GetErrors()
        match state.OutputMode with
        | Text ->
          let sb = new System.Text.StringBuilder()
          sb.AppendLine("DATA: errors") |> ignore
          for e in errs do
            sb.AppendLine(sprintf "[%d:%d-%d:%d] %s %s" e.StartLine e.StartColumn e.EndLine e.EndColumn
                            (if e.Severity = Severity.Error then "ERROR" else "WARNING") e.Message)
            |> ignore
          sb.Append("<<EOF>>") |> ignore
          Console.WriteLine(sb.ToString())

        | Json -> prAsJson { Kind = "errors"; Data = errs }

        main state

    | OutputMode m -> main { state with OutputMode = m }

    | Parse(file,full) ->
        // Trigger parse request for a particular file
        let lines = readInput [] |> Array.ofList
        let text = String.concat "\n" lines
        let file = Path.GetFullPath file
        let opts = RequestOptions(agent.GetCheckerOptions(file, text, state.Project),
                                  file,
                                  text)
        agent.TriggerParseRequest(opts, full)
        printMsg "INFO" "Background parsing started"
        main { state with Files = Map.add file lines state.Files }

    | Project file ->
        // Load project file and store in state
        if File.Exists file then
          match ProjectParser.load file with
          | Some p -> 
              let files =
                [ for f in ProjectParser.getFiles p do
                    yield IO.Path.Combine(ProjectParser.getDirectory p, f) ]
              match state.OutputMode with
              | Text -> printfn "DATA: project\n%s\n<<EOF>>" (String.concat "\n" files)
              | Json -> prAsJson { Kind = "project"; Data = files }
              main { state with Project = Some p }
          | None   -> printMsg "ERROR" (sprintf "Project file '%s' is invalid" file)
                      main state
        else
          printMsg "ERROR" (sprintf "File '%s' does not exist" file)
          main state

    | Declarations file ->
        let file = Path.GetFullPath file
        if parsed file then
          let decls = agent.GetDeclarations(getoptions file)
          match state.OutputMode with
          | Text ->
              let declstrings =
                [ for tld in decls do
                    let m = tld.Declaration.Range
                    let (s1, e1), (s2, e2) =  ((m.StartColumn, m.StartLine), (m.EndColumn, m.EndLine))
                    yield sprintf "[%d:%d-%d:%d] %s" e1 s1 e2 s2 tld.Declaration.Name
                    for d in tld.Nested do
                      let m = d.Range
                      let (s1, e1), (s2, e2) = ((m.StartColumn, m.StartLine), (m.EndColumn, m.EndLine))
                      yield sprintf "  - [%d:%d-%d:%d] %s" e1 s1 e2 s2 d.Name ]
              printfn "DATA: declarations\n%s\n<<EOF>>" (String.concat "\n" declstrings)
          | Json -> prAsJson { Kind = "declarations"; Data = decls }
        main state

    | HelpText sym ->

        match Map.tryFind sym state.HelpText with
        | None -> ()
        | Some d ->
         
          let tip = TipFormatter.formatTip d
          let helptext = Map.add sym tip Map.empty
          prAsJson { Kind = "helptext"; Data = helptext }

        main state

    | PosCommand(cmd, file, line, col, timeout) ->
        let file = Path.GetFullPath file
        if parsed file && posok file line col then
          let opts = getoptions file

          match cmd with
          | Completion ->
              let decls = agent.DoCompletion(opts, line, col, state.Files.[file].[line], timeout)

              match decls with
              | Some (decls, residue) ->
                  match state.OutputMode with
                  | Text ->
                      printfn "DATA: completion"
                      for d in decls.Items do Console.WriteLine(d.Name)
                      printfn "<<EOF>>"
                      main state
                  | Json ->
                      
                      let ds = List.sortBy (fun (d: Declaration) -> d.Name)
                                 [ for d in decls.Items do yield d ]
                      match List.tryFind (fun (d: Declaration) -> d.Name.StartsWith residue) ds with
                      | None -> ()
                      | Some d -> let tip = TipFormatter.formatTip d.DescriptionText
                                  let helptext = Map.add d.Name tip Map.empty
                                  prAsJson { Kind = "helptext"; Data = helptext }
                                  
                      prAsJson { Kind = "completion"
                                 Data = [ for d in decls.Items do yield d.Name ] }

                      let helptext =
                        Seq.fold (fun m (d: Declaration) -> Map.add d.Name d.DescriptionText m) Map.empty decls.Items

                      main { state with HelpText = helptext }
              | None -> 
                  printMsg "ERROR" "Could not get type information"
                  main state

          | ToolTip ->
              let tipopt = agent.GetToolTip(opts, line, col, state.Files.[file].[line], timeout)

              match tipopt with
              | None -> printMsg "INFO" "No tooltip information"
              | Some tip ->
                match tip with
                | ToolTipText(elems) when elems |> List.forall (function
                  ToolTipElementNone -> true | _ -> false) ->
                  printMsg "INFO" "No tooltip information"
                | _ ->
                    match state.OutputMode with
                    | Text ->
                      Console.WriteLine("DATA: tooltip")
                      Console.WriteLine(TipFormatter.formatTip tip)
                      Console.WriteLine("<<EOF>>")
                    | Json -> prAsJson { Kind = "tooltip"; Data = TipFormatter.formatTip tip }

              main state
          
          | FindDeclaration ->

            match agent.FindDeclaration(opts, line, col, state.Files.[file].[line], timeout) with
            | None
            | Some (FindDeclResult.DeclNotFound _) -> printMsg "ERROR" "Could not find declaration"
            | Some (FindDeclResult.DeclFound range) ->
              
              match state.OutputMode with
              | Text -> printfn "DATA: finddecl\n%s:%d:%d\n<<EOF>>" range.FileName (range.StartLine-1) range.StartColumn
              | Json ->
                  let data = { Line = range.StartLine-1; Column = range.StartColumn; File = range.FileName }
                  prAsJson { Kind = "finddecl"; Data = data }
            
            main state

        else
          main state

    | CompilerLocation ->
        let locopt =
          FSharpEnvironment.BinFolderOfDefaultFSharpCompiler
            FSharpCompilerVersion.LatestKnown
        match locopt with
        | None -> printMsg "ERROR" "Could not find compiler"; main state
        | Some loc ->

        match state.OutputMode with
        | Text -> printfn "DATA: compilerlocation\n%s\n<<EOF>>" loc
        | Json -> prAsJson { Kind = "compilerlocation"; Data = loc }

        main state

    | Help ->
        match state.OutputMode with
        | Text -> Console.WriteLine(helpText)
        | Json -> prAsJson { Kind = "INFO"; Data = helptext }
        main state

    | Error(msg) ->
        printMsg "ERROR" msg
        main state

    | Quit ->
        (!Debug.output).Close ()
        0

  [<EntryPoint>]
  let entry args =
    let extra = Options.p.Parse args
    if extra.Count <> 0 then
      printfn "Unrecognised arguments: %s" (String.concat "," extra)
      1
    else
      try
        main initialState
      finally
        (!Debug.output).Close ()
