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
type ImplementInterfaceCodeAction(doc:TextDocument, interfaceData: InterfaceData, fsSymbolUse:FSharpSymbolUse, lineStr, tyRes:ParseAndCheckResults, indentSize) as x =
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
          let foundToken =
              tokens
              |> Array.tryPick (fun t -> if t.CharClass = FSharpTokenCharKind.Keyword && t.LeftColumn < startCol && t.TokenName = "NEW"
                                         then Some t.LeftColumn else None) 

          match foundToken with
          | Some s -> s
          | None -> startCol

    let hasWith = 
        tokens |> Array.tryPick (fun (t: FSharpTokenInfo) ->
                  if t.CharClass = FSharpTokenCharKind.Keyword && 
                     t.LeftColumn >= startCol &&
                     t.TokenName = "WITH" then Some() else None)
    let withCol = if hasWith.IsSome then None else Some interfaceData.Range.EndColumn
    indentCol, withCol
    
  override x.Run (_context: IRefactoringContext, _script:obj) = 
     let line = fsSymbolUse.RangeAlternate.StartLine

     let startindent, withCol = getIndentAndWithColumn()
     let e = fsSymbolUse.Symbol :?> FSharpEntity

     let getMemberByLocation(name, range: range) =
       let lineStr = 
           doc.GetLineText(range.StartLine)
       tyRes.GetSymbolAtLocation(range.StartLine, range.EndColumn, lineStr, [name])
     let implementedMemberSignatures = InterfaceStubGenerator.getImplementedMemberSignatures getMemberByLocation fsSymbolUse.DisplayContext interfaceData
                                       |> Async.RunSynchronously
     let formatted = InterfaceStubGenerator.formatInterface (startindent + indentSize) indentSize interfaceData.TypeParameters "x" "raise (System.NotImplementedException())" fsSymbolUse.DisplayContext implementedMemberSignatures e
     let docLine = doc.GetLine(line)
     match withCol with
     | Some p -> doc.Insert(docLine.Offset + p, " with")
     | _ -> ()
     // Trim initial spaces here to keep InteraceStubGenerator easily diffable to VFPT
     let trimmed = match formatted.IndexOfAny ([|'\r';'\n'|]) with
                   | x when x > 0 -> formatted.Substring(x)
                   | _            -> formatted
     let insertpoint =  docLine.EndOffset
     doc.Insert(insertpoint, trimmed)

/// A code action provider is a factory that creates code actions for a document at a given location.
type ImplementInterfaceCodeActionProvider() as x =
  inherit CodeActionProvider()
  do 
    x.MimeType    <- "text/x-fsharp"
    x.Category    <- "Refactoring"
    x.Title       <- "Implement Interface category"
    x.Description <- "Implement this interface"
  override x.IdString = "ImplementInterfaceCodeActionProvider" 

  override x.GetActions(doc: Document, _ctx: obj, location: TextLocation, _cancellation: CancellationToken) = 
    if doc.ParsedDocument <> null then
        match doc.ParsedDocument.Ast with
        | :? ParseAndCheckResults as ast ->
            seq {
                match ast.ParseTree with 
                | Some parseTree ->
                    let pos = mkPos location.Line location.Column
                    let interfaceData = InterfaceStubGenerator.tryFindInterfaceDeclaration pos parseTree
                    match interfaceData with 
                    | Some iface ->
                        let lineStr = doc.Editor.GetLineText(location.Line)
                        let symbol = ast.GetSymbol(location.Line, location.Column, lineStr) |> Async.RunSynchronously
                        match symbol with
                        | Some sy ->
                            match sy.Symbol with
                            | :? FSharpEntity as e when e.IsInterface ->
                                 yield ImplementInterfaceCodeAction(doc.Editor.Document, iface, sy, lineStr, ast, doc.Editor.Options.IndentationSize) :> _
                            | _ -> ()
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
