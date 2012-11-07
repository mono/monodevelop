// --------------------------------------------------------------------------------------
// Simple implementation of F#-like MailboxProcessor
// Used because the standard implementation seems to have some issues (?) on Mono (?)
// For more information see project in /Related/MailboxIssue
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp.MailBox

#nowarn "40"

open System.Threading
open System.Collections.Generic

// --------------------------------------------------------------------------------------

/// Represents a channel that is used for sending replies back to the caller
type AsyncReplyChannel<'T> = 
  abstract Reply : 'T -> unit
  
/// Simple implementation of F# MailboxProcessor type
/// This is (intentionally) somewhat naive, to make sure it works (before someone 
/// finds out what is wrong with the standard MailboxProcessor on Mono..?)  
/// Notable simplification is that we use just a single queue of messages
/// We also use busy waiting with Sleep to wait for messages, which is not ideal
type SimpleMailboxProcessor<'T>(body : SimpleMailboxProcessor<'T> -> Async<unit>) as this =
  let sync = new obj()
  let queue = new List<'T>()
  
  /// Try to get element from the queue if there is any
  let safeTryPop() = 
    lock sync (fun () -> 
      if queue.Count = 0 then None
      else 
        let res = Some(queue.[0])
        queue.RemoveAt(0)
        res )
        
  /// Start the agent (using the body provided during construction)
  member x.Start() =
    body this |> Async.Start     
  
  /// Try receive a message from the mailbox processor 
  /// (to be called from the body of the processor)
  member x.TryReceive(?timeout) = 
    let timeout = defaultArg timeout System.Int32.MaxValue
    let time = System.Environment.TickCount
    let rec loop() = async {
      // Try to get item from mailbox using a lock
      let itm = safeTryPop()
      // Return if found or if timeout, otherwise busy loop
      match itm with 
      | None when (System.Environment.TickCount - time) < timeout -> 
          do! Async.Sleep(50)
          return! loop()
      | res -> return res }
    loop()

  /// Receive a message from the mailbox processor
  /// (to be called from the body of the processor)
  /// Throws System.TimeoutException if no message is received
  member x.Receive(?timeout) = async { 
    let! res = x.TryReceive(?timeout = timeout)
    match res with
    | None -> return raise(new System.TimeoutException())
    | Some(v) -> return v }
    
  /// Add a message to the agent's queue
  member x.Post(msg) = 
    lock sync (fun () -> queue.Add(msg))
  
  
  /// Returns the current length of the queue
  member x.CurrentQueueLength = 
    lock sync (fun () -> queue.Count)
  
  /// Send a message to the agent and wait until the agent replies.
  /// Throws System.TimeoutException if the reply is not sent soon enough
  member x.PostAndReply(f, ?timeout) = 
    let evt = new AutoResetEvent(false)
    let value = ref None
    let chnl = 
      { new AsyncReplyChannel<_> with 
          member x.Reply(res) = 
            value := Some(res)
            evt.Set() |> ignore }
    x.Post(f chnl)
    match timeout with
    | Some(timeout) -> evt.WaitOne(timeout : int) |> ignore
    | _ -> evt.WaitOne() |> ignore
    match !value with 
    | Some(v) -> v
    | None -> raise (new System.TimeoutException())
    
  /// Try to process messages using the specified function
  /// (function is called when a message is received to see if 
  /// the message can be handled)
  member x.Scan(f) = async {
    while true do
      let work = lock sync (fun () -> 
        let indexed = Seq.zip (seq { 0 .. queue.Count - 1 }) queue
        let sel = indexed |> Seq.tryPick (fun (i, inp) -> f inp |> Option.map (fun v -> i, v))
        match sel with
        | Some(i, work) -> 
            queue.RemoveAt(i) 
            Some(work)
        | _ -> None )
        
      match work with
      | None -> do! Async.Sleep(50)
      | Some(work) -> do! work }
   
  /// Creates a new mailbx processor with the body specified as 
  /// a parameter and start it immediately after it is created
  static member Start(body) = 
    let mb = new SimpleMailboxProcessor<'T>(body)
    mb.Start()
    mb
