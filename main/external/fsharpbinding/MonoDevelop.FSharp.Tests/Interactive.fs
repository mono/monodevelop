namespace MonoDevelopTests
open System.Threading
open NUnit.Framework
open MonoDevelop.FSharp.MonoDevelop
open MonoDevelop.FSharp
open System.IO
open System.Reflection
open System.Threading.Tasks
open System.Runtime.CompilerServices

[<TestFixture>]
module Interactive =
    let toTask computation : Task = Async.StartAsTask computation :> _

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive receives prompt``() =
      async {
        let (/) a b = Path.Combine(a,b)
        let testDllFolder = Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName
        let pathToExe = "\"" + testDllFolder/".."/".."/".."/".."/".."/"build"/"AddIns"/"BackendBindings"/"MonoDevelop.FSharpInteractive.Service.exe\""
        let ses = InteractiveSession(pathToExe)
        do! Async.Sleep 1000 // give the process chance to start
        if ses.HasExited() then
            Assert.Fail("Interactive session has exited")
        ses.StartReceiving()
        let finished = new AutoResetEvent(false) // using AutoResetEvent because I can't get Async.AwaitEvent to work here without a hang
        ses.PromptReady.Add(fun _ -> finished.Set() |> ignore)
        finished.WaitOne() |> ignore
        Assert.Pass() } |> toTask
