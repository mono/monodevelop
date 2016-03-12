namespace MonoDevelop.FSharpInteractive
open System
open System.IO
open System.Text
open Nessos.FsPickler
open Nessos.FsPickler.Json
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

/// Wrapper for fsi with support for returning completions
module CompletionServer =
    [<EntryPoint>]
    let main argv = 
        let inStream = Console.In
        let outStream = Console.Out

        let sbErr = new StringBuilder()
        use errorStream = new StringWriter(sbErr)
        let server = "MonoDevelop" + Guid.NewGuid().ToString("n")
        // This flag makes fsi send the SERVER-PROMPT> prompt
        // once it's output the header
        let args = "--fsi-server:" + server + " "
        let argv = [| "--readline-"; args  |]
        let pickler = FsPickler.CreateJsonSerializer()

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        let fsiSession = FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errorStream)

        let (|Input|_|) (command: string) =
            if command.StartsWith("input ") then
                Some(command.Substring(6))
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

        let rec main() = 
            let parseInput() = 
                async {
                    let! command = inStream.ReadLineAsync() |> Async.AwaitTask
                    
                    match command with
                    | Input input ->
                        let result, warnings = fsiSession.EvalInteractionNonThrowing input
                        for w in warnings do
                            do! writeLine (sprintf "%s at %d,%d" w.Message w.StartLineAlternate w.StartColumn)
                        do! writeLine "SERVER-PROMPT>"
                        //match result with
                        ////| Choice1Of2 (Some value) -> () // we're good
                        ////| Choice1Of2 None -> printfn "null or no result"
                        ////| Choice2Of2 ex -> printfn "exception %s" ex.Message
                        //| Choice1Of2 () -> ()
                          
                                
                        //        //printfn "%s" warning.Message
                        //| Choice2Of2 exn ->
                        //    do! outStream.WriteLineAsync exn.Message |> Async.AwaitTask
                        //use errorReader = new StreamReader()
                        //let rec readStdErr() =
                        //    let line = errorReader.ReadLine()
                        //    if line <> null then
                        //        printfn "stderr %s" line
                        //        readStdErr()
                        //    else
                        //        ()

                        //readStdErr()
                    | Completion context ->
                        let col, lineStr = context
                        let! results = Completion.getCompletions(fsiSession, lineStr, col)

                        let json = pickler.PickleToString results
                        do! Console.Error.WriteLineAsync json |> Async.AwaitTask

                    | _ -> printfn "Could not parse command - %s" command
                }
            parseInput() |> Async.RunSynchronously
            main()
        
        main()

        0 // return an integer exit code

