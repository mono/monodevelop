namespace MonoDevelop.FSharp

open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open MonoDevelop.Core
open MonoDevelop.Core.Text
open MonoDevelop.Components.MainToolbar
open MonoDevelop.Ide
open MonoDevelop.Projects
open MonoDevelop.Core.Execution
open MonoDevelop.Ide.Gui

type FakeSearchResult(solution: Solution, match', matchedString, rank, scriptPath) =
    inherit SearchResult(match', matchedString, rank)

    override x.SearchResultType = SearchResultType.Type

    override x.Description =
        sprintf "Runs the FAKE %s task in %s" matchedString solution.Name

    override x.PlainText = "FAKE " + matchedString

    override x.Icon = getImage "md-command"

    override x.CanActivate = true

    override x.Activate() =
        let monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ("FAKE")
        let processStartInfo = Runtime.ProcessService.CreateProcessStartInfo(scriptPath, matchedString, string solution.BaseDirectory, true)
        let fakeProcess = Process.Start processStartInfo

        fakeProcess.EnableRaisingEvents <- true
        fakeProcess.BeginOutputReadLine()
        fakeProcess.BeginErrorReadLine()
        fakeProcess.OutputDataReceived.Add(fun de -> monitor.Log.WriteLine de.Data)
        fakeProcess.ErrorDataReceived.Add(fun de -> monitor.ErrorLog.WriteLine de.Data)

        let pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor monitor
        pad.BringToFront()

        fakeProcess.Exited.Add(fun _ -> monitor.Dispose())

type FakeSearchCategory() =
    inherit SearchCategory("FAKE", sortOrder = SearchCategory.FirstCategory)

    override x.get_Tags() = [|"fake"|]

    override x.IsValidTag _tag = true

    override x.GetResults(searchCallback, pattern, token) =

        let addResult (solution: Solution, m: Match, rank, scriptPath) =
            if token.IsCancellationRequested then ()
            else
                let sr = FakeSearchResult(solution, pattern.Pattern, m.Groups.[1].Value, rank, scriptPath)
                searchCallback.ReportResult sr

        Task.Run(
          (fun () ->
              let matcher = StringMatcher.GetMatcher (pattern.Pattern, false)
              async {
                  for solution in IdeApp.Workspace.GetAllSolutions() do

                    let launcherScript = if Platform.IsWindows then
                                              "build.cmd"
                                          else
                                              "build.sh"

                    let fakeScriptPath = Path.Combine([|string solution.BaseDirectory; "build.fsx"|])
                    let launcherScriptPath = Path.Combine([|string solution.BaseDirectory; launcherScript|])
                    if File.Exists(fakeScriptPath) && File.Exists(launcherScriptPath)  then
                        let fakeScript = File.ReadAllText fakeScriptPath

                        Regex.Matches(fakeScript, "Target \"([\\w.]+)\"")
                        |> Seq.cast<Match>
                        |> Seq.choose(fun x -> let (matched, rank) = matcher.CalcMatchRank ("FAKE " + x.Groups.[1].Value)
                                               match matched with
                                               | true -> Some (solution, x, rank, launcherScriptPath)
                                               | _ -> None)
                        |> Seq.iter addResult }
              |> Async.StartAndLogException ), token)
