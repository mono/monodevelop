open System
open Nessos.UnionArgParser
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Reflection
open Nessos.FsPickler

type Arguments =
  | Project of string
  with
    interface IArgParserTemplate with
      member s.Usage =
        match s with
        | Project _ -> "specify a F# project file (.fsproj)."

[<EntryPoint>]
let main argv =
  // build the argument parser
  try
    let parser = UnionArgParser.Create<Arguments>()

    let results = parser.Parse argv
    let projectFile = results.GetResult(<@ Project @>)

    let checker = FSharpChecker.Create()
   
    let fsharpProjectOptions = checker.GetProjectOptionsFromProjectFile(projectFile)

    let json = FsPickler.CreateBinary()
    let outstream = Console.OpenStandardOutput()
    json.Serialize(outstream, fsharpProjectOptions)
  with ex ->
    Console.Out.WriteLine(ex)

  0 // return an integer exit code


