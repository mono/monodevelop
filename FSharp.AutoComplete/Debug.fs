module Debug
  
  let verbose = ref false
  let categories : Ref<Option<Set<string>>> =
    ref None


  [<Sealed>]
  type Format<'T> private () =
    static let rec mkKn (ty: System.Type) =
      if Reflection.FSharpType.IsFunction(ty) then
          let _, ran = Reflection.FSharpType.GetFunctionElements(ty)
          let f = mkKn ran
          Reflection.FSharpValue.MakeFunction(ty, fun _ -> f)
      else
          box ()

    static let instance : 'T =
      unbox (mkKn typeof<'T>)
    static member Instance = instance

  let inline print (fmt: Printf.TextWriterFormat<'a>) : 'a =
    if !verbose then
      printfn fmt
    else
      Format<_>.Instance

  let inline printc cat fmt =
    if !verbose && (match !categories with
                    | None -> true
                    | Some c -> Set.contains cat c) then
      printf "[%s] " cat
      printfn fmt
    else
      Format<_>.Instance
