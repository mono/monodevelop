namespace MonoDevelop.FSharp

open System
open Mono.Cecil

module Object =
    let eqHack (f: 'a -> 'b) (x: 'a) (yobj: Object) : Boolean =
        match yobj with
        | :? 'a as y -> f x = f y
        | _          -> false

    let compHack (f: 'a -> 'b) (x: 'a) (yobj: Object) : Int32 =
        match yobj with
        | :? 'a as y -> compare (f x) (f y)
        | _          -> invalidArg "yobj" "Cannot compare elements of incompatible types"

type Digraph<'n> when 'n : comparison =
    Map<'n, Set<'n>>

module Digraph =

    let addNode (n: 'n) (g: Digraph<'n>) : Digraph<'n> =
        match Map.tryFind n g with
        | None -> Map.add n Set.empty g
        | Some _ -> g

    let addEdge ((n1, n2): 'n * 'n) (g: Digraph<'n>) : Digraph<'n> =
        let g' =
            match Map.tryFind n2 g with
            | None -> addNode n2 g
            | Some _ -> g
        match Map.tryFind n1 g with
        | None -> Map.add n1 (Set.singleton n2) g'
        | Some ns -> Map.add n1 (Set.add n2 ns) g'

    let nodes (g: Digraph<'n>) : List<'n> =
        Map.fold (fun xs k _ -> k::xs) [] g

    let roots (g: Digraph<'n>) : List<'n> =
        List.filter (fun n -> not (Map.exists (fun _ v -> Set.contains n v) g)) (nodes g)

    let topSort (h: Digraph<'n>) : List<'n> =
        let rec dfs (g: Digraph<'n>, order: List<'n>, rts: List<'n>) : List<'n> =
            if List.isEmpty rts then
                order
            else
                let n = List.head rts
                let order' = n::order
                let g' = Map.remove n g
                let rts' = roots g'
                dfs (g', order', rts')
        dfs (h, [], roots h)

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
        ordering
