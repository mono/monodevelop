open System
open System.IO
open System.Text
open Nessos.FsPickler
open Nessos.FsPickler.Json
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

[<EntryPoint>]
let main argv = 
    // Intialize output and input streams
    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let inStream = Console.In
    let outStream = new StringWriter(sbOut)
    let fsiErrStream = new StringWriter(sbErr)
    let outStream = Console.Out
    // Build command line arguments & start FSI session
    let argv = [| "poop" |]
    let allArgs = Array.append argv [|"--noninteractive"|]
    let pickler = FsPickler.CreateBinarySerializer()
    let outstream = Console.OpenStandardError()
    let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
    let fsiSession = FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, fsiErrStream)

    let _parseResults, checkResults, _checkProjectResults = fsiSession.ParseAndCheckInteraction("let x = 1")
    let ret = fsiSession.EvalInteractionNonThrowing "let x = 1;;"

    pickler.Serialize(outstream, checkResults)
    0 // return an integer exit code

