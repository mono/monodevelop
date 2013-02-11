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

p.project "Timeouts.fsproj"
p.parse "Program.fs"
p.send "completion \"Program.fs\" 7 19 500\n"
p.send "tooltip \"Program.fs\" 7 19 200\n"
p.send "completion \"Program.fs\" 7 19 2000\n"
p.completion "Program.fs" 7 19
p.send "tooltip \"Program.fs\" 7 19 200\n"
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

