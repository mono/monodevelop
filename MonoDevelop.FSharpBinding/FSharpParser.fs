namespace MonoDevelop.FSharp

open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open System.Threading

type FSharpParsedDocument(fileName) = 
    inherit DefaultParsedDocument(fileName)
    member val Tokens = None with get,set
    member val AllSymbolsKeyed = Dictionary<_,_>() with get, set

module ParsedDocument =
  let create (parseOptions: ParseOptions, parseResults: ParseAndCheckResults, defines) =
    //Try creating tokens
    async {
      let fileName = parseOptions.FileName
      let shortFilename = Path.GetFileName fileName
      let doc = new FSharpParsedDocument(fileName, Flags = ParsedDocumentFlags.NonSerializable)
      LoggingService.LogDebug ("FSharpParser: Processing tokens on {0}", shortFilename)
      try
        let readOnlyDoc = TextEditorFactory.CreateNewReadonlyDocument (parseOptions.Content, fileName)
        let lineDetails =
          [ for i in 1..readOnlyDoc.LineCount do
              let line = readOnlyDoc.GetLine(i)
              yield Tokens.LineDetail(line.LineNumber, line.Offset, readOnlyDoc.GetTextAt(line.Offset, line.Length)) ]
        let tokens = Tokens.getTokens lineDetails fileName defines
        doc.Tokens <- Some(tokens)
      with ex ->
        LoggingService.LogWarning ("FSharpParser: Couldn't update token information", ex)
      
      //Get all the symboluses now rather than in semantic highlighting
      LoggingService.LogDebug ("FSharpParser: Processing symbol uses on {0}", shortFilename)
  
      /// Format errors for the given line (if there are multiple, we collapse them into a single one)
      let formatError (error : FSharpErrorInfo) = 
        // Single error for this line
        let errorType = 
          if error.Severity = FSharpErrorSeverity.Error then ErrorType.Error
          else ErrorType.Warning
        Error(errorType, String.wrapText error.Message 80, DocumentRegion (error.StartLineAlternate, error.StartColumn + 1, error.EndLineAlternate, error.EndColumn + 1))

      parseResults.GetErrors() |> (Seq.map formatError >> doc.AddRange)
      let! allSymbolUses = parseResults.GetAllUsesOfAllSymbolsInFile()
      match allSymbolUses with
      | Some symbolUses ->
        for symbolUse in symbolUses do
          if not (doc.AllSymbolsKeyed.ContainsKey symbolUse.RangeAlternate.End)
          then doc.AllSymbolsKeyed.Add(symbolUse.RangeAlternate.End, symbolUse)
      | None -> ()

      //Set code folding regions, GetNavigationItems may throw in some situations
      LoggingService.LogDebug ("FSharpParser: processing regions on {0}", shortFilename)
      try 
        let regions = 
          let processDecl (decl : SourceCodeServices.FSharpNavigationDeclarationItem) = 
            let m = decl.Range
            FoldingRegion(decl.Name, DocumentRegion(m.StartLine, m.StartColumn + 1, m.EndLine, m.EndColumn + 1))

          seq {for toplevel in parseResults.GetNavigationItems() do
                 yield processDecl toplevel.Declaration
                 for next in toplevel.Nested do
                   yield processDecl next }
        regions |> doc.AddRange
      with ex -> LoggingService.LogWarning ("FSharpParser: Couldn't update navigation items.", ex)
      //Store the AST of active results
      doc.Ast <- parseResults
      return doc
    }

// An instance of this type is created by MonoDevelop (as defined in the .xml for the AddIn) 
type FSharpParser() = 
    inherit TypeSystemParser()

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

        let shortFilename = Path.GetFileName fileName
        LoggingService.LogDebug ("FSharpParser: Parse starting on {0}", shortFilename)

        let isObsolete filename version cancellationRequested =
          SourceCodeServices.IsResultObsolete(fun () ->
            let shortFilename = Path.GetFileName filename
            try
              if not cancellationRequested then
                match MonoDevelop.tryGetVisibleDocument filename with
                | Some doc ->
                  let newVersion = doc.Editor.Version
                  if newVersion.BelongsToSameDocumentAs(version) && newVersion.CompareAge(version) = 0 then
                    false
                  else
                    LoggingService.LogDebug ("FSharpParser: Parse {0} is obsolete type check cancelled, file has changed", shortFilename)
                    true
                | None ->
                  LoggingService.LogDebug ("FSharpParser: Parse {0} is obsolete type check cancelled, file no longer visible", shortFilename)
                  true
              else
                LoggingService.LogDebug ("FSharpParser: Parse {0} is obsolete type check cancelled by cancellationToken", shortFilename)
                true
            with ex ->
              LoggingService.LogDebug ("FSharpParser: Parse {0} unable to determine cancellation due to exception", shortFilename, ex)
              false ) 

        Async.StartAsTask(
            cancellationToken = cancellationToken,
            computation = async {

            let doc = async {                    
              match tryGetFilePath fileName proj with
              | Some filePath -> 
                  LoggingService.LogDebug ("FSharpParser: Running ParseAndCheckFileInProject for {0}", shortFilename)
                  let projectFile = proj |> function null -> filePath | proj -> proj.FileName.ToString()
                  let obsolete = isObsolete parseOptions.FileName parseOptions.Content.Version cancellationToken.IsCancellationRequested
                  let! results = languageService.ParseAndCheckFileInProject(projectFile, filePath, 0, content.Text, obsolete)
                  //if you ever want to see the current parse tree
                  //let pt = match results.ParseTree with Some pt -> sprintf "%A" pt | _ -> "" 
                  LoggingService.LogDebug ("FSharpParser: Parse and check results retieved on {0}", shortFilename)
                  let defines = CompilerArguments.getDefineSymbols filePath (proj |> Option.ofNull)
                  return! ParsedDocument.create(parseOptions, results , defines)
              | None -> return FSharpParsedDocument(fileName, Flags = ParsedDocumentFlags.NonSerializable)} |> Async.RunSynchronously
            
            doc.LastWriteTimeUtc <- try File.GetLastWriteTimeUtc(fileName) with _ -> DateTime.UtcNow
            LoggingService.LogDebug ("FSharpParser: returning ParsedDocument on {0}", shortFilename)
            return doc :> _})