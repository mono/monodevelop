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

  let (|Prefix|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None
  
  let normalizeOptions options =  
      options
      |> Array.map (fun o -> match o with
                             | Prefix "-r:" rest -> "-r:" + Path.GetFullPath(rest)
                             | _ -> o)

  let rec normalizeProject (path, options) =
      let fullPath = Path.GetFullPath(path)
      fullPath, { options with ProjectFileName = fullPath
                               OtherOptions = normalizeOptions options.OtherOptions
                               ReferencedProjects = options.ReferencedProjects 
                                                    |> Array.map normalizeProject }

  let result =
    try
      let parser = ArgumentParser.Create<Arguments>()
      let results = parser.Parse argv
      let projectFile = results.GetResult(<@ Project @>)
      Environment.CurrentDirectory <- Path.GetDirectoryName projectFile
      let checker = FSharpChecker.Create()
      //let projectFileInfo = FSharpProjectFileInfo.Parse(projectFile, enableLogging= true)
      //let log = projectFileInfo.LogOutput
      let fsharpProjectOptions = checker.GetProjectOptionsFromProjectFile(projectFile)
      let (_, normalizedProject) = normalizeProject (".", fsharpProjectOptions)

      Choice1Of2 normalizedProject
    with
    | ex -> Choice2Of2 ex
  pickler.Serialize(outstream, result)
  0