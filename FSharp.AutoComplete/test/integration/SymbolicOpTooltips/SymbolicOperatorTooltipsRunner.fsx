#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

(*
 * This test runs through tooltips for symbolic operators
 *
 *)

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "output.txt"

let p = new FSharpAutoCompleteWrapper()

p.parse "Script.fsx"
p.tooltip "Script.fsx" 5 13
p.tooltip "Script.fsx" 5 14
p.tooltip "Script.fsx" 5 15
p.tooltip "Script.fsx" 5 16
p.tooltip "Script.fsx" 5 17
p.tooltip "Script.fsx" 5 18
p.tooltip "Script.fsx" 5 19
p.tooltip "Script.fsx" 5 20
p.tooltip "Script.fsx" 5 21
p.tooltip "Script.fsx" 5 22
p.tooltip "Script.fsx" 5 23
p.tooltip "Script.fsx" 5 24
p.tooltip "Script.fsx" 5 25
p.tooltip "Script.fsx" 5 26
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

