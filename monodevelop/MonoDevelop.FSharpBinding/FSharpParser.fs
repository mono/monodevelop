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
    do Debug.WriteLine("Parsing: Creating FSharpParser")
    
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
    let formatError (error : ErrorInfo) = 
        // Single error for this line
        let errorType = 
            if error.Severity = Severity.Error then ErrorType.Error
            else ErrorType.Warning
        Error(errorType, wrapText error.Message 80, DomRegion(error.StartLineAlternate, error.StartColumn + 1, error.EndLineAlternate, error.EndColumn + 1))
    
    override x.Parse(storeAst : bool, fileName : string, content : System.IO.TextReader, proj : MonoDevelop.Projects.Project) = 
        if fileName = null || not (CompilerArguments.supportedExtension (Path.GetExtension(fileName))) then null
        else 
            let fileContent = content.ReadToEnd()
            Debug.WriteLine
                ("[Thread {0}] Parsing: Update in FSharpParser.Parse to file {1}, hash {2}", 
                 System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, hash fileContent)
            let doc = new FSharpParsedDocument(fileName)
            doc.Flags <- doc.Flags ||| ParsedDocumentFlags.NonSerializable
            // Not sure if these are needed yet. 
            doc.CreateRefactoringContext <- Func<_, _, _>(fun doc token -> FSharpRefactoringContext() :> IRefactoringContext)
            doc.CreateRefactoringContextWithEditor <- Func<_, _, _, _> (fun data resolver token -> FSharpRefactoringContext() :> IRefactoringContext)
            Debug.WriteLine
                ("[Thread {0}]: TriggerParse file {1}, hash {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, 
                 hash fileContent)
            let filePathOpt = 
                // TriggerParse will work only for full paths
                if IO.Path.IsPathRooted(fileName) then Some(fileName)
                else 
                    let doc = IdeApp.Workbench.ActiveDocument
                    if doc <> null then 
                        let file = doc.FileName.FullPath.ToString()
                        if file = "" then None
                        else Some file
                    else None
            match filePathOpt with
            | None -> ()
            | Some filePath -> 
                let projFile, files, args, framework = MonoDevelop.getCheckerArgs (proj, filePath)
                let results = 
                    MDLanguageService.Instance.ParseAndCheckFileInProject(projFile, fileName, fileContent, files, args, framework, storeAst) 
                    |> Async.RunSynchronously
                match results.GetErrors() with
                | Some errors -> 
                    errors 
                    |> Array.map formatError 
                    |> doc.Add
                | _ -> ()
                //Set code folding regions, GetNavigationItems may throw in some situations
                try 
                    let regions = 
                        let processDecl (decl : SourceCodeServices.DeclarationItem) = 
                            let m = decl.Range
                            FoldingRegion(decl.Name, DomRegion(m.StartLine, m.StartColumn + 1, m.EndLine, m.EndColumn + 1))
                        seq { 
                            for toplevel in results.GetNavigationItems() do
                                yield processDecl toplevel.Declaration
                                for next in toplevel.Nested do
                                    yield processDecl next
                        }
                    doc.Add(regions)
                with _ -> Debug.Assert(false, "couldn't update navigation items, ignoring")
                //also store the AST of active results if applicable 
                //Is there any reason not to store the AST? The navigation extension depends on it
                if storeAst then doc.Ast <- results
            doc.LastWriteTimeUtc <- try File.GetLastWriteTimeUtc(fileName) with _ -> DateTime.UtcNow
            doc :> ParsedDocument
