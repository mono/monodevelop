namespace MonoDevelop.FSharp
open FSharp.Compiler.SourceCodeServices
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.FSharp.Shared
open MonoDevelop.Ide.Editor
open ExtCore.Control
open System.IO

module Tokens =
    let getTokenAtPoint (editor:TextEditor) offset =
        let line, col, txt = editor.GetLineInfoFromOffset offset
        MonoDevelop.FSharp.Shared.Lexer.tokenizeLine txt [||] line txt Lexer.singleLineQueryLexState
        |> MonoDevelop.FSharp.Shared.Lexer.findTokenAt col

    let isInvalidTipTokenAtPoint (editor:TextEditor) offset =
        match getTokenAtPoint editor offset with
        | Some token -> MonoDevelop.FSharp.Shared.Lexer.isNonTipToken token
        | None -> true

    let isInvalidCompletionToken (token:FSharpTokenInfo option) =
        match token with 
        | Some token -> MonoDevelop.FSharp.Shared.Lexer.isNonTipToken token
        | None -> false
                
    let tryGetTokens source defines fileName =
        try
            LoggingService.logDebug "FSharpParser: Processing tokens for %s" (Path.GetFileName fileName)
            let readOnlyDoc = TextEditorFactory.CreateNewReadonlyDocument (source, fileName)
            let lines = readOnlyDoc.GetLines() |> Seq.map readOnlyDoc.GetLineText
            let tokens = MonoDevelop.FSharp.Shared.Lexer.getTokensWithInitialState FSharpTokenizerLexState.Initial lines (Some fileName) defines
            Some(tokens)
        with ex ->
            LoggingService.LogWarning ("FSharpParser: Couldn't update token information for {0}", Path.GetFileName fileName, ex)
            None