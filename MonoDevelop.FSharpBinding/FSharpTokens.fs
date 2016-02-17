namespace MonoDevelop.FSharp
open System
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop
open MonoDevelop.Ide.Editor
open ExtCore.Control
open MonoDevelop.Core
open System.IO

module Tokens =
    let isInvalidTipTokenAtPoint (editor:TextEditor) (context:DocumentContext) offset =
        let line, col, txt = editor.GetLineInfoFromOffset offset
        let lineTokens =
            maybe { let! pd = context.TryGetFSharpParsedDocument()
                    let! tokens = pd.Tokens
                    let lineTokens, _state = tokens |> List.item (line - 1)
                    return lineTokens }
                    |> Option.getOrElse (fun () -> Lexer.tokenizeLine txt [||] line txt Lexer.singleLineQueryLexState)

        let caretToken = lineTokens |> Lexer.findTokenAt col
        let isInvalid =
            match caretToken with
            | Some token -> Lexer.isNonTipToken token
            | None -> true
        isInvalid
        
    let tryGetTokens source defines fileName =
        try
            LoggingService.LogDebug ("FSharpParser: Processing tokens for {0}", Path.GetFileName fileName)
            let readOnlyDoc = TextEditorFactory.CreateNewReadonlyDocument (source, fileName)
            let lines = readOnlyDoc.GetLines() |> Seq.map readOnlyDoc.GetLineText
            let tokens = Lexer.getTokensWithInitialState 0L lines fileName defines
            Some(tokens)
        with ex ->
            LoggingService.LogWarning ("FSharpParser: Couldn't update token information for {0}", Path.GetFileName fileName, ex)
            None