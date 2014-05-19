// --------------------------------------------------------------------------------------
// User interface panels for F# project properties
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open Gtk
open Gdk
open System
open System.IO
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
  let fsiFontNamePropName = "FSharpBinding.FsiFontName"
  let fsiBaseColorPropName ="FSharpBinding.BaseColorPropName"
  let fsiTextColorPropName ="FSharpBinding.TextColorPropName"
  let fsiMatchWitThemePropName = "FSharpBinding.MatchWitThemePropName"
  
  let mutable widget : FSharpSettingsWidget = null

  // NOTE: This setting is only relevant when xbuild is not being used.
  member internal x.setCompilerDisplay(use_default:bool) = 
    if widget.CheckCompilerUseDefault.Active <> use_default then
      widget.CheckCompilerUseDefault.Active <- use_default
    let prop_compiler_path = PropertyService.Get<string>(fscPathPropName,"")
    let default_compiler_path = match CompilerArguments.getDefaultFSharpCompiler() with | Some(r) -> r | None -> ""
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
    let default_interp_path = CompilerArguments.getDefaultInteractive
    let default_interp_args = ""

    x.setInteractiveDisplay(prop_interp_path = "" && prop_interp_args = "")
    x.setCompilerDisplay( (prop_compiler_path = "") )

    let fontName = MonoDevelop.Ide.Fonts.FontService.MonospaceFont.Family
    widget.FontInteractive.FontName <- PropertyService.Get<string>(fsiFontNamePropName, fontName)
    
        
    //fsi colors
    widget.MatchThemeCheckBox.Clicked.Add(fun _ -> 
                                            if(widget.MatchThemeCheckBox.Active) then // there may be a race condition here.
                                                widget.ColorsHBox.Hide()
                                            else
                                                widget.ColorsHBox.Show())
    
    let (_, matchWithTheme) = PropertyService.Get<string>(fsiMatchWitThemePropName, "false")
                              |> System.Boolean.TryParse
                            
    if matchWithTheme then widget.ColorsHBox.Hide()
    else widget.ColorsHBox.Show()                            
    
    widget.MatchThemeCheckBox.Active <- matchWithTheme
    
    let textColor = PropertyService.Get<string>(fsiTextColorPropName, "#000000") |> strToColor
    widget.TextColorButton.Color <- textColor
    
    let baseColor = PropertyService.Get<string>(fsiBaseColorPropName, "#FFFFFF") |> strToColor
    widget.BaseColorButton.Color <- baseColor

    // Implement checkbox for F# Interactive options
    widget.CheckInteractiveUseDefault.Toggled.Add(fun _ -> 
        x.setInteractiveDisplay(widget.CheckInteractiveUseDefault.Active))

    // Implement checkbox for F# Compiler options
    widget.CheckCompilerUseDefault.Toggled.Add(fun _ -> 
        x.setCompilerDisplay(widget.CheckCompilerUseDefault.Active))
    
    widget.Show()
    upcast widget 
  
  override x.ApplyChanges() =
    PropertyService.Set(fscPathPropName, if widget.CheckCompilerUseDefault.Active then null else widget.EntryCompilerPath.Text)

    PropertyService.Set(fsiPathPropName, if widget.CheckInteractiveUseDefault.Active then null else widget.EntryPath.Text)
    PropertyService.Set(fsiArgumentsPropName, if widget.CheckInteractiveUseDefault.Active then null else widget.EntryArguments.Text)

    PropertyService.Set(fsiFontNamePropName, widget.FontInteractive.FontName)
    PropertyService.Set(fsiBaseColorPropName, widget.BaseColorButton.Color |> colorToStr)
    PropertyService.Set(fsiTextColorPropName, widget.TextColorButton.Color |> colorToStr)
    
    let matchWithTheme = if widget.MatchThemeCheckBox.Active then "true" else "false"
    PropertyService.Set(fsiMatchWitThemePropName, matchWithTheme)

    FSharpInteractivePad.Fsi |> Option.iter (fun fsi -> fsi.UpdateFont()    
                                                        fsi.UpdateColors())

    
    
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
    widget.EntryCommandLine.Text <- if (String.IsNullOrWhiteSpace fsconfig.OtherFlags) then "" else fsconfig.OtherFlags
    widget.EntryDefines.Text <- fsconfig.DefineConstants
  
  override x.ApplyChanges() =
    let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
    let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters

    fsconfig.DebugSymbols <- widget.CheckDebugInfo.Active
    fsconfig.Optimize <- widget.CheckOptimize.Active
    fsconfig.GenerateTailCalls <- widget.CheckTailCalls.Active
    fsconfig.DocumentationFile <- 
        if widget.CheckXmlDocumentation.Active then 
           // We use '\' because that's what Visual Studio uses.
           //
           // We use uppercase 'XML' because that's what Visual Studio does.
           // A shame to perpetuate that horror but cross-tool portability of project
           // files without mucking with them needlessly is very nice.
           config.OutputDirectory.ToRelative(x.ConfiguredProject.BaseDirectory).ToString().Replace("/","\\").TrimEnd('\\') 
              + "\\" + Path.GetFileNameWithoutExtension(config.CompiledOutputName.ToString())+".XML" 
        else 
           null
    fsconfig.OtherFlags <- if (String.IsNullOrWhiteSpace widget.EntryCommandLine.Text) then null else widget.EntryCommandLine.Text
    fsconfig.DefineConstants <- widget.EntryDefines.Text


