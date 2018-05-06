namespace MonoDevelopTests
open System
open System.Text.RegularExpressions
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Core
open MonoDevelop.Core.Text

module FixtureSetup =
    let firstRun = ref true

    let initialiseMonoDevelop() =
        if !firstRun then
            firstRun := false
            //Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", "/tmp")
            //Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", "/tmp")
            MonoDevelop.FSharp.MDLanguageService.DisableVirtualFileSystem()
            Xwt.Application.Initialize (Xwt.ToolkitType.Gtk)
            Runtime.Initialize (true)
            MonoDevelop.Ide.DesktopService.Initialize()

            GuiUnit.TestRunner.ExitCode |> ignore // hack to get GuiUnit into the AppDomain

module TestHelpers =
    let filename = if Platform.IsWindows then "c:\\test.fsx" else "test.fsx"

    let parseAndCheckFile source =
        async {
            try
                let checker = FSharpChecker.Create()
                let! projOptions, _errors = checker.GetProjectOptionsFromScript(filename, source)
                let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(filename, 0, source , projOptions)

                // Construct new typed parse result if the task succeeded
                let results =
                  match checkAnswer with
                  | FSharpCheckFileAnswer.Succeeded(checkResults) ->
                      ParseAndCheckResults(Some checkResults, Some parseResults)
                  | FSharpCheckFileAnswer.Aborted ->
                      ParseAndCheckResults(None, Some parseResults)
                if parseResults.Errors.Length > 0 then
                    printfn "%A" parseResults.Errors
                return results
            with exn ->
                printf "%A" exn
                return ParseAndCheckResults(None, None) }

    let createDocWithParseResults source compilerDefines (parseFile:string -> ParseAndCheckResults) =
        FixtureSetup.initialiseMonoDevelop()

        let results = parseFile source

        results.CheckResults |> Option.iter(fun r -> if r.Errors.Length > 0 then printfn "%A" r.Errors)
        let options = ParseOptions(FileName = filename, Content = StringTextSource(source))

        let parsedDocument =
            ParsedDocument.create options results [compilerDefines] (Some (new DocumentLocation(0,0))) |> Async.RunSynchronously

        let doc = TextEditorFactory.CreateNewReadonlyDocument(StringTextSource(source), filename, "text/fsharp")
        let editor = MonoDevelop.Ide.Editor.TextEditorFactory.CreateNewEditor (doc)

        TestDocument(filename, parsedDocument, editor)

    let createDoc source compilerDefines =
        createDocWithParseResults source compilerDefines (fun source -> parseAndCheckFile source |> Async.RunSynchronously)

    let createDocWithoutParsing source compilerDefines =
        createDocWithParseResults source compilerDefines (fun _ -> ParseAndCheckResults(None, None))

    let getAllSymbols source =
        async {
          let! results = parseAndCheckFile source
          return! results.GetAllUsesOfAllSymbolsInFile()
        } |> Async.RunSynchronously

    let stripHtml html =
        Regex.Replace(html, "<.*?>", "")
