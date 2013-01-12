#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "completebadposition.txt"

let p = new FSharpAutoCompleteWrapper()

p.project "Project/Test1.fsproj"
p.parse "Program.fs"
p.completion "Program.fs" 50 0
p.completion "Program.fs" 1 100
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("completebadposition.txt", output)
#q;;
