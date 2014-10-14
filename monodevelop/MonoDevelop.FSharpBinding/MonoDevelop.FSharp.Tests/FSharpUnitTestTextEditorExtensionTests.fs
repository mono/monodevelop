namespace MonoDevelopTests
open System
open System.IO
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open FSharp.CompilerBinding
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger

[<TestFixture>]
type FSharpUnitTestTextEditorExtensionTests() =
    inherit TestBase()
    let (++) a b= Path.Combine (a,b)

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

    let noTests = """
open System
open NUnit.Framework

type Test() =
    member x.TestOne() = ()
"""

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

    let nunitRef =
        ProjectReference (ReferenceType.Assembly, __SOURCE_DIRECTORY__ ++ @"../packages/NUnit.2.6.2/lib/nunit.framework.dll" )

    let createDoc (text:string) references =
        let doc,viewContent = TestHelpers.createDoc(text) references
        let test = new FSharpUnitTestTextEditorExtension()
        test.Initialize (doc)
        viewContent.Contents.Add (test)

        try doc.UpdateParseDocument() |> ignore
        with exn -> Diagnostics.Debug.WriteLine(exn.ToString())
        test

    [<TestFixtureSetUp>]
    override x.Setup() =
        base.Setup()
  
    [<Test>]
    member x.``Basic Test covering normal and double quoted tests in a test fixture`` () =
        let testExtension = createDoc normalAndDoubleTick [nunitRef]
        let res = testExtension.GatherUnitTests() |> Seq.toList
        match res with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "A+Test"
            fixture.LineNumber |> should equal 5

            t1.UnitTestIdentifier |> should equal "A+Test.TestOne"
            t1.LineNumber |> should equal 7
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "A+Test.Test Two"
            t2.LineNumber |> should equal 11
            t2.IsIgnored |> should equal true
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"

    [<Test>]
    member x.``No tests`` () =
        let testExtension = createDoc noTests [nunitRef]
        let tests = testExtension.GatherUnitTests()
        tests.Count |> should equal 0

    [<Test>]
    member x.``Nested Test covering normal and double quoted tests in a test fixture`` () =
        let testExtension = createDoc nestedTests [nunitRef]

        match testExtension.GatherUnitTests() |> Seq.toList with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "A+Test+Test"
            fixture.LineNumber |> should equal 6

            t1.UnitTestIdentifier |> should equal "A+Test+Test.TestOne"
            t1.LineNumber |> should equal 8
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "A+Test+Test.Test Two"
            t2.LineNumber |> should equal 12
            t2.IsIgnored |> should equal true
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"

    [<Test>]
    member x.``Tests present but no NUnit reference`` () =
        let testExtension = createDoc normalAndDoubleTick []
        let tests = testExtension.GatherUnitTests()
        tests.Count |> should equal 0
