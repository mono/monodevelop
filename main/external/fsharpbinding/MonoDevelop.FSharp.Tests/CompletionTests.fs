namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.FSharp.Completion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.CodeCompletion
open FsUnit
open MonoDevelop

type ParseBeforeCompletion = Parse | NoParse

type ``Completion Tests``() =
    let getCompletions (input: string) parse =
        let offset = input.LastIndexOf "$"
        if offset = -1 then
            failwith "Input must contain a $"
        let input = input.Remove(offset, 1)
        let doc =
            match parse with
            | Parse -> TestHelpers.createDoc input "defined"
            | NoParse -> TestHelpers.createDocWithoutParsing input "defined"

        let editor = doc.Editor
        editor.CaretOffset <- offset
        let ctx = new CodeCompletionContext()
        ctx.TriggerOffset <- offset
        let results =
            Completion.codeCompletionCommandImpl(editor, doc, ctx, false)
            |> Async.RunSynchronously

        results |> Seq.toList

    let getCompletionStrings (input: string) parse =
        let completions = getCompletions input parse
        completions |> List.map (fun c -> c.DisplayText)

    let getCompletionsAndRhs (input: string) parse =
        let completions = getCompletions input parse
        completions |> List.map (fun c -> sprintf "%s %s" c.DisplayText (c.GetRightSideDescription false |> TestHelpers.stripHtml))

    [<Test>]
    member x.``Completes namespace``() =
        let results = getCompletionStrings "open System.Text.$" Parse
        results |> should contain "RegularExpressions"

    [<Test>]
    member x.``Completes list``() =
        let results = getCompletionStrings "[].$" NoParse
        results |> should contain "Head"

    [<Test>]
    member x.``Completes application``() =
        let results = getCompletionStrings "System.DateTime(2000,1,1).$" NoParse
        results |> should contain "Day"

    [<Test>]
    member x.``Completes application property``() =
        let results = getCompletionStrings "System.IO.File.Open(\"path\", System.IO.FileMode.Open).SafeFileHandle.$" NoParse
        results |> should contain "Close"

    [<Test>]
    member x.``Array completion shouldn't contain identifier``() =
        let results = getCompletionStrings 
                        """
                        let x = [1;2;3]
                        x.[0].$
                        """ Parse
        results |> shouldnot contain "x"

    [<Test>]
    member x.``Does not contain identifier``() =
        let results = getCompletionStrings "DateTime(2000,1,1).Day Da$" Parse
        let results2 = results |> List.filter (fun c -> c.StartsWith "Day")
        results2 |> shouldnot contain "Day"

    [<Test>]
    member x.``Completes local identifier``() =
        let results = getCompletionStrings 
                        """
                        module mymodule =
                            let completeme = 1
                            let x = compl$
                        """ Parse
                        
        results |> should contain "completeme"

    [<TestCase("let x$")>]
    [<TestCase("let (x$")>]
    [<TestCase("let! x$")>]
    [<TestCase("let in$")>]
    [<TestCase("let x $")>]
    [<TestCase("let x =$")>]
    [<TestCase("let x, y$")>]
    [<TestCase("let x = \"System.$")>]
    [<TestCase("#r \"/Users/user/some/dll\"$")>]
    //[<TestCase("let x = ``System.$");Ignore("Not implemented yet")>]
    [<TestCase("member x$")>]
    [<TestCase("override x$")>]
    [<TestCase("1$")>]
    [<TestCase("fun c$")>]
    [<TestCase("0uy$")>]
    [<TestCase("let x = [1..$")>]
    [<TestCase("[ for i in 0 .$. 99")>]
    [<TestCase("for s$")>]
    [<TestCase("| Some s$")>]
    member x.``Empty completions``(input: string) =
        let results = getCompletionStrings input Parse
        results |> should be Empty

    [<TestCase("let x = s$")>]
    [<TestCase("let x = \"\".$.")>]
    [<TestCase("let x (y:strin$")>]
    [<TestCase("member x.Something (y:strin$")>]
    member x.``Not empty completions``(input: string) =
        let results = getCompletionStrings input Parse
        results |> shouldnot be Empty

    [<Test>]
    member x.``Keywords don't appear after dot``() =
        let results = getCompletionStrings @"let x = string.l$" Parse
        results |> shouldnot contain "let"

    [<Test>]
    member x.``Keywords appear after whitespace``() =
        let results = getCompletionStrings @" l$" Parse
        results |> should contain "let"

    [<Test>]
    member x.``Keywords appear at start of line``() =
        let results = getCompletionStrings @" l$" Parse
        results |> should contain "let"

    [<Test>]
    member x.``Keywords appear at column 0``() =
        let results = getCompletionStrings @"o$" Parse
        results |> should contain "open"

    [<Test>]
    member x.``Keywords can be parameters``() =
        let results = getCompletionStrings @"let x = new System.IO.FileInfo(n$" Parse
        results |> should contain "null"

    [<Test>]
    member x.``Completes modifiers``() =
        let results = getCompletionStrings @"let mut$" Parse
        results |> should contain "mutable"

    [<Test>]
    member x.``Completes idents without parse results``() =
        let results = getCompletionStrings @"let add first second = f$" NoParse
        results |> should contain "first"

    [<Test>]
    member x.``Does not complete long idents without parse results``() =
        let results = getCompletionStrings @"let add first second = first.$" NoParse
        results |> shouldnot contain "first"

    [<Test>]
    member x.``Does not complete current residue without parse results``() =
        let results = getCompletionStrings @"let add first second = z$" NoParse
        results |> shouldnot contain "z"

     
    [<Test>]
    member x.``Completes lambda``() =
        let results = getCompletionStrings @"let x = ""string"" |> Seq.map (fun c -> c.$" Parse
        results |> should contain "ToString"
        results |> shouldnot contain "mutable"

    [<Test>]
    member x.``Completes local identifier with mismatched parens``() =
        let results = getCompletionStrings
                        """
                        type rectangle(width, height) =
                            class end

                        module s =
                            let height = 10
                            let x = rectangle(he$
                        """ Parse
        results |> should contain "height"

    [<Test>]
    member x.``Does not complete inside multiline comment``() =
        let results = getCompletionStrings
                        """
                        (*
                        Li$
                        *)
                        """ Parse
        results |> should be Empty

    [<Test>]
    member x.``Does not complete inside multiline comment without end delimiter``() =
        let results = getCompletionStrings
                        """
                        (*
                        Li$
                        """ Parse
        results |> should be Empty

    [<Test>]
    member x.``Does not complete inside single line comment``() =
        let results = getCompletionStrings "// Li$" Parse
        results |> should be Empty

    [<Test>]
    member x.``Can complete auto import item``() =
        let results = getCompletionsAndRhs "P$" Parse
        results |> should contain "Path (from System.IO)"

    [<Test>]
    member x.``Contains two Path items``() =
        let results = getCompletionsAndRhs
                        """
                        let Path = 1
                        P$
                        """ Parse |> List.filter(fun c -> c.StartsWith("Path"))

        results |> should contain "Path int"
        results |> should contain "Path (from System.IO)"

    [<Test>]
    member x.``Should not contain (from System.IO)``() =
        let results = getCompletionsAndRhs
                        """
                        module mymodule =
                            open System.IO
                            Pat$
                        """ Parse |> List.filter(fun c -> c.StartsWith("Path"))

        results |> shouldnot contain "Path (from System.IO)"

    [<Test>]
    member x.``Completes attribute``() =
        let input = 
            """
            type TestAttribute() =
                inherit System.Attribute()

            type TestCaseAttribute() =
              inherit TestAttribute()
            [<t$
            """
        let results = getCompletionStrings input Parse
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