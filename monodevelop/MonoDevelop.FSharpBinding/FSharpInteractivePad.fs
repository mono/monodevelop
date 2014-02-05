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
open FSharp.CompilerBinding

[<AutoOpen>]
module ColorHelpers =
    let strToColor s = 
        let c = ref (new Color())
        match Color.Parse (s, c) with
        | true -> !c
        | false -> new Color() // black is as good a guess as any here
        
    let colorToStr (c:Color) =
        sprintf "#%04X%04X%04X" c.Red c.Green c.Blue
        
    let cairoToHsl (c:Cairo.Color) = HslColor.op_Implicit(c)
    let gdkToHsl (c:Gdk.Color) = HslColor.op_Implicit(c)
    let hslToCairo (c:HslColor) : Cairo.Color = HslColor.op_Implicit(c)
    let hslToGdk (c:HslColor) : Gdk.Color = HslColor.op_Implicit(c)
     
    let cairoToGdk = cairoToHsl >> hslToGdk

[<AutoOpen>]
module EventHandlerHelpers = 
  type IDelegateEvent<'Del when 'Del :> Delegate> with
    member this.Subscribe handler =
      do this.AddHandler(handler)
      { new IDisposable with 
          member x.Dispose() =
            this.RemoveHandler(handler) }

type FSharpCommands = 
  | ShowFSharpInteractive = 0
  | SendSelection = 1
  | SendLine = 2

type KillIntent = 
  | Restart
  | Kill
  | NoIntent // Unexpected kill, or from #q/#quit, so we prompt  

type FSharpInteractivePad() =
  let view = new ConsoleView()
  let mutable killIntent = NoIntent
  let mutable isPrompting = false
  let mutable activeDoc : IDisposable option = None
  
  let isInsideFSharpFile () = 
    if IdeApp.Workbench.ActiveDocument = null ||
       IdeApp.Workbench.ActiveDocument.FileName.FileName = null then false
    else
      let file = IdeApp.Workbench.ActiveDocument.FileName.ToString()
      CompilerArguments.supportedExtension(Path.GetExtension(file))

  let getCorrectDirectory () = 
    if IdeApp.Workbench.ActiveDocument <> null && isInsideFSharpFile() then
      let doc = IdeApp.Workbench.ActiveDocument.FileName.ToString()
      if doc <> null then Path.GetDirectoryName(doc) |> Some else None
    else None

  let setupSession() = 
    let ses = InteractiveSession()
    let textReceived = ses.TextReceived.Subscribe(fun t -> view.WriteOutput t )
    let promptReady = ses.PromptReady.Subscribe(fun () -> view.Prompt true )
    ses.Exited.Add(fun e -> 
      textReceived.Dispose()
      promptReady.Dispose()
      if killIntent = NoIntent then
        DispatchService.GuiDispatch(fun () ->
          Debug.WriteLine (sprintf "Interactive: process stopped")
          view.WriteOutput("\nSession termination detected. Press Enter to restart."))
        isPrompting <- true
      elif killIntent = Restart then 
        DispatchService.GuiDispatch view.Clear
      killIntent <- NoIntent)
    ses.StartReceiving()
    // Make sure we're in the correct directory after a start/restart. No ActiveDocument event then.
    getCorrectDirectory() |> Option.iter (fun path -> ses.SendCommand("#silentCd @\"" + path + "\";;"))
    ses
    
  let session = ref (Some(setupSession()))

  let sendCommand (str:string) = 
     session := match !session with 
                | None -> Some (setupSession())
                | s -> s
     !session |> Option.iter (fun s -> s.SendCommand(str))

  let resetFsi intent = 
    killIntent <- intent
    !session |> Option.iter (fun ses -> ses.Kill())
    if intent = Restart then session := Some (setupSession())
  
  let AddSourceToSelection selection =
     let stap = IdeApp.Workbench.ActiveDocument.Editor.SelectionRange.Offset
     let line = IdeApp.Workbench.ActiveDocument.Editor.OffsetToLineNumber(stap)
     let file = IdeApp.Workbench.ActiveDocument.FileName
     String.Format("# {0} \"{1}\"\n{2}\n" ,line,file.FullPath,selection)  

  let ensureCorrectDirectory _ =
    getCorrectDirectory()
    |> Option.iter (fun path -> sendCommand ("#silentCd @\"" + path + "\";;") )
    
  let consoleInputHandler (cie : ConsoleInputEventArgs) = 
    if isPrompting then 
      isPrompting <- false
      session := None
      sendCommand ""
    elif cie.Text.EndsWith(";;") then 
      sendCommand cie.Text
  
  /// Make path absolute using the specified 'root' path if it is not already
  let makeAbsolute root (path:string) = 
    let path = path.Replace("\"","")
    if Path.IsPathRooted(path) then path
    else Path.Combine(root, path)
    
  //let handler = 
  do Debug.WriteLine ("InteractivePad: created!")
  #if DEBUG
  do view.Destroyed.Add (fun _ -> Debug.WriteLine ("Interactive: view destroyed"))
  do IdeApp.Exiting.Add (fun _ -> Debug.WriteLine ("Interactive: app exiting!!"))
  do IdeApp.Exited.Add  (fun _ -> Debug.WriteLine ("Interactive: app exited!!"))
  #endif

  member x.Shutdown()  = 
    do Debug.WriteLine (sprintf "Interactive: x.Shutdown()!")
    resetFsi Kill
 
  interface MonoDevelop.Ide.Gui.IPadContent with
    member x.Dispose() =
      Debug.WriteLine ("Interactive: disposing pad...")
      activeDoc |> Option.iter (fun ad -> ad.Dispose())
      x.Shutdown()

    member x.Control : Gtk.Widget = view :> Gtk.Widget
  
    member x.Initialize(container:MonoDevelop.Ide.Gui.IPadWindow) = 
      view.ConsoleInput.Add consoleInputHandler
      view.Child.KeyPressEvent.Add(fun ea ->
        if ea.Event.State &&& ModifierType.ControlMask = ModifierType.ControlMask && ea.Event.Key = Key.period then
          !session |> Option.iter (fun s -> s.Interrupt()))
      activeDoc <- IdeApp.Workbench.ActiveDocumentChanged.Subscribe ensureCorrectDirectory |> Some

      x.UpdateFont()   

      view.ShadowType <- Gtk.ShadowType.None
      view.ShowAll()
      
      match view.Child with
      | :? Gtk.TextView as v -> 
            v.PopulatePopup.Add(fun (args) -> 
                                    let item = new Gtk.MenuItem(GettextCatalog.GetString("Reset"))
                                    item.Activated.Add(fun _ -> x.RestartFsi())
                                    item.Show()
                                    args.Menu.Add(item))
      | _ -> ()
      
      x.UpdateColors()
                            
      let toolbar = container.GetToolbar(Gtk.PositionType.Right)

      let buttonClear = new DockToolButton("gtk-clear")
      buttonClear.Clicked.Add(fun _ -> view.Clear())
      buttonClear.TooltipText <- GettextCatalog.GetString("Clear")
      toolbar.Add(buttonClear)
      
      let buttonRestart = new DockToolButton("gtk-refresh")
      buttonRestart.Clicked.Add(fun _ -> x.RestartFsi())
      buttonRestart.TooltipText <- GettextCatalog.GetString("Reset")
      toolbar.Add(buttonRestart)
      
      toolbar.ShowAll()
      
    member x.RedrawContent() = ()
  
  member x.RestartFsi() = resetFsi Restart
  member x.ClearFsi() = view.Clear()
    
  member x.UpdateColors() =
    match view.Child with
    | :? Gtk.TextView as v -> 
          let colourStyles = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle(MonoDevelop.Ide.IdeApp.Preferences.ColorScheme)
          
          let (_, shouldMatch) = PropertyService.Get<string>("FSharpBinding.MatchWitThemePropName", "false") |> System.Boolean.TryParse
          let themeTextColour = colourStyles.PlainText.Foreground |> cairoToGdk
          let themeBackColour = colourStyles.PlainText.Background |> cairoToGdk
          if(shouldMatch) then
              v.ModifyText(Gtk.StateType.Normal, themeTextColour)
              v.ModifyBase(Gtk.StateType.Normal, themeBackColour)
          else
              let textColour = PropertyService.Get<string>("FSharpBinding.TextColorPropName", "#000000") 
                                  |> ColorHelpers.strToColor
              let backColour = PropertyService.Get<string>("FSharpBinding.BaseColorPropName", "#FFFFFF") 
                                  |> ColorHelpers.strToColor
              v.ModifyText(Gtk.StateType.Normal, textColour)
              v.ModifyBase(Gtk.StateType.Normal, backColour)
    | _ -> ()
    
  member x.UpdateFont() = 
    let fontName = DesktopService.DefaultMonospaceFont
    let fontName = PropertyService.Get<string>("FSharpBinding.FsiFontName", fontName)
    Debug.WriteLine (sprintf "Interactive: Loading font '%s'" fontName)
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
      let line = IdeApp.Workbench.ActiveDocument.Editor.Caret.Line
      let text = IdeApp.Workbench.ActiveDocument.Editor.GetLineText(line)
      let file = IdeApp.Workbench.ActiveDocument.FileName
      let sel = String.Format("# {0} \"{1}\"\n{2}\n", line, file.FullPath, text)
      sendCommand sel
      //advance to the next line
      IdeApp.Workbench.ActiveDocument.Editor.SetCaretTo(line + 1, Mono.TextEditor.DocumentLocation.MinColumn, false)

  member x.IsSelectionNonEmpty = 
    if IdeApp.Workbench.ActiveDocument = null || 
       IdeApp.Workbench.ActiveDocument.FileName.FileName = null then false  
    else
      let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
      not(String.IsNullOrEmpty(sel))
    
  member x.IsInsideFSharpFile = isInsideFSharpFile()
      
  member x.LoadReferences() =
    Debug.WriteLine("FSI:  #LoadReferences")
    let project = IdeApp.Workbench.ActiveDocument.Project :?> DotNetProject

    let references =
        let getAbsProjRefs (proj:DotNetProject) = 
            proj.GetReferencedAssemblies(ConfigurationSelector.Default, true)
            |> Seq.map (makeAbsolute (proj.BaseDirectory.ToString()))

        let projRefAssemblies =
            project.References 
            |> Seq.filter (fun refs -> refs.ReferenceType = ReferenceType.Project)
            |> Seq.map (fun refs -> IdeApp.Workspace.GetAllProjects() 
                                    |> Seq.find (fun proj -> proj.Name = refs.Reference && proj :? DotNetProject) :?> DotNetProject)
            |> Seq.collect (fun dnp -> getAbsProjRefs dnp)

        getAbsProjRefs project
        |> Seq.append projRefAssemblies
        |> Seq.filter (fun ref ->  not (ref.Contains "mscorlib.dll" || ref.Contains "FSharp.Core.dll") )
        |> Seq.distinct
        |> Seq.toArray
    
    let orderAssemblyReferences = FSharp.CompilerBinding.OrderAssemblyReferences()
    let orderedreferences = orderAssemblyReferences.Order references
    ensureCorrectDirectory()
    sendCommand orderedreferences
      
  static member CurrentPad =  
    let existing = 
      try IdeApp.Workbench.GetPad<FSharpInteractivePad>()
      with _ -> 
        Debug.WriteLine (sprintf "Interactive: GetPad<FSharpInteractivePad>() failed, silently ignoring")
        null // It throws after addin is loaded (before restart)
    if existing <> null then existing
    else IdeApp.Workbench.AddPad
          ( new FSharpInteractivePad(), "FSharp.MonoDevelop.FSharpInteractivePad", 
            "F# Interactive", "Center Bottom", IconId("md-fs-project"))

  static member CurrentFsi = 
    FSharpInteractivePad.CurrentPad.Content :?> FSharpInteractivePad


type ShowFSharpInteractive() =
  inherit CommandHandler()
  override x.Run() = 
    let pad = FSharpInteractivePad.CurrentPad
    pad.BringToFront(true)
  override x.Update(info:CommandInfo) =
    info.Enabled <- true
    info.Visible <- true

type InteractiveCommand(desription, command, ?bringToFront) =
  inherit CommandHandler()
  let toFront = defaultArg bringToFront false
  override x.Update(info:CommandInfo) =
    let fsi = FSharpInteractivePad.CurrentFsi
    info.Enabled <- true
    info.Visible <- fsi.IsInsideFSharpFile
  override x.Run() =
    Debug.WriteLine(desription)
    command()
    FSharpInteractivePad.CurrentPad.BringToFront(toFront)

  
type SendSelection() =
  inherit InteractiveCommand("Interactive: Send selection to F# interactive invoked!",
                             FSharpInteractivePad.CurrentFsi.SendSelection)

type SendLine() =
  inherit InteractiveCommand("Interactive: Send line to F# interactive invoked!",
                             FSharpInteractivePad.CurrentFsi.SendLine)

type SendReferences() =
  inherit InteractiveCommand("Interactive: Load references in F# interactive invoked!",
                             FSharpInteractivePad.CurrentFsi.LoadReferences)

type RestartFsi() =
  inherit InteractiveCommand("Interactive: Restart invoked!",
                             FSharpInteractivePad.CurrentFsi.RestartFsi)

type ClearFsi() =
  inherit InteractiveCommand("Interactive: Clear invoked!",
                             FSharpInteractivePad.CurrentFsi.ClearFsi)
