// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

// Disable warnings for obsolete MSBuild.
// Mono doesn't support the latest API.
#nowarn "0044"

open System
open System.IO
open Microsoft.Build.BuildEngine
open FSharp.CompilerBinding

type MonoProjectParser private (p: Project) =

  let loadtime = DateTime.Now

  static member Load (uri : string) : Option<IProjectParser> =
    let p = new Project()
    if File.Exists uri then
      try
        p.Load(uri)
        Some (new MonoProjectParser(p) :> IProjectParser)
      with :? InvalidProjectFileException ->
        None
    else
      None

  interface IProjectParser with
    member x.FileName = p.FullFileName
    member x.LoadTime = loadtime
    member x.Directory = Path.GetDirectoryName (x :> IProjectParser).FileName

    member x.GetFiles =
      let fs  = p.GetEvaluatedItemsByName("Compile")
      let dir = (x :> IProjectParser).Directory
      [| for f in fs do yield IO.Path.Combine(dir, f.FinalItemSpec) |]

    member x.FrameworkVersion =
      match p.GetEvaluatedProperty("TargetFrameworkVersion") with
      | "v2.0" -> FSharpTargetFramework.NET_2_0
      | "v3.0" -> FSharpTargetFramework.NET_3_0
      | "v3.5" -> FSharpTargetFramework.NET_3_5
      | "v4.0" -> FSharpTargetFramework.NET_4_0
      | "v4.5" -> FSharpTargetFramework.NET_4_5
      | _      -> FSharpTargetFramework.NET_4_5

    member x.Output =
      IO.Path.Combine((x :> IProjectParser).Directory,
                      (p.GetEvaluatedProperty "OutDir"),
                      (p.GetEvaluatedProperty "TargetFileName"))

    member x.GetReferences =
      let x = x :> IProjectParser
      ignore <| p.Build([|"ResolveAssemblyReferences"|])
      [| for i in p.GetEvaluatedItemsByName("ResolvedFiles")
           do yield "-r:" + i.FinalItemSpec
         for i in p.GetEvaluatedItemsByName("ProjectReference") do
           let fsproj = Path.Combine(x.Directory, i.FinalItemSpec)
           match MonoProjectParser.Load fsproj with
           | None -> ()
           | Some cp -> yield "-r:" + cp.Output |]

    member x.GetOptions =
      let getprop s = p.GetEvaluatedProperty s
      let split (s: string) (cs: char[]) =
        if s = null then [||]
        else s.Split(cs, StringSplitOptions.RemoveEmptyEntries)
      let getbool (s: string) =
        match (Boolean.TryParse s) with
        | (true, result) -> result
        | (false, _) -> false
      let optimize     = getprop "Optimize" |> getbool
      let tailcalls    = getprop "Tailcalls" |> getbool
      let debugsymbols = getprop "DebugSymbols" |> getbool
      let defines = split (getprop "DefineConstants") [|';';',';' '|]
      let otherflags = getprop "OtherFlags" 
      let otherflags = if otherflags = null
                       then [||]
                       else split otherflags [|' '|]
      [|
        yield "--noframework"
        for symbol in defines do yield "--define:" + symbol
        yield if debugsymbols then  "--debug+" else  "--debug-"
        yield if optimize then "--optimize+" else "--optimize-"
        yield if tailcalls then "--tailcalls+" else "--tailcalls-"
        yield! otherflags
        yield! (x :> IProjectParser).GetReferences
       |]
