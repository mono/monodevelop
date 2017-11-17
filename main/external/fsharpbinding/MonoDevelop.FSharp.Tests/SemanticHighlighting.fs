namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Ide.Editor
open FsUnit

[<TestFixture>]
type SemanticHighlighting() =
    let getStyle (content : string) =
        let fixedc = content.Replace("$", "")
        let doc = TestHelpers.createDoc fixedc "defined"
        let tsc = SyntaxMode.tryGetTokensSymbolsAndColours doc

        let segments =
            doc.Editor.GetLines()
            |> Seq.map (fun line -> SyntaxMode.getColouredSegment tsc line.LineNumber line.Offset (doc.Editor.GetLineText line))

        for line in segments do
            line |> Seq.toList |> List.rev |> List.iteri (fun i seg ->
                printfn """%s"%s" Style:%s S:%i E:%i L:%i"""
                    (String.replicate i " ")
                    (doc.Editor.GetTextBetween(seg.Offset, seg.EndOffset))
                    seg.ColorStyleKey
                    seg.Offset
                    seg.EndOffset
                    seg.Length )
            printfn "\n"

        let offset = content.IndexOf("$")
        let endOffset = content.LastIndexOf("$") - 1
        let segment = segments |> Seq.concat |>  Seq.tryFind (fun s -> s.Offset = offset && s.EndOffset = endOffset)
        match segment with
        | Some(s) -> s.ColorStyleKey
        | _ -> "segment not found"

    [<Test>]
    member x.Undefined_IfDef() =
       let content ="""
#if undefined
let sub = (-)
$let$ add = (+)
#endif"""
       getStyle content |> should equal "punctuation.definition.comment.source"

    [<Test>]
    member x.Module_is_highlighted() =
        let content = """
module MyModule =
    let someFunc() = ()

module Consumer =
    $MyModule$.someFunc()"""
        let output = getStyle content
        output |> should equal "entity.name.class"

    [<Test>]
    member x.Some_is_highlighted() =
        let content = 
            """
            module MyModule =
            let x = $Some$ 1
            """
        let output = getStyle content
        output |> should equal "entity.name.class"

    [<Test>]
    member x.Type_is_highlighted() =
        let content = """
open System

module MyModule =
    let guid = $Guid$.NewGuid()"""
        let output = getStyle content
        output |> should equal "entity.name.struct"

    [<Test>]
    member x.Add_is_plain_text() =
        let content = "let $add$ = (+)"
        getStyle content |> should equal "entity.name.function"

    [<TestCase("let $add$ = (+)", "entity.name.function")>]
    [<TestCase("let $simpleBinding$ = 1", "entity.name.field")>]
    [<TestCase("type o = $System$.Object", "source.fs")>]
    [<TestCase("type o = System.$Object$", "entity.name.class")>]
    [<TestCase("let x, $_y$ = 1, 2", "source.fs")>]
    member x.Semantic_highlighting(source, expectedStyle) =
        getStyle source |> should equal expectedStyle
        
    [<Test>]    
    member x.Generics_are_highlighted() =
        let content = """
type Class<$'a$>() = class end
    let _ = new Class<_>()"""
        let output = getStyle content
        output |> should equal "entity.name.typeparameter"
     
    [<Test>]    
    member x.Type_constraints_are_highlighted() =
        let content = "type Constrained<'a when $'a$ :> IDisposable> = class end"
        let output = getStyle content
        output |> should equal "entity.name.typeparameter"

    [<Test>]    
    member x.Static_inlined_type_constraints_are_highlighted() =
        let content = "let inline test (x: $^a$) (y: ^b) = x + y"
        let output = getStyle content
        output |> should equal "entity.name.typeparameter"

    [<Test>]
    member x.``Computation expression is highlighted as a keyword``() =
        let content = """
        module ComputationExpressions
        let x2 = $query$ { for i in 0 .. 100 do
                         where (i = 0)
                         select (i,i) }
        """
        let output = getStyle content
        output |> should equal "keyword.other.source"
