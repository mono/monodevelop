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

p.project "FindDecl.fsproj"
p.parse "FileTwo.fs"
p.parse "Script.fsx"
p.parse "Program.fs"
p.finddeclaration "Program.fs" 5 15
p.finddeclaration "Program.fs" 7 19
p.finddeclaration "Program.fs" 13 25
p.finddeclaration "Program.fs" 9 19
p.finddeclaration "Script.fsx" 5 16
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

