namespace MonoDevelop.FSharp

open System
open System.Text
open System.Collections.Generic
open System.Linq
open System.Xml
open System.Xml.Linq
open System.Diagnostics

type Style = 
| Type of string 
| Parameter of string 
| Code of string
| Exception of string

module Styles =
    let simpleMarkup style=
        match style with
        | Type name -> String.Format("<i>{0}</i> ", name)
        | Parameter name -> String.Format("<i>{0}</i> ", name)
        | Code name -> String.Format("<i>{0}</i> ", name)
        | Exception name -> String.Format("\n   <i>{0}</i>", name)
        
    let none style=
        match style with
        | Type name -> name
        | Parameter name -> name
        | Code name -> name
        | Exception name -> name
            
module Tooltips = 
        
    let strip start (str:string)= 
        if str.StartsWith start then str.Substring(start.Length)
        else str
    
    let trim (str:String) =
        str.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun s -> s.Trim() )
        |> String.concat(" ")
            
    let unqualifyName (txt:String) = txt.Substring(txt.LastIndexOf(".") + 1)  

    let xn = XName.op_Implicit
    let firstOrDefault seq = Enumerable.FirstOrDefault(seq)
    let singleOrDefault seq = Enumerable.SingleOrDefault(seq)
    let where (pred: XElement -> bool) elements = Enumerable.Where(elements, pred)
    let attribute name (element:XElement) = element.Attribute <| xn name
            
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
                                  acc.Append(addStyle <| Type fragment)
                                  
                       | "paramref" -> let attrib = element |> attribute "name"
                                       if attrib = null then acc else
                                       let fragment = attrib.Value
                                       acc.Append(addStyle <| Parameter fragment)
                                          
                       | "c" -> acc.Append(addStyle <| Code element.Value)

                       | _ -> processNodes acc (element.Nodes())
                | :? XText as xt -> let fragment = xt.Value |> trim
                                    acc.AppendFormat("{0} ", fragment)
                | _ -> acc )
                
        processNodes sb <| element.Nodes()
        
    let getTooltip (addStyle: Style -> string) (str:string) = 
        let xdoc = XElement.Parse("<Root>" + str + "</Root>")
        let summary = xdoc.Descendants(xn "summary") |> firstOrDefault |> elementValue addStyle
     
        xdoc.Elements(xn "exception")
        |> Seq.iteri (fun i element -> 
            if i = 0 then summary.Append("\n\nExceptions:") |> ignore
            match element |> attribute "cref" with 
            | null -> () 
            | cref -> let fragment = cref.Value 
                                     |> strip "T:"
                                     |> unqualifyName
                      summary.Append(addStyle <| Exception fragment) |> ignore)         
        summary.ToString()
       
    let getParameterTip (addStyle: Style -> string) (str:String) (param:String) =
        let xdoc = XElement.Parse("<Root>" + str + "</Root>")
        let par = xdoc.Descendants(xn "param") 
                  |> where (fun element -> (element |> attribute "name").Value = param) 
                  |> singleOrDefault
        if par = null then str else (elementValue addStyle par).ToString()
        
        