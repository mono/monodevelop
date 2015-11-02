open System
open System.IO
open Nessos.Argu
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Reflection
open Nessos.FsPickler.Json

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
    let normalizedReferences = 
      fsharpProjectOptions.ReferencedProjects
      |> Array.map (fun p -> let (name, options) = p
                             let fullPath = Path.GetFullPath(name)
                             fullPath, { options with ProjectFileName = fullPath })

    let pickler = FsPickler.CreateJsonSerializer()
    let outstream = Console.OpenStandardOutput()
    pickler.Serialize(outstream, { fsharpProjectOptions with ReferencedProjects = normalizedReferences })
    0
  with ex ->
    Console.Out.WriteLine(ex)
    1