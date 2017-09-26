namespace MonoDevelop.FSharp

open System
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open Microsoft.FSharp.Compiler.SourceCodeServices

module statusBarTooltip =
    let runInMainThread f = Runtime.RunInMainThread(fun () -> f() |> ignore) |> ignore

    let getTooltip (editor:TextEditor) (context:DocumentContext) =
        context.TryGetAst()
        |> Option.iter(fun ast ->
            async {
                let lineText = editor.GetLineText(editor.CaretLine)
                let! tooltip = ast.GetToolTip(editor.CaretLine, editor.CaretColumn-1, lineText)
                tooltip |> Option.iter(fun (tip, _lineNr) ->
                    let firstResult = function
                        | FSharpToolTipElement.Group items ->
                            items 
                            |> List.map(fun data -> data.MainDescription)
                            |> List.tryFind(String.IsNullOrWhiteSpace >> not)
                        | _ -> None

                    match tip with
                    | FSharpToolTipText items ->
                        items
                        |> Seq.tryPick firstResult
                        |> Option.iter(fun text -> runInMainThread(fun() -> Ide.IdeApp.Workbench.StatusBar.ShowMessage text)))
            } |> Async.StartImmediate)

type StatusBarTooltipExtension() =
    inherit TextEditorExtension()
    let mutable disposables = []
    override x.Initialize() =
        disposables <-
          [ x.Editor.CaretPositionChanged
            |> Observable.filter(fun _ -> PropertyService.Get(Settings.showStatusBarTooltips, false))
            |> Observable.throttle (TimeSpan.FromMilliseconds 500.0)
            |> Observable.subscribe (fun _ -> statusBarTooltip.getTooltip x.Editor x.DocumentContext)

            PropertyService.PropertyChanged.Subscribe
                (fun p -> if p.Key = Settings.showStatusBarTooltips then
                              Ide.IdeApp.Workbench.StatusBar.ShowReady()) ]

    override x.Dispose() =
        disposables |> List.iter(fun d -> d.Dispose())
