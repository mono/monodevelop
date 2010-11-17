// --------------------------------------------------------------------------------------
// Simple monadic parser generator that we use in the IntelliSense
// --------------------------------------------------------------------------------------

module FSharp.Parser
#nowarn "40" // recursive references checked at runtime

open System
open Microsoft.FSharp.Collections

/// Add some useful methods for creating strings from sequences
type System.String with
  static member ofSeq s = new String(s |> Seq.toArray)
  static member ofReversedSeq s = new String(s |> Seq.toArray |> Array.rev)

/// Parser is implemented using lazy list (so that we can use seq<_>)  
type Parser<'T> = P of (LazyList<char> -> ('T * LazyList<char>) list)


// Basic functions needed by the computation builed

let result v = P(fun c -> [v, c])
let zero () = P(fun c -> [])
let bind (P p) f = P(fun inp ->
  [ for (pr, inp') in p inp do
      let (P pars) = f pr
      yield! pars inp' ])
let plus (P p) (P q) = P (fun inp ->
  (p inp) @ (q inp) )

type ParserBuilder() =
  member x.Bind(v, f) = bind v f
  member x.Zero() = zero()
  member x.Return(v) = result(v)
  member x.ReturnFrom(p) = p
  member x.Combine(a, b) = plus a b
  member x.Delay(f) = f()

let parser = new ParserBuilder()

// --------------------------------------------------------------------------------------
// Basic combinators for composing parsers

let item = P(function | LazyList.Nil -> [] | LazyList.Cons(c, r) -> [c,r])

let sequence (P p) (P q) = P (fun inp -> 
  [ for (pr, inp') in p inp do
      for (qr, inp'') in q inp' do 
        yield (pr, qr), inp''])

let sat p = parser { 
  let! v = item 
  if (p v) then return v }

let char x = sat ((=) x)
let digit = sat Char.IsDigit    
let lower = sat Char.IsLower
let upper = sat Char.IsUpper
let letter = sat Char.IsLetter
let nondigit = sat (Char.IsDigit >> not)
let whitespace = sat (Char.IsWhiteSpace)

let alphanum = parser { 
  return! letter
  return! digit }  

let rec word = parser {
  return []
  return! parser {
    let! x = letter
    let! xs = word
    return x::xs } }

let string (str:string) = 
  let chars = str.ToCharArray() |> List.ofSeq
  let rec string' = function
    | [] -> result []
    | x::xs -> parser { 
        let! y = char x
        let! ys = string' xs 
        return y::ys }
  string' chars

let rec many p = parser {
  return! parser { 
    let! it = p
    let! res = many p
    return it::res } 
  return [] }

let rec map f p = parser { 
  let! v = p 
  return f v }
             
let apply (P p) (str:seq<char>) = 
  let res = str |> LazyList.ofSeq |> p
  res |> List.map fst
