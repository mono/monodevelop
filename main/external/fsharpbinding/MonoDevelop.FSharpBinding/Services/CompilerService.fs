// --------------------------------------------------------------------------------------
// Compilation of projects - generates command line options for
// the compiler and parses compiler error messages
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions
open MonoDevelop.Core
open MonoDevelop.Core.Assemblies
open MonoDevelop.Projects
open MonoDevelop.Ide
open CompilerArguments
// --------------------------------------------------------------------------------------

/// Functions that implement compilation, parsing, etc..
//
// NOTE: Only used when xbuild support is not enabled. When xbuild is enabled, the .targets file finds
// FSharp.Build.dll which finds the F# compiler and builds the compilation arguments.
module CompilerService =
    /// Generate various command line arguments for the project
    let private generateCmdArgs (config:DotNetProjectConfiguration, projReferencedAssemblies, regLangVersion, configSel) =
      [ match config.CompileTarget with
        | CompileTarget.Library  -> yield "--target:library"
        | CompileTarget.Module   -> yield "--target:module"
        | CompileTarget.WinExe   -> yield "--target:winexe"
        | (*CompileTarget.Exe*)_ -> yield "--target:exe"

        if config.SignAssembly then yield "--keyfile:" + CompilerArguments.wrapFile(config.AssemblyKeyFile.ToString())
        yield "--out:" + CompilerArguments.wrapFile (config.CompiledOutputName.ToString())

        // Generate compiler options based on F# specific project settings
        let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters

        if not (String.IsNullOrEmpty fsconfig.DocumentationFile) then
            let docFile = config.CompiledOutputName.ChangeExtension(".xml").ToString()
            yield ("--doc:" + CompilerArguments.wrapFile docFile)

        let shouldWrap = true// The compiler argument paths should always be wrapped, since some paths (ie. on Windows) may contain spaces.
        let proj = config.ParentItem
        yield! CompilerArguments.generateCompilerOptions (proj, projReferencedAssemblies, fsconfig, regLangVersion, CompilerArguments.getTargetFramework config.TargetFramework.Id, configSel, shouldWrap) ]


    let private regParseFsOutput = Regex(@"(?<file>[^\(]*)\((?<line>[0-9]*),(?<col>[0-9]*)\):\s(?<type>[^:]*)\s(?<err>[^:]*):\s(?<msg>.*)", RegexOptions.Compiled);
    let private regParseFsOutputNoNum = Regex(@"(?<file>[^\(]*)\((?<line>[0-9]*),(?<col>[0-9]*)\):\s(?<type>[^:]*)\s(?<msg>.*)", RegexOptions.Compiled);
    let private regParseFsOutputNoLocation = Regex(@"(?<type>[^:]*)\s(?<err>[^:]*):\s(?<msg>.*)", RegexOptions.Compiled);

    /// Process a single message emitted by the F# compiler
    let processMsg msg =
        let m =
            let t1 = regParseFsOutput.Match(msg)
            if t1.Success then t1 else
            let t2 = regParseFsOutputNoNum.Match(msg)
            if t2.Success then t2 else
            regParseFsOutputNoLocation.Match(msg)
        let get (s:string) = match m.Groups.Item(s) with null -> None | v -> match v.Value with null | "" -> None | x -> Some x
        if m.Success then
            let errNo = match get "err" with None -> "" | Some v -> v
            let file = match get "file" with None -> "unknown-file"  | Some v -> v
            let line = match get "line" with None -> 1 | Some v -> int32 v
            let col = match get "col" with None -> 1 | Some v -> int32 v
            let msg = match get "msg" with None -> "" | Some v -> v
            let isError = match get "type" with None -> true | Some v -> (v <> "warning")
            isError, (file, line, col, errNo, msg)
        else
            true, ("unknown-file", 0, 0, "0", msg)

  (*
    processMsg "warning FS0075: The command-line option '--warnon' is for internal use only"
        = (false,("unknown-file", 1, 1, "FS0075","The command-line option '--warnon' is for internal use only"))

    processMsg @"C:\test\a.fs(2,17): warning FS0025: Incomplete pattern matches on this expression. For example, the value '0' may indicate a case not covered by the pattern(s)."
        = (false,(@"C:\test\a.fs", 2, 17, "FS0025","Incomplete pattern matches on this expression. For example, the value '0' may indicate a case not covered by the pattern(s)."))

    processMsg @"C:\test space\a.fs(2,15): error FS0001: The type 'float' does not match the type 'int'"
      = (true,(@"C:\test space\a.fs", 2, 15, "FS0001","The type 'float' does not match the type 'int'"))

    processMsg "error FS0082: Could not resolve this reference. Could not locate the assembly \"foo.dll\". Check to make sure the assembly exists on disk. If this reference is required by your code, you may get compilation errors. (Code=MSB3245)"
      = (true,("unknown-file", 1, 1, "FS0082","Could not resolve this reference. Could not locate the assembly \"foo.dll\". Check to make sure the assembly exists on disk. If this reference is required by your code, you may get compilation errors. (Code=MSB3245)"))
  *)

    /// Run the F# compiler with the specified arguments (passed as a list)
    /// and print the arguments to progress monitor (Output in MonoDevelop)
    let compile (runtime:TargetRuntime) (framework:TargetFramework) (monitor:ProgressMonitor) projectDir argsList =

  //    let nw x = if x = None then "None" else x.Value
  //    monitor.Log.WriteLine("Env compiler: " + nw (Common.getCompilerFromEnvironment runtime framework))
  //    monitor.Log.WriteLine("Override compiler: " + PropertyService.Get<string>("FSharpBinding.FscPath"))
  //    monitor.Log.WriteLine("DefaultDefault compiler: " + (nw Common.getDefaultFSharpCompiler))
  //    monitor.Log.WriteLine("Runtime: " + runtime.Id)
  //    monitor.Log.WriteLine("Framework: " + framework.Id.ToString())
  //    monitor.Log.WriteLine("Default Runtime:" + IdeApp.Preferences.DefaultTargetRuntime.Id);
  //    monitor.Log.WriteLine("Default Framework:" + (Common.getDefaultTargetFramework IdeApp.Preferences.DefaultTargetRuntime).Id.ToString())

        let br = BuildResult()

        // Concatenate arguments & run
        let fscPath =
            match CompilerArguments.getCompilerFromEnvironment runtime framework with
            | Some(result) -> Some(result)
            | None ->
              match PropertyService.Get<string>("FSharpBinding.FscPath","") with
              | result when result <> "" ->
                  if runtime.Id <> IdeApp.Preferences.DefaultTargetRuntime.Value.Id then
                      br.AddWarning("No compiler found for the selected runtime; using default compiler instead.") |> ignore
                  Some(result)
              | _ ->
                match CompilerArguments.getDefaultFSharpCompiler() with
                | Some(result) ->
                    if runtime.Id <> IdeApp.Preferences.DefaultTargetRuntime.Value.Id then
                        br.AddWarning("No compiler found for the selected runtime; using default compiler instead.") |> ignore
                    Some(result)
                | None ->
                    br.AddError("No compiler found; add a default compiler in the F# settings.") |> ignore
                    None

        let args = String.concat "\n" argsList

        if fscPath = None then
            br.FailedBuildCount <- 1
            br
        else
            monitor.Log.WriteLine("{0} {1}", fscPath.Value, args)
            let args = String.concat " " argsList
            let startInfo =
                new ProcessStartInfo
                  (FileName = fscPath.Value, UseShellExecute = false, Arguments = args,
                  RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = projectDir)
            LoggingService.LogDebug ("Compiler: Compile using: {0} Arguments: {1}", fscPath.Value, args)
            let p = Process.Start(startInfo)

            LoggingService.LogDebug ("Compiler: Reading output..." )
            // Read all output and fold multi-line
            let lines =
                [ let line = ref ""
                  while (line := p.StandardError.ReadLine(); !line <> null) do
                    LoggingService.LogDebug ("Compiler: OUTPUT: {0}", !line)
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
                | true, (f, l, c, n, m) -> br.AddError(f, l, c, n, m) |> ignore
                | false, (f, l, c, n, m) -> br.AddWarning(f, l, c, n, m) |> ignore


            LoggingService.LogDebug ("Compiler: Waiting for exit...")
            p.WaitForExit()
            LoggingService.LogDebug ("Compiler: Done with compilation" )
            br.CompilerOutput <- String.concat "\n" lines
            br

    // ------------------------------------------------------------------------------------
    /// Compiles the specified F# project using the current configuration
    let Compile(items, config:DotNetProjectConfiguration, projReferencedAssemblies, configSel, monitor) : BuildResult =
        let runtime = config.TargetRuntime
        let framework = config.TargetFramework
        let root = Path.GetDirectoryName(config.ParentItem.FileName.FullPath.ToString())
        let args =
            [ yield! [ "--noframework --nologo" ]
              yield! generateCmdArgs(config, projReferencedAssemblies, None, configSel)
              yield! CompilerArguments.generateOtherItems items

              // Generate source files
              let files = items
                          |> CompilerArguments.getSourceFiles
                          |> List.map CompilerArguments.wrapFile
              yield! files ]

        compile runtime framework monitor root args

