namespace MonoDevelopTests
open Microsoft.FSharp.Compiler.SourceCodeServices
open System.Text.RegularExpressions
open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp

[<TestFixture>]
type TestTooltipProvider() =
    inherit TestBase()

    let stripHtml html = 
        Regex.Replace(html, "<.*?>", "")

    let htmlDecode (s: string) =
        s.Replace("&lt;", "<")
         .Replace("&gt;", ">")
         .Replace("&apos;", "'")

    let getTooltip source displayName =  
        let checker = FSharpChecker.Create()

        let file = "test.fsx"

        let projOptions = 
            checker.GetProjectOptionsFromScript(file, source)
                |> Async.RunSynchronously

        let allSymbols =
            async {
                let! pfr, cfa = checker.ParseAndCheckFileInProject(file, 0, source, projOptions)
                match cfa with
                | FSharpCheckFileAnswer.Succeeded cfr ->
                    let! symbols = cfr.GetAllUsesOfAllSymbolsInFile() 
                    return Some symbols
                | _ -> return None } |> Async.RunSynchronously

        let symbol =
            match allSymbols with
            | Some symbols -> symbols |> Seq.tryFind (fun s -> s.Symbol.DisplayName.StartsWith(displayName))
            | None -> None
        

        let tooltip = SymbolTooltips.getTooltipFromSymbolUse symbol.Value

        let signature =
            match tooltip with
            | ToolTip (tip, _) -> tip
            | _ ->  ""

        signature |> stripHtml |> htmlDecode

    [<Test>]
    member this.Formats_tooltip_arrows_right_aligned() =
        let input = 
            """
            open System
            let toBePartiallyApplied (datNumba: int) (thaString: string) (be: bool) =
                ()
            """
        let allEqual coll = Seq.forall2 (fun elem1 elem2 -> elem1 = elem2) coll
        let signature = getTooltip input "toBePartiallyApplied"

        let expected = """val toBePartiallyApplied :
   datNumba : int    ->
   thaString: string ->
   be       : bool   
           -> unit"""

        signature.ToString() |> should equal expected

    [<Test>]
    member this.Formats_forall2_tooltip_arrows_right_aligned() =
        let input = 
            """
            open System
            let allEqual coll = Seq.forall2 (fun elem1 elem2 -> elem1 = elem2) coll
            """
        
        let signature = getTooltip input "forall2"

        let expected = """val forall2 :
   predicate: 'T1 -> 'T2 -> bool ->
   source1  : seq<'T1>           ->
   source2  : seq<'T2>           
           -> bool"""
        
        signature.ToString() |> should equal expected
        // Base Type Constraint

    [<Test>]
    member this.Formats_base_type_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T :> System.Exception> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T :> System.Exception>"""

        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_interface_type_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T :> System.IComparable> = 
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T :> System.IComparable>"""

        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_null_type_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : null> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : null>"""

        signature.ToString() |> should startWith expected
    
    [<Test>]
    member this.Formats_member_constraint_with_static_member_tooltip() =
        let input = 
            """
            type A<'T when 'T : (static member staticMethod1 : unit -> 'T) > =
                class end
            """
        
        let signature = getTooltip input "A"
        let expected = """type A<^T when ^T : (static member staticMethod1 : unit -> ^T)>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_member_constraint_with_instance_member_tooltip() =
        let input = 
            """
            type A<'T when 'T : (member Method1 : 'T -> int)> =
                class end
            """
        
        let signature = getTooltip input "A"
        let expected = """type A<^T when ^T : (member Method1 : ^T -> int)>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_member_constraint_with_property_tooltip() =
        let input = 
            """
            type A<'T when 'T : (member Property1 : int)> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<^T when ^T : (member Property1 : int)>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_constructor_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : (new : unit -> 'T)>() =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : (new : unit -> 'T)>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_reference_type_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : not struct> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : not struct>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_enum_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : enum<uint32>> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : enum<uint32>>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_comparison_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : comparison> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : comparison>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_equality_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : equality> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : equality>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_delegate_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : delegate<obj * System.EventArgs, unit>> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : delegate<obj * System.EventArgs, unit>>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    member this.Formats_unmanaged_constraint_tooltip() =
        let input = 
            """
            type A<'T when 'T : unmanaged> =
                class end
            """
        
        let signature = getTooltip input "A"

        let expected = """type A<'T when 'T : unmanaged>"""
        signature.ToString() |> should startWith expected

    [<Test>]
    [<Ignore>]
    member this.Formats_member_constraints_with_two_type_parameters_tooltip() =
        let input = 
            """
            let inline add(value1 : ^T when ^T : (static member (+) : ^T * ^T -> ^T), value2: ^T) =
                value1 + value2
            """
        
        let signature = getTooltip input "add"

        let expected = """???"""
        signature.ToString() |> should startWith expected

    [<Test>]
    [<Ignore>]
    member this.Formats_member_operator_constraint_tooltip() =
        let input = 
            """
            let inline heterogenousAdd(value1 : ^T when (^T or ^U) : (static member (+) : ^T * ^U -> ^T), value2 : ^U) =
                value1 + value2
            """
        
        let signature = getTooltip input "heterogenousAdd"

        let expected = """???"""
        signature.ToString() |> should startWith expected
    
    [<Test>]
    member this.Formats_multiple_type_constraints_tooltip() =
        let input = 
            """
            type A<'T,'U when 'T : equality and 'U : equality> =
                class end
            """
        
        let signature = getTooltip input "A"
        // not exactly right, but probably good enough for a tooltip
        // and I get this behaviour for free
        let expected = """type A<'T when 'T : equality,'U when 'U : equality>"""
        signature.ToString() |> should startWith expected

type Class14<'T,'U when 'T : equality and 'U : equality> =
    class end
//type yo = 
//    let inline add(value1 : ^T when ^T : (static member (+) : ^T * ^T -> ^T), value2: ^T) =
//        value1 + value2
//let inline heterogenousAdd(value1 : ^T when (^T or ^U) : (static member (+) : ^T * ^U -> ^T), value2 : ^U) =
//    value1 + value2
type Class12<'T when 'T : delegate<obj * System.EventArgs, unit>> =
   class end

type Class11<'T when 'T : equality> =
   class end

type Class10<'T when 'T : comparison> =
   class end

type Class9<'T when 'T : enum<uint32>> =
    class end

type Class8<'T when 'T : not struct> =
   class end

type Class7<'T when 'T : (new : unit -> 'T)>() =
    class end
   //member val Field = new 'T()

type Class6<'T when 'T : (member Property1 : int)> =
    class end

type A<'T> = { i : 'T -> unit }
type Class1<'T when 'T :> System.Exception> =
    class end

type A1<'T when 'T : (static member staticMethod1 : unit -> 'T) > =
                class end
type B<'a> = 
    class end

//^a when (^a : (member Accessibility : FSharpAccessibility)`
//type A<'T> (requires 'T : Exception)>