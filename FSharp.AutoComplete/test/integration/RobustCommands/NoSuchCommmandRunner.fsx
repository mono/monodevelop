#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "nosuchcommand.txt"

let p = new FSharpAutoCompleteWrapper()

p.send "BadCommand\n"
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("nosuchcommand.txt", output)

