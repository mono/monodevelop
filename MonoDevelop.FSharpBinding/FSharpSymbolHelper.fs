namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Compiler.SourceCodeServices
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Ide
open MonoDevelop.Components

module Symbols =
    let getLocationFromSymbolUse (s: FSharpSymbolUse) =
        [s.Symbol.DeclarationLocation; s.Symbol.SignatureLocation]
        |> List.choose id
        |> Seq.distinctBy (fun r -> r.FileName)
        |> Seq.toList
        
    let getLocationFromSymbol (s:FSharpSymbol) =
        [s.DeclarationLocation; s.SignatureLocation]
        |> List.choose id
        |> Seq.distinctBy (fun r -> r.FileName)
        |> Seq.toList 
    
        
    ///Given a column and line string returns the identifier portion of the string
    let lastIdent column lineString =
        match Parsing.findLongIdents(column, lineString) with
        | Some (_col, identIsland) -> Seq.last identIsland
        | None -> ""

    ///Returns a TextSegment that is trimmed to only include the identifier
    let getTextSegment (doc:Editor.TextEditor) (symbolUse:FSharpSymbolUse) column line =
        let lastIdent = lastIdent column line
        let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent

        let startOffset = doc.LocationToOffset(start.Line, start.Column+1)
        let endOffset = doc.LocationToOffset(finish.Line, finish.Column+1)
        MonoDevelop.Core.Text.TextSegment.FromBounds(startOffset, endOffset)

    let getEditorForFileName (fileName:string) =
        match IdeApp.Workbench.GetDocument (fileName) with
        | null ->           
            let doc = MonoDevelop.Ide.Editor.TextEditorFactory.LoadDocument (fileName)
            MonoDevelop.Ide.Editor.TextEditorFactory.CreateNewEditor (doc)
        | doc -> doc.Editor

    let getOffsets (range:Microsoft.FSharp.Compiler.Range.range) (editor:Editor.IReadonlyTextDocument) =
        let startOffset = editor.LocationToOffset (range.StartLine, range.StartColumn+1)
        let endOffset = editor.LocationToOffset (range.EndLine, range.EndColumn+1)
        startOffset, endOffset

    let getTextSpan (range:Microsoft.FSharp.Compiler.Range.range) (editor:Editor.IReadonlyTextDocument) =
        let startOffset, endOffset = getOffsets range editor
        Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (startOffset, endOffset)

    let getTrimmedRangesForDeclarations lastIdent (symbolUse:FSharpSymbolUse) = 
        symbolUse
        |> getLocationFromSymbolUse
        |> List.map (fun range -> 
            let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent
            range.FileName, start, finish)

    let getTrimmedOffsetsForDeclarations lastIdent (symbolUse:FSharpSymbolUse) = 
        let trimmedSymbols = getTrimmedRangesForDeclarations lastIdent symbolUse 
        trimmedSymbols
        |> List.map (fun (fileName, start, finish) ->
            let editor = getEditorForFileName fileName
            let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
            let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
            //if startOffset < 0 then argOutOfRange "startOffset" "broken"
            //if endOffset < 0  then argOutOfRange "endOffset" "broken"
            fileName, startOffset, endOffset)

    let getTrimmedTextSpanForDeclarations lastIdent (symbolUse:FSharpSymbolUse) =
        let trimmedSymbols = getTrimmedRangesForDeclarations lastIdent symbolUse
        trimmedSymbols
        |> List.map (fun (fileName, start, finish) ->
            let editor = getEditorForFileName fileName
            let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
            let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
            let ts = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (startOffset, endOffset)
            let ls = Microsoft.CodeAnalysis.Text.LinePositionSpan(Microsoft.CodeAnalysis.Text.LinePosition(start.Line, start.Column),
                                                                  Microsoft.CodeAnalysis.Text.LinePosition(finish.Line, finish.Column))
            fileName, ts, ls)

    let getOffsetsTrimmed lastIdent (symbolUse:FSharpSymbolUse) =
        let filename = symbolUse.RangeAlternate.FileName
        let editor = getEditorForFileName filename
        let start, finish = Symbol.trimSymbolRegion symbolUse lastIdent
        let startOffset = editor.LocationToOffset (start.Line, start.Column+1)
        let endOffset = editor.LocationToOffset (finish.Line, finish.Column+1)
        filename, startOffset, endOffset

    let getTextSpanTrimmed lastIdent (symbolUse:FSharpSymbolUse) =
        let filename, start, finish = getOffsetsTrimmed lastIdent symbolUse
        filename, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (start, finish)

[<AutoOpen>]
module CorePatterns =
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

[<AutoOpen>]
module ExtendedPatterns = 
    let (|Constructor|_|) symbol =
        match symbol with
        | CorePatterns.MemberFunctionOrValue func -> 
            if func.IsConstructor || func.IsImplicitConstructor then Some func
            else None
        | _ -> None

    let (|TypeAbbreviation|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpAbbreviation -> Some symbol
        | _ -> None

    let (|Class|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsClass -> Some symbol
        | CorePatterns.Entity s when s.IsFSharp &&
                                     s.IsOpaque &&
                                     not s.IsFSharpModule &&
                                     not s.IsNamespace &&
                                     not s.IsDelegate &&
                                     not s.IsFSharpUnion &&
                                     not s.IsFSharpRecord &&
                                     not s.IsInterface &&
                                     not s.IsValueType -> Some s
        | _ -> None

    let (|Delegate|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsDelegate -> Some symbol
        | _ -> None

    let (|Event|_|) symbol =
        match symbol with
        | CorePatterns.MemberFunctionOrValue symbol when symbol.IsEvent -> Some symbol
        | _ -> None

    let (|Property|_|) symbol =
        match symbol with
        | CorePatterns.MemberFunctionOrValue symbol when
            symbol.IsProperty || symbol.IsPropertyGetterMethod || symbol.IsPropertySetterMethod -> Some symbol
        | _ -> None

    let (|Function|Operator|Pattern|ClosureOrNestedFunction|Val|Unknown|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | CorePatterns.MemberFunctionOrValue symbol
            when not (symbol.IsConstructor) ->
                match symbol.FullTypeSafe with
                | Some fullType when fullType.IsFunctionType
                                    && not symbol.IsPropertyGetterMethod
                                    && not symbol.IsPropertySetterMethod ->
                    if symbol.IsOperatorOrActivePattern then
                        if symbolUse.IsFromPattern then Pattern symbol
                        else Operator symbol
                    else
                        if not symbol.IsModuleValueOrMember then ClosureOrNestedFunction symbol
                        else Function symbol                       
                | Some _fullType -> 
                  if symbol.IsOperatorOrActivePattern then
                    if symbolUse.IsFromPattern then Pattern symbol else Operator symbol
                  else Val symbol 
                | None -> Unknown
        | _ -> Unknown

    let (|Enum|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsEnum -> Some symbol
        | _ -> None

    let (|Interface|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsInterface -> Some symbol
        | _ -> None

    let (|Module|_|) symbol = 
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpModule -> Some symbol
        | _ -> None

    let (|Namespace|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsNamespace -> Some symbol
        | _ -> None

    let (|Record|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpRecord -> Some symbol
        | _ -> None

    let (|Union|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpUnion -> Some symbol
        | _ -> None

    let (|ValueType|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsValueType -> Some symbol
        | _ -> None

type XmlDoc =
  ///A full xmldoc tooltip
| Full of string
  ///A lookup of key, filename
| Lookup of string * string option
  ///No xmldoc
| EmptyDoc

type ToolTips =
  ///A ToolTip of signature, summary
| ToolTip of string * XmlDoc
  ///A empty tip
| EmptyTip

[<AutoOpen>]
module internal Highlight =
    type HighlightType =
    | Symbol | Brackets | Keyword | UserType | Number

    let getColourScheme () =
        Highlighting.SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme.Value)

    let hl str (style: Highlighting.ChunkStyle) =
        let color = getColourScheme().GetForeground (style) |> GtkUtil.ToGdkColor
        let colorString = HelperMethods.GetColorString (color)
        String.Format ("""<span foreground="{0}">{1}</span>""", colorString, str)

    let asType t s =
        let cs = getColourScheme ()
        match t with
        | Symbol -> hl s cs.KeywordOperators
        | Brackets -> hl s cs.PunctuationForBrackets
        | Keyword -> hl s cs.KeywordTypes
        | UserType -> hl s cs.UserTypes
        | Number -> hl s cs.Number

type TooltipResults =
| ParseAndCheckNotFound
| NoToolTipText
| NoToolTipData
| Tooltip of MonoDevelop.Ide.Editor.TooltipItem

module SymbolTooltips =

    type NestedFunctionParams =
    | GenericParam of FSharpGenericParameter
    | TupleParam of IList<FSharpType>
    | NamedType of FSharpType

    let internal escapeText = GLib.Markup.EscapeText

    /// Concat two strings with a space between if both a and b are not IsNullOrWhiteSpace
    let internal (++) (a:string) (b:string) =
        match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> a + " " + b

    let getKeywordTooltip (keyword:string) =
      let signatureline = asType Keyword keyword ++ "(keyword)"
      let summary =
        match KeywordList.keywordDescriptions.TryGetValue keyword with
        | true, description -> Full description
        | false, _ -> EmptyDoc
      ToolTip(signatureline, summary)

    let getSummaryFromSymbol (symbol:FSharpSymbol) =
        let xmlDoc, xmlDocSig = 
            match symbol with
            | :? FSharpMemberOrFunctionOrValue as func -> func.XmlDoc, func.XmlDocSig
            | :? FSharpEntity as fse -> fse.XmlDoc, fse.XmlDocSig
            | :? FSharpField as fsf -> fsf.XmlDoc, fsf.XmlDocSig
            | :? FSharpUnionCase as fsu -> fsu.XmlDoc, fsu.XmlDocSig
            | :? FSharpActivePatternCase as apc -> apc.XmlDoc, apc.XmlDocSig
            | :? FSharpGenericParameter as gp -> gp.XmlDoc, ""
            | _ -> ResizeArray() :> IList<_>, ""

        if xmlDoc.Count > 0
        then Full (String.Join( "\n", xmlDoc |> Seq.map escapeText))
        else Lookup(xmlDocSig, symbol.Assembly.FileName)

    let getUnioncaseSignature displayContext (unionCase:FSharpUnionCase) =
        if unionCase.UnionCaseFields.Count > 0 then
           let typeList =
              unionCase.UnionCaseFields
              |> Seq.map (fun unionField -> unionField.Name ++ asType Symbol ":" ++ asType UserType (escapeText (unionField.FieldType.Format displayContext)))
              |> String.concat (asType Symbol " * " )
           unionCase.Name ++ asType Keyword "of" ++ typeList
         else unionCase.Name

    let getFuncSignature displayContext (func: FSharpMemberOrFunctionOrValue) indent signatureOnly =
        let indent = String.replicate indent " "
        let functionName =
            let name = 
                if func.IsConstructor then func.EnclosingEntity.DisplayName
                else func.DisplayName
            escapeText name

        let modifiers =
            let accessibility =
                match func.Accessibility with
                | a when a.IsInternal -> asType Keyword "internal"
                | a when a.IsPrivate -> asType Keyword "private"
                | _ -> ""

            let modifier =
                //F# types are prefixed with new, should non F# types be too for consistancy?
                if func.IsConstructor then
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
        
        let retType =
            //This try block will be removed when FCS updates
            try 
                asType UserType (escapeText(func.ReturnParameter.Type.Format displayContext))
            with _ex ->
                try
                    if func.FullType.GenericArguments.Count > 0 then
                        let lastArg = func.FullType.GenericArguments |> Seq.last
                        asType UserType (escapeText(lastArg.Format displayContext))
                    else "Unknown"
                with _ -> "Unknown"

        let padLength =
            let allLengths = argInfos |> List.concat |> List.map (fun p -> match p.Name with None -> 0 | Some name -> name.Length)
            match allLengths with
            | [] -> 0
            | l -> l |> List.max

        let formatName indent padding (parameter:FSharpParameter) =
          match parameter.Name with
          | None -> indent
          | Some name -> indent + name.PadRight (padding) + asType Symbol ":" 

        match argInfos with
        | [] ->
            //When does this occur, val type within  module?
            if signatureOnly then retType
            else asType Keyword modifiers ++ functionName ++ asType Symbol ":" ++ retType
                   
        | [[]] ->
            //A ctor with () parameters seems to be a list with an empty list
            if signatureOnly then retType
            else asType Keyword modifiers ++ functionName ++ asType Symbol "() :" ++ retType 
        | many ->
            let allParamsLengths = 
                many 
                |> List.map (fun listOfParams ->
                                 (listOfParams, listOfParams    
                                                |> List.map (fun p -> (p.Type.Format displayContext).Length)
                                                |> List.sum))
            let maxLength =
                match allParamsLengths with
                | [] -> 0
                | l -> l |> List.map(fun p -> snd p + 1) |> List.max
  
            let parameterTypeWithPadding (p: FSharpParameter) length =
                (escapeText (p.Type.Format displayContext) + (String.replicate (maxLength - length) " "))

            let allParams =
                allParamsLengths
                |> List.map(fun p -> let (paramTypes, length) = p 
                                     paramTypes
                                     |> List.map(fun p -> formatName indent padLength p ++ asType UserType (parameterTypeWithPadding p length))
                                     |> String.concat (asType Symbol " *" ++ "\n"))
                |> String.concat (asType Symbol "->" + "\n")
            
            let typeArguments =
                allParams +  "\n" + indent + (String.replicate (max (padLength-1) 0) " ") +  asType Symbol "->" ++ retType

            if signatureOnly then typeArguments
            else asType Keyword modifiers ++ functionName ++ asType Symbol ":" + "\n" + typeArguments

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
            | _ when fse.IsNamespace    -> "namespace"
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
            asType Symbol "|" ++ (fse.UnionCases 
                                  |> Seq.map (getUnioncaseSignature displayContext)
                                  |> String.concat ("\n" + asType Symbol "| " ) )

        let delegateTip () =
            let invoker =
                fse.MembersFunctionsAndValues |> Seq.find (fun f -> f.DisplayName = "Invoke")
            let invokerSig = getFuncSignature FSharpDisplayContext.Empty invoker 6 true
            asType Symbol " =" + "\n" +
            "   " + asType Keyword "delegate" + " of\n" + invokerSig
                                 
        let typeDisplay =
            let name =
              if fse.GenericParameters.Count > 0 then
                let p = fse.GenericParameters |> Seq.map (fun gp -> gp.DisplayName) |> String.concat ","
                asType UserType fse.DisplayName + asType Brackets (escapeText "<") + asType UserType p + asType Brackets (escapeText ">")
              else asType UserType fse.DisplayName

            let basicName = modifier + asType Keyword typeName ++ name
            //TODO: add generic constraint display
            if fse.IsFSharpAbbreviation then
              basicName ++ asType Brackets "=" ++ asType Keyword (fse.AbbreviatedType.Format displayContext)
            else
              basicName 

        let fullName =
            match fse.TryGetFullNameWithUnderScoreTypes() with
            | Some fullname -> "\n\n<small>Full name: " + escapeText fullname + "</small>"
            | None -> "\n\n<small>Full name: " + fse.QualifiedName + "</small>"

        match fse.IsFSharpUnion, fse.IsEnum, fse.IsDelegate with
        | true, false, false -> typeDisplay + uniontip () + fullName
        | false, true, false -> typeDisplay + enumtip () + fullName
        | false, false, true -> typeDisplay + delegateTip () + fullName
        | _ -> typeDisplay + fullName

    let getValSignature displayContext (v:FSharpMemberOrFunctionOrValue) =
        let retType = asType UserType (escapeText(v.FullType.Format(displayContext)))
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

    let getAPCaseSignature displayContext (apc:FSharpActivePatternCase) =
      let findVal =
        apc.Group.EnclosingEntity.Value.MembersFunctionsAndValues
        |> Seq.tryFind (fun thing -> thing.DisplayName.Contains apc.DisplayName)
        |> Option.map (fun v -> getFuncSignature displayContext v 3 false)

      match findVal with
      | Some v -> v
      | None -> apc.Group.OverallType.Format displayContext


    let getTooltipFromSymbolUse (symbol:FSharpSymbolUse) =
        match symbol with
        | Entity fse ->
            try
                let signature = getEntitySignature symbol.DisplayContext fse
                ToolTip(signature, getSummaryFromSymbol fse)
            with exn ->
                ToolTips.EmptyTip

        | Constructor func ->
            if func.EnclosingEntity.IsValueType || func.EnclosingEntity.IsEnum then
                //ValueTypes
                let signature = getFuncSignature symbol.DisplayContext func 3 false
                ToolTip(signature, getSummaryFromSymbol func)
            else
                //ReferenceType constructor
                let signature = getFuncSignature symbol.DisplayContext func 3 false
                ToolTip(signature, getSummaryFromSymbol func)

        | Operator func ->
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            ToolTip(signature, getSummaryFromSymbol func)

        | Pattern func ->
            //Active pattern or operator
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            ToolTip(signature, getSummaryFromSymbol func)

        | ClosureOrNestedFunction func ->
            //represents a closure or nested function
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            let summary = getSummaryFromSymbol func
            ToolTip(signature, summary)
             
        | Function func ->
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            ToolTip(signature, getSummaryFromSymbol func) 

        | Val func ->
            //val name : Type
            let signature = getValSignature symbol.DisplayContext func
            ToolTip(signature, getSummaryFromSymbol func)

        | Field fsf ->
            let signature = getFieldSignature symbol.DisplayContext fsf
            ToolTip(signature, getSummaryFromSymbol fsf)

        | UnionCase uc ->
            let signature = getUnioncaseSignature symbol.DisplayContext uc
            ToolTip(signature, getSummaryFromSymbol uc)

        | ActivePatternCase apc ->
            let signature = getAPCaseSignature symbol.DisplayContext apc
            ToolTip(signature, getSummaryFromSymbol apc)
         
        | _ ->
            ToolTips.EmptyTip
