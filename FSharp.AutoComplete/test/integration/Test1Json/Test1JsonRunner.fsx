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
Threading.Thread.Sleep(8000)
p.completion "Script.fsx" 6 15
p.completion "Program.fs" 8 19
p.completion "Program.fs" 4 22
p.completion "Program.fs" 6 13
p.completion "Program.fs" 10 19
p.tooltip "FileTwo.fs" 9 6
p.tooltip "Program.fs" 6 15
p.tooltip "Program.fs" 4 8
p.tooltip "Script.fsx" 4 9
p.finddeclaration "Program.fs" 8 22
p.finddeclaration "Script.fsx" 6 15
p.declarations "Program.fs"
p.declarations "FileTwo.fs"
p.declarations "Script.fsx"
Threading.Thread.Sleep(1000)
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.json", output)

