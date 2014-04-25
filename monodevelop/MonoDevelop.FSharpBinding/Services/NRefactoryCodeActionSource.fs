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

type FSharpRefactoringContext() = 
   interface IRefactoringContext with   
      override x.CreateScript () = { 
         new System.IDisposable with 
           member this.Dispose() = ()
        }


/// <summary>
/// A code action represents a menu entry that does edit operation in one document.
/// </summary>
type ImplementInterfaceCodeAction() as x =
  inherit  CodeAction()
  do 
    x.Title    <- "Implement Interface"
    x.IdString <- "ImplementInterfaceCodeAction"
  override y.Run (context: IRefactoringContext, script:obj) = ()

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
  override y.IdString = "ImplementInterfaceCodeActionProvider" 

  override y.GetActions(doc: Document, ctx: obj, location: TextLocation, cancellation: CancellationToken) = 
    let projectFilename, files, args, framework = MonoDevelop.getCheckerArgsFromProject(doc.Project :?> DotNetProject, IdeApp.Workspace.ActiveConfiguration)
    if doc.ParsedDocument <> null then
      match doc.ParsedDocument.Ast with
        | :? ParseAndCheckResults as ast -> seq {
            let currentFile = doc.FileName.ToString()
            
            let lineStr = doc.Editor.GetLineText(location.Line)
            let symbol = ast.GetSymbol(location.Line, location.Column, lineStr) |> Async.RunSynchronously
            match symbol with 
            | Some sy -> 
               match sy.Symbol with
               | :? FSharpEntity as e when e.IsInterface ->
                   //TODO: Check if completely implemented -> no command
                   yield ImplementInterfaceCodeAction() :> _
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
