// --------------------------------------------------------------------------------------
// Main file - contains types that call F# compiler service in the background, display
// error messages and expose various methods for to be used from MonoDevelop integration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp
#nowarn "40"

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.Xml
open System.Text
open System.Diagnostics
open ExtCore.Control
open Mono.TextEditor
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Core
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library
open MonoDevelop.Ide.TypeSystem

module MonoDevelop =

    type TextEditor with
        member x.GetLineInfoFromOffset (offset) =
            let loc  = x.OffsetToLocation(offset)
            let line, col = max loc.Line 1, loc.Column - 1
            let currentLine = x.GetLineByOffset(offset)
            let lineStr = x.Text.Substring(currentLine.Offset, currentLine.EndOffset - currentLine.Offset)
            line, col, lineStr

        member x.GetLineInfoByCaretOffset () =
            x.GetLineInfoFromOffset x.CaretOffset
           
    let inline private (>>=) a b = Option.bind b a
    let inline private (!) a = Option.ofNull a
    
    type TypeSystem.ParsedDocument with
        member x.TryGetAst() =
            match x.Ast with
            | null -> None
            | :? ParseAndCheckResults as ast
                -> Some ast
            | _ -> None

    type DocumentContext with
        member x.TryGetParsedDocument() =
            !x >>=(fun pd -> !pd.ParsedDocument)
            
        member x.TryGetAst() =
            x.TryGetParsedDocument() >>= (fun pd -> pd.TryGetAst())

        member x.TryGetCheckResults() =
            x.TryGetAst() >>= (fun ast -> ast.CheckResults)

    let internal getConfig () =
        match IdeApp.Workspace with
        | null -> ConfigurationSelector.Default
        | ws ->
           match ws.ActiveConfiguration with
           | null -> ConfigurationSelector.Default
           | config -> config


    let visibleDocuments() =
        IdeApp.Workbench.Documents
        |> Seq.filter (fun doc -> match doc.Window with
                                  | :? Gtk.Widget as w -> w.HasScreen
                                  | _ -> false )

    let isDocumentVisible filename =
        visibleDocuments()
        |> Seq.exists (fun d -> d.FileName.ToString() = filename)

    let tryGetVisibleDocument filename =
        visibleDocuments()
        |> Seq.tryFind (fun d -> d.FileName.ToString() = filename)


/// Provides functionality for working with the F# interactive checker running in background
type MDLanguageService() =
  /// Single instance of the language service. We don't want the VFS during tests, so set it to blank from tests
  /// before Instance is evaluated
  static let mutable vfs =
      lazy (let originalFs = Shim.FileSystem
            let fs = new FileSystem(originalFs, (fun () -> seq { yield! match IdeApp.Workbench with
                                                                        | null -> Seq.empty.ToImmutableList()
                                                                        | _ -> IdeApp.Workbench.Documents }))
            Shim.FileSystem <- fs
            fs :> IFileSystem)

  static let mutable instance =
    lazy
        let _ = vfs.Force()
        // VisualFSharp sets extraProjectInfo to be the Roslyn Workspace
        // object, but we don't have that level of integration yet
        let extraProjectInfo = None 
        new LanguageService(
            (fun (changedfile, _) ->
                try
                    let doc = IdeApp.Workbench.ActiveDocument
                    if doc <> null && doc.FileName.FullPath.ToString() = changedfile then
                        LoggingService.logDebug "FSharp Language Service: Compiler notifying document '%s' is dirty and needs reparsing.  Reparsing as its the active document." (Path.GetFileName changedfile)
                with exn  ->
                   LoggingService.logDebug "FSharp Language Service: Error while attempting to notify document '%s' needs reparsing\n%s" (Path.GetFileName changedfile) (exn.ToString()) ), extraProjectInfo)

  static member Instance with get () = instance.Force ()
                         and  set v  = instance <- lazy v
  // Call this before Instance is called
  static member DisableVirtualFileSystem() =
      vfs <- lazy (Shim.FileSystem)

  static member invalidateProjectFile(projectFile: FilePath) =
      try
          if File.Exists (projectFile.FullPath.ToString()) then
              MDLanguageService.Instance.TryGetProjectCheckerOptionsFromCache(projectFile.FullPath.ToString(), [("Configuration", IdeApp.Workspace.ActiveConfigurationId)])
              |> Option.iter(fun options ->
                  MDLanguageService.Instance.InvalidateConfiguration(options)
                  MDLanguageService.Instance.ClearProjectInfoCache())
      with ex -> LoggingService.LogError ("Could not invalidate configuration", ex)

  static member invalidateFiles (args:#ProjectFileEventInfo seq) =
      for projectFileEvent in args do
          if FileService.supportedFilePath projectFileEvent.ProjectFile.FilePath then
              MDLanguageService.invalidateProjectFile(projectFileEvent.ProjectFile.FilePath)
[<AutoOpen>]
module MDLanguageServiceImpl =
    let languageService = MDLanguageService.Instance

/// Various utilities for working with F# language service
module internal ServiceUtils =
    /// Translates icon code that we get from F# language service into a MonoDevelop icon
    let getIcon (navItem: FSharpNavigationDeclarationItem) =
        match navItem.Kind with
        | NamespaceDecl -> "md-name-space"
        | _ ->
            match navItem.Glyph with
            | FSharpGlyph.Class -> "md-class"
            | FSharpGlyph.Enum -> "md-enum"
            | FSharpGlyph.Struct -> "md-struct"
            | FSharpGlyph.ExtensionMethod -> "md-struct"
            | FSharpGlyph.Delegate -> "md-delegate"
            | FSharpGlyph.Interface -> "md-interface"
            | FSharpGlyph.Module -> "md-module"
            | FSharpGlyph.NameSpace -> "md-name-space"
            | FSharpGlyph.Method -> "md-method";
            | FSharpGlyph.OverridenMethod -> "md-method";
            | FSharpGlyph.Property -> "md-property"
            | FSharpGlyph.Event -> "md-event"
            | FSharpGlyph.Constant -> "md-field"
            | FSharpGlyph.EnumMember -> "md-field"
            | FSharpGlyph.Exception -> "md-exception"
            | FSharpGlyph.Typedef -> "md-class"
            | FSharpGlyph.Type -> "md-type"
            | FSharpGlyph.Union -> "md-type"
            | FSharpGlyph.Variable -> "md-field"
            | FSharpGlyph.Field -> "md-field"
            | FSharpGlyph.Error -> "md-breakpint"

module internal KeywordList =
    let modifiers = 
        dict [
            "abstract",  """Indicates a method that either has no implementation in the type in which it is declared or that is virtual and has a default implementation."""
            "inline",  """Used to indicate a function that should be integrated directly into the caller's code."""
            "mutable",  """Used to declare a variable, that is, a value that can be changed."""
            "private",  """Restricts access to a member to code in the same type or module."""
            "public",  """Allows access to a member from outside the type."""
        ]
