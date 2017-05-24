namespace MonoDevelopTests
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open FsUnit
open NUnit.Framework
open MonoDevelop.FSharp

[<TestFixture>]
module Interactive =
    let toTask computation : Task = Async.StartAsTask computation :> _

    let createSession() =
        async {
            let (/) a b = Path.Combine(a,b)
            let testDllFolder = Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName
            let pathToExe = "\"" + testDllFolder/".."/".."/".."/".."/".."/"build"/"AddIns"/"FSharpBinding"/"MonoDevelop.FSharpInteractive.Service.exe\""
            let ses = InteractiveSession(pathToExe)
            ses.StartReceiving()
            let finished = new AutoResetEvent(false) // using AutoResetEvent because I can't get Async.AwaitEvent to work here without a hang
            ses.PromptReady.Add(fun _ -> finished.Set() |> ignore)
            let succeeded = finished.WaitOne(10000)
            if not succeeded then Assert.Fail "Timed out waiting for prompt"
            return ses
        }

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive receives completions``() =
        async {
            let mutable results = [||]
            let! session = createSession()
            let finished = new AutoResetEvent(false)
            session.CompletionsReceived.Add(fun completions -> results <- completions |> Array.map(fun c -> c.displayText)
                                                               finished.Set() |> ignore)
            session.SendCompletionRequest "Lis" 3
            let succeeded = finished.WaitOne(5000)
            if succeeded then results |> should contain "List"
            else Assert.Fail "Timeout" } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive receives parameter hints``() =
        async {
            let mutable results = [||]
            let! session = createSession()
            let finished = new AutoResetEvent(false)
            session.ParameterHintReceived.Add(fun parameters -> results <- 
                                                                    parameters 
                                                                    |> Array.map(fun p -> match p with
                                                                                          | MonoDevelop.FSharp.Shared.ParameterTooltip.ToolTip (_signature, _doc, parameters) -> parameters
                                                                                          | MonoDevelop.FSharp.Shared.ParameterTooltip.EmptyTip -> [||])
                                                                finished.Set() |> ignore)
            session.SendParameterHintRequest "System.DateTime.Now.AddDays(" 28
            let succeeded = finished.WaitOne(5000)
            if succeeded then results |> should equal [| [|"value"|] |]
            else Assert.Fail "Timeout" } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive evaluates 1+1``() =
        async {
            let mutable results = String.empty
            let! session = createSession()
            let finished = new AutoResetEvent(false)
            session.TextReceived.Add(fun output -> results <- output 
                                                   finished.Set() |> ignore)
            session.SendInput "1+1;;"
            let succeeded = finished.WaitOne(5000)
            if succeeded then results |> should equal "val it : int = 2\n"
            else Assert.Fail "Timeout" } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive evaluates multiline expression``() =
        async {
            let mutable results = String.empty
            let! session = createSession()
            let finished = new AutoResetEvent(false)
            session.TextReceived.Add(fun output -> results <- output 
                                                   finished.Set() |> ignore)
            session.SendInput "let myfun x="
            session.SendInput "    if (x > 0) then 'a'"
            session.SendInput "    else 'b'"
            session.SendInput ";;"    
            let succeeded = finished.WaitOne(5000)
            if succeeded then results |> should equal "val myfun : x:int -> char\n"
            else Assert.Fail "Timeout" } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive evaluates complex type``() =
        async {
            let mutable results = String.empty
            let! session = createSession()
            let finished = new AutoResetEvent(false)
            session.TextReceived.Add(fun output -> results <- output 
                                                   finished.Set() |> ignore)
            session.SendInput "type CmdResult = ErrorLevel of string * int;;"
            let succeeded = finished.WaitOne(5000)
            if succeeded then results |> should equal "type CmdResult = | ErrorLevel of string * int\n"
            else Assert.Fail "Timeout" } |> toTask
