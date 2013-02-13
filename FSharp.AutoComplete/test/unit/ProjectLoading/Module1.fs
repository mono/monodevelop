module Module1

open Xunit

open FSharp.InteractiveAutocomplete
open System.IO

[<Fact>]
let TestProjectLibraryResolution () =
  let p  = ProjectParser.load "../../data/Test1.fsproj"
  Assert.True(Option.isSome p)
  let rs = ProjectParser.getReferences p.Value
  Assert.True(4  = rs.Length)

[<Fact>]
let Test2ndLevelDepsResolution () =
  let p  = ProjectParser.load "../../data/Test2.fsproj"
  Assert.True(Option.isSome p)
  let rs = ProjectParser.getReferences p.Value
  Assert.True(6  = rs.Length, sprintf "%A" rs)
  Assert.True(Array.exists (fun (r: string) -> r.Contains("Test1.dll")) rs)
  

