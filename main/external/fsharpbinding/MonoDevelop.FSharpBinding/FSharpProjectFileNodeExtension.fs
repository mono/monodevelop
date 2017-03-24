namespace MonoDevelop.FSharp

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Projects
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui.Pads.ProjectPad
open System.Linq
open System.Xml
open System.Xml.Linq
open Linq2Xml

/// The command handler type for nodes in F# projects in the solution explorer.
type FSharpProjectNodeCommandHandler() =
    inherit NodeCommandHandler()

    /// Reload project causing the node tree up refresh with new ordering
    let reloadProject (file: ProjectFile) =
        use monitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor(true)
        monitor.BeginTask("Reloading Project", 1) |> ignore
        file.Project.ParentFolder.ReloadItem(monitor, file.Project) |> ignore
        monitor.Step (1)
        monitor.EndTask()

    member x.MoveNodes (moveToNode: ProjectFile) (movingNode:ProjectFile) position =
        let projectFile = movingNode.Project.FileName.ToString()

        let descendantsNamed ns name ancestor =
            ///partially apply the default namespace of msbuild to xs
            let xd = xs ns
            descendants (xd name) ancestor

        // If the "Compile" element contains a "Link" element then it is a linked file,
        // so use that value for comparison when finding the node.
        let nodeName ns (node:XElement) =
          let link = node |> descendantsNamed ns "Link" |> firstOrNone
          match link with
          | Some l -> l.Value
          | None -> node |> attributeValue "Include"

        //open project file
        use file = IO.File.Open(projectFile, FileMode.Open)
        let xdoc = XElement.Load(file)
        file.Close()
        let defaultNamespace = xdoc.GetDefaultNamespace().NamespaceName
        let descendantsByNamespace = descendantsNamed defaultNamespace
        //get movable nodes from the project file
        let movableNodes = (descendantsByNamespace "Compile" xdoc).
                            Concat(descendantsByNamespace "EmbeddedResource" xdoc).
                            Concat(descendantsByNamespace "Content" xdoc).
                            Concat(descendantsByNamespace "None" xdoc)

        let findByIncludeFile name seq =
            seq |> where (fun elem -> nodeName defaultNamespace elem = name )
                |> firstOrNone

        let getFullName (pf:ProjectFile) = pf.ProjectVirtualPath.ToString().Replace("/", "\\")

        let movingElement = movableNodes |> findByIncludeFile (getFullName movingNode)
        let moveToElement = movableNodes |> findByIncludeFile (getFullName moveToNode)

        let addFunction (moveTo:XElement) (position:DropPosition) =
            match position with
            | DropPosition.Before -> moveTo.AddBeforeSelf : obj -> unit
            | DropPosition.After -> moveTo.AddAfterSelf : obj -> unit
            | _ -> ignore

        match (movingElement, moveToElement, position) with
        | Some(moving), Some(moveTo), (DropPosition.Before | DropPosition.After) ->
            moving.Remove()
            //if the moving node contains a DependentUpon node as a child remove the DependentUpon nodes
            moving |> descendantsByNamespace "DependentUpon" |> Seq.iter (fun node -> node.Remove())
            //get the add function using the position
            let add = addFunction moveTo position
            add(moving)

            let settings = XmlWriterSettings(OmitXmlDeclaration = true, Indent = true)
            use writer = XmlWriter.Create(projectFile, settings)
            xdoc.Save(writer);
        | _ -> ()//If we cant find both nodes or the position isnt before or after we dont continue

    /// Implement drag and drop of nodes in F# projects in the solution explorer.
    override x.OnNodeDrop(dataObject, dragOperation, position) =
        match dataObject, dragOperation with
        | :? ProjectFile as movingNode, DragOperation.Move ->
            //Move as long as this is a drag op and the moving node is a project file
            match x.CurrentNode.DataItem with
            | :? ProjectFile as moveToNode ->
                x.MoveNodes moveToNode movingNode position
                reloadProject moveToNode
            | _ -> ()//unsupported
        | _ -> //otherwise use the base behaviour
            base.OnNodeDrop(dataObject, dragOperation, position)

    /// Implement drag and drop of nodes in F# projects in the solution explorer.
    override x.CanDragNode() = DragOperation.Move

    /// Implement drag and drop of nodes in F# projects in the solution explorer.
    override x.CanDropNode(_dataObject, _dragOperation) = true

    /// Implement drag and drop of nodes in F# projects in the solution explorer.
    override x.CanDropNode(dataObject, _dragOperation, _position) =
        //currently we are going to only support dropping project files from the same parent project
        match (dataObject, x.CurrentNode.DataItem) with
        | (:? ProjectFile as drag), (:? ProjectFile as drop) ->
          drag.Project = drop.Project && drop.ProjectVirtualPath.ParentDirectory = drag.ProjectVirtualPath.ParentDirectory
        | _ -> false
  //This would allow anything to be droppped as long as it was in the same project and path level
  //We would need to add to moveNodes so it knows how to find ProvectFolders and other items that mught be present
  //      | drag, drop ->
  //          match getProjectAndPath drag, getProjectAndPath drop with
  //          | Some(project1, project1Path), Some(project2, project2Path) -> project1 = project2 && project1Path.ParentDirectory = project2Path.ParentDirectory
  //          | _ -> false


/// MD/XS extension for the F# project nodes in the solution explorer.
type FSharpProjectFileNodeExtension() =
    inherit NodeBuilderExtension()

    let (|FSharpProject|_|) (project:Project) =
        match project with
        | :? DotNetProject as dnp when dnp.LanguageName = "F#" -> Some dnp
        | _ -> None

    /// Check if an item in the project model is recognized by this extension.
    let (|SupportedProjectFile|SupportedProjectFolder|NotSupported|) (item:obj) =
        match item with
        | :? ProjectFile as projfile when projfile.Project <> null ->
             match projfile.Project with
             | FSharpProject _ -> SupportedProjectFile(projfile)
             | _ -> NotSupported
        | :? ProjectFolder as projfolder when projfolder.Project <> null ->
             match projfolder.Project with
             | FSharpProject _ -> SupportedProjectFolder(projfolder)
             | _ -> NotSupported
        | _ -> NotSupported

    let findIndex thing =
        match thing with
        | SupportedProjectFile(file) -> file.Project.Files.IndexOf(file)
        | SupportedProjectFolder(folder) ->
            let childfile =
                folder.Project.Files
                |> Seq.tryFind (fun p -> p.FilePath.IsChildPathOf folder.Path)

            match childfile with
            | Some file -> folder.Project.Files.IndexOf file
            | None -> //fallback to finding a directory subtype
                let folderIndex =
                    folder.Project.Files
                    |> Seq.filter (fun file -> file.Subtype = Subtype.Directory)
                    |> Seq.tryFindIndex(fun pf -> pf.FilePath = folder.Path)
                match folderIndex with
                | Some i -> i
                | _ -> NodeBuilder.DefaultSort
        | NotSupported -> NodeBuilder.DefaultSort


    override x.CanBuildNode(dataType:Type) =
        // Extend any file or folder belonging to a F# project
        typedefof<ProjectFile>.IsAssignableFrom(dataType) || typedefof<ProjectFolder>.IsAssignableFrom (dataType)

    override x.CompareObjects(thisNode:ITreeNavigator, otherNode:ITreeNavigator) : int =
        match (otherNode.DataItem, thisNode.DataItem) with
        | SupportedProjectFile other, SupportedProjectFile thisNode -> compare (findIndex thisNode) (findIndex other)
        | SupportedProjectFolder other, SupportedProjectFolder thisNode -> compare (findIndex thisNode) (findIndex other)
        | SupportedProjectFile other, SupportedProjectFolder thisNode -> compare (findIndex thisNode) (findIndex other)
        | SupportedProjectFolder other, SupportedProjectFile thisNode -> compare (findIndex thisNode) (findIndex other)
        | _ -> NodeBuilder.DefaultSort

    override x.CommandHandlerType = typeof<FSharpProjectNodeCommandHandler>
