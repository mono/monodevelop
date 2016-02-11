namespace MonoDevelop.FSharp
open System
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop
open ExtCore.Control

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
              
    let isCurrentTokenInvalid (editor:MonoDevelop.Ide.Editor.TextEditor) (tokenisedLines:TokenisedLine list option) project offset =
        try
            let line, col, lineStr = editor.GetLineInfoFromOffset offset
            let filepath = (editor.FileName.ToString())
            let defines = CompilerArguments.getDefineSymbols filepath (project |> Option.ofNull)

            let quickLine =
                maybe {
                    let! tokenisedLines = tokenisedLines
                    let (TokenisedLine(_lineDetail, _tokens, lastLineState)) = tokenisedLines.[line-1]
                    let linedetail = Seq.singleton (LineDetail(line, offset, lineStr))
                    return getTokensWithInitialState lastLineState linedetail filepath defines }

            let isTokenAtOffset col t = col-1 >= t.LeftColumn && col-1 <= t.RightColumn

            let caretToken =
                match quickLine with
                | Some line ->
                    //we have a line
                    match line with
                    | [single] ->
                        let (TokenisedLine(_lineDetail, lineTokens, _state)) = single
                        lineTokens |> List.tryFind (isTokenAtOffset col)
                    | _ -> None //should only be one
                | None ->
                    let lineDetails =
                        [ for i in 1..line do
                              let line = editor.GetLine(i)
                              yield LineDetail(line.LineNumber, line.Offset, editor.GetTextAt(line.Offset, line.Length)) ]
                    let tokens = getTokens lineDetails filepath defines
                    let (TokenisedLine(_lineDetail, lineTokens, _state)) = tokens.[line-1]
                    lineTokens |> List.tryFind (isTokenAtOffset col)

            let isTokenInvalid =
                match caretToken with
                | Some token -> token.ColorClass = FSharpTokenColorKind.Comment ||
                                token.ColorClass = FSharpTokenColorKind.String ||
                                token.ColorClass = FSharpTokenColorKind.Text ||
                                token.ColorClass = FSharpTokenColorKind.InactiveCode
                | None -> true
            isTokenInvalid
        with ex -> true
