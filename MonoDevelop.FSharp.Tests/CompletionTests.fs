namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open Mono.TextEditor
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.CodeCompletion
open FsUnit

type ``Completion Tests``() =
    let getCompletions (input: string) =
        let offset = input.IndexOf "|"
        let input = input.Replace("|", "")
        let doc = TestHelpers.createDoc input "defined"
        let editor = doc.Editor
        editor.CaretOffset <- offset
        let ctx = new CodeCompletionContext()
        ctx.TriggerOffset <- offset
        //ctx.TriggerLine <- editor.CaretLine
        let results =
            Completion.codeCompletionCommandImpl(editor, doc, ctx, true)
            |> Async.RunSynchronously
            |> Seq.map (fun c -> c.DisplayText)
        results |> Seq.iter (fun r -> printfn "%s" r)
        
        results |> Seq.toList

    [<Test>]
    member x.``Completes namespace``() =
        let results = getCompletions "open System.Text.|"
        results |> should equal ["RegularExpressions"]

    [<TestCase("let x|")>]
    [<TestCase("let x |")>]
    [<TestCase("let x =|")>]
    [<TestCase("let x, y|")>]
    member x.``Empty completions``(input: string) =
        let results = getCompletions input
        results |> should be Empty

    [<TestCase("let x = s|")>]
    member x.``Not empty completions``(input: string) =
        let results = getCompletions input
        results |> should be NotEmpty