namespace MonoDevelop.FSharp

open System
open System.Diagnostics
open System.IO
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Core
open MonoDevelop.Ide.TypeSystem
open ICSharpCode.NRefactory.TypeSystem

type FSharpParsedDocument(fileName) = 
     inherit DefaultParsedDocument(fileName)
  
// An instance of this type is created by MonoDevelop (as defined in the .xml for the AddIn) 
type FSharpParser() =
  inherit TypeSystemParser()
  do Debug.WriteLine("Parsing: Creating FSharpParser")
        
  /// Holds the previous errors reported by a file. 
  let activeResults = System.Collections.Generic.Dictionary<string,Error list>()

  /// Holds the previous content used to generate the previous errors. An entry is only present if we have 
  /// scheduled a new ReparseDocument() to update the errors.
  let activeRequests = System.Collections.Generic.Dictionary<string,string>()

  override x.Parse(storeAst:bool, fileName:string, content:System.IO.TextReader, proj:MonoDevelop.Projects.Project) =
    if fileName = null || not (CompilerArguments.supportedExtension(IO.Path.GetExtension(fileName))) then null else

    let fileContent = content.ReadToEnd()

    Debug.WriteLine("[Thread {0}] Parsing: Update in FSharpParser.Parse to file {1}, hash {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, hash fileContent)

    let doc = new FSharpParsedDocument(fileName)
    doc.Flags <- doc.Flags ||| ParsedDocumentFlags.NonSerializable

    // Check if this is a reparse.
    match activeRequests.TryGetValue(fileName) with 
    | true, content when content = fileContent ->
        activeRequests.Remove(fileName) |> ignore

    | _ ->
  
      Debug.WriteLine("[Thread {0}]: TriggerParse file {1}, hash {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, hash fileContent)
      // Trigger a parse/typecheck in the background. After the parse/typecheck is completed, request another parse to report the errors.
      //
      // Trigger parsing in the language service 
      let filePathOpt = 
          // TriggerParse will work only for full paths
          if IO.Path.IsPathRooted(fileName) then 
              Some(FilePath(fileName) )
          else 
             let doc = IdeApp.Workbench.ActiveDocument 
             if doc <> null then 
                 let file = doc.FileName
                 if file.FullPath.ToString() = "" then None else Some file
             else None

      match filePathOpt with 
      | None -> ()
      | Some filePath -> 
        let config = IdeApp.Workspace.ActiveConfiguration
        if config <> null then 
          // Keep a record that we have an inflight check of this going on
          activeRequests.[fileName] <- fileContent
          LanguageService.Service.TriggerParse(filePath, fileContent, proj, config, afterCompleteTypeCheckCallback=(fun (_,errors) ->

                    Debug.WriteLine("[Thread {0}]: Callback after parsing, file {1}, hash {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, fileName, hash fileContent)

                    // Keep the result until we reparse
                    activeResults.[fileName] <- errors

                    // Schedule a reparse to actually update the errors, checking first if this is still the active document
                    try 
                       let doc = IdeApp.Workbench.ActiveDocument
                       if doc <> null && doc.FileName.FullPath.ToString() = fileName then 
                           Debug.WriteLine("[Thread {0}]: Parsing: Requesting re-parse of file {1}, hash {2}",System.Threading.Thread.CurrentThread.ManagedThreadId,fileName, hash fileContent)
                           doc.ReparseDocument()
                    with _ -> ()))

    if activeResults.ContainsKey(fileName) then
        for er in activeResults.[fileName] do 
            doc.Errors.Add(er)    

    doc.LastWriteTimeUtc <- (try File.GetLastWriteTimeUtc (fileName) with _ -> DateTime.UtcNow) 
    doc :> ParsedDocument
