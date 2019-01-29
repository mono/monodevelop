namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading
open MonoDevelop.FSharp.Shared
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Highlighting
open MonoDevelop.Components
open MonoDevelop.FSharp.Shared
open ExtCore.Control

module Symbols =
    let getLocationFromSymbolUse (s: FSharpSymbolUse) =
        [s.Symbol.DeclarationLocation; s.Symbol.SignatureLocation]
        |> List.choose id
        |> List.distinctBy (fun r -> r.FileName)

    let getLocationFromSymbol (s:FSharpSymbol) =
        [s.DeclarationLocation; s.SignatureLocation]
        |> List.choose id
        |> List.distinctBy (fun r -> r.FileName)

    ///Given a column and line string returns the identifier portion of the string
    let lastIdent column lineString =
        match Parsing.findIdents column lineString SymbolLookupKind.ByLongIdent with
        | Some (_col, identIsland) -> Seq.last identIsland
        | None -> ""

    ///Returns a TextSegment that is trimmed to only include the identifier
    let getTextSegment (doc:Editor.TextEditor) (symbolUse:FSharpSymbolUse) column line =
        let lastIdent = lastIdent column line
        let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent

        let startOffset = doc.LocationToOffset(start.Line, start.Column+1)
        let endOffset = doc.LocationToOffset(finish.Line, finish.Column+1)
        let startOffset =
            if startOffset = endOffset then 
                endOffset-symbolUse.Symbol.DisplayName.Length
            else
                startOffset
        MonoDevelop.Core.Text.TextSegment.FromBounds(startOffset, endOffset)

    let getEditorDataForFileName (fileName:string) =
        match IdeApp.Workbench.GetDocument (fileName) with
        | null ->
            let doc = Editor.TextEditorFactory.LoadDocument (fileName)
            let editor = new TextEditorData()
            editor.Text <- doc.Text
            editor
        | doc -> doc.Editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()

    let getOffsets (range:Range.range) (editor:Editor.IReadonlyTextDocument) =
        let startOffset = editor.LocationToOffset (range.StartLine, range.StartColumn+1)
        let endOffset = editor.LocationToOffset (range.EndLine, range.EndColumn+1)
        startOffset, endOffset

    let getTextSpan (range:Range.range) (editor:Editor.IReadonlyTextDocument) =
        let startOffset, endOffset = getOffsets range editor
        Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (startOffset, endOffset)

    let getTrimmedRangesForDeclarations lastIdent (symbolUse:FSharpSymbolUse) =
        symbolUse
        |> getLocationFromSymbolUse
        |> Seq.map (fun range ->
            let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent
            range.FileName, start, finish)

    let getTrimmedOffsetsForDeclarations lastIdent (symbolUse:FSharpSymbolUse) =
        let trimmedSymbols = getTrimmedRangesForDeclarations lastIdent symbolUse
        trimmedSymbols
        |> Seq.map (fun (fileName, start, finish) ->
            let editor = getEditorDataForFileName fileName
            let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
            let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
            //if startOffset < 0 then argOutOfRange "startOffset" "broken"
            //if endOffset < 0  then argOutOfRange "endOffset" "broken"
            fileName, startOffset, endOffset)

    let getTrimmedTextSpanForDeclarations lastIdent (symbolUse:FSharpSymbolUse) =
        let trimmedSymbols = getTrimmedRangesForDeclarations lastIdent symbolUse
        trimmedSymbols
        |> Seq.map (fun (fileName, start, finish) ->
            let editor = getEditorDataForFileName fileName
            let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
            let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
            let ts = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (startOffset, endOffset)
            let ls = Microsoft.CodeAnalysis.Text.LinePositionSpan(Microsoft.CodeAnalysis.Text.LinePosition(start.Line, start.Column),
                                                                  Microsoft.CodeAnalysis.Text.LinePosition(finish.Line, finish.Column))
            fileName, ts, ls)

    let getOffsetsTrimmed lastIdent (symbolUse:FSharpSymbolUse) =
        let filename = symbolUse.RangeAlternate.FileName
        let editor = getEditorDataForFileName filename
        let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent
        let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
        let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
        filename, startOffset, endOffset

    let getOffsetAndLength lastIdent (symbolUse:FSharpSymbolUse) =
        let editor = getEditorDataForFileName symbolUse.RangeAlternate.FileName
        let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent
        let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
        let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
        startOffset, endOffset - startOffset

    let getTextSpanTrimmed lastIdent (symbolUse:FSharpSymbolUse) =
        let filename, start, finish = getOffsetsTrimmed lastIdent symbolUse
        filename, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (start, finish)

[<AutoOpen>]
module SymbolUse =
    let (|ActivePatternCase|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpActivePatternCase as ap-> ActivePatternCase(ap) |> Some
        | _ -> None

    let (|Entity|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpEntity as ent -> Some ent
        | _ -> None

    let (|Field|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpField as field-> Some field
        |  _ -> None

    let (|GenericParameter|_|) (symbol: FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpGenericParameter as gp -> Some gp
        | _ -> None

    let (|MemberFunctionOrValue|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpMemberOrFunctionOrValue as func -> Some func
        | _ -> None

    let (|ActivePattern|_|) = function
        | MemberFunctionOrValue m when m.IsActivePattern -> Some m | _ -> None

    let (|Parameter|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpParameter as param -> Some param
        | _ -> None

    let (|StaticParameter|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpStaticParameter as sp -> Some sp
        | _ -> None

    let (|UnionCase|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpUnionCase as uc-> Some uc
        | _ -> None

    let (|Constructor|_|) = function
        | MemberFunctionOrValue func when func.IsConstructor || func.IsImplicitConstructor -> Some func
        | _ -> None

    let (|TypeAbbreviation|_|) = function
        | Entity symbol when symbol.IsFSharpAbbreviation -> Some symbol
        | _ -> None

    let (|Class|_|) = function
        | Entity symbol when symbol.IsClass -> Some symbol
        | Entity s when s.IsFSharp &&
                        s.IsOpaque &&
                        not s.IsFSharpModule &&
                        not s.IsNamespace &&
                        not s.IsDelegate &&
                        not s.IsFSharpUnion &&
                        not s.IsFSharpRecord &&
                        not s.IsInterface &&
                        not s.IsValueType -> Some s
        | _ -> None

    let (|Delegate|_|) = function
        | Entity symbol when symbol.IsDelegate -> Some symbol
        | _ -> None

    let (|Event|_|) = function
        | MemberFunctionOrValue symbol when symbol.IsEvent -> Some symbol
        | _ -> None

    let (|Property|_|) = function
        | MemberFunctionOrValue symbol when symbol.IsProperty || symbol.IsPropertyGetterMethod || symbol.IsPropertySetterMethod -> Some symbol
        | _ -> None

    let inline private notCtorOrProp (symbol:FSharpMemberOrFunctionOrValue) =
        not symbol.IsConstructor && 
        not symbol.IsPropertyGetterMethod && 
        not symbol.IsPropertySetterMethod &&
        not symbol.IsProperty &&
        not (symbol.LogicalName = ".ctor")

    let (|Method|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when symbol.IsModuleValueOrMember  &&
                                            not symbolUse.IsFromPattern &&
                                            not symbol.IsOperatorOrActivePattern &&
                                            not symbol.IsPropertyGetterMethod &&
                                            not symbol.IsPropertySetterMethod -> Some symbol
        | _ -> None

    let (|Function|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when notCtorOrProp symbol  &&
                                            symbol.IsModuleValueOrMember &&
                                            not symbol.IsOperatorOrActivePattern &&
                                            not symbolUse.IsFromPattern ->
            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Operator|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                            not symbolUse.IsFromPattern &&
                                            not symbol.IsActivePattern &&
                                            symbol.IsOperatorOrActivePattern ->
            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Pattern|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                            not symbol.IsOperatorOrActivePattern &&
                                            symbolUse.IsFromPattern ->
            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType ->Some symbol
            | _ -> None
        | _ -> None


    let (|ClosureOrNestedFunction|_|) = function
        | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                            not symbol.IsOperatorOrActivePattern &&
                                            not symbol.IsModuleValueOrMember ->
            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    
    let (|Val|_|) = function
        | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                            not symbol.IsOperatorOrActivePattern ->
            match symbol.FullTypeSafe with
            | Some _fullType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Enum|_|) = function
        | Entity symbol when symbol.IsEnum -> Some symbol
        | _ -> None

    let (|Interface|_|) = function
        | Entity symbol when symbol.IsInterface -> Some symbol
        | _ -> None

    let (|Module|_|) = function
        | Entity symbol when symbol.IsFSharpModule -> Some symbol
        | _ -> None

    let (|Namespace|_|) = function
        | Entity symbol when symbol.IsNamespace -> Some symbol
        | _ -> None

    let (|Record|_|) = function
        | Entity symbol when symbol.IsFSharpRecord -> Some symbol
        | _ -> None

    let (|Union|_|) = function
        | Entity symbol when symbol.IsFSharpUnion -> Some symbol
        | _ -> None

    let (|ValueType|_|) = function
        | Entity symbol when symbol.IsValueType && not symbol.IsEnum -> Some symbol
        | _ -> None

    let (|ComputationExpression|_|) (symbol:FSharpSymbolUse) =
        if symbol.IsFromComputationExpression then Some symbol
        else None
        
    let (|Attribute|_|) = function
        | Entity ent ->
            if ent.AllBaseTypes
               |> Seq.exists (fun t ->
                                  if t.HasTypeDefinition then
                                      t.TypeDefinition.TryFullName
                                      |> Option.exists ((=) "System.Attribute" )
                                  else false)
            then Some ent
            else None
        | _ -> None

    let symbolToIcon (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | ActivePatternCase _ -> "ActivePatternCase"
        | Field _ -> "Field"
        | UnionCase _ -> "UnionCase"
        | Class _ -> "Class"
        | Delegate _ -> "Delegate"
        | Constructor _  -> "Constructor"
        | Event _ -> "Event"
        | Property _ -> "Property"
        | Function f ->
            if f.IsExtensionMember then "ExtensionMethod"
            elif f.IsMember then "Method"
            else "Field"
        | Operator _ -> "Operator"
        | ClosureOrNestedFunction _ -> "ClosureOrNestedFunction"
        | Val _ -> "Val"
        | Enum _ -> "Enum"
        | Interface _ -> "Interface"
        | Module _ -> "Module"
        | Namespace _ -> "Namespace"
        | Record _ -> "Record"
        | Union _ -> "Union"
        | ValueType _ -> "ValueType"
        | Entity _ -> "Entity"
        | _ -> "Event"

//type XmlDoc =
//  ///A full xmldoc tooltip
//| Full of string
//  ///A lookup of key, filename
//| Lookup of string * string option
//  ///No xmldoc
//| EmptyDoc

//type ToolTips =
//  ///A ToolTip of signature, summary
//  | ToolTip of signature:string * doc:XmlDoc * footer:string
//    ///A empty tip
//  | EmptyTip
[<AutoOpen>]
module PrintParameter =
    let print sb = Printf.bprintf sb "%s"

[<AutoOpen>]
module Highlight =
    type HighlightType =
    | Symbol | Brackets | Keyword | UserType | Number

    let getColourPart x = round(x * 255.0) |> int

    let argbToHex (c : Cairo.Color) =
        sprintf "#%02X%02X%02X" (getColourPart c.R) (getColourPart c.G) (getColourPart c.B)

    let getEditor() =
        let editor = TextEditorFactory.CreateNewEditor()
        editor.MimeType <- "text/x-fsharp"
        let assembly = typeof<SyntaxHighlighting>.Assembly
        use stream = assembly.GetManifestResourceStream("F#.sublime-syntax")
        use reader = new StreamReader(stream)
        let highlighting = Sublime3Format.ReadHighlighting(reader)
        highlighting.PrepareMatches()
        editor.SyntaxHighlighting <- new SyntaxHighlighting(highlighting, editor)
        editor

    let private editor =
        Runtime.RunInMainThread getEditor
        |> Async.AwaitTask 
        |> Async.RunSynchronously

    let getHighlightedMarkup s =
        editor.Text <- s
        let data = editor.GetContent<ITextEditorDataProvider>().GetTextEditorData()
        data.GetMarkup(0, s.Length, false, true, false, true)

    let syntaxHighlight s =
        Runtime.RunInMainThread (fun () -> getHighlightedMarkup s)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let asUnderline = sprintf "_STARTUNDERLINE_%s_ENDUNDERLINE_" // we replace with real markup after highlighting

module SymbolTooltips =
    /// Concat two strings with a space between if both a and b are not IsNullOrWhiteSpace
    let internal (++) (a:string) (b:string) =
        match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> a + " " + b

    let formatSummary (summary:XmlDoc) =
        match summary with
        | Full(summary) ->
            TooltipsXml.getTooltipSummary Styles.simpleMarkup summary

        | Lookup(key, potentialFilename) ->
            maybe { let! filename = potentialFilename
                    let! markup = TooltipXmlDoc.findDocForEntity(filename, key)
                    let summary = TooltipsXml.getTooltipSummary Styles.simpleMarkup markup
                    return summary } |> Option.fill ""
        | EmptyDoc -> ""

    let getTooltipInformationFromTip tip =
        async {
            try
                let signature, xmldoc, footer = tip
                let signature = syntaxHighlight signature
                let toolTipInfo = new TooltipInformation(SignatureMarkup = signature, FooterMarkup=footer)
                let result =
                  match xmldoc with
                  | Full(summary) -> toolTipInfo.SummaryMarkup <- summary
                                     toolTipInfo
                  | Lookup(key, potentialFilename) ->
                      let summary =
                        maybe { let! filename = potentialFilename
                                let! markup = TooltipXmlDoc.findDocForEntity(filename, key)
                                let summary = TooltipsXml.getTooltipSummary Styles.simpleMarkup markup
                                return summary }
                      summary |> Option.iter (fun summary -> toolTipInfo.SummaryMarkup <- summary)
                      toolTipInfo
                  | EmptyDoc -> toolTipInfo
                return result
            with ex ->
                LoggingService.LogError ("F# Tooltip error", ex)
                return TooltipInformation() }

    let getTooltipInformation symbol =
        async {
            let tip = MonoDevelop.FSharp.Shared.SymbolTooltips.getTooltipFromSymbolUse symbol
            match tip with
            | Some tip' -> return! getTooltipInformationFromTip tip'
            | _ -> return TooltipInformation()
        }

    let getTooltipInformationFromSignature summary signature parameterName =
        let summary, parameterInfo =
            match summary with
            | Full(summary) ->
              let parameterMarkup =
                match TooltipsXml.getParameterTip Styles.simpleMarkup summary parameterName with
                | Some p -> parameterName ++ ":" ++ p
                | None -> ""
              summary, parameterMarkup
            | Lookup(key, filename) ->
                let summaryAndparameterInfo =
                  maybe { let! filename = filename
                          let! markup = TooltipXmlDoc.findDocForEntity(filename, key)
                          let parameterMarkup =
                              match TooltipsXml.getParameterTip Styles.simpleMarkup markup parameterName with
                              | Some p -> parameterName ++ ":" ++ p
                              | None -> ""
                          let summary = TooltipsXml.getTooltipSummary Styles.simpleMarkup markup
                          return (summary, parameterMarkup) }

                summaryAndparameterInfo |> Option.getOrElse (fun () -> "", "")
            | EmptyDoc -> "", ""
        let toolTipInfo = TooltipInformation(SignatureMarkup = signature, SummaryMarkup=summary)
        if not (String.isNullOrEmpty parameterInfo) then
            toolTipInfo.AddCategory("Parameter", parameterInfo)
        toolTipInfo

    let getParameterTooltipInformation symbol parameterIndex =
        match symbol with
        | MemberFunctionOrValue m ->
          let parameterName =
            match m.CurriedParameterGroups |> Seq.toList with
            | [single] when parameterIndex < single.Count ->
                let param = single.[parameterIndex]
                match param.Name with
                | Some n -> n
                | _ -> param.DisplayName
            | _ -> ""
          let signature = syntaxHighlight (MonoDevelop.FSharp.Shared.SymbolTooltips.getFuncSignatureWithFormat symbol.DisplayContext m {Indent=3;Highlight=Some(parameterName)})
          let signature = signature.Replace("_STARTUNDERLINE_", "<u>").Replace("_ENDUNDERLINE_", "</u>")
          let summary = MonoDevelop.FSharp.Shared.SymbolTooltips.getSummaryFromSymbol m

          getTooltipInformationFromSignature summary signature parameterName
        | _ -> TooltipInformation()
