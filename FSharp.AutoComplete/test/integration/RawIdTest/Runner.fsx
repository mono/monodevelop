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

let p = new FSharpAutoCompleteWrapper()

p.parse "Test-Module.fsx"
p.parse "Test-Class.fsx"
p.completion "Test-Module.fsx" 9 2
p.completion "Test-Module.fsx" 11 12
p.completion "Test-Module.fsx" 13 13
p.completion "Test-Class.fsx" 9 2
p.completion "Test-Class.fsx" 11 12
p.completion "Test-Class.fsx" 13 13
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

