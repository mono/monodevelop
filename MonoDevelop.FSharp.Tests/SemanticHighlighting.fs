namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Highlighting
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger
open Reflection

[<TestFixture>]
type SemanticHighlighting() =
    inherit TestBase()

    let content = """#if test
let add = (+)
#endif
let simpleBinding = 42
"""

    let lineToSegments (syntaxMode:FSharpSyntaxMode) (line:IDocumentLine) =
        syntaxMode.GetColoredSegments line
        |> Seq.toList

    let simplifySegments (segments:ColoredSegment list) =
        segments |> List.map (fun s -> s.Offset, s.EndOffset, s.Length, s.ColorStyleKey)

    [<Test>]  
    member x.InactiveRegions() =
        let doc, _viewContent = TestHelpers.createDoc content [] ""
        use syntaxMode = new FSharpSyntaxMode(doc.Editor, doc)
        syntaxMode?DocumentParsed()
        let segments =
            [1..4]
            |> List.map (doc.Editor.GetLine >> lineToSegments syntaxMode >> simplifySegments)

        segments
        |> shouldEqual
           [[(0, 3, 3, "Preprocessor"); (3, 4, 1, "Plain Text"); (4, 8, 4, "Plain Text")]
            [(9, 22, 13, "Excluded Code")]
            [(23, 29, 6, "Preprocessor")]
            [(30, 33, 3, "Keyword(Type)"); (33, 34, 1, "Plain Text"); (34, 47, 13, "User Field Declaration"); (47, 48, 1, "Plain Text"); (48, 49, 1, "Punctuation"); (49, 50, 1, "Plain Text"); (50, 52, 2, "Number")]]
   
    [<Test>]  
    member x.ActiveRegions() =
        let doc, _viewContent = TestHelpers.createDoc content [] "test"
        use syntaxMode = new FSharpSyntaxMode(doc.Editor, doc)
        syntaxMode?DocumentParsed()
        let segments =
            [1..4]
            |> List.map (doc.Editor.GetLine >> lineToSegments syntaxMode >> simplifySegments)

        segments
        |> shouldEqual
            [[(0, 3, 3, "Preprocessor"); (3, 4, 1, "Plain Text"); (4, 8, 4, "Plain Text")];
             [(9, 12, 3, "Keyword(Type)"); (12, 13, 1, "Plain Text"); (13, 16, 3, "User Method Declaration"); (16, 17, 1, "Plain Text"); (17, 18, 1, "Punctuation"); (18, 19, 1, "Plain Text"); (19, 20, 1, "Punctuation(Brackets)"); (20, 21, 1, "Punctuation"); (21, 22, 1, "Punctuation(Brackets)")]
             [(23, 29, 6, "Preprocessor")]
             [(30, 33, 3, "Keyword(Type)"); (33, 34, 1, "Plain Text"); (34, 47, 13, "User Field Declaration"); (47, 48, 1, "Plain Text"); (48, 49, 1, "Punctuation"); (49, 50, 1, "Plain Text"); (50, 52, 2, "Number")]]