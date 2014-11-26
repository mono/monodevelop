#load "../TestHelpers.fsx"
open TestHelpers
open System.IO
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
File.Delete "output.txt"

let p = new FSharpAutoCompleteWrapper()

p.project "Proj1/Proj1.fsproj"
p.parse "Proj1/Ops.fs"
p.parse "Proj1/Program.fs"
p.completion "Proj1/Program.fs" 8 19
p.completion "Proj1/Program.fs" 4 22
p.completion "Proj1/Program.fs" 6 13
p.completion "Proj1/Program.fs" 10 19

p.project "Proj2/Proj2.fsproj"
p.parse "Proj2/Core.fs"
p.parse "Proj2/Program.fs"
p.completion "Proj2/Program.fs" 8 19
p.completion "Proj2/Program.fs" 4 22
p.completion "Proj2/Program.fs" 6 13
p.completion "Proj2/Program.fs" 10 19

p.parse "Proj1/Ops.fs"
p.parse "Proj1/Program.fs"
p.completion "Proj1/Program.fs" 8 19
p.completion "Proj1/Program.fs" 4 22
p.completion "Proj1/Program.fs" 6 13
p.completion "Proj1/Program.fs" 10 19

p.parse "Proj2/Core.fs"
p.parse "Proj2/Program.fs"
p.completion "Proj2/Program.fs" 8 19
p.completion "Proj2/Program.fs" 4 22
p.completion "Proj2/Program.fs" 6 13
p.completion "Proj2/Program.fs" 10 19

p.send "quit\n"
let output = p.finalOutput ()
File.WriteAllText("output.txt", output)

