namespace MonoDevelop.FSharp

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

    override x.GatherUnitTests () =
        let loc = x.Document.Editor.Caret.Location
        let tests = ResizeArray<AbstractUnitTestTextEditorExtension.UnitTestLocation>()

        if x.Document.ParsedDocument = null || IdeApp.Workbench.ActiveDocument <> x.Document then tests :> IList<_> else
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
                                  | :? FSharpMemberFunctionOrValue as fom -> 
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
                                       | :? FSharpMemberFunctionOrValue as func -> 
                                            let typeName = func.EnclosingEntity.QualifiedName
                                            let methName = func.CompiledName
                                            let isIgnored = func.Attributes |> Seq.exists (hasAttributeNamed "NUnit.Framework.IgnoreAttribute")
                                            //add test cases
                                            let testCases = func.Attributes |> Seq.filter (hasAttributeNamed "NUnit.Framework.TestCaseAttribute")
                                            testCases
                                            |> Seq.iter (fun tc -> let ctorArgs = tc.ConstructorArguments |> Seq.cast<string>
                                                                   let fullArgs = "(" + (ctorArgs |> String.concat ", ") + ")"
                                                                   test.TestCases.Add fullArgs )
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