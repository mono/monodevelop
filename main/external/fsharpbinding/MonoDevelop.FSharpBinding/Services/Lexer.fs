namespace MonoDevelop.FSharp

open System.Diagnostics
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.Core

type SymbolKind =
    | Ident
    | Operator
    | GenericTypeParameter
    | StaticallyResolvedTypeParameter
    | ActivePattern
    | Other

type LexerSymbol =
    { Kind: SymbolKind
      Line: int
      LeftColumn: int
      RightColumn: int
      Text: string }
    member x.Range = x.Line, x.LeftColumn, x.Line, x.RightColumn

[<RequireQualifiedAccess>]
type SymbolLookupKind =
    | Fuzzy
    | ByRightColumn
    | ByLongIdent
    | Simple

type internal DraftToken =
    { Kind: SymbolKind
      Token: FSharpTokenInfo
      RightColumn: int }
    static member inline Create kind token =
        { Kind = kind; Token = token; RightColumn = token.LeftColumn + token.FullMatchedLength - 1 }

type TokenisedLine = TokenisedLine of tokens:FSharpTokenInfo list * stateAtEOL:int64

module Lexer =
    /// Get the array of all lex states in current source
    let internal getLexStates defines (source: string) =
        [|
            /// Iterate through the whole line to get the final lex state
            let rec loop (lineTokenizer: FSharpLineTokenizer) lexState =
                match lineTokenizer.ScanToken lexState with
                | None, newLexState -> newLexState
                | Some _, newLexState ->
                    loop lineTokenizer newLexState

            let sourceTokenizer = FSharpSourceTokenizer(defines, None)
            let lines = String.getLines source
            let mutable lexState = 0L
            for line in lines do
                yield lexState
                let lineTokenizer = sourceTokenizer.CreateLineTokenizer line
                lexState <- loop lineTokenizer lexState
        |]

    /// Provide a default implementation where we cache lex states of the current document.
    /// Assume that current document will be queried repeatedly
    /// Note: The source and defines are equality checked whenever this function is used which 
    /// means that currentDocumentState is rebuilt whenever a different source or defines list is presented
    let queryLexState =
        let mutable currentDocumentState = ref None
        fun source defines line ->
            let lexStates =
                match !currentDocumentState with
                | Some (lexStates, s, d) when s = source && d = defines ->
                    lexStates
                // OPTIMIZE: if the new document has the current document as a prefix,
                // we can reuse lexing results and process only the added part.
                | _ ->
                    LoggingService.LogDebug "queryLexState: lexing current document"
                    let lexStates = getLexStates defines source
                    currentDocumentState := Some (lexStates, source, defines)
                    lexStates
            Debug.Assert(line >= 0 && line < Array.length lexStates, "Should have lex states for every line.")
            lexStates.[line]

    let singleLineQueryLexState _ _ _ = 0L

    /// Return all tokens of current line
    let tokenizeLine source (args: string[]) line lineStr queryLexState =
        let defines =
            args |> Seq.choose (fun s -> if s.StartsWith "--define:" then Some s.[9..] else None)
                 |> Seq.toList
        let sourceTokenizer = FSharpSourceTokenizer(defines, None)
        let lineTokenizer = sourceTokenizer.CreateLineTokenizer lineStr
        let rec loop lexState acc =
            match lineTokenizer.ScanToken lexState with
            | Some tok, state -> loop state (tok :: acc)
            | _ -> List.rev acc
        loop (queryLexState source defines line) []
            
    let inline isIdentifier t = t.CharClass = FSharpTokenCharKind.Identifier
    let inline isOperator t = t.ColorClass = FSharpTokenColorKind.Operator
    let inline internal (|GenericTypeParameterPrefix|StaticallyResolvedTypeParameterPrefix|ActivePattern|Other|) ((token: FSharpTokenInfo), (lineStr:string)) =
        if token.Tag = FSharpTokenTag.QUOTE then GenericTypeParameterPrefix
        elif token.Tag = FSharpTokenTag.INFIX_AT_HAT_OP then
             // The lexer return INFIX_AT_HAT_OP token for both "^" and "@" symbols.
             // We have to check the char itself to distinguish one from another.
             if token.FullMatchedLength = 1 && lineStr.[token.LeftColumn] = '^' then
                StaticallyResolvedTypeParameterPrefix
             else Other
        elif token.Tag = FSharpTokenTag.LPAREN then
            if token.FullMatchedLength = 1 &&
               lineStr.Length > token.LeftColumn+1 &&
               lineStr.[token.LeftColumn+1] = '|' then
               ActivePattern
            else Other
        else Other

    // Operators: Filter out overlapped operators (>>= operator is tokenized as three distinct tokens: GREATER, GREATER, EQUALS.
    // Each of them has FullMatchedLength = 3. So, we take the first GREATER and skip the other two).
    //
    // Generic type parameters: we convert QUOTE + IDENT tokens into single IDENT token, altering its LeftColumn
    // and FullMathedLength (for "'type" which is tokenized as (QUOTE, left=2) + (IDENT, left=3, length=4)
    // we'll get (IDENT, left=2, length=5).
    //
    // Statically resolved type parameters: we convert INFIX_AT_HAT_OP + IDENT tokens into single IDENT token, altering its LeftColumn
    // and FullMathedLength (for "^type" which is tokenized as (INFIX_AT_HAT_OP, left=2) + (IDENT, left=3, length=4)
    // we'll get (IDENT, left=2, length=5).
    let internal fixTokens lineStr (tokens : FSharpTokenInfo list) =
        tokens
        |> List.fold (fun (acc, lastToken) token ->
            match lastToken with
            | Some t when token.LeftColumn <= t.RightColumn ->
                acc, lastToken
            | Some ( {Kind = SymbolKind.ActivePattern} as lastToken) when token.Tag = FSharpTokenTag.BAR || token.Tag = FSharpTokenTag.IDENT || token.Tag = FSharpTokenTag.UNDERSCORE ->
                let mergedToken = 
                    {lastToken.Token with Tag = FSharpTokenTag.IDENT
                                                RightColumn = token.RightColumn
                                                FullMatchedLength = lastToken.Token.FullMatchedLength + token.FullMatchedLength }
                                                                             
                acc, Some { lastToken with Token = mergedToken; RightColumn = lastToken.RightColumn + token.FullMatchedLength }
            | _ ->
                match token, lineStr with
                | GenericTypeParameterPrefix -> acc, Some (DraftToken.Create GenericTypeParameter token)
                | StaticallyResolvedTypeParameterPrefix -> acc, Some (DraftToken.Create StaticallyResolvedTypeParameter token)
                | ActivePattern -> acc, Some (DraftToken.Create ActivePattern token)
                | Other ->
                    let draftToken =
                        match lastToken with
                        | Some { Kind = GenericTypeParameter | StaticallyResolvedTypeParameter as kind } when isIdentifier token ->
                              DraftToken.Create kind { token with LeftColumn = token.LeftColumn - 1
                                                                  FullMatchedLength = token.FullMatchedLength + 1 }
                        | Some ( { Kind = SymbolKind.ActivePattern } as ap) when token.Tag = FSharpTokenTag.RPAREN ->
                              DraftToken.Create SymbolKind.Ident ap.Token
                        | _ ->
                            let kind = if isOperator token then Operator elif isIdentifier token then Ident else Other
                            DraftToken.Create kind token
                    draftToken :: acc, Some draftToken
            ) ([], None)
        |> fst
    
    
    let getTokensWithInitialState state lines filename defines =
        [ let mutable state = state
          let sourceTok = FSharpSourceTokenizer(defines, filename)
          for lineText in lines do
              let tokenizer = sourceTok.CreateLineTokenizer(lineText)
              let rec parseLine() =
                  [ match tokenizer.ScanToken(state) with
                    | Some(tok), nstate ->
                        state <- nstate
                        yield tok
                        yield! parseLine()
                    | None, nstate -> state <- nstate ]
              yield parseLine(), lineText ]
    
    let findTokenAt col (tokens:FSharpTokenInfo list) =
        let isTokenAtOffset col (t:FSharpTokenInfo) = col-1 >= t.LeftColumn && col-1 <= t.RightColumn
        tokens |> List.tryFindBack (isTokenAtOffset col)
      
    let isNonTipToken token =
        token.ColorClass = FSharpTokenColorKind.Comment || token.ColorClass = FSharpTokenColorKind.String ||
                        (*token.ColorClass = FSharpTokenColorKind.Text ||*) token.ColorClass = FSharpTokenColorKind.InactiveCode

    // Returns symbol at a given position.
    let getSymbolFromTokens (tokens: FSharpTokenInfo list) line col (lineStr: string) lookupKind: LexerSymbol option =
        let tokens = fixTokens lineStr tokens
            
        // One or two tokens that in touch with the cursor (for "let x|(g) = ()" the tokens will be "x" and "(")
        let tokensUnderCursor =
            match lookupKind with
            | SymbolLookupKind.Simple | SymbolLookupKind.Fuzzy ->
                tokens |> List.filter (fun x -> x.Token.LeftColumn <= col && x.RightColumn + 1 >= col)
            | SymbolLookupKind.ByRightColumn ->
                tokens |> List.filter (fun x -> x.RightColumn = col)
            | SymbolLookupKind.ByLongIdent ->
                tokens |> List.filter (fun x -> x.Token.LeftColumn <= col)

        //printfn "Filtered tokens: %+A" tokensUnderCursor
        match lookupKind with
        | SymbolLookupKind.ByLongIdent ->
            // Try to find start column of the long identifiers
            // Assume that tokens are ordered in an decreasing order of start columns
            let rec tryFindStartColumn tokens =
                match tokens with
                | {Kind = SymbolKind.Other; Token = t1 } :: _ when t1.CharClass = FSharpTokenCharKind.Operator ->
                    Some t1.LeftColumn
                | {Kind = SymbolKind.Other; Token = t1 } :: remainingTokens when t1.Tag = FSharpTokenTag.DOT ->
                    tryFindStartColumn remainingTokens
                | {Kind = Ident; Token = t1} :: {Kind = SymbolKind.Other; Token = t2} :: remainingTokens ->
                    if t2.Tag = FSharpTokenTag.DOT then
                        tryFindStartColumn remainingTokens
                    else
                        Some t1.LeftColumn
                | {Kind = Ident; Token = t} :: _ ->
                    Some t.LeftColumn
                | {Kind = SymbolKind.Other; Token = t} :: _ when t.TokenName = "HASH" ->
                    Some t.LeftColumn
                | _ :: _ | [] ->
                    None
            let decreasingTokens =
                match tokensUnderCursor |> List.sortBy (fun token -> - token.Token.LeftColumn) with
                // Skip the first dot if it is the start of the identifier
                | {Kind = Operator; Token = t} :: remainingTokens when t.Tag = FSharpTokenTag.DOT ->
                    remainingTokens
                | newTokens -> newTokens

            match decreasingTokens with
            | [] -> None
            | [only] when only.Token.TokenName = "HASH" ->
                Some 
                 { Kind = SymbolKind.Other
                   Line = line
                   LeftColumn = only.Token.LeftColumn
                   RightColumn = only.Token.RightColumn
                   Text = lineStr }
            | first :: _ ->
                tryFindStartColumn decreasingTokens
                |> Option.map (fun leftCol ->
                    { Kind = Ident
                      Line = line
                      LeftColumn = leftCol
                      RightColumn = first.RightColumn + 1
                      Text = lineStr.[leftCol..first.RightColumn] })
        | SymbolLookupKind.Fuzzy
        | SymbolLookupKind.ByRightColumn ->
            // Select IDENT token. If failed, select OPERATOR token.
            tokensUnderCursor
            |> List.tryFind (fun { DraftToken.Kind = k } ->
                match k with
                | Ident | GenericTypeParameter | StaticallyResolvedTypeParameter -> true
                | _ -> false)
            |> Option.orTry (fun _ -> tokensUnderCursor |> List.tryFind (fun { DraftToken.Kind = k } -> k = Operator))
            |> Option.map (fun token ->
                { Kind = token.Kind
                  Line = line
                  LeftColumn = token.Token.LeftColumn
                  RightColumn = token.RightColumn + 1
                  Text = lineStr.Substring(token.Token.LeftColumn, token.Token.FullMatchedLength) })
        | SymbolLookupKind.Simple ->
            tokensUnderCursor
            |> List.tryLast
            |> Option.map (fun token ->
                { Kind = token.Kind
                  Line = line
                  LeftColumn = token.Token.LeftColumn
                  RightColumn = token.RightColumn + 1
                  Text = lineStr.Substring(token.Token.LeftColumn, token.Token.FullMatchedLength) })

    let getSymbol source line col lineStr lookupKind (args: string[]) queryLexState =
        let tokens = tokenizeLine source args line lineStr queryLexState
        try
            getSymbolFromTokens tokens line col lineStr lookupKind
        with e ->
            LoggingService.LogInfo (sprintf "Getting lex symbols failed with %O" e)
            None