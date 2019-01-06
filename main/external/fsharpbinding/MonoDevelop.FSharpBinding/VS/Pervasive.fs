﻿[<AutoOpen>]
module MonoDevelop.FSharp.Pervasive

open System
open System.Diagnostics

[<RequireQualifiedAccess>]
module String =   
    open System.IO

    let getLines (str: string) =
        use reader = new StringReader(str)
        [|  let mutable line = reader.ReadLine()
            while not (isNull line) do
                yield line
                line <- reader.ReadLine()
            if str.EndsWith("\n") then
            // last trailing space not returned
            // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
                yield String.Empty
        |]


type System.IServiceProvider with
    member x.GetService<'T>() = x.GetService(typeof<'T>) :?> 'T
    member x.GetService<'S, 'T>() = x.GetService(typeof<'S>) :?> 'T

[<Sealed>]
type MaybeBuilder () =
    // 'T -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Return value: 'T option =
        Some value

    // M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.ReturnFrom value: 'T option =
        value

    // unit -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Zero (): unit option =
        Some ()     // TODO: Should this be None?

    // (unit -> M<'T>) -> M<'T>
    [<DebuggerStepThrough>]
    member __.Delay (f: unit -> 'T option): 'T option =
        f ()

    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Combine (r1, r2: 'T option): 'T option =
        match r1 with
        | None ->
            None
        | Some () ->
            r2

    // M<'T> * ('T -> M<'U>) -> M<'U>
    [<DebuggerStepThrough>]
    member inline __.Bind (value, f: 'T -> 'U option): 'U option =
        Option.bind f value

    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    [<DebuggerStepThrough>]
    member __.Using (resource: ('T :> System.IDisposable), body: _ -> _ option): _ option =
        try body resource
        finally
            if not <| obj.ReferenceEquals (null, box resource) then
                resource.Dispose ()

    // (unit -> bool) * M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member x.While (guard, body: _ option): _ option =
        if guard () then
            // OPTIMIZE: This could be simplified so we don't need to make calls to Bind and While.
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    // seq<'T> * ('T -> M<'U>) -> M<'U>
    // or
    // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
    [<DebuggerStepThrough>]
    member x.For (sequence: seq<_>, body: 'T -> unit option): _ option =
        // OPTIMIZE: This could be simplified so we don't need to make calls to Using, While, Delay.
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (
                enum.MoveNext,
                x.Delay (fun () ->
                    body enum.Current)))

let maybe = MaybeBuilder()

[<Sealed>]
type AsyncMaybeBuilder () =
    [<DebuggerStepThrough>]
    member __.Return value : Async<'T option> = Some value |> async.Return

    [<DebuggerStepThrough>]
    member __.ReturnFrom value : Async<'T option> = value

    [<DebuggerStepThrough>]
    member __.ReturnFrom (value: 'T option) : Async<'T option> = async.Return value

    [<DebuggerStepThrough>]
    member __.Zero () : Async<unit option> =
        Some () |> async.Return

    [<DebuggerStepThrough>]
    member __.Delay (f : unit -> Async<'T option>) : Async<'T option> = async.Delay f

    [<DebuggerStepThrough>]
    member __.Combine (r1, r2 : Async<'T option>) : Async<'T option> =
        async {
            let! r1' = r1
            match r1' with
            | None -> return None
            | Some () -> return! r2
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: Async<'T option>, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            let! value' = value
            match value' with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: System.Threading.Tasks.Task<'T>, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            let! value' = Async.AwaitTask value
            return! f value'
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: 'T option, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            match value with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Using (resource : ('T :> IDisposable), body : _ -> Async<_ option>) : Async<_ option> =
        try body resource
        finally if not (isNull resource) then resource.Dispose ()

    [<DebuggerStepThrough>]
    member x.While (guard, body : Async<_ option>) : Async<_ option> =
        if guard () then
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    [<DebuggerStepThrough>]
    member x.For (sequence : seq<_>, body : 'T -> Async<unit option>) : Async<_ option> =
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (enum.MoveNext, x.Delay (fun () -> body enum.Current)))

    [<DebuggerStepThrough>]
    member inline __.TryWith (computation : Async<'T option>, catchHandler : exn -> Async<'T option>) : Async<'T option> =
            async.TryWith (computation, catchHandler)

    [<DebuggerStepThrough>]
    member inline __.TryFinally (computation : Async<'T option>, compensation : unit -> unit) : Async<'T option> =
            async.TryFinally (computation, compensation)

let asyncMaybe = AsyncMaybeBuilder()

let inline liftAsync (computation : Async<'T>) : Async<'T option> =
    async {
        let! a = computation
        return Some a 
    }

module Async =
    let map (f: 'T -> 'U) (a: Async<'T>) : Async<'U> =
        async {
            let! a = a
            return f a
        }

    /// Creates an asynchronous workflow that runs the asynchronous workflow given as an argument at most once. 
    /// When the returned workflow is started for the second time, it reuses the result of the previous execution.
    let cache (input : Async<'T>) =
        let agent = MailboxProcessor<AsyncReplyChannel<_>>.Start <| fun agent ->
            async {
                let! replyCh = agent.Receive ()
                let! res = input
                replyCh.Reply res
                while true do
                    let! replyCh = agent.Receive ()
                    replyCh.Reply res 
            }
        async { return! agent.PostAndAsyncReply id }

type AsyncBuilder with
    member __.Bind(computation: System.Threading.Tasks.Task<'a>, binder: 'a -> Async<'b>): Async<'b> =
        async {
            let! a = Async.AwaitTask computation
            return! binder a
        }

    member __.ReturnFrom(computation: System.Threading.Tasks.Task<'a>): Async<'a> = Async.AwaitTask computation


module Option =
    let guard (x: bool) : Option<unit> =
        if x then Some() else None

module List =
    let foldi (folder : 'State -> int -> 'T -> 'State) (state : 'State) (xs : 'T list) =
        let mutable state = state
        let mutable i = 0
        for x in xs do
            state <- folder state i x
            i <- i + 1
        state

module Seq =
    open System.Collections.Immutable

    let toImmutableArray (xs: seq<'a>) : ImmutableArray<'a> = xs.ToImmutableArray()