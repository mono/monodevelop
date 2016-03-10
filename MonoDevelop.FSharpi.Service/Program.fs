namespace MonoDevelop.FSharpInteractive
open System
open System.IO
open System.Text
open Nessos.FsPickler
open Nessos.FsPickler.Json
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

type CompletionContext = { column: int; lineStr: string }

module CompletionServer =
    [<EntryPoint>]
    let main argv = 
        let inStream = new StringReader("")
        let inStream = Console.In
        let outStream = Console.Out
        let argv = [| "--noninteractive" |]
        let pickler = FsPickler.CreateJsonSerializer()
        let errorStream = Console.OpenStandardError()
        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        let fsiSession = FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, outStream)

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

        let rec main() = 
            let parseInput() = 
                async {
                    let! command = inStream.ReadLineAsync() 
                                   |> Async.AwaitTask
                    
                    match command with
                    | Input input ->
                        fsiSession.EvalInteractionNonThrowing input |> ignore
                    | Completion context ->
                        let col, lineStr = context
                        let! results = Completion.getCompletions(fsiSession, lineStr, col)
                        pickler.Serialize (errorStream, results)
                    | _ -> printfn "%s %s" "Could not parse command - " command
                }
            parseInput() |> Async.RunSynchronously
            main()
        
        main()

        0 // return an integer exit code

