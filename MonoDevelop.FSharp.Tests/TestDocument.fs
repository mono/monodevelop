namespace MonoDevelopTests
open System
open MonoDevelop.Ide.Editor
open MonoDevelop.Projects
open System.Threading.Tasks

type TestDocument(name, parsedDocument, editor: TextEditor) =
  inherit DocumentContext()

    override x.Name = name
    override x.Project = null
    override x.AnalysisDocument = null
    override x.ParsedDocument = parsedDocument
    override x.AttachToProject(_) = ()
    override x.ReparseDocument() = ()
    override x.GetOptionSet() = null
    override x.UpdateParseDocument() = Task.FromResult parsedDocument
    member x.Editor = editor
