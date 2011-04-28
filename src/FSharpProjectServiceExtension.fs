namespace FSharp.MonoDevelop

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Projects
open Mono.Addins

type FSharpProjectServiceExtension() =
  inherit ProjectServiceExtension()

  override x.SupportsItem(item:IBuildTarget) =
    // Extend any F# project
    (item :? DotNetProject) && (item :?> DotNetProject).LanguageName = "F#"

  override x.PopulateSupportFileList(project:Project, list:FileCopySet, configuration:ConfigurationSelector) =
    base.PopulateSupportFileList (project, list, configuration)
    let outDir = (project.GetConfiguration (configuration) :?> ProjectConfiguration).OutputDirectory
    let added = list.Add(new FilePath (AddinManager.CurrentAddin.GetFilePath("FSharp.Core.dll")), true)
    ()
    
    
