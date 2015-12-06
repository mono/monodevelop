namespace MonoDevelopTests
open System
open MonoDevelop.Ide.Editor
open MonoDevelop.Projects
open System.Threading.Tasks

type TestDocument(name, parsedDocument, editor: TextEditor, references) =
  inherit DocumentContext()
    
    let project = Services.ProjectService.CreateDotNetProject ("F#")
      do
        project.References.AddRange references

      override x.Name = name
      override x.Project = project :> Project
      override x.AnalysisDocument = null
      override x.ParsedDocument = parsedDocument
      override x.AttachToProject(_) = ()
      override x.ReparseDocument() = ()
      override x.GetOptionSet() = null
      override x.UpdateParseDocument() = Task.FromResult parsedDocument
      member x.Editor = editor
