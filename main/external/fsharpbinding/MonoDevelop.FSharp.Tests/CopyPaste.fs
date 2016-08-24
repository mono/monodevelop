namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open MonoDevelop.Ide.Editor
open FsUnit

[<TestFixture>]
type ``Copy and Paste tests``() =
    let assertPaste (input:string) (expected:string) =
        let offset = input.IndexOf("$")
        let length = input.LastIndexOf("$") - offset - 1
        let pastePosition = input.IndexOf("PASTE_HERE") - 2
        let input = input.Replace("$", "").Replace("PASTE_HERE", "")
        let copyText = input.[offset..offset+length-1]
        let doc = TestHelpers.createDocWithoutParsing input "defined"
        let pasteHandler = FSharpTextPasteHandler doc.Editor
        let copyData = pasteHandler.GetCopyData(offset, length)
        let pasteText = pasteHandler.FormatPlainText(pastePosition, copyText, copyData)
        pasteText |> shouldEqualIgnoringLineEndings expected

    [<Test>]
    member x.``Plain copy paste``() =
        let input =
            """
            $let f =
              let a = 3
              ()$
            PASTE_HERE
            """

        let expected = 
            """
            let f =
              let a = 3
              ()
            """.Trim()

        assertPaste input expected

    [<Test>]
    member x.``Indented copy paste``() =
        let input =
            """
            $let f =
              let a = 3
              ()$
              PASTE_HERE
            """

        let expected = 
            """
              let f =
                let a = 3
                ()
            """.Trim()

        assertPaste input expected

    [<Test>]
    member x.``Copy blank line paste``() =
        let input =
            "$\n" +
            "  let f =\n" +
            "    let a = 3\n" +
            "    ()$\n" +
            "  PASTE_HERE"

        let expected = 
            "\n" + 
            "  let f =\n" +
            "    let a = 3\n" +
            "    ()"

        assertPaste input expected

    [<Test>]
    member x.``Copy indented blank line paste``() =
        let input =
            "$\n" +
            "  let f =\n" +
            "    let a = 3\n" +
            "    ()$\n" +
            "    PASTE_HERE"

        let expected = 
            "\n" + 
            "    let f =\n" +
            "      let a = 3\n" +
            "      ()"

        assertPaste input expected