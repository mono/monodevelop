namespace MonoDevelopTests
open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger

[<TestFixture>]
type DebuggerExpressionResolver() =
    inherit TestBase()

    let content = """type TestOne() =
    member val PropertyOne = "42" with get, set
    member x.FunctionOne(parameter) = ()

let localOne = TestOne()
let localTwo = localOne.PropertyOne"""

    let getBasicOffset expr =
        let startOffset = content.IndexOf (expr, StringComparison.Ordinal)
        startOffset + (expr.Length / 2)

    let resolveExpression (doc:Document, content:string, offset:int) =
        let resolvers = doc.GetContents<FSharpTextEditorCompletion> () |> Seq.toArray
        let resolver = (resolvers.[0] :> IDebuggerExpressionResolver)
        Async.AwaitTask (resolver.ResolveExpressionAsync(doc.Editor, doc,offset, Async.DefaultCancellationToken))
        |> Async.RunSynchronously

    [<Test>]
    [<TestCase("localOne")>]
    [<TestCase("localTwo")>]
    member x.TestBasicLocalVariable(localVariable) =
        let basicOffset = getBasicOffset (localVariable)
        let doc, _viewContent = TestHelpers.createDoc content [] ""
        let debugDataTipInfo = resolveExpression (doc, content, basicOffset)
        debugDataTipInfo.Text |> should equal localVariable