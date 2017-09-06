// --------------------------------------------------------------------------------------
// Serializable types that store F# project parameters (build order) and
// F# compiler parameters (debug mode, tail-calls, etc.)
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core.Serialization
open MonoDevelop.Projects

/// Serializable type respresnting F# compiler parameters
type FSharpCompilerParameters() =
    inherit MonoDevelop.Projects.DotNetCompilerParameters()
    
    [<ItemProperty ("Optimize")>]
    let mutable optimize = true
    
    [<ItemProperty ("GenerateTailCalls", DefaultValue = false)>]
    let mutable generateTailCalls = false
    
    [<ItemProperty ("NoStdLib", DefaultValue = false)>]
    let mutable noStdLib = false
    
    [<ItemProperty("DefineConstants")>]
    let mutable defineConstants = ""
    
    [<ItemProperty("OtherFlags", DefaultValue="")>]
    let mutable otherFlags = ""
    
    [<ProjectPathItemProperty("DocumentationFile", DefaultValue="")>]
    let mutable documentationFile = ""
    
    [<ItemProperty("PlatformTarget", DefaultValue="anycpu")>]
    let mutable platformTarget = "anycpu"
    
    [<ItemProperty("TreatWarningsAsErrors", DefaultValue = false)>]
    let mutable warnAsError = false
    
    [<ItemProperty("WarningLevel", DefaultValue = 4)>]
    let mutable warningLevel = 4
    
    [<ItemProperty("NoWarn", DefaultValue = "")>]
    let mutable nowarn = String.empty

    [<ItemProperty("DebugType", DefaultValue = "portable")>]
    let mutable debugType = "portable"

    member x.Optimize with get () = optimize and set v = optimize <- v
    member x.GenerateTailCalls with get () = generateTailCalls and set v = generateTailCalls <- v
    override x.NoStdLib with get () = noStdLib and set v = noStdLib <- v
    member x.DefineConstants with get () = defineConstants and set v = defineConstants <- v
    member x.OtherFlags with get () = otherFlags and set v = otherFlags <- v
    member x.DocumentationFile with get () = documentationFile and set v = documentationFile <- v
    member x.PlatformTarget with get () = platformTarget and set v = platformTarget <- v
    member x.TreatWarningsAsErrors with get () = warnAsError and set v = warnAsError <- v
    member x.WarningLevel with get () = warningLevel and set v = warningLevel <- v
    member x.NoWarn with get () = nowarn and set v = nowarn <- v
    member x.DebugType with get () = debugType and set v = debugType <- v
    
    override x.AddDefineSymbol(symbol) =
        if System.String.IsNullOrEmpty x.DefineConstants then
          x.DefineConstants <- symbol
        else
          x.DefineConstants <- x.DefineConstants + ";" + symbol
    
    override x.RemoveDefineSymbol(symbol) =
        if x.DefineConstants = symbol then
          x.DefineConstants <- ""
        elif (String.IsNullOrWhiteSpace >> not) x.DefineConstants then
          x.DefineConstants <- x.DefineConstants.Replace(";" + symbol, "")
    
    override x.GetDefineSymbols () =
        if String.IsNullOrWhiteSpace x.DefineConstants then
          Seq.empty
        else
          x.DefineConstants.Split (';', ',', ' ', '\t')
          |> Seq.where (String.IsNullOrWhiteSpace >> not)
    
    override x.CreateCompilationOptions () =
        null //TODO
    
    override x.CreateParseOptions (_) =
        null //TODO
    
    override x.Write pset =
        pset.SetPropertyOrder ("DebugSymbols", "DebugType", "Optimize", "OutputPath", "DefineConstants", "ErrorReport", "WarningLevel", "TreatWarningsAsErrors", "DocumentationFile");
        base.Write pset
