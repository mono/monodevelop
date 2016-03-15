#nowarn "40"
namespace MonoDevelop.FSharp

open System
open System.Diagnostics
open System.IO

open Gdk
open MonoDevelop.Components
open MonoDevelop.Components.Docking
open MonoDevelop.Components.Commands
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Projects
open MonoDevelop.FSharp

open MonoDevelop
open Mono.TextEditor
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.SourceEditor
open System.Threading.Tasks
open System.Collections.Generic
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

type FSharpInteractiveTextEditorOptions(options: MonoDevelop.Ide.Editor.DefaultSourceEditorOptions) =
    inherit TextEditorOptions()
    interface Mono.TextEditor.ITextEditorOptions with
        member x.ColorScheme = options.ColorScheme

type FsiDocumentContext() as this =
    inherit DocumentContext()
    let name = "__FSI__.fsx"
    let pd = new FSharpParsedDocument(name) :> ParsedDocument
    let project = Services.ProjectService.CreateDotNetProject ("F#")

    let mutable view:ICompletionWidget = null
    let mutable editor:TextEditor = null

    let contextChanged = DelegateEvent<_>()
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

    member x.SourceEditorView 
        with set (value) = view <- value
    member x.Editor 
            with set (value) = editor <- value
    
    interface ICompletionWidget with
        member x.CaretOffset
            with get() = view.CaretOffset
            and set(offset) = view.CaretOffset <- offset
        member x.TextLength = editor.Length
        member x.SelectedLength = view.SelectedLength
        member x.GetText(startOffset, endOffset) =
            view.GetText(startOffset, endOffset)
        member x.GetChar offset = editor.GetCharAt offset
        member x.Replace(offset, count, text) =
            view.Replace(offset, count, text)
        member x.GtkStyle = view.GtkStyle
        member x.ZoomLevel = view.ZoomLevel
        member x.CreateCodeCompletionContext triggerOffset =
            view.CreateCodeCompletionContext triggerOffset
        member x.CurrentCodeCompletionContext 
            with get() = view.CurrentCodeCompletionContext

        member x.GetCompletionText ctx = view.GetCompletionText ctx

        member x.SetCompletionText (ctx, partialWord, completeWord) =
            view.SetCompletionText (ctx, partialWord, completeWord)
        member x.SetCompletionText (ctx, partialWord, completeWord, completeWordOffset) =
            view.SetCompletionText (ctx, partialWord, completeWord, completeWordOffset)
        [<CLIEvent>]
        member x.CompletionContextChanged = contextChanged.Publish

type FSharpInteractivePad2() =
    inherit MonoDevelop.Ide.Gui.PadContent()
   
    let options = DefaultSourceEditorOptions.Instance
    let ctx = FsiDocumentContext()
    let doc = TextEditorFactory.CreateNewDocument()

    do
        options.ShowLineNumberMargin <- false
        options.TabsToSpaces <- true
        options.ShowWhitespaces <- ShowWhitespaces.Never
        doc.FileName <- FilePath ctx.Name

    let editor = TextEditorFactory.CreateNewEditor(ctx, doc, TextEditorType.Default)

    let mutable killIntent = NoIntent
    let mutable promptReceived = false
    let mutable activeDoc : IDisposable option = None
    let commandHistoryPast = new Stack<string> ()
    let commandHistoryFuture = new Stack<string> ()

    let getCorrectDirectory () =
        if IdeApp.Workbench.ActiveDocument <> null && FileService.isInsideFSharpFile() then
            let doc = IdeApp.Workbench.ActiveDocument.FileName.ToString()
            if doc <> null then Path.GetDirectoryName(doc) |> Some else None
        else None

    let nonBreakingSpace = "\u00A0" // used to disable editor syntax highlighting for output
    let fsiOutput t =
        editor.InsertAtCaret (nonBreakingSpace + t)

    let prompt() =
        editor.InsertAtCaret ("\n> ")

    let setupSession() =
        try
            let ses = InteractiveSession()

            let textReceived = ses.TextReceived.Subscribe(fun t -> Runtime.RunInMainThread(fun () -> fsiOutput t) |> ignore)
            let promptReady = ses.PromptReady.Subscribe(fun () -> Runtime.RunInMainThread(fun () -> promptReceived <- true; prompt() ) |> ignore)
            //let colourSchemChanged =
            //    IdeApp.Preferences.ColorScheme.Changed.Subscribe (fun _ -> this.UpdateColors ())
            ses.Exited.Add(fun _ ->
                textReceived.Dispose()
                promptReady.Dispose()
                //colourSchemChanged.Dispose()
                if killIntent = NoIntent then
                    Runtime.RunInMainThread(fun () ->
                        LoggingService.LogDebug ("Interactive: process stopped")
                        fsiOutput "\nSession termination detected. Press Enter to restart.") |> ignore
                elif killIntent = Restart then
                    Runtime.RunInMainThread (fun () -> editor.Text <- "") |> ignore
                killIntent <- NoIntent
                promptReceived <- false)
            ses.StartReceiving()
            // Make sure we're in the correct directory after a start/restart. No ActiveDocument event then.
            getCorrectDirectory() |> Option.iter (fun path -> ses.SendInput("#silentCd @\"" + path + "\";;"))
            Some(ses)
        with exn -> None

    let mutable session = setupSession()
    let prompt = "> "
    let getLineWithoutPrompt (lineStr:string) =
        if lineStr.[0..1] = prompt || lineStr.[0..1] = "- " then
            lineStr.[2..]
        else
            lineStr
    let getCaretLine() =
        let lineStr =
            editor.CaretLine 
            |> editor.GetLine 
            |> editor.GetLineText
        getLineWithoutPrompt lineStr

    let setCaretLine (s:string) =
        let line = editor.GetLineByOffset editor.CaretOffset
        editor.ReplaceText(line.Offset, line.EndOffset - line.Offset, prompt + s)

    let resetFsi intent =
        killIntent <- intent
        session |> Option.iter (fun ses -> ses.Kill())
        if intent = Restart then session <- setupSession()

    member x.Session = session
    member x.Shutdown()  =
        do LoggingService.LogDebug ("Interactive: Shutdown()!")
        resetFsi Kill

    member x.SendCommand command =
        session 
        |> Option.iter(fun ses ->
            commandHistoryPast.Push command
            ses.SendInput command)

    member x.RequestCompletions lineStr column =
        session 
        |> Option.iter(fun ses ->
            ses.SendCompletionRequest lineStr (column + 1))

    member x.ProcessCommandHistoryUp () =
        if commandHistoryPast.Count > 0 then
            if commandHistoryFuture.Count = 0 then
                commandHistoryFuture.Push (getCaretLine())
            else
                if commandHistoryPast.Count = 1 then ()
                else commandHistoryFuture.Push (commandHistoryPast.Pop ())
            setCaretLine (commandHistoryPast.Peek ())

    member x.ProcessCommandHistoryDown () =
        if commandHistoryFuture.Count > 0 then
            if commandHistoryFuture.Count = 1 then
                setCaretLine (commandHistoryFuture.Pop ())
            else
                commandHistoryPast.Push (commandHistoryFuture.Pop ())
                setCaretLine (commandHistoryPast.Peek ())

    override x.Dispose() =
        LoggingService.LogDebug ("Interactive: disposing pad...")
        //activeDoc |> Option.iter (fun ad -> ad.Dispose())
        x.Shutdown()
        editor.Dispose()

    override x.Control = editor :> Control

    static member Pad =
        try let pad = IdeApp.Workbench.GetPad<FSharpInteractivePad2>()
            
            if pad <> null then Some(pad)
            else
                //*attempt* to add the pad manually this seems to fail sporadically on updates and reinstalls, returning null
                let pad = IdeApp.Workbench.AddPad(new FSharpInteractivePad2(),
                                                  "FSharp.MonoDevelop.FSharpInteractivePad2",
                                                  "F# Interactive",
                                                  "Center Bottom",
                                                  IconId("md-fs-project"))
                if pad <> null then Some(pad)
                else None
        with exn -> None

    static member BringToFront(grabfocus) =
        FSharpInteractivePad2.Pad |> Option.iter (fun pad -> pad.BringToFront(grabfocus))

    static member Fsi =
        FSharpInteractivePad2.Pad |> Option.bind (fun pad -> Some(pad.Content :?> FSharpInteractivePad2))

    member x.SendSelection() =
        if x.IsSelectionNonEmpty then
            let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
            getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\";;") )

            x.SendCommand (sel + ";;")
        else
          //if nothing is selected send the whole line
            x.SendLine()

    member x.SendLine() =
        if IdeApp.Workbench.ActiveDocument = null then ()
        else
            getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\";;") )

            let line = IdeApp.Workbench.ActiveDocument.Editor.CaretLine
            let text = IdeApp.Workbench.ActiveDocument.Editor.GetLineText(line)
            let file = IdeApp.Workbench.ActiveDocument.FileName
            let sel = String.Format("# {0} \"{1}\"\ninput {2};;\n", line, file.FullPath, text)
            x.SendCommand sel
            //advance to the next line
            if PropertyService.Get ("FSharpBinding.AdvanceToNextLine", true)
            then IdeApp.Workbench.ActiveDocument.Editor.SetCaretLocation (line + 1, Mono.TextEditor.DocumentLocation.MinColumn, false)

    member x.SendFile() =
        let text = IdeApp.Workbench.ActiveDocument.Editor.Text
        getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\";;") )

        x.SendCommand (text + ";;")

    member x.IsSelectionNonEmpty =
        if IdeApp.Workbench.ActiveDocument = null ||
            IdeApp.Workbench.ActiveDocument.FileName.FileName = null then false
        else
            let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
            not(String.IsNullOrEmpty(sel))

    member x.LoadReferences() =
        LoggingService.LogDebug ("FSI:  #LoadReferences")
        let project = IdeApp.Workbench.ActiveDocument.Project :?> DotNetProject
        
        let references =
            let args =
                CompilerArguments.getReferencesFromProject project
                |> Seq.choose (fun ref -> if (ref.Contains "mscorlib.dll" || ref.Contains "FSharp.Core.dll")
                                          then None
                                          else
                                              let ref = ref |> String.replace "-r:" ""
                                              if File.Exists ref then Some ref
                                              else None )
                |> Seq.distinct
                |> Seq.toArray
            args

        let orderAssemblyReferences = MonoDevelop.FSharp.OrderAssemblyReferences()
        let orderedreferences = orderAssemblyReferences.Order references

        getCorrectDirectory()
            |> Option.iter (fun path -> x.SendCommand ("#silentCd @\"" + path + "\";;") )

        orderedreferences
        |> List.iter (fun a -> x.SendCommand (sprintf  """#r @"%s";;""" a.Path ))

    override x.Initialize(container:MonoDevelop.Ide.Gui.IPadWindow) =
        do 
            LoggingService.LogDebug ("InteractivePad: created!")
            editor.MimeType <- "text/x-fsharp"
            //editor.InsertAtCaret ("\n")
            ctx.SourceEditorView <- editor.GetContent<ICompletionWidget>()
            ctx.Editor <- editor
            let toolbar = container.GetToolbar(DockPositionType.Right)
    
            let buttonClear = new DockToolButton("gtk-clear")
            buttonClear.Clicked.Add(fun _ -> editor.Text <- "")
            buttonClear.TooltipText <- GettextCatalog.GetString("Clear")
            toolbar.Add(buttonClear)
    
            let buttonRestart = new DockToolButton("gtk-refresh")
            buttonRestart.Clicked.Add(fun _ -> x.RestartFsi())
            buttonRestart.TooltipText <- GettextCatalog.GetString("Reset")
            toolbar.Add(buttonRestart)

            toolbar.ShowAll()
    member x.RestartFsi() = resetFsi Restart
    
    member x.ClearFsi() = editor.Text <- ""
/// handles keypresses for F# Interactive
type FSharpFsiEditorCompletion() =
    inherit TextEditorExtension()

    override x.IsValidInContext(context) =
        context :? FsiDocumentContext

    override x.KeyPress (descriptor:KeyDescriptor) =
        match FSharpInteractivePad2.Fsi with
        | Some fsi -> 
            let startLine = x.Editor.CaretLine

            let getCaretLine() =
                let line =
                    x.Editor.CaretLine
                    |> x.Editor.GetLine

                if line.Length > 0 then
                    (x.Editor.GetLineText line), line
                else
                    "", line

            let lineStr, line = getCaretLine()
            let lineHadPrompt = lineStr.StartsWith "> "
            if lineHadPrompt && x.Editor.CaretColumn < 3 then
                x.Editor.CaretColumn <- 3
            let result = 
                match descriptor.SpecialKey with
                | SpecialKey.Return -> 
                    if x.Editor.CaretLine = x.Editor.LineCount then
                        if lineHadPrompt || lineStr.TrimStart().StartsWith("- ") then
                            fsi.SendCommand lineStr.[2..]
                        else
                            fsi.SendCommand lineStr
                              
                        x.Editor.CaretOffset <- line.EndOffset
                        x.Editor.InsertAtCaret "\n"
                        if not (lineStr.TrimEnd().EndsWith(";;")) then
                            x.Editor.InsertAtCaret "- "
                    false
                | SpecialKey.Up -> 
                    if x.Editor.CaretLine = x.Editor.LineCount then
                        fsi.ProcessCommandHistoryUp()
                        let topLine = x.Editor.VisibleLines |> Seq.head
                        x.Editor.ScrollTo(x.Editor.OffsetToLocation topLine.Offset)
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
                    if (x.Editor.CaretLine <> x.Editor.LineCount) || x.Editor.CaretColumn > 3 then // 3 = prompt width
                        base.KeyPress (descriptor)
                    else
                        false
                | SpecialKey.BackSpace
                | SpecialKey.Delete ->
                    if x.Editor.CaretLine = x.Editor.LineCount && x.Editor.CaretColumn > 3 then // 3 = prompt width
                        base.KeyPress (descriptor)
                    else
                        false
                | _ -> base.KeyPress (descriptor)

            if lineStr.StartsWith "> " && x.Editor.CaretColumn < 3 && x.Editor.CaretLine = startLine  then
                let editedLine, _ = getCaretLine()
                // Fixes ctrl-w and maybe other keystrokes that remove the prompt
                if lineHadPrompt && not (editedLine.StartsWith "> ") then
                    x.Editor.InsertText (line.Offset, "> ")
                x.Editor.CaretColumn <- 3

            result
        | _ -> base.KeyPress (descriptor)

type FSharpInteractivePad() as this =
    inherit MonoDevelop.Ide.Gui.PadContent()
    let view = new FSharpConsoleView()

    do view.InitialiseEvents()
    let mutable killIntent = NoIntent
    let mutable promptReceived = false
    let mutable activeDoc : IDisposable option = None

    let getCorrectDirectory () =
        if IdeApp.Workbench.ActiveDocument <> null && FileService.isInsideFSharpFile() then
            let doc = IdeApp.Workbench.ActiveDocument.FileName.ToString()
            if doc <> null then Path.GetDirectoryName(doc) |> Some else None
        else None

    let setupSession() =
        try
            let ses = InteractiveSession()
            let textReceived = 
                ses.TextReceived.Subscribe(fun t -> Runtime.RunInMainThread(fun () -> view.WriteOutput(t, promptReceived) ) |> ignore)
            let promptReady = 
                ses.PromptReady.Subscribe(fun () -> Runtime.RunInMainThread(fun () -> promptReceived<- true; view.Prompt(true, Prompt.Normal) ) |> ignore)
            let colourSchemChanged =
                IdeApp.Preferences.ColorScheme.Changed.Subscribe (fun _ -> this.UpdateColors ())
            ses.Exited.Add(fun _ ->
                textReceived.Dispose()
                promptReady.Dispose()
                colourSchemChanged.Dispose()
                if killIntent = NoIntent then
                    Runtime.RunInMainThread(fun () ->
                        LoggingService.LogDebug ("Interactive: process stopped")
                        view.WriteOutput("\nSession termination detected. Press Enter to restart.", false)) |> ignore
                elif killIntent = Restart then
                    Runtime.RunInMainThread view.Clear |> ignore
                killIntent <- NoIntent
                promptReceived <- false)
            ses.StartReceiving()
            // Make sure we're in the correct directory after a start/restart. No ActiveDocument event then.
            getCorrectDirectory() |> Option.iter (fun path -> ses.SendInput("#silentCd @\"" + path + "\";;"))
            Some(ses)
        with exn -> None

    let session = ref (setupSession())

    let sendCommand (str:string) =
        session := match !session with
                   | None -> setupSession()
                   | s -> s
        !session |> Option.iter (fun s -> s.SendInput(str))

    let resetFsi intent =
        killIntent <- intent
        !session |> Option.iter (fun ses -> ses.Kill())
        if intent = Restart then session := setupSession()

    let AddSourceToSelection selection =
        let stap = IdeApp.Workbench.ActiveDocument.Editor.SelectionRange.Offset
        let line = IdeApp.Workbench.ActiveDocument.Editor.OffsetToLineNumber(stap)
        let file = IdeApp.Workbench.ActiveDocument.FileName
        String.Format("# {0} \"{1}\"\n{2};;\n" ,line,file.FullPath,selection)

    let ensureCorrectDirectory _ =
        getCorrectDirectory()
        |> Option.iter (fun path -> sendCommand ("#silentCd @\"" + path + "\";;") )

    let consoleInputHandler (cie:string) =
        sendCommand cie

    /// Make path absolute using the specified 'root' path if it is not already
    let makeAbsolute root (path:string) =
        let path = path.Replace("\"","")
        if Path.IsPathRooted(path) then path
        else Path.Combine(root, path)

    //let handler =
    do LoggingService.LogDebug ("InteractivePad: created!")

    member x.Shutdown()  =
        do LoggingService.LogDebug ("Interactive: Shutdown()!")
        resetFsi Kill

    override x.Dispose() =
        LoggingService.LogDebug ("Interactive: disposing pad...")
        activeDoc |> Option.iter (fun ad -> ad.Dispose())
        x.Shutdown()
        view.Dispose()

    override x.Control : Control = Control.op_Implicit view

    override x.Initialize(container:MonoDevelop.Ide.Gui.IPadWindow) =
        view.ConsoleInput.Add consoleInputHandler
        view.Child.KeyPressEvent.Add(fun ea ->
          if ea.Event.State &&& ModifierType.ControlMask = ModifierType.ControlMask && ea.Event.Key = Key.period then
              !session |> Option.iter (fun s -> s.Interrupt()))
        if x.IsValidSession then
            activeDoc <- IdeApp.Workbench.ActiveDocumentChanged.Subscribe ensureCorrectDirectory |> Some

        x.UpdateFont()

        view.ShadowType <- Gtk.ShadowType.None
        view.ShowAll()

        match view.Child with
        | :? Gtk.TextView as v ->
            v.PopulatePopup.Add(fun args ->
                                    let item = new Gtk.MenuItem(GettextCatalog.GetString("Reset"))
                                    item.Activated.Add(fun _ -> x.RestartFsi())
                                    item.Show()
                                    args.Menu.Add(item))
        | _ -> ()

        x.UpdateColors()

        let toolbar = container.GetToolbar(DockPositionType.Right)

        let buttonClear = new DockToolButton("gtk-clear")
        buttonClear.Clicked.Add(fun _ -> view.Clear())
        buttonClear.TooltipText <- GettextCatalog.GetString("Clear")
        toolbar.Add(buttonClear)

        let buttonRestart = new DockToolButton("gtk-refresh")
        buttonRestart.Clicked.Add(fun _ -> x.RestartFsi())
        buttonRestart.TooltipText <- GettextCatalog.GetString("Reset")
        toolbar.Add(buttonRestart)

        toolbar.ShowAll()

    member x.RestartFsi() = resetFsi Restart

    member x.ClearFsi() = view.Clear()

    member x.UpdateColors() =
        match view.Child with
        | :? Gtk.TextView as v ->
            let colourStyles = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle(MonoDevelop.Ide.IdeApp.Preferences.ColorScheme.Value)

            let shouldMatch = PropertyService.Get ("FSharpBinding.MatchWithThemePropName", true)
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
        LoggingService.LogDebug ("Interactive: Loading font '{0}'", fontName)
        let font = Pango.FontDescription.FromString(fontName)
        view.SetFont(font)

    member x.SendSelection() =
        if x.IsSelectionNonEmpty then
            let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
            ensureCorrectDirectory()
            sendCommand (AddSourceToSelection sel)
        else
          //if nothing is selected send the whole line
            x.SendLine()

    member x.SendLine() =
        if IdeApp.Workbench.ActiveDocument = null then ()
        else
            ensureCorrectDirectory()
            let line = IdeApp.Workbench.ActiveDocument.Editor.CaretLine
            let text = IdeApp.Workbench.ActiveDocument.Editor.GetLineText(line)
            let file = IdeApp.Workbench.ActiveDocument.FileName
            let sel = String.Format("# {0} \"{1}\"\n{2};;\n", line, file.FullPath, text)
            sendCommand sel
            //advance to the next line
            if PropertyService.Get ("FSharpBinding.AdvanceToNextLine", true)
            then IdeApp.Workbench.ActiveDocument.Editor.SetCaretLocation (line + 1, Mono.TextEditor.DocumentLocation.MinColumn, false)

    member x.SendFile() =
        let text = IdeApp.Workbench.ActiveDocument.Editor.Text
        ensureCorrectDirectory()
        sendCommand (AddSourceToSelection text)

    member x.IsSelectionNonEmpty =
        if IdeApp.Workbench.ActiveDocument = null ||
            IdeApp.Workbench.ActiveDocument.FileName.FileName = null then false
        else
            let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
            not(String.IsNullOrEmpty(sel))

    member x.LoadReferences() =
        LoggingService.LogDebug ("FSI:  #LoadReferences")
        let project = IdeApp.Workbench.ActiveDocument.Project :?> DotNetProject
        
        let references =
            let args =
                CompilerArguments.getReferencesFromProject project
                |> Seq.choose (fun ref -> if (ref.Contains "mscorlib.dll" || ref.Contains "FSharp.Core.dll")
                                          then None
                                          else
                                              let ref = ref |> String.replace "-r:" ""
                                              if File.Exists ref then Some ref
                                              else None )
                |> Seq.distinct
                |> Seq.toArray
            args

        let orderAssemblyReferences = MonoDevelop.FSharp.OrderAssemblyReferences()
        let orderedreferences = orderAssemblyReferences.Order references
        ensureCorrectDirectory()
        orderedreferences
        |> List.iter (fun a -> sendCommand (sprintf  """#r @"%s";;""" a.Path ))


    member x.IsValidSession = !session |> Option.isSome

    static member private Pad =
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
        FSharpInteractivePad2.Pad |> Option.iter (fun pad -> pad.BringToFront(grabfocus))

    static member Fsi =
        FSharpInteractivePad2.Pad |> Option.bind (fun pad -> Some(pad.Content :?> FSharpInteractivePad2))

  type InteractiveCommand(command) =
    inherit CommandHandler()

    override x.Update(info:CommandInfo) =
        info.Enabled <- true
        info.Visible <- FileService.isInsideFSharpFile()
    override x.Run() =
        FSharpInteractivePad.Fsi
        |> Option.iter (fun fsi -> command fsi
                                   FSharpInteractivePad2.BringToFront(false))

  type ShowFSharpInteractive() =
      inherit InteractiveCommand(ignore)
      override x.Update(info:CommandInfo) =
          info.Enabled <- true
          info.Visible <- true

  type SendSelection() =
      inherit InteractiveCommand(fun fsi -> fsi.SendSelection())

  type SendLine() =
      inherit InteractiveCommand(fun fsi -> fsi.SendLine())

  type SendFile() =
      inherit InteractiveCommand(fun fsi -> fsi.SendFile())

  type SendReferences() =
      inherit InteractiveCommand(fun fsi -> fsi.LoadReferences())

  type RestartFsi() =
      inherit InteractiveCommand(fun fsi -> fsi.RestartFsi())

  type ClearFsi() =
      inherit InteractiveCommand(fun fsi -> fsi.ClearFsi())
