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
  let pickler = FsPickler.CreateJsonSerializer()
  let outstream = Console.OpenStandardOutput()
  let result =
    try
      let parser = ArgumentParser.Create<Arguments>()
      let results = parser.Parse argv
      let projectFile = results.GetResult(<@ Project @>)
      let checker = FSharpChecker.Create()
      let res = FSharpProjectFileInfo.Parse(projectFile, enableLogging= true)
      let log = res.LogOutput
      let fsharpProjectOptions = checker.GetProjectOptionsFromProjectFile(projectFile)
      let normalizedReferences = 
        fsharpProjectOptions.ReferencedProjects
        |> Array.map (fun p -> let (name, options) = p
                               let fullPath = Path.GetFullPath(name)
                               fullPath, { options with ProjectFileName = fullPath })

      Choice1Of2 { fsharpProjectOptions with ReferencedProjects = normalizedReferences }
    with
    | ex -> Choice2Of2(ex)
  pickler.Serialize(outstream, result)
  0