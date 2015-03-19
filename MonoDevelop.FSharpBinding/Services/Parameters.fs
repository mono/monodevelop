// --------------------------------------------------------------------------------------
// Serializable types that store F# project parameters (build order) and 
// F# compiler parameters (debug mode, tail-calls, etc.)
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open System
open MonoDevelop.Projects
open MonoDevelop.Core.Serialization

/// Serializable type respresnting F# compiler parameters
type FSharpCompilerParameters() = 
  inherit MonoDevelop.Projects.DotNetConfigurationParameters()

  [<ItemProperty ("Optimize")>]           
  let mutable optimize = true
     
  [<ItemProperty ("DebugSymbols")>]    
  let mutable debugSymbols = true

  [<ItemProperty ("GenerateTailCalls", DefaultValue = false)>] 
  let mutable generateTailCalls = false
    
  [<ItemProperty ("NoStdLib", DefaultValue = false)>]
  let mutable noStdLib = false

  //can be "pdbonly", "full"
  [<ItemProperty ("DebugType", DefaultValue = "full")>]
  let mutable debugType = "full"

  [<ItemProperty("DefineConstants")>]
  let mutable defineConstants = ""

  [<ItemProperty("OtherFlags", DefaultValue="")>]
  let mutable otherFlags = ""

  [<ItemProperty("DocumentationFile", DefaultValue="")>]
  let mutable documentationFile = ""

  [<ItemProperty("PlatformTarget", DefaultValue="anycpu")>]
  let mutable platformTarget = "anycpu"

  member x.Optimize with get () = optimize and set v = optimize <- v
  member x.DebugSymbols with get () = debugSymbols and set v = debugSymbols <- v
  member x.GenerateTailCalls with get () = generateTailCalls and set v = generateTailCalls <- v
  override x.NoStdLib with get () = noStdLib and set v = noStdLib <- v
  override x.DebugType with get () = debugType and set v = debugType <- v
  member x.DefineConstants with get () = defineConstants and set v = defineConstants <- v
  member x.OtherFlags with get () = otherFlags and set v = otherFlags <- v
  member x.DocumentationFile with get () = documentationFile and set v = documentationFile <- v
  member x.PlatformTarget with get () = platformTarget and set v = platformTarget <- v

  override x.AddDefineSymbol(symbol) =
    if System.String.IsNullOrEmpty x.DefineConstants then
      x.DefineConstants <- symbol
    else
      x.DefineConstants <- x.DefineConstants + ";" + symbol

  override x.RemoveDefineSymbol(symbol) =
    if x.DefineConstants = symbol then
      x.DefineConstants <- null
    elif x.DefineConstants <> null then
      x.DefineConstants <- x.DefineConstants.Replace(";" + symbol, null)
      
  override x.GetDefineSymbols () =
    x.DefineConstants.Split (';', ',', ' ', '\t')
    |> Seq.where (not << String.IsNullOrWhiteSpace)

  override x.HasDefineSymbol(symbol) =
    x.DefineConstants.Split(';', ',', ' ', '\t') |> Array.exists (fun s -> symbol = s)

  override x.CreateCompilationOptions () =
      null //TODO

  override x.CreateParseOptions () =
      null //TODO

