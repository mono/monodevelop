// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open System
open System.IO
open Microsoft.Build
open Microsoft.Build.Execution

open FSharp.CompilerBinding

type DotNetProjectParser private (p: ProjectInstance) =

  let loadtime = DateTime.Now

  static member Load (uri : string) : Option<IProjectParser> =
    try
      let visualStudioVersion =
        let pfx86 = Environment.GetFolderPath
                             Environment.SpecialFolder.ProgramFilesX86

        let programFiles = 
          if String.IsNullOrWhiteSpace pfx86 then
            Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles
          else
            pfx86

        let fsharp31Dir = Path.Combine(programFiles,
                                       @"Microsoft SDKs\F#\3.1\Framework\v4.0")

        if Directory.Exists fsharp31Dir then "12.0" else "11.0"

      let props = dict [ "VisualStudioVersion", visualStudioVersion ]
      let p = new ProjectInstance(uri, props, "4.0")
      Some (new DotNetProjectParser(p) :> IProjectParser)
    with
      | :? Exceptions.InvalidProjectFileException -> None

  interface IProjectParser with
    member x.FileName = p.FullPath
    member x.LoadTime = loadtime
    member x.Directory = p.Directory

    member x.GetFiles =
      let fs  = p.GetItems("Compile")
      let dir = (x :> IProjectParser).Directory
      [| for f in fs do yield IO.Path.Combine(dir, f.EvaluatedInclude) |]

    member x.FrameworkVersion =
      match p.GetPropertyValue("TargetFrameworkVersion") with
      | "v2.0" -> FSharpTargetFramework.NET_2_0
      | "v3.0" -> FSharpTargetFramework.NET_3_0
      | "v3.5" -> FSharpTargetFramework.NET_3_5
      | "v4.0" -> FSharpTargetFramework.NET_4_0
      | "v4.5" -> FSharpTargetFramework.NET_4_5
      | _      -> FSharpTargetFramework.NET_4_5

    member x.Output = p.GetPropertyValue "TargetPath"

    // On .NET MSBuild, it seems to be the case that child projects
    // are built with the 'default targets' when trying to resolve
    // assembly references recursively. This is a) overkill, and
    // b) doesn't always succeed. Here we load child projects and
    // return their outputs.
    member x.GetReferences =
      ignore <| p.Build([|"ResolveAssemblyReferences"|], [])
      [| for i in p.GetItems("ReferencePath") do
           yield "-r:" + i.EvaluatedInclude
         for cp in p.GetItems("ProjectReference") do
           match DotNetProjectParser.Load (cp.GetMetadataValue("FullPath")) with
           | None -> ()
           | Some p' -> yield Path.Combine(Path.GetDirectoryName (x :> IProjectParser).Output,
                                           Path.GetFileName p'.Output)
      |]

    member x.GetOptions =
      let getprop s = p.GetPropertyValue s
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



