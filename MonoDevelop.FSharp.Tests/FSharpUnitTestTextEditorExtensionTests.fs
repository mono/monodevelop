namespace MonoDevelopTests
open System
open NUnit.Framework
open MonoDevelop.FSharp
open FsUnit

[<TestFixture>]
type FSharpUnitTestTextEditorExtensionTests() =
    let createDoc (text:string) =
        let doc = TestHelpers.createDoc(text) [] ""
        let test = new FSharpUnitTestTextEditorExtension()
        test.Initialize (doc.Editor, doc)
        test

    let createDocWithReference (text:string) =
        let attributes = """
namespace NUnit.Framework
open System
type TestAttribute() =
  inherit Attribute()
type TestFixtureAttribute() =
  inherit Attribute()
type IgnoreAttribute() =
  inherit Attribute()
type TestCaseAttribute() =
  inherit Attribute()
"""
        createDoc (attributes + text)

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
        let testExtension = createDocWithReference normalAndDoubleTick
        let res = testExtension.GatherUnitTests (Async.DefaultCancellationToken)
                  |> Async.AwaitTask
                  |> Async.RunSynchronously
                  |> Seq.toList
        match res with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "NUnit.Framework.Test"

            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test.TestOne"
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "NUnit.Framework.Test.``Test Two``"
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

        let testExtension = createDocWithReference noTests
        let tests = testExtension.GatherUnitTests(Async.DefaultCancellationToken)
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
        tests.Count |> should equal 0

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
        let testExtension = createDocWithReference nestedTests

        match testExtension.GatherUnitTests(Async.DefaultCancellationToken)
              |> Async.AwaitTask
              |> Async.RunSynchronously
              |> Seq.toList with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test"

            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.TestOne"
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.``Test Two``"
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
        let testExtension = createDoc normalAndDoubleTick
        let tests = testExtension.GatherUnitTests(Async.DefaultCancellationToken)
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
        tests.Count |> should equal 0