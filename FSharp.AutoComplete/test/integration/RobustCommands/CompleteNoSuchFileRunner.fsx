#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "completenosuchfile.txt"

let p = new FSharpAutoCompleteWrapper()

p.project "Project/Test1.fsproj"
p.completion "NoSuchFile.fs" 0 0
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("completenosuchfile.txt", output)

