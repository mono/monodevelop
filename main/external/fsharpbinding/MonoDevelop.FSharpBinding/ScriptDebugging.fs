namespace MonoDevelop.FSharp
open System
open System.IO
open System.Threading
open System.Threading.Tasks
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.Components.Commands
open MonoDevelop.Core
open MonoDevelop.Core.Assemblies
open MonoDevelop.Core.Execution
open MonoDevelop.Debugger
open MonoDevelop.Projects
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Components
open CompilerArguments

type ConsoleKind = Internal | External

type ScriptBuildTarget(scriptPath, consoleKind, source) =
    let (/) a b = Path.Combine(a,b)
    let runtimeFolder =
        match IdeApp.Preferences.DefaultTargetRuntime.Value with
        | :? MonoTargetRuntime as monoRuntime -> monoRuntime.MonoDirectory
        | :? MsNetTargetRuntime as dotnetRuntime -> dotnetRuntime.RootDirectory |> string
        | _ -> failwith "Unknown runtime"

    let tempPath = Path.GetTempPath() / Path.GetFileNameWithoutExtension (scriptPath |> string)

    let scriptFileName = Path.GetFileName (scriptPath |> string)
    let exeName = tempPath / Path.ChangeExtension (scriptFileName, ".exe")

    let getSourceReferences() =
        async {
            let filename = scriptPath |> string
            let checker = FSharpChecker.Create()
            let! opts, _errors = checker.GetProjectOptionsFromScript(filename, source)
            let! _parseFileResults, checkFileResults = 
                    checker.ParseAndCheckFileInProject(filename, 0, source, opts)
            let checkResults =
                match checkFileResults with
                | FSharpCheckFileAnswer.Succeeded res -> res
                | res -> failwithf "Parsing did not finish... (%A)" res

            let projectContext = checkResults.ProjectContext
            return projectContext.GetReferencedAssemblies()
                   |> List.choose (fun a -> a.FileName)
                   |> List.filter(fun a -> not(a.StartsWith runtimeFolder))
        }

    static let emptyTask = Task.FromResult None :> Task
    interface IBuildTarget with
        member x.Build(monitor, _config, _buildReferencedTargets, _operationContext) =
            async {
                if not (Directory.Exists tempPath) then
                    Directory.CreateDirectory tempPath |> ignore

                let! references = getSourceReferences()
                references 
                |> List.iter(fun r -> // copy dll + pdb, mdb, optdata, sigdata etc
                                      let wildcardPath = Path.ChangeExtension(Path.GetFileName r, "*")
                                      let path = Path.GetDirectoryName r
                                      LoggingService.logDebug "Getting files in %s %s" path wildcardPath
                                      let files = Directory.GetFiles(path, wildcardPath)
                                      files |> Seq.iter(fun file ->   
                                          let destination = Path.Combine(tempPath, Path.GetFileName file)
                                          File.Copy(file, destination, true)))

                let runtime = IdeApp.Preferences.DefaultTargetRuntime.Value
                let framework = Project.getDefaultTargetFramework runtime
                let args =
                    [ 
                      yield "--target:exe --nologo -g --debug:portable --define:DEBUG --define:INTERACTIVE --optimize- --tailcalls-"
                      yield "--fullpaths --flaterrors --highentropyva-"
                      if not Platform.IsWindows then
                          yield "--noframework"
                          yield sprintf "-r:%s/4.5-api/System.dll" runtimeFolder
                          yield sprintf "-r:%s/4.5-api/System.Core.dll" runtimeFolder
                          yield sprintf "-r:%s/4.5-api/System.Drawing.dll" runtimeFolder
                          yield "--define:MONO"
                      else
                          yield "--platform:x86" // Our debugger only works for 32bit apps on Windows
                      yield wrapFile (scriptPath |> string)
                      yield sprintf "--out:%s" (wrapFile exeName) ]

                return CompilerService.compile runtime framework monitor tempPath args
            } |> StartAsyncAsTask monitor.CancellationToken

        member x.CanBuild _configSelector = true
        member x.NeedsBuilding _configSelector = true
        member x.CanExecute(_context, _configSelector) = true
        member x.Clean(_monitor, _config, _operationContext) = Task.FromResult (BuildResult())

        member x.Execute(monitor, context, _configSelector) =
            async {
                let command = Runtime.ProcessService.CreateCommand exeName
                command.WorkingDirectory <- Path.GetDirectoryName (scriptPath |> string)
                let tokenSource = new CancellationTokenSource()
                let token = tokenSource.Token

                let console =
                    match consoleKind with
                    | Internal -> context.ConsoleFactory.CreateConsole token
                    | External -> context.ExternalConsoleFactory.CreateConsole token
                let oper = context.ExecutionHandler.Execute(command, console)

                use stopper = monitor.CancellationToken.Register (Action(fun() -> oper.Cancel()))
                do! oper.Task |> Async.AwaitTask 
            } |> StartAsyncAsTask monitor.CancellationToken :> Task

        member x.PrepareExecution(_monitor, _context, _configSelector) = emptyTask

        member x.GetExecutionDependencies() = Seq.empty
        member x.Name = scriptPath |> string

type FSharpDebugScriptTextEditorExtension() =
    inherit TextEditorExtension()

    member x.StartDebugging consoleKind =
        let buildTarget = ScriptBuildTarget (x.Editor.FileName, consoleKind, x.Editor.Text)
        let debug = IdeApp.ProjectOperations.Debug buildTarget
        debug.Task

    [<CommandHandler("MonoDevelop.FSharp.Editor.DebugScriptInternal")>]
    member x.DebugScriptInternalConsole() =
        x.StartDebugging Internal

    [<CommandHandler("MonoDevelop.FSharp.Editor.DebugScriptExternal")>]
    member x.DebugScriptExternalConsole() =
        x.StartDebugging External

    [<CommandUpdateHandler("MonoDevelop.FSharp.Editor.DebugScriptInternal")>]
    member x.DebugScriptInternalConsole(ci:CommandInfo) =
        ci.Visible <- FileSystem.IsAScript (x.Editor.FileName |> string)

    [<CommandUpdateHandler("MonoDevelop.FSharp.Editor.DebugScriptExternal")>]
    member x.DebugScriptExternalConsole(ci:CommandInfo) =
        ci.Visible <- FileSystem.IsAScript (x.Editor.FileName |> string)

type DebugScriptNodeHandler() =
    inherit NodeCommandHandler()
    member x.IsVisible ()=
        match x.CurrentNode.DataItem with
        | :? ProjectFile as pf -> FileSystem.IsAScript (pf.FilePath |> string)
        | _ -> false

    member x.StartDebugging consoleKind =
        let file = x.CurrentNode.DataItem :?> ProjectFile
        let doc = IdeApp.Workbench.OpenDocument(file.FilePath, null, true) |> Async.AwaitTask |> Async.RunSynchronously
        let buildTarget = ScriptBuildTarget (file.FilePath, consoleKind, doc.Editor.Text)
        let debug = IdeApp.ProjectOperations.Debug buildTarget
        debug.Task

    [<CommandHandler("MonoDevelop.FSharp.SolutionPad.DebugScriptInternal")>]
    member x.``Debug script on internal console`` () =
        x.StartDebugging Internal

    [<CommandUpdateHandler("MonoDevelop.FSharp.SolutionPad.DebugScriptInternal")>]
    member x.``Debug script on internal console (update command)`` (ci: CommandInfo) =
        ci.Visible <- x.IsVisible()
                     
    [<CommandHandler("MonoDevelop.FSharp.SolutionPad.DebugScriptExternal")>]
    member x.``Debug script on external console`` () =
        x.StartDebugging External

    [<CommandUpdateHandler("MonoDevelop.FSharp.SolutionPad.DebugScriptExternal")>]
    member x.``Debug script on external console (update command)`` (ci: CommandInfo) =
        ci.Visible <- x.IsVisible()

type DebugScriptBuilder() =
    inherit NodeBuilderExtension()
    override x.CanBuildNode _dataType = true
    override x.CommandHandlerType = typeof<DebugScriptNodeHandler>