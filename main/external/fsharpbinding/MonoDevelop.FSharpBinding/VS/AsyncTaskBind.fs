[<AutoOpen>]
module AsyncTaskBind

open System.Threading.Tasks

type Microsoft.FSharp.Control.AsyncBuilder with
    member x.Bind(computation:Task<'T>, binder:'T -> Async<'R>) =  x.Bind(Async.AwaitTask computation, binder)
    member x.ReturnFrom(computation:Task<'T>) = x.ReturnFrom(Async.AwaitTask computation)