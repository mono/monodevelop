namespace MonoDevelop.FSharp
open System
open System.Reflection
open System.Collections.Generic
open Gtk
open MonoDevelop.Core
open MonoDevelop.Ide.Execution
open Pango
 
type FSharpConsoleView() as x = 
  inherit ScrolledWindow()

  let console = Event<_>()
  let mutable scriptLines = ""
  let commandHistoryPast = new Stack<string> ()
  let commandHistoryFuture = new Stack<string> ()
  let mutable inBlock = false
  let mutable blockText = ""
  let textView = new TextView (WrapMode = Gtk.WrapMode.Word)
  let buffer = textView.Buffer
  let inputBeginMark = buffer.CreateMark (null, buffer.EndIter, true)
  // The 'Freezer' tag is used to keep everything except the input line from being editable
  let freezerTag = new TextTag ("Freezer", Editable = false)
  let errorTag   = new TextTag ("error", Background = "#dc3122", Foreground = "white", Weight = Weight.Bold)
  let warningTag = new TextTag ("warning", Foreground = "black", Background = "yellow")
  let debugTag   = new TextTag ("debug", Foreground = "darkgrey")

  do x.Add (textView)
     x.ShowAll ()
     buffer.TagTable.Add (freezerTag)
     buffer.TagTable.Add (errorTag)
     buffer.TagTable.Add (warningTag)
     buffer.TagTable.Add (debugTag)

  member x.InitialiseEvents() =
    let handleKeyPressDelegate =
      let mainType = typeof<FSharpConsoleView>
      let meth = mainType.GetMethod("HandleKeyPress")
      Delegate.CreateDelegate(typeof<KeyPressEventHandler>, x, meth) :?> KeyPressEventHandler

    textView.add_KeyPressEvent(handleKeyPressDelegate)
    textView.PopulatePopup.AddHandler(x.TextViewPopulatePopup)

  [<GLib.ConnectBeforeAttribute>]
  member x.HandleKeyPress(_o:obj, args) =
    if (x.ProcessKeyPressEvent (args)) then
        args.RetVal <- true
  
  member x.TextViewPopulatePopup =
    PopulatePopupHandler(fun sender args ->
    let item = new MenuItem (Mono.Unix.Catalog.GetString ("Clear"))
    let sep = new SeparatorMenuItem ()

    item.Activated.Add (fun _ -> x.Clear ())
    item.Show ()
    sep.Show ()

    args.Menu.Add (sep)
    args.Menu.Add (item))

//  member x.ClearActivated (e) = x.Clear ()
  member x.SetFont (font) = textView.ModifyFont (font)
  member x.TextView = textView
  member val PromptString = "> " with get, set
  member val AutoIndent = false with get, set
  member val PromptMultiLineString = ">> " with get, set

  member x.ProcessReturn () =
      if inBlock then
        if x.InputLine = "" then
          x.ProcessInput (blockText)
          blockText <- ""
          inBlock <- false
        else 
          blockText <- blockText + "\n" + x.InputLine
          let mutable whiteSpace = null
          if x.AutoIndent then
            let r = Text.RegularExpressions.Regex (@"^(\s+).*")
            whiteSpace <- r.Replace (x.InputLine, "$1")
            if x.InputLine.EndsWith (x.BlockStart, StringComparison.InvariantCulture) then
              whiteSpace <- whiteSpace + "\t"
          
          x.Prompt (true, true)
          if x.AutoIndent then
            x.InputLine <- x.InputLine + whiteSpace
      else
        // Special case for start of new code block
        if not (String.IsNullOrEmpty (x.BlockStart) && x.InputLine.Trim().EndsWith (x.BlockStart, StringComparison.InvariantCulture)) then
          inBlock <- true
          blockText <- x.InputLine
          x.Prompt (true, true)
          if x.AutoIndent then
            x.InputLine <- x.InputLine + "\t"
        

        // Bookkeeping
        if (x.InputLine <> "") then
          // Everything but the last item (which was input),
          //in the future stack needs to get put back into the
          // past stack
          while (commandHistoryFuture.Count > 1) do
            commandHistoryPast.Push (commandHistoryFuture.Pop())
          // Clear the pesky junk input line
          commandHistoryFuture.Clear()

          // Record our input line
          commandHistoryPast.Push(x.InputLine)
          if scriptLines = "" then
            scriptLines <- scriptLines + x.InputLine
          else
            scriptLines <- scriptLines + "\n" + x.InputLine

          x.ProcessInput (x.InputLine)

  member x.ProcessCommandHistoryUp () =
      if not inBlock && commandHistoryPast.Count > 0 then
        if commandHistoryFuture.Count = 0 then
          commandHistoryFuture.Push (x.InputLine)
        else
          if commandHistoryPast.Count = 1 then ()
          else commandHistoryFuture.Push (commandHistoryPast.Pop ())
        x.InputLine <- commandHistoryPast.Peek ()

  member x.ProcessCommandHistoryDown () =
      if not inBlock && commandHistoryFuture.Count > 0 then
        if commandHistoryFuture.Count = 1 then
          x.InputLine <- commandHistoryFuture.Pop ()
        else
          commandHistoryPast.Push (commandHistoryFuture.Pop ())
          x.InputLine <- commandHistoryPast.Peek ()

  member x.InputLineBegin = buffer.GetIterAtMark(inputBeginMark) :TextIter
  member x.InputLineEnd = buffer.EndIter :TextIter
  member x.Cursor = buffer.GetIterAtMark (buffer.InsertMark) : TextIter
  member x.Buffer = textView.Buffer
  member x.GetIterLocation ( iter:TextIter) = textView.GetIterLocation (iter)

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

      | Gdk.Key.period -> false
      | _ -> false

    returnCode

  // The current input line
  member x.InputLine
    with get() =  buffer.GetText (x.InputLineBegin, x.InputLineEnd, false)
    and set(v) =
        let mutable start = x.InputLineBegin
        let mutable end' = x.InputLineEnd
        buffer.Delete (&start, &end')
        start <- x.InputLineBegin
        buffer.Insert (&start, v)

  member x.ProcessInput (line:string) =
    x.WriteOutput ("\n")
    console.Trigger(line)

  member x.WriteOutput (line) =
    x.WriteOutput (line, LogLevel.Default)

  member x.WriteOutput (line, logLevel) =
    let tag = x.GetTag (logLevel)
    let mutable start = buffer.EndIter

    if (tag = null) then
      buffer.Insert(&start, line)
    else
      buffer.InsertWithTags(&start, line, tag)
    
    buffer.PlaceCursor (buffer.EndIter)
    textView.ScrollMarkOnscreen (buffer.InsertMark)

  member x.GetTag (logLevel:LogLevel) =
    match (logLevel) with
    | LogLevel.Critical
    | LogLevel.Error
      -> [|errorTag|]
    | LogLevel.Warning
      -> [|warningTag|]
    | LogLevel.Debug
      -> [|debugTag|]
    | _ -> null


  member x.Prompt (newLine) =
    x.Prompt (newLine, false)

  member x.Prompt (newLine, multiline) =
    let mutable end' = buffer.EndIter
    if newLine then
      buffer.Insert (&end', "\n")
    if multiline then
      buffer.Insert (&end', x.PromptMultiLineString)
    else
      buffer.Insert (&end', x.PromptString)

    buffer.PlaceCursor (buffer.EndIter)
    textView.ScrollMarkOnscreen (buffer.InsertMark)

    x.UpdateInputLineBegin ()

    // Freeze all the text except our input line
    buffer.ApplyTag(freezerTag, buffer.StartIter, x.InputLineBegin)

  member x.UpdateInputLineBegin () =
    // Record the end of where we processed, used to calculate start
    // of next input line
    buffer.MoveMark (inputBeginMark, buffer.EndIter)

  member x.Clear () =
    buffer.Text <- ""
    scriptLines <- ""
    x.Prompt (false)

  member x.ClearHistory () =
    commandHistoryFuture.Clear ()
    commandHistoryPast.Clear ()

  member val BlockStart = "" with get, set
  member val BlockEnd   = "" with get, set

  [<CLIEvent>]
  member x.ConsoleInput = console.Publish