namespace FSharp.CompilerBinding

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