// --------------------------------------------------------------------------------------
// (c) Tomas Petricek, http://tomasp.net/blog
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open System
open System.IO
open System.Collections.Generic

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.InteractiveAutocomplete.Parsing
open FSharp.CompilerBinding.Reflection

open Newtonsoft.Json

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
type internal RequestOptions(opts, file, src, mode) =
  member x.Options : CheckOptions = opts
  member x.FileName : string = file
  member x.Source : string = src
  member x.OutputMode : OutputMode = mode
  member x.WithSource(source) =
    RequestOptions(opts, file, source, mode)

  override x.ToString() =
    sprintf "FileName: '%s'\nSource length: '%d'\nOptions: %s, %A, %A, %b, %b"
      x.FileName x.Source.Length x.Options.ProjectFileName x.Options.ProjectFileNames
      x.Options.ProjectOptions x.Options.IsIncompleteTypeCheckEnvironment
      x.Options.UseScriptResolutionRules


/// Message type that is used by 'IntelliSenseAgent'
type internal IntelliSenseAgentMessage =
  | TriggerParseRequest of RequestOptions * bool
  | GetTypeCheckInfo of RequestOptions * int option * AsyncReplyChannel<TypeCheckInfo option>
  | GetErrors of AsyncReplyChannel<ErrorInfo[]>
  | GetDeclarationsMessage of RequestOptions * AsyncReplyChannel<TopLevelDeclaration[]>

/// Used to marshal completion candidates
/// before serializing to JSON
type Candidate =
  {
    Name: string
    Help: string
  }

/// Provides an easy access to F# IntelliSense service
type internal IntelliSenseAgent() =

  /// Create an F# IntelliSense service
  let checker = InteractiveChecker.Create(ignore)

  /// Creates an empty "Identifier" token (we need it when getting ToolTip)
  let identToken = FsParser.tagOfToken(FsParser.token.IDENT(""))

  /// Calls F# IntelliSense service repeatedly in an (asynchronous) loop
  /// until the type check request succeeds
  let rec waitForTypeCheck(opts:RequestOptions, untypedInfo) = async {
    let info =
      checker.TypeCheckSource
        ( untypedInfo, opts.FileName, identToken, opts.Source,
          opts.Options, IsResultObsolete(fun () -> false) )
    match info with
    | TypeCheckSucceeded(res) when res.TypeCheckInfo.IsSome ->
        return res.TypeCheckInfo.Value, res.Errors
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
          let untypedInfo = checker.UntypedParse(opts.FileName, opts.Source, opts.Options)
          let res =
            checker.TypeCheckSource
              ( untypedInfo, opts.FileName, 0, opts.Source,
                opts.Options, IsResultObsolete(fun () -> false))
          let errors =
            match res with
            | TypeCheckSucceeded(res) -> res.Errors
            | _ -> errors
          // Start full background parsing if requested..
          if full then checker.StartBackgroundCompile(opts.Options)
          return! loop errors

      | GetDeclarationsMessage(opts, repl) ->
          let untypedInfo = checker.UntypedParse(opts.FileName, opts.Source, opts.Options)
          repl.Reply(untypedInfo.GetNavigationItems().Declarations)
          return! loop errors

      | GetTypeCheckInfo(opts, timeout, reply) ->
          // Try to get information for the IntelliSense (in the specified time)
          let untypedInfo = checker.UntypedParse(opts.FileName, opts.Source, opts.Options)
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
        checker.GetCheckOptionsFromScriptRoot(fileName, source, System.DateTime.Now)

          // The InteractiveChecker resolution doesn't sometimes
          // include FSharp.Core and other essential assemblies, so we may
          // need to bring over some more code from the monodevelop binding to
          // handle that situation.


      // We are in a project - construct options using current properties
      | Some proj, _ ->
        let projFile = ProjectParser.getFileName proj
        let files = ProjectParser.getFiles proj
        let args = ProjectParser.getOptions proj

        CheckOptions.Create(projFile, files, args, false, false, ProjectParser.getLoadTime proj)

    // Print contents of check option for debugging purposes
    Debug.print "Checkoptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A"
                         opts.ProjectFileName
                         opts.ProjectFileNames
                         opts.ProjectOptions
                         opts.IsIncompleteTypeCheckEnvironment
                         opts.UseScriptResolutionRules
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
  member x.GetTypeCheckInfo(opts : RequestOptions, time) : Option<TypeCheckInfo> =
    // First check if cached results are available
    let checkres = checker.TryGetRecentTypeCheckResultsForFile(opts.FileName, opts.Options)
    match checkres with
    | Some(untyped, typed, _) when typed.TypeCheckInfo.IsSome ->
      Debug.print "Worker: Quick parse completed - success"
      typed.TypeCheckInfo
    | _ ->
      // Otherwise try to get type information & run the request
      agent.PostAndReply(fun r -> GetTypeCheckInfo(opts, time, r))

  /// Invokes dot-completion request and writes information to the standard output
  member x.DoCompletion(opts : RequestOptions, ((line, column) as pos), lineStr, time) =
    let info = x.GetTypeCheckInfo(opts, time)
    let decls =
      Option.bind (fun (info: TypeCheckInfo) ->
        // Get the long identifier before the current location
        // 'residue' is the part after the last dot and 'longName' is before
        // e.g.  System.Console.Wri  --> "Wri", [ "System"; "Console"; ]
        let lookBack = Parsing.createBackStringReader lineStr (column - 1)
        let residue, longName =
          lookBack |> Parsing.getFirst Parsing.parseBackIdentWithResidue

        // Get items & generate output
        try
          Some (info.GetDeclarations(pos, lineStr, (longName, residue), 0, defaultArg time 1000))
        with :? System.TimeoutException as e -> None) info
                   
    match decls with
    | Some decls ->
      printfn "DATA: completion"
      match opts.OutputMode with
      | Json ->
        let cs =
          [ for d in decls.Items do
            yield { Name = d.Name
                    Help = TipFormatter.formatTip d.DescriptionText } ]
        Console.WriteLine(JsonConvert.SerializeObject(cs))
      | Text ->
        for d in decls.Items do Console.WriteLine(d.Name)
      printfn "<<EOF>>"
    | None -> printfn "ERROR: Could not get type information\n<<EOF>>"


  /// Gets ToolTip for the specified location (and prints it to the output)
  member x.GetToolTip(opts, ((line, column) as pos), lineStr, time) =
    match x.GetTypeCheckInfo(opts, time) with
    | Some(info) ->
      // Parsing - find the identifier around the current location
      // (we look for full identifier in the backward direction, but only
      // for a short identifier forward - this means that when you hover
      // 'B' in 'A.B.C', you will get intellisense for 'A.B' module)
      let lookBack = Parsing.createBackStringReader lineStr column
      let lookForw = Parsing.createForwardStringReader lineStr (column + 1)
      let backIdent = Parsing.getFirst Parsing.parseBackLongIdent lookBack
      let nextIdent = Parsing.getFirst Parsing.parseIdent lookForw

      let identIsland =
        match List.rev backIdent with
        | last::prev -> (last + nextIdent)::prev |> List.rev
        | [] -> []

      match identIsland with
      | [ "" ] ->
        // There is no identifier at the current location
        printfn "INFO: No identifier found at this location\n<<EOF>>"
      | _ ->
        // Assume that we are inside identifier (F# services can also handle
        // case when we're in a string in '#r "Foo.dll"' but we don't do that)
        let tip = info.GetDataTipText(pos, lineStr, identIsland, identToken)
        match tip with
        | DataTipText(elems)
          when elems |> List.forall (function
            DataTipElementNone -> true | _ -> false) ->
              printfn "INFO: No tooltip information\n<<EOF>>"
        | _ ->
          Console.WriteLine("DATA: tooltip")
          Console.WriteLine(TipFormatter.formatTip tip)
          Console.WriteLine("<<EOF>>")
    | None -> printfn "ERROR: Could not get type information\n<<EOF>>"

  /// Finds the point of declaration of the symbol at pos
  /// and writes information to the standard output
  member x.FindDeclaration(opts : RequestOptions, ((line, column) as pos), lineStr, time) =
    match x.GetTypeCheckInfo(opts, time) with
    | Some(info) ->
      // Parsing - find the identifier around the current location
      // (we look for full identifier in the backward direction, but only
      // for a short identifier forward - this means that when you hover
      // 'B' in 'A.B.C', you will get intellisense for 'A.B' module)
      let lookBack = Parsing.createBackStringReader lineStr column
      let lookForw = Parsing.createForwardStringReader lineStr (column + 1)
      let backIdent = Parsing.getFirst Parsing.parseBackLongIdent lookBack
      let nextIdent = Parsing.getFirst Parsing.parseIdent lookForw

      let identIsland =
        match List.rev backIdent with
        | last::prev -> (last + nextIdent)::prev |> List.rev
        | [] -> []

      match identIsland with
      | [ "" ] ->
        // There is no identifier at the current location
        printfn "ERROR: No identifier found at this location\n<<EOF>>"
      | _ ->
        // Assume that we are inside identifier (F# services can also handle
        // case when we're in a string in '#r "Foo.dll"' but we don't do that)
        // Get items & generate output
        // TODO: Need this first because of VS debug info coming out
        Console.WriteLine("DATA: finddecl")
        match info.GetDeclarationLocation(pos, lineStr, identIsland, identToken, true) with
        | DeclFound (line,col,file) ->
            printfn "%s:%d:%d\n<<EOF>>" file line col
        | DeclNotFound -> printfn "ERROR: Could not find point of declaration\n<<EOF>>"
    | None -> printfn "ERROR: Could not get type information\n<<EOF>>"

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
    tooltip ""<filename>"" <line> <col> [timeout]
      - get tool tip for the specified location
    finddecl ""<filename>"" <line> <col> [timeout]
      - find the point of declaration of the object at specified position
    project ""<filename>""
      - associates the current session with the specified project
    outputmode {json,text}
      - switches the output format. json offers richer data
        for some commands"

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
    | PosCommand of PosCommand * string * Position * int option
    | Declarations of string
    | GetErrors
    | Parse of string * bool
    | Error of string
    | Project of string
    | OutputMode of OutputMode
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
    return PosCommand(f, filename, (line, col), timeout) }

  // Parses always and returns default error message
  let error = parser {
    return Error("ERROR: Unknown command or wrong arguments\n<<EOF>>") }

  // Parase any of the supported commands
  let parseCommand =
    function
    | null -> Quit
    | input ->
      let reader = Parsing.createForwardStringReader input 0
      let cmds = errors <|> help <|> declarations <|> parse <|> project <|> completionTipOrDecl <|> outputmode <|> quit <|> error
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
  }

/// Contains main loop of the application
module internal Main =
  open CommandInput

  let initialState = { Files = Map.empty; Project = None; OutputMode = Text }

  // Main agent that handles IntelliSense requests
  let agent = new IntelliSenseAgent()

  let rec main (state:State) : int =
    let parsed file =
      let ok = Map.containsKey file state.Files
      if not ok then printfn "ERROR: File '%s' not parsed\n<<EOF>>\n" file
      ok

    /// Is the specified position consistent with internal state of file?
    let posok file line col =
      let lines = state.Files.[file]
      let ok = line < lines.Length && line >= 0 &&
               col <= lines.[line].Length && col >= 0
      if not ok then Console.WriteLine("ERROR: Position is out of range\n<<EOF>>")
      ok

    Debug.print "main state is:\nproject: %b\nfiles: %A\nmode: %A"
                (Option.isSome state.Project)
                (Map.fold (fun ks k _ -> k::ks) [] state.Files)
                state.OutputMode
    match parseCommand(Console.ReadLine()) with
    | GetErrors ->
        let errs = agent.GetErrors()
        printfn "DATA: errors"
        for e in errs do
          printfn "[%d:%d-%d:%d] %s %s" e.StartLine e.StartColumn e.EndLine e.EndColumn
                    (if e.Severity = Severity.Error then "ERROR" else "WARNING") e.Message
        Console.WriteLine("<<EOF>>")
        main state

    | OutputMode m ->
        main { state with OutputMode = m }

    | Parse(file,full) ->
        // Trigger parse request for a particular file
        let lines = readInput [] |> Array.ofList
        let text = String.concat "\n" lines
        let file = Path.GetFullPath file
        if File.Exists file then
          let opts = RequestOptions(agent.GetCheckerOptions(file, text, state.Project),
                                    file,
                                    text,
                                    state.OutputMode)
          agent.TriggerParseRequest(opts, full)
          Console.WriteLine("INFO: Background parsing started\n<<EOF>>")
          main { state with Files = Map.add file lines state.Files }
        else
          printfn "ERROR: File '%s' does not exist\n<<EOF>>" file
          main state

    | Project file ->
        // Load project file and store in state
        if File.Exists file then
          match ProjectParser.load file with
          | Some p -> Console.WriteLine("DATA: project")
                      for f in ProjectParser.getFiles p do
                        Console.WriteLine(IO.Path.Combine(ProjectParser.getDirectory p, f))
                      Console.WriteLine("<<EOF>>")
                      main { state with Project = Some p }
          | None   -> printfn "ERROR: Project file '%s' is invalid\n<<EOF>>" file
                      main state
        else
          printfn "ERROR: File '%s' does not exist\n<<EOF>>" file
          main state

    | Declarations file ->
        let file = Path.GetFullPath file
        if parsed file then
          let text = String.concat "\n" state.Files.[file]
          let opts = RequestOptions(agent.GetCheckerOptions(file, text, state.Project),
                                    file,
                                    text,
                                    state.OutputMode)
          let decls = agent.GetDeclarations(opts)
          printfn "DATA: declarations"
          for tld in decls do
            let (s1, e1), (s2, e2) = tld.Declaration.Range
            printfn "[%d:%d-%d:%d] %s" e1 s1 e2 s2 tld.Declaration.Name
            for d in tld.Nested do
              let (s1, e1), (s2, e2) = d.Range
              printfn "  - [%d:%d-%d:%d] %s" e1 s1 e2 s2 d.Name
          printfn "<<EOF>>"
        main state

    | PosCommand(cmd, file, ((line, col) as pos), timeout) ->
        let file = Path.GetFullPath file
        if parsed file && posok file line col then
          let text = String.concat "\n" state.Files.[file]
          let opts = RequestOptions(agent.GetCheckerOptions(file, text, state.Project),
                                    file,
                                    text,
                                    state.OutputMode)

          match cmd with
          | Completion -> agent.DoCompletion(opts, pos, state.Files.[file].[line], timeout)
          | ToolTip -> agent.GetToolTip(opts, pos, state.Files.[file].[line], timeout)
          | FindDeclaration -> agent.FindDeclaration(opts, pos, state.Files.[file].[line], timeout)
        main state

    | Help ->
        Console.WriteLine(helpText)
        main state

    | Error(msg) ->
        Console.WriteLine(msg)
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
