namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open MonoDevelop.Ide.Editor
open FsUnit

[<TestFixture>]
type SyntaxHighlighting() =
    let assertStyle (input:string, expectedStyle:string) =
        let offset = input.IndexOf("$")
        let length = input.LastIndexOf("$") - offset - 1
        let input = input.Replace("$", "")
        let data = new TextEditorData (new TextDocument (input))
        let syntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-fsharp")
        let style = SyntaxModeService.GetColorStyle ("Gruvbox")
        let line = data.Lines |> Seq.head
        let chunks = syntaxMode.GetChunks(style, line, offset, line.Length)
        let chunk = chunks |> Seq.tryFind (fun c -> c.Offset = offset && c.Length = length)

        let chunks = syntaxMode.GetChunks(style, line, 0, line.Length)
        let printChunks() =
            chunks |> Seq.iter (fun chunk -> printfn "%A %s" chunk input.[chunk.Offset..chunk.Offset+chunk.Length-1])

        
        match chunk with
        | Some (c) -> c.Style |> should equal expectedStyle
        | _ -> printfn "Offset - %d, Length - %d" offset length
               printChunks()
               Assert.Fail()

        let assertOffsets expectedOffset (chunk:Chunk) =
            //printfn "%d %d" chunk.Offset expectedOffset
            if chunk.Offset <> expectedOffset then
                printChunks()
                Assert.Fail("Overlapping chunks detected")
            chunk.Offset + chunk.Length

        Seq.fold assertOffsets 0 chunks |> ignore

    [<TestCase("let simpleBinding = $1$", "Number")>]
    [<TestCase("$let$ simpleBinding = 1", "Keyword(Iteration)")>]
    [<TestCase("$let!$ simpleBinding = 1", "Keyword(Iteration)")>]
    [<TestCase("let $simpleBinding$ = 1", "User Field Declaration")>]
    [<TestCase("let $offset$ = 1", "User Field Declaration")>]
    [<TestCase("let $add$ x y = x + y", "User Method Declaration")>]
    [<TestCase("let simpleBinding$ = $1", "Plain Text")>]
    [<TestCase("$open$ MonoDevelop", "Keyword(Namespace)")>]
    [<TestCase("open$ MonoDevelop$", "Plain Text")>]
    [<TestCase("open$ Mono.Text$", "Plain Text")>]
    [<TestCase("Seq.$find$ (", "User Method Declaration")>]
    [<TestCase("SyntaxModeService.$GetColorStyle$ (\"Gruvbox\")", "User Method Declaration")>]
    [<TestCase("Seq.find ($fun$ c", "Keyword(Jump)")>]
    [<TestCase("$type$ SyntaxHighlighting() =", "Keyword(Namespace)")>]
    [<TestCase("type $SyntaxHighlighting$ () =", "User Types")>]
    [<TestCase("type $``Completion Tests``$ () =", "User Types")>]
    [<TestCase("$module$ MyModule =", "Keyword(Namespace)")>]
    [<TestCase("module $MyModule$ =", "User Types")>]
    [<TestCase("[<$TestCase$(", "User Types")>]
    [<TestCase("$[<$TestCase(", "Punctuation(Brackets)")>]
    [<TestCase("inherits $SyntaxHighlighting$ () =", "User Types")>]
    [<TestCase("new $DefaultBraceMatcher$()", "User Types")>]
    [<TestCase("$match$ (startOffset, endOffset) with", "Keyword(Iteration)")>]
    [<TestCase("$else$", "Keyword(Iteration)")>]
    [<TestCase("let x (y: $string$", "User Types")>]
    [<TestCase("string.$Length$", "User Property Declaration")>]
    [<TestCase("$($", "Punctuation(Brackets)")>]
    [<TestCase("$<$", "Punctuation(Brackets)")>]
    [<TestCase("$[$", "Punctuation(Brackets)")>]
    [<TestCase("${$", "Punctuation(Brackets)")>]
    [<TestCase("do Something() |> $ignore$", "User Method Declaration")>]
    [<TestCase("let $mutable$ x   = 1", "Keyword(Modifiers)")>]
    [<TestCase("let mutable  $x$ = 1", "User Field Declaration")>]
    [<TestCase("let mutable x$ = $1", "Plain Text")>]
    [<TestCase("c.Style$ |> $should equal", "Plain Text")>]
    [<TestCase("c.Style |> $should$ equal", "User Method Declaration")>]
    [<TestCase("match $x$ with", "User Field Declaration")>]
    [<TestCase("Unchecked.defaultof<$_$>", "Plain Text")>]
    [<TestCase("Seq.$add$", "User Method Declaration")>]
    [<TestCase("let inline$ add$ x y = x + y", "User Method Declaration")>]
    [<TestCase("$override$ x.Something()", "Keyword(Modifiers)")>]
    [<TestCase("member x.$``some identifier``$ = 1", "User Field Declaration")>]
    [<TestCase("member x.$``some identifier``$ () = 1", "User Method Declaration")>]
    [<TestCase("let mutable $vbox4$ : Gtk.VBox = null", "User Field Declaration")>]
    [<TestCase("$return$ x", "Keyword(Iteration)")>]
    [<TestCase("$return!$ x", "Keyword(Iteration)")>]
    [<TestCase("member val IndentOnTryWith = false with $get, set$", "Plain Text")>]
    [<TestCase("| Some $funion$ -> ", "User Field Declaration")>]
    [<TestCase("yield $sprintf$ \"%A\"", "User Method Declaration")>]
    [<TestCase("$doc$.Editor", "User Field Declaration")>]
    [<TestCase(":> $SomeType$", "User Types")>]
    [<TestCase("($'c'$)", "String")>]
    [<TestCase("| Type of $string$", "User Types")>]
    [<TestCase("$DisplayFlags$ = DisplayFlags.DescriptionHasMarkup", "User Field Declaration")>]
    [<TestCase("let shouldEqual (x: $'a$) (y: 'a) =", "User Types")>]
    [<TestCase("| :? $string$", "User Types")>]
    [<TestCase("let inline $private$ is expr s =", "Keyword(Modifiers)")>]
    [<TestCase("let inline private$ is$ expr s =", "User Method Declaration")>]
    [<TestCase("override x.$CanHandle$ editor", "User Method Declaration")>]
    [<TestCase("let addEdge ((n1, n2): 'n * $'n$)", "User Types")>]
    [<TestCase("Map<'n, Set<'n$>>$", "Punctuation(Brackets)")>]
    [<TestCase("let docs = $openDocuments$()", "User Method Declaration")>]
    [<TestCase("let x = $true$", "Keyword(Constants)")>]
    [<TestCase("let $``simple binding``$ = 1", "User Field Declaration")>]
    [<TestCase("let inline$ ``add number``$ x y = x + y", "User Method Declaration")>]
    [<TestCase("$|>$ Option.bind", "Punctuation(Brackets)")>]
    [<TestCase("$typeof$<int>", "User Field Declaration")>]
    [<TestCase("editor.CaretOffset $<-$ offset", "Punctuation(Brackets)")>]
    [<TestCase("let x$  = $1", "Plain Text")>]
    [<TestCase("let $x$     = ", "User Field Declaration")>]
    [<TestCase(@"type thing = TP<$""$32""", "String")>]
    [<TestCase(@"type thing = TP<$MyParam$=""32"", MyOtherParam=42>", "User Field Declaration")>]
    [<TestCase(@"let rec computeSomeFunction $x$ =", "User Field Declaration")>]
    [<TestCase(@"Option.tryCast<MonoTextEditor$>($fun e", "Punctuation(Brackets)")>]
    [<TestCase(@"let mutable session$ = $setupSession()", "Plain Text")>]
    [<TestCase(@"$0b010101$", "Number")>]
    [<TestCase(@"w11.Position <- $0$", "Number")>]
    [<TestCase(@" $-1$", "Number")>]
    [<TestCase(@"[0$..$1]", "Plain Text")>]
    [<TestCase("let mutable x$   = $1", "Plain Text")>]
    [<TestCase("$and$ Forest =", "Keyword(Namespace)")>]
    [<TestCase("let rec go $xs$ =", "User Field Declaration")>]
    [<TestCase("let x = $Some$ 1", "User Types(Enums)")>]
    [<TestCase("type $A$ =", "User Types")>]
    [<TestCase("type $A $", "User Types")>]
    [<TestCase("type A$ = $A of a", "Plain Text")>]
    [<TestCase("type A = A of $a$:int", "User Types")>]
    [<TestCase("type A = A of a:$int$", "User Types")>]
    [<TestCase("type string=$String$", "User Types")>]
    [<TestCase("module A$=$", "Plain Text")>]
    [<TestCase("let $defaultKeyword$ =", "User Field Declaration")>]
    member x.``Syntax highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)

    [<TestCase(@"static member $GetDefaultConfiguration$ :", "User Method Declaration")>]
    [<TestCase(@"member $``Syntax Highlighting``$ :", "User Method Declaration")>]
    [<TestCase(@"-> $^T$ :", "User Types")>]
    [<TestCase(@"   -> $seq$<'T>", "User Types")>]
    [<TestCase(@"   $list$ :", "User Field Declaration")>]

    member x.``Tooltip highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)
