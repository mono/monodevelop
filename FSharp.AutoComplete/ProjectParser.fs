// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open System
open Microsoft.Build.BuildEngine
open Microsoft.Build.Framework
open Microsoft.Build.Tasks
open Microsoft.Build.Utilities

module ProjectParser =

  type ProjectResolver =
    {
      project:  Project
      loadtime: DateTime
    }

  let load (uri: string) : Option<ProjectResolver> =
    let p = new Project()
    try
      p.Load(uri)
      Some { project = p; loadtime = DateTime.Now }
    with :? InvalidProjectFileException as e ->
      None

  let getFileName (p: ProjectResolver) : string = p.project.FullFileName

  let getLoadTime (p: ProjectResolver) : DateTime = p.loadtime

  let getDirectory (p: ProjectResolver) : string =
    IO.Path.GetDirectoryName p.project.FullFileName

  let getFiles (p: ProjectResolver) : string array =
    let fs  = p.project.GetEvaluatedItemsByName("Compile")
    let dir = getDirectory p
    [| for f in fs do yield IO.Path.Combine(dir, f.FinalItemSpec) |]

  // We really want the output of ResolveAssemblyReferences. However, this
  // needs as input ChildProjectReferences, which is populated by
  // ResolveProjectReferences. For some reason ResolveAssemblyReferences
  // does not depend on ResolveProjectReferences, so if we don't run it first
  // then we won't get the dll files for imported projects in this list.
  // We can therefore build ResolveReferences, which depends on both of them,
  // or [|"ResolveProjectReferences";"ResolveAssemblyReferences"|]. These seem
  // to be equivalent. See Microsoft.Common.targets if you want more info.
  let getReferences (p: ProjectResolver) : string array =
    ignore <| p.project.Build([|"ResolveReferences"|])
    [| for i in p.project.GetEvaluatedItemsByName("ResolvedFiles")
         do yield "-r:" + i.FinalItemSpec |]

  let getOptions (p: ProjectResolver) : string array =
    let getprop s = p.project.GetEvaluatedProperty s
    let split (s: string) (cs: char[]) =
      s.Split(cs, StringSplitOptions.RemoveEmptyEntries)
    // TODO: Robustify - convert.ToBoolean may fail
    let optimize     = getprop "Optimize" |> Convert.ToBoolean
    let tailcalls    = getprop "Tailcalls" |> Convert.ToBoolean
    let debugsymbols = getprop "DebugSymbols" |> Convert.ToBoolean
    let defines = split (getprop "DefineConstants") [|';';',';' '|]
    let otherflags = getprop "OtherFlags"
    let otherflags = if otherflags  = null
                     then [||]
                     else split otherflags [|' '|]
    [|
      yield "--noframework"
      for symbol in defines do yield "--define:" + symbol
      yield if debugsymbols then  "--debug+" else  "--debug-"
      yield if optimize then "--optimize+" else "--optimize-"
      yield if tailcalls then "--tailcalls+" else "--tailcalls-"
      yield! otherflags
      yield! (getReferences p)
     |]
