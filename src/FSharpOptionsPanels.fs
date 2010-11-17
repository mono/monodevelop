// --------------------------------------------------------------------------------------
// User interface panels for F# project properties
// --------------------------------------------------------------------------------------

namespace FSharp.MonoDevelop

open Gtk
open System
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui.Dialogs
open FSharp.MonoDevelop.Gui

// --------------------------------------------------------------------------------------
// F# build options - compiler configuration panel
// --------------------------------------------------------------------------------------

type FSharpSettingsPanel() = 
  inherit OptionsPanel()
  let mutable widget : FSharpSettingsWidget = null
  
  override x.CreatePanelWidget() =
    widget <- new FSharpSettingsWidget()
    
    // Implement "Browse.." button for F# Interactive path
    widget.ButtonBrowse.Clicked.Add(fun _ ->
      let args = [| box "Cancel"; box ResponseType.Cancel; box "Open"; box ResponseType.Accept |]
      use dlg = new FileChooserDialog("Browser for F# Interactive", null, FileChooserAction.Open, args)
      if dlg.Run() = int ResponseType.Accept then
        widget.EntryPath.Text <- dlg.Filename
      dlg.Hide() )
    
    // Load current state
    widget.EntryPath.Text <- PropertyService.Get<string>("FSharpBinding.FsiPath", "")
    widget.EntryArguments.Text <- PropertyService.Get<string>("FSharpBinding.FsiArguments", "")
    let fontName = MonoDevelop.Ide.DesktopService.DefaultMonospaceFont
    widget.FontInteractive.FontName <- PropertyService.Get<string>("FSharpBinding.FsiFontName", fontName)
    widget.Show()
    upcast widget 
  
  override x.ApplyChanges() =
    let origFsi = PropertyService.Get<string>("FSharpBinding.FsiPath", "")
    let origArgs = PropertyService.Get<string>("FSharpBinding.FsiArguments", "")
    PropertyService.Set("FSharpBinding.FsiPath", widget.EntryPath.Text)
    PropertyService.Set("FSharpBinding.FsiArguments", widget.EntryArguments.Text)
    PropertyService.Set("FSharpBinding.FsiFontName", widget.FontInteractive.FontName)
    FSharpInteractivePad.CurrentFsi.UpdateFont()    
    if origFsi <> widget.EntryPath.Text || origArgs <> widget.EntryArguments.Text then
      FSharpInteractivePad.CurrentFsi.RestartFsi()
    
// --------------------------------------------------------------------------------------
// F# build options - compiler configuration panel
// --------------------------------------------------------------------------------------

/// Configuration panel with settings for the F# compiler 
/// (such as generation of debug symbols, XML, tail-calls etc.)
type CodeGenerationPanel() = 
  inherit MultiConfigItemOptionsPanel()
  let mutable widget : FSharpCompilerOptionsWidget = null
  
  override x.CreatePanelWidget() =
    widget <- new FSharpCompilerOptionsWidget()
    widget.Show()
    upcast widget 
  
  override x.LoadConfigData() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters
    
    widget.CheckDebugInfo.Active <- fsconfig.GenerateDebugInfo
    widget.CheckOptimize.Active <- fsconfig.OptimizeCode
    widget.CheckTailCalls.Active <- fsconfig.GenerateTailCalls
    widget.CheckXmlDocumentation.Active <- fsconfig.GenerateXmlDoc
    widget.EntryCommandLine.Text <- fsconfig.CustomCommandLine
    widget.EntryDefines.Text <- fsconfig.DefinedSymbols
  
  override x.ApplyChanges() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters

    fsconfig.GenerateDebugInfo <- widget.CheckDebugInfo.Active
    fsconfig.OptimizeCode <- widget.CheckOptimize.Active
    fsconfig.GenerateTailCalls <- widget.CheckTailCalls.Active
    fsconfig.GenerateXmlDoc <- widget.CheckXmlDocumentation.Active
    fsconfig.CustomCommandLine <- widget.EntryCommandLine.Text
    fsconfig.DefinedSymbols <- widget.EntryDefines.Text


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
      
  override x.LoadConfigData() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.ProjectParameters :?> FSharpProjectParameters
    let store = ref null
    initializeTreeView()    
    
    // Get sources, drop common prefix (directory) and add them to the list
    let sources = Common.getSourceFiles x.ConfiguredProject.Items
    let root = System.IO.Path.GetDirectoryName(x.ConfiguredProject.FileName.FullPath.ToString())
    fileOrder <- Common.getItemsInOrder root sources fsconfig.BuildOrder true |> Seq.toArray
    
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