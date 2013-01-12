#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "output.txt"

let p = new FSharpAutoCompleteWrapper()

p.project "Test1.fsproj"
p.parse "FileTwo.fs"
p.parse "Script.fsx"
p.parse "Program.fs"
p.completion "Script.fsx" 5 13
p.completion "Program.fs" 7 19
p.completion "Program.fs" 3 22
p.completion "Program.fs" 5 13
p.send "errors\n"
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)
#q;;
