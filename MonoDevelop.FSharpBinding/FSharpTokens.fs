namespace MonoDevelop.FSharp
open System
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

module Tokens =
    type LineDetail = LineDetail of linenumber:int * lineOffset:int * text:string
    type TokenisedLine = TokenisedLine of LineDetail * tokens:FSharpTokenInfo list * stateAtEOL:int64

    let (|CollapsableToken|_|) (token:FSharpTokenInfo) =
        match token.ColorClass with
        | FSharpTokenColorKind.String
        | FSharpTokenColorKind.Comment
        | FSharpTokenColorKind.InactiveCode -> Some token
        | _ -> None

    let isSameType (token1:FSharpTokenInfo) (token2:FSharpTokenInfo) =
        token1.ColorClass = token2.ColorClass

    let mergeTokens newest oldest =
        {oldest with RightColumn = newest.RightColumn;FullMatchedLength = oldest.FullMatchedLength + newest.RightColumn - newest.LeftColumn}


    let getTokensWithInitialState state lineDetails filename defines =
        [ let state = ref state
          let sourceTok = SourceTokenizer(defines, filename)
          for LineDetail(lineNumber, lineOffset, lineText) in lineDetails do
              let tokenizer = sourceTok.CreateLineTokenizer(lineText)
              let rec parseLine() =
                  [ match tokenizer.ScanToken(!state) with
                    | Some(tok), nstate ->
                        state := nstate
                        yield tok
                        yield! parseLine()
                    | None, nstate -> state := nstate ]
              yield TokenisedLine(LineDetail(lineNumber, lineOffset, lineText), parseLine(), !state) ]

    let getTokens lineDetails filename defines =
        getTokensWithInitialState 0L lineDetails filename defines

    let collapseTokens (tokenLines:TokenisedLine list) =
        let rec parseLine (tokens: _ list) lastToken =
            [match tokens with
             | [] -> ()
             | newToken::tail ->
                 match newToken, lastToken with
                 | CollapsableToken _, None ->
                     yield! parseLine tail (Some newToken)
                 | _ , None ->
                     yield newToken
                     yield! parseLine tail None
                 | CollapsableToken newToken, Some lastToken ->
                     if isSameType newToken lastToken then
                         yield! parseLine tail (Some(mergeTokens newToken lastToken))
                     else yield lastToken
                          yield! parseLine tail (Some newToken)
                 | _, Some lastToken ->
                     yield lastToken
                     yield newToken
                     yield! parseLine tail None ]

        [| for TokenisedLine(LineDetail(lineNo, offset, line), tokens, stateEOL) in tokenLines do
              let mergedTokens = parseLine tokens None
              yield TokenisedLine(LineDetail(lineNo, offset, line), mergedTokens, stateEOL) |]

    let getMergedTokens lineDetails filename defines =
        [|let state = ref 0L
          let sourceTok = SourceTokenizer(defines, filename)
          for LineDetail(lineNumber, lineOffset, lineText) in lineDetails do
              let tokenizer = sourceTok.CreateLineTokenizer(lineText)
              let rec parseLine lastToken =
                  [ match tokenizer.ScanToken(!state) with
                    | Some(tok), nstate ->
                        state := nstate
                        match tok, lastToken with
                        | CollapsableToken _, None ->
                            yield! parseLine (Some tok)
                        | _ , None ->
                            yield tok
                            yield! parseLine None
                        | CollapsableToken newToken, Some lastToken ->
                            if isSameType newToken lastToken then
                                yield! parseLine (Some(mergeTokens tok lastToken))
                            else yield lastToken
                                 yield! parseLine(Some newToken)
                        | _, Some lastToken ->
                            yield lastToken
                            yield tok
                            yield! parseLine None

                    | None, nstate ->
                        state := nstate
                        match lastToken with
                        | Some other -> yield other
                        | _ -> ()]
              yield TokenisedLine(LineDetail(lineNumber, lineOffset, lineText), parseLine None, !state) |]
