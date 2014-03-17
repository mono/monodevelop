#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "completewithoutproject.txt"

let p = new FSharpAutoCompleteWrapper()

p.parse "Project/Script.fsx"
p.completion "Project/Script.fsx" 6 13
p.parse "Project/Program.fs"
p.completion "Project/Program.fs" 4 22
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("completewithoutproject.txt", output)

