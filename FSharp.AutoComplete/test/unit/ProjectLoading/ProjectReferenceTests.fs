module ProjectReferenceTests

open NUnit.Framework
open FsUnit

open FSharp.InteractiveAutocomplete
open System.IO

[<Test>]
let TestProjectLibraryResolution () =
  let p = ProjectParser.load "../ProjectLoading/data/Test1.fsproj"
  Option.isSome p |> should be True
  let rs = p.Value.GetReferences
  rs |> should haveLength 4

[<Test>]
let Test2ndLevelDepsResolution () =
  let p  = ProjectParser.load "../ProjectLoading/data/Test2.fsproj"
  Option.isSome p |> should be True
  let rs = p.Value.GetReferences
  rs |> should haveLength 5
  let test1ok = Array.exists (fun (s:string) -> s.Replace("\\", "/").EndsWith("data/Test2/bin/Debug/Test1.dll")) rs
  test1ok |> should be True
