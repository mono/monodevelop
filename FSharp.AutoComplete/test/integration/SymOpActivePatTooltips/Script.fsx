

let (|<>|) x y = x + y
let (|..|) x y = x + y

let val99 = 1 |<>| 1 |..| 1

let (|Zero|Succ|) n = if n = 0 then Zero else Succ(n-1)
