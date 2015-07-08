namespace MonoDevelop.FSharp
open System
open System.IO
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

  ///Helper to return an IDisposable for a event on subscribe
  type IDelegateEvent<'Del when 'Del :> Delegate> with
    member this.Subscribe handler =
      do this.AddHandler(handler)
      { new IDisposable with 
          member x.Dispose() =
            this.RemoveHandler(handler) }

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

  let ofString (s:string) =
    if String.isNullOrEmpty s then None
    else Some s