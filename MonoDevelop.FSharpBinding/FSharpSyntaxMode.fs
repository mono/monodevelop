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
open FSharp.CompilerBinding
open ExtCore.Control
open MonoDevelop.FSharp.Symbols
[<AutoOpen>]
module Patterns =
    type TokenSymbol = 
        {
            TokenInfo : FSharpTokenInfo;
            SymbolUse: FSharpSymbolUse option
            ExtraColorInfo: (Range.range * FSharpTokenColorKind) option
        }

    let (|Keyword|_|) ts =
        match ts.TokenInfo.ColorClass, ts.ExtraColorInfo with
        | FSharpTokenColorKind.Keyword, _ ->
            Some ts
        | _, Some (_range, extra) when extra = FSharpTokenColorKind.Keyword -> 
            Some ts
        | _ -> None

    let (|Punctuation|_|) (ts:TokenSymbol) =
        let token = Parser.tokenTagToTokenId ts.TokenInfo.Tag
        match token with
        | Parser.tokenId.TOKEN_PLUS_MINUS_OP
        | Parser.tokenId.TOKEN_MINUS
        | Parser.tokenId.TOKEN_STAR
        | Parser.tokenId.TOKEN_INFIX_STAR_DIV_MOD_OP
        | Parser.tokenId.TOKEN_PERCENT_OP
        | Parser.tokenId.TOKEN_INFIX_AT_HAT_OP
        | Parser.tokenId.TOKEN_QMARK
        | Parser.tokenId.TOKEN_COLON
        | Parser.tokenId.TOKEN_EQUALS
        | Parser.tokenId.TOKEN_SEMICOLON
        | Parser.tokenId.TOKEN_COMMA
        | Parser.tokenId.TOKEN_DOT
        | Parser.tokenId.TOKEN_DOT_DOT
        | Parser.tokenId.TOKEN_INT32_DOT_DOT
        | Parser.tokenId.TOKEN_UNDERSCORE
        | Parser.tokenId.TOKEN_BAR
        | Parser.tokenId.TOKEN_BAR_RBRACK
        | Parser.tokenId.TOKEN_LBRACK_LESS
        | Parser.tokenId.TOKEN_COLON_GREATER
        | Parser.tokenId.TOKEN_COLON_QMARK_GREATER
        | Parser.tokenId.TOKEN_COLON_QMARK
        | Parser.tokenId.TOKEN_INFIX_BAR_OP
        | Parser.tokenId.TOKEN_INFIX_COMPARE_OP
        | Parser.tokenId.TOKEN_COLON_COLON
        | Parser.tokenId.TOKEN_AMP_AMP
        | Parser.tokenId.TOKEN_PREFIX_OP
        | Parser.tokenId.TOKEN_COLON_EQUALS
        | Parser.tokenId.TOKEN_BAR_BAR
        | Parser.tokenId.TOKEN_RARROW
            -> Some Punctuation
        | _ -> None

    let (|PunctuationBrackets|_|) (ts:TokenSymbol) =
        let token = Parser.tokenTagToTokenId ts.TokenInfo.Tag
        match token with
        | Parser.tokenId.TOKEN_LPAREN
        | Parser.tokenId.TOKEN_RPAREN
        | Parser.tokenId.TOKEN_LBRACK
        | Parser.tokenId.TOKEN_RBRACK
        | Parser.tokenId.TOKEN_LBRACE
        | Parser.tokenId.TOKEN_RBRACE 
        | Parser.tokenId.TOKEN_LBRACK_LESS
        | Parser.tokenId.TOKEN_GREATER_RBRACK
        | Parser.tokenId.TOKEN_LESS
        | Parser.tokenId.TOKEN_GREATER
        | Parser.tokenId.TOKEN_LBRACK_BAR
        | Parser.tokenId.TOKEN_BAR_RBRACK -> Some PunctuationBrackets
        | _ -> None

    let (|Comment|_|) ts =
        if ts.TokenInfo.ColorClass = FSharpTokenColorKind.Comment then Some Comment
        else None

    let (|StringLiteral|_|) ts =
        if ts.TokenInfo.ColorClass = FSharpTokenColorKind.String then Some StringLiteral
        else None

    let (|NumberLiteral|_|) ts =
        if ts.TokenInfo.ColorClass = FSharpTokenColorKind.Number then Some NumberLiteral
        else None

    let (|InactiveCode|_|) ts =
        if ts.TokenInfo.ColorClass = FSharpTokenColorKind.InactiveCode then Some InactiveCode
        else None

    let (|PreprocessorKeyword|_|) ts =
        if ts.TokenInfo.ColorClass = FSharpTokenColorKind.PreprocessorKeyword then Some PreprocessorKeyword
        else None

    let private isIdentifier =
        function
        | FSharpTokenColorKind.Identifier
        | FSharpTokenColorKind.UpperIdentifier -> true
        | _ -> false

    let isSimpleToken tck =
        match tck with
        | FSharpTokenColorKind.Identifier
        | FSharpTokenColorKind.UpperIdentifier -> false
        | _ -> true

    let (|IdentifierSymbol|_|) ts =
        if isIdentifier ts.TokenInfo.ColorClass then
            ts.SymbolUse |> Option.map (fun su -> IdentifierSymbol(su))
        else None

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
            | ExtendedPatterns.Function _
            | ExtendedPatterns.ClosureOrNestedFunction _ ->  Some symbolUse.IsFromDefinition
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
        if isIdentifier ts.TokenInfo.ColorClass then
            match ts.SymbolUse with
            | Some symbolUse when symbolUse.IsFromComputationExpression -> Some symbolUse.Symbol.DisplayName
            | _ -> None
        else None

module internal Rules =
    let baseMode =
        let assembly = Reflection.Assembly.GetExecutingAssembly ()
        let manifest =
            assembly.GetManifestResourceNames ()
            |> Seq.tryFind (fun s -> s.Contains ("FSharpSyntaxMode"))
        
        manifest
        |> Option.map (fun manifest ->
            let provider = new ResourceStreamProvider (assembly, manifest)
            use stream = provider.Open ()
            let baseMode = SyntaxMode.Read (stream)
            baseMode)

module Keywords =
    let getType (scheme:ColorScheme) (token:TokenSymbol) =
        match Rules.baseMode with
        | Some mode -> Option.ofNull (mode.GetKeyword (token.TokenInfo.TokenName.ToLowerInvariant ()))
                       |> Option.map (fun keywords -> scheme.GetChunkStyle keywords.Color)
                       |> Option.fill scheme.KeywordTypes
        | None -> scheme.KeywordTypes

module Tokens =
    let (|InactiveCode|_|) = function
        | token when token.ColorClass = FSharpTokenColorKind.InactiveCode -> Some token
        | _ -> None

    let (|InactiveCodeSection|_|) = function
       | InactiveCode x :: xs -> 
           let rec loop ts acc = 
               match ts with 
               | InactiveCode head :: rest ->
                   loop rest (head :: acc) 
               | _ -> List.rev acc, ts
           Some (loop xs [x])
       | _ -> None

    let CollapseInactiveTokens tokens =
        let rec loop tokens =
            [   match tokens with
                | InactiveCodeSection (inactiveSegs, rest) ->
                    //collapse inactivesegs
                    let startSeg = inactiveSegs |> List.head
                    let endSeg = inactiveSegs |> List.last
                    let combinedToken =
                        {startSeg with RightColumn = endSeg.RightColumn
                                       FullMatchedLength = endSeg.RightColumn - startSeg.LeftColumn}
                    yield combinedToken
                    yield! loop rest
                | head::tail -> yield head
                                yield! loop tail
                | [] -> () ]
        loop tokens


type FSharpSyntaxMode(editor, context) =
    inherit MonoDevelop.Ide.Editor.Highlighting.SemanticHighlighting(editor, context)

    let mutable segments = [||]

    let makeChunk symbolsInFile colourisations (line: IDocumentLine) (style: ColorScheme) (token: FSharpTokenInfo) =
        let lineNumber = line.LineNumber
        let symbol =
            if isSimpleToken token.ColorClass then None else
            match symbolsInFile with
            | None -> None
            | Some(symbols) ->
                symbols
                |> Array.tryFind (fun (s:FSharpSymbolUse) -> s.RangeAlternate.StartLine = lineNumber && s.RangeAlternate.EndColumn = token.RightColumn+1)

        let extraColor =
            match colourisations with
            | None -> None
            | Some(extraColourInfo) ->
                extraColourInfo
                |> Array.tryFind (fun (rng:Range.range, _) -> rng.StartLine = lineNumber && rng.EndColumn = token.RightColumn+1)

        let tokenSymbol = { TokenInfo = token; SymbolUse = symbol; ExtraColorInfo = extraColor }
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
            | Module _|ActivePatternCase|Record _|Union _|TypeAbbreviation|Class _ -> style.UserTypes
            | Namespace _ -> style.KeywordNamespace
            | Property fromDef -> if fromDef then style.UserPropertyDeclaration else style.UserPropertyUsage
            | Field fromDef -> if fromDef then style.UserFieldDeclaration else style.UserFieldUsage
            | Function fromDef -> if fromDef then style.UserMethodDeclaration else style.UserMethodUsage
            | Val fromDef -> if fromDef then style.UserFieldDeclaration else style.UserFieldUsage
            | UnionCase | Enum _ -> style.UserTypesEnums
            | Delegate _-> style.UserTypesDelegates
            | Event fromDef -> if fromDef then style.UserEventDeclaration else style.UserEventUsage
            | Interface -> style.UserTypesInterfaces
            | ValueType _ -> style.UserTypesValueTypes
            | PreprocessorKeyword -> style.Preprocessor
            | _ -> style.PlainText
 
        let seg = ColoredSegment(line.Offset + token.LeftColumn, token.RightColumn - token.LeftColumn + 1, chunkStyle.Name)
        //Uncomment to visualise tokens segments
        //LoggingService.LogInfo (sprintf """Segment: %s S:%i E:%i L:%i - "%s" """ seg.ColorStyleKey seg.Offset seg.EndOffset seg.Length (editor.GetTextBetween (seg.Offset, seg.EndOffset)) )
        seg

    override x.DocumentParsed () =
        let localParsedDocument = context.ParsedDocument
        if localParsedDocument <> null then
            let parseAndCheckResults = localParsedDocument.Ast |> tryCast<ParseAndCheckResults>
            match parseAndCheckResults with
            | Some pd ->
                let symbolsInFile =
                    try Async.RunSynchronously (pd.GetAllUsesOfAllSymbolsInFile(), ServiceSettings.maximumTimeout)
                    with _ -> None 
                let colourisations = pd.GetExtraColorizations ()
                let alllines = editor.GetLines()

                // Create source tokenizer
                let defines = CompilerArguments.getDefineSymbols context.Name (Some(context.Project))
                let sourceTok = SourceTokenizer(defines, context.Name)

                // Parse lines using the tokenizer
                let tokens = 
                    [| let state = ref 0L
                      for line in alllines do
                        let lineTxt = editor.GetLineText(line)
                        let tokenizer = sourceTok.CreateLineTokenizer(lineTxt)
                        let rec parseLine() =
                            [ match tokenizer.ScanToken(!state) with
                              | Some(tok), nstate ->
                                  yield tok
                                  state := nstate
                                  yield! parseLine()
                              | None, nstate -> state := nstate ]
                        yield line, parseLine() |]
                                        
                let processedTokens =
                    let style = getColourScheme ()
                    tokens
                    |> Array.map
                        (fun (line, chunks) ->
                            chunks
                            |> Tokens.CollapseInactiveTokens
                            |> List.map (makeChunk symbolsInFile colourisations line style))
                segments <- processedTokens
                Gtk.Application.Invoke (fun _ _ -> x.NotifySemanticHighlightingUpdate())
            | None -> ()

    override x.GetColoredSegments (segment) =
        let line = editor.GetLineByOffset segment.Offset
        let lineNumber = line.LineNumber
        if segments.Length >= lineNumber
        then segments.[lineNumber-1] |> List.toSeq
        else Seq.empty