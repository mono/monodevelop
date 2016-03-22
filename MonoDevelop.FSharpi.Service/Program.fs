namespace MonoDevelop.FSharpInteractive
open System
open System.IO
open System.Text
open MonoDevelop.Ide
open Newtonsoft.Json
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

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
        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration(Settings.fsi, true)
        let fsiSession = FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, outStream)

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

        let (|ColorScheme|_|) (command: string) =
            if command.StartsWith("colorscheme ") then
                Some(command.[12..])
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

        let writeLine (s:string) =
            async {
                do! outStream.WriteLineAsync s |> Async.AwaitTask
            }

        let rec main(currentInput) =
            let parseInput() =
                async {
                    let! command = inStream.ReadLineAsync() |> Async.AwaitTask

                    match command with
                    | Input input ->
                        if input.EndsWith(";;") then
                            try
                                let result, warnings = fsiSession.EvalInteractionNonThrowing (currentInput + input)
                                match result with
                                | Choice1Of2 () -> ()
                                | Choice2Of2 exn -> do! writeLine (exn |> string)
                                for w in warnings do
                                    do! writeLine (sprintf "%s at %d,%d" w.Message w.StartLineAlternate w.StartColumn)

                                if not (input.StartsWith "#silentCd") then
                                    do! writeLine "SERVER-PROMPT>"
                            with
                            | exn -> do! writeLine (exn |> string)
                            return ""
                        else
                            return currentInput + "\n" + input
                    | ColorScheme colorScheme ->
                        //IdeApp.Preferences.ColorScheme.Value <- colorScheme
                        return currentInput
                    | Tooltip filter ->
                        let! tooltip = Completion.getCompletionTooltip filter
                        let json = JsonConvert.SerializeObject tooltip
                        do! Console.Error.WriteLineAsync ("tooltip " + json) |> Async.AwaitTask
                        return currentInput
                    | Completion context ->
                        let col, lineStr = context
                        let! results = Completion.getCompletions(fsiSession, lineStr, col)

                        let json = JsonConvert.SerializeObject results
                        do! Console.Error.WriteLineAsync ("completion " + json) |> Async.AwaitTask
                        return currentInput
                    | _ -> do! writeLine (sprintf "Could not parse command - %s" command)
                           return currentInput
                }
            let currentInput = parseInput() |> Async.RunSynchronously
            main(currentInput)

        main("")

        0 // return an integer exit code

