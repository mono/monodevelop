// --------------------------------------------------------------------------------------
// Serializable types that store F# project parameters (build order) and 
// F# compiler parameters (debug mode, tail-calls, etc.)
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp

open MonoDevelop.Projects
open MonoDevelop.Core.Serialization

/// Serializable type that is used for storing file build order
type FSharpProjectParameters() =
  inherit DotNetProjectParameters()

  [<field:ItemProperty("BuildOrder"); DefaultValue>]
  val mutable private buildOrder : string[]

  member x.BuildOrder 
    with get() = if x.buildOrder = null then [| |] else x.buildOrder
    and set(value) = x.buildOrder <- value


/// Serializable type respresnting F# compiler parameters
type FSharpCompilerParameters() = 
  inherit ConfigurationParameters()
  
  [<field:ItemProperty("DebugSymbols"); DefaultValue>]
  val mutable private  debugSymbols : bool

  [<field:ItemProperty("DebugType"); DefaultValue>]
  val mutable private  debugType : string

  [<field:ItemProperty("Optimize"); DefaultValue>]
  val mutable private optimize : bool

  [<field:ItemProperty("DocumentationFile"); DefaultValue>]
  val mutable private documentationFile : string

  [<field:ItemProperty("Tailcalls"); DefaultValue>]
  val mutable private generateTailCalls : bool

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
    with get() = x.generateTailCalls
    and set(value) = x.generateTailCalls <- value
        
  member x.Optimize
    with get() = x.optimize
    and set(value) = 
        x.optimize <- value
        x.debugType <- (if x.DebugSymbols then (if x.optimize then "pdbonly" else "full") else "none")
        
  member x.DebugSymbols
    with get() = x.debugSymbols
    and set(value) = 
        x.debugSymbols <- value
        x.debugType <- (if x.DebugSymbols then (if x.optimize then "pdbonly" else "full") else "none")