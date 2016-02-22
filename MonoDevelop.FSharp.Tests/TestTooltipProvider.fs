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

    let getTooltip (source: string) =
        let offset = source.IndexOf("§")
        let source = source.Replace("§", "")

        let doc = TestHelpers.createDoc source ""
        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset

        let symbolUse = doc.Ast.GetSymbolAtLocation(line, col - 1, lineStr)
                        |> Async.RunSynchronously
                        |> Option.get

        SymbolTooltips.getTooltipFromSymbolUse symbolUse

    let getTooltipSignature (source: string) =
        let signature =
            match getTooltip source with
            | Some(tip, _signature, footer) -> tip
            | _ ->  ""

        signature |> stripHtml |> htmlDecode

    let getTooltipFooter (source: string) =
        let footer =
            match getTooltip source with
            | Some(tip, _signature, footer) -> footer
            | _ ->  ""

        footer |> stripHtml |> htmlDecode

    [<Test>]
    member this.Formats_tooltip_arrows_right_aligned() =
        let input =
            """
            open System
            let toBeParti§allyApplied (datNumba: int) (thaString: string) (be: bool) =
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
    member this.Formats_forall2_tooltip_arrows_right_aligned() =
        let input =
            """
            open System
            let allEqual coll = Seq.fora§ll2 (fun elem1 elem2 -> elem1 = elem2) coll
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
    member this.Formats_base_type_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T :> System.Exception> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T :> System.Exception>"""

        signature |> should startWith expected

    [<Test>]
    member this.Formats_interface_type_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T :> System.IComparable> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T :> System.IComparable>"""

        signature |> should startWith expected

    [<Test>]
    member this.Formats_null_type_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : null> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : null>"""

        signature |> should startWith expected

    [<Test>]
    member this.Formats_member_constraint_with_static_member_tooltip() =
        let input =
            """
            type A§<'T when 'T : (static member staticMethod1 : unit -> 'T) > =
                class end
            """

        let signature = getTooltipSignature input
        let expected = """type A<^T when ^T : (static member staticMethod1 : unit -> ^T)>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_member_constraint_with_instance_member_tooltip() =
        let input =
            """
            type A§<'T when 'T : (member Method1 : 'T -> int)> =
                class end
            """

        let signature = getTooltipSignature input
        let expected = """type A<^T when ^T : (member Method1 : ^T -> int)>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_member_constraint_with_property_tooltip() =
        let input =
            """
            type A§<'T when 'T : (member Property1 : int)> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<^T when ^T : (member Property1 : int)>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_constructor_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : (new : unit -> 'T)>() =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : (new : unit -> 'T)>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_reference_type_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : not struct> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : not struct>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_enum_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : enum<uint32>> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : enum<uint32>>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_comparison_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : comparison> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : comparison>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_equality_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : equality> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : equality>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_delegate_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : delegate<obj * System.EventArgs, unit>> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : delegate<obj * System.EventArgs, unit>>"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_unmanaged_constraint_tooltip() =
        let input =
            """
            type A§<'T when 'T : unmanaged> =
                class end
            """

        let signature = getTooltipSignature input

        let expected = """type A<'T when 'T : unmanaged>"""
        signature |> should startWith expected

    [<Test>]
    [<Ignore>]
    member this.Formats_member_constraints_with_two_type_parameters_tooltip() =
        let input =
            """
            let inline ad§d(value1 : ^T when ^T : (static member (+) : ^T * ^T -> ^T), value2: ^T) =
                value1 + value2
            """

        let signature = getTooltipSignature input

        let expected = """???"""
        signature |> should startWith expected

    [<Test>]
    [<Ignore>]
    member this.Formats_member_operator_constraint_tooltip() =
        let input =
            """
            let inline heterog§enousAdd(value1 : ^T when (^T or ^U) : (static member (+) : ^T * ^U -> ^T), value2 : ^U) =
                value1 + value2
            """

        let signature = getTooltipSignature input

        let expected = """???"""
        signature |> should startWith expected

    [<Test>]
    member this.Formats_multiple_type_constraints_tooltip() =
        let input =
            """
            type A§<'T,'U when 'T : equality and 'U : equality> =
                class end
            """

        let signature = getTooltipSignature input
        // not exactly right, but probably good enough for a tooltip
        // and I get this behaviour for free
        let expected = """type A<'T when 'T : equality,'U when 'U : equality>"""
        signature |> should startWith expected
        
    [<Test>]
    member this.``Formats struct type_constraints tooltip``() =
        let input =
            """
            type A§<'T when 'T : struct> =
                class end
            """

        let signature = getTooltipSignature input
        let expected = """type A<'T when 'T : struct>"""
        signature |> should startWith expected

    [<Test>]
    member this.``Formats backticked val tooltip``() =
        let input =
            """
            let ``backt§icked val`` =
                ()
            """
        let signature = getTooltipSignature input
        let expected = """val ``backticked val`` : unit"""

        signature |> should startWith expected
        
    [<Test>]
    member this.``Formats backticked function tooltip``() =
        let input =
            """
            let ``backt§icked fun`` =
                ()
            """
        let signature = getTooltipSignature input
        let expected = """val ``backticked fun`` : unit"""

        signature |> should startWith expected
        
    [<Test>]
    member this.``Formats operator tooltip``() =
        let input =
            """
            let add = ( +§ )
                ()
            """
        let signature = getTooltipSignature input
        let expected = """val ( + ) :
   x:  ^T1 ->
   y:  ^T2 
   ->  ^T3"""

        signature |> should equal expected
        
    [<Test;Ignore>]
    member this.``Format Active Pattern tooltip``() =
        let input =
            """
            let (|Ev§en|Odd|) v =
                if v % 2 = 0 then Even(v)
                else Odd(v)
            """
        let signature = getTooltipSignature input
        let expected = """unit"""

        signature |> should equal expected
        
    [<Test>]
    member this.``Displays member type and assembly``() =
        let sequence = ["string"].Head
        let input = """let se§quence = ["string"].Head"""
        let footer = getTooltipFooter input
        let expected = "From type:\tString\nAssembly:\tFSharp.Core"

        footer |> shouldEqualIgnoringLineEndings expected

 