// --------------------------------------------------------------------------------------
// Main file - contains types that call F# compiler service in the background, display
// error messages and expose various methods for to be used from MonoDevelop integration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp
#nowarn "40"

open System
open System.IO
open System.Xml
open System.Text
open System.Diagnostics
open Mono.TextEditor
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Core
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

module MonoDevelop =
    type TextEditor with
        member x.GetLineInfoFromOffset (offset) =
            let loc  = x.OffsetToLocation(offset)
            let line, col = max loc.Line 1, loc.Column-1
            let currentLine = x.GetLineByOffset(offset)
            let lineStr = x.Text.Substring(currentLine.Offset, currentLine.EndOffset - currentLine.Offset)
            line, col, lineStr

        member x.GetLineInfoByCaretOffset () =
            x.GetLineInfoFromOffset x.CaretOffset

    type MonoDevelop.Ide.TypeSystem.ParsedDocument with
        member x.TryGetAst() =
            match x.Ast with
            | null -> None
            | :? FSharp.CompilerBinding.ParseAndCheckResults as ast
                -> Some ast
            | _ -> None

    let internal getConfig () =
        match MonoDevelop.Ide.IdeApp.Workspace with
        | null -> MonoDevelop.Projects.ConfigurationSelector.Default
        | ws ->
           match ws.ActiveConfiguration with
           | null -> MonoDevelop.Projects.ConfigurationSelector.Default
           | config -> config

/// Provides functionality for working with the F# interactive checker running in background
type MDLanguageService() =
  /// Single instance of the language service. We don't want the VFS during tests, so set it to blank from tests
  /// before Instance is evaluated
  static let mutable vfs =
      lazy (let originalFs = Shim.FileSystem
            let fs = new FileSystem(originalFs, (fun () -> seq { yield! IdeApp.Workbench.Documents }))
            Shim.FileSystem <- fs
            fs :> IFileSystem)

  static let mutable instance =
    lazy
        let _ = vfs.Force()
        new FSharp.CompilerBinding.LanguageService(
            (fun changedfile ->
                try
                    let doc = IdeApp.Workbench.ActiveDocument
                    if doc <> null && doc.FileName.FullPath.ToString() = changedfile then
                        LoggingService.LogInfo("FSharp Language Service: Compiler notifying document '{0}' is dirty and needs reparsing.  Reparsing as its the active document.", (Path.GetFileName changedfile))
                        doc.ReparseDocument()
                with exn  -> 
                   LoggingService.LogInfo("FSharp Language Service: Error while attempting to notify document '{0}' needs reparsing", (Path.GetFileName changedfile), exn) ))

  static member Instance with get () = instance.Force ()
                         and  set v  = instance <- lazy v
  // Call this before Instance is called
  static member DisableVirtualFileSystem() =
        vfs <- lazy (Shim.FileSystem)
        
  static member SupportedFileExtensions =
    [".fsscript"; ".fs"; ".fsx"; ".fsi"; ".sketchfs"]

  /// Is the specified extension supported F# file?
  static member SupportedFileName fileName =
    let ext = Path.GetExtension(fileName).ToLower()
    MDLanguageService.SupportedFileExtensions
    |> List.exists ((=) ext)
    
  static member SupportedFilePath (filePath:FilePath) = 
      MDLanguageService.SupportedFileName (filePath.ToString())

/// Various utilities for working with F# language service
module internal ServiceUtils =
  let map =
    [ 0x0000,  "md-class"
      0x0003,  "md-enum"
      0x00012, "md-struct"
      0x00018, "md-struct" (* value type *)
      0x0002,  "md-delegate"
      0x0008,  "md-interface"
      0x000e,  "md-module" (* module *)
      0x000f,  "md-name-space"
      0x000c,  "md-method";
      0x000d,  "md-extensionmethod" (* method2 ? *)
      0x00011, "md-property"
      0x0005,  "md-event"
      0x0007,  "md-field" (* fieldblue ? *)
      0x0020,  "md-field" (* fieldyellow ? *)
      0x0001,  "md-field" (* const *)
      0x0004,  "md-property" (* enummember *)
      0x0006,  "md-exception" (* exception *)
      0x0009,  "md-text-file-icon" (* TextLine *)
      0x000a,  "md-regular-file" (* Script *)
      0x000b,  "Script" (* Script2 *)
      0x0010,  "md-tip-of-the-day" (* Formula *);
      0x00013, "md-class" (* Template *)
      0x00014, "md-class" (* Typedef *)
      0x00015, "md-type" (* Type *)
      0x00016, "md-type" (* Union *)
      0x00017, "md-field" (* Variable *)
      0x00019, "md-class" (* Intrinsic *)
      0x0001f, "md-breakpint" (* error *)
      0x00021, "md-misc-files" (* Misc1 *)
      0x0022,  "md-misc-files" (* Misc2 *)
      0x00023, "md-misc-files" (* Misc3 *) ] |> Map.ofSeq

  /// Translates icon code that we get from F# language service into a MonoDevelop icon
  let getIcon glyph =
    match map.TryFind (glyph / 6), map.TryFind (glyph % 6) with
    | Some(s), _ -> s // Is the second number good for anything?
    | _, _ -> "md-breakpoint"
