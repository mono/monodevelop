namespace MonoDevelop.FSharp

#if MDVERSION_5_5
#else
open System
open System.Collections.Generic
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.NUnit
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

module unitTestGatherer =
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

    let gatherUnitTests (editor: TextEditor, allSymbols:FSharpSymbolUse [] option) =
        let tests = ResizeArray<UnitTestLocation>()
        
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
                |> Seq.choose
                    (fun symbolUse -> 
                        let range = symbolUse.RangeAlternate
                        let startOffset = editor.LocationToOffset(range.StartLine, range.StartColumn+1)
                        let test = UnitTestLocation(startOffset)
                        match symbolUse.Symbol with
                        | :? FSharpMemberOrFunctionOrValue as func -> 
                            let typeName =
                                match func.EnclosingEntitySafe with
                                | Some ent -> ent.QualifiedName
                                | None _ ->
                                    MonoDevelop.Core.LoggingService.LogWarning(sprintf "F# GatherUnitTests: found a unit test method with no qualified name: %s" func.FullName)
                                    func.CompiledName
                            let methName = PrettyNaming.QuoteIdentifierIfNeeded func.CompiledName
                            let isIgnored =
                                func.Attributes
                                |> Seq.exists (hasAttributeNamed "NUnit.Framework.IgnoreAttribute")
                            //add test cases
                            let testCases =
                                func.Attributes
                                |> Seq.filter (hasAttributeNamed "NUnit.Framework.TestCaseAttribute")
                            testCases
                            |> Seq.map createTestCase
                            |> test.TestCases.AddRange
                            test.UnitTestIdentifier <- typeName + "." + methName
        
                            test.IsIgnored <- isIgnored
                            Some test
                        | :? FSharpEntity as entity ->
                            let typeName = entity.QualifiedName
                            let isIgnored =
                                entity.Attributes
                                |> Seq.exists (hasAttributeNamed "NUnit.Framework.IgnoreAttribute")
                            test.UnitTestIdentifier <- typeName
                            test.IsIgnored <- isIgnored
                            test.IsFixture <- true
                            Some test
                        | _ -> None)
                |> Some
        testSymbols
        |> Option.iter tests.AddRange 
        tests

    let hasNUnitReference (p:Project)=
        match p with
        | null -> false
        | :? MonoDevelop.Projects.DotNetProject as dnp ->
            try
                dnp.GetReferencedAssemblies(MonoDevelop.getConfig())
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Seq.toArray
                |> Seq.exists (fun r -> r.EndsWith ("nunit.framework.dll", StringComparison.InvariantCultureIgnoreCase)) 
            with ex ->
                MonoDevelop.Core.LoggingService.LogInternalError ("FSharpUnitTestTextEditorExtension: GatherUnitTests failed", ex)
                false
        | _ -> false

type FSharpUnitTestTextEditorExtension() =
    inherit AbstractUnitTestTextEditorExtension()

    override x.GatherUnitTests (cancellationToken) =
        let tests = ResizeArray<UnitTestLocation>()

        if x.DocumentContext = null || 
            x.DocumentContext.ParsedDocument = null || 
            not (unitTestGatherer.hasNUnitReference x.DocumentContext.Project) then
                Threading.Tasks.Task.FromResult (tests :> IList<_>)
        else
        Async.StartAsTask (
            cancellationToken = cancellationToken,
            computation = async {
                match x.DocumentContext.ParsedDocument.Ast with
                | :? ParseAndCheckResults as ast ->
                    let! symbols = ast.GetAllUsesOfAllSymbolsInFile()
                    tests.AddRange (unitTestGatherer.gatherUnitTests (x.Editor, symbols))
                | _ -> ()
                return tests :> IList<_>})
    #endif

type FSharpNUnitSourceCodeLocationFinder() =
    inherit NUnitSourceCodeLocationFinder()

    override x.GetSourceCodeLocationAsync(project, fixtureNamespace, fixtureTypeName, testName, token) =
        let computation =
            async {
                let idx = testName.IndexOf("(")
                let testName =
                    if idx > - 1 then
                        testName.Substring(idx)
                    else
                        testName
                
                let symbol = Search.getAllSymbolsInAllProjects()
                             |> AsyncSeq.toSeq
                             |> Seq.tryFind (fun sym -> 
                                 match sym.Symbol with
                                 | :? FSharpMemberOrFunctionOrValue as func ->
                                    func.CompiledName = testName
                                 | _ -> false)
                             
                
                match symbol with
                | Some sym ->
                    let location = sym.RangeAlternate
                    return SourceCodeLocation(location.FileName, location.StartLine, location.StartColumn + 1)
                | _ -> return null //?
            } 
        Async.StartAsTask(computation = computation, cancellationToken = token)

