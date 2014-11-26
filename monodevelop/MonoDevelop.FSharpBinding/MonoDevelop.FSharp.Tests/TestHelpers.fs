module TestHelpers

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

let createDoc (text:string) references =
        let workbenchWindow = TestWorkbenchWindow()
        let viewContent = new TestViewContent()
        let filePath = match Platform.IsWindows with
                       | true -> FilePath(@"C:\Temp\test.fsproj")
                       | _ -> FilePath("test.fsproj")
        let project = new DotNetAssemblyProject ("F#", Name="test", FileName = filePath)
        project.References.AddRange references
        let projectConfig = project.AddNewConfiguration("Debug")
        TypeSystemService.LoadProject (project) |> ignore

        viewContent.Project <- project

        workbenchWindow.SetViewContent(viewContent)

        viewContent.ContentName <- "a.fs"
        viewContent.GetTextEditorData().Document.MimeType <- "text/x-fsharp"
        let doc = Document(workbenchWindow)

        (viewContent :> IEditableTextBuffer).Text <- text
        (viewContent:> IEditableTextBuffer).CursorPosition <- 0

        let pfile = doc.Project.AddFile("a.fs")

        let textEditorCompletion = new FSharpTextEditorCompletion()
        textEditorCompletion.Initialize(doc)
        viewContent.Contents.Add(textEditorCompletion)

        try 
            doc.UpdateParseDocument() |> ignore
        with exn -> Diagnostics.Debug.WriteLine(exn.ToString())
        doc, viewContent