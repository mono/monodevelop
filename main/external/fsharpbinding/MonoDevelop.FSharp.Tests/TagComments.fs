namespace MonoDevelopTests

open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp
open System.Threading

[<TestFixture>]
module ``Tag comment tests`` =
    let getComments (input:string) =

        let doc = TestHelpers.createDoc input ""
        let parsedDoc = doc.ParsedDocument :?> FSharpParsedDocument
        let tags =
            parsedDoc.GetTagCommentsAsync(CancellationToken.None)
            |> Async.AwaitTask |> Async.RunSynchronously
        printf "%A" tags
        tags

    [<Test>]
    let ``should find tag comments``() =
        let source =
            """
            //TODO: testing 123
            let x = 1 //  FIXME: testing abc
            // nothing special here
            """
        let result = getComments source
        result.Count |> should equal 2
        result.[0].Key |> should equal "TODO"
        result.[0].Text |> should equal "TODO: testing 123"

        result.[1].Key |> should equal "FIXME"
        result.[1].Text |> should equal "FIXME: testing abc"