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
  inherit DotNetConfigurationParameters()

  let asBool (s:string) = (String.Compare(s, "true", StringComparison.InvariantCultureIgnoreCase) = 0)
  let asString (b:bool) = if b then "true" else "false"

  // This is logically a boolean but we serialize as a string to always save as lower-case "true" rather than "True"
  // This keeps the text of the project file identical to Visual Studio.
  let mutable debugSymbols = "true"

  // This is logically a boolean but we serialize as a string to always save as lower-case "true" rather than "True"
  // This keeps the text of the project file identical to Visual Studio.
  let mutable optimize = "false"

  // This is logically a boolean but we serialize as a string to always save as lower-case "true" rather than "True"
  // This keeps the text of the project file identical to Visual Studio.
  let mutable generateTailCalls = "true"

  override val NoStdLib = false with get, set
#if MDVERSION_5_6_3
  member val DebugType = "" with get, set
#else
#if MDVERSION_5_5_4
  member val DebugType = "" with get, set
#else
  override val DebugType = "" with get, set
#endif
#endif

  member val DefineConstants = "" with get, set
  member val OtherFlags = "" with get, set
  member val DocumentationFile = "" with get, set
  member val PlatformTarget = "anycpu" with get, set

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

  member x.GenerateTailCalls
    with get() = asBool generateTailCalls
    and set(value) = 
        if generateTailCalls <> asString value then 
            generateTailCalls <- asString value 
        
  member x.Optimize
    with get() = asBool optimize
    and set(value) = 
        if optimize <> asString value then 
            optimize <- asString value
            x.DebugType <- (if x.DebugSymbols then (if x.Optimize then "pdbonly" else "full") else "none")
        
  member x.DebugSymbols
    with get() = asBool debugSymbols
    and set(value) = 
        if debugSymbols <> asString value then 
            debugSymbols <- asString value
            x.DebugType <- (if x.DebugSymbols then (if x.Optimize then "pdbonly" else "full") else "none")