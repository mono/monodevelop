open System
open System.IO
open System.Collections
open Nessos.Argu
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Reflection
open MsgPack.Serialization

type Arguments =
  | Project of string
  with
    interface IArgParserTemplate with
      member s.Usage =
        match s with
        | Project _ -> "specify a F# project file (.fsproj)."

[<EntryPoint>]
let main argv =
  try
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse argv
    let projectFile = results.GetResult(<@ Project @>)
    let checker = FSharpChecker.Create()
    let fsharpProjectOptions = checker.GetProjectOptionsFromProjectFile(projectFile)
    let serializer = SerializationContext.Default.GetSerializer<FSharpProjectOptions>()
    let outstream = Console.OpenStandardOutput()
    serializer.Pack(outstream, fsharpProjectOptions)
    0
  with ex ->
    Console.Out.WriteLine(ex)
    1