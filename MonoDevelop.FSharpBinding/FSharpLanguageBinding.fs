namespace MonoDevelop.FSharp

open System
open System.IO

open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open FSharp.Compiler
open FSharp.CompilerBinding
open System.Linq
open MonoDevelop.Projects.Formats.MSBuild

// TODO NPM: this class is going away. Most of this functionality can be implemented by overriding
// methods in the new FSharpProject class.

type FSharpLanguageBinding() =

  let langServ = MDLanguageService.Instance
    
  let invalidateProjectFile(project:Project) =
    match project with
    | :? DotNetProject as dnp when dnp.LanguageName = "F#" ->
        try
            let options = langServ.GetProjectCheckerOptions(dnp.FileName.ToString(), [("Configuration", IdeApp.Workspace.ActiveConfigurationId)])
            langServ.InvalidateConfiguration(options)
            langServ.ClearProjectInfoCache()
        with ex -> LoggingService.LogError ("Could not invalidate configuration", ex)
    | _ -> ()
    
  let invalidateFiles (args:#ProjectFileEventInfo seq) =
    for projectFileEvent in args do
        if MDLanguageService.SupportedFileName (projectFileEvent.ProjectFile.FilePath.ToString()) then
            invalidateProjectFile(projectFileEvent.Project)

  let invalidateConfig _args =
      IdeApp.Workspace.GetAllProjects()
      |> Seq.iter invalidateProjectFile

  let eventDisposer =
      ResizeArray<IDisposable> ()
                 
  // Watch for changes that trigger a reparse, but only if we're running within the IDE context
  // and not from mdtool or something like it.
  do if IdeApp.IsInitialized then
      //Add events to invalidate FCS if anything imprtant to do with configuration changes
      //e.g. Files added/removed/renamed, or references added/removed      
      IdeApp.Workspace.ActiveConfigurationChanged.Subscribe(invalidateConfig) |> eventDisposer.Add
      IdeApp.Workspace.FileAddedToProject.Subscribe(invalidateFiles) |> eventDisposer.Add
      IdeApp.Workspace.FileRemovedFromProject.Subscribe(invalidateFiles) |> eventDisposer.Add
      IdeApp.Workspace.FileRenamedInProject.Subscribe(invalidateFiles) |> eventDisposer.Add
      IdeApp.Workspace.FilePropertyChangedInProject.Subscribe(invalidateFiles) |> eventDisposer.Add
      IdeApp.Workspace.ReferenceAddedToProject.Subscribe(fun (r:ProjectReferenceEventArgs) -> invalidateProjectFile(r.Project)) |> eventDisposer.Add
      IdeApp.Workspace.ReferenceRemovedFromProject.Subscribe(fun (r:ProjectReferenceEventArgs) -> invalidateProjectFile(r.Project)) |> eventDisposer.Add
      IdeApp.Workspace.SolutionUnloaded.Subscribe(fun _ -> langServ.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()) |> eventDisposer.Add

  interface IDisposable with
    member x.Dispose () = 
      for disp in eventDisposer do
        disp.Dispose ()
