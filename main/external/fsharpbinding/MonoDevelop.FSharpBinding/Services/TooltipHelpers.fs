#nowarn "44" // Don't warn about obsolete code. Monodoc.Generators isn't available on Windows
namespace MonoDevelop.FSharp
open System
open System.IO
open System.Text
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Xml
open System.Xml.Linq
open MonoDevelop.Core
open ExtCore.Control

[<RequireQualifiedAccess>]
type Style =
| Type of string
| Parameter of string
| Code of string
| Exception of string

module Styles =
    let simpleMarkup style =
        match style with
        | Style.Type name -> String.Format("<i>{0}</i> ", name)
        | Style.Parameter name -> String.Format("<i>{0}</i> ", name)
        | Style.Code name -> String.Format("<tt>{0}</tt> ", name)
        | Style.Exception name -> String.Format("\n<i>{0}</i>", name)

module Linq2Xml =
    let xn = XName.op_Implicit
    let xs ns local = XName.Get(local, ns)
    let firstOrDefault seq = Enumerable.FirstOrDefault(seq)
    let firstOrNone seq =
        let iter = Enumerable.FirstOrDefault(seq)
        match iter with null -> None | _ -> Some(iter)

    let singleOrDefault seq = Enumerable.SingleOrDefault(seq)
    let where (pred: XElement -> bool) elements = Enumerable.Where(elements, pred)
    let attribute name (element:XElement) = element.Attribute <| xn name
    let attributeValue name element = (attribute name element).Value
    let descendants xs (element: XElement) = element.Descendants(xs)
    let previousNodeOrNone (element: XElement) =
        match element.PreviousNode with
        | null -> None
        | node -> Some(node)

module TooltipsXml =
    open Linq2Xml
    let private strip start (str:string)=
        if str.StartsWith start then str.Substring(start.Length)
        else str

    let private trim (str:String) =
        str.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun s -> s.Trim() )
        |> String.concat(" ")

    let private unqualifyName (txt:String) = txt.Substring(txt.LastIndexOf(".") + 1)

    let private elementValue (style: Style -> string) (element:XElement) =
        let sb = StringBuilder()
        if element = null then sb else
        let rec processNodes (sb: StringBuilder) (nodes: IEnumerable<XNode>) =
            nodes.Aggregate(sb, fun acc node ->
                match node with
                | null -> acc
                | :? XElement as element ->
                       match element.Name.LocalName with
                       | "para" -> processNodes acc (element.Nodes())

                       | "see" -> match element |> attribute "cref" with
                                  | null -> acc
                                  | attrib -> let fragment = attrib.Value |> (strip "T:" >> unqualifyName >> Style.Type >> style)
                                              acc.Append(fragment)

                       | "paramref" -> match element |> attribute "name" with
                                       | null -> acc
                                       | attrib -> let fragment = attrib.Value |> (Style.Parameter >> style)
                                                   acc.Append(fragment)

                       | "c" -> let fragment = element.Value |> (GLib.Markup.EscapeText >> Style.Code >> style)
                                acc.Append(fragment)
                       | "attribution" -> acc //skip attribution elements
                       | unknown ->
                           LoggingService.LogError("Error in Tooltip parsing, unknown element in summary: " + unknown)
                           processNodes acc (element.Nodes())
                | :? XText as xt -> acc.AppendFormat("{0} ", xt.Value |> (GLib.Markup.EscapeText >> trim))
                | _ -> acc )
        processNodes sb (element.Nodes())

    let getTooltipSummary (style: Style -> string) (str:string) =
        try let xdoc =
                if str.StartsWith("<?xml") then XElement.Parse(str)
                else XElement.Parse("<Root>" + str + "</Root>")

            //if no nodes were found then return the string verbatim
            let anyNodes = xdoc.Descendants() |> Enumerable.Any
            if not anyNodes then str else
            let summary = xdoc.Descendants(xn "summary") |> firstOrDefault |> elementValue style

            xdoc.Elements(xn "exception")
            |> Seq.iteri (fun i element ->
                if i = 0 then summary.Append("\n\nExceptions\n") |> ignore
                match element |> attribute "cref" with
                | null -> ()
                | cref -> let exceptionType = cref.Value |> (strip "T:" >> unqualifyName >> Style.Exception >> style)
                          if i > 0 then summary.AppendLine() |> ignore
                          summary.AppendFormat( "{0}: {1}", exceptionType, element.Value) |> ignore)

            summary.ToString().TrimEnd()
        //if the summary cannot be parsed just escape the text
        with exn -> GLib.Markup.EscapeText str

    let getParameterTip (addStyle: Style -> string) (str:String) (param:String) =
        let xdoc =
            if str.StartsWith("<?xml") then XElement.Parse(str)
            else XElement.Parse("<Root>" + str + "</Root>")
        let par = xdoc.Descendants(xn "param")
                  |> where (fun element -> (element |> attribute "name").Value = param)
                  |> singleOrDefault
        if par = null then None else Some((elementValue addStyle par).ToString())

type FSharpXmlDocumentationProvider(xmlPath) =
    inherit Microsoft.CodeAnalysis.XmlDocumentationProvider()
    member x.XmlPath = xmlPath
    member x.GetDocumentation documentationCommentId =
        base.GetDocumentationForSymbol(documentationCommentId, Globalization.CultureInfo.CurrentCulture, CancellationToken.None)

    override x.GetSourceStream(_cancellationToken) =
        new FileStream(xmlPath, FileMode.Open, FileAccess.Read) :> Stream

    override x.Equals(obj) =
        obj 
        |> Option.tryCast<FSharpXmlDocumentationProvider>
        |> Option.bind(fun d -> Some (d.XmlPath = xmlPath))
        |> Option.fill false
            
    override x.GetHashCode() = xmlPath.GetHashCode()

module TooltipXmlDoc =
    ///lru based memoize
    let private memoize f n =
        let lru = ref (ExtCore.Caching.LruCache.create n)
        fun x -> match (!lru).TryFind x with
                 | Some entry, cache ->
                     lru := cache
                     entry
                 | None, cache ->
                     let res = f x
                     lru := cache.Add (x, res)
                     res
    
    /// Memoize the objects that manage access to XML files, keeping only 20 most used
    // @todo consider if this needs to be a weak table in some way
    let private xmlDocProvider =
        memoize (fun x ->
            try Some (FSharpXmlDocumentationProvider(x))
            with exn -> None) 20u
    
    let private tryExt file ext = Option.condition File.Exists (Path.ChangeExtension(file,ext))
    
    /// Return the XmlDocumentationProvider for an assembly
    let findXmlDocProviderForAssembly file  =
        maybe {let! xmlFile = Option.coalesce (tryExt file "xml") (tryExt file "XML")
               return! xmlDocProvider xmlFile }
    
    let findXmlDocProviderForEntity (file, key:string)  =
        maybe {let! docReader = findXmlDocProviderForAssembly file
               let doc = docReader.GetDocumentation key
               if String.IsNullOrEmpty doc then return! None
               else return doc}
    
    ///check helpxml exist
    let tryGetDoc key =
        try
            let helpTree = MonoDevelop.Projects.HelpService.HelpTree
            if helpTree = null then None else
            let helpxml = helpTree.GetHelpXml(key)
            if helpxml = null then None else Some(helpxml)
        with ex ->
            LoggingService.LogError ("GetHelpXml failed for key {0}", key, ex)
            None
    
    let (|MemberName|_|) (name:string) =
        let dotRight = name.LastIndexOf '.'
        if dotRight < 1 || dotRight >= name.Length - 1 then None else
        let typeName = name.[0..dotRight-1]
        let elemName = name.[dotRight+1..]
        Some ("T:" + typeName, elemName)
    
    let (|Method|_|) (key:string) =
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
                   let noArgs =
                       try int (ps.[1].Split([| '.' |], StringSplitOptions.RemoveEmptyEntries).[0] )
                       with _ -> 0
    
                   nameAndCount, noArgs, args
               else
                   nameAndCount, 0, args
    
           match name with
           | MemberName(typeName,elemName) -> Some (typeName, elemName, count, args)
           | _ -> None
       else None
    
    let (|FieldPropertyOrEvent|_|) (key:string) =
       if key.StartsWith "P:" || key.StartsWith "F:" || key.StartsWith "E:" then
           let name = key.[2..]
           match name with
           | MemberName(typeName,elemName) -> Some (typeName, elemName)
           | _ -> None
       else None
    
    let (|Type|_|) (key:string) =
       if key.StartsWith "T:" then
          Some key
       else None
    
    let private trySelectOverload (nodes: XmlNodeList, argsFromKey:string[]) =
        if (nodes.Count = 1) then Some nodes.[0] else
    
        let result =
          [ for x in nodes -> x ] |> Seq.tryFind (fun curNode ->
            let paramList = curNode.SelectNodes ("Parameters/*")
            let paramTypes = [| for p in paramList -> p.Attributes.GetNamedItem("Type").Value |]
            (paramList <> null) && (argsFromKey.Length = paramList.Count) && paramTypes = argsFromKey )
    
        match result with
        | None -> None
        | Some node ->
            let docs = node.SelectSingleNode ("Docs")
            if docs = null then None else Some docs
    
    let private typeMemberFormatter name =
      if name = "#ctor" then "/Type/Members/Member[@MemberName='.ctor']"
      else "/Type/Members/Member[@MemberName='" + name + "']"
    
    /// Try to find the MonoDoc documentation for a file/key pair representing an entity with documentation
    let findMonoDocProviderForEntity (_file, key) =
        match key with
        | Type(typ) ->
            maybe {let! docXml = tryGetDoc typ
                   return docXml.OuterXml}
        | FieldPropertyOrEvent (parentId, name) ->
            maybe {let! doc = tryGetDoc (parentId)
                   let docXml = doc.SelectSingleNode (typeMemberFormatter name)
                   return docXml.OuterXml }
        | Method(parentId, name, _count, args) ->
            maybe {
                    let! doc = tryGetDoc (parentId)
                    let nodeXmls = doc.SelectNodes (typeMemberFormatter name)
                    let! docXml = trySelectOverload (nodeXmls, args)
                    return docXml.OuterXml }
        | _ -> LoggingService.LogWarning ("findMonoDocProviderForEntity, No match for key: {0}", key)
               None
    
    /// Find the documentation for a file/key pair representing an entity with documentation
    let findDocForEntity (file, key)  =
        match findXmlDocProviderForEntity (file, key) with
        | Some doc -> Some doc
        | None -> findMonoDocProviderForEntity (file, key)

/// Formatting of TooltipElement information displayed in tooltips and autocompletion
module TooltipFormatting =
  open Microsoft.FSharp.Compiler.SourceCodeServices

  /// Format some of the data returned by the F# compiler
  let private buildFormatComment cmt =
    match cmt with
    | FSharpXmlDoc.Text(s) -> TooltipsXml.getTooltipSummary Styles.simpleMarkup <| s.Trim()
    | FSharpXmlDoc.XmlDocFileSignature(file,key) ->
        match TooltipXmlDoc.findDocForEntity (file, key) with
        | None -> String.Empty
        | Some doc -> TooltipsXml.getTooltipSummary Styles.simpleMarkup doc
    | _ -> String.Empty

  /// Format some of the data returned by the F# compiler
  let private buildFormatElement el =
    let signatureB, commentB = StringBuilder(), StringBuilder()
    match el with
    | FSharpToolTipElement.None -> ()
    | FSharpToolTipElement.Group(items) ->
        let items, msg =
            if items.Length > 10 then
                (items |> Seq.take 10 |> List.ofSeq), sprintf "   <i>(+%d other overloads)</i>" (items.Length - 10)
            else items, null
        if (items.Length > 1) then
            signatureB.AppendLine("Multiple overloads") |> ignore
        items |> Seq.iteri (fun i (tooltipData) ->
            signatureB.Append(GLib.Markup.EscapeText (tooltipData.MainDescription))  |> ignore
            if i = 0 then
                let html = buildFormatComment tooltipData.XmlDoc
                if not (String.IsNullOrWhiteSpace html) then
                    commentB.AppendLine(html) |> ignore
                    commentB.Append(GLib.Markup.EscapeText "\n")  |> ignore )
        if msg <> null then signatureB.Append(msg) |> ignore
    | FSharpToolTipElement.CompositionError(err) ->
        signatureB.Append("Composition error: " + GLib.Markup.EscapeText(err)) |> ignore
    signatureB.ToString().Trim(), commentB.ToString().Trim()

  /// Format tool-tip that we get from the language service as string
  //
  // TODO: Use the current projects policy to get line length
  // Document.Project.Policies.Get<TextStylePolicy>(types) or fall back to:
  // MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy (types)
  let formatTip (FSharpToolTipText(list)) =
      [ for item in list ->
          let signature, summary = buildFormatElement item
          signature, summary ]

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let private extractParamTipFromComment paramName comment =
    match comment with
    | FSharpXmlDoc.Text(s) -> TooltipsXml.getParameterTip Styles.simpleMarkup s paramName
    // For 'FSharpXmlDoc.XmlDocFileSignature' we can get documentation from 'xml' files, and via MonoDoc on Mono
    | FSharpXmlDoc.XmlDocFileSignature(file,key) ->
        maybe {let! docReader = TooltipXmlDoc.findXmlDocProviderForAssembly file
               let doc = docReader.GetDocumentation key
               if String.IsNullOrEmpty doc then return! None else
               let parameterTip = TooltipsXml.getParameterTip Styles.simpleMarkup doc paramName
               return! parameterTip}
    | _ -> None

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let private extractParamTipFromElement paramName element =
      match element with
      | FSharpToolTipElement.None -> None
      | FSharpToolTipElement.Group items -> items |> List.tryPick (fun data -> extractParamTipFromComment paramName data.XmlDoc)
      | FSharpToolTipElement.CompositionError _err -> None

  /// For elements with XML docs, the parameter descriptions are buried in the XML. Fetch it.
  let extractParamTip paramName (FSharpToolTipText elements) =
      List.tryPick (extractParamTipFromElement paramName) elements
