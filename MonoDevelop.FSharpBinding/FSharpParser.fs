namespace MonoDevelop.FSharp

open System
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Core
open MonoDevelop.Ide.TypeSystem

type FSharpParsedDocument(fileName) = 
     inherit ParsedDocument(fileName)
  

type FSharpParser() =
  inherit AbstractTypeSystemParser()
  
  //override x.CanParse(fileName) =
  //  Common.supportedExtension(IO.Path.GetExtension(fileName))
      
  member x.Parse(dom:Document, fileName:string, fileContent:string) : ParsedDocument =
    Debug.tracef "Parsing" "Update in FSharpParser.Parse"
  
    // If this call is not a result of updating errors (but a usual call 
    // from MonoDevelop), then we trigger update in the background
    if not LanguageService.Service.UpdatingErrors then 
      // Trigger parsing in the language service 
      let config = IdeApp.Workspace.ActiveConfiguration
      if IO.Path.IsPathRooted(fileName) then
        // TriggerParse will work only for full paths
        LanguageService.Service.TriggerParse(FilePath(fileName), fileContent, dom, config)
      elif IdeApp.Workbench.ActiveDocument <> null then
        // Let's try re-parse the current document?
        let file = IdeApp.Workbench.ActiveDocument.FileName
        if file.FullPath.ToString() <> "" then
          LanguageService.Service.TriggerParse(file, fileContent, dom, config)
      
    // Create parsed document with the results from the last type-checking      
    // (we could wait, but that would probably take a long time)
    let doc = new FSharpParsedDocument(fileName)
    for er in LanguageService.Service.Errors do doc.Errors.Add(er)    
    doc :> ParsedDocument
