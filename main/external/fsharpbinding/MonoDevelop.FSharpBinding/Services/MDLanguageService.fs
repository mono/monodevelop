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
          if FileService.supportedFileName (projectFileEvent.ProjectFile.FilePath.ToString()) then
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

    let keywordDescriptions =
        dict [
            "abstract",  """Indicates a method that either has no implementation in the type in which it is declared or that is virtual and has a default implementation."""
            "and",  """Used in mutually recursive bindings, in property declarations, and with multiple constraints on generic parameters."""
            "as",  """Used to give the current class object an object name. Also used to give a name to a whole pattern within a pattern match."""
            "assert",  """Used to verify code during debugging."""
            "base",  """Used as the name of the base class object."""
            "begin",  """In verbose syntax, indicates the start of a code block."""
            "class",  """In verbose syntax, indicates the start of a class definition."""
            "default",  """Indicates an implementation of an abstract method; used together with an abstract method declaration to create a virtual method."""
            "delegate",  """Used to declare a delegate."""
            "do",  """Used in looping constructs or to execute imperative code."""
            "done",  """In verbose syntax, indicates the end of a block of code in a looping expression."""
            "downcast",  """Used to convert to a type that is lower in the inheritance chain."""
            "downto",  """In a for expression, used when counting in reverse."""
            "elif",  """Used in conditional branching. A short form of else if."""
            "else",  """Used in conditional branching."""
            "end",  """In type definitions and type extensions, indicates the end of a section of member definitions.
In verbose syntax, used to specify the end of a code block that starts with the begin keyword."""
            "exception",  """Used to declare an exception type."""
            "extern",  """Indicates that a declared program element is defined in another binary or assembly."""
            "false",  """Used as a Boolean literal."""
            "finally",  """Used together with try to introduce a block of code that executes regardless of whether an exception occurs."""
            "for",  """Used in looping constructs."""
            "fun",  """Used in lambda expressions, also known as anonymous functions."""
            "function",  """Used as a shorter alternative to the fun keyword and a match expression in a lambda expression that has pattern matching on a single argument."""
            "global",  """Used to reference the top-level .NET namespace."""
            "if",  """Used in conditional branching constructs."""
            "in",  """Used for sequence expressions and, in verbose syntax, to separate expressions from bindings."""
            "inherit",  """Used to specify a base class or base interface."""
            "inline",  """Used to indicate a function that should be integrated directly into the caller's code."""
            "interface",  """Used to declare and implement interfaces."""
            "internal",  """Used to specify that a member is visible inside an assembly but not outside it."""
            "lazy",  """Used to specify a computation that is to be performed only when a result is needed."""
            "let",  """Used to associate, or bind, a name to a value or function."""
            "let!",  """Used in asynchronous workflows to bind a name to the result of an asynchronous computation, or, in other computation expressions, used to bind a name to a result, which is of the computation type."""
            "match",  """Used to branch by comparing a value to a pattern."""
            "member",  """Used to declare a property or method in an object type."""
            "module",  """Used to associate a name with a group of related types, values, and functions, to logically separate it from other code."""
            "mutable",  """Used to declare a variable, that is, a value that can be changed."""
            "namespace",  """Used to associate a name with a group of related types and modules, to logically separate it from other code."""
            "new",  """Used to declare, define, or invoke a constructor that creates or that can create an object.
Also used in generic parameter constraints to indicate that a type must have a certain constructor."""
            "not",  """Not actually a keyword. However, not struct in combination is used as a generic parameter constraint."""
            "null",  """Indicates the absence of an object.
Also used in generic parameter constraints."""
            "of",  """Used in discriminated unions to indicate the type of categories of values, and in delegate and exception declarations."""
            "open",  """Used to make the contents of a namespace or module available without qualification."""
            "or",  """Used with Boolean conditions as a Boolean or operator. Equivalent to ||.
Also used in member constraints."""
            "override",  """Used to implement a version of an abstract or virtual method that differs from the base version."""
            "private",  """Restricts access to a member to code in the same type or module."""
            "public",  """Allows access to a member from outside the type."""
            "rec",  """Used to indicate that a function is recursive."""
            "return",  """Used to indicate a value to provide as the result of a computation expression."""
            "return!",  """Used to indicate a computation expression that, when evaluated, provides the result of the containing computation expression."""
            "select",  """Used in query expressions to specify what fields or columns to extract. Note that this is a contextual keyword, which means that it is not actually a reserved word and it only acts like a keyword in appropriate context."""
            "static",  """Used to indicate a method or property that can be called without an instance of a type, or a value member that is shared among all instances of a type."""
            "struct",  """Used to declare a structure type.
Also used in generic parameter constraints.
Used for OCaml compatibility in module definitions."""
            "then",  """Used in conditional expressions.
Also used to perform side effects after object construction."""
            "to",  """Used in for loops to indicate a range."""
            "true",  """Used as a Boolean literal."""
            "try",  """Used to introduce a block of code that might generate an exception. Used together with with or finally."""
            "type",  """Used to declare a class, record, structure, discriminated union, enumeration type, unit of measure, or type abbreviation."""
            "upcast",  """Used to convert to a type that is higher in the inheritance chain."""
            "use",  """Used instead of let for values that require Dispose to be called to free resources."""
            "use!",  """Used instead of let! in asynchronous workflows and other computation expressions for values that require Dispose to be called to free resources."""
            "val",  """Used in a signature to indicate a value, or in a type to declare a member, in limited situations."""
            "void",  """Indicates the .NET void type. Used when interoperating with other .NET languages."""
            "when",  """Used for Boolean conditions (when guards) on pattern matches and to introduce a constraint clause for a generic type parameter."""
            "while",  """Introduces a looping construct."""
            "with",  """Used together with the match keyword in pattern matching expressions. Also used in object expressions, record copying expressions, and type extensions to introduce member definitions, and to introduce exception handlers."""
            "yield",  """Used in a sequence expression to produce a value for a sequence."""
            "yield!",  """Used in a computation expression to append the result of a given computation expression to a collection of results for the containing computation expression."""
            "->", """In function types, delimits arguments and return values.
Yields an expression (in sequence expressions); equivalent to the yield keyword.
Used in match expressions"""
            "<-", "Assigns a value to a variable."
            ":>", "Converts a type to type that is higher in the hierarchy."
            ":?>", "Converts a type to a type that is lower in the hierarchy."
            "<@", "Delimits a typed code quotation."
            "@>", "Delimits a typed code quotation."
            "<@@", "Delimits a untyped code quotation."
            "@@>", "Delimits a untyped code quotation."]
