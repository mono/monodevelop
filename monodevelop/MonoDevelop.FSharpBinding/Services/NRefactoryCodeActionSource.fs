namespace MonoDevelop.FSharp
open MonoDevelop.Ide.Gui
open MonoDevelop.CodeActions
open MonoDevelop.Ide.TypeSystem
open ICSharpCode.NRefactory
open System.Threading
open ICSharpCode.NRefactory.Refactoring

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
  override y.GetActions(doc: Document, ctx: obj, location: TextLocation, cancellation: CancellationToken) = seq {
    // TODO: Look at location, and decide whether it's an interface. Only return the action in that case
    yield ImplementInterfaceCodeAction() :> _
  }

type NRefactoryCodeActionSource() = 
  interface ICodeActionProviderSource with
    member x.GetProviders() = seq {
      yield ImplementInterfaceCodeActionProvider() :> _
    }
