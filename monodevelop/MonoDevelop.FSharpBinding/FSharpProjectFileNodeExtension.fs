namespace MonoDevelop.FSharp

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Projects
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Pads.ProjectPad

open System.Collections.Generic
open System.Linq
open System.Xml
open System.Xml.Linq
open Linq2Xml

type FSharpProjectNodeCommandHandler() =
  inherit NodeCommandHandler()

  let cachedPosition = ref Unchecked.defaultof<_>

  let reloadProject (currentNode: ITreeNavigator) =
    //reload project causing the node tree up refresh with new ordering
    use monitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor(true)
    monitor.BeginTask("Reloading Project", 1)
    let file = currentNode.DataItem :?> ProjectFile
    file.Project.ParentFolder.ReloadItem(monitor, file.Project) |> ignore
    monitor.Step (1)
    monitor.EndTask()

  let moveNodes (currentNode: ITreeNavigator) (movingNode:ProjectFile) position =
        let moveToNode = currentNode.DataItem :?> ProjectFile

        let projectFile = movingNode.Project.FileName.ToString()

        ///partially apply the default namespace of msbuild to xs
        let xd = xs "http://schemas.microsoft.com/developer/msbuild/2003"

        //open project file
        use file = IO.File.Open(projectFile, FileMode.Open)
        let xdoc = XElement.Load(file)
        file.Close()

        //get all the compile nodes from the project file
        let compileNodes = xdoc |> descendants (xd "Compile")

        let findByIncludeFile name seq = 
            seq |> where (fun elem -> (elem |> attributeValue "Include") = Path.GetFileName(name) )
                |> firstOrNone
        
        let getFullName (pf:ProjectFile) = pf.ProjectVirtualPath.ToString().Replace("/", "\\")

        let movingElement = compileNodes |> findByIncludeFile (getFullName movingNode)
        let moveToElement = compileNodes |> findByIncludeFile (getFullName moveToNode)

        let addFunction (moveTo:XElement) (position:DropPosition) =
            match position with
            | DropPosition.Before -> moveTo.AddBeforeSelf : obj -> unit
            | DropPosition.After -> moveTo.AddAfterSelf : obj -> unit
            | _ -> ignore

        match (movingElement, moveToElement, position) with
        | Some(moving), Some(moveTo), (DropPosition.Before | DropPosition.After) ->
            moving.Remove()
            //if the moving node contains a DependentUpon node as a child remove the DependentUpon nodes
            moving.Descendants( xd "DependentUpon") |> Seq.iter (fun node -> node.Remove())
            //get the add function using the position
            let add = addFunction moveTo position
            add(moving)
            xdoc.Save(projectFile)
            reloadProject currentNode

        | _ -> ()//If we cant find both nodes or the position isnt before or after we dont continue

  override x.OnNodeDrop(dataObject, dragOperation, position) =
    match dataObject, dragOperation with
    | :? ProjectFile as movingNode, DragOperation.Move ->
        //Move as long as this is a drag op and the moving node is a project file
        moveNodes x.CurrentNode movingNode position

    | _ -> //otherwise use the base behaviour
           base.OnNodeDrop(dataObject, dragOperation, position) 
        
  override x.CanDragNode() = DragOperation.Move

  override x.CanDropNode(dataObject, dragOperation) = true

  override x.CanDropNode(dataObject, dragOperation, position) =
      //currently we are going to only support dropping project files from the same parent project
      match (dataObject, x.CurrentNode.DataItem) with
      | (:? ProjectFile as drag), (:? ProjectFile as drop) -> drag.Project = drop.Project
      | _ -> false


type FSharpProjectFileNodeExtension() =
  inherit NodeBuilderExtension()

  override x.CanBuildNode(dataType:Type) =
    // Extend any file or folder belonging to a F# project
    typedefof<ProjectFile>.IsAssignableFrom (dataType) || typedefof<ProjectFolder>.IsAssignableFrom (dataType)

  override x.CompareObjects(thisNode:ITreeNavigator, otherNode:ITreeNavigator) : int =
    match (otherNode.DataItem, thisNode.DataItem) with
    | (:? ProjectFile as file2), (:? ProjectFile as file1) -> 
      if (file1.Project = file2.Project) && (file1.Project :? DotNetProject) && ((file1.Project :?> DotNetProject).LanguageName = "F#") then
            file1.Project.Files.IndexOf(file1).CompareTo(file2.Project.Files.IndexOf(file2))
      else NodeBuilder.DefaultSort
    | (:? ProjectFolder as folder1), (:? ProjectFolder as folder2) ->
         use file = IO.File.Open(folder1.Project.FileName.ToString(), FileMode.Open)
         let xdoc = XElement.Load(file)
         file.Close()

         //get the folders
         let folders = xdoc |> descendants (xs "http://schemas.microsoft.com/developer/msbuild/2003" "Folder") |> Seq.toArray
         let includes = folders |> Array.map (attributeValue "Include")

         //This can probably be replaced with something like whats in ProjectFile.VirtualProjectPath
         let getRealName (pf:ProjectFolder) =
             let rec loop (pf:ProjectFolder) current =
                 match pf.Parent with
                 | :? ProjectFolder as ppf ->
                    loop ppf (pf :: current)
                 | _ -> pf :: current
             let bits = loop pf []
             let path = bits |> Seq.map (fun pf -> pf.Name) |> String.concat "\\"
             path + "\\"

         let folder1RealName = getRealName folder1
         let folder2RealName = getRealName folder2
         let folder1Index = includes |> Array.tryFindIndex ((=) folder1RealName)
         let folder2Index = includes |> Array.tryFindIndex ((=) folder2RealName)
         match folder1Index, folder2Index with
         | Some(i1), Some(i2) -> 
            i2.CompareTo(i1)
         | _ -> NodeBuilder.DefaultSort
    | _ -> NodeBuilder.DefaultSort

  override x.CommandHandlerType = typeof<FSharpProjectNodeCommandHandler>


