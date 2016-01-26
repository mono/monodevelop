open System
open System.IO
open Nessos.Argu
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Reflection
open Nessos.FsPickler.Json

type Arguments =
    | Project of string
    | Log
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Project _ -> "specify a F# project file (.fsproj)."
                | Log -> "Set to return a log of processing"

[<EntryPoint>]
let main argv =
    let pickler = FsPickler.CreateJsonSerializer()
    let outstream = Console.OpenStandardOutput()

    let (|Prefix|_|) (p:string) (s:string) =
      if s.StartsWith(p) then
          Some(s.Substring(p.Length))
      else
          None

    let normalizeOptions options =
        options
        |> Array.map (fun o -> match o with
                               | Prefix "-r:" rest -> "-r:" + Path.GetFullPath(rest)
                               | Prefix "--out:" rest -> "--out:" + Path.GetFullPath(rest)
                               | _ -> o)

    let rec normalizeProject (path, options) =
        let fullPath = Path.GetFullPath(path)
        fullPath, { options with ProjectFileName = Path.GetFullPath options.ProjectFileName
                                 OtherOptions = normalizeOptions options.OtherOptions
                                 ReferencedProjects = options.ReferencedProjects
                                                      |> Array.map normalizeProject }

    let result =
        try
            let parser = ArgumentParser.Create<Arguments>()
            let results = parser.Parse argv
            let projectFile = results.GetResult(<@ Project @>)
            let log = results.Contains(<@ Log @>)
            Environment.CurrentDirectory <- Path.GetDirectoryName projectFile
            let fsharpProjectOptions, logs =  CompilerService.ProjectCracker.GetProjectOptionsFromProjectFile(projectFile, enableLogging=log)
            let (_, normalizedProject) = normalizeProject (projectFile, fsharpProjectOptions)
            Choice1Of2 (normalizedProject, logs)
        with
        | ex -> Choice2Of2 ex
    pickler.Serialize(outstream, result)
    0
