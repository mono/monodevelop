namespace MonoDevelopTests
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

            let pathToExe = "\"" + testDllFolder/"MonoDevelop.FSharpInteractive.Service.exe\""
            let ses = new InteractiveSession(pathToExe)
            ses.StartReceiving()
            do! ses.PromptReady |> Async.AwaitEvent
            return ses
        }

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive receives completions``() =
        async {
            let! session = createSession()
            session.SendCompletionRequest "Lis" 3
            let! completions = session.CompletionsReceived |> Async.AwaitEvent
            let results = completions |> Array.map(fun c -> c.displayText)
            session.KillNow()
            results |> should contain "List"
        } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive receives parameter hints``() =
        async {
            let! session = createSession()
            session.SendParameterHintRequest "System.DateTime.Now.AddDays(" 28
            let! parameters = session.ParameterHintReceived |> Async.AwaitEvent
            let results = parameters
                          |> Array.map
                               (function
                                | MonoDevelop.FSharp.Shared.ParameterTooltip.ToolTip (_signature, _doc, parameters) -> parameters
                                | MonoDevelop.FSharp.Shared.ParameterTooltip.EmptyTip -> [||])

            session.KillNow()
            results |> should equal [| [|"value"|] |]
        } |> toTask

    let sendInput (session:InteractiveSession) input =
        session.SendInput input None

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive evaluates 1+1``() =
        async {
            let! session = createSession()
            sendInput session "1+1;;"
            let! results = session.TextReceived |> Async.AwaitEvent
            session.KillNow()
            results |> should equal "val it : int = 2\n"
        } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive evaluates multiline expression``() =
        async {
            let! session = createSession()
            sendInput session "let myfun x="
            sendInput session "    if (x > 0) then 'a'"
            sendInput session "    else 'b'"
            sendInput session ";;"

            let! results = session.TextReceived |> Async.AwaitEvent
            session.KillNow()
            results |> should equal "val myfun : x:int -> char\n"
        } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive evaluates complex type``() =
        async {
            let! session = createSession()
            sendInput session "type CmdResult = ErrorLevel of string * int;;"
            let! results = session.TextReceived |> Async.AwaitEvent
            session.KillNow()
            results |> should equal "type CmdResult = | ErrorLevel of string * int\n"
        } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Bug 56611``() =
        async {
            let! session = createSession()
            sendInput session "type O = { X:string };;"
            sendInput session "[| {X=\"\"} |];;"
            do! session.TextReceived |> Async.AwaitEvent |> Async.Ignore
            do! session.TextReceived |> Async.AwaitEvent |> Async.Ignore
            let! results = session.TextReceived |> Async.AwaitEvent
            session.KillNow()
            results |> should equal "val it : O [] = [|{X = \"\";}|]\n"
        } |> toTask

    [<Test;AsyncStateMachine(typeof<Task>)>]
    let ``Interactive send references uses real assemblies #43307``() =
        async {
            let! session = createSession()
            let sln = UnitTests.Util.GetSampleProject ("bug43307", "bug43307.sln")
            use monitor = UnitTests.Util.GetMonitor ()
            use! sol = Services.ProjectService.ReadWorkspaceItem (monitor, sln |> FilePath) |> Async.AwaitTask
            use project = sol.GetAllItems<FSharpProject> () |> Seq.head

            //workaround the fact that the project doesn't have a stable relative path
            //to newtonsoft.json under the test harness by removing and re-adding with known path
            let jsonAsmLoc = typeof<Newtonsoft.Json.JsonConvert>.Assembly.Location
            let jsonRef =
                project.References
                |> Seq.filter (fun r -> r.Include.Equals "Newtonsoft.Json")
                |> Seq.head
            do project.References.Remove (jsonRef) |> ignore
            project.References.Add (ProjectReference.CreateAssemblyFileReference (jsonAsmLoc |> FilePath))

            let! refs = project.GetOrderedReferences(CompilerArguments.getConfig())
            refs
            |> List.iter (fun a -> sendInput session (sprintf  @"#r ""%s"";;" a.Path))
            let finished = new AutoResetEvent(false)
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
            sendInput session input

            let rec getOutput() =
                async {
                    let! output = session.TextReceived |> Async.AwaitEvent
                    if output.Contains "jsonObj" then
                        return output
                    else
                        return! getOutput()

                }
            let! results = getOutput()
            session.KillNow()
            results |> should equal "val jsonObj : string = \"[{\"Name\":\"Bad Boys\",\"Year\":1995}]\"\n"
        } |> toTask

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
    let ``Interactive gets source file and directory``() =
        async {
            let sourceFile = Some "/SomeFolder/SomeFile.fsx"
            let! session = createSession()
            session.SendInput "printfn __SOURCE_FILE__;;" sourceFile
            let! output = session.TextReceived |> Async.AwaitEvent
            output |> should equal ("SomeFile.fsx\n")
            // ignore `val it: unit = ()`
            let! ignore = session.TextReceived |> Async.AwaitEvent
            session.SendInput "printfn __SOURCE_DIRECTORY__;;" sourceFile
            let! output = session.TextReceived |> Async.AwaitEvent
            session.KillNow()
            output |> should equal "/SomeFolder\n"
        } |> toTask

module ``Shell history`` =
    [<Test>]
    let ``Can go back``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Up() |> should equal (Some "1")

    [<Test>]
    let ``Can go back twice``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Push "2"
        hist.Up() |> should equal (Some "2")
        hist.Up() |> should equal (Some "1")
            
    [<Test>]
    let ``Can't go back three times``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Push "2"
        hist.Up() |> should equal (Some "2")
        hist.Up() |> should equal (Some "1")
        hist.Up() |> should equal None

    [<Test>]
    let ``Down on empty list returns none``() =
        let hist = ShellHistory()
        hist.Down() |> should equal None

    [<Test>]
    let ``Up then down returns empty``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Push "2"
        hist.Up() |> should equal (Some "2")
        hist.Down() |> should equal None

    [<Test>]
    let ``Up up down returns 2``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Push "2"
        hist.Up() |> should equal (Some "2")
        hist.Up() |> should equal (Some "1")
        hist.Down() |> should equal (Some "2")
        hist.Down() |> should equal None

    [<Test>]
    let ``Up down up``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Up() |> should equal (Some "1")
        hist.Down() |> should equal None
        hist.Up() |> should equal (Some "1")
      
    [<Test>]
    let ``Up down down down up``() =
        let hist = ShellHistory()
        hist.Push "1"
        hist.Up() |> should equal (Some "1")
        hist.Down() |> should equal None
        hist.Down() |> should equal None
        hist.Down() |> should equal None
        hist.Up() |> should equal (Some "1")