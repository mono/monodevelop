namespace MonoDevelop.FSharp

open FSharp.CompilerBinding
open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.TypeSystem
open System 
open System.Diagnostics
open System.IO
open System.Text
open System.Threading

type FSharpParsedDocument(fileName) = 
    inherit DefaultParsedDocument(fileName)

// An instance of this type is created by MonoDevelop (as defined in the .xml for the AddIn) 
type FSharpParser() = 
    inherit TypeSystemParser()

    let languageService = MDLanguageService.Instance 
    /// Split a line so it fits to a line width
    let splitLine (sb : StringBuilder) (line : string) lineWidth = 
        let emit (s : string) = sb.Append(s) |> ignore
        
        let indent = 
            line
            |> Seq.takeWhile (fun c -> c = ' ')
            |> Seq.length
        
        let words = line.Split(' ')
        let mutable i = 0
        let mutable first = true
        for word in words do
            if first || i + word.Length < lineWidth then 
                emit word
                emit " "
                i <- i + word.Length + 1
                first <- false
            else 
                sb.AppendLine() |> ignore
                for i in 1..indent do
                    emit " "
                emit word
                emit " "
                i <- indent + word.Length + 1
                first <- true
        sb.AppendLine() |> ignore
    
    /// Wrap text so it fits to a line width
    let wrapText (text : String) lineWidth = 
        //dont wrap empty lines
        if text.Length = 0 then text
        else 
            let sb = StringBuilder()
            let lines = text.Split [| '\r'; '\n' |]
            for line in lines do
                if line.Length <= lineWidth then sb.AppendLine(line) |> ignore
                else splitLine sb line lineWidth
            sb.ToString()
    
    /// Format errors for the given line (if there are multiple, we collapse them into a single one)
    let formatError (error : FSharpErrorInfo) = 
        // Single error for this line
        let errorType = 
            if error.Severity = FSharpErrorSeverity.Error then ErrorType.Error
            else ErrorType.Warning
        Error(errorType, wrapText error.Message 80, Editor.DocumentRegion (error.StartLineAlternate, error.StartColumn + 1, error.EndLineAlternate, error.EndColumn + 1))

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

            let doc = new FSharpParsedDocument(fileName, Flags = ParsedDocumentFlags.NonSerializable)
            LoggingService.LogInfo ("FSharpParser: [Thread {0}] Parse {1}, ", Thread.CurrentThread.ManagedThreadId, shortFilename)
                                   
            match tryGetFilePath fileName proj with
            | None -> ()
            | Some filePath -> 
                let! results =
                  try
                    let projectFile = proj |> function null -> filePath | proj -> proj.FileName.ToString()
                    LoggingService.LogInfo ("FSharpParser: [Thread {0}] Running ParseAndCheckFileInProject for {1}", Thread.CurrentThread.ManagedThreadId, shortFilename)
                    languageService.GetTypedParseResultWithTimeout(projectFile, filePath, content.Text, AllowStaleResults.MatchingSource)
                  with
                  | :? TimeoutException ->
                    doc.IsInvalid <- true
                    LoggingService.LogWarning ("FSharpParser: [Thread {0}] ParseAndCheckFileInProject timed out for {1}", Thread.CurrentThread.ManagedThreadId, shortFilename)
                    async.Return None
                  | :? Tasks.TaskCanceledException ->
                    doc.IsInvalid <- true
                    LoggingService.LogWarning ("FSharpParser: [Thread {0}] ParseAndCheckFileInProject was cancelled for {1}", Thread.CurrentThread.ManagedThreadId, shortFilename)
                    async.Return None
                  | ex ->
                    doc.IsInvalid <- true
                    LoggingService.LogError("FSharpParser: [Thread {0}] Error processing ParseAndCheckFileResults for {1}", Thread.CurrentThread.ManagedThreadId, shortFilename, ex)
                    async.Return None
                match results with
                | Some results ->                                                                     
                  results.GetErrors()
                  |> Option.iter (Array.map formatError >> doc.AddRange)

                  //Set code folding regions, GetNavigationItems may throw in some situations
                  try 
                    let regions = 
                      let processDecl (decl : SourceCodeServices.FSharpNavigationDeclarationItem) = 
                        let m = decl.Range
                        FoldingRegion(decl.Name, Editor.DocumentRegion(m.StartLine, m.StartColumn + 1, m.EndLine, m.EndColumn + 1))
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
                  LoggingService.LogError("FSharpParser: [Thread {0}] Error ParseAndCheckFileResults for {1} no results returned", Thread.CurrentThread.ManagedThreadId, shortFilename)

            doc.LastWriteTimeUtc <- try File.GetLastWriteTimeUtc(fileName) with _ -> DateTime.UtcNow
            return doc :> _})

