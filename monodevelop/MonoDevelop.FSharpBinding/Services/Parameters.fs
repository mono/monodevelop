// --------------------------------------------------------------------------------------
// Serializable types that store F# project parameters (build order) and 
// F# compiler parameters (debug mode, tail-calls, etc.)
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open MonoDevelop.Projects
open MonoDevelop.Core.Serialization

/// Serializable type respresnting F# compiler parameters
type FSharpCompilerParameters() as this = 
  inherit ConfigurationParameters()
  let asBool (s:string) = (System.String.Compare(s, "true", System.StringComparison.InvariantCultureIgnoreCase) = 0)
  let asString (b:bool) = if b then "true" else "false"
   
  do this.platformTarget <- "anycpu"
  
  [<field:ItemProperty("PlatformTarget",DefaultValue="anycpu"); DefaultValue>]
  val mutable private platformTarget : string
        
  [<field:ItemProperty("DebugSymbols"); DefaultValue>]
  // This is logically a boolean but we serialize as a string to always save as lower-case "true" rather than "True"
  // This keeps the text of the project file identical to Visual Studio.
  val mutable private  debugSymbols : string

  [<field:ItemProperty("DebugType"); DefaultValue>]
  val mutable private  debugType : string

  [<field:ItemProperty("Optimize"); DefaultValue>]
  // This is logically a boolean but we serialize as a string to always save as lower-case "true" rather than "True"
  // This keeps the text of the project file identical to Visual Studio.
  val mutable private optimize : string

  [<field:ItemProperty("DocumentationFile"); DefaultValue>]
  val mutable private documentationFile : string

  [<field:ItemProperty("Tailcalls"); DefaultValue>]
  // This is logically a boolean but we serialize as a string to always save as lower-case "true" rather than "True"
  // This keeps the text of the project file identical to Visual Studio.
  val mutable private generateTailCalls : string

  [<field:ItemProperty("DefineConstants"); DefaultValue>]
  val mutable private defineConstants : string

  [<field:ItemProperty("OtherFlags"); DefaultValue>]
  val mutable private otherFlags : string

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
      
#if MDVERSION_4_2_2
#else
#if MDVERSION_4_2_3
#else
#if MDVERSION_4_2_4
#else
#if MDVERSION_4_2_5
#else
#if MDVERSION_4_3_3
#else
#if MDVERSION_4_3_4
#else

  override x.GetDefineSymbols () =
    x.DefineConstants.Split (';', ',', ' ', '\t')
    |> Seq.where (fun s -> not (System.String.IsNullOrWhiteSpace(s)))
#endif
#endif
#endif
#endif
#endif
#endif
  override x.HasDefineSymbol(symbol) =
    x.DefineConstants.Split(';', ',', ' ', '\t') |> Array.exists (fun s -> symbol = s)

  member x.DefineConstants 
    with get() = if x.defineConstants = null then "" else x.defineConstants
    and set(value) = x.defineConstants <- value

  member x.OtherFlags
    with get() = if x.otherFlags = null then "" else x.otherFlags
    and set(value) = x.otherFlags <- value

  member x.DocumentationFile
    with get() = if x.documentationFile = null then "" else x.documentationFile
    and set(value) = x.documentationFile <- value

  member x.GenerateTailCalls
    with get() = asBool x.generateTailCalls
    and set(value) = 
        if x.generateTailCalls <> asString value then 
            x.generateTailCalls <- asString value 
        
  member x.Optimize
    with get() = asBool x.optimize
    and set(value) = 
        if x.optimize <> asString value then 
            x.optimize <- asString value
            x.debugType <- (if x.DebugSymbols then (if x.Optimize then "pdbonly" else "full") else "none")
        
  member x.DebugSymbols
    with get() = asBool x.debugSymbols
    and set(value) = 
        if x.debugSymbols <> asString value then 
            x.debugSymbols <- asString value
            x.debugType <- (if x.DebugSymbols then (if x.Optimize then "pdbonly" else "full") else "none")
        
  member x.PlatformTarget
    with get() = if x.platformTarget = null then "anycpu" else x.platformTarget
    and set(value) = x.platformTarget <- value
