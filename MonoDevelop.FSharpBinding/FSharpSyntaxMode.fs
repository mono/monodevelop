namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Highlighting
open MonoDevelop.Core
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore.Control
open MonoDevelop.FSharp.Symbols

[<AutoOpen>]
module Patterns = 
  type TokenSymbol = 
    { TokenInfo : FSharpTokenInfo
      SymbolUse : FSharpSymbolUse option
      ExtraColorInfo : (Range.range * FSharpTokenColorKind) option }
  
  let (|Keyword|_|) ts = 
    match ts.TokenInfo.ColorClass, ts.ExtraColorInfo with
    | FSharpTokenColorKind.Keyword, _ -> Some ts
    | _, Some(_range, extra) when extra = FSharpTokenColorKind.Keyword -> Some ts
    | _ -> None
  
  let (|Punctuation|_|) (ts : TokenSymbol) = 
    if ts.TokenInfo.Tag = FSharpTokenTag.PLUS_MINUS_OP
       || ts.TokenInfo.Tag = FSharpTokenTag.MINUS
       || ts.TokenInfo.Tag = FSharpTokenTag.STAR 
       || ts.TokenInfo.Tag = FSharpTokenTag.INFIX_STAR_DIV_MOD_OP
       || ts.TokenInfo.Tag = FSharpTokenTag.PERCENT_OP 
       || ts.TokenInfo.Tag = FSharpTokenTag.INFIX_AT_HAT_OP
       || ts.TokenInfo.Tag = FSharpTokenTag.QMARK 
       || ts.TokenInfo.Tag = FSharpTokenTag.COLON
       || ts.TokenInfo.Tag = FSharpTokenTag.EQUALS
       || ts.TokenInfo.Tag = FSharpTokenTag.SEMICOLON 
       || ts.TokenInfo.Tag = FSharpTokenTag.COMMA
       || ts.TokenInfo.Tag = FSharpTokenTag.DOT
       || ts.TokenInfo.Tag = FSharpTokenTag.DOT_DOT 
       || ts.TokenInfo.Tag = FSharpTokenTag.INT32_DOT_DOT
       || ts.TokenInfo.Tag = FSharpTokenTag.UNDERSCORE 
       || ts.TokenInfo.Tag = FSharpTokenTag.BAR
       || ts.TokenInfo.Tag = FSharpTokenTag.COLON_GREATER 
       || ts.TokenInfo.Tag = FSharpTokenTag.COLON_QMARK_GREATER
       || ts.TokenInfo.Tag = FSharpTokenTag.COLON_QMARK 
       || ts.TokenInfo.Tag = FSharpTokenTag.INFIX_BAR_OP
       || ts.TokenInfo.Tag = FSharpTokenTag.INFIX_COMPARE_OP 
       || ts.TokenInfo.Tag = FSharpTokenTag.COLON_COLON
       || ts.TokenInfo.Tag = FSharpTokenTag.AMP_AMP 
       || ts.TokenInfo.Tag = FSharpTokenTag.PREFIX_OP
       || ts.TokenInfo.Tag = FSharpTokenTag.COLON_EQUALS 
       || ts.TokenInfo.Tag = FSharpTokenTag.BAR_BAR
       || ts.TokenInfo.Tag = FSharpTokenTag.RARROW
    then Some Punctuation
    else None
  
  let (|PunctuationBrackets|_|) (ts : TokenSymbol) = 
    if ts.TokenInfo.Tag = FSharpTokenTag.LPAREN
       || ts.TokenInfo.Tag = FSharpTokenTag.RPAREN
       || ts.TokenInfo.Tag = FSharpTokenTag.LBRACK 
       || ts.TokenInfo.Tag = FSharpTokenTag.RBRACK
       || ts.TokenInfo.Tag = FSharpTokenTag.LBRACE
       || ts.TokenInfo.Tag = FSharpTokenTag.RBRACE 
       || ts.TokenInfo.Tag = FSharpTokenTag.LBRACK_LESS
       || ts.TokenInfo.Tag = FSharpTokenTag.GREATER_RBRACK 
       || ts.TokenInfo.Tag = FSharpTokenTag.LESS
       || ts.TokenInfo.Tag = FSharpTokenTag.GREATER 
       || ts.TokenInfo.Tag = FSharpTokenTag.LBRACK_BAR
       || ts.TokenInfo.Tag = FSharpTokenTag.BAR_RBRACK
    then Some PunctuationBrackets
    else None
  
  let (|Comment|_|) ts = 
    if ts.TokenInfo.ColorClass = FSharpTokenColorKind.Comment then Some Comment else None
  
  let (|StringLiteral|_|) ts = 
    if ts.TokenInfo.ColorClass = FSharpTokenColorKind.String then Some StringLiteral else None
  
  let (|NumberLiteral|_|) ts = 
    if ts.TokenInfo.ColorClass = FSharpTokenColorKind.Number then Some NumberLiteral else None
  
  let (|InactiveCode|_|) ts = 
    if ts.TokenInfo.ColorClass = FSharpTokenColorKind.InactiveCode then Some InactiveCode else None
  
  let (|PreprocessorKeyword|_|) ts = 
    if ts.TokenInfo.ColorClass = FSharpTokenColorKind.PreprocessorKeyword then Some PreprocessorKeyword else None
  
  let inline private isIdentifier symbol = 
    match symbol with FSharpTokenColorKind.Identifier | FSharpTokenColorKind.UpperIdentifier -> true | _ -> false
  
  let inline isSimpleToken symbol = 
    match symbol with FSharpTokenColorKind.Identifier | FSharpTokenColorKind.UpperIdentifier -> false | _ -> true
  
  let (|IdentifierSymbol|_|) ts = 
    if isIdentifier ts.TokenInfo.ColorClass then ts.SymbolUse |> Option.map (fun su -> IdentifierSymbol(su)) else None
  
  let (|Namespace|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Namespace ns -> Some ns
      | _ -> None
    | _ -> None
  
  let (|Class|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse ->
      match symbolUse with
      | ExtendedPatterns.Class cl -> Some cl
      | _ -> None
    | _ -> None
  
  let (|Property|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Property _pr -> Some symbolUse.IsFromDefinition
      | _ -> None
    | _ -> None
  
  let (|Field|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | CorePatterns.Field _ -> Some symbolUse.IsFromDefinition
      | _ -> None
    | _ -> None
  
  let (|Function|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Function _ | ExtendedPatterns.ClosureOrNestedFunction _ -> Some symbolUse.IsFromDefinition
      | _ -> None
    | _ -> None
  
  let (|Val|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Val _ -> Some symbolUse.IsFromDefinition
      | _ -> None
    | _ -> None
  
  let (|Delegate|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Delegate dl -> Some dl
      | _ -> None
    | _ -> None
  
  let (|Event|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Event _ev -> Some symbolUse.IsFromDefinition
      | _ -> None
    | _ -> None
  
  let (|Enum|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Enum en -> Some en
      | _ -> None
    | _ -> None
  
  let (|Record|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Record r -> Some r
      | _ -> None
    | _ -> None
  
  let (|ValueType|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.ValueType v -> Some v
      | _ -> None
    | _ -> None
  
  let (|Module|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Module m -> Some m
      | _ -> None
    | _ -> None
  
  let (|Union|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Union u -> Some u
      | _ -> None
    | _ -> None
  
  let (|GenericParameter|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | CorePatterns.GenericParameter _ -> Some GenericParameter
      | _ -> None
    | _ -> None
  
  let (|UnionCase|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | CorePatterns.UnionCase _ -> Some UnionCase
      | _ -> None
    | _ -> None
  
  let (|ActivePatternCase|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | CorePatterns.ActivePatternCase _ -> Some ActivePatternCase
      | _ -> None
    | _ -> None
  
  let (|Interface|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.Interface _ -> Some Interface
      | _ -> None
    | _ -> None
  
  let (|TypeAbbreviation|_|) ts = 
    match ts with
    | IdentifierSymbol symbolUse -> 
      match symbolUse with
      | ExtendedPatterns.TypeAbbreviation _ -> Some TypeAbbreviation
      | _ -> None
    | _ -> None
  
  let (|ComputationExpression|_|) ts = 
    if isIdentifier ts.TokenInfo.ColorClass
    then
      match ts.SymbolUse with
      | Some su when su.IsFromComputationExpression ->
        Some su.Symbol.DisplayName
      | _ -> None
    else None

module internal Rules = 
  let baseMode = 
    let assembly = Reflection.Assembly.GetExecutingAssembly()
    let manifest = assembly.GetManifestResourceNames() |> Seq.tryFind (fun s -> s.Contains("FSharpSyntaxMode"))
    manifest
    |> Option.map (fun manifest -> 
                     let provider = ResourceStreamProvider(assembly, manifest)
                     use stream = provider.Open()
                     let baseMode = SyntaxMode.Read(stream)
                     baseMode)

module Keywords = 
  let getType (scheme : ColorScheme) (token : TokenSymbol) = 
    match Rules.baseMode with
    | Some mode -> 
      Option.ofNull (mode.GetKeyword(token.TokenInfo.TokenName.ToLowerInvariant()))
      |> Option.map (fun keywords -> scheme.GetChunkStyle keywords.Color)
      |> Option.fill scheme.KeywordTypes
    | None -> scheme.KeywordTypes

type FSharpSyntaxMode(editor, context) = 
  inherit SemanticHighlighting(editor, context)
  let mutable segments = None
  
  let makeChunk (symbolsInFile : FSharpSymbolUse array option) colourisations lineNo lineOffset (style : ColorScheme) token = 
    let symbol = 
      if isSimpleToken token.ColorClass then None
      else
        symbolsInFile
        |> Option.bind (Array.tryFind (fun s -> s.RangeAlternate.StartLine = lineNo && s.RangeAlternate.EndColumn = token.RightColumn + 1))
    
    let extraColor = 
      colourisations
      |> Option.bind (Array.tryFind (fun (rng : Range.range, _) -> rng.StartLine = lineNo && rng.EndColumn = token.RightColumn + 1))
    
    let tokenSymbol = 
      { TokenInfo = token; SymbolUse = symbol; ExtraColorInfo = extraColor }
    
    let chunkStyle = 
      match tokenSymbol with
      | InactiveCode -> style.ExcludedCode
      | ComputationExpression _name -> style.KeywordTypes
      | Punctuation -> style.Punctuation
      | PunctuationBrackets -> style.PunctuationForBrackets
      | Keyword ts -> Keywords.getType style ts
      | Comment -> style.CommentsSingleLine
      | StringLiteral -> style.String
      | NumberLiteral -> style.Number
      | Module _ | ActivePatternCase | Record _ | Union _ | TypeAbbreviation | Class _ -> style.UserTypes
      | Namespace _ -> style.KeywordNamespace
      | Property fromDef -> if fromDef then style.UserPropertyDeclaration else style.UserPropertyUsage
      | Field fromDef -> if fromDef then style.UserFieldDeclaration else style.UserFieldUsage
      | Function fromDef -> if fromDef then style.UserMethodDeclaration else style.UserMethodUsage
      | Val fromDef -> if fromDef then style.UserFieldDeclaration else style.UserFieldUsage
      | UnionCase | Enum _ -> style.UserTypesEnums
      | Delegate _ -> style.UserTypesDelegates
      | Event fromDef -> if fromDef then style.UserEventDeclaration else style.UserEventUsage
      | Interface -> style.UserTypesInterfaces
      | ValueType _ -> style.UserTypesValueTypes
      | PreprocessorKeyword -> style.Preprocessor
      | _ -> style.PlainText
    
    let seg = ColoredSegment(lineOffset + token.LeftColumn, token.RightColumn - token.LeftColumn + 1, chunkStyle.Name)
    //Uncomment to visualise tokens segments
    //LoggingService.LogInfo (sprintf """Segment: %s S:%i E:%i L:%i - "%s" """ seg.ColorStyleKey seg.Offset seg.EndOffset seg.Length (editor.GetTextBetween (seg.Offset, seg.EndOffset)) )
    seg
  
  override x.DocumentParsed() = 
    let processedTokens = 
      maybe { 
        let! localParsedDocument = context.ParsedDocument |> Option.ofNull
        let! pd = localParsedDocument |> Option.tryCast<FSharpParsedDocument>
        let! checkResults = pd.Ast |> Option.tryCast<ParseAndCheckResults>
        let symbolsInFile = 
          try Async.RunSynchronously(checkResults.GetAllUsesOfAllSymbolsInFile(), ServiceSettings.maximumTimeout)
          with _ -> None
        
        let colourisations = checkResults.GetExtraColorizations()
        let lineDetails = 
          editor.GetLines() |> Seq.map (fun line -> Tokens.LineDetail(line.LineNumber, line.Offset, editor.GetLineText line))
        let defines = CompilerArguments.getDefineSymbols context.Name (context.Project |> Option.ofNull)
        
        let processedTokens = 
          let style = getColourScheme()
          
          let tokens = 
            match pd.Tokens with
            | Some t -> t
            | None -> Tokens.getTokens lineDetails context.Name defines
          tokens
          |> List.map 
               (fun (Tokens.TokenisedLine(lineNumber, lineOffset, chunks, _state)) -> 
                  chunks |> List.map (makeChunk symbolsInFile colourisations lineNumber lineOffset style))
          |> Array.ofList
        return processedTokens
      }
    processedTokens |> Option.iter (fun _ -> 
                         segments <- processedTokens
                         Gtk.Application.Invoke(fun _ _ -> x.NotifySemanticHighlightingUpdate()))
  
  override x.GetColoredSegments(segment) = 
    let line = editor.GetLineByOffset segment.Offset
    let lineNumber = line.LineNumber
    match segments with
    | Some segments when segments.Length >= lineNumber -> segments.[lineNumber - 1] |> List.toSeq
    | _ -> Seq.empty