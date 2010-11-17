// --------------------------------------------------------------------------------------
// Compilation of projects - generates command line options for 
// the compiler and parses compiler error messages 
// --------------------------------------------------------------------------------------

namespace FSharp.MonoDevelop

open System
open System.IO
open System.Diagnostics
open System.CodeDom.Compiler
open System.Text.RegularExpressions

open MonoDevelop.Core
open MonoDevelop.Projects

open Microsoft.FSharp.Compiler.CodeDom

// --------------------------------------------------------------------------------------

/// Functions that implement compilation, parsing, etc..
module CompilerService = 
  let private regParseFsOutput = Regex(@"(?<file>[^\(]*)\((?<line>[0-9]*),(?<col>[0-9]*)\):\s(?<type>[^:]*)\s(?<err>[^:]*):\s(?<msg>.*)", RegexOptions.Compiled);
  let private regParseFsOutputNoNum = Regex(@"(?<file>[^\(]*)\((?<line>[0-9]*),(?<col>[0-9]*)\):\s(?<type>[^:]*)\s(?<msg>.*)", RegexOptions.Compiled);


  /// Generate various command line arguments for the project
  let private generateCmdArgs (config:DotNetProjectConfiguration) items configSel = seq {
    match config.CompileTarget with
    | CompileTarget.Library  -> yield "--target:library"
    | CompileTarget.Module   -> yield "--target:module"
    | CompileTarget.WinExe   -> yield "--target:winexe"
    | (*CompileTarget.Exe*)_ -> yield "--target:exe"
    
    if config.SignAssembly then yield "--keyfile:" + Common.wrapFile config.AssemblyKeyFile
    yield "--out:" + Common.wrapFile (config.CompiledOutputName.ToString())

    // Generate compiler options based on F# specific project settings
    let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters
    yield! Common.generateCompilerOptions fsconfig items configSel }


  /// Process a single message emitted by the F# compiler
  let private processMsg msg = 
    let m = 
      let t1 = regParseFsOutput.Match(msg) in
        if (t1.Success) then t1 else regParseFsOutputNoNum.Match(msg)
    if (m.Success) then 
      let errNo = (if (m.Groups.Item("err") <> null) then (m.Groups.Item("err")).Value else "") 
      let info = 
        m.Groups.Item("file").Value, Int32.Parse(m.Groups.Item("line").Value), 
          Int32.Parse(m.Groups.Item("col").Value), errNo, m.Groups.Item("msg").Value
      let isWarning = ((m.Groups.Item("type")).Value = "warning")
      (not isWarning), info
    else 
      true, ("unknown-file", 0, 0, "0", msg)


  /// Run the F# compiler with the specified arguments (passed as a list)
  /// and print the arguments to progress monitor (Output in MonoDevelop)
  let private compile (monitor:IProgressMonitor) argsList = 
    let args = String.concat "\n" argsList
    monitor.Log.WriteLine("{0} {1}", Common.fscPath, args)
    
    // Concatenate arguments & run
    let args = String.concat " " argsList
    let startInfo = 
      // If we're running "exe" file on Mono, then we need to run "mono fsc.exe"
      if Common.fscPath.EndsWith(".exe") && Environment.runningOnMono then
        Debug.tracef "Compiler" "Compile using: mono Arguments: %s" (Common.fscPath + " " + args)
        new ProcessStartInfo
          (FileName = "mono", UseShellExecute = false, Arguments = Common.fscPath + " " + args, 
           RedirectStandardError = true, CreateNoWindow = true) 
      else
        Debug.tracef "Compiler" "Compile using: %s Arguments: %s" Common.fscPath args
        new ProcessStartInfo
          (FileName = Common.fscPath, UseShellExecute = false, Arguments = args, 
           RedirectStandardError = true, CreateNoWindow = true) 
    let p = Process.Start(startInfo) 
    
    // Read all output and fold multi-line 
    let lines = 
      [ let line = ref ""
        while (line := p.StandardError.ReadLine(); !line <> null) do
          yield !line 
        yield "" ]    
    let messages = 
      lines 
        |> Seq.fold (fun (current, all) line -> 
          if line = "" then [], (List.rev current)::all 
          else line::current, all) ([], []) 
        |> snd |> List.rev
        |> List.map (String.concat " ")
        |> List.filter (fun s -> s.Trim().Length > 0)
        
    // Parse messages and build results        
    let br = new BuildResult()
    for msg in messages do
      match processMsg msg with
      | true, (f, l, c, n, m) -> br.AddError(f, l, c, n, m)
      | false, (f, l, c, n, m) -> br.AddWarning(f, l, c, n, m)
    
    p.WaitForExit()
    br.CompilerOutput <- String.concat "\n" messages
    br
  
  // ------------------------------------------------------------------------------------
  
  /// Compiles the specified F# project using the current configuration
  /// and prints command line command to the MonoDevelop output
  let Compile(items, config:DotNetProjectConfiguration, configSel, monitor) : BuildResult =
    [ yield! [ "--noframework --nologo" ]
      yield! generateCmdArgs config items configSel  
      yield! Common.generateReferences items configSel true
      yield! Common.generateOtherItems items 
      
      // Generate source files (sort using current configuration)
      let fsconfig = config.ProjectParameters :?> FSharpProjectParameters
      let files = Common.getSourceFiles items
      let root = System.IO.Path.GetDirectoryName(config.ProjectParameters.ParentProject.FileName.FullPath.ToString())
      yield! Common.getItemsInOrder root files fsconfig.BuildOrder false ]
    |> compile monitor
    
    // CSharpCompilerParameters compilerParameters = (CSharpCompilerParameters)configuration.CompilationParameters ?? new CSharpCompilerParameters ();
