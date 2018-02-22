namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.UnitTesting
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices

module unitTestGatherer =
    let hasAttributeNamed (att:FSharpAttribute) (unitTestMarkers: IUnitTestMarkers[]) (filter:  string -> IUnitTestMarkers -> bool) =
        let attributeName = att.AttributeType.TryFullName
        match attributeName with
        | Some name ->
            unitTestMarkers
            |> Seq.exists (filter name)
        | None -> false

    let createTestCase (tc:FSharpAttribute) =
        let sb = Text.StringBuilder()
        let print format = Printf.bprintf sb format
        print "%s" "("
        tc.ConstructorArguments 
        |> Seq.iteri (fun i (_,arg) ->
            if i > 0 then print "%s" ", "
            match arg with
            | :? string as s -> print "\"%s\"" s
            | :? char as c -> print "\"%c\"" c
            | other -> print "%s" (other |> string))
        print "%s" ")"
        sb |> string

    let gatherUnitTests (unitTestMarkers: IUnitTestMarkers[], editor: TextEditor, allSymbols:FSharpSymbolUse [] option) =
        let hasAttribute a = hasAttributeNamed a unitTestMarkers
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
                                  |> Seq.exists (fun a -> hasAttribute a (fun attributeName m -> m.TestMethodAttributeMarker = attributeName || m.TestCaseMethodAttributeMarker = attributeName) )
                              | :? FSharpEntity as fse -> 
                                      fse.MembersFunctionsAndValues
                                      |> Seq.exists (fun fom -> fom.Attributes
                                                                |> Seq.exists (fun a -> hasAttribute a (fun attributeName m -> m.TestMethodAttributeMarker = attributeName || m.TestCaseMethodAttributeMarker = attributeName) ))
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
                                match func.EnclosingEntity with
                                | Some ent -> ent.QualifiedName
                                | None _ ->
                                    MonoDevelop.Core.LoggingService.LogWarning(sprintf "F# GatherUnitTests: found a unit test method with no qualified name: %s" func.FullName)
                                    func.CompiledName
                            let methName = func.CompiledName
                            let isIgnored =
                                func.Attributes
                                |> Seq.exists (fun a -> hasAttribute a (fun attributeName m -> m.IgnoreTestMethodAttributeMarker = attributeName))
                            //add test cases
                            let testCases =
                                func.Attributes
                                |> Seq.filter (fun a -> hasAttribute a (fun attributeName m -> m.TestCaseMethodAttributeMarker = attributeName))
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
                                |> Seq.exists (fun a -> hasAttribute a (fun attributeName m -> m.IgnoreTestMethodAttributeMarker = attributeName))
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
                |> Seq.map (fun a -> a.FilePath.FileName)
                |> Seq.exists (fun r -> r.EndsWith ("nunit.framework.dll", StringComparison.InvariantCultureIgnoreCase)
                                        || r.EndsWith ("GuiUnit.exe", StringComparison.InvariantCultureIgnoreCase)) 
            with ex ->
                MonoDevelop.Core.LoggingService.LogInternalError ("FSharpUnitTestTextEditorExtension: GatherUnitTests failed", ex)
                false
        | _ -> false

type FSharpUnitTestTextEditorExtension() =
    inherit AbstractUnitTestTextEditorExtension()

    let emptyResult = ResizeArray<UnitTestLocation>() :> IList<_>
    override x.GatherUnitTests (unitTestMarkers, cancellationToken) =
        if x.DocumentContext = null || 
            x.DocumentContext.ParsedDocument = null || 
            not (unitTestGatherer.hasNUnitReference x.DocumentContext.Project) then
                Threading.Tasks.Task.FromResult emptyResult
        else
            async {
                match x.DocumentContext.TryGetAst() with
                | Some ast ->
                    let! symbols = ast.GetAllUsesOfAllSymbolsInFile()
                    return unitTestGatherer.gatherUnitTests (unitTestMarkers, x.Editor, symbols) :> IList<_>
                | None -> return emptyResult 
            }
            |> StartAsyncAsTask cancellationToken
                
module nunitSourceCodeLocationFinder =
    let tryFindTest fixtureNamespace fixtureTypeName testName (topmostEntity:FSharpEntity) = 
        let matchesType (ent:FSharpEntity) =
            let matchesNamespace() = 
                match topmostEntity.Namespace with
                | Some ns -> ns = fixtureNamespace
                | None -> fixtureNamespace = null

            ent.DisplayName = fixtureTypeName && matchesNamespace()

        let rec getEntityAndNestedEntities (entity:FSharpEntity) =
            seq { yield entity
                  for child in entity.NestedEntities do
                      yield! getEntityAndNestedEntities child }

        getEntityAndNestedEntities topmostEntity
        |> Seq.filter matchesType
        |> Seq.collect (fun e -> e.MembersFunctionsAndValues)
        |> Seq.tryFind (fun m -> m.CompiledName = testName)

open nunitSourceCodeLocationFinder

type FSharpNUnitSourceCodeLocationFinder() =
    inherit NUnitSourceCodeLocationFinder()

    override x.GetSourceCodeLocationAsync(_project, fixtureNamespace, fixtureTypeName, testName, token) =
        let tryFindTest' = tryFindTest fixtureNamespace fixtureTypeName testName 
        async {
                let symbol = 
                    Search.getAllFSharpProjects()
                    |> Seq.filter unitTestGatherer.hasNUnitReference
                    |> Seq.map languageService.GetCachedProjectCheckResult
                    |> Seq.choose id
                    |> Seq.filter (fun c -> not c.HasCriticalErrors)
                    |> Seq.collect (fun c -> c.AssemblySignature.Entities)
                    |> Seq.tryPick tryFindTest'

                match symbol with
                | Some sym ->
                    let location = sym.ImplementationLocation
                    match location with
                    | Some loc ->
                        return SourceCodeLocation(loc.FileName, loc.StartLine, loc.StartColumn + 1)
                    | _ -> return null
                | _ -> return null //?
            } 
        |> StartAsyncAsTask token
       