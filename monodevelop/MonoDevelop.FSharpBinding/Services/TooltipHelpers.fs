namespace MonoDevelop.FSharp

open System
open System.Text
open System.Collections.Generic
open System.Linq
open System.Xml
open System.Xml.Linq
open MonoDevelop.Core

type Style = 
    | Type of string 
    | Parameter of string 
    | Code of string
    | Exception of string

module Styles =
    let simpleMarkup style =
        match style with
        | Type name -> String.Format("<i>{0}</i> ", name)
        | Parameter name -> String.Format("<i>{0}</i> ", name)
        | Code name -> String.Format("<tt>{0}</tt> ", name)
        | Exception name -> String.Format("\n   <i>{0}</i>", name)
        
module Linq2Xml =
    let xn = XName.op_Implicit
    let xs ns local = XName.Get(local, ns)
    let firstOrDefault seq = Enumerable.FirstOrDefault(seq)
    let firstOrNone seq = 
        let iter = Enumerable.FirstOrDefault(seq)
        if iter <> null then Some(iter) else None

    let singleOrDefault seq = Enumerable.SingleOrDefault(seq)
    let where (pred: XElement -> bool) elements = Enumerable.Where(elements, pred)
    let attribute name (element:XElement) = element.Attribute <| xn name
    let attributeValue name element = (attribute name element).Value
    let descendants xs (element: XElement) = element.Descendants(xs)
    let previousNodeOrNone (element: XElement) =
        match element.PreviousNode with
        | null -> None 
        | node -> Some(node)
                       
module Tooltips = 
    open Linq2Xml    
    let strip start (str:string)= 
        if str.StartsWith start then str.Substring(start.Length)
        else str
    
    let trim (str:String) =
        str.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun s -> s.Trim() )
        |> String.concat(" ")
            
    let unqualifyName (txt:String) = txt.Substring(txt.LastIndexOf(".") + 1)  
            
    let elementValue (addStyle: Style -> string) (element:XElement) =
        let sb = StringBuilder()
        if element = null then sb else
        let rec processNodes (sb: StringBuilder) (nodes: IEnumerable<XNode>) =
            nodes.Aggregate(sb, fun acc node ->
                match node with
                | null -> acc
                | :? XElement as element ->
                       match element.Name.LocalName with
                       | "para" -> processNodes acc (element.Nodes())
                       
                       | "see" -> let attrib = element |> attribute "cref"
                                  if attrib = null then acc else
                                  let fragment = attrib.Value 
                                                 |> strip "T:"
                                                 |> unqualifyName
                                                 |> Type
                                                 |> addStyle
                                  acc.Append(fragment)
                                  
                       | "paramref" -> let attrib = element |> attribute "name"
                                       if attrib = null then acc else
                                       let fragment = attrib.Value
                                                      |> Parameter
                                                      |> addStyle
                                       acc.Append(fragment)
                                          
                       | "c" -> let fragment = element.Value 
                                               |> GLib.Markup.EscapeText
                                               |> Code
                                               |> addStyle
                                acc.Append(fragment)
                       | "attribution" -> acc //skip attribution elements
                       | unknown -> LoggingService.LogError("Error in Tooltip parsing, unknown element in summary: " + element.Name.LocalName)
                                    processNodes acc (element.Nodes())
                | :? XText as xt -> acc.AppendFormat("{0} ", xt.Value |> GLib.Markup.EscapeText |> trim)
                | _ -> acc )
        processNodes sb (element.Nodes())

    let getTooltip (addStyle: Style -> string) (str:string) = 
        try let xdoc =
            //XElement.Parse("<Root>" + str + "</Root>")
                XElement.Parse(str)
            //if no nodes were found then return the string verbatim
            let anyNodes = xdoc.Descendants() |> Enumerable.Any
            if not anyNodes then str else
            let summary = xdoc.Descendants(xn "summary") |> firstOrDefault |> elementValue addStyle
            
            xdoc.Elements(xn "exception")
            |> Seq.iteri (fun i element -> 
                if i = 0 then summary.Append("\n\nExceptions:") |> ignore
                match element |> attribute "cref" with 
                | null -> () 
                | cref -> let fragment = cref.Value 
                                         |> strip "T:"
                                         |> unqualifyName
                                         |> Exception
                                         |> addStyle
                          summary.Append(fragment) |> ignore)         
            if summary.Length > 0 then summary.ToString()
            //If theres nothing in the StringBuilder then there's either no summary or exception elements,
            //or something went wrong, simply return the str escaped rather than nothing.
            else GLib.Markup.EscapeText str
        //if the tooltip contains invalid xml return the str escaped
        with exn ->
            LoggingService.LogError("Error in Tooltip parsing:\n" + exn.ToString())
            GLib.Markup.EscapeText str
       
    let getParameterTip (addStyle: Style -> string) (str:String) (param:String) =
        let xdoc = XElement.Parse("<Root>" + str + "</Root>")
        let par = xdoc.Descendants(xn "param") 
                  |> where (fun element -> (element |> attribute "name").Value = param) 
                  |> singleOrDefault
        if par = null then None else Some((elementValue addStyle par).ToString())
        
        