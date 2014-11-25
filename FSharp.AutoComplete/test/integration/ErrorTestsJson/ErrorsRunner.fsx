#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "output.txt"

let p = new FSharpAutoCompleteWrapper()

p.send "outputmode json\n"
p.project "Test1.fsproj"
p.parse "Program.fs"
p.parse "Script.fsx"
Threading.Thread.Sleep(8000)
p.completion "Program.fs" 6 13
p.completion "Script.fsx" 6 13
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.json", output)

