namespace MonoDevelop.FSharp

#if MDVERSION_5_5
#else
open System
open System.Collections.Generic
open MonoDevelop.Ide
open MonoDevelop.NUnit
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding

type FSharpUnitTestTextEditorExtension() =
    inherit AbstractUnitTestTextEditorExtension()
    
    let hasAttributeNamed name (att:FSharpAttribute) =
        att.AttributeType.FullName.Contains name

    let createTestCase (tc:FSharpAttribute) =
        let sb = Text.StringBuilder(32)
        sb.Append "(" |> ignore
        tc.ConstructorArguments 
        |> Seq.iteri (fun i (_,arg) ->
            if i > 0 then sb.Append ", " |> ignore
            match arg with
            | :? string as s -> sb.AppendFormat ("\"{0}\"", s) |> ignore
            | :? char as c -> sb.AppendFormat ("\"{0}\"", c) |> ignore
            | other -> sb.Append (other) |> ignore )
        sb.Append ")" |> ignore
        sb.ToString ()

    override x.GatherUnitTests () =
        let tests = ResizeArray<AbstractUnitTestTextEditorExtension.UnitTestLocation>()

        let hasNUnitReference =
            match x.Document.Project with
            | null -> false
            | :? MonoDevelop.Projects.DotNetProject as dnp ->
                let refs = dnp.GetReferencedAssemblies(MonoDevelop.getConfig()) |> Seq.toArray
                refs |> Seq.exists (fun r -> r.EndsWith ("nunit.framework.dll", StringComparison.InvariantCultureIgnoreCase)) 
            | _ -> false

        if x.Document.ParsedDocument = null || not hasNUnitReference then tests :> IList<_> else

        match x.Document.ParsedDocument.Ast with
        | :? ParseAndCheckResults as ast ->
            let allSymbols = ast.GetAllUsesOfAllSymbolsInFile() |> Async.RunSynchronously
            let testSymbols = 
                match allSymbols with
                | None -> None
                | Some symbols ->
                    symbols 
                    |> Array.filter
                        (fun s -> match s.Symbol with
                                  | :? FSharpMemberOrFunctionOrValue as fom -> 
                                        fom.Attributes
                                        |> Seq.exists (hasAttributeNamed "NUnit.Framework.TestAttribute")
                                  | :? FSharpEntity as fse ->
                                        fse.Attributes
                                        |> Seq.exists (hasAttributeNamed "NUnit.Framework.TestFixtureAttribute")
                                  | _ -> false )
                    |> Seq.distinctBy (fun su -> su.RangeAlternate)
                    |> Seq.choose (fun symbolUse -> 
                                       let range = symbolUse.RangeAlternate
                                       let test = AbstractUnitTestTextEditorExtension.UnitTestLocation(range.StartLine)
                                       match symbolUse.Symbol with
                                       | :? FSharpMemberOrFunctionOrValue as func -> 
                                            let typeName = func.EnclosingEntity.QualifiedName
                                            let methName = func.CompiledName
                                            let isIgnored = func.Attributes |> Seq.exists (hasAttributeNamed "NUnit.Framework.IgnoreAttribute")
                                            //add test cases
                                            let testCases = func.Attributes |> Seq.filter (hasAttributeNamed "NUnit.Framework.TestCaseAttribute")
                                            testCases
                                            |> Seq.map createTestCase
                                            |> test.TestCases.AddRange
                                            test.UnitTestIdentifier <- typeName + "." + methName
                                            test.IsIgnored <- isIgnored
                                            Some test
                                        | :? FSharpEntity as entity ->
                                            let typeName = entity.QualifiedName
                                            let isIgnored = entity.Attributes |> Seq.exists (hasAttributeNamed "NUnit.Framework.IgnoreAttribute")
                                            test.UnitTestIdentifier <- typeName
                                            test.IsIgnored <- isIgnored
                                            test.IsFixture <- true
                                            Some test
                                        | _ -> None)
                    |> Some
            testSymbols
            |> Option.iter tests.AddRange
        | _ -> ()
        tests :> IList<_>
#endif
