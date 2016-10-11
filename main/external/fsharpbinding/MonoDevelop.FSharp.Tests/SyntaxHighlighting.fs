namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open Mono.TextEditor
open MonoDevelop.Ide.Editor.Highlighting
open MonoDevelop.Ide.Editor
open FsUnit
open System.Threading
open System.IO
//open System.Linq

[<TestFixture>]
type SyntaxHighlighting() =
    do
        FixtureSetup.initialiseMonoDevelop()

    let assertStyle (input:string, expectedStyle:string) =
        //Assert.Fail()
        //editor.SyntaxHighlighting.GetHighlightedLineAsync
        //GetMarkUp
        //SignatureMarkupCreator
        //SyntaxHighlightingService.GetColorFromScope (editorTheme, scope, EditorThemeColors.Foreground)
        //Syntax
        let offset = input.IndexOf("$")
        let length = input.LastIndexOf("$") - offset - 1
        let input = input.Replace("$", "")
        let data = new TextEditorData (new TextDocument (input))
        let markup = data.GetMarkup(offset, length, false, true, false, false)
        let editor = TextEditorFactory.CreateNewEditor ()
        use reader = File.OpenText("/Users/jason/src/monodevelop/main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor.Highlighting/syntaxes/FSharp/F#.sublime-syntax")
        let highlighting = Sublime3Format.ReadHighlighting(reader)
        highlighting.PrepareMatches()
        editor.Text <- input
        editor.SyntaxHighlighting <- MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlighting(highlighting, editor)
        let stack = editor.GetScopeStackAsync(offset+1, CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously
        let first = stack |> Seq.head
        Assert.AreEqual(expectedStyle, first)
        //let syntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-fsharp")
        //let style = SyntaxModeService.GetColorStyle ("Gruvbox")
        //let line = data.Lines |> Seq.head
        //let chunks = syntaxMode.GetChunks(style, line, offset, line.Length)
        //let chunk = chunks |> Seq.tryFind (fun c -> c.Offset = offset && c.Length = length)

        //let chunks = syntaxMode.GetChunks(style, line, 0, line.Length)
        //let printChunks() =
        //    chunks |> Seq.iter (fun chunk -> printfn "%A %s" chunk input.[chunk.Offset..chunk.Offset+chunk.Length-1])

        
        //match chunk with
        //| Some (c) -> c.Style |> should equal expectedStyle
        //| _ -> printfn "Offset - %d, Length - %d" offset length
        //       printChunks()
        //       Assert.Fail()

        //let assertOffsets expectedOffset (chunk:Chunk) =
        //    //printfn "%d %d" chunk.Offset expectedOffset
        //    if chunk.Offset <> expectedOffset then
        //        printChunks()
        //        Assert.Fail("Overlapping chunks detected")
        //    chunk.Offset + chunk.Length

        //Seq.fold assertOffsets 0 chunks |> ignore
    [<TestCase("$namespace$ MonoDevelop", "keyword.source.fs")>]
    [<TestCase("let simpleBinding = $1$", "constant.numeric.source.fs")>]
    [<TestCase("$let$ simpleBinding = 1", "keyword.source.fs")>]
    [<TestCase("$let!$ simpleBinding = 1", "keyword.source.fs")>]
    [<TestCase("let $simpleBinding$ = 1", "entity.name.field")>]
    [<TestCase("let $offset$ = 1", "entity.name.field")>]
    [<TestCase("let $add$ x y = x + y", "User Method Declaration")>]
    [<TestCase("let simpleBinding$ = $1", "source.fs")>]
    [<TestCase("$open$ MonoDevelop", "keyword.source.fs")>]
    [<TestCase("open$ MonoDevelop$", "source.fs")>]
    [<TestCase("open$ Mono.Text$", "source.fs")>]
    [<TestCase("Seq.$find$ (", "User Method Declaration")>]
    [<TestCase("SyntaxModeService.$GetColorStyle$ (\"Gruvbox\")", "User Method Declaration")>]
    [<TestCase("Seq.find ($fun$ c", "Keyword(Jump)")>]
    [<TestCase("$type$ SyntaxHighlighting() =", "Keyword(Namespace)")>]
    [<TestCase("type $SyntaxHighlighting$ () =", "entity.name.class")>]
    [<TestCase("type $``Completion Tests``$ () =", "entity.name.class")>]
    [<TestCase("$module$ MyModule =", "keyword.source.fs")>]
    [<TestCase("module $MyModule$ =", "entity.name.class")>]
    [<TestCase("[<$TestCase$(", "entity.name.class")>]
    [<TestCase("$[<$TestCase(", "Punctuation(Brackets)")>]
    [<TestCase("inherits $SyntaxHighlighting$ () =", "entity.name.class")>]
    [<TestCase("new $DefaultBraceMatcher$()", "entity.name.class")>]
    [<TestCase("$match$ (startOffset, endOffset) with", "keyword.source.fs")>]
    [<TestCase("$else$", "keyword.source.fs")>]
    [<TestCase("let x (y: $string$", "entity.name.class")>]
    [<TestCase("string.$Length$", "User Property Declaration")>]
    [<TestCase("$($", "Punctuation(Brackets)")>]
    [<TestCase("$<$", "Punctuation(Brackets)")>]
    [<TestCase("$[$", "Punctuation(Brackets)")>]
    [<TestCase("${$", "Punctuation(Brackets)")>]
    [<TestCase("do Something() |> $ignore$", "User Method Declaration")>]
    [<TestCase("let $mutable$ x   = 1", "keyword.source.fs")>]
    [<TestCase("let mutable  $x$ = 1", "entity.name.field")>]
    [<TestCase("let mutable x$ = $1", "source.fs")>]
    [<TestCase("c.Style$ |> $should equal", "source.fs")>]
    [<TestCase("c.Style |> $should$ equal", "User Method Declaration")>]
    [<TestCase("match $x$ with", "entity.name.field")>]
    [<TestCase("Unchecked.defaultof<$_$>", "source.fs")>]
    [<TestCase("Seq.$add$", "User Method Declaration")>]
    [<TestCase("let inline$ add$ x y = x + y", "User Method Declaration")>]
    [<TestCase("$override$ x.Something()", "keyword.source.fs")>]
    [<TestCase("member x.$``some identifier``$ = 1", "entity.name.field")>]
    [<TestCase("member x.$``some identifier``$ () = 1", "User Method Declaration")>]
    [<TestCase("let mutable $vbox4$ : Gtk.VBox = null", "entity.name.field")>]
    [<TestCase("$return$ x", "keyword.source.fs")>]
    [<TestCase("$return!$ x", "keyword.source.fs")>]
    [<TestCase("member val IndentOnTryWith = false with $get, set$", "source.fs")>]
    [<TestCase("| Some $funion$ -> ", "entity.name.field")>]
    [<TestCase("yield $sprintf$ \"%A\"", "User Method Declaration")>]
    [<TestCase("$doc$.Editor", "entity.name.field")>]
    [<TestCase(":> $SomeType$", "entity.name.class")>]
    [<TestCase("($'c'$)", "String")>]
    [<TestCase("| Type of $string$", "entity.name.class")>]
    [<TestCase("$DisplayFlags$ = DisplayFlags.DescriptionHasMarkup", "entity.name.field")>]
    [<TestCase("let shouldEqual (x: $'a$) (y: 'a) =", "entity.name.class")>]
    [<TestCase("| :? $string$", "entity.name.class")>]
    [<TestCase("let inline $private$ is expr s =", "Keyword(Modifiers)")>]
    [<TestCase("let inline private$ is$ expr s =", "User Method Declaration")>]
    [<TestCase("override x.$CanHandle$ editor", "User Method Declaration")>]
    [<TestCase("let addEdge ((n1, n2): 'n * $'n$)", "entity.name.class")>]
    [<TestCase("Map<'n, Set<'n$>>$", "Punctuation(Brackets)")>]
    [<TestCase("let docs = $openDocuments$()", "User Method Declaration")>]
    [<TestCase("let x = $true$", "Keyword(Constants)")>]
    [<TestCase("let $``simple binding``$ = 1", "entity.name.field")>]
    [<TestCase("let inline$ ``add number``$ x y = x + y", "User Method Declaration")>]
    [<TestCase("$|>$ Option.bind", "Punctuation(Brackets)")>]
    [<TestCase("$typeof$<int>", "entity.name.field")>]
    [<TestCase("editor.CaretOffset $<-$ offset", "Punctuation(Brackets)")>]
    [<TestCase("let x$  = $1", "source.fs")>]
    [<TestCase("let $x$     = ", "entity.name.field")>]
    [<TestCase(@"type thing = TP<$""$32""", "String")>]
    [<TestCase(@"type thing = TP<$MyParam$=""32"", MyOtherParam=42>", "entity.name.field")>]
    [<TestCase(@"let rec computeSomeFunction $x$ =", "entity.name.field")>]
    [<TestCase(@"Option.tryCast<MonoTextEditor$>($fun e", "Punctuation(Brackets)")>]
    [<TestCase(@"let mutable session$ = $setupSession()", "source.fs")>]
    [<TestCase(@"$0b010101$", "constant.numeric.source.fs")>]
    [<TestCase(@"w11.Position <- $0$", "constant.numeric.source.fs")>]
    [<TestCase(@" $-1$", "constant.numeric.source.fs")>]
    [<TestCase(@"[0$..$1]", "source.fs")>]
    [<TestCase("let mutable x$   = $1", "source.fs")>]
    [<TestCase("$and$ Forest =", "keyword.source.fs")>]
    [<TestCase("let rec go $xs$ =", "entity.name.field")>]
    [<TestCase("let x = $Some$ 1", "entity.name.class")>]
    [<TestCase("type $A$ =", "entity.name.class")>]
    [<TestCase("type $A $", "entity.name.class")>]
    [<TestCase("type A$ = $A of a", "source.fs")>]
    [<TestCase("type A = A of $a$:int", "entity.name.class")>]
    [<TestCase("type A = A of a:$int$", "entity.name.class")>]
    [<TestCase("type string=$String$", "entity.name.class")>]
    [<TestCase("module A$=$", "source.fs")>]
    [<TestCase("let $defaultKeyword$ =", "entity.name.field")>]
    member x.``Syntax highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)

    [<TestCase(@"static member $GetDefaultConfiguration$ :", "User Method Declaration")>]
    [<TestCase(@"member $``Syntax Highlighting``$ :", "User Method Declaration")>]
    [<TestCase(@"-> $^T$ :", "entity.name.class")>]
    [<TestCase(@"   -> $seq$<'T>", "entity.name.class")>]
    [<TestCase(@"   $list$ :", "entity.name.field")>]

    member x.``Tooltip highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)
