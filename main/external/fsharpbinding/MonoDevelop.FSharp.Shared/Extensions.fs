namespace MonoDevelop.FSharp.Shared
open System
open System.Text
open System.Threading.Tasks
open System.IO
open Microsoft.FSharp.Compiler.SourceCodeServices
open ExtCore

module Seq =
    let tryHead items =
        if Seq.isEmpty items then None else Some (Seq.head items)
        
module List =
    
    ///Returns the greatest of all elements in the list that is less than the threshold
    let maxUnderThreshold nmax =
        List.maxBy(fun n -> if n > nmax then 0 else n)
        
module Option =
    let inline getOrElse f o =
        match o with
        | Some v -> v
        | _      -> f()

    let inline tryCast<'T> (o: obj): 'T option =
        match o with
        | null -> None
        | :? 'T as a -> Some a
        | _ -> None

    let inline cast o =
        (Option.bind tryCast) o

    /// Convert string into Option string where null and String.Empty result in None
    let inline ofString (s:string) =
        if String.isNullOrEmpty s then None
        else Some s

    /// Some(Some x) -> Some x | None -> None
    let inline flatten x =
        match x with
        | Some x -> x
        | None -> None

    /// Gets the option if Some x, otherwise try to get another value
    let inline orTry f =
        function
        | Some x -> Some x
        | None -> f()

module String =
  /// Split a line so it fits to a line width
  let splitLine (sb : StringBuilder) (line : string) lineWidth =
      let emit (s : string) = sb.Append(s) |> ignore

      let indent =
          line
          |> Seq.takeWhile (fun c -> c = ' ')
          |> Seq.length

      let words = line.Split(' ')
      let mutable i = 0
      let mutable first = true
      for word in words do
          if first || i + word.Length < lineWidth then
              emit word
              emit " "
              i <- i + word.Length + 1
              first <- false
          else
              sb.AppendLine() |> ignore
              for i in 1..indent do
                  emit " "
              emit word
              emit " "
              i <- indent + word.Length + 1
              first <- true
      sb.AppendLine() |> ignore

  /// Wrap text so it fits to a line width
  let wrapText (text : String) lineWidth =
        //dont wrap empty lines
        if text.Length = 0 then text
        else
            let sb = StringBuilder()
            let lines = text.Split [| '\r'; '\n' |]
            for line in lines do
                if line.Length <= lineWidth then sb.AppendLine(line) |> ignore
                else splitLine sb line lineWidth
            sb.ToString()

  let inline isNotNull v = not (isNull v)
  let getLines (str: string) =
      use reader = new StringReader(str)
      [|
          let line = ref (reader.ReadLine())
          while isNotNull (!line) do
              yield !line
              line := reader.ReadLine()
          if str.EndsWith("\n") then
              // last trailing space not returned
              // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
              yield String.Empty
      |]

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

        member x.XmlDocSig =
            match x with
            | :? FSharpMemberOrFunctionOrValue as func -> func.XmlDocSig
            | :? FSharpEntity as fse -> fse.XmlDocSig
            | :? FSharpField as fsf -> fsf.XmlDocSig
            | :? FSharpUnionCase as fsu -> fsu.XmlDocSig
            | :? FSharpActivePatternCase as apc -> apc.XmlDocSig
            | :? FSharpGenericParameter -> ""
            | _ -> ""

    type FSharpMemberOrFunctionOrValue with
      // FullType may raise exceptions (see https://github.com/fsharp/fsharp/issues/307).
        member x.FullTypeSafe = Option.attempt (fun _ -> x.FullType)
        member x.IsConstructor = x.CompiledName = ".ctor"
        member x.IsOperatorOrActivePattern =
            let name = x.DisplayName
            if name.StartsWith "( " && name.EndsWith " )" && name.Length > 4
            then name.Substring (2, name.Length - 4) |> String.forall (fun c -> c <> ' ')
            else false

    type FSharpEntity with
        member x.TryGetFullName() =
            Option.attempt (fun _ -> x.TryFullName)
            |> Option.flatten
            |> Option.orTry (fun _ -> Option.attempt (fun _ -> String.Join(".", x.AccessPath, x.DisplayName)))

        member x.TryGetFullNameWithUnderScoreTypes() =
            try
                let name = String.Join(".", x.AccessPath, x.DisplayName)
                if x.GenericParameters.Count > 0 then
                  Some (name + "<" + String.concat "," (x.GenericParameters |> Seq.map (fun gp -> gp.DisplayName)) + ">")
                else Some name
            with _ -> None

        member x.UnAnnotate() =
            let rec realEntity (s:FSharpEntity) =
                if s.IsFSharpAbbreviation && s.AbbreviatedType.HasTypeDefinition
                then realEntity s.AbbreviatedType.TypeDefinition
                else s
            realEntity x

        member x.InheritanceDepth() =
            let rec loop (ent:FSharpEntity) l =
                match ent.BaseType with
                | Some bt -> loop (bt.TypeDefinition.UnAnnotate()) l + 1
                | None -> l
            loop x 0
         
        //TODO: Do we need to unannotate like above?   
        member x.AllBaseTypes =
            let rec allBaseTypes (entity:FSharpEntity) =
                [
                    match entity.TryFullName with
                    | Some _ ->
                        match entity.BaseType with
                        | Some bt ->
                            yield bt
                            if bt.HasTypeDefinition then
                                yield! allBaseTypes bt.TypeDefinition
                        | _ -> ()
                    | _ -> ()
                ]
            allBaseTypes x

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

    /// Add some useful methods for creating strings from sequences
    type System.String with
        static member ofSeq s = new String(s |> Seq.toArray)
        static member ofReversedSeq s = new String(s |> Seq.toArray |> Array.rev)

    ///Helper to return an IDisposable for a event on subscribe
    type IDelegateEvent<'Del when 'Del :> Delegate> with
        member this.Subscribe handler =
            do this.AddHandler(handler)
            { new IDisposable with
                  member x.Dispose() =
                      this.RemoveHandler(handler) }

[<AutoOpen>]
module ConstraintExt =
    type FSharpGenericParameterMemberConstraint with
        member x.IsProperty =
            (x.MemberIsStatic && x.MemberArgumentTypes.Count = 0) ||
            (not x.MemberIsStatic && x.MemberArgumentTypes.Count = 1)

module AsyncChoice =
    let inline ofOptionWith e =
        Control.Async.map (Choice.ofOptionWith e)

[<AutoOpen>]
module AsyncChoiceCE =
    type ExtCore.Control.AsyncChoiceBuilder with
        member inline __.ReturnFrom (choice : Choice<'T, 'Error>) : Async<Choice<'T, 'Error>> = async.Return choice

        member inline __.Bind (value : Choice<'T, 'Error>, binder : 'T -> Async<Choice<'U, 'Error>>) : Async<Choice<'U, 'Error>> =
            async {
                match value with
                | Error error -> return Choice2Of2 error
                | Success x -> return! binder x
            }

module Async =
    let inline startAsPlainTask (work : Async<unit>) =
        System.Threading.Tasks.Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)

    let inline awaitPlainTask (task: Task) = 
        let continuation (t : Task) : unit =
            if t.IsFaulted then raise t.Exception
        task.ContinueWith continuation |> Async.AwaitTask

[<AutoOpen>]
module AsyncTaskBind =
    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(computation:Task<'T>, binder:'T -> Async<'R>) =  x.Bind(Async.AwaitTask computation, binder)
        member x.ReturnFrom(computation:Task<'T>) = x.ReturnFrom(Async.AwaitTask computation)
        member x.Bind(computation:Task, binder:unit -> Async<unit>) =  x.Bind(Async.awaitPlainTask computation, binder)
        member x.ReturnFrom(computation:Task) = x.ReturnFrom(Async.awaitPlainTask computation)