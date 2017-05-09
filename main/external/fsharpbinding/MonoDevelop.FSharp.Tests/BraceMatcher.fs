namespace MonoDevelopTests

open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp

[<TestFixture>]
module ``Brace matcher tests`` =
    let getMatchingParens (input:string) =
        let offset = input.LastIndexOf "|"
        if offset = -1 then
            failwith "Input must contain a |"
        let input = input.Remove(offset, 1)
        let doc = TestHelpers.createDoc input ""
        let editor = doc.Editor
        editor.CaretOffset <- offset-1
        let res =
            braceMatcher.getMatchingBraces doc.Editor doc (offset-1)
            |> Async.RunSynchronously
        res.Value

    [<Test>]
    let ``should find matching left parens``() =
        let source = "let square (x: int)| = x * x"
        let result = getMatchingParens source
        result.IsCaretInLeft |> should equal false

    [<Test>]
    let ``should find matching right parens``() =
        let source = "let square (|x: int) = x * x"
        let result = getMatchingParens source
        result.IsCaretInLeft |> should equal true
