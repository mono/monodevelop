// --------------------------------------------------------------------------------------
// Common utilities for environment, debugging and working with project files
// --------------------------------------------------------------------------------------

namespace FSharp.MonoDevelop

open System
open System.IO
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui
open Mono.Addins

module Environment = 
  /// Are we running on the Mono platform?
  let runningOnMono = 
    try System.Type.GetType("Mono.Runtime") <> null
    with _ -> false        


module Debug = 
  open Microsoft.FSharp.Compiler.Reflection // (?) operator

#if LOGGING
  /// If trace is enabled, we print more information
  let traceEnabled = 
    [ "Exception", (true, ConsoleColor.Red)
      "Errors", (false, ConsoleColor.DarkCyan)
      "Parsing", (false, ConsoleColor.Blue)
      "Worker", (false, ConsoleColor.Green)
      "LanguageService", (false, ConsoleColor.DarkGreen) 
      "Gui", (false, ConsoleColor.DarkYellow)
      "Result", (false, ConsoleColor.Magenta)
      "Interactive", (false, ConsoleColor.Gray)
      "Checkoptions", (true, ConsoleColor.DarkGray)
      "Resolution", (true, ConsoleColor.Gray)
      "Compiler", (false, ConsoleColor.DarkRed)
      "Config", (false, ConsoleColor.DarkMagenta)
    ] |> Map.ofSeq
#else
  /// If trace is enabled, we print more information
  let traceEnabled = 
    [ "Exception", (true, ConsoleColor.Red)
      "Errors", (false, ConsoleColor.DarkCyan)
      "Parsing", (false, ConsoleColor.Blue)
      "Worker", (false, ConsoleColor.Green)
      "LanguageService", (false, ConsoleColor.DarkGreen) 
      "Gui", (false, ConsoleColor.DarkYellow)
      "Result", (false, ConsoleColor.Magenta)
      "Interactive", (false, ConsoleColor.Gray)
      "Checkoptions", (false, ConsoleColor.DarkGray)
      "Resolution", (false, ConsoleColor.Gray)
      "Compiler", (false, ConsoleColor.DarkRed)
      "Config", (false, ConsoleColor.DarkMagenta)
    ] |> Map.ofSeq
#endif

  /// Prints debug information - to debug output (on windows, because this is
  /// easy to see in VS) or to console (on Mono, because this prints to terminal) 
  let print (s:string) clr = 
    if Environment.runningOnMono then 
      let orig = Console.ForegroundColor
      Console.ForegroundColor <- clr
      Console.WriteLine(s)
      Console.ForegroundColor <- orig      
    else System.Diagnostics.Debug.WriteLine(s)

  /// Prints debug information - to debug output or to console 
  /// Prints only when the specified category is enabled
  let tracef category fmt = 
    fmt |> Printf.kprintf (fun s -> 
      let enabled, clr = traceEnabled.[category] 
      if enabled then 
        print ("[F#] [" + category + "] " + s) clr )

  /// Debug assert - displays a dialog with an error message
  let assertf fmt = 
    fmt |> Printf.kprintf (fun s -> 
      System.Diagnostics.Debug.Assert(false, s) )

  /// Prints detailed information about exception
  let tracee category e = 
    let rec printe s (e:exn) = 
      let name = e.GetType().FullName
      tracef "Exception" "[%s] %s: %s (%s)\n\nStack trace: %s\n\n" category s name e.Message e.StackTrace
      if name = "Microsoft.FSharp.Compiler.ErrorLogger+Error" then
        let (tup:obj) = e?Data0 
        tracef "Exception" "[%s] Compile error (%d): %s" category tup?Item1 tup?Item2
      elif name = "Microsoft.FSharp.Compiler.ErrorLogger+InternalError" then
        tracef "Exception" "[%s] Internal Error message: %s" category e?Data0
      elif name = "Microsoft.FSharp.Compiler.ErrorLogger+ReportedError" then
        let (inner:obj) = e?Data0 
        if inner = null then tracef category "Reported error is null"
        else printe "Reported error" (inner?Value)
      elif e.InnerException <> null then
        printe "Inner exception" e.InnerException
        
    printe "Exception" e
    
// --------------------------------------------------------------------------------------
// Assembly resolution in a script file - a workaround that replaces functionality
// from 'GetCheckOptionsFromScriptRoot' (which doesn't work well on Mono)
// --------------------------------------------------------------------------------------

open System.Runtime.InteropServices
open Microsoft.FSharp.Compiler.SourceCodeServices

module ScriptOptions =

  // Print debug information about compiler path
  match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler with
  | Some(path) -> Debug.tracef "Resolution" "Default compiler path: '%s'" path
  | None -> Debug.tracef "Resolution" "Default compiler path: (unknown)" 

  /// Make path absolute using the specified 'root' path if it is not already
  let makeAbsolute root (path:string) = 
    let path = path.Replace("\"","")
    if Path.IsPathRooted(path) then path
    else Path.Combine(root, path)
  
  /// Returns default directories to be used when searching for DLL files
  let getDefaultDirectories root includes =   
    // Return all known directories
    [ let runtime = RuntimeEnvironment.GetRuntimeDirectory() 
      yield if not (runtime.EndsWith("1.0", StringComparison.Ordinal)) then runtime
            else Path.Combine(Path.GetDirectoryName runtime, "4.0") // Mono 
      match root with 
      | Some(root) -> yield! includes |> List.map (makeAbsolute root)
      | None -> yield! includes |> List.filter Path.IsPathRooted
      yield! root |> Option.toList                  
      yield! FSharpEnvironment.BinFolderOfDefaultFSharpCompiler |> Option.toList ]
                
  /// Returns true if the specified file exists (and never throws exception)
  let safeExists f = 
    try File.Exists(f) with _ -> false
    
  /// Resolve assembly in the specified list of directories
  let rec resolveAssembly dirs asm =
    match dirs with 
    | dir::dirs ->
        let asmPath = Path.Combine(dir, asm)
        let any = List.tryFind safeExists [ asm; asm + ".dll"; asmPath; asmPath + ".dll" ]
        match any with 
        | Some(file) -> Some(file)
        | _ -> resolveAssembly dirs asm
    | [] -> None
  
#if CUSTOM_SCRIPT_RESOLUTION
  /// Returns assemblies to be used as "-r" directives when 
  /// working in a standalone script file                
  let private getDefaultScriptReferences root includes referenced = 
    let syscore = "System.Core, Version=3.5.0.0, Culture=neutral, " +
                  "PublicKeyToken=b77a5c561934e089"
                  
    // Explicitly defined standard assemblies                  
    let net35 = try System.Reflection.Assembly.Load syscore |> ignore; true with _ -> false
    let assemblies = 
      [ "mscorlib"; "FSharp.Core"; "FSharp.Compiler.Interactive.Settings";
        "System"; "System.Xml"; "System.Runtime.Remoting"; "System.Data"; 
        "System.Runtime.Serialization.Formatters.Soap"; "System.Drawing"; "System.Web"; 
        "System.Web.Services"; "System.Windows.Forms" ] @
      if net35 then [ "System.Core" ] else [] 

    let dirs = getDefaultDirectories (Some root) includes
    
    // Resolve all default and explicitly referenced assemblies and return them
    [ for asm in assemblies @ referenced do
        let res = resolveAssembly dirs asm 
        if res = None then
          Debug.tracef "Resolution" "Could not resolve '%s' in any of %A." asm dirs
        yield! res |> Option.toList ]

  // ------------------------------------------------------------------------------------
  // A simple parser that loads script file and looks for preprocessor directives
  // (we need this, because assembly resolution in language service doesnt work or Mono)
  // ------------------------------------------------------------------------------------

  /// Extracts text specified by token location from a line   
  let (|TokenText|) (line:string) (tok:TokenInformation) = 
    line.Substring(tok.LeftColumn, tok.RightColumn - tok.LeftColumn + 1)

  /// Parse file and return list of "#nowarn", "#I", "#r" and "#load" directives
  let parseFile fileName (source:string) = 
    let source = source.Replace("\r\n", "\n").Replace("\r", "\n")
    let lines = source.Split [| '\n' |]
    let sourceTok = new SourceTokenizer([ "INTERACTIVE" ], fileName)
    
    // A function that takes a line and returns a function for reading tokens
    let lineTokenizer = 
      let state = ref 0L
      (fun line ->
          let tokenizer = sourceTok.CreateLineTokenizer(line)
          tokenizer.StartNewLine()
          (fun () -> 
              let (v, nstate) = tokenizer.ScanToken(!state) 
              state := nstate
              v ) )

    let references = new ResizeArray<_>()
    let loads = new ResizeArray<_>()
    let nowarns = new ResizeArray<_>()
    let includes = new ResizeArray<_>() 
    
    // Iterate over all lines and process each line separately              
    for line in lines do 
      let nextToken = lineTokenizer line
      
      /// Represents a state where we're reading arguments of # directive
      let rec readHashArgument hash acc = 
        match nextToken() with 
        | Some(tok & TokenText line str) when tok.TokenName = "STRING_TEXT" ->
            readHashArgument hash (str::acc)
        | _ ->
            let string = List.rev acc |> String.concat ""
            let string = 
              if string.StartsWith("@") then string.Substring(2).Replace("\"\"", "\"")
              else string.Substring(1).Replace("\\\\", "\\") // not quite complete
            match hash with 
            | "#r" -> references.Add(string)
            | "#nowarn" -> nowarns.Add(string)
            | "#load" -> loads.Add(string)
            | "#I" -> includes.Add(string)
            | _ -> () 
      
      /// Represents a state where we're skipping whitespace
      let rec skipWhiteSpaceAfterHash(hash) =
        match nextToken() with 
        | Some(tok) when tok.TokenName = "WHITESPACE" -> 
            skipWhiteSpaceAfterHash(hash)
        | Some(tok & TokenText line str) when tok.TokenName = "STRING_TEXT" ->
            readHashArgument hash [str]
        | _ -> ()
        
      /// Represents an initial state - just waiting for the first token
      let rec parseLine() = 
        match nextToken() with
        | Some(tok & TokenText line str) when tok.TokenName = "HASH" ->
            skipWhiteSpaceAfterHash(str)
        | Some(tok) -> parseLine()
        | None -> ()
      parseLine() 

    references |> List.ofSeq, nowarns |> List.ofSeq,
      loads |> List.ofSeq, includes |> List.ofSeq
      
  // ------------------------------------------------------------------------------------
  /// Retrusn compiler options to be used by language service 
  /// when working with a standalone F# script file
  let getScriptOptionsForFile(fileName, source) = 
    let references, nowarns, loads, includes = parseFile fileName source
    let root = Path.GetDirectoryName(fileName)

    let references = getDefaultScriptReferences root includes references
    let references = references |> List.map ((+) "-r:") 
    let nowarns = nowarns |> List.map ((+) "--nowarn:")
    let loads = loads |> List.map (makeAbsolute root)
    
    let otherFlags = ["--noframework"; "--warn:3"] 
    let args = otherFlags @ nowarns @ references |> Array.ofSeq
    let fileNames = loads @ [ fileName ] |> Array.ofSeq
    CheckOptions.Create(fileName + ".fsproj", fileNames, args, false, true) 
#endif

// --------------------------------------------------------------------------------------
// Common utilities for working with files & extracting information from 
// MonoDevelop objects (e.g. references, project items etc.)
// --------------------------------------------------------------------------------------

module Common = 
  /// Wraps the given string between double quotes
  let wrapFile s = "\"" + s + "\""  

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
  
  /// Is the specified extension supported F# file?
  let supportedExtension ext = 
    [".fsscript"; ".fs"; ".fsx"; ".fsi"] |> Seq.exists (fun sup ->
        String.Compare(ext, sup, true) = 0)

  /// Is the specified extension used by F# script files?
  let fsharpScriptExtension ext = 
    [".fsscript"; ".fsx"] |> Seq.exists (fun sup ->
        String.Compare(ext, sup, true) = 0)

  /// Generates references for the current project & configuration as a 
  /// list of strings of the form [ "-r:<full-path>"; ... ]
  let generateReferences (items:ProjectItemCollection) configSelector shouldWrap = seq { 
    let wrapf = if shouldWrap then wrapFile else id
    let files = 
      [ for ref in items.GetAll<ProjectReference>() do
          for file in ref.GetReferencedFileNames(configSelector) do
            yield file ]
    
    // If 'FSharp.Core.dll' is not in the set of references, we need to 
    // resolve it and add it (this can be removed when assembly resolution in the
    // langauge service is fixed on Mono, because LS will try to do this)
    let coreRef = files |> List.exists (fun fn -> fn.EndsWith("FSharp.Core.dll") )
    if not coreRef then
      let dirs = ScriptOptions.getDefaultDirectories None []
      match ScriptOptions.resolveAssembly dirs "FSharp.Core" with
      | Some fn -> yield "-r:" + wrapf(fn)
      | None -> Debug.tracef "Resolution" "FSharp.Core assembly resolution failed!"
      
    for file in files do 
      yield "-r:" + wrapf(file) }


  /// Generates command line options for the compiler specified by the 
  /// F# compiler options (debugging, tail-calls etc.), custom command line
  /// parameters and assemblies referenced by the project ("-r" options)
  let generateCompilerOptions (fsconfig:FSharpCompilerParameters) items config =
    let dashr = generateReferences items config false |> Array.ofSeq
    let defines = fsconfig.DefinedSymbols.Split([| ';'; ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    [| yield "--noframework"
       for symbol in defines do yield "--define:" + symbol
       if fsconfig.GenerateDebugInfo then 
         yield "--debug+" 
         yield "--define:DEBUG" 
       else 
         yield "--debug-"
       yield if fsconfig.OptimizeCode then "--optimize+" else "--optimize-"
       yield if fsconfig.GenerateTailCalls then "--tailcalls+" else "--tailcalls-"
       // TODO: This currently ignores escaping using "..."
       for arg in fsconfig.CustomCommandLine.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) do
         yield arg 
       yield! dashr |] 
  

  /// Get source files of the current project (returns files that have 
  /// build action set to 'Compile', but not e.g. scripts or resources)
  let getSourceFiles (items:ProjectItemCollection) = seq {
    for file in items.GetAll<ProjectFile>() do
      if file.BuildAction = "Compile" && file.Subtype <> Subtype.Directory then 
        yield file.Name.ToString() }

  /// Creates a relative path from one file or folder to another. 
  let makeRelativePath (root:string) (file:string) = 
    let file = Uri(file)
    let sep = Path.DirectorySeparatorChar.ToString()
    let root = Uri(if root.EndsWith(sep) then root else root + sep + "dummy" )
    root.MakeRelativeUri(file).ToString().Replace("/", sep)

  /// Create a list containing same items as the 'items' parameter that preserves
  /// the order specified by 'ordered' (and new items are at the end)  
  let getItemsInOrder root items ordered relative =
    let ordered = seq { for f in ordered -> Path.Combine(root, f) }
    let itemsSet, orderedSet = set items, set ordered
    let keep = Set.intersect orderedSet itemsSet
    let ordered = ordered |> Seq.filter (fun el -> Set.contains el keep)
    let procf = if relative then makeRelativePath root else id
    seq { for f in ordered do yield procf f
          for f in itemsSet - orderedSet do yield procf f }
    
  /// Generate inputs for the compiler (excluding source code!); returns list of items 
  /// containing resources (prefixed with the --resource parameter)
  let generateOtherItems (items:ProjectItemCollection) = seq {
    for file in items.GetAll<ProjectFile>() do
      match file.BuildAction with
      | _ when file.Subtype = Subtype.Directory -> ()
      | "EmbeddedResource" -> yield "--resource:" + (wrapFile(file.Name.ToString()))
      | "None" | "Content" | "Compile" -> ()
      | s -> failwith("Items of type '" + s + "' not supported") }


  /// If we cannto find the F# compiler, we use just "fsc.exe" on 
  /// .NET or we use "fsharpc" command on Mono (installed by the package)
  let fscPath = 
    match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler with
    | _ when ScriptOptions.safeExists(AddinManager.CurrentAddin.GetFilePath ("fsc.exe")) ->
        // Use the fsi bundled with the add-in
        AddinManager.CurrentAddin.GetFilePath ("fsc.exe")
    | _ when Environment.runningOnMono && ((ScriptOptions.safeExists "/usr/bin/fsharpc") || (ScriptOptions.safeExists "/usr/local/bin/fsharpc")) ->
        // On Mono, we always prefer 'fsharpc' script, especially if we can find it
        "fsharpc"
    | Some(dir) when File.Exists(Path.Combine(dir, "fsc.exe")) ->  
        // If we can find 'fsc.exe' then that is good no matter where we're running
        Path.Combine(dir, "fsc.exe")
        // Fallback case - depends on the platform
    | _ -> if Environment.runningOnMono then "fsharpc" else "fsc.exe"

  /// If we cannto find F# Interactive, we use just "fsi.exe" on 
  /// .NET or we use "fsharpi" command on Mono (installed by the package)
  let fsiPath = 
    match FSharpEnvironment.BinFolderOfDefaultFSharpCompiler with
    | _ when ScriptOptions.safeExists(AddinManager.CurrentAddin.GetFilePath ("fsi.exe")) ->
        // Use the fsi bundled with the add-in
        AddinManager.CurrentAddin.GetFilePath ("fsi.exe")
    | _ when Environment.runningOnMono && ScriptOptions.safeExists("/usr/bin/fsharpi") ->
        // On Mono, we always prefer 'fsharpi' script, especially if we can find it
        "fsharpi"
    | Some(dir) when ScriptOptions.safeExists(Path.Combine(dir, "fsi.exe")) ->  
        // If we can find 'fsi.exe' then that is good no matter where we're running
        Path.Combine(dir, "fsi.exe")
        // Fallback case - depends on the platform
    | _ -> if Environment.runningOnMono then "fsharpi" else "fsi.exe"

  do Debug.tracef "Resolution" "Paths:\n - fsc = %s\n - fsi = %s" fscPath fsiPath
