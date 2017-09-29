namespace MonoDevelopTests
open System
open FsUnit
open MonoDevelop
open MonoDevelop.FSharp
open MonoDevelop.FSharp.MonoDevelop
open MonoDevelop.UnitTesting
open NUnit.Framework
open nunitSourceCodeLocationFinder

[<TestFixture>]
module ``Source code location finder`` =
    let findTest (source: string) fixtureNamespace fixtureTypeName testName =
        let offset = source.IndexOf("$")
        let source = source.Replace("$", "")

        let doc = TestHelpers.createDoc source ""
        let line, col, lineStr = doc.Editor.GetLineInfoFromOffset offset

        let symbolUse = doc.Ast.GetSymbolAtLocation(line, col - 1, lineStr) |> Async.RunSynchronously
        match symbolUse with
        | Some symbolUse' ->
            match symbolUse' with
            | Entity entity ->
                match tryFindTest fixtureNamespace fixtureTypeName testName entity with
                | Some _ -> Assert.Pass()
                | None -> failwith "No test found in entity"
            | _ -> failwith "No entity found at location"
        | _ -> failwith "No symbol found at location"

    [<Test>]
    let ``Can find test without namespace``() =
        let source =
            """
            module myT$ests =
                [<Test>] let myTest() = ()
            """
        findTest source null "myTests" "myTest"

    [<Test>]
    let ``Can find test with namespace``() =
        let source =
            """
            namespace TestNamespace
            module myT$ests =
                [<Test>] let myTest() = ()
            """
        findTest source "TestNamespace" "myTests" "myTest"

    [<Test>]
    let ``Can find test in nested module``() =
        let source =
            """
            namespace TestNamespace
            module par$ent =
              module myTests =
                [<Test>] let myTest() = ()
            """
        findTest source "TestNamespace" "myTests" "myTest"

    [<Test>]
    let ``Can find test in deeply nested module``() =
        let source =
            """
            namespace TestNamespace
            module grand$parent =
              module parent =
                module myTests =
                  [<Test>] let myTest() = ()
            """
        findTest source "TestNamespace" "myTests" "myTest"

[<TestFixture>]
type FSharpUnitTestTextEditorExtensionTests() =
    let gatherTests (text:string) =
        let editor = TestHelpers.createDoc text ""
        let ast = editor.Ast
        let symbols = ast.GetAllUsesOfAllSymbolsInFile() |> Async.RunSynchronously

        let markers =
            [|{ new IUnitTestMarkers
                with
                    member x.TestMethodAttributeMarker = "NUnit.Framework.TestAttribute"
                    member x.TestCaseMethodAttributeMarker = "NUnit.Framework.TestCaseAttribute"
                    member x.IgnoreTestMethodAttributeMarker = "NUnit.Framework.IgnoreAttribute"
                    member x.IgnoreTestClassAttributeMarker = "NUnit.Framework.IgnoreAttribute" }|]

        unitTestGatherer.gatherUnitTests (markers, editor.Editor, symbols)
        |> Seq.toList

    let gatherTestsWithReference (text:string) =
        let attributes = """
namespace NUnit.Framework
open System
type TestAttribute =
  inherit Attribute
  new() = { inherit Attribute() }
  new(name) = { inherit Attribute() }
type TestFixtureAttribute() =
  inherit Attribute()
type IgnoreAttribute() =
  inherit Attribute()
type TestCaseAttribute =
  inherit TestAttribute
  new() = { inherit Attribute() }
  new(name) = { inherit Attribute() }
"""
        gatherTests (attributes + text)

    [<Test>]
    member x.BasicTestCoveringNormalAndDoubleQuotedTestsInATestFixture () =
        let normalAndDoubleTick = """
open System
open NUnit.Framework
[<TestFixture>]
type Test() =
    [<Test>]
    member x.TestOne() = ()

    [<Test>]
    [<Ignore>]
    member x.``Test Two``() = ()
"""
        let res = gatherTestsWithReference normalAndDoubleTick
        match res with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "NUnit.Framework.Test"

            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test.TestOne"
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "NUnit.Framework.Test.Test Two"
            t2.IsIgnored |> should equal true
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"

    [<Test>]
    member x.NoTests () =
        let noTests = """
open System
open NUnit.Framework

type Test() =
    member x.TestOne() = ()
"""

        let tests = gatherTestsWithReference noTests
        tests.Length |> should equal 0
    
    [<Test>]
    member x.``Module tests without TestFixtureAttribute are detected`` () =
        let noTests = """
module someModule =

  open NUnit.Framework

  [<Test>]
  let atest () =
      ()
"""

        let tests = gatherTestsWithReference noTests
        tests.Length |> should equal 2

    [<Test>]
    member x.NestedTestCoveringNormalAndDoubleQuotedTestsInATestFixture () =
        let nestedTests = """
open System
open NUnit.Framework
module Test =
    [<TestFixture>]
    type Test() =
        [<Test>]
        member x.TestOne() = ()

        [<Test>]
        [<Ignore>]
        member x.``Test Two``() = ()
"""
        let tests = gatherTestsWithReference nestedTests

        match tests with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test"

            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.TestOne"
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.Test Two"
            t2.IsIgnored |> should equal true
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"

    [<Test>]
    member x.TestsPresentButNoNUnitReference () =
        let normalAndDoubleTick = """
open System
open NUnit.Framework
[<TestFixture>]
type Test() =
    [<Test>]
    member x.TestOne() = ()

    [<Test>]
    [<Ignore>]
    member x.``Test Two``() = ()
"""
        let tests = gatherTests normalAndDoubleTick

        tests.Length |> should equal 0

    [<Test>]
    member x.``Test cases`` () =
        let nestedTests = """
open System
open NUnit.Framework
module Test =
    [<TestFixture>]
    type Test() =
        [<TestCase("a string")>]
        member x.TestOne(s:string) = ()
        """
        let tests = gatherTestsWithReference nestedTests

        match tests with
        | [fixture;t1] -> 
            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.TestOne"
            t1.IsIgnored |> should equal false
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"
  