module ProjectLoadingTests

open NUnit.Framework
open FsUnit

open FSharp.InteractiveAutocomplete
open System.IO

[<Test>]
let TestProjectLoadDoesNotExist () =
  let p = ProjectParser.load "not_there.fsproj"
  p |> should equal None

[<Test>]
let TestProjectLoadMalformed () =
  let p = ProjectParser.load "../ProjectLoading/data/malformed.fsproj"
  p |> should equal None

