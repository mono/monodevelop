namespace MonoDevelopTests
open System
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
type DebuggerExpressionResolver() =
    inherit TestBase()
    let mutable doc = Unchecked.defaultof<Document>

    let content = """type TestOne() =
    member val PropertyOne = "42" with get, set
    member x.FunctionOne(parameter) = ()

let localOne = TestOne()
let localTwo = localOne.PropertyOne"""

    let getBasicOffset expr =
        let startOffset = content.IndexOf (expr, StringComparison.Ordinal)
        startOffset + (expr.Length / 2)

    let resolveExpression (doc:Document, content:string, offset:int) =
        let resolver = doc.GetContents<FSharpTextEditorCompletion> () |> Seq.toArray
        (resolver.[0] :> IDebuggerExpressionResolver).ResolveExpression(doc.Editor, doc,offset)

    [<TestFixtureSetUp>]
    override x.Setup() =
        base.Setup()
        doc <- fst (TestHelpers.createDoc(content) [])

    
    [<Test>]
    [<TestCase("localOne")>]
    [<TestCase("localTwo")>]
    member x.TestBasicLocalVariable(localVariable) =
        let basicOffset = getBasicOffset (localVariable)
        let expression, offset = resolveExpression (doc, content, basicOffset)
        expression |> should equal localVariable

   

