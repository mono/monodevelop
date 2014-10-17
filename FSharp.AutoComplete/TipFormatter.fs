// --------------------------------------------------------------------------------------
// (c) Tomas Petricek, http://tomasp.net/blog
// --------------------------------------------------------------------------------------
module internal FSharp.InteractiveAutocomplete.TipFormatter

open System.Text
open Microsoft.FSharp.Compiler.SourceCodeServices

// --------------------------------------------------------------------------------------
// Formatting of tool-tip information displayed in F# IntelliSense
// --------------------------------------------------------------------------------------

let private buildFormatComment cmt (sb:StringBuilder) =
  match cmt with
  | FSharpXmlDoc.Text s -> sb.AppendLine(s)
  // For 'XmlCommentSignature' we could get documentation from 'xml'
  // files, but I'm not sure whether these are available on Mono
  | _ -> sb

// If 'isSingle' is true (meaning that this is the only tip displayed)
// then we add first line "Multiple overloads" because MD prints first
// int in bold (so that no overload is highlighted)
let private buildFormatElement isSingle el (sb:StringBuilder) =
  match el with
  | FSharpToolTipElement.None -> sb
  | FSharpToolTipElement.Single(it, comment) ->
      sb.AppendLine(it) |> buildFormatComment comment
  | FSharpToolTipElement.Group(items) ->
      let items, msg =
        if items.Length > 10 then
          (items |> Seq.take 10 |> List.ofSeq),
            sprintf "   (+%d other overloads)</i>" (items.Length - 10)
        else items, null
      if (isSingle && items.Length > 1) then
        sb.AppendLine("Multiple overloads") |> ignore
      for (it, comment) in items do
        sb.AppendLine(it) |> buildFormatComment comment |> ignore
      if msg <> null then sb.AppendFormat(msg) else sb
  | FSharpToolTipElement.CompositionError(err) ->
      sb.Append("Composition error: " + err)

let private buildFormatTip tip (sb:StringBuilder) =
  match tip with
  | FSharpToolTipText([single]) -> sb |> buildFormatElement true single
  | FSharpToolTipText(its) ->
      sb.AppendLine("Multiple items") |> ignore
      its |> Seq.mapi (fun i it -> i = 0, it) |> Seq.fold (fun sb (first, item) ->
        if not first then sb.AppendLine("\n--------------------\n") |> ignore
        sb |> buildFormatElement false item) sb

/// Format tool-tip that we get from the language service as string
let formatTip tip =
  (buildFormatTip tip (new StringBuilder())).ToString().Trim('\n', '\r').Replace("\r","")
