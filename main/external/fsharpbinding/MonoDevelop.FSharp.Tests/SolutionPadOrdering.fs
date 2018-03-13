namespace MonoDevelopTests

open System.IO
open MonoDevelop.Core
open MonoDevelop.FSharp
open MonoDevelop.Ide
open MonoDevelop.Projects
open MonoDevelop.Ide.Gui.Pads.ProjectPad
open NUnit.Framework
open FsUnit

module SolutionPadOrdering =
    let comparison =
        (new FSharpProjectFileNodeExtension()).Compare

    let (/) a b = Path.Combine (a, b) |> FilePath

    let addFile (project: DotNetProject) fileName =
        let file = new ProjectFile(fileName)
        project.AddFile file
        box file

    let addLinkedFile (project: DotNetProject) fileName linkName =
        let file = new ProjectFile(fileName)
        file.Link <- FilePath linkName
        project.AddFile(file)
        box file

    [<Test>]
    let ``folder appears before file``() =
        use project = Services.ProjectService.CreateDotNetProject ("F#")

        let folderA = new ProjectFolder(FilePath "folderA", project) |> box
        let folderB = new ProjectFolder(FilePath "folderB", project) |> box

        let file7 = addFile project "file7.fs"
        let file6 = addFile project "folderB/file6.fs"
        let file5 = addFile project "folderB/file5.fs"
        let file4 = addFile project "file4.fs"
        let file3 = addFile project "folderA/file3.fs"
        let file2 = addFile project "folderA/file2.fs"
        let file1 = addFile project "file1.fs"

        [ folderA; file5; folderB; file1; file4; file3; file2; file6; file7 ]
        |> List.sortWith comparison
        |> should equal [ file7; folderB; file6; file5; file4; folderA; file3; file2; file1 ]

    [<Test>]
    let ``linked items are ordered correctly by link path``() =
        use project = Services.ProjectService.CreateDotNetProject ("F#")

        // project folders use absolute paths,
        // Linked files use paths relative to the project file
        let tempPath = Path.GetTempPath()

        project.FileName <- tempPath/"project.fsproj"
        let folderA = new ProjectFolder(tempPath/"linkA", project) |> box
        let folderB = new ProjectFolder(tempPath/"linkB", project) |> box

        let file7 = addFile project "/file7.fs"
        let file6 = addLinkedFile project "/file6.fs" "linkB/file6.fs"
        let file5 = addLinkedFile project "/file5.fs" "linkB/file5.fs" 
        let file4 = addFile project "/file4.fs"
        let file3 = addLinkedFile project "/file3.fs" "linkA/file3.fs"
        let file2 = addLinkedFile project "/file2.fs" "linkA/file2.fs"
        let file1 = addFile project "/file1.fs"

        [ folderA; file5; folderB; file1; file4; file3; file2; file6; file7 ]
        |> List.sortWith comparison
        |> should equal [ file7; folderB; file6; file5; file4; folderA; file3; file2; file1 ]
