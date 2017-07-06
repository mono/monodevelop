namespace MonoDevelopTests
open System
open NUnit.Framework
open MonoDevelop.FSharp
open FsUnit
open MonoDevelop.UnitTesting
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