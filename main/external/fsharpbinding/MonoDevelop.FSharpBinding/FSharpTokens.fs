namespace MonoDevelop.FSharp
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.FSharp.Shared
open MonoDevelop.Ide.Editor
open ExtCore.Control
open System.IO

module Tokens =
    let getTokenAtPoint (editor:TextEditor) (context:DocumentContext) offset =
        let line, col, txt = editor.GetLineInfoFromOffset offset
        let getTokens() =
            Lexer.tokenizeLine txt [||] line txt Lexer.singleLineQueryLexState
        
        let lineTokens =
            maybe { let! pd = context.TryGetFSharpParsedDocument()
                    let! tokens = pd.Tokens
                    let lineTokens, _line = tokens |> List.item (line - 1)
                    return lineTokens }
                    |> Option.getOrElse getTokens

        let caretToken = lineTokens |> Lexer.findTokenAt col
        match caretToken with
        | Some token -> Some token
        | None -> // the background semantic parse hasn't caught up yet,
                  // so tokenize the current line now
                  getTokens() |> Lexer.findTokenAt col

    let isInvalidTipTokenAtPoint (editor:TextEditor) (context:DocumentContext) offset =
        match getTokenAtPoint editor context offset with 
        | Some token -> Lexer.isNonTipToken token
        | None -> true

    let isInvalidCompletionToken (token:FSharpTokenInfo option) =
        match token with 
        | Some token -> Lexer.isNonTipToken token
        | None -> false
                
    let tryGetTokens source defines fileName =
        try
            LoggingService.logDebug "FSharpParser: Processing tokens for %s" (Path.GetFileName fileName)
            let readOnlyDoc = TextEditorFactory.CreateNewReadonlyDocument (source, fileName)
            let lines = readOnlyDoc.GetLines() |> Seq.map readOnlyDoc.GetLineText
            let tokens = Lexer.getTokensWithInitialState 0L lines (Some fileName) defines
            Some(tokens)
        with ex ->
            LoggingService.LogWarning ("FSharpParser: Couldn't update token information for {0}", Path.GetFileName fileName, ex)
            None