module FileTwo

type Foo =
  | Bar
  | Qux

let addition x y = x + y

let add x y = x + y

type NewObjectType() =

  member x.Terrific (y : int) : int =
    y

let plus x = addition x x x

let blast = unnamed

