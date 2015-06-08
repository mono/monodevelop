namespace MonoDevelop.FSharp
open System
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AutoOpen>]
module FSharpSymbolExt =
  type FSharpSymbol with
    member x.IsSymbolLocalForProject = 
        match x with 
        | :? FSharpParameter -> true
        | :? FSharpMemberOrFunctionOrValue as m -> not m.IsModuleValueOrMember || not m.Accessibility.IsPublic
        | :? FSharpEntity as m -> not m.Accessibility.IsPublic
        | :? FSharpGenericParameter -> true
        | :? FSharpUnionCase as m -> not m.Accessibility.IsPublic
        | :? FSharpField as m -> not m.Accessibility.IsPublic
        | _ -> false


  type FSharpMemberOrFunctionOrValue with
    // FullType may raise exceptions (see https://github.com/fsharp/fsharp/issues/307). 
    member x.FullTypeSafe = Option.attempt (fun _ -> x.FullType)

  type FSharpEntity with
      member x.TryGetFullName() =
        match Option.attempt (fun _ -> x.TryFullName) with
        | Some x -> x
        | None -> Some (String.Join(".", x.AccessPath, x.DisplayName))


open System.IO
[<AutoOpen>]
module FrameworkExt =
  type Path with
    static member GetFullPathSafe path =
      try Path.GetFullPath path
      with _ -> path

  type Text.StringBuilder with
    ///Apply a predicate to the last character of the StringBuilder instance
    member x.LastCharacterIs f =
      if x.Length > 0 then f x.[x.Length - 1]
      else false

    ///Apply a predicate to the first character of the StringBuilder instance
    member x.FirstCharacterIs f =
      if x.Length > 0 then f x.[0]
      else false

module Option =
  let getOrElse f o = 
    match o with
    | Some v -> v
    | _      -> f()

  let tryCast<'T> (o: obj): 'T option = 
    match o with
    | null -> None
    | :? 'T as a -> Some a
    | _ -> None