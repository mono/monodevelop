namespace MonoDevelop.FSharp
open MonoDevelop.Ide.Gui
open MonoDevelop.CodeActions
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Projects
open ICSharpCode.NRefactory
open System.Threading
open ICSharpCode.NRefactory.Refactoring
open FSharp.CompilerBinding
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Range
open Mono.TextEditor

type FSharpRefactoringContext() = 
   interface IRefactoringContext with   
      override x.CreateScript () = { 
         new System.IDisposable with 
           member this.Dispose() = ()
        }

/// <summary>
/// A code action represents a menu entry that does edit operation in one document.
/// </summary>
type ImplementInterfaceCodeAction(doc:TextDocument, interfaceData: InterfaceData, fsSymbolUse:FSharpSymbolUse, lineStr) as x =
  inherit  CodeAction()
  do 
    x.Title    <- "Implement Interface"
    x.IdString <- "ImplementInterfaceCodeAction"

  /// Find the indent level, and the column to insert ' with' at, if any 
  let getIndentAndWithColumn() =
    let sourceTok = SourceTokenizer([], "C:\\test.fsx")
    let tokenizer = sourceTok.CreateLineTokenizer(lineStr)
    let tokens = Seq.unfold (fun s -> match tokenizer.ScanToken(s) with
                                      | Some t, s -> Some(t,s)
                                      | _         -> None) 0L |> Array.ofSeq
    let startCol = interfaceData.Range.StartColumn
    let indentCol = 
       match interfaceData with
       | InterfaceData.Interface _ -> (doc.GetLineIndent fsSymbolUse.RangeAlternate.StartLine).Length
       | InterfaceData.ObjExpr _   -> 
          tokens |> Array.tryPick (fun (t: TokenInformation) ->
                  if t.CharClass = TokenCharKind.Keyword && 
                     t.LeftColumn < startCol &&
                     t.TokenName = "NEW" then Some t.LeftColumn else None) 
                 |> Option.getOrElse startCol
    let hasWith = 
        tokens |> Array.tryPick (fun (t: TokenInformation) ->
                  if t.CharClass = TokenCharKind.Keyword && 
                     t.LeftColumn >= startCol &&
                     t.TokenName = "WITH" then Some() else None)
    let withCol = if hasWith.IsSome then None else Some interfaceData.Range.EndColumn
    indentCol, withCol
    
  override x.Run (context: IRefactoringContext, script:obj) = 
     let line = fsSymbolUse.RangeAlternate.StartLine
     let indent = 3
     let startindent, withCol = getIndentAndWithColumn()
     let e = fsSymbolUse.Symbol :?> FSharpEntity
     
     let formatted = InterfaceStubGenerator.formatInterface (startindent + indent) indent interfaceData.TypeParameters "x" "raise (System.NotImplementedException())" fsSymbolUse.DisplayContext e
     match withCol with
     | Some p -> doc.Insert(doc.GetLine(line).Offset + p, " with")
     | _ -> ()
     let insertpoint = doc.GetLine(line).NextLine.Offset
     doc.Replace(insertpoint, 0, formatted)
     ()

/// <summary>
/// A code action provider is a factory that creates code actions for a document at a given location.
/// Maybe best to have one of these for each refactoring
/// </summary>
type ImplementInterfaceCodeActionProvider() as x =
  inherit CodeActionProvider()
  do 
    x.MimeType    <- "text/x-fsharp"
    x.Category    <- "Test" // TODO: These are for preferences, but these actions don't show up there yet. Find out why. 
    x.Title       <- "Implement Interface category"
    x.Description <- "Implement this interface"
  override x.IdString = "ImplementInterfaceCodeActionProvider" 

  override x.GetActions(doc: Document, ctx: obj, location: TextLocation, cancellation: CancellationToken) = 
    let projectFilename, files, args, framework = MonoDevelop.getCheckerArgsFromProject(doc.Project :?> DotNetProject, IdeApp.Workspace.ActiveConfiguration)
    if doc.ParsedDocument <> null then
      match doc.ParsedDocument.Ast with
        | :? ParseAndCheckResults as ast -> seq {
            match ast.ParseTree with 
            | Some parseTree ->
              let lineStr = doc.Editor.GetLineText(location.Line)
              let pos = mkPos location.Line location.Column
              let interfaceData = InterfaceStubGenerator.tryFindInterfaceDeclaration pos parseTree
              let symbol = ast.GetSymbol(location.Line, location.Column, lineStr) |> Async.RunSynchronously
            
              match interfaceData, symbol with 
              | Some iface, Some sy -> 
                 match sy.Symbol with
                 | :? FSharpEntity as e when e.IsInterface ->
                    yield ImplementInterfaceCodeAction(doc.Editor.Document, iface, sy, lineStr) :> _
                 | _ -> ()
              | _ -> ()
            | _ -> ()
          }
        | _ -> Seq.empty
     else Seq.empty
     
type NRefactoryCodeActionSource() = 
  interface ICodeActionProviderSource with
    member x.GetProviders() = seq {
      yield ImplementInterfaceCodeActionProvider() :> _
    }
