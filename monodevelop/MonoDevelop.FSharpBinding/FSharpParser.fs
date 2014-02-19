namespace MonoDevelop.FSharp

open System
open System.Diagnostics
open System.IO
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Core
open MonoDevelop.Ide.TypeSystem
open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler
open FSharp.CompilerBinding

type FSharpParsedDocument(fileName) = 
     inherit DefaultParsedDocument(fileName)
  
// An instance of this type is created by MonoDevelop (as defined in the .xml for the AddIn) 
type FSharpParser() =
  inherit TypeSystemParser()
  do Debug.WriteLine("Parsing: Creating FSharpParser")
        
  /// Format errors for the given line (if there are multiple, we collapse them into a single one)
  let formatError (error:ErrorInfo) =
      // Single error for this line
      let typ = if error.Severity = Severity.Error then ErrorType.Error
                else ErrorType.Warning
      Error(typ, error.Message, DomRegion(error.StartLine + 1, error.StartColumn + 1, error.EndLine + 1, error.EndColumn + 1))
  
  /// To be called from the language service mailbox processor (on a 
  /// GUI thread!) when new errors are reported for the specified file
  let makeErrors(currentErrors:ErrorInfo[]) = 
    [ for error in currentErrors do
          yield formatError error ]

  override x.Parse(storeAst:bool, fileName:string, content:System.IO.TextReader, proj:MonoDevelop.Projects.Project) =
    if fileName = null || proj = null || not (CompilerArguments.supportedExtension(Path.GetExtension(fileName))) then null else

    let fileContent = content.ReadToEnd()

    Debug.WriteLine("[Thread {0}] Parsing: Update in FSharpParser.Parse to file {1}, hash {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, hash fileContent)

    let doc = new FSharpParsedDocument(fileName)
    doc.Flags <- doc.Flags ||| ParsedDocumentFlags.NonSerializable
       
    Debug.WriteLine("[Thread {0}]: TriggerParse file {1}, hash {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, hash fileContent)
    // Trigger a parse/typecheck in the background. After the parse/typecheck is completed, request another parse to report the errors.
    //
    // Trigger parsing in the language service 
    let filePathOpt = 
        // TriggerParse will work only for full paths
        if IO.Path.IsPathRooted(fileName) then 
            Some(fileName)
        else 
           let doc = IdeApp.Workbench.ActiveDocument 
           if doc <> null then 
               let file = doc.FileName.ToString()
               if file = "" then None else Some file
           else None
    
    let callTriggerParseAndWaitForResults(fileName, filePath, fileContent, files, args, framework) =
        use wh = new Threading.ManualResetEvent(false)
        let errs = ref Array.empty
        MDLanguageService.Instance.TriggerParse(fileName, filePath, fileContent, files, args, framework,
            (fun (_,errors) -> errs := errors
                               wh.Set() |> ignore ))
        wh.WaitOne(400) |> ignore
        !errs
                 
    match filePathOpt with 
    | None -> doc :> _
    | Some filePath -> 
      let config = IdeApp.Workspace.ActiveConfiguration
      if config = null then doc :> _ else 
        let files = CompilerArguments.getSourceFiles(proj.Items) |> Array.ofList
        let args = CompilerArguments.getArgumentsFromProject(proj, config)
        let framework = CompilerArguments.getTargetFramework( (proj :?> MonoDevelop.Projects.DotNetProject).TargetFramework.Id)
  
        let errors = callTriggerParseAndWaitForResults(proj.FileName.ToString(), filePath, fileContent, files, args, framework)
        //add the errors to the doc
        for er in makeErrors errors do
            doc.Errors.Add(er) 
  
        let typedParsedResult = MDLanguageService.Instance.GetTypedParseResult(proj.FileName.ToString(), filePath, fileContent, files, args, true, 400, framework)
  
        //GetNavigationItems may throw in some situations
        try
          let regions =
            let processDecl (decl:SourceCodeServices.DeclarationItem) =
              let (sc,sl), (fc,fl) = decl.Range
              FoldingRegion(decl.Name, DomRegion(sl,sc+1, fl,fc+1))
                  
            seq{for toplevel in typedParsedResult.ParseFileResults.GetNavigationItems().Declarations do
                  yield processDecl toplevel.Declaration
                  for next in toplevel.Nested do yield processDecl next}
          doc.Add(regions)
        with _ -> Debug.Assert(false, "couldn't update navigation items, ignoring")  
  
        if storeAst then
            doc.Ast <- typedParsedResult
        //update the write time as we have updated the AST
        doc.LastWriteTimeUtc <- try File.GetLastWriteTimeUtc fileName
                                with _ -> DateTime.UtcNow    

        doc :> ParsedDocument
