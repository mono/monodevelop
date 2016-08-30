namespace MonoDevelop.FSharpInteractive
open System
open System.IO
open System.Text
open Newtonsoft.Json
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

open MonoDevelop.FSharp.Shared
/// Wrapper for fsi with support for returning completions
module CompletionServer =
    [<EntryPoint>]
    let main argv =
        let inStream = Console.In
        let outStream = Console.Out
        let server = "MonoDevelop" + Guid.NewGuid().ToString("n")

        // This flag makes fsi send the SERVER-PROMPT> prompt
        // once it's output the header
        let args = "--fsi-server:" + server + " "
        let argv = [| "--readline-"; args  |]

        let serializer = JsonSerializer.Create()

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration(Microsoft.FSharp.Compiler.Interactive.Settings.fsi, true)

        let fsiSession = FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, outStream, true)
       
        let (|Input|_|) (command: string) =
            if command.StartsWith("input ") then
                Some(command.[6..])
            else
                None

        let (|Tooltip|_|) (command: string) =
            if command.StartsWith("tooltip ") then
                Some(command.[8..])
            else
                None

        let (|Completion|_|) (command: string) =
            if command.StartsWith("completion ") then
                let input = command.[11..]
                let splitIndex = input.IndexOf(" ")
                if (splitIndex = -1) then
                    None
                else
                    let colStr = input.[0..splitIndex]
                    let success, col = Int32.TryParse colStr
                    if success then
                        Some (col, input.[splitIndex..])
                    else
                        None
            else
                None

        let (|ParameterHints|_|) (command: string) =
            if command.StartsWith("parameter-hints ") then
                let input = command.[16..]
                let splitIndex = input.IndexOf(" ")
                if (splitIndex = -1) then
                    None
                else
                    let colStr = input.[0..splitIndex]
                    let success, col = Int32.TryParse colStr
                    if success then
                        Some (col, input.[splitIndex..])
                    else
                        None
            else
                None

        let writeOutput (s:string) =
            async {
                do! outStream.WriteLineAsync s |> Async.AwaitTask
            }

        let writeData commandType obj =
            async {
                let json = JsonConvert.SerializeObject obj
                do! Console.Error.WriteLineAsync (commandType + " " + json) |> Async.AwaitTask
            }

        let rec main(currentInput) =
            let parseInput() =
                async {
                    let! command = inStream.ReadLineAsync() |> Async.AwaitTask

                    match command with
                    | Input input ->
                        if input.EndsWith(";;") then
                            let result, warnings = fsiSession.EvalInteractionNonThrowing (currentInput + "\n" + input)
                            match result with
                            | Choice1Of2 () -> ()
                            | Choice2Of2 exn -> do! writeOutput (exn |> string)
                            for w in warnings do
                                do! writeOutput (sprintf "%s at %d,%d" w.Message w.StartLineAlternate w.StartColumn)

                            if not (input.StartsWith "#silentCd") then
                                do! writeOutput "SERVER-PROMPT>"
                            return ""
                        else
                           return currentInput + "\n" + input
                    | Tooltip filter ->
                        let! tooltip = Completion.getCompletionTooltip filter
                        do! writeData "tooltip" tooltip
                        return currentInput
                    | Completion context ->
                        let col, lineStr = context
                        let! results = Completion.getCompletions(fsiSession, lineStr, col)
                        do! writeData "completion" results
                        return currentInput
                    | ParameterHints context ->
                        let col, lineStr = context
                        let! results = Completion.getParameterHints(fsiSession, lineStr, col)
                        do! writeData "parameter-hints" results
                        return currentInput
                    | _ -> do! writeOutput (sprintf "Could not parse command - %s" command)
                           return currentInput
                }
            let currentInput =
                try
                    parseInput() |> Async.RunSynchronously
                with
                | exn ->
                    writeOutput (exn |> string) |> Async.RunSynchronously
                    currentInput
            main(currentInput)

        Console.SetOut outStream
        main("")

        0 // return an integer exit code

