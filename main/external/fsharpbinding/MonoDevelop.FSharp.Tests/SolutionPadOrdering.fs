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

    [<Test>]
    let ``folder appears before file``() =
        use project = Services.ProjectService.CreateDotNetProject ("F#")
        let folder = new ProjectFolder(FilePath "folderb", project)
        // we need to add a file into the folder so that we
        // can determine the index of the folder
        project.AddFile(new ProjectFile("folderb/ignore.fs"))

        let file = new ProjectFile("/afile.fs")
        project.AddFile(file)

        let filesAndFolders = [ box file; box folder ]

        filesAndFolders
        |> List.sortWith comparison
        |> should equal [ box folder; box file ]

    [<Test>]
    let ``linked items are ordered correctly by link path``() =
        use project = Services.ProjectService.CreateDotNetProject ("F#")

        // project folders use absolute paths,
        // Links use relative paths
        let tempPath = Path.GetTempPath()

        project.FileName <- tempPath/"project.fsproj"
        let folderA = new ProjectFolder(tempPath/"linkA", project)
        let file = new ProjectFile("/afile.fs")
        file.Link <- FilePath "linkA/afile.fs"
        project.AddFile(file)

        let folderB = new ProjectFolder(tempPath/"linkB", project)
        let file = new ProjectFile("/bfile.fs")
        file.Link <- FilePath "linkB/bfile.fs"
        project.AddFile(file)
        let filesAndFolders = [ box folderB; box folderA ]

        filesAndFolders
        |> List.sortWith comparison
        |> should equal [ box folderA; box folderB ]