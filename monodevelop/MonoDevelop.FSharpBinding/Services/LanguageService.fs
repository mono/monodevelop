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

open Microsoft.FSharp.Compiler.SourceCodeServices

module FsParser = Microsoft.FSharp.Compiler.Parser

// --------------------------------------------------------------------------------------

/// Contains settings of the F# language service
module ServiceSettings = 

  /// When making blocking calls from the GUI, we specify this
  /// value as the timeout, so that the GUI is not blocked forever
  let blockingTimeout = 500
  
  /// How often should we trigger the 'OnIdle' event and run
  /// background compilation of the current project?
  let idleTimeout = 3000

  /// When errors are reported, we don't show them immediately (because appearing
  /// bubbles while typing are annoying). We show them when the user doesn't
  /// type anything new into the editor for the time specified here
  let errorTimeout = 1000

  // What version of the FSharp language are we supporting? 
  // This will evenually be made a project/script parameter.
  let fsVersion = FSharpCompilerVersion.FSharp_3_0


// --------------------------------------------------------------------------------------
/// Formatting of tool-tip information displayed in F# IntelliSense
module internal TipFormatter = 

  /// A standard memoization function
  let memoize f = 
      let d = new System.Collections.Generic.Dictionary<_,_>(HashIdentity.Structural)
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
          if System.String.IsNullOrEmpty doc then None else Some doc

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
          
             let pieces = key.Split( [|'('; ')' |], System.StringSplitOptions.RemoveEmptyEntries)
             if pieces.Length < 2 then key, 0, [| |] else
             let nameAndCount = pieces.[0]
             let argsText = pieces.[1].Replace(")","")
             let args = argsText.Split(',')
             if nameAndCount.Contains "`" then 
                 let ps = nameAndCount.Split( [| '`' |],System.StringSplitOptions.RemoveEmptyEntries) 
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
      | SimpleKey (parentId, name) -> 
          Debug.WriteLine (sprintf "SimpleKey parentId= %s, name= %s" parentId name )
          match tryGetDoc ("T:" + parentId) with
          | Some doc -> let docXml = doc.SelectSingleNode (typeMemberFormatter name)
                        Debug.WriteLine (sprintf "SimpleKey xml (simple)= null" )
                        if docXml = null then None else 
                        Debug.WriteLine (sprintf "Simple xml (simple)= <<<%s>>>" docXml.OuterXml )
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
    | DataTipElementNone -> ()
    | DataTipElement(it, comment) -> 
        Debug.WriteLine("DataTipElement: " + it)
        sb.AppendLine(GLib.Markup.EscapeText it) |> ignore
        let html = buildFormatComment comment 
        if not (String.IsNullOrWhiteSpace html) then 
            sb.Append(GLib.Markup.EscapeText "\n")  |> ignore
            sb.AppendLine(html) |> ignore
    | DataTipElementGroup(items) -> 
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
    | DataTipElementCompositionError(err) -> 
        sb.Append("Composition error: " + GLib.Markup.EscapeText(err)) |> ignore
      
  /// Format some of the data returned by the F# compiler
  let private buildFormatTip canAddHeader tip (sb:StringBuilder) = 
    match tip with
    | DataTipText([single]) -> sb.Append(GLib.Markup.EscapeText "\n")  |> ignore
                               buildFormatElement true single sb
    | DataTipText(its) -> 
        if canAddHeader then 
            sb.AppendLine("Multiple items") |> ignore
        its |> Seq.iteri (fun i item ->
          if i <> 0 then sb.AppendLine("\n--------------------\n") |> ignore
          buildFormatElement false item sb) 

  /// Format tool-tip that we get from the language service as string        
  let formatTip canAddHeader tip = 
    let sb = new StringBuilder()
    buildFormatTip canAddHeader tip sb
    let text = sb.ToString()
    let textSquashed =  MonoDevelop.Ide.TypeSystem.AmbienceService.BreakLines(text,90)
    textSquashed.Trim('\n', '\r')

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let private extractParamTipFromComment paramName comment =  
    match comment with
    | XmlCommentText(s) -> None
    // For 'XmlCommentSignature' we can get documentation from 'xml' files, and via MonoDoc on Mono
    | XmlCommentSignature(file,key) -> 
        match findXmlDocProviderForAssembly file with 
        | None -> None
        | Some docReader ->
            let doc = docReader.GetDocumentation(key)
            if System.String.IsNullOrEmpty(doc) then  None else
            let parameterTip = Tooltips.getParameterTip Styles.simpleMarkup doc paramName
            Some ( (*GLib.Markup.EscapeText( *) parameterTip )
    | _ -> None

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let private extractParamTipFromElement paramName element = 
      match element with 
      | DataTipElementNone -> None
      | DataTipElement(it, comment) -> extractParamTipFromComment paramName comment 
      | DataTipElementGroup(items) -> List.tryPick (snd >> extractParamTipFromComment paramName) items
      | DataTipElementCompositionError(err) -> None

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let extractParamTip paramName (DataTipText elements) = 
      List.tryPick (extractParamTipFromElement paramName) elements

  ///Formats tool-tip and turns the first line into heading.  MonoDevelop does this automatically 
  ///for completion data, so we do the same thing explicitly for hover tool-tips
  let formatTipWithHeader tip = 
    let str = formatTip true tip
    let parts = str.Split([| '\n'; '\r' |], 2, StringSplitOptions.RemoveEmptyEntries)
    "<b>" + parts.[0] + "</b>" +
      (if parts.Length > 1 then "\n\n" + parts.[1] else "")
    

// --------------------------------------------------------------------------------------
/// Parsing utilities for IntelliSense (e.g. parse identifier on the left-hand side
/// of the current cursor location etc.)
module Parsing = 
  open FSharp.Parser
  
  /// Parses F# short-identifier (i.e. not including '.'); also ignores active patterns
  let parseIdent =  
    many (sat PrettyNaming.IsIdentifierPartCharacter) |> map String.ofSeq

  let fsharpIdentCharacter = sat PrettyNaming.IsIdentifierPartCharacter
  /// Parse F# short-identifier and reverse the resulting string
  let parseBackIdent =  
    parser { 
        let! x = optional (string "``")
        let! res = many (if x.IsSome then (whitespace <|> fsharpIdentCharacter) else fsharpIdentCharacter) |> map String.ofReversedSeq 
        let! _ = optional (string "``") 
        return res }

  /// Parse remainder of a long identifier before '.' (e.g. "Name.space.")
  /// (designed to look backwards - reverses the results after parsing)
  let rec parseBackLongIdentRest = parser {
    return! parser {
      let! _ = char '.'
      let! ident = parseBackIdent
      let! rest = parseBackLongIdentRest
      return ident::rest }
    return [] } 
    
  /// Parse long identifier with residue (backwards) (e.g. "Console.Wri")
  /// and returns it as a tuple (reverses the results after parsing)
  let parseBackIdentWithResidue = parser {
    let! residue = many fsharpIdentCharacter 
    let residue = String.ofReversedSeq residue
    let! _ = optional (string "``")
    return! parser {
      let! long = parseBackLongIdentRest
      return residue, long |> List.rev }
    return residue, [] }   

  /// Parse long identifier and return it as a list (backwards, reversed)
  let parseBackLongIdent = parser {
    return! parser {
      let! ident = parseBackIdent
      let! rest = parseBackLongIdentRest
      return ident::rest |> List.rev }
    return [] }

  let parseBackTriggerThenLongIdent = parser {
    let! _ = char '('
    let! _  = many whitespace
    return! parseBackLongIdent
    }

  /// Create sequence that reads the string backwards
  let createBackStringReader (str:string) from = seq { 
    for i in (min from (str.Length - 1)) .. -1 .. 0 do yield str.[i] }

  /// Create sequence that reads the string forwards
  let createForwardStringReader (str:string) from = seq { 
    for i in (max 0 from) .. (str.Length - 1) do yield str.[i] }

  /// Returns first result returned by the parser
  let tryGetFirst p s = match apply p s with h::_ -> Some h | [] -> None
   
// --------------------------------------------------------------------------------------
/// Wraps the result of type-checking and provides methods for implementing
/// various IntelliSense functions (such as completion & tool tips)
type internal TypedParseResult(info:TypeCheckInfo) =
    let token = FsParser.tagOfToken(FsParser.token.IDENT("")) 

    let preCrack (offset, doc:Mono.TextEditor.TextDocument) = 
        let loc  = doc.OffsetToLocation(offset)
        let line, col = max (loc.Line - 1) 0, loc.Column
        let currentLine = doc.Lines |> Seq.nth line
        let lineStr = doc.Text.Substring(currentLine.Offset, currentLine.EndOffset - currentLine.Offset)
        (loc, line, col, currentLine, lineStr)

    // Parsing - find the identifier around the current location
    // (we look for full identifier in the backward direction, but only
    // for a short identifier forward - this means that when you hover
    // 'B' in 'A.B.C', you will get intellisense for 'A.B' module)
    let crackSymbolText (offset:int, doc:Mono.TextEditor.TextDocument) = 
    
        let loc, line, col, currentLine, lineStr = preCrack (offset, doc)
    
        let lookBack = Parsing.createBackStringReader lineStr (col-1)
        let lookForw = Parsing.createForwardStringReader lineStr col
    
        let backIdentOpt = Parsing.tryGetFirst Parsing.parseBackLongIdent lookBack
        match backIdentOpt with 
        | None -> None 
        | Some backIdent -> 
        let nextIdentOpt = Parsing.tryGetFirst Parsing.parseIdent lookForw
        match nextIdentOpt with 
        | None -> None 
        | Some nextIdent -> 
    
        let currentIdent, identIsland =
          match List.rev backIdent with
          | last::prev -> 
             let current = last + nextIdent
             current, current::prev |> List.rev
          | [] -> "", []

        Debug.WriteLine(sprintf "Result: Crack symbol text at %d:%d (offset %d - %d)\nIdentifier: %A (Current: %s) \nLine string: %s"  
                              line col currentLine.Offset currentLine.EndOffset identIsland currentIdent lineStr)

        let token = FsParser.tagOfToken(FsParser.token.IDENT("")) 
        match identIsland with
        | [] | [ "" ] -> None
        | _ -> Some (line,col,lineStr,identIsland,currentIdent,token)
        
    /// Crack the info prior to a '(' or ',' once the method tip trigger '(' shows
    let crackSymbolTextAtGetMethodsTrigger (offset:int, doc:Mono.TextEditor.TextDocument) = 

        let loc, line, col, currentLine, lineStr = preCrack (offset, doc)
        let lookBack = Parsing.createBackStringReader lineStr (col-1)
        let backIdentOpt = Parsing.tryGetFirst Parsing.parseBackTriggerThenLongIdent lookBack
        match backIdentOpt with 
        | None -> None 
        | Some backIdent -> 

        let currentIdent, identIsland =
          match List.rev backIdent with
          | last::prev -> last, (last::prev |> List.rev)
          | [] -> "", []

        match identIsland with
        | [] | [ "" ] -> None
        | _ -> Some (line,col,lineStr,identIsland,currentIdent,token)
        

    /// Get declarations at the current location in the specified document
    /// (used to implement dot-completion in 'FSharpTextEditorCompletion.fs')
    member x.GetDeclarations(doc:Document) = 
        let lineStr = doc.Editor.GetLineText(doc.Editor.Caret.Line)
    
        // Get the long identifier before the current location
        // 'residue' is the part after the last dot and 'longName' is before
        // e.g.  System.Console.Wri  --> "Wri", [ "System"; "Console"; ]
        let lookBack = Parsing.createBackStringReader lineStr (doc.Editor.Caret.Column - 2)
        match Parsing.tryGetFirst Parsing.parseBackIdentWithResidue lookBack with 
        | None -> DeclarationSet.Empty
        | Some (residue, longName) ->
    
        Debug.WriteLine(sprintf "Result: GetDeclarations: column: %d, ident: %A\n    Line: %s" (doc.Editor.Caret.Line - 1) (longName, residue) lineStr)
        let res = info.GetDeclarations( (doc.Editor.Caret.Line - 1, doc.Editor.Caret.Column - 1), lineStr, (longName, residue), 0, ServiceSettings.blockingTimeout) // 0 is tokenTag, which is ignored in this case

        Debug.WriteLine(sprintf "Result: GetDeclarations: returning %d items" res.Items.Length)
        res

  /// Get the tool-tip to be displayed at the specified offset (relatively
  /// from the beginning of the current document)
    member x.GetToolTip(offset:int, doc:Mono.TextEditor.TextDocument) =
        match crackSymbolText(offset, doc) with 
        | None -> DataTipText.Empty 
        | Some(line,col,lineStr,identIsland,currentIdent,token) ->
          let res = info.GetDataTipText((line, col), lineStr, identIsland, token)
          match res with
          | DataTipText(elems) when elems |> List.forall (function DataTipElementNone -> true | _ -> false) -> 
            // This works if we're inside "member x.Foo" and want to get 
            // tool tip for "Foo" (but I'm not sure why)
            Debug.WriteLine("Result: First attempt returned nothing"   )
            let res = info.GetDataTipText((line, col + 2), lineStr, [ currentIdent ], token)
            Debug.WriteLine( "Result: Returning the result of second try"   )
            res
          | _ -> 
            Debug.WriteLine( "Result: Got something, returning"  )
            res 

    member x.GetDeclarationLocation(offset:int, doc:Mono.TextEditor.TextDocument) =
        match crackSymbolText(offset, doc) with 
        | None -> FindDeclResult.NotFound 
        | Some(line,col,lineStr,identIsland,currentIdent,token) ->
            let res = info.GetDeclarationLocation((line, col), lineStr, identIsland, token, true)
            Debug.WriteLine( "Result: Got something, returning"  )
            res 

    member x.GetMethods(offset:int, doc:Mono.TextEditor.TextDocument) =
        match crackSymbolTextAtGetMethodsTrigger(offset, doc) with 
        | None -> None
        | Some(line,col,lineStr,identIsland,currentIdent,token) ->
            let res = info.GetMethods((line, col), lineStr, Some identIsland, token)
            Debug.WriteLine( "Result: Got something, returning"  )
            Some res 

// --------------------------------------------------------------------------------------

type internal AfterCompleteTypeCheckCallback = (FilePath * Error list -> unit) option

/// Represents request send to the background worker
/// We need information about the current file and project (options)
type internal ParseRequest (file:FilePath, source:string, options:CheckOptions, fullCompile:bool, afterCompleteTypeCheckCallback: AfterCompleteTypeCheckCallback) =
  member x.File  = file
  member x.Source = source
  member x.Options = options
  member x.StartFullCompile = fullCompile
  /// A callback that gets called asynchronously on a background thread after a full, complete and accurate typecheck of a file has finally completed.
  member x.AfterCompleteTypeCheckCallback = afterCompleteTypeCheckCallback
  
// --------------------------------------------------------------------------------------
// Language service - is a mailbox processor that deals with requests from the user
// interface - mainly to trigger background parsing or get current parsing results
// All processing in the mailbox is quick - however, if we don't have required info
// we post ourselves a message that will be handled when the info becomes available

type internal LanguageServiceMessage = 
  // Trigger parse request in ParserWorker
  | TriggerRequest of ParseRequest
  // Request for information - when we receive this, we parse and reply when information become available
  | UpdateAndGetTypedInfo of ParseRequest * AsyncReplyChannel<TypedParseResult>
  | GetTypedInfoDone of AsyncReplyChannel<TypedParseResult>
  

open System.Reflection
open FSharp.CompilerBinding.Reflection
open ICSharpCode.NRefactory.TypeSystem
open MonoDevelop.Ide.TypeSystem

/// Provides functionality for working with the F# interactive checker running in background
type internal LanguageService private () =

  // Single instance of the language service
  static let instance = Lazy.Create(fun () -> LanguageService())

  /// Format errors for the given line (if there are multiple, we collapse them into a single one)
  let formatError (error:ErrorInfo) =
      // Single error for this line
      let typ = if error.Severity = Severity.Error then ErrorType.Error else ErrorType.Warning
      new Error(typ, error.Message, DomRegion(error.StartLine + 1, error.StartColumn, error.EndLine + 1, error.EndColumn + 1))
  
  /// To be called from the language service mailbox processor (on a 
  /// GUI thread!) when new errors are reported for the specified file
  let makeErrors(currentErrors:ErrorInfo[]) = 
    [ for error in currentErrors do
          yield formatError error ]

  /// Load times used to reset type checking properly on script/project load/unload. It just has to be unique for each project load/reload.
  /// Not yet sure if this works for scripts.
  let fakeDateTimeRepresentingTimeLoaded proj = System.DateTime(abs (int64 (match proj with null -> 0 | _ -> proj.GetHashCode())) % 103231L)
  

  // -----------------------------------------------------------------------------------
  // Nuke the checker when the current requested language version changes
  let reqLangVersion = FSharpCompilerVersion.LatestKnown 
  
  // Create an instance of interactive checker. The callback is called by the F# compiler service
  // when its view of the prior-typechecking-state of the start of a file has changed, for example
  // when the background typechecker has "caught up" after some other file has been changed, 
  // and its time to re-typecheck the current file.
  let checker = 
   InteractiveChecker.Create(fun file -> 
      DispatchService.GuiDispatch(fun () ->
                    try 
                     Debug.WriteLine(sprintf "Parsing: Considering re-typcheck of file %s because compiler reports it needs it" file)
                     let doc = IdeApp.Workbench.ActiveDocument
                     if doc <> null && doc.FileName.FullPath.ToString() = file then 
                         Debug.WriteLine(sprintf "Parsing: Requesting re-parse of file '%s' because some errors were reported asynchronously and we should return a new document showing these" file)
                         doc.ReparseDocument()
                    with _ -> ()))

  // Mailbox of this 'LanguageService'
  let mbox = MailboxProcessor.Start(fun mbox ->
    
    // Tail-recursive loop that remembers the current state
    // (untyped and typed parse results)
    let rec loop typedInfo =
      mbox.Scan(fun msg ->
        Debug.WriteLine(sprintf "Worker: Checking message %s" (msg.GetType().Name))
        match msg, typedInfo with 
        | TriggerRequest(info), _ -> Some <| async {
          let newTypedInfo = 
           try
            Debug.WriteLine("Worker: TriggerRequest")
            let fileName = info.File.FullPath.ToString()        
            Debug.WriteLine("Worker: Request parse received")
            // Run the untyped parsing of the file and report result...
            Debug.WriteLine("Worker: Untyped parse...")
            let untypedInfo = try checker.UntypedParse(fileName, info.Source, info.Options) with e -> Debug.WriteLine(sprintf "Worker: Error in UntypedParse: %s" (e.ToString())); reraise ()
              
            // Now run the type-checking
            let fileName = CompilerArguments.fixFileName(fileName)
            Debug.WriteLine("Worker: Typecheck source...")
            let res = 
                try
                    checker.TypeCheckSource( untypedInfo, fileName, 0, info.Source,info.Options, IsResultObsolete(fun () -> false) ) 
                with e -> Debug.WriteLine(sprintf "Worker: Error in TypeCheckSource: %s" (e.ToString())); reraise ()
              
            // If this is 'full' request, then start background compilations too
            if info.StartFullCompile then
                Debug.WriteLine(sprintf "Worker: Starting background compilations")
                checker.StartBackgroundCompile(info.Options)
            Debug.WriteLine(sprintf "Worker: Parse completed")

            let file = info.File
            let updatedTyped = res

            // Construct new typed parse result if the task succeeded
            let newTypedInfo =
              match updatedTyped with
              | TypeCheckSucceeded(results) ->
                  // Handle errors on the GUI thread
                  Debug.WriteLine(sprintf "LanguageService: Update typed info - is some? %A" results.TypeCheckInfo.IsSome)
                  match info.AfterCompleteTypeCheckCallback with 
                  | None -> ()
                  | Some cb -> 
                      Debug.WriteLine (sprintf "Errors: Got update for: %s" (IO.Path.GetFileName(file.FullPath.ToString())))
                      DispatchService.GuiDispatch(fun () -> cb(file, makeErrors results.Errors))

                  match results.TypeCheckInfo with
                  | Some(info) -> Some(TypedParseResult(info))
                  | _ -> typedInfo
              | _ -> 
                  Debug.WriteLine("LanguageService: Update typed info - failed")
                  typedInfo
            newTypedInfo
           with e -> 
            Debug.WriteLine (sprintf "Errors: Got unexpected background error: %s" (e.ToString()))
            typedInfo
          return! loop newTypedInfo }

        
        // When we receive request for information and we don't have it we trigger a 
        // parse request and then send ourselves a message, so that we can reply later
        | UpdateAndGetTypedInfo(req, reply), _ -> Some <| async { 
            Debug.WriteLine ("LanguageService: UpdateAndGetTypedInfo")
            mbox.Post(TriggerRequest(req))
            mbox.Post(GetTypedInfoDone(reply)) 
            return! loop typedInfo }
                    
        | GetTypedInfoDone(reply), (Some typedRes) -> Some <| async {
            Debug.WriteLine (sprintf "LanguageService: GetTypedInfoDone")
            reply.Reply(typedRes)
            return! loop typedInfo }

        // We didn't have information to reply to a request - keep waiting for results!
        // The caller will probably timeout.
        | GetTypedInfoDone _, None -> 
            Debug.WriteLine("Worker: No match found for the message, leaving in queue until info is available")
            None )
        
    // Start looping with no initial information        
    async { // Delay a bit, on app startup let the projects load first
            do! Async.Sleep 4000
            return! loop None} )

  /// Constructs options for the interactive checker for the given file in the project under the given configuration.
  member x.GetCheckerOptions(fileName, source, proj:MonoDevelop.Projects.Project, config:ConfigurationSelector) =
    let ext = Path.GetExtension(fileName)
    let opts = 
      if (proj = null || ext = ".fsx" || ext = ".fsscript") then
      
        // We are in a stand-alone file or we are in a project, but currently editing a script file
        try
          let fileName = CompilerArguments.fixFileName(fileName)
          Debug.WriteLine (sprintf "CheckOptions: Creating for stand-alone file or script: '%s'" fileName )
          let opts = checker.GetCheckOptionsFromScriptRoot(fileName, source, fakeDateTimeRepresentingTimeLoaded proj)
          
          // The InteractiveChecker resolution doesn't sometimes
          // include FSharp.Core and other essential assemblies, so we need to include them by hand
          if opts.ProjectOptions |> Seq.exists (fun s -> s.Contains("FSharp.Core.dll")) then opts
          else 
            // Add assemblies that may be missing in the standard assembly resolution
            Debug.WriteLine("CheckOptions: Adding missing core assemblies.")
            let dirs = ScriptOptions.getDefaultDirectories (FSharpCompilerVersion.LatestKnown, TargetFrameworkMoniker.NET_4_0 )
            opts.WithOptions 
              [| yield! opts.ProjectOptions; 
                 match ScriptOptions.resolveAssembly dirs "FSharp.Core" with
                 | Some fn -> yield sprintf "-r:%s" fn
                 | None -> Debug.WriteLine("Resolution: FSharp.Core assembly resolution failed!")
                 match ScriptOptions.resolveAssembly dirs "FSharp.Compiler.Interactive.Settings" with
                 | Some fn -> yield sprintf "-r:%s" fn
                 | None -> Debug.WriteLine("Resolution: FSharp.Compiler.Interactive.Settings assembly resolution failed!") |]
        with e ->
          failwithf "Exception when getting check options for '%s'\n.Details: %A" fileName e
          
      // We are in a project - construct options using current properties
      else
        let projFile = proj.FileName.ToString()
        Debug.WriteLine (sprintf "CheckOptions: Creating for file '%s' in project '%s'" fileName projFile )
        let files = CompilerArguments.getSourceFiles(proj.Items) 
        
        // Read project configuration (compiler & build)
        let projConfig = proj.GetConfiguration(config) :?> DotNetProjectConfiguration
        let fsbuild = projConfig.ProjectParameters :?> FSharpProjectParameters
        let fsconfig = projConfig.CompilationParameters :?> FSharpCompilerParameters
        
        // Order files using the configuration settings & get options
        let shouldWrap = false //It is unknown if the IntelliSense fails to load assemblies with wrapped paths.
        let args = CompilerArguments.generateCompilerOptions (fsconfig, reqLangVersion, projConfig.TargetFramework.Id, proj.Items, config, shouldWrap) |> Array.ofList
        let root = Path.GetDirectoryName(proj.FileName.FullPath.ToString())
        let files = CompilerArguments.getItemsInOrder root files fsbuild.BuildOrder false |> Array.ofList
        CheckOptions.Create(projFile, files, args, false, false, fakeDateTimeRepresentingTimeLoaded proj) 

    // Print contents of check option for debugging purposes
    Debug.WriteLine(sprintf "Checkoptions: ProjectFileName: %s, ProjectFileNames: %A, ProjectOptions: %A, IsIncompleteTypeCheckEnvironment: %A, UseScriptResolutionRules: %A" 
                         opts.ProjectFileName opts.ProjectFileNames opts.ProjectOptions 
                         opts.IsIncompleteTypeCheckEnvironment opts.UseScriptResolutionRules)
    opts
  
  /// Parses and type-checks the given file in the given project under the given configuration. The callback
  /// is called after the complete typecheck has been performed.
  member x.TriggerParse(file:FilePath, src, proj:MonoDevelop.Projects.Project, config, afterCompleteTypeCheckCallback) = 
    let fileName = file.FullPath.ToString()
    let opts = x.GetCheckerOptions(fileName, src, proj, config)
    Debug.WriteLine(sprintf "Parsing: Trigger parse (fileName=%s)" fileName)
    mbox.Post(TriggerRequest(ParseRequest(file, src, opts, true, Some afterCompleteTypeCheckCallback)))

  member x.GetTypedParseResult(file:FilePath, src, proj:MonoDevelop.Projects.Project, config, timeout)  : TypedParseResult = 
    let fileName = file.FullPath.ToString()
    let opts = x.GetCheckerOptions(fileName, src, proj, config)
    Debug.WriteLine(sprintf "Parsing: Get typed parse result (fileName=%s)" fileName)
    let req = ParseRequest(file, src, opts, false, None)

    // Try to get recent results from the F# service
    match checker.TryGetRecentTypeCheckResultsForFile(fileName, req.Options) with
    | Some(untyped, typed, _) when typed.TypeCheckInfo.IsSome ->
        Debug.WriteLine(sprintf "Worker: Quick parse completed - success")
        TypedParseResult(typed.TypeCheckInfo.Value)
    | _ ->
        // If we didn't get a recent set of type checking results, we put in a request and wait for at most 'timeout' for a response
        mbox.PostAndReply((fun repl -> UpdateAndGetTypedInfo(req, repl)), timeout = timeout)
    
  /// Single instance of the language service
  static member Service = instance.Value
    
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