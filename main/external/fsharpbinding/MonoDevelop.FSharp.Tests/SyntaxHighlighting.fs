namespace MonoDevelopTests

open System
open System.IO
open System.Threading
open NUnit.Framework
open MonoDevelop.Ide.Editor.Highlighting
open MonoDevelop.Ide.Editor

[<TestFixture>]
type SyntaxHighlighting() =
    let setupHighlighting (editor:TextEditor) =
        editor.MimeType <- "text/x-fsharp"
        let assembly = typeof<MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlighting>.Assembly
        use stream = assembly.GetManifestResourceStream("F#.sublime-syntax")
        use reader = new StreamReader(stream)
        let highlighting = Sublime3Format.ReadHighlighting(reader)
        highlighting.PrepareMatches()
        editor.SyntaxHighlighting <- new MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlighting(highlighting, editor)

    let assertStyle (input:string, expectedStyle:string) =
        let offset = input.IndexOf("$")
        let length = input.LastIndexOf("$") - offset - 1
        let input = input.Replace("$", "")
        let doc = TestHelpers.createDocWithoutParsing (" " + input) "defined"
        let editor = doc.Editor
        setupHighlighting editor
        let stack = editor.GetScopeStackAsync(offset+1, CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously
        let first = stack |> Seq.head

        Assert.AreEqual(expectedStyle, first)

    [<TestCase("let assertStyle $($input", "punctuation.section.brackets")>]
    [<TestCase("printfn $\"string\"$", "string.quoted.double.source.fs")>]
    [<TestCase("override x.CompareTo $other$", "entity.name.field")>]
    [<TestCase("let $getEditor$()", "entity.name.function")>]
    [<TestCase("$\"_underline_\"$", "string.quoted.double.source.fs")>]
    [<TestCase("let $_x$, y = 1,2", "source.fs")>]
    [<TestCase("$yield$ x", "keyword.source.fs")>]
    [<TestCase("$let$ (|Event|_|) = function", "keyword.source.fs")>]
    [<TestCase("$namespace$ MonoDevelop", "keyword.source.fs")>]
    [<TestCase("namespace $MonoDevelop$", "source.fs")>]
    [<TestCase("let simpleBinding = $1$", "constant.numeric.source.fs")>]
    [<TestCase("$let$ simpleBinding = 1", "keyword.source.fs")>]
    [<TestCase("$let!$ simpleBinding = 1", "keyword.source.fs")>]
    [<TestCase("let $simpleBinding$ = 1", "entity.name.field")>]
    [<TestCase("let $offset$ = 1", "entity.name.field")>]
    [<TestCase("let $add$ x y = x + y", "entity.name.function")>]
    [<TestCase("let add $param$", "entity.name.field")>]
    [<TestCase("let $add$ param y = param + y", "entity.name.function")>]
    [<TestCase("let simpleBinding$ = $1", "source.fs")>]
    [<TestCase("$open$ MonoDevelop", "keyword.source.fs")>]
    [<TestCase("open$ MonoDevelop$", "source.fs")>]
    [<TestCase("open$ Mono.Text$", "source.fs")>]
    [<TestCase("Seq.$find$ (", "entity.name.function")>]
    [<TestCase("SyntaxModeService.$GetColorStyle$ (\"Gruvbox\")", "entity.name.function")>]
    [<TestCase("Seq.find ($fun$ c", "keyword.source.fs")>]
    [<TestCase("$type$ SyntaxHighlighting() =", "keyword.source.fs")>]
    [<TestCase("type $SyntaxHighlighting$ () =", "entity.name.class")>]
    [<TestCase("type $``Completion Tests``$ () =", "entity.name.class")>]
    [<TestCase("$module$ MyModule =", "keyword.source.fs")>]
    [<TestCase("module $MyModule$ =", "entity.name.class")>]
    [<TestCase("[<$TestCase$(", "entity.name.class")>]
    [<TestCase("$[<$TestCase(", "punctuation.section.brackets")>]
    [<TestCase("inherits $SyntaxHighlighting$ () =", "entity.name.class")>]
    [<TestCase("new $DefaultBraceMatcher$()", "entity.name.class")>]
    [<TestCase("$match$ (startOffset, endOffset) with", "keyword.source.fs")>]
    [<TestCase("$else$", "keyword.source.fs")>]
    [<TestCase("let x (y: $string$", "entity.name.class")>]
    [<TestCase("string.$Length$", "entity.name.property")>]
    [<TestCase("$($", "punctuation.section.brackets")>]
    [<TestCase("$<$", "punctuation.section.brackets")>]
    [<TestCase("$[$", "punctuation.section.brackets")>]
    [<TestCase("${$", "punctuation.section.brackets")>]
    [<TestCase("do Something() |> $ignore$", "entity.name.function")>]
    [<TestCase("let $mutable$ x   = 1", "keyword.source.fs")>]
    [<TestCase("let mutable  $x$ = 1", "entity.name.field")>]
    [<TestCase("let mutable x$ = $1", "source.fs")>]
    [<TestCase("c.Style$ |> $should equal", "source.fs")>]
    [<TestCase("c.Style |> $should$ equal", "entity.name.function")>]
    [<TestCase("1 |> $fun$ x -> x", "keyword.source.fs")>]
    [<TestCase("match $x$ with", "entity.name.field")>]
    [<TestCase("Unchecked.defaultof<$_$>", "source.fs")>]
    [<TestCase("Seq.$add$", "entity.name.function")>]
    [<TestCase("let inline$ add$ x y = x + y", "entity.name.function")>]
    [<TestCase("$override$ x.Something()", "keyword.source.fs")>]
    [<TestCase("member x.$``some identifier``$ = 1", "entity.name.field")>]
    [<TestCase("member x.$``some identifier``$ () = 1", "entity.name.function")>]
    [<TestCase("let mutable $vbox4$ : Gtk.VBox = null", "entity.name.field")>]
    [<TestCase("$return$ x", "keyword.source.fs")>]
    [<TestCase("$return!$ x", "keyword.source.fs")>]
    [<TestCase("member val IndentOnTryWith = false with $get, set$", "source.fs")>]
    [<TestCase("| Some $funion$ -> ", "entity.name.field")>]
    //[<TestCase("yield $sprintf$ \"%A\"", "entity.name.function")>]
    [<TestCase("$doc$.Editor", "entity.name.field")>]
    [<TestCase(":> $SomeType$", "entity.name.class")>]
    [<TestCase("($'c'$)", "string.quoted.single.source.fs")>]
    [<TestCase("| Type of $string$", "entity.name.class")>]
    [<TestCase("$DisplayFlags$ = DisplayFlags.DescriptionHasMarkup", "entity.name.field")>]
    [<TestCase("let shouldEqual (x: $'a$) (y: 'a) =", "entity.name.class")>]
    [<TestCase("| :? $string$", "entity.name.class")>]
    [<TestCase("let inline $private$ is expr s =", "keyword.source.fs")>]
    [<TestCase("let inline private$ is$ expr s =", "entity.name.function")>]
    [<TestCase("override x.$CanHandle$ editor", "entity.name.function")>]
    [<TestCase("let addEdge ((n1, n2): 'n * $'n$)", "entity.name.class")>]
    [<TestCase("Map<'n, Set<'n$>>$", "punctuation.section.brackets")>]
    [<TestCase("let docs = $openDocuments$()", "entity.name.function")>]
    [<TestCase("let x = $true$", "constant.language.source.fs")>]
    [<TestCase("let $``simple binding``$ = 1", "entity.name.field")>]
    [<TestCase("let inline$ ``add number``$ x y = x + y", "entity.name.function")>]
    [<TestCase("$|>$ Option.bind", "punctuation.section.brackets")>]
    [<TestCase("$typeof$<int>", "entity.name.field")>]
    [<TestCase("editor.CaretOffset $<-$ offset", "punctuation.section.brackets")>]
    [<TestCase("let x$  = $1", "source.fs")>]
    [<TestCase("let $x$     = ", "entity.name.field")>]
    [<TestCase(@"type thing = TP<$""$32""", "string.quoted.double.source.fs")>]
    [<TestCase(@"type thing = TP<$MyParam$=""32"", MyOtherParam=42>", "entity.name.field")>]
    [<TestCase(@"let rec computeSomeFunction $x$ =", "entity.name.field")>]
    [<TestCase(@"Option.tryCast<MonoTextEditor$>($fun e", "punctuation.section.brackets")>]
    [<TestCase(@"let mutable session$ = $setupSession()", "source.fs")>]
    [<TestCase(@"$0b010101$", "constant.numeric.source.fs")>]
    [<TestCase(@"w11.Position <- $0$", "constant.numeric.source.fs")>]
    [<TestCase(@" $-1$", "constant.numeric.source.fs")>]
    [<TestCase(@"[0$..$1]", "source.fs")>]
    [<TestCase("let mutable x$   = $1", "source.fs")>]
    [<TestCase("$and$ Forest =", "keyword.source.fs")>]
    [<TestCase("let x = $Some$ 1", "entity.name.class")>]
    [<TestCase("type $A$ =", "entity.name.class")>]
    [<TestCase("type $A $", "entity.name.class")>]
    [<TestCase("type A$ = $A of a", "source.fs")>]
    [<TestCase("type A = A of $a$:int", "entity.name.class")>]
    [<TestCase("type A = A of a:$int$", "entity.name.class")>]
    [<TestCase("type string=$String$", "entity.name.class")>]
    [<TestCase("module A$=$", "source.fs")>]
    [<TestCase("let $defaultKeyword$ =", "entity.name.field")>]
    [<TestCase("addButton (\"$gtk-save$\"", "string.quoted.double.source.fs")>]
    [<TestCase(@"namespace $rec$ MonoDevelop", "keyword.source.fs")>]
    [<TestCase(@"$type$ internal SomeType", "keyword.source.fs")>]
    [<TestCase(@"$#I$ /some/path", "meta.preprocessor.source.fs")>]
    [<TestCase("sprintf \"Some multi\n$line string$\"", "string.quoted.double.source.fs")>]
    [<TestCase("module $My.Namespace.$Module", "source.fs")>]
    [<TestCase("module My.Namespace.$Module$", "entity.name.class")>]
    [<TestCase("module rec My.Namespace.$Module$", "entity.name.class")>]
    [<TestCase("module rec $My.Namespace$.Module", "source.fs")>]
    [<TestCase("$// Some comment$", "comment.line.source.fs")>]
    [<TestCase("$/// Some comment$", "comment.line.documentation.source.fs")>]
    [<TestCase("$(* Some comment*)$", "comment.block.source.fs")>]
    [<TestCase("$let$ ((x,y", "keyword.source.fs")>]
    member x.``Syntax highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)

    [<TestCase(@"static member $GetDefaultConfiguration$ :", "entity.name.function")>]
    [<TestCase(@"member $``Syntax Highlighting``$ :", "entity.name.function")>]
    [<TestCase(@"-> $^T$ :", "entity.name.class")>]
    [<TestCase(@"   -> $seq$<'T>", "entity.name.class")>]
    [<TestCase(@"   $list$ :", "entity.name.field")>]
    member x.``Tooltip highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)
