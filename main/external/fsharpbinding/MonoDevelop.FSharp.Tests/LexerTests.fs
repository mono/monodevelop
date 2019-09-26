namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp.Shared
open FSharp.Compiler.SourceCodeServices

[<TestFixture>]
module LexerTests =
    [<Test>]
    let ``can parse long line``() =
        let line = sprintf "let x = \"%s\"" (String('*', 10000000))
        let sourceTok = FSharpSourceTokenizer([], None)
        let tokenizer = sourceTok.CreateLineTokenizer line
        let tokens, state = Lexer.parseLine tokenizer [] FSharpTokenizerLexState.Initial
        Assert.AreNotEqual(state, FSharpTokenizerLexState.Initial)


    [<Test>]
    let ``can parse long file``() =
        let lines = [ for i in 1..100000 do
                        yield sprintf "let x = %i" i ]
        let sourceTok = FSharpSourceTokenizer([], None)
        let res = Lexer.getTokensWithInitialState FSharpTokenizerLexState.Initial lines (Some "test.fsx") []
        ()
