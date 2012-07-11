// --------------------------------------------------------------------------------------
// Compilation of projects - generates command line options for 
// the compiler and parses compiler error messages 
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open System.IO
open System.Diagnostics
open System.CodeDom.Compiler
open System.Text.RegularExpressions

open MonoDevelop.Core
open MonoDevelop.Core.Assemblies
open MonoDevelop.Projects
open MonoDevelop.Ide

open Microsoft.FSharp.Compiler.CodeDom

// --------------------------------------------------------------------------------------

/// Functions that implement compilation, parsing, etc..
module CompilerService = 
  let private regParseFsOutput = Regex(@"(?<file>[^\(]*)\((?<line>[0-9]*),(?<col>[0-9]*)\):\s(?<type>[^:]*)\s(?<err>[^:]*):\s(?<msg>.*)", RegexOptions.Compiled);
  let private regParseFsOutputNoNum = Regex(@"(?<file>[^\(]*)\((?<line>[0-9]*),(?<col>[0-9]*)\):\s(?<type>[^:]*)\s(?<msg>.*)", RegexOptions.Compiled);


  /// Generate various command line arguments for the project
  let private generateCmdArgs (config:DotNetProjectConfiguration) items configSel = 
    [ match config.CompileTarget with
      | CompileTarget.Library  -> yield "--target:library"
      | CompileTarget.Module   -> yield "--target:module"
      | CompileTarget.WinExe   -> yield "--target:winexe"
      | (*CompileTarget.Exe*)_ -> yield "--target:exe"
    
      if config.SignAssembly then yield "--keyfile:" + Common.wrapFile config.AssemblyKeyFile
      yield "--out:" + Common.wrapFile (config.CompiledOutputName.ToString())
    
      // Generate compiler options based on F# specific project settings
      let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters
    
      if not (String.IsNullOrEmpty fsconfig.DocumentationFile) then 
          yield ("--doc:" + Common.wrapFile fsconfig.DocumentationFile)

      let shouldWrap = true// The compiler argument paths should always be wrapped, since some paths (ie. on Windows) may contain spaces.
      yield! Common.generateCompilerOptions fsconfig items configSel shouldWrap ]


  /// Process a single message emitted by the F# compiler
  let private processMsg msg = 
    let m = 
      let t1 = regParseFsOutput.Match(msg) 
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
  let private compile (runtime:TargetRuntime) (framework:TargetFramework) (monitor:IProgressMonitor) argsList = 
  
//    let nw x = if x = None then "None" else x.Value 
//    monitor.Log.WriteLine("Env compiler: " + nw (Common.getCompilerFromEnvironment runtime framework))
//    monitor.Log.WriteLine("Override compiler: " + PropertyService.Get<string>("FSharpBinding.FscPath"))
//    monitor.Log.WriteLine("DefaultDefault compiler: " + (nw Common.getDefaultDefaultCompiler))
//    monitor.Log.WriteLine("Runtime: " + runtime.Id)
//    monitor.Log.WriteLine("Framework: " + framework.Id.ToString())
//    monitor.Log.WriteLine("Default Runtime:" + IdeApp.Preferences.DefaultTargetRuntime.Id);
//    monitor.Log.WriteLine("Default Framework:" + (Common.getDefaultTargetFramework IdeApp.Preferences.DefaultTargetRuntime).Id.ToString())

    let br = BuildResult()

    // Concatenate arguments & run
    let fscPath =
      match Common.getCompilerFromEnvironment runtime framework with
      | Some(result) -> Some(result)
      | None -> 
        match PropertyService.Get<string>("FSharpBinding.FscPath","") with
        | result when result <> "" -> 
          if runtime.Id <> IdeApp.Preferences.DefaultTargetRuntime.Id then
            br.AddWarning("No compiler found for the selected runtime; using default compiler instead.")
          Some(result)
        | _ ->
          match Common.getDefaultDefaultCompiler() with
          | Some(result) ->
            if runtime.Id <> IdeApp.Preferences.DefaultTargetRuntime.Id then
              br.AddWarning("No compiler found for the selected runtime; using default compiler instead.")
            Some(result)
          | None ->
            br.AddError("No compiler found; add a default compiler in the F# settings.")
            None
    
// The communication with the resident compiler doesn't seem robust enough to use this on Linux
#if USE_RESIDENT
    // Add the "stay resident" --resident compiler option 
    // if using "fsharpc" as the compiler, i.e. a Mono compiler that supports this option.
    let argsList = 
        match fscPath with 
        | None -> argsList
        | Some p -> if p.EndsWith "fsharpc" then "--resident" :: argsList else argsList
#endif
        
    let args = String.concat "\n" argsList

        //monitor.Log.WriteLine("fscPath: " + nw fscPath)

    if fscPath = None then
      br.FailedBuildCount <- 1
      br
    else 
      monitor.Log.WriteLine("{0} {1}", fscPath.Value, args)
      let args = String.concat " " argsList
      let startInfo = 
        new ProcessStartInfo
          (FileName = fscPath.Value, UseShellExecute = false, Arguments = args,
           RedirectStandardError = true, CreateNoWindow = true) 
      Debug.tracef "Compiler" "Compile using: %s Arguments: %s" fscPath.Value args
      let p = Process.Start(startInfo) 
      
      Debug.tracef "Compiler" "Reading output..." 
      // Read all output and fold multi-line 
      let lines = 
        [ let line = ref ""
          while (line := p.StandardError.ReadLine(); !line <> null) do
            Debug.tracef "Compiler" "OUTPUT: %s" !line 
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
      for msg in messages do
        match processMsg msg with
        | true, (f, l, c, n, m) -> br.AddError(f, l, c, n, m)
        | false, (f, l, c, n, m) -> br.AddWarning(f, l, c, n, m)

            
      Debug.tracef "Compiler" "Waiting for exit..." 
      p.WaitForExit()
      Debug.tracef "Compiler" "Done with compilation" 
      br.CompilerOutput <- String.concat "\n" lines
      br
  
  // ------------------------------------------------------------------------------------
  
  /// Compiles the specified F# project using the current configuration
  /// and prints command line command to the MonoDevelop output
  let Compile(items, config:DotNetProjectConfiguration, configSel, monitor) : BuildResult =
    let runtime = MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime
    let framework = config.TargetFramework
    let args = 
        [ yield! [ "--noframework --nologo" ]
          yield! generateCmdArgs config items configSel  
          yield! Common.generateOtherItems items 
      
          // Generate source files (sort using current configuration)
          let fsconfig = config.ProjectParameters :?> FSharpProjectParameters
          let files = Common.getSourceFiles items
          let root = System.IO.Path.GetDirectoryName(config.ProjectParameters.ParentProject.FileName.FullPath.ToString())
          yield! Common.getItemsInOrder root files fsconfig.BuildOrder false ]
          
    compile runtime framework monitor args
    
    // CSharpCompilerParameters compilerParameters = (CSharpCompilerParameters)configuration.CompilationParameters ?? new CSharpCompilerParameters ();
