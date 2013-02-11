module Test2File1

[<EntryPoint>]
let main args =
  printfn "Hello world (%d, %d, %d)"
    Test2File2.variable3
    Test1File1.variable2
    Test1File2.variable1
  0
