namespace MonoDevelop.FSharp

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Projects

type FSharpProjectFileNodeExtension() =
  inherit NodeBuilderExtension()

  override x.CanBuildNode(dataType:Type) =
    // Extend any file belonging to a F# project
    typedefof<ProjectFile>.IsAssignableFrom (dataType)

  override x.CompareObjects(thisNode:ITreeNavigator, otherNode:ITreeNavigator) : int =
    if otherNode.DataItem :? ProjectFile then
      let file1 = thisNode.DataItem :?> ProjectFile
      let file2 = otherNode.DataItem :?> ProjectFile
      if (file1.Project <> null) && (file1.Project = file2.Project) && (file1.Project :? DotNetProject) && ((file1.Project :?> DotNetProject).LanguageName = "F#") then
        file1.Project.Files.IndexOf(file1).CompareTo(file2.Project.Files.IndexOf(file2))
      else
        NodeBuilder.DefaultSort
    else
      NodeBuilder.DefaultSort
    
    
