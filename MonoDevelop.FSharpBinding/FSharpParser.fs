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
  inherit AbstractTypeSystemParser()
  do Debug.WriteLine("Parsing: Creating FSharpParser")
        
  /// Holds the previous errors reported by a file. 
  let prevErrors = System.Collections.Generic.Dictionary<string,Error list>()

  /// Holds the previous content used to generate the previous errors. An entry is only present if we have 
  /// scheduled a new ReparseDocument() to update the errors.
  let prevContent = System.Collections.Generic.Dictionary<string,string>()

  interface ITypeSystemParser with
   override x.Parse(storeAst:bool, fileName:string, content:System.IO.TextReader, proj:MonoDevelop.Projects.Project) =
    let fileContent = content.ReadToEnd()
    Debug.WriteLine("Parsing: Update in FSharpParser.Parse")
  
    // Trigger a parse/typecheck in the background. After the parse/typecheck is completed, request another parse to report the errors.
    //
    // Skip this is this call is a result of updating errors and the content still matches.
    if fileName <> null && not (prevContent.ContainsKey(fileName) && prevContent.[fileName] = fileContent ) && CompilerArguments.supportedExtension(IO.Path.GetExtension(fileName)) then 
      // Trigger parsing in the language service 
      let filePathOpt = 
          // TriggerParse will work only for full paths
          if IO.Path.IsPathRooted(fileName) then 
              Some(FilePath(fileName) )
          else 
             let doc = IdeApp.Workbench.ActiveDocument 
             if doc <> null then 
                 let file = doc.FileName
                 if file.FullPath.ToString() <> "" then Some file else None
             else None
      match filePathOpt with 
      | None -> ()
      | Some filePath -> 
        let config = IdeApp.Workspace.ActiveConfiguration
        if config <> null then 
          LanguageService.Service.TriggerParse(filePath, fileContent, proj, config, afterCompleteTypeCheckCallback=(fun (fileName,errors) ->

                    let file = fileName.FullPath.ToString()
                    if file <> null then
                      prevErrors.[file] <- errors
                      prevContent.[file] <- fileContent
                      // Scheule another parse to actually update the errors 
                      try 
                         let doc = IdeApp.Workbench.ActiveDocument
                         if doc <> null && doc.FileName.FullPath.ToString() = file then 
                             Debug.WriteLine(sprintf "Parsing: Requesting re-parse of file '%s' because some errors were reported asynchronously and we should return a new document showing these" file)
                             doc.ReparseDocument()
                      with _ -> ()))

    // Create parsed document with the results from the last type-checking      
    // (we could wait, but that can take a long time)
    let doc = new FSharpParsedDocument(fileName)
    doc.Flags <- doc.Flags ||| ParsedDocumentFlags.NonSerializable
    let errors = 
      match fileName with
      | null -> []
      | _ ->
        match prevErrors.TryGetValue(fileName) with 
        | true,err -> 
            prevContent.Remove(fileName) |> ignore
            err
        | _ -> [ ] 

    for er in errors do 
        Debug.WriteLine(sprintf "Parsing: Adding error, message '%s', region '%A'" er.Message (er.Region.BeginLine,er.Region.BeginColumn,er.Region.EndLine,er.Region.EndColumn))
        doc.Errors.Add(er)    

    doc.LastWriteTimeUtc <- (try File.GetLastWriteTimeUtc (fileName) with _ -> DateTime.UtcNow) 
    doc :> ParsedDocument
