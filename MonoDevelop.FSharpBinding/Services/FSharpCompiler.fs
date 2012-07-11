
// --------------------------------------------------------------------------------------
// Wrapper for the APIs in 'FSharp.Compiler.dll' and 'FSharp.Compiler.Server.Shared.dll'
// The API is currently internal, so we call it using the (?) operator and Reflection
// --------------------------------------------------------------------------------------

namespace Microsoft.FSharp.Compiler

open System
open System.IO
open System.Reflection
open System.Text
open System.Globalization
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide
open MonoDevelop.Core.Assemblies
open MonoDevelop.Core
open MonoDevelop.FSharp
open MonoDevelop.FSharp.Reflection
open Mono.Addins
    
// --------------------------------------------------------------------------------------
// Assembly resolution in a script file - a workaround that replaces functionality
// from 'GetCheckOptionsFromScriptRoot' (which doesn't work well on Mono)
// --------------------------------------------------------------------------------------

type FSharpCompilerVersionNumber = 
      | Version_4_0 | Version_4_3
      override x.ToString() = match x with | Version_4_0 -> "4.0.0.0" | Version_4_3 -> "4.3.0.0"

    /// Wrapper type for the 'FSharp.Compiler.dll' assembly - expose types we use
type FSharpCompiler(versionNumber:FSharpCompilerVersionNumber) =      
    let asm = Assembly.Load("FSharp.Compiler, Version="+versionNumber.ToString()+", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
    let asm2 = Assembly.Load("FSharp.Compiler.Server.Shared, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
    static let v40 = lazy FSharpCompiler(Version_4_0) 
    static let v43 = lazy FSharpCompiler(Version_4_3) 
    member __.InteractiveChecker = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.InteractiveChecker")
    member __.IsResultObsolete = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.IsResultObsolete")
    member __.CheckOptions = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.CheckOptions")
#if USE_FSHARP_COMPILER_TOKENIZATION
    member __.SourceTokenizer = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.SourceTokenizer")
    member __.TokenInformation = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.TokenInformation")
#endif
    member __.Parser = asm.GetType("Microsoft.FSharp.Compiler.Parser")
    member __.InteractiveServer = asm2.GetType("Microsoft.FSharp.Compiler.Server.Shared.FSharpInteractiveServer")
    static member Get(version) = 
        let c = match version with Version_4_0 -> v40 | Version_4_3 -> v43
        try c.Force() with e -> Debug.tracee "Compiler" e; reraise()
    
module Parser = 
  
  /// Represents a token
  type token = 
    | WrappedToken of obj
    /// Creates a token representing the specified identifier
    static member IDENT(version, name) = 
      WrappedToken(FSharpCompiler.Get(version).Parser?token?IDENT?``.ctor``(name))
  
  /// Returns the tag of the specified token
  let tagOfToken(version, WrappedToken token) =
    FSharpCompiler.Get(version).Parser?tagOfToken(token) : int

// --------------------------------------------------------------------------------------
// Wrapper for 'Microsoft.Compiler.Server.Shared', which contains some API for
// controlling F# Interactive using reflection (e.g. for interrupt)
// --------------------------------------------------------------------------------------
    
module Server =
  module Shared = 
    
    type FSharpInteractiveServer(wrapped:obj) =
      static member StartClient(compilerVersion,channel:string) = 
        FSharpInteractiveServer(FSharpCompiler.Get(compilerVersion).InteractiveServer?StartClient(channel))
      member x.Interrupt() : unit = wrapped?Interrupt()

// --------------------------------------------------------------------------------------
// Source code services (Part 1) - contains wrappers for tokenization etc.     
// --------------------------------------------------------------------------------------

module SourceCodeServices =

#if USE_FSHARP_COMPILER_TOKENIZATION
  type TokenColorKind =
    | Comment = 2
    | Default = 0
    | Identifier = 3
    | InactiveCode = 7
    | Keyword = 1
    | Number = 9
    | Operator = 10
    | PreprocessorKeyword = 8
    | String = 4
    | Text = 0
    | UpperIdentifier = 5

  type TokenCharKind =
    | Comment = 10
    | Default = 0
    | Delimiter = 6
    | Identifier = 2
    | Keyword = 1
    | LineComment = 9
    | Literal = 4
    | Operator = 5
    | String = 3
    | Text = 0
    | WhiteSpace = 8

  type TriggerClass(wrapped:obj) = 
    member x.Wrapped = wrapped
      
  type TokenInformation(wrapped:obj) =
    member x.LeftColumn : int = wrapped?LeftColumn
    member x.RightColumn : int = wrapped?RightColumn
    member x.Tag : int = wrapped?Tag
    member x.TokenName : string = wrapped?TokenName
    member x.ColorClass : TokenColorKind = enum<TokenColorKind>(unbox wrapped?ColorClass)
    member x.CharClass : TokenCharKind = enum<TokenCharKind>(unbox wrapped?CharClass)
    member x.TriggerClass : TriggerClass = TriggerClass(wrapped?TriggerClass)
    member x.WithRightColumn(rightColumn:int) = 
      TokenInformation
        ( FSharpCompiler.TokenInformation?``.ctor``
            ( x.LeftColumn, rightColumn, int x.ColorClass, int x.CharClass,
              x.TriggerClass.Wrapped, x.Tag, x.TokenName ) )
    member x.WithTokenName(tokenName:string) = 
      TokenInformation
        ( FSharpCompiler.TokenInformation?``.ctor``
            ( x.LeftColumn, x.RightColumn, x.ColorClass, x.CharClass,
              x.TriggerClass.Wrapped, x.Tag, tokenName ) )
    
  type LineTokenizer(wrapped:obj) = 
    member x.StartNewLine() : unit = wrapped?StartNewLine()
    member x.ScanToken(state:int64) = 
      let tup : obj = wrapped?ScanToken(state)
      let optInfo, newstate = tup?Item1, tup?Item2
      let optInfo = 
        if optInfo = null then None
        else Some(new TokenInformation(optInfo?Value))
      optInfo, newstate
      
  type SourceTokenizer(defines:string list, source:string) =
    let wrapped = FSharpCompiler.SourceTokenizer?``.ctor``(defines, source)
    member x.CreateLineTokenizer(line:string) = 
      LineTokenizer(wrapped?CreateLineTokenizer(line))
#endif    
  // ------------------------------------------------------------------------------------

  module Array = 
    let untypedMap f (a:System.Array) = 
      Array.init a.Length (fun i -> f (a.GetValue(i)))

  module List = 
    let untypedMap f (l:obj) =
      (l :?> System.Collections.IEnumerable) |> Seq.cast<obj> |> Seq.map f |> List.ofSeq
    
  module PrettyNaming = 
    let IsIdentifierPartCharacter (c:char) = 
      let cat = System.Char.GetUnicodeCategory(c)
      cat = UnicodeCategory.UppercaseLetter ||
      cat = UnicodeCategory.LowercaseLetter ||
      cat = UnicodeCategory.TitlecaseLetter ||
      cat = UnicodeCategory.ModifierLetter ||
      cat = UnicodeCategory.OtherLetter ||
      cat = UnicodeCategory.LetterNumber || 
      cat = UnicodeCategory.DecimalDigitNumber ||
      cat = UnicodeCategory.ConnectorPunctuation ||
      cat = UnicodeCategory.NonSpacingMark ||
      cat = UnicodeCategory.SpacingCombiningMark || c = '\''
    
  // ------------------------------------------------------------------------------------
  // Source code services (Part 2) - contains wrappers for parsing & type checking.     
  // ------------------------------------------------------------------------------------
    
  type Position = int * int

  type Names = string list 

  type NamesWithResidue = Names * string 

  type XmlComment(wrapped:obj) =
    member x.Wrapped = wrapped

  let (|XmlCommentNone|XmlCommentText|XmlCommentSignature|) (xml:XmlComment) = 
    if xml.Wrapped?IsXmlCommentNone then XmlCommentNone()
    elif xml.Wrapped?IsXmlCommentText then XmlCommentText(xml.Wrapped?Item : string)
    elif xml.Wrapped?IsXmlCommentSignature then 
      let it1, it2 : string * string = xml.Wrapped?Item1, xml.Wrapped?Item2
      XmlCommentSignature(it1, it2)
    else failwith "Unexpected XmlComment value!"

  type DataTipElement(wrapped:obj) = 
    member x.Wrapped = wrapped

  let (|DataTipElementNone|DataTipElement|DataTipElementGroup|DataTipElementCompositionError|) (el:DataTipElement) = 
    if el.Wrapped?IsDataTipElementNone then 
      DataTipElementNone
    elif el.Wrapped?IsDataTipElement then 
      let (s:string) = el.Wrapped?Item1
      let xml = XmlComment(el.Wrapped?Item2)
      DataTipElement(s, xml)
    elif el.Wrapped?IsDataTipElementGroup then  
      let list = el.Wrapped?Item |> List.untypedMap (fun tup ->
        let (s:string) = tup?Item1
        let xml = XmlComment(tup?Item2)
        s, xml )
      DataTipElementGroup(list)
    elif el.Wrapped?IsDataTipElementCompositionError then 
      DataTipElementCompositionError(el.Wrapped?Item : string)
    else 
      failwith "Unexpected DataTipElement value!"

  type DataTipText(wrapped:obj) = 
    member x.Wrapped = wrapped
    static member Empty = DataTipText(null)

  let (|DataTipText|) (d:DataTipText) = 
    if d.Wrapped = null then []
    else
      d.Wrapped?Item |> List.untypedMap (fun o ->
        DataTipElement(o))
    
  type FileTypeCheckStateIsDirty = string -> unit
          
  /// Callback that indicates whether a requested result has become obsolete.    
  [<NoComparison;NoEquality>]
  type IsResultObsolete = 
      | IsResultObsolete of (unit->bool)

  type CheckOptions(version,wrapped:obj) =
    member x.Wrapped = wrapped
    member x.ProjectFileName : string = wrapped?ProjectFileName
    member x.ProjectFileNames : string array = wrapped?ProjectFileNames
    member x.ProjectOptions : string array = wrapped?ProjectOptions
    member x.IsIncompleteTypeCheckEnvironment : bool = wrapped?IsIncompleteTypeCheckEnvironment 
    member x.UseScriptResolutionRules : bool = wrapped?UseScriptResolutionRules
    static member Create(version,fileName:string, fileNames:string[], options:string[], incomplete:bool, scriptRes:bool) =
      CheckOptions(version,FSharpCompiler.Get(version).CheckOptions?``.ctor``(fileName, fileNames, options, incomplete, scriptRes))
    member x.WithOptions(options:string[]) =
      CheckOptions.Create
        ( version,x.ProjectFileName, x.ProjectFileNames, options, x.IsIncompleteTypeCheckEnvironment, x.UseScriptResolutionRules )
      
  type UntypedParseInfo(wrapped:obj) =
    member x.Wrapped = wrapped
    /// Name of the file for which this information were created
    //abstract FileName                       : string
    /// Get declaraed items and the selected item at the specified location
    //abstract GetNavigationItems             : unit -> NavigationItems
    /// Return the inner-most range associated with a possible breakpoint location
    //abstract ValidateBreakpointLocation : Position -> Range option
    /// When these files change then the build is invalid
    //abstract DependencyFiles : unit -> string list


  type Severity = Warning | Error

  type Declaration(wrapped:obj) =
    member x.Name : string = wrapped?Name
    member x.DescriptionText : DataTipText = DataTipText(wrapped?DescriptionText)
    member x.Glyph : int = wrapped?Glyph

  type DeclarationSet(wrapped:obj) =
    member x.Items = 
      wrapped?Items |> Array.untypedMap (fun o -> Declaration(o))

  type TypeCheckInfo(version:FSharpCompilerVersionNumber, wrapped:obj) =
    /// Resolve the names at the given location to a set of declarations
    member x.GetDeclarations(pos:Position, line:string, names:NamesWithResidue, tokentag:int) =
      DeclarationSet(wrapped?GetDeclarations(pos, line, names, tokentag))
      
    /// Resolve the names at the given location to give a data tip 
    member x.GetDataTipText(pos:Position, line:string, names:Names, tokentag:int) : DataTipText =
      DataTipText(wrapped?GetDataTipText(pos, line, names, tokentag))
      
    /// Resolve the names at the given location to give F1 keyword
    // member GetF1Keyword : Position * string * Names -> string option
    // Resolve the names at the given location to a set of methods
    // member GetMethods : Position * string * Names option * (*tokentag:*)int -> MethodOverloads
    /// Resolve the names at the given location to the declaration location of the corresponding construct
    // member GetDeclarationLocation : Position * string * Names * (*tokentag:*)int * bool -> FindDeclResult
    /// A version of `GetDeclarationLocation` augmented with the option (via the `bool`) parameter to force .fsi generation (even if source exists); this is primarily for testing
    // member GetDeclarationLocationInternal : bool -> Position * string * Names * (*tokentag:*)int * bool -> FindDeclResult
    member x.Version = version

  type ErrorInfo(wrapped:obj) =
    member x.StartLine : int = wrapped?StartLine
    member x.EndLine : int = wrapped?EndLine
    member x.StartColumn : int = wrapped?StartColumn
    member x.EndColumn : int = wrapped?EndColumn
    member x.Severity : Severity = 
      if wrapped?Severity?IsError then Error else Warning
    member x.Message : string = wrapped?Message
    member x.Subcategory : string = wrapped?Subcategory
  
  /// A handle to the results of TypeCheckSource
  type TypeCheckResults(version:FSharpCompilerVersionNumber,wrapped:obj) =
    /// The errors returned by parsing a source file
    member x.Errors : ErrorInfo[] = 
      wrapped?Errors |> Array.untypedMap (fun e -> ErrorInfo(e))
      
    /// A handle to type information gleaned from typechecking the file. 
    member x.TypeCheckInfo : TypeCheckInfo option = 
      if wrapped?TypeCheckInfo = null then None 
      else Some(TypeCheckInfo(wrapped?TypeCheckInfo?Value))

  type TypeCheckAnswer(version,wrapped:obj) =
    member x.Wrapped = wrapped
    member x.Version = version

  let (|NoAntecedant|Aborted|TypeCheckSucceeded|) (tc:TypeCheckAnswer) = 
    if tc.Wrapped?IsNoAntecedant then NoAntecedant() 
    elif tc.Wrapped?IsAborted then Aborted() 
    elif tc.Wrapped?IsTypeCheckSucceeded then 
      TypeCheckSucceeded(TypeCheckResults(tc.Version,tc.Wrapped?Item))
    else failwith "Unexpected TypeCheckAnswer value"    
  
  type TypeCheckSucceededImpl(tyres:TypeCheckResults) =
    member x.IsTypeCheckSucceeded = true
    member x.IsAborted = false
    member x.IsNoAntecedant = false
    member x.Item = tyres
    
  let TypeCheckSucceeded (version,arg) = 
    TypeCheckAnswer(version,TypeCheckSucceededImpl(arg))
    
  type InteractiveChecker(version:FSharpCompilerVersionNumber,wrapped:obj) =
      /// Crate an instance of the wrapper
      static member Create (version,dirty:FileTypeCheckStateIsDirty) =
        InteractiveChecker(version,FSharpCompiler.Get(version).InteractiveChecker?Create(dirty))
        
      /// Parse a source code file, returning a handle that can be used for obtaining navigation bar information
      /// To get the full information, call 'TypeCheckSource' method on the result
      member x.UntypedParse(filename:string, source:string, options:CheckOptions) : UntypedParseInfo =
        UntypedParseInfo(wrapped?UntypedParse(filename, source, options.Wrapped))

      /// Typecheck a source code file, returning a handle to the results of the parse including
      /// the reconstructed types in the file.
      ///
      /// Return None if the background builder is not yet done prepring the type check results for the antecedent to the 
      /// file.
      member x.TypeCheckSource
          ( parsed:UntypedParseInfo, filename:string, fileversion:int, 
            source:string, options:CheckOptions, (IsResultObsolete f)) =
        TypeCheckAnswer
          (version, 
           (wrapped?TypeCheckSource
              ( parsed.Wrapped, filename, fileversion, source, options.Wrapped, 
                FSharpCompiler.Get(version).IsResultObsolete?NewIsResultObsolete(f) ) : obj))
      
      /// For a given script file, get the CheckOptions implied by the #load closure
      member x.GetCheckOptionsFromScriptRoot(filename:string, source:string, loadedTimeStamp:System.DateTime) : CheckOptions =
        // GetCheckOptionsFromScriptRoot takes an extra argument in 4.3.0.0. Ignore it in 4.0.0.0
        match version with 
        | Version_4_0 -> CheckOptions(wrapped?GetCheckOptionsFromScriptRoot(filename, source))
        | Version_4_3 -> CheckOptions(wrapped?GetCheckOptionsFromScriptRoot(filename, source, loadedTimeStamp))
          

      /// Try to get recent type check results for a file. This may arbitrarily refuse to return any
      /// results if the InteractiveChecker would like a chance to recheck the file, in which case
      /// UntypedParse and TypeCheckSource should be called. If the source of the file
      /// has changed the results returned by this function may be out of date, though may
      /// still be usable for generating intellsense menus and information.
      member x.TryGetRecentTypeCheckResultsForFile(filename:string, options:CheckOptions) =
        let res = wrapped?TryGetRecentTypeCheckResultsForFile(filename, options.Wrapped) : obj
        if res = null then None else
          let tuple = res?Value
          Some(UntypedParseInfo(tuple?Item1), TypeCheckResults(tuple?Item2), int tuple?Item3)

      /// Begin background parsing the given project.
      member x.StartBackgroundCompile(options:CheckOptions) =
        wrapped?StartBackgroundCompile(options.Wrapped)

      // Members that are not supported by the wrapper
      
      /// Parse a source code file, returning information about brace matching in the file
      /// Return an enumeration of the matching parethetical tokens in the file
      // member MatchBraces : filename : string * source: string * options: CheckOptions -> (Range * Range) array
                
      /// This function is called when the configuration is known to have changed for reasons not encoded in the CheckOptions.
      /// For example, dependent references may have been deleted or created.
      // member InvalidateConfiguration : options : CheckOptions -> unit    

      /// Stop the background compile.
      // member StopBackgroundCompile : unit -> unit
      /// Block until the background compile finishes.
      // member WaitForBackgroundCompile : unit -> unit
      
      /// Report a statistic for testability
      // static member GlobalForegroundParseCountStatistic : int

      /// Report a statistic for testability
      // static member GlobalForegroundTypeCheckCountStatistic : int

      // member GetSlotsCount : options : CheckOptions -> int
      // member UntypedParseForSlot : slot:int * options : CheckOptions -> UntypedParseInfo
