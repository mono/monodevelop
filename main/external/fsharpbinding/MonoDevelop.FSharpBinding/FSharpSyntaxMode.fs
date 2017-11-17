namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Collections.Immutable
open MonoDevelop.FSharp.Shared
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Highlighting
open MonoDevelop.Core
open Mono.TextEditor.Highlighting
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore.Control
open MonoDevelop
open Gtk

[<AutoOpen>]
module Patterns =
    type TokenSymbol =
        { TokenInfo : FSharpTokenInfo
          SymbolUse : FSharpSymbolUse option
          ExtraColorInfo : (Range.range * SemanticClassificationType) option }

    let (|Keyword|_|) ts =
        match ts.TokenInfo.ColorClass, ts.ExtraColorInfo with
        | FSharpTokenColorKind.Keyword, _ -> Some ts
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
        if isIdentifier ts.TokenInfo.ColorClass
        then ts.SymbolUse |> Option.map (fun su -> IdentifierSymbol(su))
        else None
        
    let (|Namespace|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Namespace ns -> Some ns
            | _ -> None
        | _ -> None

    let (|Class|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Class cl -> Some cl
            | _ -> None
        | _ -> None

    let (|Property|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Property _pr -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Field|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Field f -> Some (symbolUse.IsFromDefinition, f.IsMutable)
            | _ -> None
        | _ -> None

    let (|Function|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Function _ | SymbolUse.ClosureOrNestedFunction _  -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None
        
    let (|Constructor|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Constructor _  -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Val|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Val v ->
                let isMut = v.IsMutable && (match v.EnclosingEntity with Some de -> not de.IsEnum | None -> v.IsMutable)
                Some (symbolUse, isMut)
            | _ -> None
        | _ -> None

    let (|Delegate|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Delegate dl -> Some dl
            | _ -> None
        | _ -> None

    let (|Event|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Event _ev -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Enum|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Enum en -> Some en
            | _ -> None
        | _ -> None

    let (|Record|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Record r -> Some r
            | _ -> None
        | _ -> None

    let (|ValueType|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.ValueType v -> Some v
            | _ -> None
        | _ -> None

    let (|Module|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Module m -> Some m
            | _ -> None
        | _ -> None

    let (|Union|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Union u -> Some u
            | _ -> None
        | _ -> None

    let (|GenericParameter|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.GenericParameter _ -> Some GenericParameter
            | _ -> None
        | _ -> None

    let (|UnionCase|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.UnionCase _ -> Some UnionCase
            | _ -> None
        | _ -> None

    let (|ActivePatternCase|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.ActivePatternCase _ -> Some ActivePatternCase
            | _ -> None
        | _ -> None

    let (|Interface|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.Interface _ -> Some Interface
            | _ -> None
        | _ -> None

    let (|TypeAbbreviation|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse with
            | SymbolUse.TypeAbbreviation _ -> Some TypeAbbreviation
            | _ -> None
        | _ -> None

    let (|WildcardIdentifier|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            if symbolUse.Symbol.DisplayName.StartsWith "_" then
                Some WildcardIdentifier
            else
                None
        | _ -> None

    let (|ComputationExpression|_|) ts =
        match ts.ExtraColorInfo with
        | Some(_range, extra) when extra = SemanticClassificationType.ComputationExpression -> Some ts
        | _ -> None

    module SyntaxMode =
        let makeChunk (symbolsInFile:IDictionary<_,_>) lineNo lineOffset (colourisations:(Range.range * SemanticClassificationType) [] option) token =
            let symbol =
                if token.CharClass = FSharpTokenCharKind.Identifier || token.CharClass = FSharpTokenCharKind.Operator then
                    match symbolsInFile.TryGetValue(Range.mkPos lineNo (token.RightColumn + 1)) with
                    | true, v -> Some(v)
                    | _ -> None
                else None
    
            let extraColor =
                if token.CharClass = FSharpTokenCharKind.Identifier || token.CharClass = FSharpTokenCharKind.Operator then
                    colourisations
                    |> Option.bind (Array.tryFind (fun (rng : Range.range, _) -> rng.StartLine = lineNo && rng.EndColumn = token.RightColumn + 1))
                else None
    
            let highlightMutable isMut = isMut && PropertyService.Get("FSharpBinding.HighlightMutables", false)

            let makeSeg (chunkStyle:string) =
               ColoredSegment(lineOffset + token.LeftColumn, token.RightColumn - token.LeftColumn + 1, ScopeStack.Empty.Push(chunkStyle))
               |> Some
                //Uncomment to visualise tokens segments
                //LoggingService.LogInfo (sprintf """Segment: %s S:%i E:%i L:%i - "%s" """ seg.ColorStyleKey seg.Offset seg.EndOffset seg.Length (editor.GetTextBetween (seg.Offset, seg.EndOffset)) )
         
            let tryGetStyle =
                match { TokenInfo = token; SymbolUse = symbol; ExtraColorInfo = extraColor } with
                | WildcardIdentifier ->
                    makeSeg "source.fs"
                | InactiveCode ->
                    makeSeg "punctuation.definition.comment.source"
                | ComputationExpression _name ->
                    makeSeg "keyword.other.source"
                | Module _ | ActivePatternCase | Record _ | Union _ | TypeAbbreviation | Class _ | Constructor _ ->
                    makeSeg EditorThemeColors.UserTypes
                | GenericParameter _ ->
                    makeSeg EditorThemeColors.UserTypesTypeParameters
                | Namespace _ ->
                    makeSeg "source.fs"
                | Property fromDef ->
                    if fromDef then makeSeg EditorThemeColors.UserPropertyDeclaration 
                    else makeSeg EditorThemeColors.UserPropertyUsage
                | Field (fromDef, isMut) ->
                    if highlightMutable isMut then makeSeg EditorThemeColors.UserTypesMutable
                    elif fromDef then makeSeg EditorThemeColors.UserFieldDeclaration
                    else makeSeg EditorThemeColors.UserFieldUsage
                | Function fromDef ->
                    if fromDef then makeSeg EditorThemeColors.UserMethodDeclaration
                    else makeSeg EditorThemeColors.UserMethodUsage
                | Val (su, isMut) ->
                    if highlightMutable isMut then makeSeg EditorThemeColors.UserTypesMutable
                    //elif su.Symbol.DisplayName.StartsWith "_" then style.ExcludedCode 
                    elif su.IsFromDefinition then makeSeg EditorThemeColors.UserFieldDeclaration
                    else makeSeg EditorThemeColors.UserFieldUsage
                | UnionCase | Enum _ ->
                    makeSeg EditorThemeColors.UserTypes
                | Delegate _ ->
                    makeSeg EditorThemeColors.UserTypesDelegates
                | Event fromDef ->
                    if fromDef then makeSeg EditorThemeColors.UserEventDeclaration
                    else makeSeg EditorThemeColors.UserEventUsage
                | Interface ->
                    makeSeg EditorThemeColors.UserTypesInterfaces
                | ValueType _ ->
                    makeSeg EditorThemeColors.UserTypesValueTypes
                //| Keyword ts -> makeSeg (Keywords.getType style ts)
                //| Comment -> makeSeg style.CommentsSingleLine
                //| StringLiteral -> makeSeg style.String
                //| NumberLiteral -> makeSeg style.Number
                //| Punctuation -> None //makeSeg style.Punctuation
                //| PunctuationBrackets -> None //makeSeg style.PunctuationForBrackets
                //| PreprocessorKeyword -> None//Some style.Preprocessor
                | _other -> None
            tryGetStyle
            
        let tryGetTokensSymbolsAndColours (context:DocumentContext) =
            maybe {
                let! pd = context.TryGetFSharpParsedDocument()
                let! checkResults = pd.TryGetAst()
                let! tokens = pd.Tokens 
                let symbolsInFile = pd.AllSymbolsKeyed
                let colourisations = checkResults.GetExtraColorizations None
                let formatters = checkResults.GetStringFormatterColours()
                return tokens, symbolsInFile, colourisations, formatters }

        let getColouredSegment tokenssymbolscolours lineNumber lineOffset txt =
            match tokenssymbolscolours with
            | Some (tokens:_ list, symbols, colours, _formatters) when tokens.Length >= lineNumber ->
                let tokens, _lineText = tokens.[lineNumber-1]
                tokens
                |> Lexer.fixTokens txt
                |> List.choose (fun draft -> makeChunk symbols lineNumber lineOffset colours {draft.Token with RightColumn = draft.RightColumn} )
                |> List.toSeq
            | _ -> Seq.empty


type FSharpSyntaxMode(editor, context) =
    inherit SemanticHighlighting(editor, context)
    let tokenssymbolscolours = ref None
    //let style = ref (getColourScheme())
    //let colourSchemChanged =
    //    IdeApp.Preferences.ColorScheme.Changed.Subscribe
    //        (fun _ (eventArgs:EventArgs) ->
    //                          let colourStyles = SyntaxModeService.GetColorStyle(IdeApp.Preferences.ColorScheme.Value)
    //                          (*style := colourStyles*) )
                                  
    override x.DocumentParsed() =
        if MonoDevelop.isDocumentVisible context.Name then
            SyntaxMode.tryGetTokensSymbolsAndColours context
            |> Option.iter (fun tsc -> tokenssymbolscolours := Some tsc
                                       Application.Invoke(fun _ _ -> x.NotifySemanticHighlightingUpdate()))

    override x.GetColoredSegments(segment) =
        let line = editor.GetLineByOffset segment.Offset
        let lineNumber = line.LineNumber
        let txt = editor.GetLineText line

        SyntaxMode.getColouredSegment !tokenssymbolscolours lineNumber line.Offset txt// !style
        
    //interface IDisposable with member x.Dispose() = colourSchemChanged.Dispose()