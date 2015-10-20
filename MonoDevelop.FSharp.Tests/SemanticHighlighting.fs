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
open Reflection

[<TestFixture>]
type SemanticHighlighting() = 
  inherit TestBase()
  
  let getStyle (content : string) = 
    let fixedc = content.Replace("§", "")
    let doc, _viewContent = TestHelpers.createDoc fixedc [] "defined"
    let syntaxMode = new FSharpSyntaxMode(doc.Editor, doc)
    //let parsed = doc.UpdateParseDocument()
    let segments = 
      syntaxMode.GetProcessedTokens().Value
      |> Seq.collect (fun s -> s)
      |> Seq.distinct
      |> Seq.sortBy (fun s -> s.Offset)

    segments 
    |> Seq.iter 
         (fun seg -> 
         printf """Segment: %s S:%i E:%i L:%i - "%s" %s""" seg.ColorStyleKey seg.Offset seg.EndOffset seg.Length 
           (doc.Editor.GetTextBetween(seg.Offset, seg.EndOffset)) Environment.NewLine)

    let offset = content.IndexOf("§")
    let endOffset = content.LastIndexOf("§") - 1
    let segment = segments |> Seq.tryFind (fun s -> s.Offset = offset && s.EndOffset = endOffset)
    match segment with
    | Some(s) -> s.ColorStyleKey
    | _ -> "segment not found"
  
  [<Test>]
  member x.If_is_preprocessor() = 
    let content = 
        """§#if§ undefined
        let add = (+)
        #endif
        """
    getStyle content |> should equal "Preprocessor"
  
  [<Test>]
  member x.Test_is_plain_text() = 
    let content = 
        """#if §undefined§
        let add = (+)
        #endif
        """
    getStyle content |> should equal "Plain Text"
  
  [<Test>]
  member x.Ifdeffed_code_is_excluded() = 
    let content = 
        """#if undefined
        §let§ add = (+)
        #endif
        """
    getStyle content |> should equal "Excluded Code"
  
  [<Test>]
  member x.Endif_is_preprocessor() = 
    let content = 
        """#if undefined
        let add = (+)
        §#endif§
        """
    getStyle content |> should equal "Preprocessor"
  
  [<Test>]
  member x.Let_is_keyword() = 
    let content = 
        """#if defined
        §let§ add = (+)
        #endif
        """
    getStyle content |> should equal "Keyword(Type)"
  
  [<Test>]
  [<Ignore>]
  member x.Module_is_highlighted() = 
    let content = """
                module MyModule() = 
                    let someFunc() = ()
                
                module Consumer = 
                    §MyModule§.someFunc()
                """
    getStyle content |> should equal "?????"
  
  [<Test>]
  [<Ignore>]
  member x.Type_is_highlighted() = 
    let content = """
                open System
                 
                module MyModule = 
                    let guid = §Guid§.NewGuid()
                """
    getStyle content |> should equal "?????"
  
  [<Test>]
  member x.Add_is_plain_text() = 
    let content = "let §add§ = (+)"
    getStyle content |> should equal "Plain Text"
  
  [<TestCase("let add = (§+§)", "Punctuation")>]
  [<TestCase("let §add§ = (+)", "Plain Text")>]
  [<TestCase("let add = §(§+)", "Punctuation(Brackets)")>]
  [<TestCase("let §simpleBinding§ = 1", "Plain Text")>]
  [<TestCase("let simpleBinding = §1§", "Number")>]
  member x.Semantic_highlighting(source, expectedStyle) = 
    printf "%s" source
    getStyle source |> should equal expectedStyle
