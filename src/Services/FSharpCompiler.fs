// --------------------------------------------------------------------------------------
// Wrapper for the APIs in 'FSharp.Compiler.dll' and 'FSharp.Compiler.Server.Shared.dll'
// The API is currently internal, so we call it using the (?) operator and Reflection
// --------------------------------------------------------------------------------------

// Using 'Microsoft' namespace to make the API as similar to the actual one as possible
namespace Microsoft.FSharp.Compiler

open System
open System.Reflection
open Microsoft.FSharp.Reflection
open System.Globalization

/// Implements the (?) operator that makes it possible to access internal methods
/// and properties and contains definitions for F# assemblies
module Reflection =   
  // Various flags configurations for Reflection
  let staticFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Static 
  let instanceFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Instance
  let ctorFlags = instanceFlags
  let inline asMethodBase(a:#MethodBase) = a :> MethodBase
  
  let (?) (o:obj) name : 'R =
    // The return type is a function, which means that we want to invoke a method
    if FSharpType.IsFunction(typeof<'R>) then
      let argType, resType = FSharpType.GetFunctionElements(typeof<'R>)
      FSharpValue.MakeFunction(typeof<'R>, fun args ->
        // We treat elements of a tuple passed as argument as a list of arguments
        // When the 'o' object is 'System.Type', we call static methods
        let methods, instance, args = 
          let args = 
            if argType = typeof<unit> then [| |]
            elif not(FSharpType.IsTuple(argType)) then [| args |]
            else FSharpValue.GetTupleFields(args)
          if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then 
            let methods = (unbox<Type> o).GetMethods(staticFlags) |> Array.map asMethodBase
            let ctors = (unbox<Type> o).GetConstructors(ctorFlags) |> Array.map asMethodBase
            Array.concat [ methods; ctors ], null, args
          else 
            o.GetType().GetMethods(instanceFlags) |> Array.map asMethodBase, o, args
        
        // A simple overload resolution based on the name and number of parameters only
        let methods = 
          [ for m in methods do
              if m.Name = name && m.GetParameters().Length = args.Length then yield m ]
        match methods with 
        | [] -> failwithf "No method '%s' with %d arguments found" name args.Length
        | _::_::_ -> failwithf "Multiple methods '%s' with %d arguments found" name args.Length
        | [:? ConstructorInfo as c] -> c.Invoke(args)
        | [ m ] -> m.Invoke(instance, args) ) |> unbox<'R>
    else
      // When the 'o' object is 'System.Type', we access static properties
      let typ, flags, instance = 
        if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then unbox o, staticFlags, null
        else o.GetType(), instanceFlags, o
      
      // Find a property that we can call and get the value
      let prop = typ.GetProperty(name, flags)
      if prop = null && instance = null then 
        // Try nested type...
        let nested = typ.Assembly.GetType(typ.FullName + "+" + name)
        if nested = null then 
          failwithf "Property or nested type '%s' not found in '%s' using flags '%A'." name typ.Name flags
        elif not ((typeof<'R>).IsAssignableFrom(typeof<System.Type>)) then
          failwithf "Cannot return nested type '%s' as value of type '%s'." nested.Name (typeof<'R>.Name)
        else nested |> box |> unbox<'R>
      else
        // Call property
        let meth = prop.GetGetMethod(true)
        if prop = null then failwithf "Property '%s' found, but doesn't have 'get' method." name
        try meth.Invoke(instance, [| |]) |> unbox<'R>
        with _ -> failwithf "Failed to get value of '%s' property (of type '%s')" name typ.Name


  /// Wrapper type for the 'FSharp.Compiler.dll' assembly - expose types we use
  type FSharpCompiler private () =      
    static let asm = Assembly.Load("FSharp.Compiler, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
    static member InteractiveChecker = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.InteractiveChecker")
    static member IsResultObsolete = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.IsResultObsolete")
    static member CheckOptions = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.CheckOptions")
    static member SourceTokenizer = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.SourceTokenizer")
    static member TokenInformation = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.TokenInformation")
    static member Parser = asm.GetType("Microsoft.FSharp.Compiler.Parser")
    
  /// Wrapper type for the 'FSharp.Compiler.Server.Shared.dll' assembly - expose types we use
  type FSharpCompilerServerShared private () =      
    static let asm = Assembly.Load("FSharp.Compiler.Server.Shared, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
    static member InteractiveServer = asm.GetType("Microsoft.FSharp.Compiler.Server.Shared.FSharpInteractiveServer")

// --------------------------------------------------------------------------------------
/// Wrapper for the 'Microsoft.FSharp.Compiler.Parser' module
// --------------------------------------------------------------------------------------

module Parser = 
  open Reflection
  let wrapped = FSharpCompiler.Parser
  
  /// Represents a token
  type token = 
    | WrappedToken of obj
    /// Creates a token representing the specified identifier
    static member IDENT(name) = 
      WrappedToken(wrapped?token?IDENT?``.ctor``(name))
  
  /// Returns the tag of the specified token
  let tagOfToken(WrappedToken token) =
    wrapped?tagOfToken(token) : int

// --------------------------------------------------------------------------------------
// Wrapper for 'Microsoft.Compiler.Server.Shared', which contains some API for
// controlling F# Interactive using reflection (e.g. for interrupt)
// --------------------------------------------------------------------------------------
    
module Server =
  module Shared = 
    open Reflection
    
    type FSharpInteractiveServer(wrapped:obj) =
      static member StartClient(channel:string) = 
        FSharpInteractiveServer
          (FSharpCompilerServerShared.InteractiveServer?StartClient(channel))
      member x.Interrupt() : unit = wrapped?Interrupt()

// --------------------------------------------------------------------------------------
// Source code services (Part 1) - contains wrappers for tokenization etc.     
// --------------------------------------------------------------------------------------

module SourceCodeServices =
  open Reflection

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
    
  // ------------------------------------------------------------------------------------

  module Array = 
    let untypedMap f (a:System.Array) = 
      Array.init a.Length (fun i -> f (a.GetValue(i)))

  module List = 
    let rec untypedMap f (l:obj) =
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

  type CheckOptions(wrapped:obj) =
    member x.Wrapped = wrapped
    member x.ProjectFileName : string = wrapped?ProjectFileName
    member x.ProjectFileNames : string array = wrapped?ProjectFileNames
    member x.ProjectOptions : string array = wrapped?ProjectOptions
    member x.IsIncompleteTypeCheckEnvironment : bool = wrapped?IsIncompleteTypeCheckEnvironment 
    member x.UseScriptResolutionRules : bool = wrapped?UseScriptResolutionRules
    static member Create(fileName:string, fileNames:string[], options:string[], incomplete:bool, scriptRes:bool) =
      CheckOptions(FSharpCompiler.CheckOptions?``.ctor``(fileName, fileNames, options, incomplete, scriptRes))
    member x.WithOptions(options:string[]) =
      CheckOptions.Create
        ( x.ProjectFileName, x.ProjectFileNames, options, x.IsIncompleteTypeCheckEnvironment, 
          x.UseScriptResolutionRules )
      
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

  type TypeCheckInfo(wrapped:obj) =
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
  type TypeCheckResults(wrapped:obj) =
    /// The errors returned by parsing a source file
    member x.Errors : ErrorInfo array = 
      wrapped?Errors |> Array.untypedMap (fun e -> ErrorInfo(e))
      
    /// A handle to type information gleaned from typechecking the file. 
    member x.TypeCheckInfo : TypeCheckInfo option = 
      if wrapped?TypeCheckInfo = null then None 
      else Some(TypeCheckInfo(wrapped?TypeCheckInfo?Value))

  type TypeCheckAnswer(wrapped:obj) =
    member x.Wrapped = wrapped

  let (|NoAntecedant|Aborted|TypeCheckSucceeded|) (tc:TypeCheckAnswer) = 
    if tc.Wrapped?IsNoAntecedant then NoAntecedant() 
    elif tc.Wrapped?IsAborted then Aborted() 
    elif tc.Wrapped?IsTypeCheckSucceeded then 
      TypeCheckSucceeded(TypeCheckResults(tc.Wrapped?Item))
    else failwith "Unexpected TypeCheckAnswer value"    
  
  type TypeCheckSucceededImpl(tyres:TypeCheckResults) =
    member x.IsTypeCheckSucceeded = true
    member x.IsAborted = false
    member x.IsNoAntecedant = false
    member x.Item = tyres
    
  let TypeCheckSucceeded arg = 
    TypeCheckAnswer(TypeCheckSucceededImpl(arg))
    
  type InteractiveChecker(wrapped:obj) =
      /// Crate an instance of the wrapper
      static member Create (dirty:FileTypeCheckStateIsDirty) =
        InteractiveChecker(FSharpCompiler.InteractiveChecker?Create(dirty))
        
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
          ( wrapped?TypeCheckSource
              ( parsed.Wrapped, filename, fileversion, source, options.Wrapped, 
                FSharpCompiler.IsResultObsolete?NewIsResultObsolete(f) ) : obj)
      
      /// For a given script file, get the CheckOptions implied by the #load closure
      member x.GetCheckOptionsFromScriptRoot(filename:string, source:string) : CheckOptions =
        CheckOptions(wrapped?GetCheckOptionsFromScriptRoot(filename, source))
          

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
