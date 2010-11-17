// --------------------------------------------------------------------------------------
// Serializable types that store F# project parameters (build order) and 
// F# compiler parameters (debug mode, tail-calls, etc.)
// --------------------------------------------------------------------------------------

namespace FSharp.MonoDevelop

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
  
  [<field:ItemProperty("GenerateDebugInfo"); DefaultValue>]
  val mutable public GenerateDebugInfo : bool

  [<field:ItemProperty("OptimizeCode"); DefaultValue>]
  val mutable public OptimizeCode : bool

  [<field:ItemProperty("GenerateXmlDoc"); DefaultValue>]
  val mutable public GenerateXmlDoc : bool

  [<field:ItemProperty("GenerateTailCalls"); DefaultValue>]
  val mutable public GenerateTailCalls : bool

  [<field:ItemProperty("DefinedSymbols"); DefaultValue>]
  val mutable private definedSymbols : string

  [<field:ItemProperty("CustomCommandLine"); DefaultValue>]
  val mutable private customCommandLine : string

  member x.DefinedSymbols 
    with get() = if x.definedSymbols = null then "" else x.definedSymbols
    and set(value) = x.definedSymbols <- value

  member x.CustomCommandLine
    with get() = if x.customCommandLine = null then "" else x.customCommandLine
    and set(value) = x.customCommandLine <- value
