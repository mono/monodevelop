namespace MonoDevelopTests
open System
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Core
open MonoDevelop.Core.Text

type FixtureSetup() =
  static let firstRun = ref true

  member x.Initialise() =
    if !firstRun then
      firstRun := false
      MonoDevelop.FSharp.MDLanguageService.DisableVirtualFileSystem()
      MonoDevelop.Ide.DesktopService.Initialize()
      
      GuiUnit.TestRunner.ExitCode |> ignore // hack to get GuiUnit into the AppDomain

module TestHelpers =
  let filename = if Platform.IsWindows then "c:\\test.fsx" else "test.fsx"

  let parseAndCheckFile source =
    async {
      try
         let checker = FSharpChecker.Create()
         let! projOptions = checker.GetProjectOptionsFromScript(filename, source)
         let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(filename, 0, source , projOptions)

         // Construct new typed parse result if the task succeeded
         let results =
           match checkAnswer with
           | FSharpCheckFileAnswer.Succeeded(checkResults) ->
               ParseAndCheckResults(Some checkResults, Some parseResults)
           | FSharpCheckFileAnswer.Aborted ->
               ParseAndCheckResults(None, Some parseResults)
               
         return results
      with exn ->
        printf "%A" exn
        return ParseAndCheckResults(None, None) }

  let createDoc source compilerDefines =
    FixtureSetup().Initialise()

    let results = parseAndCheckFile source |> Async.RunSynchronously
    let options = ParseOptions(FileName = filename, Content = StringTextSource(source))
    let parsedDocument =
      ParsedDocument.create options results [compilerDefines] |> Async.RunSynchronously

    let doc = TextEditorFactory.CreateNewReadonlyDocument(StringTextSource(source), filename, "text/fsharp")
    let editor = MonoDevelop.Ide.Editor.TextEditorFactory.CreateNewEditor (doc)

    TestDocument(filename, parsedDocument, editor)

  let getAllSymbols source =
    async {
      let! results = parseAndCheckFile source
      return! results.GetAllUsesOfAllSymbolsInFile()
    } |> Async.RunSynchronously
