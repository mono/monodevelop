
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


/// Wrapper type for the 'FSharp.Compiler.dll' assembly - expose types we use
type FSharpCompiler(reqVersion:FSharpCompilerVersion) =      
    let otherVersion = match reqVersion with FSharp_2_0 -> FSharp_3_0 | FSharp_3_0 -> FSharp_2_0
    let tryVersion (ver:FSharpCompilerVersion) = 
         try 
           // Try getting the assemblies using the microsoft strong name
           let asm = Assembly.Load("FSharp.Compiler, Version="+ver.ToString()+", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
           let asm2 = Assembly.Load("FSharp.Compiler.Server.Shared, Version="+ver.ToString()+", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
           asm,asm2,ver
         with _ -> 
           // Try getting the assemblies by partial name. 
           let asm = Assembly.LoadWithPartialName("FSharp.Compiler, Version="+ver.ToString())
           let asm2 = Assembly.LoadWithPartialName("FSharp.Compiler.Server.Shared, Version="+ver.ToString())
           asm,asm2,ver
    let asm,asm2,actualVersion = 
        try tryVersion reqVersion 
        with e -> 
            match (try Some (tryVersion otherVersion) with e -> None) with 
            | Some res -> res 
            | None -> reraise()
        
    static let v20 = lazy FSharpCompiler(FSharp_2_0) 
    static let v30 = lazy FSharpCompiler(FSharp_3_0) 
    let interactiveCheckerType = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.InteractiveChecker")
    let isResultObsoleteType = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.IsResultObsolete")
    let declarationSetType = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.DeclarationSet")
    let checkOptionsType = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.CheckOptions")
    let parserType = asm.GetType("Microsoft.FSharp.Compiler.Parser")
    let interactiveServerType = asm2.GetType("Microsoft.FSharp.Compiler.Server.Shared.FSharpInteractiveServer")
    let fSharpCore = asm.GetReferencedAssemblies() |> Array.find (fun x -> x.Name = "FSharp.Core") |> fun a -> Assembly.Load(a)
    let fSharpValueType = fSharpCore.GetType("Microsoft.FSharp.Reflection.FSharpValue")
    let unitType = fSharpCore.GetType(typeof<unit>.FullName)
    let fsharpFuncType = fSharpCore.GetType("Microsoft.FSharp.Core.FSharpFunc`2")
    let asyncType = fSharpCore.GetType("Microsoft.FSharp.Control.FSharpAsync")
    let fsharpListType = fSharpCore.GetType("Microsoft.FSharp.Collections.FSharpList`1")
    let fsharpOptionType = fSharpCore.GetType("Microsoft.FSharp.Core.FSharpOption`1")
    
    let funcConvertType = fSharpCore.GetType("Microsoft.FSharp.Core.FuncConvert")
    let toFSharpFunc = funcConvertType.GetMethods() |> Array.find (fun x -> x.Name = "ToFSharpFunc" && x.GetParameters().[0].ParameterType.Name = "Converter`2")
    let runSynchronously = asyncType.GetMethod "RunSynchronously"

    member __.InteractiveCheckerType = interactiveCheckerType
    member __.IsResultObsoleteType = isResultObsoleteType
    member __.CheckOptionsType = checkOptionsType
    member __.DeclarationSetType = declarationSetType
    member __.ParserType = parserType
    member __.InteractiveServerType = interactiveServerType
    member __.FSharpCore = fSharpCore
    member __.UnitType = unitType
    member __.AsyncType = asyncType
    member __.AsyncRunSynchronouslyMethod = runSynchronously
    member __.ActualVersion = actualVersion
    member __.NotifyFileTypeCheckStateIsDirtyType = 
        // only valid when version is 4.3.0.0
        asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.NotifyFileTypeCheckStateIsDirty")


    // We support multiple backend FSharp.Compiler.dll. 
    //    - Some of these have slight differences in their "SourceCodeServices" API
    //    - We use soft-binding via the "dynamic operator" (?) reflection 
    // Annoyingly, these can use different FSharp.Core.dll, because MonoDevelop doesn't unify FSHarp.Core 4.0.0.0 and FSharp.Core 4.3.0.0.
    // For the most part this doesn't matter, for two reasons:
    //    - We invoke most funcitonality via the dynamic operator
    //    - On the whole the SourceCodeServices API doesnt' transact F#-specific data in argument and return position.
    // However, in the few places where the SourceCodeServices API does transact F#-specific types (e.g. lists and functions) we need to 
    // manually construct F# values that correspond the types in the FSharp.Core.dll used by the target FSharp.Compiler.dll we 
    // are connecting to.
    //
    // We assume the mscorlib.dll being used is always the same, i.e. MonoDevelop unifies these into 4.0.0.0.
    //
    member x.MakeFunctionType(dom,ran) = fsharpFuncType.MakeGenericType [| dom; ran |]
    member x.MakePairType(t1,t2) = typedefof<int * int>.MakeGenericType [| t1; t2 |]
    
    member x.MakeFunction(domTy,ranTy,converter:Converter<obj,obj>) = 
        // Convert a 'Converter<obj,obj>' to a 'obj -> obj' using Microsoft.FSharp.Core.FuncConvert.ToFSharpFunc
        let m2 = toFSharpFunc.MakeGenericMethod([| typeof<obj>; typeof<obj> |])
        let f = m2.Invoke(null, [| converter |])
        // Convert the 'obj -> obj' to a 'domTy -> ranTy' using Microsoft.FSharp.Reflection.FSharpValue.MakeFunction
        let fty = x.MakeFunctionType(domTy,ranTy)
        fSharpValueType?MakeFunction(fty, f)

    member x.MakePair(ty1,ty2,v1:obj,v2:obj) = 
        let fty = x.MakePairType(ty1,ty2)
        fSharpValueType?MakeTuple([| v1; v2 |], fty)

    member x.MakeListType(elemTy) = fsharpListType.MakeGenericType [| elemTy |]
    member x.MakeList(elemTy,elems: 'a list) =
        let listTy = x.MakeListType elemTy
        List.foldBack (fun a x -> listTy?Cons(a, x)) elems (listTy?get_Empty())

    member x.MakeOptionType(elemTy) = fsharpOptionType.MakeGenericType [| elemTy |]
    member x.MakeOption(elemTy,elem:obj) =
        let optionTy = x.MakeOptionType elemTy
        optionTy?Some(elem)

    static member Current 
      with get() = 
        let fsVersion = FSharpCompilerVersion.CurrentRequestedVersion  
        let c = match fsVersion with FSharp_2_0 -> v20 | FSharp_3_0 -> v30
        try c.Force() with e -> Debug.tracee "Compiler" e; reraise()
#if USE_FSHARP_COMPILER_TOKENIZATION
    member __.SourceTokenizer = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.SourceTokenizer")
    member __.TokenInformation = asm.GetType("Microsoft.FSharp.Compiler.SourceCodeServices.TokenInformation")
#endif
    
module Parser = 
  
  /// Represents a token
  type token = 
    | WrappedToken of obj
    /// Creates a token representing the specified identifier
    static member IDENT(name) = 
      WrappedToken(FSharpCompiler.Current.ParserType?token?IDENT?``.ctor``(name))
  
  /// Returns the tag of the specified token
  let tagOfToken(WrappedToken token) =
    FSharpCompiler.Current.ParserType?tagOfToken(token) : int

// --------------------------------------------------------------------------------------
// Wrapper for 'Microsoft.Compiler.Server.Shared', which contains some API for
// controlling F# Interactive using reflection (e.g. for interrupt)
// --------------------------------------------------------------------------------------
    
module Server =
  module Shared = 
    
    type FSharpInteractiveServer(wrapped:obj) =
      static member StartClient(channel:string) = 
        FSharpInteractiveServer(FSharpCompiler.Current.InteractiveServerType?StartClient(channel))
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
    
  type NotifyFileTypeCheckStateIsDirty = string -> unit
          
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
    member x.LoadTime : System.DateTime = 
          // This property only available with F# 2.0
          let fsc = FSharpCompiler.Current
          match fsc.ActualVersion with 
          | FSharpCompilerVersion.FSharp_2_0 -> System.DateTime.Now
          | FSharpCompilerVersion.FSharp_3_0 -> try wrapped?LoadTime with _ -> System.DateTime.Now
    static member Create(fileName:string, fileNames:string[], options:string[], incomplete:bool, scriptRes:bool, loadTime:System.DateTime) =
      let res = 
          let fsc = FSharpCompiler.Current
          match fsc.ActualVersion with 
          | FSharpCompilerVersion.FSharp_2_0 -> 
              fsc.CheckOptionsType?``.ctor``(fileName, fileNames, options, incomplete, scriptRes)
          | FSharpCompilerVersion.FSharp_3_0 -> 
              let noUnresolvedReferences = null (* this null is 'None' in the F# representation of an option value *) 
              fsc.CheckOptionsType?``.ctor``(fileName, fileNames, options, incomplete, scriptRes, loadTime, noUnresolvedReferences)
      CheckOptions(res)

    member x.WithOptions(options:string[]) =
      CheckOptions.Create
        ( x.ProjectFileName, x.ProjectFileNames, options, x.IsIncompleteTypeCheckEnvironment, x.UseScriptResolutionRules, x.LoadTime  )
      
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
    member x.GetDeclarations(pos:Position, line:string, (names,residue), tokentag:int, timeout:int) =
      let fsc = FSharpCompiler.Current
      let names = fsc.MakeList(typeof<string>, names)
      let namesAndResidue = fsc.MakePair(fsc.MakeListType(typeof<string>), typeof<string>, names, residue)
      let res = 
          match fsc.ActualVersion with 
          | FSharp_2_0 -> 
              wrapped?GetDeclarations(pos, line, namesAndResidue, tokentag)
          | FSharp_3_0 -> 
              // The F# 3.0 api takes an additional two arguments and returns an asynchronous result. 
              // We force the result synchronously under a timeout
              let funcArgType = typeof<obj * ((int * int) * (int * int))>
              // TODO: fill this in. Currently we fill in with (fun _ -> false) relative to the FSharp.Core.dll used by the FSharp.Compiler.dll
              let hasTextChangedSinceLastTypecheck = fsc.MakeFunction(funcArgType, typeof<bool>, Converter<obj,obj>(fun _ -> box false))
              let untypedParseInfoOpt = null
              let asyncDeclSet : obj = wrapped?GetDeclarations(untypedParseInfoOpt, pos, line, namesAndResidue, hasTextChangedSinceLastTypecheck)
              let optionalTimeout = fsc.MakeOption(typeof<int>,timeout)
              let optionalCancellationToken = null
              try fsc.AsyncRunSynchronouslyMethod.MakeGenericMethod(fsc.DeclarationSetType).Invoke(null,[| asyncDeclSet; optionalTimeout; optionalCancellationToken |])
              with :? System.Reflection.TargetInvocationException as e -> raise e.InnerException

      DeclarationSet(res)
      
    /// Resolve the names at the given location to give a data tip 
    member x.GetDataTipText(pos:Position, line:string, names:Names, tokentag:int) : DataTipText =
      let fsc = FSharpCompiler.Current
      let names = fsc.MakeList(typeof<string>, names)
      DataTipText(wrapped?GetDataTipText(pos, line, names, tokentag))
      
    /// Resolve the names at the given location to give F1 keyword
    // member GetF1Keyword : Position * string * Names -> string option
    // Resolve the names at the given location to a set of methods
    // member GetMethods : Position * string * Names option * (*tokentag:*)int -> MethodOverloads
    /// Resolve the names at the given location to the declaration location of the corresponding construct
    // member GetDeclarationLocation : Position * string * Names * (*tokentag:*)int * bool -> FindDeclResult

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
    member x.Errors : ErrorInfo[] =  wrapped?Errors |> Array.untypedMap (fun e -> ErrorInfo(e))
      
    /// A handle to type information gleaned from typechecking the file. 
    member x.TypeCheckInfo : TypeCheckInfo option = 
      let fsc = FSharpCompiler.Current
      match fsc.ActualVersion with 
      | FSharp_2_0 -> 
          if wrapped?TypeCheckInfo = null then None 
          else Some(TypeCheckInfo(wrapped?TypeCheckInfo?Value))
      // The types "TypeCheckInfo" and "TypeCheckResults" were merged in F# 3.0
      | FSharp_3_0 -> 
          Some(TypeCheckInfo(wrapped))

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
    
  let TypeCheckSucceeded (arg) = 
    TypeCheckAnswer(TypeCheckSucceededImpl(arg))
    
  type InteractiveChecker(wrapped:obj) =
      /// Crate an instance of the wrapper
      static member Create (dirty:NotifyFileTypeCheckStateIsDirty) =
        let dirty : obj = 
            let fsc = FSharpCompiler.Current
            let funcVal = fsc.MakeFunction(typeof<string>,fsc.UnitType, Converter<obj,obj>(fun obj -> dirty (obj :?> string) |> box))
            match fsc.ActualVersion with 
            | FSharp_2_0 -> funcVal (* this was just a function value in F# 2.0 *)
            | FSharp_3_0 -> fsc.NotifyFileTypeCheckStateIsDirtyType?NewNotifyFileTypeCheckStateIsDirty(funcVal)
        InteractiveChecker(FSharpCompiler.Current.InteractiveCheckerType?Create(dirty))
        
      /// Parse a source code file, returning a handle that can be used for obtaining navigation bar information
      /// To get the full information, call 'TypeCheckSource' method on the result
      member x.UntypedParse(filename:string, source:string, options:CheckOptions) : UntypedParseInfo =
        UntypedParseInfo(wrapped?UntypedParse(filename, source, options.Wrapped))

      /// Typecheck a source code file, returning a handle to the results of the parse including
      /// the reconstructed types in the file.
      ///
      /// Return None if the background builder is not yet done prepring the type check results for the antecedent to the 
      /// file.
      member x.TypeCheckSource( parsed:UntypedParseInfo, filename:string, fileversion:int, source:string, options:CheckOptions, (IsResultObsolete f)) =
        let isResultObsolete = 
            let fsc = FSharpCompiler.Current
            let f2 = fsc.MakeFunction(fsc.UnitType,typeof<bool>, Converter<obj,obj>(fun obj -> f () |> box))
            fsc.IsResultObsoleteType?NewIsResultObsolete(f2)
        let res : obj = 
            let fsc = FSharpCompiler.Current
            match fsc.ActualVersion with 
            | FSharp_2_0 -> wrapped?TypeCheckSource(parsed.Wrapped, filename, fileversion, source, options.Wrapped, isResultObsolete ) 
            | FSharp_3_0 -> wrapped?TypeCheckSource(parsed.Wrapped, filename, fileversion, source, options.Wrapped, isResultObsolete, null (* textSnapshotInfo *) ) 
        TypeCheckAnswer(res)
      
      /// For a given script file, get the CheckOptions implied by the #load closure
      member x.GetCheckOptionsFromScriptRoot(filename:string, source:string, loadedTimeStamp:System.DateTime) : CheckOptions =
        // GetCheckOptionsFromScriptRoot takes an extra argument in 4.3.0.0. Ignore it in 4.0.0.0
          let fsc = FSharpCompiler.Current
          match fsc.ActualVersion with 
          | FSharp_2_0 -> CheckOptions(wrapped?GetCheckOptionsFromScriptRoot(filename, source))
          | FSharp_3_0 -> CheckOptions(wrapped?GetCheckOptionsFromScriptRoot(filename, source, loadedTimeStamp))
          

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
