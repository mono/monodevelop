namespace MonoDevelop.FSharp

open System
open MonoDevelop.Projects
open MonoDevelop.Projects.Formats.MSBuild

type FSharpResourceIdBuilder() =
  inherit MSBuildResourceHandler()
  
  override x.GetDefaultResourceId(pf:ProjectFile) : string =
    base.GetDefaultResourceId(pf)
