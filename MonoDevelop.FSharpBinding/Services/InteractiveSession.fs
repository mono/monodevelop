namespace MonoDevelop.FSharp

open System
open System.Reflection
open System.IO
open System.Diagnostics
open MonoDevelop.Ide
open MonoDevelop.Core

type InteractiveSession() =
    let server = "MonoDevelop" + Guid.NewGuid().ToString("n")
    // Turn off the console and add the remoting connection
    let args = "--readline- --fsi-server:" + server + " "

    // Get F# Interactive path and command line args from settings
    let args = args + PropertyService.Get<_>("FSharpBinding.FsiArguments", "")
    let path =
        match PropertyService.Get<_>("FSharpBinding.FsiPath", "") with
        | s when s <> "" -> s
        | _ ->
            match CompilerArguments.getDefaultInteractive() with
            | Some(s) -> s
            | None -> ""

    let mutable waitingForResponse = false

    let _check =
        if path = "" then
              MonoDevelop.Ide.MessageService.ShowError( "No path to F# Interactive set, and default could not be located.", "Have you got F# installed, see http://fsharp.org for details.")
              raise (InvalidOperationException("No path to F# Interactive set, and default could not be located."))
    let fsiProcess =
        let startInfo =
            new ProcessStartInfo
              (FileName = path, UseShellExecute = false, Arguments = args,
              RedirectStandardError = true, CreateNoWindow = true, RedirectStandardOutput = true,
              RedirectStandardInput = true, StandardErrorEncoding = Text.Encoding.UTF8, StandardOutputEncoding = Text.Encoding.UTF8)

        try
            LoggingService.LogDebug (sprintf "Interactive: Starting file=%s, Args=%A" path args)
            Process.Start(startInfo)
        with e ->
            LoggingService.LogDebug (sprintf "Interactive: Error %s" (e.ToString()))
            reraise()

    let textReceived = Event<_>()
    let promptReady = Event<_>()

    do
        Event.merge fsiProcess.OutputDataReceived fsiProcess.ErrorDataReceived
          |> Event.filter (fun de -> de.Data <> null)
          |> Event.add (fun de ->
              LoggingService.LogDebug (sprintf "Interactive: received %s" de.Data)
              if de.Data.Trim() = "SERVER-PROMPT>" then
                  promptReady.Trigger()
              elif de.Data.Trim() <> "" then
                  //let str = (if waitingForResponse then waitingForResponse <- false; "\n" else "") + de.Data + "\n"
                  if waitingForResponse then waitingForResponse <- false
                  textReceived.Trigger(de.Data + "\n"))
        fsiProcess.EnableRaisingEvents <- true

    member x.Interrupt() =
        LoggingService.LogDebug (sprintf "Interactive: Break!" )

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
                    LoggingService.LogDebug (sprintf "Interactive: waiting for process exit after #q... %d" (i*200))
                    fsiProcess.WaitForExit(200) |> ignore

        if not fsiProcess.HasExited then
            fsiProcess.Kill()
            for i in 0 .. 10 do
                if not fsiProcess.HasExited then
                    LoggingService.LogDebug (sprintf "Interactive: waiting for process exit after kill... %d" (i*200))
                    fsiProcess.WaitForExit(200) |> ignore

        if not fsiProcess.HasExited then
            LoggingService.LogWarning (sprintf "Interactive: failed to get process exit after kill" )

    member x.SendCommand(str:string) =
        waitingForResponse <- true
        LoggingService.LogDebug (sprintf "Interactive: sending %s" str)
        let stream = fsiProcess.StandardInput.BaseStream
        let bytes = Text.Encoding.UTF8.GetBytes(str + "\n")
        stream.Write(bytes,0,bytes.Length)
        stream.Flush()

    member x.Exited = fsiProcess.Exited
