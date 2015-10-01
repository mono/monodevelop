namespace MonoDevelop.FSharp

open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open System 
open System.Diagnostics
open System.IO
open System.Text
open System.Threading

type FSharpParsedDocument(fileName) = 
    inherit DefaultParsedDocument(fileName)
    member val Tokens = None with get,set

// An instance of this type is created by MonoDevelop (as defined in the .xml for the AddIn) 
type FSharpParser() = 
    inherit TypeSystemParser()

    let languageService = MDLanguageService.Instance 

    
    /// Format errors for the given line (if there are multiple, we collapse them into a single one)
    let formatError (error : FSharpErrorInfo) = 
        // Single error for this line
        let errorType = 
            if error.Severity = FSharpErrorSeverity.Error then ErrorType.Error
            else ErrorType.Warning
        Error(errorType, String.wrapText error.Message 80, DocumentRegion (error.StartLineAlternate, error.StartColumn + 1, error.EndLineAlternate, error.EndColumn + 1))

    let tryGetFilePath fileName (project: MonoDevelop.Projects.Project) = 
        // TriggerParse will work only for full paths
        if IO.Path.IsPathRooted(fileName) then Some(fileName)
        else 
            let workBench = IdeApp.Workbench
            match workBench with
            | null -> 
                let filePaths = project.GetItemFiles(true)
                let res = filePaths |> Seq.find(fun t -> t.FileName = fileName)
                Some(res.FullPath.ToString())
            | wb ->    
                match wb.ActiveDocument with
                | null -> None
                | doc -> let file = doc.FileName.FullPath.ToString()
                         if file = "" then None else Some file
            

    override x.Parse(parseOptions, cancellationToken) =
        let fileName = parseOptions.FileName
        let content = parseOptions.Content
        let proj = parseOptions.Project
        if fileName = null || not (MDLanguageService.SupportedFileName (fileName)) then null else

        Async.StartAsTask(
            cancellationToken = cancellationToken,
            computation = async {
            let shortFilename = Path.GetFileName fileName

            let curVersion = parseOptions.Content.Version
            let isObsolete =
                SourceCodeServices.IsResultObsolete(fun () -> 
                let doc = IdeApp.Workbench.GetDocument(parseOptions.FileName)
                let newVersion = doc.Editor.Version
                if newVersion.BelongsToSameDocumentAs(curVersion) && newVersion.CompareAge(curVersion) = 0
                then
                  LoggingService.LogInfo ("FSharpParser: Parse {0} is not obsolete", shortFilename)
                  false
                else
                  LoggingService.LogInfo ("FSharpParser: Parse {0} is obsolete type check cancelled", shortFilename)
                  true ) 

            let doc = new FSharpParsedDocument(fileName, Flags = ParsedDocumentFlags.NonSerializable)
            LoggingService.LogInfo ("FSharpParser: Parse {0}, ", shortFilename)
                                   
            match tryGetFilePath fileName proj with
            | None -> ()
            | Some filePath -> 
                let! results =
                  try
                    let projectFile = proj |> function null -> filePath | proj -> proj.FileName.ToString()
                    LoggingService.LogInfo ("FSharpParser: Running ParseAndCheckFileInProject for {0}", shortFilename)
                    languageService.GetTypedParseResultWithTimeout(projectFile, filePath, content.Text, AllowStaleResults.No, obsoleteCheck = isObsolete )
                  with
                  | :? TimeoutException ->
                    doc.IsInvalid <- true
                    LoggingService.LogWarning ("FSharpParser: ParseAndCheckFileInProject timed out for {0}", shortFilename)
                    async.Return None
                  | :? Tasks.TaskCanceledException ->
                    doc.IsInvalid <- true
                    LoggingService.LogWarning ("FSharpParser: ParseAndCheckFileInProject was cancelled for {0}", shortFilename)
                    async.Return None
                  | ex ->
                    doc.IsInvalid <- true
                    LoggingService.LogError("FSharpParser: Error processing ParseAndCheckFileResults for {0}", shortFilename, ex)
                    async.Return None
                match results with
                | Some results ->                                                                     
                  results.GetErrors()
                  |> Option.iter (Array.map formatError >> doc.AddRange)

                  //Try creating tokens
                  try
                    let readOnlyDoc = TextEditorFactory.CreateNewReadonlyDocument (parseOptions.Content, fileName)
                    let lineDetails =
                      [ for i in 1..readOnlyDoc.LineCount do
                          let line = readOnlyDoc.GetLine(i)
                          yield Tokens.LineDetail(line.LineNumber, line.Offset, readOnlyDoc.GetTextAt(line.Offset, line.Length)) ]
                    let defines = CompilerArguments.getDefineSymbols filePath (proj |> Option.ofNull)
                    let tokens = Tokens.getTokens lineDetails filePath defines
                    doc.Tokens <- Some(tokens)
                  with ex ->
                    LoggingService.LogWarning ("FSharpParser: Couldn't update token information", ex)

                  //Set code folding regions, GetNavigationItems may throw in some situations
                  try 
                    let regions = 
                      let processDecl (decl : SourceCodeServices.FSharpNavigationDeclarationItem) = 
                        let m = decl.Range
                        FoldingRegion(decl.Name, DocumentRegion(m.StartLine, m.StartColumn + 1, m.EndLine, m.EndColumn + 1))
                      seq {for toplevel in results.GetNavigationItems() do
                             yield processDecl toplevel.Declaration
                             for next in toplevel.Nested do
                               yield processDecl next }
                    regions |> doc.AddRange
                  with ex -> LoggingService.LogWarning ("FSharpParser: Couldn't update navigation items.", ex)
                  //Store the AST of active results
                  doc.Ast <- results
                | None ->
                  doc.IsInvalid <- true
                  LoggingService.LogError("FSharpParser: Error ParseAndCheckFileResults for {0} no results returned", shortFilename)

            doc.LastWriteTimeUtc <- try File.GetLastWriteTimeUtc(fileName) with _ -> DateTime.UtcNow
            return doc :> _})

