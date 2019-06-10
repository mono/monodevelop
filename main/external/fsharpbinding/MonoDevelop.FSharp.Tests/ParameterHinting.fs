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
open System.Threading.Tasks
open System.Runtime.CompilerServices

type ``Parameter Hinting``() =

    [<SetUp;AsyncStateMachine(typeof<Task>)>]
    let ``run before test``() =
        FixtureSetup.initialiseMonoDevelopAsync()

    let getHints (input: string) =
        let offset = input.LastIndexOf "|"
        if offset = -1 then
            failwith "Input must contain a |"
        let input = input.Remove(offset, 1)
        let doc = TestHelpers.createDoc input "defined"
        let editor = doc.Editor
        editor.CaretOffset <- offset
        let ctx = new CodeCompletionContext()
        ctx.TriggerOffset <- offset

        let index = ParameterHinting.getParameterIndex(editor, editor.Text.LastIndexOf("("))
        let hints = ParameterHinting.getHints(editor, doc, ctx)
                    |> Async.RunSynchronously
        hints.[0].GetParameterName(index - 1) // index is 1 based

    [<SetUp;AsyncStateMachine(typeof<Task>)>]
    let ``run before test``() =
        FixtureSetup.initialiseMonoDevelopAsync()

    [<TestCase("System.IO.Path.ChangeExtension(|", "path")>]
    [<TestCase("System.IO.Path.ChangeExtension(|)", "path")>]
    [<TestCase("System.IO.Path.ChangeExtension   (|)", "path")>]
    [<TestCase("System.IO.Path.ChangeExtension(pa|", "path")>]
    [<TestCase(@"System.IO.Path.ChangeExtension(""/tmp.fs"",|", "extension")>]
    member x.``Parameter hinting``(input, expected) =
        getHints input |> should equal expected

