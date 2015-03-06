namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open FSharp.CompilerBinding
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger
open MonoDevelopTests

module TestHelpers =

    let createDoc (text:string) references =
        let tww = TestWorkbenchWindow()
        let content = new TestViewContent()
        tww.ViewContent <- content
        content.ContentName <- "/a.fs"
        content.Data.MimeType <- "text/x-fsharp"

        let endPos = text.IndexOf ('$')
        let text = 
            if endPos > 0 then text.Substring (0, endPos) + text.Substring (endPos + 1)
            else text

        let project = new DotNetAssemblyProject ("F#", Name="test", FileName = FilePath("test.fsproj"))
        project.References.AddRange references
        let projectConfig = project.AddNewConfiguration("Debug")

        use solution = new MonoDevelop.Projects.Solution ()
        solution.AddConfiguration ("", true) |> ignore
        solution.DefaultSolutionFolder.AddItem (project)
        using ( new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ())
            (fun monitor -> MonoDevelop.Ide.TypeSystem.TypeSystemService.Load (solution, monitor) |> ignore)

        content.Project <- project

        content.Text <- text
        content.CursorPosition <- max 0 endPos
        let doc = Document(tww)

        let pfile = doc.Project.AddFile("a.fs")

        let compExt = new FSharpTextEditorCompletion()
        compExt.Initialize(doc.Editor, doc)
        content.Contents.Add(compExt)

        try
            try 
                doc.UpdateParseDocument() |> ignore
            with exn -> Diagnostics.Debug.WriteLine(exn.ToString())
        finally
            MonoDevelop.Ide.TypeSystem.TypeSystemService.Unload solution
        doc, content