module Module1

open NUnit.Framework
open FsUnit

open FSharp.InteractiveAutocomplete
open System.IO

[<Test>]
let TestProjectLibraryResolution () =
  let p  = ProjectParser.load "../../data/Test1.fsproj"
  Option.isSome p |> should be True
  let rs = ProjectParser.getReferences p.Value
  rs |> should haveLength 4

[<Test>]
let Test2ndLevelDepsResolution () =
  let p  = ProjectParser.load "../../data/Test2.fsproj"
  Option.isSome p |> should be True
  let rs = ProjectParser.getReferences p.Value
  rs |> should haveLength 6
  rs |> Array.map Path.GetFileName
     |> should contain "Test1.dll"

