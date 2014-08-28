// --------------------------------------------------------------------------------------
// Provides tool tips with F# hints for MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.IO
open System.Collections.Generic
open FSharp.CompilerBinding
open Mono.TextEditor
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.SourceEditor
open MonoDevelop.Ide.CodeCompletion
open Gdk
open MonoDevelop.Components
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AutoOpen>]
module FSharpTypeExt =
    let isOperator (name: string) =
            if name.StartsWith "( " && name.EndsWith " )" && name.Length > 4 then
                name.Substring (2, name.Length - 4) |> String.forall (fun c -> c <> ' ')
            else false

    let rec getAbbreviatedType (fsharpType: FSharpType) =
        if fsharpType.IsAbbreviation then
            let typ = fsharpType.AbbreviatedType
            if typ.HasTypeDefinition then getAbbreviatedType typ
            else fsharpType
        else fsharpType

    let isReferenceCell (fsharpType: FSharpType) = 
        let ty = getAbbreviatedType fsharpType
        ty.HasTypeDefinition && ty.TypeDefinition.IsFSharpRecord && ty.TypeDefinition.FullName = "Microsoft.FSharp.Core.FSharpRef`1"
    
    type FSharpType with
        member x.IsReferenceCell =
            isReferenceCell x

type XmlDoc =
  ///A full xmldoc tooltip
| Full of string
  ///A lookup of key, filename
| Lookup of string * string option
  ///No xmldoc
| EmptyDoc

type ToolTips =
  ///A ToolTip of signature, summary, TextSegment
| ToolTip of string * XmlDoc * TextSegment
  ///A empty tip
| EmptyTip

module Highlight =
    let getColourScheme () =
        Highlighting.SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme)

    let hl str (style: Highlighting.ChunkStyle) =
        let color = getColourScheme().GetForeground (style) |> GtkUtil.ToGdkColor
        let  colorString = HelperMethods.GetColorString (color)
        "<span foreground=\"" + colorString + "\">" + str + "</span>"

    let asSymbol s =
        let cs = getColourScheme ()
        hl s cs.KeywordOperators

    let asKeyword k =
        let cs = getColourScheme ()
        hl k cs.KeywordTypes
    
    let asUserType u =
        let cs = getColourScheme ()
        hl u cs.UserTypes

[<AutoOpen>]
module NewTooltips =

    let escapeText = GLib.Markup.EscapeText

    /// Add two strings with a space between
    let (++) a b = a + " " + b

    let getSegFromSymbolUse (editor:TextEditor) (symbolUse:FSharpSymbolUse)  =
        let startOffset = editor.Document.LocationToOffset(symbolUse.RangeAlternate.StartLine, symbolUse.RangeAlternate.StartColumn)
        let endOffset = editor.Document.LocationToOffset(symbolUse.RangeAlternate.EndLine, symbolUse.RangeAlternate.EndColumn)
        TextSegment.FromBounds(startOffset, endOffset)

    let getSummaryFromSymbol (symbolUse:FSharpSymbolUse) (backupSig: Lazy<Option<string * string>>) =
        let xmlDoc, xmlDocSig = 
            match symbolUse.Symbol with
            | :? FSharpMemberFunctionOrValue as func -> func.XmlDoc, func.XmlDocSig
            | :? FSharpEntity as fse -> fse.XmlDoc, fse.XmlDocSig
            | :? FSharpField as fsf -> fsf.XmlDoc, fsf.XmlDocSig
            | _ -> ResizeArray() :> IList<_>, ""

        if xmlDoc.Count > 0 then Full (String.Join( "\n", xmlDoc |> Seq.map escapeText))
        else
            if String.IsNullOrWhiteSpace xmlDocSig then
                let backup = backupSig.Force()
                match backup with
                | Some (key, file) ->Lookup (key, Some file)
                | None -> XmlDoc.EmptyDoc
            else Lookup(xmlDocSig, symbolUse.Symbol.Assembly.FileName)

    let getTooltipFromSymbol (symbolUse:FSharpSymbolUse option) editor (backUpSig: Lazy<_>) =
        match symbolUse with
        | Some symbolUse -> 
            match symbolUse.Symbol with
            | :? FSharpEntity as fse ->
                try
                    let displayName = fse.DisplayName

                    let modifier = match fse.Accessibility with
                                   | a when a.IsInternal -> Highlight.asKeyword "internal "
                                   | a when a.IsPrivate -> Highlight.asKeyword "private "
                                   | _ -> ""

                    let attributes =
                        // Maybe search for modifier attributes like abstract, sealed and append them above the type:
                        // [<Abstract>]
                        // type Test = ...
                        String.Join ("\n", fse.Attributes 
                                           |> Seq.map (fun a -> let name = a.AttributeType.DisplayName.Replace("Attribute", "")
                                                                let parameters = String.Join(", ",  a.ConstructorArguments |> Seq.filter (fun ca -> ca :? string ) |> Seq.cast<string>)
                                                                if String.IsNullOrWhiteSpace parameters then "[<" + name + ">]"
                                                                else "[<" + name + "( " + parameters + " )" + ">]" ) 
                                           |> Seq.toArray)

                    let signature =
                        let typeName =
                            match fse with
                            | _ when fse.IsFSharpModule -> "module"
                            | _ when fse.IsEnum         -> "enum"
                            | _ when fse.IsValueType    -> "struct"
                            | _                         -> "type"

                        let enumtip () =
                            Highlight.asSymbol " =" + "\n" + 
                            Highlight.asSymbol "|" ++
                            (fse.FSharpFields
                            |> Seq.filter (fun f -> not f.IsCompilerGenerated)
                            |> Seq.map (fun field -> field.Name) //TODO Fix FSC to expose enum filed literals: field.Name + (hl " = " cs.KeywordOperators) + hl field.LiteralValue cs.UserTypesValueTypes*)
                            |> String.concat ("\n" + Highlight.asSymbol "| " ) )
           

                        let uniontip () = 
                            Highlight.asSymbol " =" + "\n" + 
                            Highlight.asSymbol "|" ++
                            (fse.UnionCases 
                            |> Seq.map (fun unionCase -> 
                                            if unionCase.UnionCaseFields.Count > 0 then
                                               let typeList =
                                                  unionCase.UnionCaseFields
                                                  |> Seq.map (fun unionField -> unionField.Name ++ Highlight.asSymbol ":" ++ Highlight.asUserType (escapeText (unionField.FieldType.Format symbolUse.DisplayContext)))
                                                  |> String.concat (Highlight.asSymbol " * " )
                                               unionCase.Name ++ Highlight.asKeyword "of" ++ typeList
                                             else unionCase.Name)

                            |> String.concat ("\n" + Highlight.asSymbol "| " ) )
                                                 
                        let typeDisplay = modifier + Highlight.asKeyword typeName ++ Highlight.asUserType displayName
                        let fullName = "\n\nFull name: " + fse.FullName
                        match fse.IsFSharpUnion, fse.IsEnum with
                        | true, false -> typeDisplay + uniontip () + fullName
                        | false, true -> typeDisplay + enumtip () + fullName
                        | _ -> typeDisplay + fullName

                    ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                with exn -> ToolTips.EmptyTip

            | :? FSharpMemberFunctionOrValue as func ->
                try
                if func.CompiledName = ".ctor" then 
                    if func.EnclosingEntity.IsValueType || func.EnclosingEntity.IsEnum then
                        //TODO: Add ValueType
                        ToolTips.EmptyTip
                    else
                        //TODO: Add ReferenceType
                        ToolTips.EmptyTip

                elif func.FullType.IsFunctionType && not func.IsPropertyGetterMethod && not func.IsPropertySetterMethod && not symbolUse.IsFromComputationExpression then 
                    if isOperator func.DisplayName then
                        //TODO: Add operators, the text will look like:
                        // val ( symbol ) : x:string -> y:string -> string
                        // Full name: Name.Blah.( symbol )
                        // Note: (In the current compiler tooltips a closure defined symbol will be missing the named types and the full name)
                        ToolTips.EmptyTip
                    else
                        //TODO: Add closure/nested functions
                        if not func.IsModuleValueOrMember then
                            //represents a closure or nested function
                            ToolTips.EmptyTip
                        else
                            let signature =

                                let backupSignature = func.FullType.Format symbolUse.DisplayContext
                                let argInfos =
                                    func.CurriedParameterGroups 
                                    |> Seq.map Seq.toList 
                                    |> Seq.toList 

                                let retType = Highlight.asUserType (escapeText(func.ReturnParameter.Type.Format symbolUse.DisplayContext))

                                //example of building up the parameters using Display name and Type
                                let signature =
                                    let padLength = 
                                        let allLengths = argInfos |> List.concat |> List.map (fun p -> p.DisplayName.Length)
                                        match allLengths with
                                        | [] -> 0
                                        | l -> l |> List.max

                                    match argInfos with
                                    | [] -> retType //When does this occur, val type within  module?
                                    | [[]] -> retType //A ctor with () parameters seems to be a list with an empty list
                                    | [[single]] -> "   " + single.DisplayName + Highlight.asSymbol ":" ++ Highlight.asUserType (escapeText (single.Type.Format symbolUse.DisplayContext))
                                                    + "\n   " +  Highlight.asSymbol "->" ++ retType
                                    | many ->
                                        let allParams =
                                            many
                                            |> List.map(fun listOfParams ->

                                                            listOfParams
                                                            |> List.map(fun (p:FSharpParameter) ->
                                                                            "   " + p.DisplayName.PadRight (padLength) + Highlight.asSymbol ":" ++ Highlight.asUserType (escapeText (p.Type.Format symbolUse.DisplayContext)))
                                                                            |> String.concat (Highlight.asSymbol " *" ++ "\n"))
                                            |> String.concat (Highlight.asSymbol " ->" + "\n") 
                                        allParams +  "\n   " + (String.replicate (max (padLength-1) 0) " ") +  Highlight.asSymbol "->" ++ retType

                                let modifiers = 
                                    if func.IsMember then 
                                        if func.IsInstanceMember then
                                            if func.IsDispatchSlot then "abstract member"
                                            else "member"
                                        else "static member"
                                    else
                                        if func.InlineAnnotation = FSharpInlineAnnotation.AlwaysInline then "inline val"
                                        elif func.IsInstanceMember then "val"
                                        else "val" //does this need to be static prefixed?

                                Highlight.asKeyword modifiers ++ func.DisplayName ++ Highlight.asSymbol ":" + "\n" + signature

                            ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)                            

                else
                    //val name : Type
                    let signature =
                            let retType = Highlight.asUserType (escapeText(func.ReturnParameter.Type.Format symbolUse.DisplayContext))
                            let prefix = 
                                if func.IsMutable then Highlight.asKeyword "val" ++ Highlight.asKeyword "mutable"
                                else Highlight.asKeyword "val"
                            prefix ++ func.DisplayName ++ Highlight.asSymbol ":" ++ retType

                    ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                with exn -> ToolTips.EmptyTip

            | _ -> ToolTips.EmptyTip

        | None -> ToolTips.EmptyTip

type TooltipResults =
| ParseAndCheckNotFound
| NoToolTipText
| NoToolTipData
| Tooltip of TooltipItem

/// Resolves locations to tooltip items, and orchestrates their display.
/// We resolve language items to an NRefactory symbol.
type FSharpTooltipProvider() = 
    inherit Mono.TextEditor.TooltipProvider()

    // Keep the last result and tooltip window cached
    let mutable lastResult = None : TooltipItem option

    static let mutable lastWindow = None
   
    let killTooltipWindow() =
       match lastWindow with
       | Some(w:TooltipInformationWindow) -> w.Destroy()
       | None -> ()

    let isSupported fileName= 
        [|".fs";".fsi";".fsx";".fsscript"|] 
        |> Array.exists ((=) (Path.GetExtension fileName))

    override x.GetItem (editor, offset) =
      try
        let activeDoc = IdeApp.Workbench.ActiveDocument
        if activeDoc = null then null else

        let fileName = activeDoc.FileName.FullPath.ToString()
        let extEditor = editor :?> ExtensibleTextEditor
     
        if not (isSupported fileName) then null else
        let docText = editor.Text
        if docText = null || offset >= docText.Length || offset < 0 then null else

        let projFile, files, args, framework = MonoDevelop.getCheckerArgs(extEditor.Project, fileName)

        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, editor.Document)
        let result = async {
           let! parseAndCheckResults = MDLanguageService.Instance.GetTypedParseResultWithTimeout (projFile, fileName, docText, files, args, AllowStaleResults.MatchingSource, ServiceSettings.blockingTimeout, framework)
           LoggingService.LogInfo "TooltipProvider: Getting tool tip"
           match parseAndCheckResults with
           | None -> return ParseAndCheckNotFound
           | Some parseAndCheckResults ->
               let! symbol = parseAndCheckResults.GetSymbol(line, col, lineStr)
               //Hack: Because FCS does not always contain XmlDocSigs for tooltips we have to have to currently use the old tooltips
               // to extract the signature, this is only limited in that it deals with onlt a single tooltip in a group/list
               // This should be fine as there are issues with genewric tooltip xmldocs anyway
               // e.g. generics such as Dictionary<'a,'b>.Contains currently dont work.
               let! tip = parseAndCheckResults.GetToolTip(line, col, lineStr)
               //we create the backupSig as lazily as possible we could put the asyn call in here but I was worried about GC retension.
               let backupSig = 
                   lazy
                       match tip with
                       | Some (ToolTipText xs, (_,_)) when xs.Length > 0 ->
                           let first = xs.Head    
                           match first with
                           | ToolTipElement (name, xmlComment) ->
                                match xmlComment with
                                | XmlCommentSignature (key, file) -> Some (file, key)
                                | _ -> None
                           | ToolTipElementGroup tts when tts.Length > 0 ->
                               let name, xmlComment = tts.Head
                               match xmlComment with
                               | XmlCommentSignature (key, file) -> Some (file, key)
                               | _ -> None
                           | ToolTipElementCompositionError _ -> None
                           | _ -> None
                       | _ -> None

               let typeTip = getTooltipFromSymbol symbol extEditor backupSig
               // As the new tooltips are unfinished we match ToolTip here to use the new tooltips and anything else to run through the old tooltip system
               // In the section above we return EmptyTip for any tooltips symbols that have not yet ben finished
               match typeTip with
               | ToolTip(signature, summary, textSeg) ->
                    //check to see if the last result is the same tooltipitem, if so return the previous tooltipitem
                    match lastResult with
                    | Some(tooltipItem) when
                        tooltipItem.Item :? (string * XmlDoc) &&
                        tooltipItem.Item :?> (string * XmlDoc) = (signature, summary) &&
                        tooltipItem.ItemSegment = textSeg ->
                            return Tooltip tooltipItem
                    //If theres no match or previous cached result generate a new tooltipitem
                    | Some(_)
                    | None -> let tooltipItem = TooltipItem ((signature, summary), textSeg)
                              lastResult <- Some(tooltipItem)
                              return Tooltip tooltipItem
               | EmptyTip ->
                   // Get tool-tip from the language service
                   let! tip = parseAndCheckResults.GetToolTip(line, col, lineStr)
                   match tip with
                   | None -> return NoToolTipText
                   | Some (ToolTipText(elems),_) when elems |> List.forall (function ToolTipElementNone -> true | _ -> false) -> return NoToolTipData
                   | Some(tiptext,(col1,col2)) -> 
                       LoggingService.LogInfo "TooltipProvider: Got data"
                       //check to see if the last result is the same tooltipitem, if so return the previous tooltipitem
                       match lastResult with
                       | Some(tooltipItem) when
                           tooltipItem.Item :? ToolTipText && 
                           tooltipItem.Item :?> ToolTipText = tiptext && 
                           tooltipItem.ItemSegment = TextSegment(editor.LocationToOffset (line, col1 + 1), col2 - col1) ->
                               return Tooltip tooltipItem
                       //If theres no match or previous cached result generate a new tooltipitem
                       | Some(_)
                       | None -> let line = editor.Document.OffsetToLineNumber offset
                                 let segment = TextSegment(editor.LocationToOffset (line, col1 + 1), col2 - col1)
                                 let tooltipItem = TooltipItem (tiptext, segment)
                                 lastResult <- Some(tooltipItem)
                                 return Tooltip tooltipItem } |> Async.RunSynchronously
        match result with
        | ParseAndCheckNotFound -> LoggingService.LogWarning "TooltipProvider: ParseAndCheckResults not found"; null
        | NoToolTipText -> LoggingService.LogWarning "TooltipProvider: TootipText not returned"; null
        | NoToolTipData -> LoggingService.LogWarning "TooltipProvider: No data found"; null
        | Tooltip t -> t
       
      with exn -> LoggingService.LogError ("TooltipProvider: Error retrieving tooltip", exn); null

    override x.CreateTooltipWindow (editor, offset, modifierState, item) = 
        let doc = IdeApp.Workbench.ActiveDocument
        if (doc = null) then null else
        //At the moment as the new tooltips are unfinished we have two types here
        // ToolTipText for the old tooltips and (string * XmlDoc) for the new tooltips
        match item.Item with 
        | :? ToolTipText as titem ->
            let tooltip = TipFormatter.formatTip(titem)
            let (signature, comment) = 
                match tooltip with
                | [signature,comment] -> signature,comment
                //With multiple tips just take the head.  
                //This shouldnt happen anyway as we split them in the resolver provider
                | multiple -> multiple |> List.head
            //dont show a tooltip if there is no content
            if String.IsNullOrEmpty(signature) then null 
            else            
                let result = new TooltipInformationWindow(ShowArrow = true)
                let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
                if not (String.IsNullOrEmpty(comment)) then toolTipInfo.SummaryMarkup <- comment
                result.AddOverload(toolTipInfo)
                result.RepositionWindow ()                  
                result :> _

        | :? (string * XmlDoc) as tip -> 
            let signature, xmldoc = tip
            let result = new TooltipInformationWindow(ShowArrow = true)
            let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
            match xmldoc with
            | Full(summary) -> toolTipInfo.SummaryMarkup <- summary
            | Lookup(key, potentialFilename) ->
                let summary = 
                    maybe {let! filename = potentialFilename
                           let! markup = TipFormatter.findDocForEntity(filename, key)
                           let summary = Tooltips.getTooltip Styles.simpleMarkup markup
                           return summary}
                summary |> Option.iter (fun summary -> toolTipInfo.SummaryMarkup <- summary)
            | EmptyDoc -> ()
            result.AddOverload(toolTipInfo)
            result.RepositionWindow ()                  
            result :> _

        | _ -> LoggingService.LogError "TooltipProvider: Type mismatch, not a FSharpLocalResolveResult"
               null
    
    override x.ShowTooltipWindow (editor, offset, modifierState, mouseX, mouseY, item) =
        match (lastResult, lastWindow) with
        | Some(lastRes), Some(lastWin) when item.Item = lastRes.Item && lastWin.IsRealized ->
            lastWin :> _                   
        | _ -> killTooltipWindow()
               match x.CreateTooltipWindow (editor, offset, modifierState, item) with
               | :? TooltipInformationWindow as tipWindow ->
                   let positionWidget = editor.TextArea
                   let region = item.ItemSegment.GetRegion(editor.Document)
                   let p1, p2 = editor.LocationToPoint(region.Begin), editor.LocationToPoint(region.End)
                   let caret = Gdk.Rectangle (int p1.X - positionWidget.Allocation.X, 
                                              int p2.Y - positionWidget.Allocation.Y, 
                                              int (p2.X - p1.X), 
                                              int editor.LineHeight)
                   //For debug this is usful for visualising the tooltip location
                   // editor.SetSelection(item.ItemSegment.Offset, item.ItemSegment.EndOffset)
               
                   tipWindow.ShowPopup(positionWidget, caret, MonoDevelop.Components.PopupPosition.Top)
                   tipWindow.EnterNotifyEvent.Add(fun _ -> editor.HideTooltip (false))
                   //cache last window shown
                   lastWindow <- Some(tipWindow)
                   lastResult <- Some(item)
                   tipWindow :> _
               | _ -> LoggingService.LogError "TooltipProvider: Type mismatch, not a TooltipInformationWindow"
                      null
            
    interface IDisposable with
        member x.Dispose() = killTooltipWindow()
