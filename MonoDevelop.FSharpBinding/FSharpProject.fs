namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Projects.Formats.MSBuild
open System.Xml

type FSharpProject() = 
    inherit DotNetProject()
    // Keep the platforms combo of CodeGenerationPanelWidget in sync with this list
    let supportedPlatforms = [| "anycpu"; "x86"; "x64"; "itanium" |]
    
    override x.OnInitialize() = 
        base.OnInitialize()
    
    override x.OnWriteProject(monitor, msproject) = 
        base.OnWriteProject(monitor, msproject)
        // This updates the old guid to the new one on saving the project
        try 
            let fsimportExists = 
                msproject.Imports 
                |> Seq.exists 
                       (fun import -> import.Project.EndsWith("FSharp.Targets", StringComparison.OrdinalIgnoreCase))
            if fsimportExists then 
                msproject.GetGlobalPropertyGroup().GetProperties()
                |> Seq.tryFind (fun p -> p.Name = "ProjectTypeGuids")
                |> Option.iter (fun guids -> 
                       guids.Element.InnerText <- guids.Element.InnerText.Split
                                                      ([| ';' |], StringSplitOptions.RemoveEmptyEntries)
                                                  |> Array.filter 
                                                         (fun guid -> 
                                                         not 
                                                             (guid.Equals
                                                                  ("{4925A630-B079-445D-BCD4-3A9C94FE9307}", 
                                                                   StringComparison.OrdinalIgnoreCase)))
                                                  |> String.concat ";")
        with exn -> LoggingService.LogWarning("Failed to remove old F# guid", exn)
    
    override x.OnCompileSources(items, config, configSel, monitor) : BuildResult = 
        CompilerService.Compile(items, config, configSel, monitor)
    
    override x.OnCreateCompilationParameters(options : XmlElement) : DotNetCompilerParameters = 
        let pars = new FSharpCompilerParameters()
        // Set up the default options
        if options <> null then 
            let platform = options.GetAttribute("Platform")
            if (supportedPlatforms |> Array.exists (fun x -> x.Contains(platform))) then pars.PlatformTarget <- platform
            let debugAtt = options.GetAttribute("DefineDebug")
            if (System.String.Compare("True", debugAtt, StringComparison.OrdinalIgnoreCase) = 0) then 
                pars.AddDefineSymbol "DEBUG"
                pars.DebugSymbols <- true
                pars.Optimize <- false
                pars.GenerateTailCalls <- false
            let releaseAtt = options.GetAttribute("Release")
            if (System.String.Compare("True", releaseAtt, StringComparison.OrdinalIgnoreCase) = 0) then 
                pars.DebugSymbols <- false
                pars.Optimize <- true
                pars.GenerateTailCalls <- true
        // TODO: set up the documentation file to be AssemblyName.xml by default (but how do we get AssemblyName here?)
        // pars.DocumentationFile <- ""
        //    System.IO.Path.GetFileNameWithoutExtension(config.CompiledOutputName.ToString())+".xml" 
        pars :> DotNetCompilerParameters
    
    override x.OnGetSupportedClrVersions() = 
        [| ClrVersion.Net_2_0; ClrVersion.Net_4_0; ClrVersion.Net_4_5; ClrVersion.Clr_2_1 |]
