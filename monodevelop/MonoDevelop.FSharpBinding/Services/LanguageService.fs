// --------------------------------------------------------------------------------------
// Main file - contains types that call F# compiler service in the background, display
// error messages and expose various methods for to be used from MonoDevelop integration
// --------------------------------------------------------------------------------------

namespace MonoDevelop.FSharp
#nowarn "40"

open System
open System.IO
open System.Xml
open System.Text
open System.Threading
open System.Diagnostics

open MonoDevelop.Core
open MonoDevelop.Core.Assemblies
open MonoDevelop.Ide
open MonoDevelop.Ide.Tasks
open MonoDevelop.Ide.Gui
open MonoDevelop.Projects

open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.Completion
open ICSharpCode.NRefactory.Documentation
open ICSharpCode.NRefactory.Editor

open FSharp.CompilerBinding
open MonoDevelop.FSharp
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

open Microsoft.FSharp.Compiler.Ast  
open Microsoft.FSharp.Compiler.Range
// --------------------------------------------------------------------------------------

/// Contains settings of the F# language service
module ServiceSettings = 

  /// When making blocking calls from the GUI, we specify this value as the timeout, so that the GUI is not blocked forever
  let blockingTimeout = 500
  
  /// How often should we trigger the 'OnIdle' event and run background compilation of the current project?
  let idleTimeout = 3000

  /// When errors are reported, we don't show them immediately (because appearing bubbles while typing are annoying). 
  /// We show them when the user doesn't type anything new into the editor for the time specified here
  let errorTimeout = 1000

  // What version of the FSharp language are we supporting?  This will evenually be made a project/script parameter.
  let fsVersion = FSharpCompilerVersion.FSharp_3_0


// --------------------------------------------------------------------------------------
/// Formatting of tool-tip information displayed in F# IntelliSense
module internal TipFormatter = 

  /// A standard memoization function
  let memoize f = 
      let d = new Collections.Generic.Dictionary<_,_>(HashIdentity.Structural)
      fun x -> if d.ContainsKey x then d.[x] else let res = f x in d.[x] <- res; res

  /// Memoize the objects that manage access to XML files.
  // @todo consider if this needs to be a weak table in some way
  let xmlDocProvider = memoize (fun x -> try ICSharpCode.NRefactory.Documentation.XmlDocumentationProvider(x)
                                         with exn -> null)

  /// Return the XmlDocumentationProvider for an assembly
  let findXmlDocProviderForAssembly file  = 
      let tryExists s = try if File.Exists s then Some s else None with _ -> None
      let e = 
          match tryExists (Path.ChangeExtension(file,"xml")) with 
          | Some x -> Some x 
          | None -> tryExists (Path.ChangeExtension(file,"XML"))
      match e with 
      | None -> None
      | Some xmlFile ->
      let docReader = xmlDocProvider xmlFile
      if docReader = null then None else Some docReader

  let findXmlDocProviderForEntity (file, key:string)  = 
      match findXmlDocProviderForAssembly file with 
      | None -> None
      | Some docReader ->
          let doc = docReader.GetDocumentation key
          if String.IsNullOrEmpty doc then None else Some doc

  let (|MemberName|_|) (name:string) = 
      let dotRight = name.LastIndexOf '.'
      if dotRight < 1 || dotRight >= name.Length - 1 then None else
      let typeName = name.[0..dotRight-1]
      let elemName = name.[dotRight+1..]
      Some (typeName,elemName)

  let (|MethodKey|_|) (key:string) = 
     if key.StartsWith "M:" then 
         let key = key.[2..]
         let name,count,args = 
             if not (key.Contains "(") then key, 0, [| |] else
          
             let pieces = key.Split( [|'('; ')' |], StringSplitOptions.RemoveEmptyEntries)
             if pieces.Length < 2 then key, 0, [| |] else
             let nameAndCount = pieces.[0]
             let argsText = pieces.[1].Replace(")","")
             let args = argsText.Split(',')
             if nameAndCount.Contains "`" then 
                 let ps = nameAndCount.Split( [| '`' |],StringSplitOptions.RemoveEmptyEntries) 
                 ps.[0], (try int ps.[1] with _ -> 0) , args
             else
                 nameAndCount, 0, args
                 
         match name with 
         | MemberName(typeName,elemName) -> Some (typeName, elemName, count, args)
         | _ -> None
     else None

  let (|SimpleKey|_|) (key:string) = 
     if key.StartsWith "P:" || key.StartsWith "F:" || key.StartsWith "E:" then 
         let name = key.[2..]
        // printfn "AAA name = %A" name
         match name with 
         | MemberName(typeName,elemName) -> Some (typeName, elemName)
         | _ -> None
     else None

  let (|TypeKey|_|) (key:string) =
     if key.StartsWith "T:" then
        Some key
     else None

  let trySelectOverload (nodes: XmlNodeList, argsFromKey:string[]) =

      //printfn "AAA argsFromKey = %A" argsFromKey
      if (nodes.Count = 1) then Some nodes.[0] else
      
      let result = 
        [ for x in nodes -> x ] |> Seq.tryFind (fun curNode -> 
          let paramList = curNode.SelectNodes ("Parameters/*")
          
          Debug.WriteLine(sprintf "AAA paramList = %A" [ for x in paramList -> x.OuterXml ])
          
          (paramList <> null) &&
          (argsFromKey.Length = paramList.Count) 
          (* &&
          (p, paramList) ||> Seq.forall2 (fun pi pmi -> 
            let idString = GetTypeString pi.Type
            (idString = pmi.Attributes ["Type"].Value)) *) )

      match result with 
      | None -> None
      | Some node -> 
          let docs = node.SelectSingleNode ("Docs") 
          if docs = null then None else Some docs

  ///check helpxml exist
  let tryGetDoc key = 
    let helpTree = MonoDevelop.Projects.HelpService.HelpTree
    if helpTree = null then None 
    else try 
            let helpxml = helpTree.GetHelpXml(key)
            if helpxml = null then None else Some(helpxml)
         with ex -> Debug.WriteLine (sprintf "GetHelpXml failed for key %s:\r\n\t%A" key ex)
                    None  
                  
  let findMonoDocProviderForEntity (file, key) = 
      Debug.WriteLine (sprintf "key= %A, File= %A" key file) 
      let typeMemberFormatter name = "/Type/Members/Member[@MemberName='" + name + "']" 
      match key with
      | TypeKey(typ) -> 
          Debug.WriteLine (sprintf "Type Key = %s" typ )
          match tryGetDoc (typ) with
          | Some docXml -> if docXml = null then None else 
                           Debug.WriteLine (sprintf "TypeKey xml= <<<%s>>>" docXml.OuterXml )
                           Some docXml.OuterXml
          | None -> None
      | SimpleKey (parentId, name) -> 
          Debug.WriteLine (sprintf "SimpleKey parentId= %s, name= %s" parentId name )
          match tryGetDoc ("T:" + parentId) with
          | Some doc -> let docXml = doc.SelectSingleNode (typeMemberFormatter name)
                        if docXml = null then None else 
                        Debug.WriteLine (sprintf "SimpleKey xml= <<<%s>>>" docXml.OuterXml )
                        Some docXml.OuterXml
          | None -> None
      | MethodKey(parentId, name, count, args) -> 
          Debug.WriteLine (sprintf "MethodKey, parentId= %s, name= %s, count= %i args= %A" parentId name count args )
          match tryGetDoc ("T:" + parentId) with
          | Some doc -> let nodeXmls = doc.SelectNodes (typeMemberFormatter name)
                        let docXml = trySelectOverload (nodeXmls, args)
                        docXml |> Option.map (fun xml -> xml.OuterXml) 
          | None -> None
      | _ -> Debug.WriteLine (sprintf "**No match for key = %s" key)
             None
      
  let findDocForEntity (file, key)  = 
      match findXmlDocProviderForEntity (file, key) with 
      | Some doc -> Some doc
      | None -> findMonoDocProviderForEntity (file, key) 
  
  /// Format some of the data returned by the F# compiler
  let private buildFormatComment cmt = 
    match cmt with
    | XmlCommentText(s) -> Tooltips.getTooltip Styles.simpleMarkup <| s.Trim()
    | XmlCommentSignature(file,key) -> 
        match findDocForEntity (file, key) with 
        | None -> String.Empty
        | Some doc -> Tooltips.getTooltip Styles.simpleMarkup doc
    | _ -> String.Empty


  /// Format some of the data returned by the F# compiler
  ///
  /// If 'canAddHeader' is true (meaning that this is the only tip displayed) then we add first line 
  //  "Multiple overloads" because MD prints first int in bold (so that no overload is highlighted)
  let private buildFormatElement canAddHeader el (sb:StringBuilder) =
    match el with 
    | ToolTipElementNone -> ()
    | ToolTipElement(it, comment) -> 
        Debug.WriteLine("DataTipElement: " + it)
        sb.AppendLine(GLib.Markup.EscapeText it) |> ignore
        let html = buildFormatComment comment 
        if not (String.IsNullOrWhiteSpace html) then 
            sb.Append(GLib.Markup.EscapeText "\n")  |> ignore
            sb.AppendLine(html) |> ignore
    | ToolTipElementGroup(items) -> 
        let items, msg = 
          if items.Length > 10 then 
            (items |> Seq.take 10 |> List.ofSeq), sprintf "   <i>(+%d other overloads)</i>" (items.Length - 10) 
          else items, null
        if (canAddHeader && items.Length > 1) then
          sb.AppendLine("Multiple overloads") |> ignore
        items |> Seq.iteri (fun i (it,comment) -> 
          sb.AppendLine(GLib.Markup.EscapeText it)  |> ignore
          if i = 0 then 
              let html = buildFormatComment comment 
              if not (String.IsNullOrWhiteSpace html) then 
                  sb.Append(GLib.Markup.EscapeText "\n")  |> ignore
                  sb.AppendLine(html) |> ignore
                  sb.Append(GLib.Markup.EscapeText "\n")  |> ignore )
        if msg <> null then sb.Append(msg) |> ignore
    | ToolTipElementCompositionError(err) -> 
        sb.Append("Composition error: " + GLib.Markup.EscapeText(err)) |> ignore
      
    
  /// Format some of the data returned by the F# compiler
  let private buildFormatTip canAddHeader tip (sb:StringBuilder) = 
    match tip with
    | ToolTipText([single]) -> sb.Append(GLib.Markup.EscapeText "\n")  |> ignore
                               buildFormatElement true single sb
    | ToolTipText(its) -> 
        if canAddHeader then 
            sb.AppendLine("Multiple items") |> ignore
        its |> Seq.iteri (fun i item ->
          if i <> 0 then sb.AppendLine("\n--------------------\n") |> ignore
          buildFormatElement false item sb) 

  let splitLine (sb:StringBuilder) (line:string) lineWidth =
      let emit (s:string) = sb.Append(s) |> ignore
      let indent = line |> Seq.takeWhile (fun c -> c = ' ') |> Seq.length
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
              for i in 1 .. indent do emit " "
              emit word 
              emit " "
              i <- indent + word.Length + 1
              first <- true
      sb.AppendLine() |> ignore

  let wrap (text: String) lineWidth =
      let sb = StringBuilder()
      let lines = text.Split [|'\r';'\n'|]
      for line in lines  do
          if line.Length <= lineWidth then sb.AppendLine(line) |> ignore
          else splitLine sb line lineWidth
      sb.ToString()


  /// Format tool-tip that we get from the language service as string        
  let formatTip canAddHeader tip = 
      let sb = new StringBuilder()
      buildFormatTip canAddHeader tip sb
      let text = sb.ToString()

      //TODO: Use the current projects policy to get line length
      // Document.Project.Policies.Get<TextStylePolicy>(types) or fall back to: 
      // MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy (types)

      let wrapped = wrap text 120
      wrapped.Trim('\n', '\r')

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let private extractParamTipFromComment paramName comment =  
    match comment with
    | XmlCommentText(s) -> 
        Some(Tooltips.getParameterTip Styles.simpleMarkup s paramName)
    // For 'XmlCommentSignature' we can get documentation from 'xml' files, and via MonoDoc on Mono
    | XmlCommentSignature(file,key) -> 
        match findXmlDocProviderForAssembly file with 
        | None -> None
        | Some docReader ->
            let doc = docReader.GetDocumentation(key)
            if String.IsNullOrEmpty(doc) then  None else
            let parameterTip = Tooltips.getParameterTip Styles.simpleMarkup doc paramName
            Some ( parameterTip )
    | _ -> None

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let private extractParamTipFromElement paramName element = 
      match element with 
      | ToolTipElementNone -> None
      | ToolTipElement(it, comment) -> extractParamTipFromComment paramName comment 
      | ToolTipElementGroup(items) -> List.tryPick (snd >> extractParamTipFromComment paramName) items
      | ToolTipElementCompositionError(err) -> None

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let extractParamTip paramName (ToolTipText elements) = 
      List.tryPick (extractParamTipFromElement paramName) elements

  ///Formats tool-tip and turns the first line into heading.  MonoDevelop does this automatically 
  ///for completion data, so we do the same thing explicitly for hover tool-tips
  let formatTipWithHeader tip = 
    let str = formatTip true tip
    let parts = str.Split([| '\n'; '\r' |], 2, StringSplitOptions.RemoveEmptyEntries)
    "<b>" + parts.[0] + "</b>" +
      (if parts.Length > 1 then "\n\n" + parts.[1] else "")

module MonoDevelop =
    let getLineInfoFromOffset (offset, doc:Mono.TextEditor.TextDocument) = 
        let loc  = doc.OffsetToLocation(offset)
        let line, col = max (loc.Line - 1) 0, loc.Column-1
        let currentLine = doc.Lines |> Seq.nth line
        let lineStr = doc.Text.Substring(currentLine.Offset, currentLine.EndOffset - currentLine.Offset)
        (line, col, lineStr)
                

/// Provides functionality for working with the F# interactive checker running in background
type internal MDLanguageService private () =

  // Single instance of the language service
  static let instance = Lazy.Create(fun () ->  FSharp.CompilerBinding.LanguageService(
    (fun changedfile ->
        DispatchService.GuiDispatch(fun () -> 
            try Debug.WriteLine(sprintf "Parsing: Considering re-typcheck of: '%s' because compiler reports it needs it" changedfile)
                let doc = IdeApp.Workbench.ActiveDocument
                if doc <> null && doc.FileName.FullPath.ToString() = changedfile then 
                    Debug.WriteLine(sprintf "Parsing: Requesting re-parse of: '%s' because some errors were reported asynchronously" changedfile)
                    doc.ReparseDocument()
            with exn  -> () ))))
                
  //Single instance of the language service
  static member Instance = instance.Value
    
// --------------------------------------------------------------------------------------
/// Various utilities for working with F# language service
module internal ServiceUtils =
  let map =           
    [ 0x0000, "md-class"; 0x0003, "md-enum"; 0x00012, "md-struct";
      0x00018, "md-struct" (* value type *); 0x0002, "md-delegate"; 0x0008, "md-interface";
      0x000e, "md-class" (* module *); 0x000f, "md-name-space"; 0x000c, "md-method";
      0x000d, "md-extensionmethod" (* method2 ? *); 0x00011, "md-property";
      0x0005, "md-event"; 0x0007, "md-field" (* fieldblue ? *);
      0x0020, "md-field" (* fieldyellow ? *); 0x0001, "md-field" (* const *);
      0x0004, "md-property" (* enummember *); 0x0006, "md-class" (* exception *);
      0x0009, "md-text-file-icon" (* TextLine *); 0x000a, "md-regular-file" (* Script *);
      0x000b, "Script" (* Script2 *); 0x0010, "md-tip-of-the-day" (* Formula *);
      0x00013, "md-class" (* Template *); 0x00014, "md-class" (* Typedef *);
      0x00015, "md-class" (* Type *); 0x00016, "md-struct" (* Union *);
      0x00017, "md-field" (* Variable *); 0x00019, "md-class" (* Intrinsic *);
      0x0001f, "md-breakpint" (* error *); 0x00021, "md-misc-files" (* Misc1 *);
      0x0022, "md-misc-files" (* Misc2 *); 0x00023, "md-misc-files" (* Misc3 *); ] |> Map.ofSeq 

  /// Translates icon code that we get from F# language service into a MonoDevelop icon
  let getIcon glyph =
    match map.TryFind (glyph / 6), map.TryFind (glyph % 6) with  
    | Some(s), _ -> s // Is the second number good for anything?
    | _, _ -> "md-breakpoint"
