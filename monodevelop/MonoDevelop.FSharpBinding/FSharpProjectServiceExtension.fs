namespace MonoDevelop.FSharp

open System
open System.IO
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Projects

type FSharpProjectServiceExtension() =
  inherit ProjectServiceExtension()

  override x.SupportsItem(item:IBuildTarget) =
    // Extend any F# project
    (item :? DotNetProject) && (item :?> DotNetProject).LanguageName = "F#"

    
