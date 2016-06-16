namespace MonoDevelop.FSharp

open System
open System.Reflection
open System.IO
open System.Diagnostics
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Core
open Newtonsoft.Json

type CompletionData = {
    displayText: string
    completionText: string
    category: string
    icon: string
    overloads: CompletionData list
    description: string
}

type InteractiveSession() =
    let (|Completion|_|) (command: string) =
        if command.StartsWith("completion ") then
            let payload = command.[11..]
            Some (JsonConvert.DeserializeObject<CompletionData list> payload)
        else
            None

    let (|Tooltip|_|) (command: string) =
        if command.StartsWith("tooltip ") then
            let payload = command.[8..]
            Some (JsonConvert.DeserializeObject<MonoDevelop.FSharp.Shared.ToolTips> payload)
        else
            None

    let (|ParameterHints|_|) (command: string) =
        if command.StartsWith("parameter-hints ") then
            let payload = command.[16..]
            Some (JsonConvert.DeserializeObject<MonoDevelop.FSharp.Shared.ParameterTooltip list> payload)
        else
            None

    let path = "\"" + Path.Combine(Reflection.Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName, "MonoDevelop.FSharpInteractive.Service.exe") + "\""
    let mutable waitingForResponse = false

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

    let completionsReceivedEvent = new Event<CompletionData list>()
    let tooltipReceivedEvent = new Event<MonoDevelop.FSharp.Shared.ToolTips>()
    let parameterHintReceivedEvent = new Event<MonoDevelop.FSharp.Shared.ParameterTooltip list>()
    do
        fsiProcess.OutputDataReceived
          |> Event.filter (fun de -> de.Data <> null)
          |> Event.add (fun de ->
              LoggingService.logDebug "Interactive: received %s" de.Data
              if de.Data.Trim() = "SERVER-PROMPT>" then
                  promptReady.Trigger()
              elif de.Data.Trim() <> "" then
                  if waitingForResponse then waitingForResponse <- false
                  textReceived.Trigger(de.Data + "\n"))

        fsiProcess.ErrorDataReceived.Subscribe(fun de -> 
            if not (String.isNullOrEmpty de.Data) then
                try
                    match de.Data with
                    | Completion completions ->
                        completionsReceivedEvent.Trigger completions
                    | Tooltip tooltip ->
                        tooltipReceivedEvent.Trigger tooltip
                    | ParameterHints hints ->
                        parameterHintReceivedEvent.Trigger hints
                    | _ -> LoggingService.logDebug "[fsharpi] don't know how to process command %s" de.Data
                    
                with 
                | :? JsonException ->
                    LoggingService.logError "[fsharpi] - error deserializing error stream - %s" de.Data
                    ) |> ignore

        fsiProcess.EnableRaisingEvents <- true

    member x.Interrupt() =
        LoggingService.logDebug "Interactive: Break!"

    member x.CompletionsReceived = completionsReceivedEvent.Publish
    member x.TooltipReceived = tooltipReceivedEvent.Publish
    member x.ParameterHintReceived = parameterHintReceivedEvent.Publish

    member x.StartReceiving() =
        fsiProcess.BeginOutputReadLine()
        fsiProcess.BeginErrorReadLine()

    member x.TextReceived = textReceived.Publish
    member x.PromptReady = promptReady.Publish

    member x.Kill() =
        if not fsiProcess.HasExited then
            x.SendInput "#q;;"
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

    member x.SendInput input =
        for line in String.getLines input do
            sendCommand ("input " + line)
    
    member x.SendCompletionRequest input column =
        sendCommand (sprintf "completion %d %s" column input)

    member x.SendParameterHintRequest input column =
        sendCommand (sprintf "parameter-hints %d %s" column input)

    member x.SendTooltipRequest input  =
        sendCommand (sprintf "tooltip %s" input)

    member x.Exited = fsiProcess.Exited
