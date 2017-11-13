#nowarn "40"
namespace MonoDevelop.FSharp

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open Gdk
open Mono.TextEditor
open MonoDevelop.Components
open MonoDevelop.Components.Docking
open MonoDevelop.Components.Commands
open MonoDevelop.Core
open MonoDevelop.Core.Execution
open MonoDevelop.FSharp
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Projects

[<AutoOpen>]
module ColorHelpers =
    let strToColor s =
        let c = ref (Color())
        match Color.Parse (s, c) with
        | true -> !c
        | false -> Color() // black is as good a guess as any here

    let colorToStr (c:Color) =
        sprintf "#%04X%04X%04X" c.Red c.Green c.Blue

    let cairoToGdk (c:Cairo.Color) = GtkUtil.ToGdkColor(c)

type FSharpCommands =
    | ShowFSharpInteractive = 0
    | SendSelection = 1
    | SendLine = 2
    | SendFile = 3

type KillIntent =
    | Restart
    | Kill
    | NoIntent // Unexpected kill, or from #q/#quit, so we prompt

type ImageRendererMarker(line, image:Xwt.Drawing.Image) =
    inherit TextLineMarker()
    static let tag = obj()
    override x.Draw(editor, cr, metrics) =
        cr.DrawImage(editor, image, 30.0, metrics.LineYRenderStartPosition)

    interface ITextLineMarker with
        member x.Line with get() = line
        member x.IsVisible with get() = true and set(_value) = ()
        member x.Tag with get() = tag and set(_value) = ()

    interface IExtendingTextLineMarker with
        member x.GetLineHeight editor = editor.LineHeight + image.Height
        member x.Draw(_editor, _g, _lineNr, _lineArea) = ()
        member x.IsSpaceAbove with get() = false

type FsiDocumentContext() =
    inherit DocumentContext()
    let name = "__FSI__.fsx"
    let pd = new FSharpParsedDocument(name, None) :> ParsedDocument
    let project = Services.ProjectService.CreateDotNetProject ("F#")

    let mutable completionWidget:ICompletionWidget = null
    let mutable editor:TextEditor = null

    let contextChanged = DelegateEvent<_>()
    let mutable workingFolder: string option = None
    do 
        project.FileName <- FilePath name

    override x.ParsedDocument = pd
    override x.AttachToProject(_) = ()
    override x.ReparseDocument() = ()
    override x.GetOptionSet() = TypeSystemService.Workspace.Options
    override x.Project = project :> Project
    override x.Name = name
    override x.AnalysisDocument with get() = null
    override x.UpdateParseDocument() = Task.FromResult pd
    member x.CompletionWidget 
        with set (value) = 
            completionWidget <- value
            completionWidget.CompletionContextChanged.Add
                (fun _args -> let completion = editor.GetContent<CompletionTextEditorExtension>()
                              ParameterInformationWindowManager.HideWindow(completion, value))
    member x.Editor with set (value) = editor <- value
    member x.WorkingFolder
        with get() = workingFolder
        and set(folder) = workingFolder <- folder
    interface ICompletionWidget with
        member x.CaretOffset
            with get() = completionWidget.CaretOffset
            and set(offset) = completionWidget.CaretOffset <- offset
        member x.TextLength = editor.Length
        member x.SelectedLength = completionWidget.SelectedLength
        member x.GetText(startOffset, endOffset) =
            completionWidget.GetText(startOffset, endOffset)
        member x.GetChar offset = editor.GetCharAt offset
        member x.Replace(offset, count, text) =
            completionWidget.Replace(offset, count, text)
        member x.GtkStyle = completionWidget.GtkStyle

        member x.ZoomLevel = completionWidget.ZoomLevel
        member x.CreateCodeCompletionContext triggerOffset =
            completionWidget.CreateCodeCompletionContext triggerOffset
        member x.CurrentCodeCompletionContext 
            with get() = completionWidget.CurrentCodeCompletionContext

        member x.GetCompletionText ctx = completionWidget.GetCompletionText ctx

        member x.SetCompletionText (ctx, partialWord, completeWord) =
            completionWidget.SetCompletionText (ctx, partialWord, completeWord)
        member x.SetCompletionText (ctx, partialWord, completeWord, completeWordOffset) =
            completionWidget.SetCompletionText (ctx, partialWord, completeWord, completeWordOffset)
        [<CLIEvent>]
        member x.CompletionContextChanged = contextChanged.Publish

type FsiPrompt(icon: Xwt.Drawing.Image) =
    inherit MarginMarker()

    override x.CanDrawForeground margin = 
        margin :? IconMargin

    override x.DrawForeground (editor, cairoContext, metrics) =
        let size = metrics.Margin.Width
        let borderLineWidth = cairoContext.LineWidth

        let x = Math.Floor (metrics.Margin.XOffset - borderLineWidth / 2.0)
        let y = Math.Floor (metrics.Y + (metrics.Height - size) / 2.0)

        let deltaX = size / 2.0 - icon.Width / 2.0 + 0.5
        let deltaY = size / 2.0 - icon.Height / 2.0 + 0.5
        cairoContext.DrawImage (editor, icon, Math.Round (x + deltaX), Math.Round (y + deltaY));

type FSharpInteractivePad(editor:TextEditor) as this =
    inherit MonoDevelop.Ide.Gui.PadContent()
   
    let ctx = editor.DocumentContext :?> FsiDocumentContext
    do
        let options = new CustomEditorOptions (editor.Options)
        editor.MimeType <- "text/x-fsharp"
        editor.ContextMenuPath <- "/MonoDevelop/SourceEditor2/ContextMenu/Fsi"
        options.ShowLineNumberMargin <- false
        options.TabsToSpaces <- true
        options.ShowWhitespaces <- ShowWhitespaces.Never
        ctx.CompletionWidget <- editor.GetContent<ICompletionWidget>()
        ctx.Editor <- editor
        editor.Options <- options

    let mutable killIntent = NoIntent
    let mutable promptReceived = false
    let mutable activeDoc : IDisposable option = None
    let mutable lastLineOutput = None
    let commandHistoryPast = new Stack<string> ()
    let commandHistoryFuture = new Stack<string> ()

    let promptIcon = ImageService.GetIcon("md-breadcrumb-next")
    let newLineIcon = ImageService.GetIcon("md-template")

    let getCorrectDirectory () =
        ctx.WorkingFolder <-
            if IdeApp.Workbench.ActiveDocument <> null && FileService.isInsideFSharpFile() then
                let doc = IdeApp.Workbench.ActiveDocument.FileName.ToString()
                if doc <> null then Path.GetDirectoryName(doc) |> Some else None
            else None
        ctx.WorkingFolder

    let nonBreakingSpace = "\u00A0" // used to disable editor syntax highlighting for output

    let addMarker image =
        let data = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
        let textDocument = data.Document

        let line = data.GetLineByOffset editor.Length
        let prompt = FsiPrompt image

        textDocument.AddMarker(line, prompt)

        textDocument.CommitUpdateAll()

    let setPrompt() =
        editor.InsertText(editor.Length, "\n")
        editor.ScrollTo editor.Length
        addMarker promptIcon

    let renderImage image =
        let data = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
        let textDocument = data.Document
        let line = editor.GetLine editor.CaretLine
        let imageMarker = ImageRendererMarker(line, image)
        textDocument.AddMarker(editor.CaretLine, imageMarker)
        textDocument.CommitUpdateAll()
        editor.InsertAtCaret "\n"

    let input = new ResizeArray<_>()

    let setupSession() =
        try
            let pathToExe =
                Path.Combine(Reflection.Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName, "MonoDevelop.FSharpInteractive.Service.exe")
                |> ProcessArgumentBuilder.Quote
            let ses = InteractiveSession(pathToExe)
            input.Clear()
            promptReceived <- false
            let textReceived = ses.TextReceived.Subscribe(fun t -> Runtime.RunInMainThread(fun () -> this.FsiOutput t) |> ignore)
            let imageReceived = ses.ImageReceived.Subscribe(fun image -> Runtime.RunInMainThread(fun () -> renderImage image) |> Async.AwaitTask |> Async.RunSynchronously)
            let promptReady = ses.PromptReady.Subscribe(fun () -> Runtime.RunInMainThread(fun () -> promptReceived <- true; setPrompt() ) |> ignore)

            ses.Exited.Add(fun _ ->
                textReceived.Dispose()
                promptReady.Dispose()
                imageReceived.Dispose()
                if killIntent = NoIntent then
                    Runtime.RunInMainThread(fun () ->
                        LoggingService.LogDebug ("Interactive: process stopped")
                        this.FsiOutput "\nSession termination detected. Press Enter to restart.") |> ignore
                elif killIntent = Restart then
                    Runtime.RunInMainThread (fun () -> editor.Text <- "") |> ignore
                killIntent <- NoIntent)

            ses.StartReceiving()
            editor.GrabFocus()
            // Make sure we're in the correct directory after a start/restart. No ActiveDocument event then.
            getCorrectDirectory() |> Option.iter (fun path -> ses.SendInput("#silentCd @\"" + path + "\";;"))
            Some(ses)
        with _exn -> None

    let mutable session = None

    let getCaretLine() =
        let line = 
            editor.CaretLine 
            |> editor.GetLine 
        if line.Length > 0 then 
            editor.GetLineText line
        else
            ""

    let setCaretLine (s: string) =
        let line = editor.GetLineByOffset editor.CaretOffset
        editor.ReplaceText(line.Offset, line.EndOffset - line.Offset, s)

    let resetFsi intent =
        if promptReceived then
            killIntent <- intent
            session |> Option.iter (fun (ses: InteractiveSession) -> ses.Kill())
            if intent = Restart then session <- setupSession()

    new() =
        let ctx = FsiDocumentContext()
        let doc = TextEditorFactory.CreateNewDocument()
        do
            doc.FileName <- FilePath ctx.Name

        let editor = TextEditorFactory.CreateNewEditor(ctx, doc, TextEditorType.Default)
        new FSharpInteractivePad(editor)

    member x.FsiOutput t : unit =
        if editor.CaretColumn <> 1 then
            editor.InsertAtCaret ("\n")
        editor.InsertAtCaret (nonBreakingSpace + t)
        editor.CaretOffset <- editor.Text.Length
        editor.ScrollTo editor.CaretLocation
        lastLineOutput <- Some editor.CaretLine

    member x.Text =
        editor.Text

    member x.SetPrompt() =
        editor.InsertText(editor.Length, "\n")
        editor.ScrollTo editor.Length
        addMarker promptIcon

    member x.AddMorePrompt() =
        addMarker newLineIcon

    member x.Session = session

    member x.Shutdown()  =
        do LoggingService.LogDebug ("Interactive: Shutdown()!")
        resetFsi Kill

    member x.SendCommandAndStore command =
        input.Add command
        session 
        |> Option.iter(fun ses ->
            commandHistoryPast.Push command
            ses.SendInput (command + "\n"))

    member x.SendCommand command =
        input.Add command
        session 
        |> Option.iter(fun ses -> ses.SendInput (command + ";;"))

    member x.RequestCompletions lineStr column =
        session 
        |> Option.iter(fun ses ->
            ses.SendCompletionRequest lineStr (column + 1))

    member x.RequestTooltip symbol =
        session 
        |> Option.iter(fun ses -> ses.SendTooltipRequest symbol)

    member x.RequestParameterHint lineStr column =
        session 
        |> Option.iter(fun ses ->
            ses.SendParameterHintRequest lineStr (column + 1))

    member x.ProcessCommandHistoryUp () =
        if commandHistoryPast.Count > 0 then
            if commandHistoryFuture.Count = 0 then
                commandHistoryFuture.Push (getCaretLine())
            else
                if commandHistoryPast.Count = 0 then ()
                else commandHistoryFuture.Push (commandHistoryPast.Pop ())
            setCaretLine (commandHistoryPast.Peek ())

    member x.ProcessCommandHistoryDown () =
        if commandHistoryFuture.Count > 0 then
            if commandHistoryFuture.Count = 0 then
                setCaretLine (commandHistoryFuture.Pop ())
            else
                commandHistoryPast.Push (commandHistoryFuture.Pop ())
                setCaretLine (commandHistoryPast.Peek ())

    override x.Dispose() =
        LoggingService.LogDebug ("Interactive: disposing pad...")
        activeDoc |> Option.iter (fun ad -> ad.Dispose())
        x.Shutdown()
        editor.Dispose()

    override x.Control = editor :> Control

    static member Pad =
        try let pad = IdeApp.Workbench.GetPad<FSharpInteractivePad>()
            
            if pad <> null then Some(pad)
            else
                //*attempt* to add the pad manually this seems to fail sporadically on updates and reinstalls, returning null
                let pad = IdeApp.Workbench.AddPad(new FSharpInteractivePad(),
                                                  "FSharp.MonoDevelop.FSharpInteractivePad",
                                                  "F# Interactive",
                                                  "Center Bottom",
                                                  IconId("md-fs-project"))
                if pad <> null then Some(pad)
                else None
        with exn -> None

    static member BringToFront(grabfocus) =
        FSharpInteractivePad.Pad |> Option.iter (fun pad -> pad.BringToFront(grabfocus))

    static member Fsi =
        FSharpInteractivePad.Pad |> Option.bind (fun pad -> Some(pad.Content :?> FSharpInteractivePad))

    member x.LastOutputLine
        with get() = lastLineOutput
        and set value = lastLineOutput <- value

    member x.SendSelection() =
        if x.IsSelectionNonEmpty then
            let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
            getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\"") )

            x.SendCommand sel
        else
          //if nothing is selected send the whole line
            x.SendLine()

    member x.SendLine() =
        if isNull IdeApp.Workbench.ActiveDocument then ()
        else
            getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\"") )

            let line = IdeApp.Workbench.ActiveDocument.Editor.CaretLine
            let text = IdeApp.Workbench.ActiveDocument.Editor.GetLineText(line)
            x.SendCommand text
            //advance to the next line
            //if PropertyService.Get ("FSharpBinding.AdvanceToNextLine", true)
            //then IdeApp.Workbench.ActiveDocument.Editor.SetCaretLocation (line + 1, Mono.TextEditor.DocumentLocation.MinColumn, false)

    member x.SendFile() =
        let text = IdeApp.Workbench.ActiveDocument.Editor.Text
        getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\"") )

        x.SendCommand text

    member x.IsSelectionNonEmpty =
        if isNull IdeApp.Workbench.ActiveDocument ||
            isNull IdeApp.Workbench.ActiveDocument.FileName.FileName then false
        else
            let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
            not(String.IsNullOrEmpty(sel))

    member x.LoadReferences(project:FSharpProject) =
        LoggingService.LogDebug ("FSI:  #LoadReferences")
        let orderedreferences = project.GetOrderedReferences()

        getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\"") )

        orderedreferences
        |> List.iter (fun a -> x.SendCommand (sprintf  @"#r ""%s""" a.Path))

    override x.Initialize(container:MonoDevelop.Ide.Gui.IPadWindow) =
        LoggingService.LogDebug ("InteractivePad: created!")
        editor.MimeType <- "text/x-fsharp"
        ctx.CompletionWidget <- editor.GetContent<ICompletionWidget>()
        ctx.Editor <- editor
        let toolbar = container.GetToolbar(DockPositionType.Right)

        let addButton(icon, action, tooltip) =
            let button = new DockToolButton(icon)
            button.Clicked.Add(action)
            button.TooltipText <- tooltip
            toolbar.Add(button)

        addButton ("gtk-save", (fun _ -> x.Save()), GettextCatalog.GetString ("Save as script"))
        addButton ("gtk-open", (fun _ -> x.OpenScript()), GettextCatalog.GetString ("Open"))
        addButton ("gtk-clear", (fun _ -> editor.Text <- ""), GettextCatalog.GetString ("Clear"))
        addButton ("gtk-refresh", (fun _ -> x.RestartFsi()), GettextCatalog.GetString ("Reset"))
        toolbar.ShowAll()
        editor.RunWhenRealized(fun () -> session <- setupSession())

    member x.RestartFsi() = resetFsi Restart

    member x.ClearFsi() = editor.Text <- ""

    member x.Save() =
        let dlg = new MonoDevelop.Ide.Gui.Dialogs.OpenFileDialog(GettextCatalog.GetString ("Save as .fsx"), MonoDevelop.Components.FileChooserAction.Save)

        dlg.DefaultFilter <- dlg.AddFilter (GettextCatalog.GetString ("F# script files"), "*.fsx")
        if dlg.Run () then
            let file = 
                if dlg.SelectedFile.Extension = ".fsx" then
                    dlg.SelectedFile
                else
                    dlg.SelectedFile.ChangeExtension(".fsx")

            let lines = input |> Seq.map (fun line -> line.TrimEnd(';'))
            let fileContent = String.concat "\n" lines
            File.WriteAllText(file.FullPath.ToString(), fileContent)

    member x.OpenScript() =
        let dlg = MonoDevelop.Ide.Gui.Dialogs.OpenFileDialog(GettextCatalog.GetString ("Open script"), MonoDevelop.Components.FileChooserAction.Open)
        dlg.AddFilter (GettextCatalog.GetString ("F# script files"), [|".fs"; "*.fsi"; "*.fsx"; "*.fsscript"; "*.ml"; "*.mli" |]) |> ignore
        if dlg.Run () then
            let file = dlg.SelectedFile
            x.SendCommand ("#load @\"" + file.FullPath.ToString() + "\"")

/// handles keypresses for F# Interactive
type FSharpFsiEditorCompletion() =
    inherit TextEditorExtension()
    override x.IsValidInContext(context) =
        context :? FsiDocumentContext

    override x.KeyPress (descriptor:KeyDescriptor) =
        match FSharpInteractivePad.Fsi with
        | Some fsi -> 
            let getLineText (editor:TextEditor) (line:IDocumentLine) =
                if line.Length > 0 then
                    editor.GetLineText line
                else
                    ""

            let getInputLines (editor:TextEditor) =
                let lineNumbers =
                    match fsi.LastOutputLine with
                    | Some lineNumber ->
                        [ lineNumber+1 .. editor.CaretLine ]
                    | None -> [ editor.CaretLine ]
                lineNumbers 
                |> List.map editor.GetLine
                |> List.map (getLineText editor)

            let result =
                match descriptor.SpecialKey with
                | SpecialKey.Return ->
                    if x.Editor.CaretLine = x.Editor.LineCount then
                        let lines = getInputLines x.Editor
                        lines
                        |> List.iter(fun (lineStr) ->
                            fsi.SendCommandAndStore lineStr)

                        let line = x.Editor.GetLine x.Editor.CaretLine
                        let lineStr = getLineText x.Editor line
                        x.Editor.CaretOffset <- line.EndOffset
                        x.Editor.InsertAtCaret "\n"

                        if not (lineStr.TrimEnd().EndsWith(";;")) then
                            fsi.AddMorePrompt()
                        fsi.LastOutputLine <- Some line.LineNumber
                    false
                | SpecialKey.Up -> 
                    if x.Editor.CaretLine = x.Editor.LineCount then
                        fsi.ProcessCommandHistoryUp()
                        false
                    else
                        base.KeyPress (descriptor)
                | SpecialKey.Down -> 
                    if x.Editor.CaretLine = x.Editor.LineCount then
                        fsi.ProcessCommandHistoryDown()
                        false
                    else
                        base.KeyPress (descriptor)
                | SpecialKey.Left ->
                    if (x.Editor.CaretLine <> x.Editor.LineCount) || x.Editor.CaretColumn > 1 then
                        base.KeyPress (descriptor)
                    else
                        false
                | SpecialKey.BackSpace ->
                    if x.Editor.CaretLine = x.Editor.LineCount && x.Editor.CaretColumn > 1 then
                        base.KeyPress (descriptor)
                    else
                        false
                | _ -> 
                    if x.Editor.CaretLine <> x.Editor.LineCount then
                        x.Editor.CaretOffset <- x.Editor.Length
                    base.KeyPress (descriptor)

            result
        | _ -> base.KeyPress (descriptor)

    member x.clipboardHandler = x.Editor.GetContent<IClipboardHandler>()

    [<CommandHandler ("MonoDevelop.Ide.Commands.EditCommands.Cut")>]
    member x.Cut() = x.clipboardHandler.Cut()

    [<CommandUpdateHandler ("MonoDevelop.Ide.Commands.EditCommands.Cut")>]
    member x.CanCut(ci:CommandInfo) =
        ci.Enabled <- x.clipboardHandler.EnableCut

    [<CommandHandler ("MonoDevelop.Ide.Commands.EditCommands.Copy")>]
    member x.Copy() = x.clipboardHandler.Copy()

    [<CommandUpdateHandler ("MonoDevelop.Ide.Commands.EditCommands.Copy")>]
    member x.CanCopy(ci:CommandInfo) =
        ci.Enabled <- x.clipboardHandler.EnableCopy

    [<CommandHandler ("MonoDevelop.Ide.Commands.EditCommands.Paste")>]
    member x.Paste() = x.clipboardHandler.Paste()

    [<CommandUpdateHandler ("MonoDevelop.Ide.Commands.EditCommands.Paste")>]
    member x.CanPaste(ci:CommandInfo) =
        ci.Enabled <- x.clipboardHandler.EnablePaste

    [<CommandHandler ("MonoDevelop.Ide.Commands.ViewCommands.ZoomIn")>]
    member x.ZoomIn() = x.Editor.GetContent<IZoomable>().ZoomIn()

    [<CommandHandler ("MonoDevelop.Ide.Commands.ViewCommands.ZoomOut")>]
    member x.ZoomOut() = x.Editor.GetContent<IZoomable>().ZoomOut()

    [<CommandHandler ("MonoDevelop.Ide.Commands.ViewCommands.ZoomReset")>]
    member x.ZoomReset() = x.Editor.GetContent<IZoomable>().ZoomReset()

  type InteractiveCommand(command) =
    inherit CommandHandler()

    override x.Run() =
        FSharpInteractivePad.Fsi
        |> Option.iter (fun fsi -> command fsi
                                   FSharpInteractivePad.BringToFront(false))

  type FSharpFileInteractiveCommand(command) =
    inherit InteractiveCommand(command)

    override x.Update(info:CommandInfo) =
        info.Enabled <- true
        info.Visible <- FileService.isInsideFSharpFile()

  type ShowFSharpInteractive() =
      inherit InteractiveCommand(ignore)
      override x.Update(info:CommandInfo) =
          info.Enabled <- true
          info.Visible <- true

  type SendSelection() =
      inherit FSharpFileInteractiveCommand(fun fsi -> fsi.SendSelection())

  type SendLine() =
      inherit FSharpFileInteractiveCommand(fun fsi -> fsi.SendLine())

  type SendFile() =
      inherit FSharpFileInteractiveCommand(fun fsi -> fsi.SendFile())

  type SendReferences() =
      inherit CommandHandler()
      override x.Run() =
          async {
              let project = IdeApp.Workbench.ActiveDocument.Project :?> FSharpProject
              do! project.GetReferences()
              FSharpInteractivePad.Fsi
              |> Option.iter (fun fsi -> fsi.LoadReferences(project)
                                         FSharpInteractivePad.BringToFront(false))
          } |> Async.StartImmediate

  type RestartFsi() =
      inherit InteractiveCommand(fun fsi -> fsi.RestartFsi())

  type ClearFsi() =
      inherit InteractiveCommand(fun fsi -> fsi.ClearFsi())
