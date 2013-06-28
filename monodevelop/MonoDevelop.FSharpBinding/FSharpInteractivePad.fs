#nowarn "40"
namespace MonoDevelop.FSharp

open System
open System.Diagnostics
open System.IO
open System.Xml
open System.CodeDom.Compiler

open Gdk
open MonoDevelop.Components
open MonoDevelop.Components.Docking
open MonoDevelop.Components.Commands
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Projects

open MonoDevelop.FSharp

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
  let mutable lastCommand = ""
  let mutable killIntent = NoIntent
  let mutable isPrompting = false

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
    ses
    
  let session = ref (Some(setupSession()))

  let sendCommand (str:string) = 
     session := match !session with 
                | None -> Some (setupSession())
                | s -> s
     !session |> Option.iter (fun s ->
         lastCommand <- str.Trim()
         s.SendCommand(str))

  let resetFsi intent = 
    killIntent <- intent
    !session |> Option.iter (fun ses -> ses.Kill())
    if intent = Restart then session := Some (setupSession())
  
  let AddSourceToSelection selection =
     let stap = IdeApp.Workbench.ActiveDocument.Editor.SelectionRange.Offset
     let line = IdeApp.Workbench.ActiveDocument.Editor.OffsetToLineNumber(stap)
     let file = IdeApp.Workbench.ActiveDocument.FileName
     String.Format("# {0} \"{1}\"\n{2}" ,line,file.FullPath,selection)  
    
  //let handler = 
  do Debug.WriteLine ("InteractivePad: created!")
  #if DEBUG
  do view.Destroyed.Add (fun _ -> Debug.WriteLine ("Interactive: view destroyed"))
  do IdeApp.Exiting.Add (fun _ -> Debug.WriteLine ("Interactive: app exiting!!"))
  do IdeApp.Exited.Add  (fun _ -> Debug.WriteLine ("Interactive: app exited!!"))
  #endif

  let ensureCorrectDirectory() =
    if IdeApp.Workbench.ActiveDocument.FileName.FileName <> null then
      let path = Path.GetDirectoryName(IdeApp.Workbench.ActiveDocument.FileName.ToString())
      sendCommand ("#silentCd @\"" + path + "\";;")

  member x.Shutdown()  = 
    do Debug.WriteLine (sprintf "Interactive: x.Shutdown()!")
    resetFsi Kill
 
  interface MonoDevelop.Ide.Gui.IPadContent with
    member x.Dispose() =
      Debug.WriteLine ("Interactive: disposing pad...")
      x.Shutdown()

    member x.Control : Gtk.Widget = view :> Gtk.Widget
  
    member x.Initialize(container:MonoDevelop.Ide.Gui.IPadWindow) = 
      view.ConsoleInput.Add(fun cie -> if isPrompting then 
                                         isPrompting <- false
                                         session := None
                                         sendCommand ""
                                       else sendCommand cie.Text)
      view.Child.KeyPressEvent.Add(fun ea ->
        if ea.Event.State &&& ModifierType.ControlMask = ModifierType.ControlMask && ea.Event.Key = Key.period then
          !session |> Option.iter (fun s -> s.Interrupt()))

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
      
  member x.SendLine() = 
    if IdeApp.Workbench.ActiveDocument = null then () 
    else
      ensureCorrectDirectory()
      let line = IdeApp.Workbench.ActiveDocument.Editor.Caret.Line
      let text = IdeApp.Workbench.ActiveDocument.Editor.GetLineText(line)
      let file = IdeApp.Workbench.ActiveDocument.FileName
      let sel = String.Format("# {0} \"{1}\"\n{2}" ,line ,file.FullPath,text) 
      sendCommand sel

  member x.IsSelectionNonEmpty = 
    if IdeApp.Workbench.ActiveDocument = null || 
       IdeApp.Workbench.ActiveDocument.FileName.FileName = null then false  
    else
      let sel = IdeApp.Workbench.ActiveDocument.Editor.SelectedText
      not(String.IsNullOrEmpty(sel))
    
  member x.IsInsideFSharpFile = 
    if IdeApp.Workbench.ActiveDocument = null ||
       IdeApp.Workbench.ActiveDocument.FileName.FileName = null then false
    else
      let file = IdeApp.Workbench.ActiveDocument.FileName.ToString()
      CompilerArguments.supportedExtension(Path.GetExtension(file))
      
  member x.LoadReferences() =
    Debug.WriteLine("FSI:  #LoadReferences")
    let project = IdeApp.Workbench.ActiveDocument.Project :?> DotNetProject

    let references = project.GetReferencedAssemblies(ConfigurationSelector.Default, true)
                     |> Seq.filter (fun r ->  not <| (r.Contains "mscorlib.dll" || r.Contains "FSharp.Core.dll") )
                     |> Seq.toArray
    
    let orderReferences = FSharp.CompilerBinding.OrderAssemblyReferences()
    let references = orderReferences.Order references
    sendCommand references
      
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

type SendSelection() =
  inherit CommandHandler()
  override x.Run() =
    Debug.WriteLine (sprintf "Interactive: Send selection to F# interactive invoked!")
    FSharpInteractivePad.CurrentFsi.SendSelection()
    FSharpInteractivePad.CurrentPad.BringToFront(false)
  override x.Update(info:CommandInfo) =
    let fsi = FSharpInteractivePad.CurrentFsi
    info.Enabled <- fsi.IsSelectionNonEmpty
    info.Visible <- fsi.IsInsideFSharpFile

type SendLine() =
  inherit CommandHandler()
  override x.Run() =
    Debug.WriteLine (sprintf "Interactive: Send line to F# interactive invoked!")
    FSharpInteractivePad.CurrentFsi.SendLine()
    FSharpInteractivePad.CurrentPad.BringToFront(false)
  override x.Update(info:CommandInfo) =
    let fsi = FSharpInteractivePad.CurrentFsi
    info.Enabled <- true
    info.Visible <- fsi.IsInsideFSharpFile
    
type SendReferences() =
  inherit CommandHandler()
  override x.Run() =
    Debug.WriteLine (sprintf "Interactive: Load references in F# interactive invoked!")
    FSharpInteractivePad.CurrentFsi.LoadReferences()
    FSharpInteractivePad.CurrentPad.BringToFront(false)
  override x.Update(info:CommandInfo) =
    let fsi = FSharpInteractivePad.CurrentFsi
    info.Enabled <- true
    info.Visible <- fsi.IsInsideFSharpFile
