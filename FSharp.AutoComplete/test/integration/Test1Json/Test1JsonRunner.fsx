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

p.send "outputmode json\n"
p.project "Test1.fsproj"
p.parse "FileTwo.fs"
p.parse "Script.fsx"
p.parse "Program.fs"
p.completion "Script.fsx" 5 15
p.completion "Program.fs" 7 19
p.completion "Program.fs" 3 22
p.completion "Program.fs" 5 13
p.completion "Program.fs" 9 19
p.tooltip "FileTwo.fs" 8 6
p.tooltip "Program.fs" 5 15
p.tooltip "Program.fs" 3 8
p.tooltip "Script.fsx" 3 9
p.declarations "Program.fs"
p.declarations "FileTwo.fs"
p.declarations "Script.fsx"
p.send "errors\n"
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

