// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open FSharp.CompilerBinding

type IProjectParser =
  abstract FileName : string with get
  abstract LoadTime : System.DateTime with get
  abstract Directory : string with get
  abstract member GetFiles : string array
  abstract member FrameworkVersion : FSharpTargetFramework
  abstract member Output : string
  abstract member GetReferences : string array
  abstract member GetOptions : string array

