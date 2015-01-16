module Test2.Main

let val2 = List.map ((+) 1) [1;2]

[<EntryPoint>]
let main args =
    printfn "Hello %A" val2
    0
