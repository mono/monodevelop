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
p.tooltip "Script.fsx" 6 13
p.tooltip "Script.fsx" 6 14
p.tooltip "Script.fsx" 6 15
p.tooltip "Script.fsx" 6 16
p.tooltip "Script.fsx" 6 17
p.tooltip "Script.fsx" 6 18
p.tooltip "Script.fsx" 6 19
p.tooltip "Script.fsx" 6 20
p.tooltip "Script.fsx" 6 21
p.tooltip "Script.fsx" 6 22
p.tooltip "Script.fsx" 6 23
p.tooltip "Script.fsx" 6 24
p.tooltip "Script.fsx" 6 25
p.tooltip "Script.fsx" 6 26
p.tooltip "Script.fsx" 8 6
p.tooltip "Script.fsx" 8 10
p.tooltip "Script.fsx" 8 15
p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

