namespace MonoDevelopTests
open Microsoft.FSharp.Compiler.SourceCodeServices
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Core.Text

type FixtureSetup() =
  static let firstRun = ref true
  member x.Initialise() =
    if !firstRun then
      firstRun := false
      MonoDevelop.FSharp.MDLanguageService.DisableVirtualFileSystem()
      MonoDevelop.Ide.DesktopService.Initialize()

module TestHelpers =

  let parseAndCheckFile source filename =
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
        return ParseAndCheckResults(None, None) }  |> Async.RunSynchronously

  let createDoc source compilerDefines =
    let file = "test.fsx"
    let options = ParseOptions(FileName = file, Content = StringTextSource(source))

    let results = parseAndCheckFile source file
    let parsedDocument = FSharpParser().GetParsedDocument(options, results, [compilerDefines]) 
                         |> Async.RunSynchronously

    FixtureSetup().Initialise()
    let editor = MonoDevelop.Ide.Editor.TextEditorFactory.CreateNewEditor ()
    editor.Text <- source
    TestDocument(file, parsedDocument, editor)
