namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.FSharp.Completion
open Mono.TextEditor
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.CodeCompletion
open FsUnit
open MonoDevelop
       
type ``Completion Tests``() =
    let getParseResults (documentContext:DocumentContext, _text) =
        async {
            return documentContext.TryGetAst()
        }

    let getCompletions (input: string) parse =
        let offset = input.LastIndexOf "|"
        if offset = -1 then
            failwith "Input must contain a |"
        let input = input.Remove(offset, 1)
        let doc = 
            if parse then
                TestHelpers.createDoc input "defined"
            else
                TestHelpers.createDocWithoutParsing input "defined"

        let editor = doc.Editor
        editor.CaretOffset <- offset
        let ctx = new CodeCompletionContext()
        ctx.TriggerOffset <- offset
        let results =
            Completion.codeCompletionCommandImpl(editor, doc, ctx, false)
            |> Async.RunSynchronously
            |> Seq.map (fun c -> c.DisplayText)

        results |> Seq.toList

    [<Test>]
    member x.``Completes namespace``() =
        let results = getCompletions "open System.Text.|" true
        results |> should contain "RegularExpressions"

    [<Test>]
    member x.``Completes list``() =
        let results = getCompletions "[].|" true
        results |> should contain "Head"

    [<Test>]
    member x.``Array completion shouldn't contain identifier``() =
        let results = getCompletions 
                        """
                        let x = [1;2;3]
                        x.[0].|
                        """ true
        results |> shouldnot contain "x"

    [<Test>]
    member x.``Completes local identifier``() =
        let results = getCompletions 
                        """
                        module mymodule =
                            let completeme = 1
                            let x = compl|
                        """ true
                        
        results |> should contain "completeme"

    [<TestCase("let x|")>]
    [<TestCase("let (x|")>]
    [<TestCase("let! x|")>]
    [<TestCase("let in|")>]
    [<TestCase("let x |")>]
    [<TestCase("let x =|")>]
    [<TestCase("let x, y|")>]
    [<TestCase("let x = \"System.|")>]
    [<TestCase("#r \"/Users/user/some/dll\"|")>]
    //[<TestCase("let x = ``System.|");Ignore("Not implemented yet")>]
    [<TestCase("member x|")>]
    [<TestCase("override x|")>]
    [<TestCase("1|")>]
    [<TestCase("fun c|")>]
    [<TestCase("0uy|")>]
    [<TestCase("let x = [1..|")>]
    [<TestCase("[ for i in 0 .|. 99")>]
    [<TestCase("for s|")>]
    member x.``Empty completions``(input: string) =
        let results = getCompletions input true
        results |> should be Empty

    [<TestCase("let x = s|")>]
    [<TestCase("let x = \"\".|.")>]
    [<TestCase("let x (y:strin|")>]
    [<TestCase("member x.Something (y:strin|")>]
    member x.``Not empty completions``(input: string) =
        let results = getCompletions input true
        results |> shouldnot be Empty

    [<Test>]
    member x.``Keywords don't appear after dot``() =
        let results = getCompletions @"let x = string.l|" true
        results |> shouldnot contain "let"

    [<Test>]
    member x.``Keywords appear after whitespace``() =
        let results = getCompletions @" l|" true
        results |> should contain "let"

    [<Test>]
    member x.``Keywords appear at start of line``() =
        let results = getCompletions @" l|"true
        results |> should contain "let"

    [<Test>]
    member x.``Keywords appear at column 0``() =
        let results = getCompletions @"o|"true
        results |> should contain "open"

    [<Test>]
    member x.``Keywords can be parameters``() =
        let results = getCompletions @"let x = new System.IO.FileInfo(n|" true
        results |> should contain "null"

    [<Test>]
    member x.``Completes modifiers``() =
        let results = getCompletions @"let mut|" true
        results |> should contain "mutable"

    [<Test>]
    member x.``Completes idents without parse results``() =
        let results = getCompletions @"let add first second = f|" false
        results |> should contain "first"

    [<Test>]
    member x.``Does not complete long idents without parse results``() =
        let results = getCompletions @"let add first second = first.|" false
        results |> shouldnot contain "first"

    [<Test>]
    member x.``Does not complete current residue without parse results``() =
        let results = getCompletions @"let add first second = z|" false
        results |> shouldnot contain "z"

     
    [<Test>]
    member x.``Completes lambda``() =
        let results = getCompletions @"let x = ""string"" |> Seq.map (fun c -> c.|" true
        results |> should contain "ToString"
        results |> shouldnot contain "mutable"

    [<Test>]
    member x.``Completes local identifier with mismatched parens``() =
        let results = getCompletions
                        """
                        type rectangle(width, height) =
                            class end

                        module s =
                            let height = 10
                            let x = rectangle(he|
                        """ true
        results |> should contain "height"

    [<Test>]
    member x.``Does not complete inside multiline comment``() =
        let results = getCompletions
                        """
                        (*
                        Li|
                        *)
                        """ true
        results |> should be Empty

    [<Test>]
    member x.``Does not complete inside multiline comment without end delimiter``() =
        let results = getCompletions
                        """
                        (*
                        Li|
                        """ true
        results |> should be Empty

    [<Test>]
    member x.``Does not complete inside single line comment``() =
        let results = getCompletions "// Li|" true
        results |> should be Empty

    [<Test>]
    member x.``Completes attribute``() =
        let input = 
            """
            type TestAttribute() =
                inherit System.Attribute()

            type TestCaseAttribute() =
              inherit TestAttribute()
            [<t|
            """
        let results = getCompletions input true
        results |> should contain "Test"
        results |> should contain "TestCase"
        results |> shouldnot contain "Array"

    [<TestCase("#r @\"c:\some\path", @"c:\some\path")>]
    [<TestCase("#r \"some/path", "some/path")>]
    [<TestCase(@"#r ""c:\\some\\path", @"c:\some\path")>]
    member x.``Accepts path completions``(input, expected) =
        let doc = TestHelpers.createDoc input "defined"
       
        let completionContext =  {
            completionChar = 'x'
            lineToCaret = input
            line = 0
            column = 0
            editor = doc.Editor
            triggerOffset = 0
            ctrlSpace = true
            documentContext = doc
        }
        match completionContext with
        | FilePath(_,path) -> path |> should equal expected
        | _ -> Assert.Fail "Did not match path"