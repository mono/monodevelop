namespace MonoDevelopTests
open System
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open FsUnit
open NUnit.Framework
open MonoDevelop.Core
open MonoDevelop.Core.ProgressMonitoring
open MonoDevelop.Ide.Editor
open MonoDevelop.FSharp
open MonoDevelop.Projects
open Mono.TextEditor

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

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Bug 56611``() =
        async {
            let mutable results = String.empty
            let! session = createSession()
            let finished = new AutoResetEvent(false)
            session.TextReceived
            |> Observable.throttle (TimeSpan.FromMilliseconds 500.0)
            |> Observable.subscribe
                (fun output -> results <- output
                               finished.Set() |> ignore)
            |> ignore
            session.SendInput "type O = { X:string };;"
            session.SendInput "[| {X=\"\"} |];;"
            let succeeded = finished.WaitOne(5000)
            if succeeded then 
                results 
                |> should equal "val it : O [] = [|{X = \"\";}|]\n"
            else Assert.Fail "Timeout" } |> toTask

    [<Test;Ignore;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive send references uses real assemblies #43307``() =
        async {
            let mutable results = String.empty
            let! session = createSession()
            let directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            let sln = directoryName / "Samples" / "bug43307" / "bug43307.sln"
            use monitor = new ConsoleProgressMonitor()
            use! sol = Services.ProjectService.ReadWorkspaceItem (monitor, sln |> FilePath) |> Async.AwaitTask
            use project = sol.GetAllItems<FSharpProject> () |> Seq.head
            project.GetOrderedReferences()
            |> List.iter (fun a -> session.SendInput (sprintf  @"#r ""%s"";;" a.Path))
            let finished = new AutoResetEvent(false)
            session.TextReceived.Add(fun output -> if output.Contains "jsonObj" then
                                                       results <- output
                                                       finished.Set() |> ignore)
            let input =
                """
                type Movie = {
                    Name : string
                    Year: int
                }
                let movies = [
                     { Name = "Bad Boys"; Year = 1995 }
                ]
                let jsonObj = Newtonsoft.Json.JsonConvert.SerializeObject(movies);;
                """

            session.SendInput input
            let succeeded = finished.WaitOne(20000)
            if succeeded then results |> should equal "val jsonObj : string = \"[{\"Name\":\"Bad Boys\",\"Year\":1995}]\"\n"
            else Assert.Fail "Timeout" } |> toTask

    let getPadAndEditor() =
        FixtureSetup.initialiseMonoDevelop()
        let ctx = FsiDocumentContext()
        let doc = TextEditorFactory.CreateNewDocument()
        do
            doc.FileName <- FilePath ctx.Name

        let editor = TextEditorFactory.CreateNewEditor(ctx, doc, TextEditorType.Default)
        let pad = new FSharpInteractivePad(editor)

        let data = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
        let textDocument = data.Document
        pad, editor, textDocument

    [<Test>]
    let ``Interactive doesn't remove prompt when text is added``() =
        let pad, editor, doc = getPadAndEditor()
        pad.SetPrompt()

        let line1 = doc.GetLine editor.CaretLine
        editor.CaretLine |> should equal 2
        doc.GetMarkers line1 |> Seq.length |> should equal 1
        pad.SetPrompt()

        let line2 = doc.GetLine editor.CaretLine
        editor.CaretLine |> should equal 3
        doc.GetMarkers line2 |> Seq.length |> should equal 1

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive gets source directory``() =
        async {
            let mutable results = String.empty
            let! session = createSession()
            session.SetSourceDirectory "/"
            let finished = new AutoResetEvent(false)
            session.TextReceived.Add(fun output -> finished.Set() |> ignore)
            let succeeded = finished.WaitOne(5000)
            session.SendInput "printfn __SOURCE_DIRECTORY__;;"
            let finished = new AutoResetEvent(false)
            session.TextReceived.Add(fun output -> results <- output 
                                                   finished.Set() |> ignore)
            let succeeded = finished.WaitOne(5000)
            if succeeded then results |> should equal "/\n"
            else Assert.Fail "Timeout" } |> toTask
