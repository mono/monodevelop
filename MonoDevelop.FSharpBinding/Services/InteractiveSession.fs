namespace MonoDevelop.FSharp

open System
open System.IO
open System.Diagnostics
open MonoDevelop.Ide
open MonoDevelop.Core

type InteractiveSession() =
  let server = "MonoDevelop" + Guid.NewGuid().ToString("n")
  // Turn off the console and add the remoting connection 
  let args = "--readline- --fsi-server:" + server + " "
  
  // Get F# Interactive path and command line args from settings
  let args = args + PropertyService.Get<string>("FSharpBinding.FsiArguments", "")
  let path = 
    match PropertyService.Get<string>("FSharpBinding.FsiPath", "") with
    | s when s <> "" -> s
    | _ -> 
      match CompilerArguments.getDefaultInteractive() with
      | Some(s) -> s
      | None -> ""

  let mutable waitingForResponse = false
 
  let check = if path = "" then raise (Exception("No path to F# Interactive console set, and default could not be located."))
  let fsiProcess = 
    let startInfo = 
      new ProcessStartInfo
        (FileName = path, UseShellExecute = false, Arguments = args, 
         RedirectStandardError = true, CreateNoWindow = true, RedirectStandardOutput = true,
         RedirectStandardInput = true) 
    try
      Debug.WriteLine (sprintf "Interactive: Starting file=%s, Args=%A" path args)
      Process.Start(startInfo)
    with e ->
      Debug.WriteLine (sprintf "Interactive: Error %s" (e.ToString()))
      reraise()
    
  let client = 
      try Microsoft.FSharp.Compiler.Server.Shared.FSharpInteractiveServer.StartClient(server)
      with e -> failwithf "oops! %A" e

  let textReceived = new Event<_>()  
  let promptReady = new Event<_>()  
  
  do 
    Event.merge fsiProcess.OutputDataReceived fsiProcess.ErrorDataReceived
      |> Event.filter (fun de -> de.Data <> null)
      |> Event.add (fun de -> 
          Debug.WriteLine (sprintf "Interactive: received %s" de.Data)
          if de.Data.Trim() = "SERVER-PROMPT>" then
            DispatchService.GuiDispatch(fun () -> promptReady.Trigger())
          elif de.Data.Trim() <> "" then
            let str = (if waitingForResponse then waitingForResponse <- false; "\n" else "") + de.Data + "\n"
            DispatchService.GuiDispatch(fun () -> textReceived.Trigger(str)) )
    fsiProcess.EnableRaisingEvents <- true
  
  member x.Interrupt() =
    Debug.WriteLine (sprintf "Interactive: Break!" )
    client.Interrupt()
    
  member x.StartReceiving() = 
    fsiProcess.BeginOutputReadLine()  
    fsiProcess.BeginErrorReadLine()
    
  member x.TextReceived = textReceived.Publish
  member x.PromptReady = promptReady.Publish
  
  member x.Kill() = 
    if not fsiProcess.HasExited then 
      x.SendCommand "#q"
      for i in 0 .. 10 do 
        if not fsiProcess.HasExited then 
           Debug.WriteLine (sprintf "Interactive: waiting for process exit after #q... %d" (i*200))
           fsiProcess.WaitForExit(200) |> ignore
           
    if not fsiProcess.HasExited then 
      fsiProcess.Kill()
      for i in 0 .. 10 do 
        if not fsiProcess.HasExited then 
           Debug.WriteLine (sprintf "Interactive: waiting for process exit after kill... %d" (i*200))
           fsiProcess.WaitForExit(200) |> ignore
           
    if not fsiProcess.HasExited then 
       Debug.WriteLine (sprintf "Interactive: failed to get process exit after kill, may get hang on mac" )
    
  member x.SendCommand(str:string) = 
    waitingForResponse <- true
    Debug.WriteLine (sprintf "Interactive: sending %s" str)
    fsiProcess.StandardInput.Write(str + ";;\n")

  member x.Exited = fsiProcess.Exited
    
