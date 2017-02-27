namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open Gtk
open MonoDevelop.Core
open Microsoft.FSharp.Compiler.SourceCodeServices
open Mono.TextEditor.Highlighting
open MonoDevelop.Ide

type TokeniserOutput =
  | Token of FSharpTokenInfo * string | EndState of FSharpTokenizerLexState
  static member PlainHash = Token({LeftColumn=0
                                   RightColumn=0
                                   ColorClass=FSharpTokenColorKind.PreprocessorKeyword
                                   CharClass=FSharpTokenCharKind.Delimiter
                                   FSharpTokenTriggerClass=FSharpTokenTriggerClass.None
                                   Tag=0
                                   TokenName="#"
                                   FullMatchedLength=1}, "#")

[<RequireQualifiedAccess>]
type Prompt = Normal | Multiline | None

type Tags =
    | Freezer
    | Keyword
    | User
    | String
    | Comment
    | Inactive
    | Number
    | Operator
    | Preprocessor
    | PlainText

type FSharpConsoleView() as x =
    inherit ScrolledWindow()

    let console = Event<_>()
    let mutable scriptLines = ""
    let commandHistoryPast = new Stack<string> ()
    let commandHistoryFuture = new Stack<string> ()
    let textView = new TextView (WrapMode = Gtk.WrapMode.Word)
    let buffer = textView.Buffer
    let inputBeginMark = buffer.CreateMark (null, buffer.EndIter, true)

    //let getTextTag (chunkStyle:ChunkStyle) =
    //    new TextTag (chunkStyle.Name)
    //                Foreground=ColorScheme.ColorToMarkup chunkStyle.Foreground)

    let mutable lastLineState = 0L //used as last lines state
    let mutable tempState = 0L //used as the last stet for an in process line
    let tags = Dictionary<_,_>()

    let getTokensForLine file defines (line:string) state =
        // Get defined directives
        let defines = defines |> Option.map (fun (s:string) -> s.Split([| ' '; ';'; ',' |], StringSplitOptions.RemoveEmptyEntries) |> List.ofSeq)
        // Create source tokenizer
        let sourceTok = FSharpSourceTokenizer(defaultArg defines ["INTERACTIVE";"EDITING"], file)
        // Parse lines using the tokenizer
        let tokenizer = sourceTok.CreateLineTokenizer(line)
        let rec parseLine state =
            [ match tokenizer.ScanToken(state) with
              | Some(tok), nstate ->
                  let str = line.Substring(tok.LeftColumn, tok.RightColumn - tok.LeftColumn + 1)
                  yield Token(tok, str)
                  yield! parseLine nstate
              | None, nstate ->
                  yield EndState nstate ]
        parseLine state

    let getTokensFromLines file defines (lines:string[]) state =
        [ for line in lines do
              yield getTokensForLine file defines line state ]

    let inputBeingProcessed = ref false
    let startInputProcessing() =
        inputBeingProcessed := true
        { new IDisposable with
          member x.Dispose() = inputBeingProcessed := false }

    let applyToken (txt:string) (tag:TextTag) =
        let mutable startIter = buffer.EndIter
        buffer.InsertWithTags(&startIter, txt, [|tag|])

    let eraseCurrentLine() =
        let mutable start = x.InputLineBegin
        let mutable end' = x.InputLineEnd
        buffer.Delete (&start, &end')

    let (|Keyword|_|) t =
        match t.ColorClass with FSharpTokenColorKind.Keyword -> Some(t) | _ -> None

    let (|Identifier|_|) t =
        match t.ColorClass with FSharpTokenColorKind.Identifier -> Some(t) | _ -> None

    let (|UpperIdentifier|_|) t =
        match t.ColorClass with FSharpTokenColorKind.UpperIdentifier -> Some(t) | _ -> None

    let (|Comment|_|) t =
        match t.ColorClass with FSharpTokenColorKind.Comment -> Some(t) | _ -> None

    let (|String|_|) (t:FSharpTokenInfo) =
        match t.ColorClass with
        | FSharpTokenColorKind.String
        | FSharpTokenColorKind.Text when not (t.CharClass = FSharpTokenCharKind.Delimiter || t.CharClass = FSharpTokenCharKind.Operator) -> Some(t)
        | _ -> None

    let (|InactiveCode|_|) t =
        match t.ColorClass with FSharpTokenColorKind.InactiveCode -> Some(t) | _ -> None

    let (|Number|_|) t =
        match t.ColorClass with FSharpTokenColorKind.Number -> Some(t) | _ -> None

    let (|Operator|_|) t =
        match t.ColorClass, t.CharClass with
        | FSharpTokenColorKind.Operator, _ -> Some(t)
        | _, FSharpTokenCharKind.Delimiter
        | _, FSharpTokenCharKind.Operator -> Some(t)
        | _ -> None

    let (|Preprocessor|_|) t =
        match t.ColorClass with FSharpTokenColorKind.PreprocessorKeyword -> Some(t) | _ -> None

    let highlightLine (line:string) state =
        let tokens =
            if line.StartsWith("#") && line.Length > 1 && not (line.StartsWith("#r")) then
              [ yield TokeniserOutput.PlainHash
                yield! getTokensForLine None None line.[1..] state ]
            else getTokensForLine None None line state
        tokens |>
        List.fold (fun state t ->
                       match t with
                       | Token( t,txt) ->
                           match t with
                           | Keyword _-> applyToken txt tags.[Keyword]; state
                           | Identifier _-> applyToken txt tags.[User]; state
                           | UpperIdentifier _-> applyToken txt tags.[User]; state
                           | Comment _-> applyToken txt tags.[Comment]; state
                           | String _-> applyToken txt tags.[String]; state
                           | InactiveCode _-> applyToken txt tags.[Inactive]; state
                           | Number _-> applyToken txt tags.[Number]; state
                           | Operator _-> applyToken txt tags.[Operator]; state
                           | Preprocessor _-> applyToken txt tags.[Preprocessor]; state
                           | _ -> applyToken txt tags.[PlainText]; state
                       | EndState state -> state) 0L

    let disposables = ResizeArray()

    let createDisposable f =
        { new IDisposable with
            member x.Dispose() = f()}

    let addDisposable =
        disposables.Add

    //let updateColors() =
    //    let addTag (k:Tags) v =
    //        tags.Add(k,v)
    //        buffer.TagTable.Add v

    //    let scheme = SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme.Value)
    //    tags |> Seq.iter (fun (KeyValue(_k,v)) -> buffer.TagTable.Remove(v); v.Dispose())
    //    tags.Clear()
    //    addTag Tags.Keyword (getTextTag scheme.KeywordTypes)
    //    addTag Tags.User (getTextTag scheme.UserTypes)
    //    addTag Tags.String (getTextTag scheme.String)
    //    addTag Tags.Comment (getTextTag scheme.CommentTags)
    //    addTag Tags.Inactive (getTextTag scheme.ExcludedCode)
    //    addTag Tags.Number (getTextTag scheme.Number)
    //    addTag Tags.Operator (getTextTag scheme.Punctuation)
    //    addTag Tags.Preprocessor (getTextTag scheme.Preprocessor)
    //    addTag Tags.PlainText (getTextTag scheme.PlainText)
    //    addTag Tags.Freezer (new TextTag ("Freezer", Editable = false))

    //do updateColors()
    //   x.Add (textView)
    //   x.ShowAll ()

    member x.InitialiseEvents() =
        disposables.Add(
            IdeApp.Preferences.ColorScheme.Changed.Subscribe
              (fun _ (eventArgs:EventArgs) ->
                    //updateColors()
                    x.ShowAll()))

        let handleKeyPressDelegate =
            let mainType = typeof<FSharpConsoleView>
            let keyPress = mainType.GetMethod("HandleKeyPress")
            Delegate.CreateDelegate(typeof<KeyPressEventHandler>, x, keyPress) :?> KeyPressEventHandler

    //    let handleCopyDelegate =
    //      let mainType = typeof<FSharpConsoleView>
    //      let copy = mainType.GetMethod("HandleCopy")
    //      Delegate.CreateDelegate(typeof<EventHandler>, x, copy) :?> EventHandler
    //    textView.add_CopyClipboard(handleCopyDelegate)

        textView.Buffer.InsertText.Subscribe(x.TextInserted)
        |> addDisposable

        textView.add_KeyPressEvent(handleKeyPressDelegate)
        createDisposable (fun () -> textView.remove_KeyPressEvent(handleKeyPressDelegate))
        |> addDisposable

        textView.PopulatePopup.Subscribe(x.TextViewPopulatePopup)
        |> addDisposable

    member x.TextInserted _ =
        if not !inputBeingProcessed then
            using (startInputProcessing())
                (fun _ ->
                    let line : string = x.InputLine
                    let cursorPosition = buffer.CursorPosition
                    eraseCurrentLine()
                    let lines = line.Split([|'\n'|], StringSplitOptions.None)
                    if lines.Length = 1 then tempState <- highlightLine line lastLineState
                    else
                        lines
                        |> Array.iteri
                            (fun i line ->
                                if i < lines.Length-1 then
                                    lastLineState <- highlightLine line lastLineState
                                    x.ProcessReturn()
                                else
                                    tempState <- highlightLine line lastLineState)
                    buffer.PlaceCursor(buffer.GetIterAtOffset(cursorPosition)))

    [<GLib.ConnectBeforeAttribute>]
    member x.HandleKeyPress(_o:obj, args) =
        if (x.ProcessKeyPressEvent (args)) then
            args.RetVal <- true

    //fired on context menu copy
    [<GLib.ConnectBeforeAttribute>]
    member x.HandleCopy(_o:obj, _e: EventArgs) =
        ()

    //fired on context menu paste
    [<GLib.ConnectBeforeAttribute>]
    member x.HandlePaste(_o:obj, _e:EventArgs) =
        ()

    member x.TextViewPopulatePopup _sender args =
        let item = new MenuItem (Mono.Unix.Catalog.GetString ("Clear"))
        let sep = new SeparatorMenuItem ()

        item.Activated.Add (fun _ -> x.Clear ())
        item.Show ()
        sep.Show ()

        args.Menu.Add (sep)
        args.Menu.Add (item)

    member x.SetFont (font) = textView.ModifyFont (font)
    member x.TextView = textView
    member val PromptString = "> " with get, set
    member val PromptMultiLineString = "- " with get, set
    member val AutoIndent = false with get, set

    member x.ProcessReturn () =
        // Bookkeeping
        if (x.InputLine <> "") then

            // Everything but the last item (which was input), in the future stack needs to get put back into the past stack
            while (commandHistoryFuture.Count > 1) do
                commandHistoryPast.Push (commandHistoryFuture.Pop())
            // Clear the pesky junk input line
            commandHistoryFuture.Clear()

            // Record our input line
            commandHistoryPast.Push(x.InputLine)
            if scriptLines = "" then scriptLines <- scriptLines + x.InputLine
            else scriptLines <- scriptLines + "\n" + x.InputLine

            x.ProcessInput (x.InputLine)
            lastLineState <- tempState
            let prompt, newLine =
                if x.InputLine.EndsWith(";;\n") then Prompt.None, true
                else Prompt.Multiline, false
            x.Prompt(newLine, prompt)

    member x.ProcessCommandHistoryUp () =
        if commandHistoryPast.Count > 0 then
            if commandHistoryFuture.Count = 0 then
                commandHistoryFuture.Push (x.InputLine)
            else
                if commandHistoryPast.Count = 1 then ()
                else commandHistoryFuture.Push (commandHistoryPast.Pop ())
            x.InputLine <- commandHistoryPast.Peek ()

    member x.ProcessCommandHistoryDown () =
        if commandHistoryFuture.Count > 0 then
            if commandHistoryFuture.Count = 1 then
                x.InputLine <- commandHistoryFuture.Pop ()
            else
                commandHistoryPast.Push (commandHistoryFuture.Pop ())
                x.InputLine <- commandHistoryPast.Peek ()

    member x.InputLineBegin = buffer.GetIterAtMark(inputBeginMark)
    member x.InputLineEnd = buffer.EndIter
    member x.Cursor = buffer.GetIterAtMark (buffer.InsertMark)

    member x.ProcessKeyPressEvent ( args:KeyPressEventArgs) =
        let returnCode =
            // Short circuit to avoid getting moved back to the input line
            // when paging up and down in the shell output
            if args.Event.Key = Gdk.Key.Page_Up || args.Event.Key = Gdk.Key.Page_Down then false
            else

            // Needed so people can copy and paste, but always end up typing in the prompt.
            if x.Cursor.Compare(x.InputLineBegin) < 0 then
                buffer.MoveMark (buffer.SelectionBound, x.InputLineEnd)
                buffer.MoveMark (buffer.InsertMark, x.InputLineEnd)

            match (args.Event.Key) with
            | Gdk.Key.KP_Enter | Gdk.Key.Return ->
                x.ProcessReturn ()
                true
            | Gdk.Key.KP_Up | Gdk.Key.Up ->
                x.ProcessCommandHistoryUp ()
                true
            | Gdk.Key.KP_Down | Gdk.Key.Down ->
                x.ProcessCommandHistoryDown ()
                true
            | Gdk.Key.KP_Left | Gdk.Key.Left ->
              // On Mac, when using a small keyboard, Home is Command+Left
              if Platform.IsMac && args.Event.State.HasFlag (Gdk.ModifierType.MetaMask) then
                  buffer.MoveMark (buffer.InsertMark, x.InputLineBegin)

                  // Move the selection mark too, if shift isn't held
                  if not (args.Event.State.HasFlag (Gdk.ModifierType.ShiftMask)) then
                      buffer.MoveMark (buffer.SelectionBound, x.InputLineBegin)
                      true
                  else false
              elif x.Cursor.Compare (x.InputLineBegin) <= 0 then true
              else false
            | Gdk.Key.KP_Right | Gdk.Key.Right ->
                if x.Cursor.Compare (x.InputLineEnd) >= 0 then true
                else false
            | Gdk.Key.KP_Home | Gdk.Key.Home ->
                buffer.MoveMark (buffer.InsertMark, x.InputLineBegin)

                // Move the selection mark too, if shift isn't held
                if not (args.Event.State.HasFlag (Gdk.ModifierType.ShiftMask)) then
                    buffer.MoveMark (buffer.SelectionBound, x.InputLineBegin)
                true
            | Gdk.Key.a ->
                if (args.Event.State.HasFlag (Gdk.ModifierType.ControlMask)) then
                    buffer.MoveMark (buffer.InsertMark, x.InputLineBegin)

                    // Move the selection mark too, if shift isn't held
                    if not (args.Event.State.HasFlag (Gdk.ModifierType.ShiftMask)) then
                        buffer.MoveMark (buffer.SelectionBound, x.InputLineBegin)
                    true
                else false
            | Gdk.Key.BackSpace | Gdk.Key.Delete -> false
            | _ ->
                //do our syntax highlighting
                using (startInputProcessing()) (fun _ ->
                let nextKey = Gdk.Keyval.ToUnicode(args.Event.KeyValue) |> char |> string
                buffer.InsertAtCursor(nextKey)
                let cursorOffset = buffer.CursorPosition
                let line = x.InputLine
                eraseCurrentLine()
                let lines = line.Split([|'\n'|], StringSplitOptions.None)
                if lines.Length = 1 then tempState <- highlightLine line lastLineState
                else
                  lines
                  |> Array.iteri
                      (fun i line ->
                          if i < lines.Length-1 then
                              lastLineState <- highlightLine line lastLineState
                              x.ProcessReturn()
                          else
                              tempState <- highlightLine line lastLineState)
                buffer.PlaceCursor(buffer.GetIterAtOffset(cursorOffset)))
                true

        returnCode

    // The current input line
    member x.InputLine
        with get() =  buffer.GetText (x.InputLineBegin, x.InputLineEnd, false)
        and set(v) =
            using (startInputProcessing()) (fun _ ->
            let mutable start = x.InputLineBegin
            let mutable end' = x.InputLineEnd
            buffer.Delete (&start, &end')
            start <- x.InputLineBegin
            tempState <- highlightLine v lastLineState)

    member x.ProcessInput (line:string) =
        x.WriteOutput("\n", false)
        console.Trigger(line)

    member x.WriteOutput (line:string, highlight) =
        using (startInputProcessing()) (fun _ ->
        if highlight then
            let tokens = getTokensFromLines None None (line.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)) 0L
            tokens |>
            List.iter
              (List.iter (function
                          | Token( t,txt) ->
                              match t with
                              | Keyword _-> applyToken txt tags.[Keyword]
                              | Identifier _-> applyToken txt tags.[User]
                              | UpperIdentifier _-> applyToken txt tags.[User]
                              | Comment _-> applyToken txt tags.[Comment]
                              | String _-> applyToken txt tags.[String]
                              | InactiveCode _-> applyToken txt tags.[Inactive]
                              | Number _-> applyToken txt tags.[Number]
                              | Operator _-> applyToken txt tags.[Operator]
                              | Preprocessor _-> applyToken txt tags.[Preprocessor]
                              | _ -> applyToken txt tags.[PlainText]
                          | EndState _ ->
                              let mutable end' = buffer.EndIter
                              buffer.Insert (&end', "\n")))
        else
            let mutable start = buffer.EndIter
            buffer.Insert(&start, line)

        buffer.PlaceCursor (buffer.EndIter)
        textView.ScrollMarkOnscreen (buffer.InsertMark))

    member x.Prompt (newLine, prompt:Prompt) =
        using (startInputProcessing()) (fun _ ->
        let mutable end' = buffer.EndIter

        if newLine then buffer.Insert (&end', "\n")

        match prompt with
        | Prompt.Normal -> buffer.Insert (&end', x.PromptString)
        | Prompt.Multiline -> buffer.Insert (&end', x.PromptMultiLineString)
        | Prompt.None -> ()

        buffer.PlaceCursor (buffer.EndIter)
        textView.ScrollMarkOnscreen (buffer.InsertMark)

        buffer.MoveMark (inputBeginMark, buffer.EndIter)

        // Freeze all the text except our input line
        buffer.ApplyTag(tags.[Freezer], buffer.StartIter, x.InputLineBegin))


    member x.Clear () =
        buffer.Text <- ""
        scriptLines <- ""
        x.Prompt (false, Prompt.Normal)

    member x.ClearHistory () =
        commandHistoryFuture.Clear ()
        commandHistoryPast.Clear ()

    [<CLIEvent>]
    member x.ConsoleInput = console.Publish

    interface IDisposable with
        member x.Dispose() =
          disposables |> Seq.iter (fun d -> d.Dispose() )
