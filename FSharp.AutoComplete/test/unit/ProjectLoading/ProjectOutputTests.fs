module ProjectOutputTests

open NUnit.Framework
open FsUnit

open FSharp.InteractiveAutocomplete
open System.IO

[<Test>]
let TestProjectLibraryResolution () =
  let p = ProjectParser.load "../ProjectLoading/data/Test1.fsproj"
  Option.isSome p |> should be True
  let fs = p.Value.Output
  fs |> should equal (Path.GetFullPath "../ProjectLoading/data/Test1/bin/Debug/Test1.dll")


