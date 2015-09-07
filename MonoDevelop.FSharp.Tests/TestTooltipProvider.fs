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