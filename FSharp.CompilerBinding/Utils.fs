namespace FSharp.CompilerBinding

// This code borrowed from https://github.com/fsprojects/VisualFSharpPowerTools/

open System

[<RequireQualifiedAccess>]
module Seq =
    let tryHead s =
        if Seq.isEmpty s then None else Some (Seq.head s)

[<RequireQualifiedAccess>]
module Option =
    let inline ofNull value =
        if obj.ReferenceEquals(value, null) then None else Some value

    let inline ofNullable (value: Nullable<'a>) =
        if value.HasValue then Some value.Value else None

    let inline toNullable (value: 'a option) =
        match value with
        | Some x -> Nullable<_> x
        | None -> Nullable<_> ()

    let inline attempt (f: unit -> 'a) = try Some <| f() with _ -> None

    /// Gets the value associated with the option or the supplied default value.
    let inline getOrElse v =
        function
        | Some x -> x
        | None -> v

    /// Gets the option if Some x, otherwise the supplied default value.
    let inline orElse v =
        function
        | Some x -> Some x
        | None -> v

// Async helper functions copied from https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/ControlCollections.Async.fs
[<RequireQualifiedAccess>]
module Async =
    /// Transforms an Async value using the specified function.
    [<CompiledName("Map")>]
    let map (mapping : 'T -> 'U) (value : Async<'T>) : Async<'U> =
        async {
            // Get the input value.
            let! x = value
            // Apply the mapping function and return the result.
            return mapping x
        }

    [<RequireQualifiedAccess>]    
    module Array =
        /// Async implementation of Array.map.
        let map (mapping : 'T -> Async<'U>) (array : 'T[]) : Async<'U[]> =
            let len = Array.length array
            let result = Array.zeroCreate len

            async {
                // Apply the mapping function to each array element.
                for i in 0 .. len - 1 do
                    let! mappedValue = mapping array.[i]
                    result.[i] <- mappedValue

                // Return the completed results.
                return result
            }


/// Maybe computation expression builder, copied from ExtCore library
/// https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/Control.fs
[<Sealed>]
type MaybeBuilder () =
    // 'T -> M<'T>
    member inline x.Return value: 'T option =
        Some value

    // M<'T> -> M<'T>
    member inline x.ReturnFrom value: 'T option =
        value

    // unit -> M<'T>
    member inline x.Zero (): unit option =
        Some ()     // TODO: Should this be None?

    // (unit -> M<'T>) -> M<'T>
    member x.Delay (f: unit -> 'T option): 'T option =
        f ()

    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    member inline x.Combine (r1, r2: 'T option): 'T option =
        match r1 with
        | None ->
            None
        | Some () ->
            r2

    // M<'T> * ('T -> M<'U>) -> M<'U>
    member inline x.Bind (value, f: 'T -> 'U option): 'U option =
        Option.bind f value

    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    member x.Using (resource: ('T :> System.IDisposable), body: _ -> _ option): _ option =
        try body resource
        finally
            if not <| obj.ReferenceEquals (null, box resource) then
                resource.Dispose ()

    // (unit -> bool) * M<'T> -> M<'T>
    member x.While (guard, body: _ option): _ option =
        if guard () then
            // OPTIMIZE: This could be simplified so we don't need to make calls to Bind and While.
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    // seq<'T> * ('T -> M<'U>) -> M<'U>
    // or
    // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
    member x.For (sequence: seq<_>, body: 'T -> unit option): _ option =
        // OPTIMIZE: This could be simplified so we don't need to make calls to Using, While, Delay.
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (
                enum.MoveNext,
                x.Delay (fun () ->
                    body enum.Current)))


[<Sealed>]
type AsyncMaybeBuilder () =
    // 'T -> M<'T>
    member (*inline*) x.Return value : Async<'T option> = Some value |> async.Return

    // M<'T> -> M<'T>
    member (*inline*) x.ReturnFrom value : Async<'T option> = value

    // unit -> M<'T>
    member (*inline*) x.Zero () : Async<unit option> =
        Some ()     // TODO : Should this be None?
        |> async.Return

    // (unit -> M<'T>) -> M<'T>
    member x.Delay (f : unit -> Async<'T option>) : Async<'T option> = f ()

    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    member (*inline*) x.Combine (r1, r2 : Async<'T option>) : Async<'T option> =
        async {
            let! r1' = r1
            match r1' with
            | None -> return None
            | Some () -> return! r2
        }

    // M<'T> * ('T -> M<'U>) -> M<'U>
    member (*inline*) x.Bind (value, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            let! value' = value
            match value' with
            | None -> return None
            | Some result -> return! f result
        }
    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    member x.Using (resource : ('T :> IDisposable), body : _ -> Async<_ option>) : Async<_ option> =
        try body resource
        finally 
            if resource <> null then resource.Dispose ()

    // (unit -> bool) * M<'T> -> M<'T>
    member x.While (guard, body : Async<_ option>) : Async<_ option> =
        if guard () then
            // OPTIMIZE : This could be simplified so we don't need to make calls to Bind and While.
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    // seq<'T> * ('T -> M<'U>) -> M<'U>
    // or
    // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
    member this.For (sequence : seq<_>, body : 'T -> Async<unit option>) : Async<_ option> =
        // OPTIMIZE : This could be simplified so we don't need to make calls to Using, While, Delay.
        this.Using (sequence.GetEnumerator (), fun enum ->
            this.While (
                enum.MoveNext,
                this.Delay (fun () ->
                    body enum.Current)))

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncMaybe =
    let liftMaybe (maybe: Option<'T>) : Async<_ option> =
        async { return maybe }

    let inline liftAsync (async : Async<'T>) : Async<_ option> =
        async |> Async.map Some

[<RequireQualifiedAccess>]
module String =
    let lowerCaseFirstChar (str: string) =
        match str with
        | null -> null
        | str ->
            match str.ToCharArray() |> Array.toList with
            | [] -> str
            | h :: t when Char.IsUpper h -> String (Char.ToLower h :: t |> List.toArray)
            | _ -> str

    let extractTrailingIndex (str: string) =
        match str with
        | null -> null, None
        | _ ->
            str 
            |> Seq.toList 
            |> List.rev 
            |> Seq.takeWhile Char.IsDigit 
            |> Seq.toArray 
            |> Array.rev
            |> fun chars -> String(chars)
            |> function
               | "" -> str, None
               | index -> str.Substring (0, str.Length - index.Length), Some (int index)

[<AutoOpen; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module Pervasive =
    let inline (===) a b = LanguagePrimitives.PhysicalEquality a b
    let inline debug msg = Printf.kprintf System.Diagnostics.Debug.WriteLine msg
    let inline fail msg = Printf.kprintf System.Diagnostics.Debug.Fail msg
    let maybe = MaybeBuilder()
    let asyncMaybe = AsyncMaybeBuilder()
    
    let tryCast<'a> (o: obj): 'a option = 
        match o with
        | null -> None
        | :? 'a as a -> Some a
        | _ -> fail "Cannot cast %O to %O" (o.GetType()) typeof<'a>.Name; None

    /// Load times used to reset type checking properly on script/project load/unload. It just has to be unique for each project load/reload.
    /// Not yet sure if this works for scripts.
    let fakeDateTimeRepresentingTimeLoaded x = DateTime(abs (int64 (match x with null -> 0 | _ -> x.GetHashCode())) % 103231L)

    open System.Threading
    
    let synchronize f = 
        let ctx = SynchronizationContext.Current
        
        let thread = 
            match ctx with
            | null -> null // saving a thread-local access
            | _ -> Thread.CurrentThread
        f (fun g arg -> 
            let nctx = SynchronizationContext.Current
            match ctx, nctx with
            | null, _ -> g arg
            | _, _ when Object.Equals(ctx, nctx) && thread.Equals(Thread.CurrentThread) -> g arg
            | _ -> ctx.Post((fun _ -> g (arg)), null))

    type Microsoft.FSharp.Control.Async with
        static member EitherEvent(ev1: IObservable<'a>, ev2: IObservable<'b>) = 
            synchronize (fun f -> 
                Async.FromContinuations((fun (cont, _econt, _ccont) -> 
                    let rec callback1 = 
                        (fun value -> 
                        remover1.Dispose()
                        remover2.Dispose()
                        f cont (Choice1Of2(value)))
                    
                    and callback2 = 
                        (fun value -> 
                        remover1.Dispose()
                        remover2.Dispose()
                        f cont (Choice2Of2(value)))
                    
                    and remover1: IDisposable = ev1.Subscribe(callback1)
                    and remover2: IDisposable = ev2.Subscribe(callback2)
                    ())))

    type Atom<'T when 'T: not struct>(value: 'T) = 
        let refCell = ref value
        
        let rec swap f = 
            let currentValue = !refCell
            let result = Interlocked.CompareExchange<'T>(refCell, f currentValue, currentValue)
            if obj.ReferenceEquals(result, currentValue) then result
            else 
                Thread.SpinWait 20
                swap f
        
        member self.Value with get () = !refCell
        member self.Swap(f: 'T -> 'T) = swap f

    open System.IO

    type Path with
        static member GetFullPathSafe path =
            try Path.GetFullPath path
            with _ -> path

        static member GetFileNameSafe path =
            try Path.GetFileName path
            with _ -> path