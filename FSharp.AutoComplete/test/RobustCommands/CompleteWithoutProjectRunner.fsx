#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "completewithoutproject.txt"

let p = new FSharpAutoCompleteWrapper()

p.parse "Script.fsx"
p.completion "Script.fsx" 5 13
p.parse "Program.fs"
p.completion "Program.fs" 3 22
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("completewithoutproject.txt", output)
#q;;
