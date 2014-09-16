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
open MonoDevelop.FSharp.FSharpSymbolHelper

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

[<AutoOpen>]
module Highlight =
    type HighlightType =
    | Symbol | Keyword | UserType | Number

    let getColourScheme () =
        Highlighting.SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme)

    let hl str (style: Highlighting.ChunkStyle) =
        let color = getColourScheme().GetForeground (style) |> GtkUtil.ToGdkColor
        let colorString = HelperMethods.GetColorString (color)
        sprintf """<span foreground="%s">%s</span>""" colorString str

    let asType t s =
        let cs = getColourScheme ()
        match t with
        | Symbol -> hl s cs.KeywordOperators
        | Keyword -> hl s cs.KeywordTypes
        | UserType -> hl s cs.UserTypes
        | Number -> hl s cs.Number

[<AutoOpen>]
module NewTooltips =

    let escapeText = GLib.Markup.EscapeText

    /// Concat two strings with a space between if both a and b are not IsNullOrWhiteSpace
    let (++) (a:string) (b:string) =
        match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> a + " " + b

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
            | :? FSharpUnionCase as fsu -> fsu.XmlDoc, fsu.XmlDocSig
            | :? FSharpGenericParameter as gp -> gp.XmlDoc, ""
            | _ -> ResizeArray() :> IList<_>, ""

        if xmlDoc.Count > 0 then Full (String.Join( "\n", xmlDoc |> Seq.map escapeText))
        else
            if String.IsNullOrWhiteSpace xmlDocSig then
                let backup = backupSig.Force()
                match backup with
                | Some (key, file) ->Lookup (key, Some file)
                | None -> XmlDoc.EmptyDoc
            else Lookup(xmlDocSig, symbolUse.Symbol.Assembly.FileName)

    let getUnioncaseSignature displayContext (unionCase:FSharpUnionCase) =
        if unionCase.UnionCaseFields.Count > 0 then
           let typeList =
              unionCase.UnionCaseFields
              |> Seq.map (fun unionField -> unionField.Name ++ asType Symbol ":" ++ asType UserType (escapeText (unionField.FieldType.Format displayContext)))
              |> String.concat (asType Symbol " * " )
           unionCase.Name ++ asType Keyword "of" ++ typeList
         else unionCase.Name

    let getEntitySignature displayContext (fse: FSharpEntity) =
        let modifier =
            match fse.Accessibility with
            | a when a.IsInternal -> asType Keyword "internal "
            | a when a.IsPrivate -> asType Keyword "private "
            | _ -> ""

        let typeName =
            match fse with
            | _ when fse.IsFSharpModule -> "module"
            | _ when fse.IsEnum         -> "enum"
            | _ when fse.IsValueType    -> "struct"
            | _                         -> "type"

        let enumtip () =
            asType Symbol " =" + "\n" + 
            asType Symbol "|" ++
            (fse.FSharpFields
            |> Seq.filter (fun f -> not f.IsCompilerGenerated)
            |> Seq.map (fun field -> match field.LiteralValue with
                                     | Some lv -> field.Name + asType Symbol " = " + asType Number (string lv)
                                     | None -> field.Name )
            |> String.concat ("\n" + asType Symbol "| " ) )


        let uniontip () = 
            asType Symbol " =" + "\n" + 
            asType Symbol "|" ++
            (fse.UnionCases 
            |> Seq.map (getUnioncaseSignature displayContext)
            |> String.concat ("\n" + asType Symbol "| " ) )
                                 
        let typeDisplay = modifier + asType Keyword typeName ++ asType UserType fse.DisplayName
        let fullName = "\n\nFull name: " + fse.FullName
        match fse.IsFSharpUnion, fse.IsEnum with
        | true, false -> typeDisplay + uniontip () + fullName
        | false, true -> typeDisplay + enumtip () + fullName
        | _ -> typeDisplay + fullName

    let getFuncSignature displayContext (func: FSharpMemberFunctionOrValue) =
        let functionName =
            if isConstructor func then func.EnclosingEntity.DisplayName
            else func.DisplayName

        let modifiers =
            let accessibility =
                match func.Accessibility with
                | a when a.IsInternal -> asType Keyword "internal"
                | a when a.IsPrivate -> asType Keyword "private"
                | _ -> ""

            let modifier =
                //F# types are prefixed with new, should non F# types be too for consistancy?
                if isConstructor func then
                    if func.EnclosingEntity.IsFSharp then "new" ++ accessibility
                    else accessibility
                elif func.IsMember then 
                    if func.IsInstanceMember then
                        if func.IsDispatchSlot then "abstract member" ++ accessibility
                        else "member" ++ accessibility
                    else "static member" ++ accessibility
                else
                    if func.InlineAnnotation = FSharpInlineAnnotation.AlwaysInline then "val" ++ accessibility ++ "inline"
                    elif func.IsInstanceMember then "val" ++ accessibility
                    else "val" ++ accessibility //does this need to be static prefixed?
            modifier

        let argInfos =
            func.CurriedParameterGroups 
            |> Seq.map Seq.toList 
            |> Seq.toList 

        let retType = asType UserType (escapeText(func.ReturnParameter.Type.Format displayContext))

        let padLength = 
            let allLengths = argInfos |> List.concat |> List.map (fun p -> p.DisplayName.Length)
            match allLengths with
            | [] -> 0
            | l -> l |> List.max

        match argInfos with
        | [] ->
            //When does this occur, val type within  module?
            asType Keyword modifiers ++ functionName ++ asType Symbol ":" ++ retType
                   
        | [[]] ->
            //A ctor with () parameters seems to be a list with an empty list
            asType Keyword modifiers ++ functionName ++ asType Symbol "() :" ++ retType 
        | many ->
                let allParams =
                    many
                    |> List.map(fun listOfParams ->
                                    listOfParams
                                    |> List.map(fun p -> "   " + p.DisplayName.PadRight (padLength) + asType Symbol ":" ++ asType UserType (escapeText (p.Type.Format displayContext)))
                                    |> String.concat (asType Symbol " *" ++ "\n"))
                    |> String.concat (asType Symbol " ->" + "\n") 
                let typeArguments =
                    allParams +  "\n   " + (String.replicate (max (padLength-1) 0) " ") +  asType Symbol "->" ++ retType
                asType Keyword modifiers ++ functionName ++ asType Symbol ":" + "\n" + typeArguments

    let getValSignature displayContext (v:FSharpMemberFunctionOrValue) =
        let retType = asType UserType (escapeText(v.ReturnParameter.Type.Format displayContext))
        let prefix = 
            if v.IsMutable then asType Keyword "val" ++ asType Keyword "mutable"
            else asType Keyword "val"
        prefix ++ v.DisplayName ++ asType Symbol ":" ++ retType

    let getFieldSignature displayContext (field: FSharpField) =
        let retType = asType UserType (escapeText(field.FieldType.Format displayContext))
        match field.LiteralValue with
        | Some lv -> field.DisplayName ++ asType Symbol ":" ++ retType ++ asType Symbol "=" ++ asType Number (string lv)
        | None ->
            let prefix = 
                if field.IsMutable then asType Keyword "val" ++ asType Keyword "mutable"
                else asType Keyword "val"
            prefix ++ field.DisplayName ++ asType Symbol ":" ++ retType

    let getTooltipFromSymbol (symbolUse:FSharpSymbolUse option) editor (backUpSig: Lazy<_>) =
        match symbolUse with
        | Some symbolUse -> 
            match symbolUse.Symbol with
            | :? FSharpEntity as fse ->
                try
                    let signature = getEntitySignature symbolUse.DisplayContext fse
                    ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                with exn -> ToolTips.EmptyTip

            | :? FSharpMemberFunctionOrValue as func ->
                try
                if func.CompiledName = ".ctor" then 
                    if func.EnclosingEntity.IsValueType || func.EnclosingEntity.IsEnum then
                        //ValueTypes
                        let signature = getFuncSignature symbolUse.DisplayContext func
                        ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                        //ToolTips.EmptyTip
                    else
                        //ReferenceType constructor
                        let signature = getFuncSignature symbolUse.DisplayContext func
                        ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                        //ToolTips.EmptyTip

                elif func.FullType.IsFunctionType && not func.IsPropertyGetterMethod && not func.IsPropertySetterMethod && not symbolUse.IsFromComputationExpression then 
                    if isOperatorOrActivePattern func.DisplayName then
                        //Active pattern or operator
                        let signature = getFuncSignature symbolUse.DisplayContext func
                        ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                    else
                        //TODO: Add closure/nested functions
                        if not func.IsModuleValueOrMember then
                            //represents a closure or nested function, needs FCS support
                            ToolTips.EmptyTip
                        else
                            let signature = getFuncSignature symbolUse.DisplayContext func
                            ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)                            

                else
                    //val name : Type
                    let signature = getValSignature symbolUse.DisplayContext func
                    ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)
                with exn -> ToolTips.EmptyTip

            | :? FSharpField as fsf ->
                let signature = getFieldSignature symbolUse.DisplayContext fsf
                ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)

            | :? FSharpUnionCase as uc ->
                let signature = getUnioncaseSignature symbolUse.DisplayContext uc
                ToolTip(signature, getSummaryFromSymbol symbolUse backUpSig, getSegFromSymbolUse editor symbolUse)

            | :? FSharpActivePatternCase as apc ->
                //Theres not enough information to build this
                ToolTips.EmptyTip
               
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
    static let mutable lastWindow = None : TooltipInformationWindow option

    //keep the last enterNotofy handler so we can remove the handler as a new TipWindow is created
    let mutable enterNotify = None : IDisposable option

    let killTooltipWindow() =
       lastWindow |> Option.iter (fun w -> w.Destroy())
       enterNotify |> Option.iter (fun en -> en.Dispose ())


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
                    | None -> let tooltipItem = TooltipItem((signature, summary), textSeg)
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
                   enterNotify <- Some (tipWindow.EnterNotifyEvent.Subscribe(fun _ -> editor.HideTooltip (false)))
                   //cache last window shown
                   lastWindow <- Some(tipWindow)
                   lastResult <- Some(item)
                   tipWindow :> _
               | _ -> LoggingService.LogError "TooltipProvider: Type mismatch, not a TooltipInformationWindow"
                      null
            
    interface IDisposable with
        member x.Dispose() = killTooltipWindow()
