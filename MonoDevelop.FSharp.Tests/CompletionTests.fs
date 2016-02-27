namespace MonoDevelopTests

open System
open NUnit.Framework
open NUnit.Framework.Extensibility
open MonoDevelop.FSharp
open Mono.TextEditor
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.CodeCompletion
open FsUnit
open MonoDevelop
        
type ``Completion Tests``() =
    let getParseResults (documentContext:DocumentContext, _text, _version) =
        async {
            return documentContext.TryGetAst()
        }
    
    let getCompletions (input: string) =
        let offset = input.IndexOf "|"
        if offset = -1 then
            failwith "Input must contain a |"
        let input = input.Replace("|", "")

        let doc = TestHelpers.createDoc input "defined"
        let editor = doc.Editor
        editor.CaretOffset <- offset
        let ctx = new CodeCompletionContext()
        ctx.TriggerOffset <- offset

        let results =
            Completion.codeCompletionCommandImpl(getParseResults, editor, doc, ctx, false)
            |> Async.RunSynchronously
            |> Seq.map (fun c -> c.DisplayText)

        results |> Seq.toList

    [<Test>]
    member x.``Completes namespace``() =
        let results = getCompletions "open System.Text.|"
        results |> should contain "RegularExpressions"

    [<Test>]
    member x.``Completes local identifier``() =
        let results = getCompletions 
                        """
                        module mymodule =
                            let completeme = 1
                            let x = compl|
                        """
                        
        results |> should contain "completeme"

    [<TestCase("let x|")>]
    [<TestCase("let x |")>]
    [<TestCase("let x =|")>]
    [<TestCase("let x, y|")>]
    [<TestCase("let x = \"System.|")>]
    member x.``Empty completions``(input: string) =
        let results = getCompletions input
        results |> should be Empty

    [<TestCase("let x = s|")>]
    [<TestCase("let x = \"\".|.")>]
    member x.``Not empty completions``(input: string) =
        let results = getCompletions input
        results |> shouldnot be Empty

    [<Test>]
    member x.``Keywords don't appear after dot``() =
        let results = getCompletions @"let x = string.l|"
        results |> shouldnot contain "let"

    [<Test>]
    member x.``Keywords appear after whitespace``() =
        let results = getCompletions @" l|"
        results |> should contain "let"

    [<Test>]
    member x.``Keywords appear at start of line``() =
        let results = getCompletions @" l|"
        results |> should contain "let"

    [<Test>]
    member x.``Keywords appear at column 0``() =
        let results = getCompletions @"o|"
        results |> should contain "open"

    [<Test>]
    member x.``Completes local identifier with mismatched parens``() =
        let identifier = 1

        let results = getCompletions
                        """
                        type rectangle(width, height) =
                            class end

                        module s =
                            let height = 10
                            let x = rectangle(he|
                        """
        results |> should contain "height"
