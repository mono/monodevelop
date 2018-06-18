namespace MonoDevelop.FSharp

open System
open System.Reflection
open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Ide.Gui.Pads.ProjectPad
open MonoDevelop.Projects

type FindReferenceUsagesHandler() =
    inherit CommandHandler()

    let getSelectedReference() =
        IdeApp.ProjectOperations.CurrentSelectedProject
        |> Option.ofObj
        |> Option.bind(fun _ ->
            let pad = IdeApp.Workbench.GetPad<ProjectSolutionPad> ()
            let solutionPad = pad.Content :?> ProjectSolutionPad
            let selectedNodes = solutionPad.TreeView.GetSelectedNodes()

            match selectedNodes with
            | null -> None
            | _ when selectedNodes.Length <> 1 -> None
            | _ ->
                let dataItem = selectedNodes.[0].DataItem
                dataItem |> Option.tryCast<ProjectReference>)

    override x.Update (commandInfo:CommandInfo) =
        let reference = getSelectedReference()
        let visible, enabled =
            match getSelectedReference() with
            | Some ref ->
                (ref.Project |> Option.tryCast<FSharpProject>).IsSome, true
            | None -> true, false

        commandInfo.Visible <- visible
        commandInfo.Enabled <- enabled

    override x.Run() =
        getSelectedReference()
        |> Option.iter(fun reference ->
            async {
                use monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)

                let assemblyName = AssemblyName(reference.Reference).Name
                let! symbols = Search.getAllProjectSymbols reference.Project

                symbols
                |> Seq.filter(fun symbol ->
                    symbol.Symbol.Assembly.SimpleName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                |> Seq.map (fun symbol -> Symbols.getOffsetsTrimmed symbol.Symbol.DisplayName symbol)
                |> Seq.distinct
                |> Seq.iter (fun (filename, startOffset, endOffset) ->
                    let result = SearchResult (FileProvider (filename), startOffset, endOffset-startOffset)
                    monitor.ReportResult result)
            } |> Async.Start)
