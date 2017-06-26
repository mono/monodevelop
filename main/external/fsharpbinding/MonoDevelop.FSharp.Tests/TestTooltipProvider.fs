namespace MonoDevelopTests
open System.Text.RegularExpressions
open System.Threading
open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp.MonoDevelop
open MonoDevelop.FSharp

[<TestFixture>]
type TestTooltipProvider() =
    let stripHtml html =
        Regex.Replace(html, "<.*?>", "")

    let htmlDecode (s: string) =
        s.Replace("&lt;", "<")
         .Replace("&gt;", ">")
         .Replace("&apos;", "'")

    let getSymbol (source: string) =
        let offset = source.IndexOf("$")
        let source = source.Replace("$", "")

        let doc = TestHelpers.createDoc source ""
        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset

        let symbolUse = doc.Ast.GetSymbolAtLocation(line, col - 1, lineStr) |> Async.RunSynchronously
        lineStr, col, symbolUse, doc.Editor

    let getTooltip source =
        let _, _, symbolUse, _ = getSymbol source
        symbolUse |> Option.bind SymbolTooltips.getTooltipFromSymbolUse

    let getTooltipSignature (source: string) =
        match getTooltip source with
        | Some(tip,_,_) -> tip
        | _ ->  ""

    let getTooltipFooter (source: string) =
        let footer =
            match getTooltip source with
            | Some(_,_,footer) -> footer
            | _ ->  ""

        footer |> stripHtml |> htmlDecode
        
    let getTooltipSummary (source: string) =
        match getTooltip source with
        | Some(_,summary,_) -> SymbolTooltips.formatSummary summary
        | _ ->  ""

    [<Test>]
    member this.``Namespace has correct segment``() =
        let line, col, symbolUse, editor = getSymbol "open Sys$tem"
        let segment = Symbols.getTextSegment editor symbolUse.Value col line
        segment.Offset |> should equal 5
        segment.EndOffset |> should equal 11

    [<Test>]
    member this.``Tooltip arrows are right aligned``() =
        let input =
            """
            open System
            let toBeParti$allyApplied (datNumba: int) (thaString: string) (be: bool) =
                ()
            """

        let signature = getTooltipSignature input

        let expected = """val toBePartiallyApplied :
   datNumba : int    ->
   thaString: string ->
   be       : bool   
           -> unit"""

        signature |> shouldEqualIgnoringLineEndings expected

    [<Test>]
    member this.``Forall2 tooltip arrows right aligned``() =
        let input =
            """
            open System
            let allEqual coll = Seq.fora$ll2 (fun elem1 elem2 -> elem1 = elem2) coll
            """

        let signature = getTooltipSignature input

        let expected = """val forall2 :
   predicate: 'T1 -> 'T2 -> bool ->
   source1  : seq<'T1>           ->
   source2  : seq<'T2>           
           -> bool"""

        signature |> shouldEqualIgnoringLineEndings expected
        // Base Type Constraint

    [<Test>]
    member this.``Base type constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T :> System.Exception> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T :> System.Exception>"""

        signature |> should startWith expected

    [<Test>]
    member this.``Interface type constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T :> System.IComparable> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T :> System.IComparable>"""

        signature |> should startWith expected

    [<Test>]
    member this.``Null type constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : null> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : null>"""

        signature |> should startWith expected

    [<Test>]
    member this.``Member constraint with static member tooltip``() =
        let input =
            """
            type A$<'T when 'T : (static member staticMethod1 : unit -> 'T) > =
                class end
            """

        let signature = getTooltipSignature input
        let expected = """type A<^T when ^T : (static member staticMethod1 : unit -> ^T)>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Member constraint with instance member tooltip``() =
        let input =
            """
            type A$<'T when 'T : (member Method1 : 'T -> int)> =
                class end
            """

        let signature = getTooltipSignature input
        let expected = """type A<^T when ^T : (member Method1 : ^T -> int)>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Member constraint with property tooltip``() =
        let input =
            """
            type A$<'T when 'T : (member Property1 : int)> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<^T when ^T : (member Property1 : int)>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Constructor constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : (new : unit -> 'T)>() =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : (new : unit -> 'T)>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Reference type constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : not struct> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : not struct>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Enum constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : enum<uint32>> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : enum<uint32>>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Comparison constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : comparison> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : comparison>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Equality constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : equality> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : equality>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Delegate constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : delegate<obj * System.EventArgs, unit>> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : delegate<obj * System.EventArgs, unit>>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Unmanaged constraint tooltip``() =
        let input =
            """
            type A$<'T when 'T : unmanaged> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : unmanaged>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Member constraints with two type parameters tooltip``() =
        let input =
            """
            let inline ad$d(value1 : ^T when ^T : (static member (+) : ^T * ^T -> ^T), value2: ^T) =
                value1 + value2
            """

        let signature = getTooltipSignature input

        let expected = """val add :
   value1:  ^T  * 
   value2:  ^T 
        ->  ^T"""
        signature |> should startWith expected

    [<Test>]
    member this.``Member operator constraint tooltip``() =
        let input =
            """
            let inline heterog$enousAdd(value1 : ^T when (^T or ^U) : (static member (+) : ^T * ^U -> ^T), value2 : ^U) =
                value1 + value2
            """

        let signature = getTooltipSignature input

        let expected = """val heterogenousAdd :
   value1:  ^T  * 
   value2:  ^U 
        ->  ^T"""
        signature |> should startWith expected

    [<Test>]
    member this.``Multiple type constraints tooltip``() =
        let input =
            """
            type A$<'T,'U when 'T : equality and 'U : equality> =
                class end
            """

        let signature = getTooltipSignature input
        // not exactly right, but probably good enough for a tooltip
        // and I get this behaviour for free
        let expected = """type A<'T when 'T : equality,'U when 'U : equality>"""
        signature |> should startWith expected
        
    [<Test>]
    member this.``Struct type constraints tooltip``() =
        let input =
            """
            type A$<'T when 'T : struct> =
                class end
            """

        let signature = getTooltipSignature input
        let expected = """type A<'T when 'T : struct>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Backticked val tooltip``() =
        let input =
            """
            let ``backt$icked val`` =
                ()
            """
        let signature = getTooltipSignature input
        let expected = """val ``backticked val`` : unit"""

        signature |> should startWith expected
        
    [<Test>]
    member this.``Backticked function tooltip``() =
        let input =
            """
            let ``backt$icked fun`` =
                ()
            """
        let signature = getTooltipSignature input
        let expected = """val ``backticked fun`` : unit"""

        signature |> should startWith expected
        
    [<Test>]
    member this.``Operator tooltip``() =
        let input =
            """
            let add = ( +$ )
                ()
            """
        let signature = getTooltipSignature input
        let expected = """val ( + ) :
   x:  ^T1 ->
   y:  ^T2 
   ->  ^T3"""

        signature |> should equal expected
        
    [<TestCase("let ($|Even|Odd|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|$Even|Odd|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|Ev$en|Odd|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|Even$|Odd|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|Even|$Odd|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|Even|Od$d|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|Even|Odd$|) v = if v % 2 = 0 then Even(v) else Odd(v)");
      TestCase("let (|Even|Odd|$) v = if v % 2 = 0 then Even(v) else Odd(v)")>]
    member this.``Complete Active Pattern tooltip``(input) =
        let signature = getTooltipSignature input
        let expected = "val ( |Even|Odd| ) :\n   v: int \n   -> Choice<int,int>"
        signature |> should equal expected
        
    [<TestCase("let ($|Even|_|) v = if v % 2 = 0 then Some v else None");
      TestCase("let (|$Even|_|) v = if v % 2 = 0 then Some v else None");
      TestCase("let (|Ev$en|_|) v = if v % 2 = 0 then Some v else None");
      TestCase("let (|Even$|_|) v = if v % 2 = 0 then Some v else None");
      TestCase("let (|Even|$_|) v = if v % 2 = 0 then Some v else None");
      TestCase("let (|Even|_$|) v = if v % 2 = 0 then Some v else None");
      TestCase("let (|Even|_|$) v = if v % 2 = 0 then Some v else None")>]
    member this.``Partial Active Pattern tooltip``(input) =
        let signature = getTooltipSignature input
        let expected = "val ( |Even|_| ) :\n   v: int \n   -> int option"
        signature |> should equal expected
        
    [<Test>]
    member this.``Displays member type and assembly``() =
        let sequence = ["string"].Head
        let input = """let se$quence = ["string"].Head"""
        let footer = getTooltipFooter input
        let expected = "From type:\tString\nAssembly:\tFSharp.Core"

        footer |> shouldEqualIgnoringLineEndings expected
        
    [<Test>]
    member this.``Xml summary should be escaped``() =
        let input = """///<summary>This is the summary</summary>
type myT$ype = class end"""
        let summary = getTooltipSummary input
        let expected = "This is the summary"

        summary |> shouldEqual expected

    [<Test>]
    member this.``Function type tooltip``() =
        let input =
            """
            type Spell =
               | Frotz
               | Grotz

            type Sp$ellF = Spell -> Async<unit>
            """
        let signature = getTooltipSignature input
        let expected = "type SpellF = Spell -> Async<unit>"
        signature |> should equal expected