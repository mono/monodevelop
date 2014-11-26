module FileTwo

type Foo =
  | Bar
  | Qux

let addition x y = x + y

let add x y = x + y

type NewObjectType() =

  member x.Terrifying (y : int) : int =
    y
