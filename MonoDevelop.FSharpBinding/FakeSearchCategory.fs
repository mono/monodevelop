namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open MonoDevelop.Core
open MonoDevelop.Core.Text
open MonoDevelop.Components.Commands
open MonoDevelop.Components.MainToolbar
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Gui
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices
open Symbols

type FakePad() as this =
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
  override x.Control : Gtk.Widget = view :> Gtk.Widget

type FakeSearchResult(match', matchedString, rank) =
  inherit SearchResult(match', matchedString, rank)
  //let command = new Command
  //IdeApp.CommandService.RegisterCommand()
  override x.SearchResultType =
    SearchResultType.Type

  override x.Description =
    sprintf "Runs the FAKE %s task" matchedString

  override x.PlainText = "FAKE " + matchedString

//  override x.File = symbol.RangeAlternate.FileName
  override x.Icon =
    getImage "md-type"

  override x.CanActivate = true

  override x.Activate() =
    let pad = IdeApp.Workbench.GetPad<FakePad>()
    pad.BringToFront()

  //override x.
//  override x.GetTooltipInformation(token) =
//    new TooltipInformation("fake tooltip")
//
//  override x.Offset =
//    fst (offsetAndLength.Force())
//
//  override x.Length = 
//    snd (offsetAndLength.Force())

type FakeSearchCategory() =
  inherit SearchCategory("FAKE", sortOrder = SearchCategory.FirstCategory)

  override x.get_Tags() = [|"fake"|]

  override x.IsValidTag tag =
    true

  override x.GetResults(searchCallback, pattern, token) =

    let addResult (symbol: Project, m: Match, rank) = 
      if token.IsCancellationRequested then ()
      else
        let sr = FakeSearchResult(pattern.Pattern, m.Groups.[1].Value, rank)
        searchCallback.ReportResult sr

    Task.Run(
      (fun () ->
         let allProjectFiles =
           IdeApp.Workspace.GetAllProjects()
           |> Seq.filter (fun p -> p.SupportedLanguages |> Array.contains "F#")
           //|> Seq.map (fun p -> p)
         
         //let cachingSearch = Search.byPattern (Dictionary<_,_>())
         let matcher = StringMatcher.GetMatcher (pattern.Pattern, false)
         async {
           for projFile in allProjectFiles do
             //let projName = projFile.Name
             let root = projFile.BaseDirectory;
             let fakeScriptPath = Path.Combine([|string root; "build.fsx"|])
             if File.Exists(fakeScriptPath) then
               let fakeScript = File.ReadAllText fakeScriptPath
               Regex.Matches(fakeScript, "Target \"([\\w.]+)\"")
                 |> Seq.cast<Match> 
                 |> Seq.map (fun m -> (projFile, m, matcher.CalcMatchRank ("FAKE " + m.Groups.[1].Value)))
                 |> Seq.choose (fun x -> match x with
                                         | (p, m, (true, r)) -> Some (p, m, r) 
                                         | _ -> None)
                 |> Seq.iter addResult }
         |> Async.Start ), token)
            //addResult(projFile, rank)
          //}
//          let cachingSearch = Search.byPattern (Dictionary<_,_>())
//          async {for projFile in allProjectFiles do
//                   let! allProjectSymbols = Search.getAllProjectSymbols projFile
//                   let typeFilteredSymbols = Search.byTag pattern.Tag allProjectSymbols
//                   let matchedSymbols = typeFilteredSymbols |> cachingSearch pattern.Pattern
//                   matchedSymbols |> Seq.iter addResult }
//          |> Async.Start ), token)
