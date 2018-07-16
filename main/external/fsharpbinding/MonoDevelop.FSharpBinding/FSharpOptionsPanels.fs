// --------------------------------------------------------------------------------------
// User interface panels for F# project properties
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open Gtk
open System
open System.IO
open MonoDevelop.Components
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui.Dialogs
open MonoDevelop.FSharp.Gui

// --------------------------------------------------------------------------------------
// F# build options - compiler configuration panel
// --------------------------------------------------------------------------------------
module Settings =
    let fscPath = "FSharpBinding.FscPath"
    let fsiAdvanceToNextLine = "FSharpBinding.AdvanceToNextLine"
    let highlightMutables = "FSharpBinding.HighlightMutables"
    let showTypeSignatures = "FSharpBinding.ShowTypeSignatures"
    let showStatusBarTooltips = "FSharpBinding.ShowStatusBarTooltips"

type FSharpSettingsPanel() =
    inherit OptionsPanel()

    let mutable widget : FSharpSettingsWidget = null

    // NOTE: This setting is only relevant when xbuild is not being used.
    let setCompilerDisplay (use_default:bool) =
        if widget.CheckCompilerUseDefault.Active <> use_default then
            widget.CheckCompilerUseDefault.Active <- use_default
        let prop_compiler_path = PropertyService.Get (Settings.fscPath,"")
        let default_compiler_path = match CompilerArguments.getDefaultFSharpCompiler() with | Some(r) -> r | None -> ""
        widget.EntryCompilerPath.Text <- if use_default || prop_compiler_path = "" then default_compiler_path else prop_compiler_path
        widget.EntryCompilerPath.Sensitive <- not use_default

    override x.Dispose() =
        if widget <> null then widget.Dispose()

    override x.CreatePanelWidget() =
        widget <- new FSharpSettingsWidget()

        // Load current state
        let interactiveAdvanceToNextLine = PropertyService.Get (Settings.fsiAdvanceToNextLine, true)
        let compilerPath = PropertyService.Get (Settings.fscPath, "")
        let highlightMutables = PropertyService.Get (Settings.highlightMutables, false)
        let showTypeSignatures = PropertyService.Get (Settings.showTypeSignatures, false)
        let showStatusBarTooltips = PropertyService.Get (Settings.showStatusBarTooltips, true)

        setCompilerDisplay (compilerPath = "")

        widget.AdvanceLine.Active <- interactiveAdvanceToNextLine

        widget.CheckHighlightMutables.Active <- highlightMutables
        widget.CheckTypeSignatures.Active <- showTypeSignatures
        widget.CheckStatusBarTooltips.Active <- showStatusBarTooltips

        // Implement checkbox for F# Compiler options
        widget.CheckCompilerUseDefault.Toggled.Add (fun _ -> setCompilerDisplay widget.CheckCompilerUseDefault.Active)

        widget.Show()
        Control.op_Implicit widget

    override x.ApplyChanges() =
        PropertyService.Set (Settings.fscPath, if widget.CheckCompilerUseDefault.Active then null else widget.EntryCompilerPath.Text)
        PropertyService.Set (Settings.fsiAdvanceToNextLine, widget.AdvanceLine.Active)
        PropertyService.Set (Settings.highlightMutables, widget.CheckHighlightMutables.Active)
        PropertyService.Set (Settings.showTypeSignatures, widget.CheckTypeSignatures.Active)
        PropertyService.Set (Settings.showStatusBarTooltips, widget.CheckStatusBarTooltips.Active)

        IdeApp.Workbench.ReparseOpenDocuments()

// --------------------------------------------------------------------------------------
// F# build options - compiler configuration panel
// --------------------------------------------------------------------------------------

/// Configuration panel with settings for the F# compiler
/// (such as generation of debug symbols, XML, tail-calls etc.)
type CodeGenerationPanel() =
    inherit MultiConfigItemOptionsPanel()
    let mutable widget : FSharpCompilerOptionsWidget = null
    let mutable debugCheckedHandler = Unchecked.defaultof<IDisposable>

    let debugInformationToIndex (item:string) =
        match item.ToLower() with
        | "full" -> 0
        | "pdbonly" -> 1
        | _ -> 0

    let indexToDebugInformation i =
        match i with
        | 0 -> "full"
        | 1 -> "pdbonly"
        | _ -> "full"

    let platformInformationToIndex (item:string) =
        match item.ToLower() with
        | "anycpu" -> 0
        | "x86" -> 1
        | "x64" -> 2
        | "Itanium" -> 3
        | _ -> 0

    let indexToPlatformInformation i =
        match i with
        | 0 -> "anycpu"
        | 1 -> "x86"
        | 2 -> "x64"
        | 3 -> "Itanium"
        | _ -> "anycpu"

    override x.Dispose () =
        if widget <> null then
            widget.Dispose ()
        if debugCheckedHandler <> null then
            debugCheckedHandler.Dispose()

    override x.CreatePanelWidget() =
        widget <- new FSharpCompilerOptionsWidget ()
        debugCheckedHandler <- widget.CheckDebugInformation.Clicked.Subscribe(fun _ -> widget.ComboDebugInformation.Sensitive <- widget.CheckDebugInformation.Active )

        widget.Show ()
        Control.op_Implicit widget

    override x.LoadConfigData() =
        let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
        let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters

        widget.CheckOptimize.Active <- fsconfig.Optimize
        widget.CheckTailCalls.Active <- fsconfig.GenerateTailCalls
        widget.CheckXmlDocumentation.Active <- not (String.IsNullOrEmpty fsconfig.DocumentationFile)
        widget.EntryCommandLine.Text <- if String.IsNullOrWhiteSpace fsconfig.OtherFlags then "" else fsconfig.OtherFlags
        widget.EntryDefines.Text <- fsconfig.DefineConstants
        widget.ComboPlatforms.Active <- platformInformationToIndex fsconfig.PlatformTarget
        widget.CheckWarningsAsErrors.Active <- fsconfig.TreatWarningsAsErrors
        widget.WarningLevelSpinButton.Value <- float fsconfig.WarningLevel
        widget.EntryWarnings.Text <- if String.IsNullOrWhiteSpace fsconfig.NoWarn then "" else fsconfig.NoWarn

        if fsconfig.ParentConfiguration.DebugSymbols then
            widget.CheckDebugInformation.Active <- true
            widget.ComboDebugInformation.Sensitive <- true
            widget.ComboDebugInformation.Active <- debugInformationToIndex fsconfig.ParentConfiguration.DebugType

    override x.ApplyChanges () =
        let config = x.CurrentConfiguration :?> DotNetProjectConfiguration
        let fsconfig = config.CompilationParameters :?> FSharpCompilerParameters

        fsconfig.Optimize <- widget.CheckOptimize.Active
        fsconfig.GenerateTailCalls <- widget.CheckTailCalls.Active
        fsconfig.PlatformTarget <- indexToPlatformInformation widget.ComboPlatforms.Active
        fsconfig.TreatWarningsAsErrors <- widget.CheckWarningsAsErrors.Active
        fsconfig.WarningLevel <- widget.WarningLevelSpinButton.ValueAsInt
        fsconfig.NoWarn <- widget.EntryWarnings.Text

        if widget.CheckXmlDocumentation.Active then
            // We use '\' because that's what Visual Studio uses.
            // We use uppercase 'XML' because that's what Visual Studio does.
            // A shame to perpetuate that horror but cross-tool portability of project files without mucking with them needlessly is very nice.
            fsconfig.DocumentationFile <-
                config.OutputDirectory.ToRelative(x.ConfiguredProject.BaseDirectory).ToString().Replace("/","\\").TrimEnd('\\') + "\\" +
                Path.GetFileNameWithoutExtension(config.CompiledOutputName.ToString()) + ".XML"

        if not (String.IsNullOrWhiteSpace widget.EntryCommandLine.Text) then
            fsconfig.OtherFlags <- widget.EntryCommandLine.Text
        fsconfig.DefineConstants <- widget.EntryDefines.Text

        fsconfig.ParentConfiguration.DebugSymbols <- widget.CheckDebugInformation.Active
        fsconfig.ParentConfiguration.DebugType <- indexToDebugInformation widget.ComboDebugInformation.Active
