namespace MonoDevelop.FSharp

open System
open MonoDevelop.Projects

type PortableFSharpProjectFlavor() =
    inherit PortableDotNetProjectFlavor()

    override x.Initialize () =
        base.Initialize ()
        base.Project.UseMSBuildEngine <- Nullable(true)

    override x.OnGetDefaultImports imports =
        imports.Add @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.Portable.FSharp.Targets"
