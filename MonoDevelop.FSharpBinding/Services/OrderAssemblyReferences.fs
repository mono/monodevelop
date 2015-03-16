namespace MonoDevelop.FSharp

open System.Reflection
open System
open Mono.Cecil
open FSharp.CompilerBinding

module Object = 
  let eqHack (f: 'a -> 'b) (x: 'a) (yobj: Object) : Boolean =
    match yobj with
    | :? 'a as y -> f x = f y
    | _          -> false

  let compHack (f: 'a -> 'b) (x: 'a) (yobj: Object) : Int32 =
    match yobj with
    | :? 'a as y -> compare (f x) (f y)
    | _          -> invalidArg "yobj" "Cannot compare elements of incompatible types"

module List =
  let toStringWithDelims (fr: String) (sep: String) (bk: String) (xs: List<'a>) : String =
    let rec toSWD acc ys =
      match ys with
      | []       -> acc
      | [z]      -> sprintf "%s%A" acc z
      | y::z::zs -> toSWD (sprintf "%s%A%s" acc y sep) (z::zs)
    fr + toSWD "" xs + bk

[<CustomEquality>]
[<CustomComparison>]
[<StructuredFormatDisplay("{show}")>]
type AssemblyRef =
  {
    Path: String
    Assembly: AssemblyDefinition
    Name: String
  }

  member this.show = this.ToString ()

  override this.Equals (obj: Object) : bool =
    Object.eqHack (fun (a:AssemblyRef) -> a.Name) this obj

  override this.GetHashCode () = 
    hash this.Name
  
  interface System.IComparable with 
    member this.CompareTo (obj: Object) =
      Object.compHack (fun (p:AssemblyRef) -> p.Name) this obj

  override x.ToString () = x.Path

[<Serializable>]
type OrderAssemblyReferences () =

  let mkGraph (seeds: seq<AssemblyRef>) : Digraph<AssemblyRef> =

    let findRef (s: seq<AssemblyRef>) (m: AssemblyNameReference) =
      match Seq.tryFind (fun r -> r.Name = m.FullName) seeds with
      | None    -> s
      | Some ar -> Seq.append (Seq.singleton ar) s

    let processNode (g: Digraph<AssemblyRef>) (n: AssemblyRef) =
      let depNames = n.Assembly.MainModule.AssemblyReferences.ToArray()
      let depRefs = Array.fold findRef Seq.empty depNames
      Seq.fold (fun h c -> Digraph.addEdge (n, c) h) g depRefs

    let rec fixpoint (g: Digraph<AssemblyRef>) =
      let ns = Digraph.nodes g
      let g' = List.fold processNode g ns
      if g = g' then g else fixpoint g'

    fixpoint (Seq.fold (fun g s -> Digraph.addNode s g) Map.empty seeds)

  let mkAssemblyRef (t: String) =
    let assemblyDefinition = Mono.Cecil.AssemblyDefinition.ReadAssembly(t)
    {
      Path = t
      Assembly = assemblyDefinition
      Name = assemblyDefinition.FullName
    }

  ///Orders the passed in array of assembly references in dependency order
  member x.Order(rs: String[]) = 
    let asmRefs = Array.map mkAssemblyRef rs
    let graph = mkGraph asmRefs
    let ordering = Digraph.topSort graph
    let str = List.toStringWithDelims "#r @\"" "\"\n#r @\"" "\"" ordering
    str 
    