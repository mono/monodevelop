#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "output.txt"

let p = new FSharpAutoCompleteWrapper()

p.project "Test1.fsproj"
p.parse "Program.fs"
p.completion "Program.fs" 6 13
p.parse "Script.fsx"
p.completion "Script.fsx" 6 13
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

