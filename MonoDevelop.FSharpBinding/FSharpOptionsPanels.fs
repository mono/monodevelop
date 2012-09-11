// --------------------------------------------------------------------------------------
// User interface panels for F# project properties
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open Gtk
open System
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui.Dialogs
open MonoDevelop.FSharp.Gui

// --------------------------------------------------------------------------------------
// F# build options - compiler configuration panel
// --------------------------------------------------------------------------------------

type FSharpSettingsPanel() = 
  inherit OptionsPanel()
  let fscPathPropName = "FSharpBinding.FscPath"
  let fsiPathPropName = "FSharpBinding.FsiPath"
  let fsiArgumentsPropName = "FSharpBinding.FsiArguments"
  let enableFSharp30PropName = "FSharpBinding.EnableFSharp30"
  let fsiFontNamePropName = "FSharpBinding.FsiFontName"
  let mutable widget : FSharpSettingsWidget = null
  
  member internal x.setLanguageDisplay(enable:bool) = 
    if widget.EnableFSharp30.Active <> enable then
      widget.EnableFSharp30.Active <- enable

  member internal x.setCompilerDisplay(use_default:bool) = 
    if widget.CheckCompilerUseDefault.Active <> use_default then
      widget.CheckCompilerUseDefault.Active <- use_default
    let prop_compiler_path = PropertyService.Get<string>(fscPathPropName,"")
    let default_compiler_path = match CompilerArguments.getDefaultDefaultCompiler() with | Some(r) -> r | None -> ""
    widget.EntryCompilerPath.Text <- if use_default || prop_compiler_path = "" then default_compiler_path else prop_compiler_path
    widget.EntryCompilerPath.Sensitive <- not use_default
    widget.ButtonCompilerBrowse.Sensitive <- not use_default

  member internal x.setInteractiveDisplay(use_default:bool) =
    if widget.CheckInteractiveUseDefault.Active <> use_default then
      widget.CheckInteractiveUseDefault.Active <- use_default
    let prop_interp_path = PropertyService.Get<string>(fsiPathPropName, "")
    let prop_interp_args = PropertyService.Get<string>(fsiArgumentsPropName, "")
    let default_interp_path = match CompilerArguments.getDefaultInteractive() with | Some(r) -> r | None -> ""
    let default_interp_args = ""
    widget.EntryPath.Text <- if use_default || prop_interp_path = "" then default_interp_path else prop_interp_path
    widget.EntryArguments.Text <- if use_default || prop_interp_args = "" then default_interp_args else prop_interp_args
    widget.EntryPath.Sensitive <- not use_default
    widget.ButtonBrowse.Sensitive <- not use_default
    widget.EntryArguments.Sensitive <- not use_default
  override x.Dispose() =
    if widget <> null then
      widget.Dispose()

  override x.CreatePanelWidget() =
    widget <- new FSharpSettingsWidget()
  
    // Implement "Browse.." button for F# Interactive path
    widget.ButtonBrowse.Clicked.Add(fun _ ->
      let args = [| box "Cancel"; box ResponseType.Cancel; box "Open"; box ResponseType.Accept |]
      use dlg = new FileChooserDialog("Browser for F# Interactive", null, FileChooserAction.Open, args)
      if dlg.Run() = int ResponseType.Accept then
        widget.EntryPath.Text <- dlg.Filename
      dlg.Hide() )

    // Implement "Browse..." button for F# Compiler path
    widget.ButtonCompilerBrowse.Clicked.Add(fun _ ->
      let args = [| box "Cancel"; box ResponseType.Cancel; box "Open"; box ResponseType.Accept |]
      use dlg = new FileChooserDialog("Broser for F# Compiler", null, FileChooserAction.Open, args)
      if dlg.Run() = int ResponseType.Accept then
        widget.EntryCompilerPath.Text <- dlg.Filename
      dlg.Hide() )

    // Load current state
    let prop_interp_path = PropertyService.Get<string>(fsiPathPropName, "")
    let prop_interp_args = PropertyService.Get<string>(fsiArgumentsPropName, "")
    let prop_interp_font = PropertyService.Get<string>(fsiFontNamePropName,"")  
    let prop_compiler_path = PropertyService.Get<string>(fscPathPropName,"")
    let prop_use_fsharp30 = PropertyService.Get<string>(enableFSharp30PropName,"")

    let default_interp_path = CompilerArguments.getDefaultInteractive
    let default_interp_args = ""
    let default_interp_font = MonoDevelop.Ide.DesktopService.DefaultMonospaceFont

    x.setInteractiveDisplay(prop_interp_path = "" && prop_interp_args = "")
    x.setCompilerDisplay( (prop_compiler_path = "") )
    x.setLanguageDisplay( System.String.Compare (prop_use_fsharp30, "true", true) = 0)
    
    let fontName = MonoDevelop.Ide.DesktopService.DefaultMonospaceFont
    widget.FontInteractive.FontName <- PropertyService.Get<string>(fsiFontNamePropName, fontName)

    // Implement checkbox for F# Interactive options
    widget.CheckInteractiveUseDefault.Toggled.Add(fun _ -> 
        x.setInteractiveDisplay(widget.CheckInteractiveUseDefault.Active))

    // Implement checkbox for F# Compiler options
    widget.CheckCompilerUseDefault.Toggled.Add(fun _ -> 
        x.setCompilerDisplay(widget.CheckCompilerUseDefault.Active))

    // Toggling the language version can affect the compiler locations
    widget.EnableFSharp30.Toggled.Add(fun _ -> 
        // Apply the property immediately, to reflect changes in default compiler paths 
        PropertyService.Set(enableFSharp30PropName, if widget.EnableFSharp30.Active then "true" else "false")
        x.setInteractiveDisplay(widget.CheckInteractiveUseDefault.Active)
        x.setCompilerDisplay(widget.CheckCompilerUseDefault.Active))
    
    widget.Show()
    upcast widget 
  
  override x.ApplyChanges() =
    PropertyService.Set(enableFSharp30PropName, if widget.EnableFSharp30.Active then "true" else "false")

    PropertyService.Set(fscPathPropName, if widget.CheckCompilerUseDefault.Active then null else widget.EntryCompilerPath.Text)

    PropertyService.Set(fsiPathPropName, if widget.CheckInteractiveUseDefault.Active then null else widget.EntryPath.Text)
    PropertyService.Set(fsiArgumentsPropName, if widget.CheckInteractiveUseDefault.Active then null else widget.EntryArguments.Text)

    PropertyService.Set(fsiFontNamePropName, widget.FontInteractive.FontName)
    FSharpInteractivePad.CurrentFsi.UpdateFont()    
    
// --------------------------------------------------------------------------------------
// F# build options - compiler configuration panel
// --------------------------------------------------------------------------------------

/// Configuration panel with settings for the F# compiler 
/// (such as generation of debug symbols, XML, tail-calls etc.)
type CodeGenerationPanel() = 
  inherit MultiConfigItemOptionsPanel()
  let mutable widget : FSharpCompilerOptionsWidget = null
  
  override x.Dispose() =
    if widget <> null then
      widget.Dispose()

  override x.CreatePanelWidget() =
    widget <- new FSharpCompilerOptionsWidget()
    widget.Show()
    upcast widget 
  
  override x.LoadConfigData() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters
    
    widget.CheckDebugInfo.Active <- fsconfig.DebugSymbols
    widget.CheckOptimize.Active <- fsconfig.Optimize
    widget.CheckTailCalls.Active <- fsconfig.GenerateTailCalls
    widget.CheckXmlDocumentation.Active <- not (String.IsNullOrEmpty fsconfig.DocumentationFile)
    widget.EntryCommandLine.Text <- fsconfig.OtherFlags
    widget.EntryDefines.Text <- fsconfig.DefineConstants
  
  override x.ApplyChanges() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters

    fsconfig.DebugSymbols <- widget.CheckDebugInfo.Active
    fsconfig.Optimize <- widget.CheckOptimize.Active
    fsconfig.GenerateTailCalls <- widget.CheckTailCalls.Active
    fsconfig.DocumentationFile <- 
        if widget.CheckXmlDocumentation.Active then 
           System.IO.Path.GetFileNameWithoutExtension(config.CompiledOutputName.ToString())+".xml" 
        else 
           ""
    fsconfig.OtherFlags <- widget.EntryCommandLine.Text
    fsconfig.DefineConstants <- widget.EntryDefines.Text


// --------------------------------------------------------------------------------------
// F# project options - build order panel
// --------------------------------------------------------------------------------------

/// Panel that allows the user to specify build order of F# files in the project
/// (the order is stored separately from the MSBUILD file, because MonoDevelop
/// doesn't provide any way to modify the project file itself).
type BuildOrderPanel() = 
  inherit MultiConfigItemOptionsPanel()
  
  let mutable widget : FSharpBuildOrderWidget = null
  let mutable fileOrder : string[] = [| |]

  /// Initialize tree view component (that shows a list of files)
  let initializeTreeView() = 
    let col = new TreeViewColumn(Title = "Item name")
    widget.ListItems.AppendColumn(col) |> ignore
    let cell = new Gtk.CellRendererText();
    col.PackStart(cell, true);
    col.AddAttribute(cell, "text", 0);    
  
  override x.Dispose() =
    if widget <> null then
      widget.Dispose()
      
  override x.LoadConfigData() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.ProjectParameters :?> FSharpProjectParameters
    let store = ref null
    initializeTreeView()    
    
    // Get sources, drop common prefix (directory) and add them to the list
    let sources = CompilerArguments.getSourceFiles x.ConfiguredProject.Items
    let root = System.IO.Path.GetDirectoryName(x.ConfiguredProject.FileName.FullPath.ToString())
    fileOrder <- CompilerArguments.getItemsInOrder root sources fsconfig.BuildOrder true |> Seq.toArray
    
    // Re-generate items in the tree view list
    let updateStore () =
      store := new Gtk.ListStore [| typeof<string> |]
      widget.ListItems.Model <- !store
      for file in fileOrder do
        store.Value.AppendValues [| file |] |> ignore

    // Event handlers for reordering of files      
    Event.merge
      (widget.ButtonUp.Clicked |> Event.map (fun _ -> -1))
      (widget.ButtonDown.Clicked |> Event.map (fun _ -> 1))
    |> Event.add (fun change ->
      let succ, _, iter = widget.ListItems.Selection.GetSelected() 
      if succ then 
        let fname = store.Value.GetValue(iter, 0) :?> string
        let index = fileOrder |> Array.findIndex ((=) fname)
        if index + change >= 0 && index + change < fileOrder.Length then
          // Swap items and update the store
          let tmp = fileOrder.[index]
          fileOrder.[index] <- fileOrder.[index + change]
          fileOrder.[index + change] <- tmp
          
          // Update the store and select the original item
          updateStore() 
          let mutable iter = Unchecked.defaultof<_>
          if store.Value.IterNthChild(&iter, index + change) then
            widget.ListItems.Selection.SelectIter(iter)
          )
  
    // Update displayed items at the beginning
    updateStore()
    
  override x.ApplyChanges() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.ProjectParameters :?> FSharpProjectParameters
    fsconfig.BuildOrder <- fileOrder
    
  override x.CreatePanelWidget() =
    widget <- new FSharpBuildOrderWidget()
    widget.Show()
    upcast widget 
