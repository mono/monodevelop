#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

(*
 * This test is a simple sanity check of a basic run of the program.
 * A few completions, files and script.
 *)

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "output.txt"

if not (File.Exists "FSharp.Data/lib/net40/FSharp.Data.dll") then
  installNuGetPkg "FSharp.Data"

let p = new FSharpAutoCompleteWrapper()

p.parse "Test.fsx"
p.completion "Test.fsx" 5 19
p.completion "Test.fsx" 7 32
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

