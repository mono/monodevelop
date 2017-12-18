//namespace test

//module mymodule =
    //open System

namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Ide.Editor

[<TestFixture>]
module ``Add open statement`` =
    let addOpenWithIndent (input:string) openStatement expected indent =
        let offset = input.LastIndexOf "$"
        if offset = -1 then
            failwith "Input must contain a $"
        let input = input.Remove(offset, 1)

        let doc = TestHelpers.createDoc input "defined"
        let editor = doc.Editor
        editor.Options <- new CustomEditorOptions(IndentationSize=indent, IndentStyle=IndentStyle.Smart)
        editor.CaretOffset <- offset
        openStatements.addOpenStatement editor doc.Ast.ParseTree.Value openStatement
        Assert.AreEqual(expected, editor.Text)

    let addOpen (input:string) openStatement expected =
        addOpenWithIndent input openStatement expected 4

    [<Test>]
    let ``add System``() =
        let input =
            """
            namespace test

            module mymodule =
              $
            """

        let expected =
            """
            namespace test

            module mymodule =
                open System
              
            """

        addOpen input "System" expected

    [<Test>]
    let ``add System namespace at top level for type``() =
        let input =
            """
            namespace test

            type myType() =
              $
            """

        let expected =
            """
            namespace test

            open System
            type myType() =
              
            """

        addOpen input "System" expected

    [<Test>]
    let ``add System with indent size 2``() =
        let input =
            """
            namespace test

            module mymodule =
              $
            """

        let expected =
            """
            namespace test

            module mymodule =
              open System
              
            """

        addOpenWithIndent input "System" expected 2

    [<Test>]
    let ``adds to second module``() =
        let input =
            """
            namespace test

            module module1 =
                ()
            module module2 =
              $
            """

        let expected =
            """
            namespace test

            module module1 =
                ()
            module module2 =
                open System
              
            """

        addOpen input "System" expected

    [<Test>]
    let ``add second open``() =
        let input =
            """
            namespace test

            module mymodule =
                open System
              $
            """

        let expected =
            """
            namespace test

            module mymodule =
                open System
                open System.IO
              
            """

        addOpen input "System.IO" expected

    [<Test>]
    let ``add System before IO``() =
        let input =
            """
            namespace test

            module mymodule =
                open System.IO
              $
            """

        let expected =
            """
            namespace test

            module mymodule =
                open System
                open System.IO
              
            """

        addOpen input "System" expected

    [<Test>]
    let ``add IO with non standard indent``() =
        let input =
            "module mymodule =\n  if Fi$"

        let expected =
            "module mymodule =\n  open System.IO\n  if Fi"

        addOpenWithIndent input "System.IO" expected 2
