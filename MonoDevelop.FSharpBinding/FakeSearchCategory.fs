namespace MonoDevelop.FSharp

open System
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

type FakePad() =
  inherit MonoDevelop.Ide.Gui.AbstractPadContent()
  let view = new FSharpConsoleView()

  do view.InitialiseEvents()

  static member private Pad =
    try let pad = IdeApp.Workbench.GetPad<FakePad>()
        if pad <> null then Some(pad)
        else
            //*attempt* to add the pad manually this seems to fail sporadically on updates and reinstalls, returning null
            let pad = IdeApp.Workbench.AddPad(new FakePad(), 
                                              "MonoDevelop.FSharp.FakePad", 
                                              "FAKE task runner", 
                                              "Center Bottom", 
                                              IconId("md-command"))
            if pad <> null then Some(pad)
            else None
    with exn -> None

  member x.Run (baseDirectory, task, buildScript) =
    let fsiProcess = 
      let startInfo = 
        new ProcessStartInfo
          (FileName = buildScript, UseShellExecute = false, Arguments = task, 
           RedirectStandardError = true, CreateNoWindow = true, RedirectStandardOutput = true,
           RedirectStandardInput = true, StandardErrorEncoding = Text.Encoding.UTF8, 
           StandardOutputEncoding = Text.Encoding.UTF8,
           WorkingDirectory = baseDirectory)
      view.WriteOutput(sprintf "FAKE task runner: Starting %s %s" buildScript task, false)
      view.Clear()

      try
        Process.Start(startInfo)
      with e ->
        LoggingService.LogDebug (sprintf "FAKE task runner %s" (e.ToString()))
        reraise()
    do 
      Event.merge fsiProcess.OutputDataReceived fsiProcess.ErrorDataReceived
        |> Event.filter (fun de -> de.Data <> null)
        |> Event.add (fun de -> 
            LoggingService.LogDebug (sprintf "Interactive: received %s" de.Data)

            async {
              do! DispatchService.GuiDispatch(fun _ -> view.WriteOutput (de.Data + "\n", false))
                  |> Async.AwaitTask
            } |> Async.RunSynchronously)

      fsiProcess.EnableRaisingEvents <- true
      fsiProcess.BeginOutputReadLine()  
      fsiProcess.BeginErrorReadLine()

  override x.Control : Gtk.Widget = view :> Gtk.Widget
  override x.Initialize(_container:MonoDevelop.Ide.Gui.IPadWindow) =
    x.UpdateColors()
    x.UpdateFont()

  member x.UpdateColors() =
    match view.Child with
    | :? Gtk.TextView as v -> 
          let colourStyles = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle(MonoDevelop.Ide.IdeApp.Preferences.ColorScheme.Value)
          
          let shouldMatch = PropertyService.Get ("FSharpBinding.MatchWithThemePropName", false)
          let themeTextColour = colourStyles.PlainText.Foreground |> cairoToGdk
          let themeBackColour = colourStyles.PlainText.Background |> cairoToGdk

          if shouldMatch then
              v.ModifyText(Gtk.StateType.Normal, themeTextColour)
              v.ModifyBase(Gtk.StateType.Normal, themeBackColour)
          else
              let textColour = PropertyService.Get ("FSharpBinding.TextColorPropName", "#000000") |> ColorHelpers.strToColor
              let backColour = PropertyService.Get ("FSharpBinding.BaseColorPropName", "#FFFFFF") |> ColorHelpers.strToColor
              v.ModifyText(Gtk.StateType.Normal, textColour)
              v.ModifyBase(Gtk.StateType.Normal, backColour)
    | _ -> ()
    
  member x.UpdateFont() = 
    let fontName = MonoDevelop.Ide.Fonts.FontService.MonospaceFont.Family
    let fontName = PropertyService.Get ("FSharpBinding.FsiFontName", fontName)
    LoggingService.LogDebug ("FAKE task runner: Loading font '{0}'", fontName)
    let font = Pango.FontDescription.FromString(fontName)
    view.SetFont(font)

type FakeSearchResult(solution: Solution, match', matchedString, rank, scriptPath) =
  inherit SearchResult(match', matchedString, rank)

  override x.SearchResultType =
    SearchResultType.Type

  override x.Description =
    sprintf "Runs the FAKE %s task in %s" matchedString solution.Name

  override x.PlainText = "FAKE " + matchedString

  override x.Icon =
    getImage "md-command"

  override x.CanActivate = true

  override x.Activate() =
    let pad = IdeApp.Workbench.GetPad<FakePad>()

    pad.BringToFront()
    let padContent = pad.Content :?> FakePad

    padContent.Run (string solution.BaseDirectory, matchedString, scriptPath)

type FakeSearchCategory() =
  inherit SearchCategory("FAKE", sortOrder = SearchCategory.FirstCategory)

  override x.get_Tags() = [|"fake"|]

  override x.IsValidTag _tag =
    true

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
         |> Async.Start ), token)
