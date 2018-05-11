namespace MonoDevelop.FSharp

open MonoDevelop.Projects

type PortableFSharpProjectFlavor() =
    inherit PortableDotNetProjectFlavor()

    override x.OnGetDefaultImports imports =
        imports.Add @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.Portable.FSharp.Targets"
