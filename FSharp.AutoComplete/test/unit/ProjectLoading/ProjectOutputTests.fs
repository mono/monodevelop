module ProjectOutputTests

open NUnit.Framework
open FsUnit

open FSharp.InteractiveAutocomplete
open System.IO

[<Test>]
let TestProjectLibraryResolution () =
  let p  = ProjectParser.load "../ProjectLoading/data/Test1.fsproj"
  Option.isSome p |> should be True
  let fs = ProjectParser.getOutput p.Value
  fs |> should equal (Path.GetFullPath "../ProjectLoading/data/bin/Debug/Test1.dll")


