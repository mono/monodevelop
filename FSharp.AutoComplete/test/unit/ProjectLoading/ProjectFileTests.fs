module ProjectFileTests

open NUnit.Framework
open FsUnit

open FSharp.InteractiveAutocomplete
open System.IO

[<Test>]
let TestProjectFiles () =
  let p = ProjectParser.load "../ProjectLoading/data/Test1.fsproj"
  Option.isSome p |> should be True
  let rs = p.Value.GetFiles |> Array.map Path.GetFileName |> set
  rs |> should haveCount 2
  rs |> should equal (set [ "Test1File1.fs"; "Test1File2.fs" ])

[<Test>]
let TestProjectFiles2 () =
  let p  = ProjectParser.load "../ProjectLoading/data/Test2.fsproj"
  Option.isSome p |> should be True
  let rs = p.Value.GetFiles |> Array.map Path.GetFileName |> set
  rs |> should haveCount 2
  rs |> should equal (set [ "Test2File1.fs"; "Test2File2.fs" ])
