namespace MonoDevelop.FSharp

open System
open System.Reflection
open System.IO
open System.Diagnostics
open MonoDevelop.Ide
open MonoDevelop.Core
open Nessos.FsPickler
open Nessos.FsPickler.Json
open MonoDevelop.FSharpInteractive
type InteractiveSession() =
    //let server = "MonoDevelop" + Guid.NewGuid().ToString("n")
    // Turn off the console and add the remoting connection
    //let args = "--readline- --fsi-server:" + server + " "

    // Get F# Interactive path and command line args from settings
    //let args = args + PropertyService.Get<_>("FSharpBinding.FsiArguments", "")
    //let path =
    //    match PropertyService.Get<_>("FSharpBinding.FsiPath", "") with
    //    | s when s <> "" -> s
    //    | _ ->
    //        match CompilerArguments.getDefaultInteractive() with
    //        | Some(s) -> s
    //        | None -> ""
    let path = Path.Combine(Reflection.Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName, "MonoDevelop.FSharpInteractive.Service.exe")
    let mutable waitingForResponse = false

    //let _check =
    //    if path = "" then
    //          MonoDevelop.Ide.MessageService.ShowError( "No path to F# Interactive set, and default could not be located.", "Have you got F# installed, see http://fsharp.org for details.")
    //          raise (InvalidOperationException("No path to F# Interactive set, and default could not be located."))
    let fsiProcess =
        let processName = 
            if Environment.runningOnMono then Environment.getMonoPath() else path

        let arguments = 
            if Environment.runningOnMono then path else null

        let startInfo =
            new ProcessStartInfo
              (FileName = processName, UseShellExecute = false, Arguments = arguments,
              RedirectStandardError = true, CreateNoWindow = true, RedirectStandardOutput = true,
              RedirectStandardInput = true, StandardErrorEncoding = Text.Encoding.UTF8, StandardOutputEncoding = Text.Encoding.UTF8)

        try
            //LoggingService.LogDebug (sprintf "Interactive: Starting file=%s, Args=%A" path args)
            Process.Start(startInfo)
        with e ->
            LoggingService.LogDebug (sprintf "Interactive: Error %s" (e.ToString()))
            reraise()

    let textReceived = Event<_>()
    let promptReady = Event<_>()

    let sendCommand(str:string) =
        waitingForResponse <- true
        LoggingService.LogDebug (sprintf "Interactive: sending %s" str)
        let stream = fsiProcess.StandardInput.BaseStream
        let bytes = Text.Encoding.UTF8.GetBytes(str + "\n")
        stream.Write(bytes,0,bytes.Length)
        stream.Flush()

    //let mutable completions = List.empty<CompletionData>
    let completionsReceivedEvent = new Event<CompletionData list>()
    do
        fsiProcess.OutputDataReceived
          |> Event.filter (fun de -> de.Data <> null)
          |> Event.add (fun de ->
              LoggingService.logDebug "Interactive: received %s" de.Data
              if de.Data.Trim() = "SERVER-PROMPT>" then
                  promptReady.Trigger()
              elif de.Data.Trim() <> "" then
                  //let str = (if waitingForResponse then waitingForResponse <- false; "\n" else "") + de.Data + "\n"
                  if waitingForResponse then waitingForResponse <- false
                  textReceived.Trigger(de.Data + "\n"))

        let serializer =  FsPickler.CreateJsonSerializer()

        fsiProcess.ErrorDataReceived.Subscribe(fun de -> 
            if not (String.isNullOrEmpty de.Data) then
                try
                    let completions = serializer.UnPickleOfString<CompletionData list> de.Data
                    completionsReceivedEvent.Trigger completions
                    LoggingService.logDebug "%s" de.Data
                with 
                | :? FsPicklerException ->
                    LoggingService.logError "[fsharpi] - error deserializing error stream - %s" de.Data
                    ) |> ignore

        fsiProcess.EnableRaisingEvents <- true

    member x.Interrupt() =
        LoggingService.logDebug "Interactive: Break!"

    member x.CompletionsReceived = completionsReceivedEvent.Publish

    member x.StartReceiving() =
        fsiProcess.BeginOutputReadLine()
        fsiProcess.BeginErrorReadLine()

    member x.TextReceived = textReceived.Publish
    member x.PromptReady = promptReady.Publish

    member x.Kill() =
        if not fsiProcess.HasExited then
            sendCommand "#q"
            for i in 0 .. 10 do
                if not fsiProcess.HasExited then
                    LoggingService.logDebug "Interactive: waiting for process exit after #q... %d" (i*200)
                    fsiProcess.WaitForExit(200) |> ignore

        if not fsiProcess.HasExited then
            fsiProcess.Kill()
            for i in 0 .. 10 do
                if not fsiProcess.HasExited then
                    LoggingService.logDebug "Interactive: waiting for process exit after kill... %d" (i*200)
                    fsiProcess.WaitForExit(200) |> ignore

        if not fsiProcess.HasExited then
            LoggingService.logWarning "Interactive: failed to get process exit after kill"

    member x.SendInput input =
        for line in String.getLines input do
            sendCommand ("input " + line)
    
    member x.SendCompletionRequest input column =
        sendCommand (sprintf "completion %d %s" column input)

    member x.Exited = fsiProcess.Exited
