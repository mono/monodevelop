namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Compiler.SourceCodeServices
open Mono.TextEditor
open MonoDevelop.Ide
open MonoDevelop.Components

module Symbols =
    ///Given a column and line string returns the identifier portion of the string
    let lastIdent column lineString =
        match FSharp.CompilerBinding.Parsing.findLongIdents(column, lineString) with
        | Some (_, identIsland) -> Seq.last identIsland
        | None -> ""

    ///Returns a TextSegment that is trimmed to only include the identifier
    let getTextSegment (doc:TextDocument) (symbolUse:FSharpSymbolUse) column line =
        let lastIdent = lastIdent  column line
        let (startLine, startColumn), (endLine, endColumn) = FSharp.CompilerBinding.Symbols.trimSymbolRegion symbolUse lastIdent

        let startOffset = doc.LocationToOffset(startLine, startColumn+1)
        let endOffset = doc.LocationToOffset(endLine, endColumn+1)
        TextSegment.FromBounds(startOffset, endOffset)

[<AutoOpen>]
module FSharpTypeExt =
    let isOperatorOrActivePattern (name: string) =
            if name.StartsWith "( " && name.EndsWith " )" && name.Length > 4 then
                name.Substring (2, name.Length - 4) |> String.forall (fun c -> c <> ' ')
            else false

    let isConstructor (func: FSharpMemberOrFunctionOrValue) =
        func.CompiledName = ".ctor"

[<AutoOpen>]
module CorePatterns =
    let (|ActivePatternCase|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpActivePatternCase as ap-> ActivePatternCase(ap) |> Some
        | _ -> None

    let (|Entity|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpEntity as ent -> Entity(ent) |> Some
        | _ -> None

    let (|Field|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpField as field-> Field (field) |> Some
        |  _ -> None

    let (|GenericParameter|_|) (symbol: FSharpSymbolUse) = 
        match symbol.Symbol with
        | :? FSharpGenericParameter as gp -> GenericParameter(gp) |> Some
        | _ -> None

    let (|MemberFunctionOrValue|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpMemberOrFunctionOrValue as func -> MemberFunctionOrValue(func) |> Some
        | _ -> None

    let (|Parameter|_|) (symbol : FSharpSymbolUse) = 
        match symbol.Symbol with
        | :? FSharpParameter as param -> Parameter(param) |> Some
        | _ -> None

    let (|StaticParameter|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpStaticParameter as sp -> StaticParameter(sp) |> Some
        | _ -> None

    let (|UnionCase|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpUnionCase as uc-> UnionCase(uc) |> Some
        | _ -> None

[<AutoOpen>]
module ExtendedPatterns = 
    let (|Constructor|_|) symbol =
        match symbol with
        | CorePatterns.MemberFunctionOrValue func -> 
            if func.CompiledName = ".ctor" || func.IsImplicitConstructor then Some func
            else None
        | _ -> None

    let (|TypeAbbreviation|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpAbbreviation -> Some TypeAbbreviation
        | _ -> None

    let (|Class|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsClass -> Some(Class)
        | _ -> None

    let (|Delegate|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsDelegate -> Some(Delegate)
        | _ -> None

    let (|Event|_|) symbol =
        match symbol with
        | CorePatterns.MemberFunctionOrValue symbol when symbol.IsEvent -> Some(Event)
        | _ -> None

    let (|Property|_|) symbol =
        match symbol with
        | CorePatterns.MemberFunctionOrValue symbol when
            symbol.IsProperty || symbol.IsPropertyGetterMethod || symbol.IsPropertySetterMethod -> Some(Property)
        | _ -> None

    let (|Function|Operator|Pattern|ClosureOrNestedFunction|Val|Unknown|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | CorePatterns.MemberFunctionOrValue symbol
            when not (isConstructor symbol) ->
                match symbol.FullTypeSafe with
                | Some fullType when fullType.IsFunctionType
                                    && not symbol.IsPropertyGetterMethod
                                    && not symbol.IsPropertySetterMethod ->
                    if FSharpTypeExt.isOperatorOrActivePattern symbol.DisplayName then
                        if symbolUse.IsFromPattern then Pattern symbol
                        else Operator symbol
                    else
                        if not symbol.IsModuleValueOrMember then ClosureOrNestedFunction symbol
                        else Function symbol                       
                | Some _fullType -> Val symbol
                | None -> Unknown
        | _ -> Unknown

    let (|Enum|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsEnum -> Some(Enum)
        | _ -> None

    let (|Interface|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsInterface -> Some(Interface)
        | _ -> None

    let (|Module|_|) symbol = 
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpModule -> Some(Module)
        | _ -> None

    let (|Namespace|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsNamespace -> Some (Namespace)
        | _ -> None

    let (|Record|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpRecord -> Some(Record)
        | _ -> None

    let (|Union|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpUnion -> Some(Union)
        | _ -> None

    let (|ValueType|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsValueType -> Some(ValueType)
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

type TooltipResults =
| ParseAndCheckNotFound
| NoToolTipText
| NoToolTipData
| Tooltip of TooltipItem

module SymbolTooltips =

    let internal escapeText = GLib.Markup.EscapeText

    /// Concat two strings with a space between if both a and b are not IsNullOrWhiteSpace
    let internal (++) (a:string) (b:string) =
        match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> a + " " + b

    let getSummaryFromSymbol (symbol:FSharpSymbol) (backupSig: Lazy<Option<string * string>>) =
        let xmlDoc, xmlDocSig = 
            match symbol with
            | :? FSharpMemberOrFunctionOrValue as func -> func.XmlDoc, func.XmlDocSig
            | :? FSharpEntity as fse -> fse.XmlDoc, fse.XmlDocSig
            | :? FSharpField as fsf -> fsf.XmlDoc, fsf.XmlDocSig
            | :? FSharpUnionCase as fsu -> fsu.XmlDoc, fsu.XmlDocSig
            | :? FSharpActivePatternCase as apc -> apc.XmlDoc, apc.XmlDocSig
            | :? FSharpGenericParameter as gp -> gp.XmlDoc, ""
            | _ -> ResizeArray() :> IList<_>, ""

        if xmlDoc.Count > 0 then Full (String.Join( "\n", xmlDoc |> Seq.map escapeText))
        else
            if String.IsNullOrWhiteSpace xmlDocSig then
                match backupSig.Force() with
                | Some (key, file) -> Lookup (key, Some file)
                | None -> XmlDoc.EmptyDoc
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
            if signatureOnly then retType
            else asType Keyword modifiers ++ functionName ++ asType Symbol ":" ++ retType
                   
        | [[]] ->
            //A ctor with () parameters seems to be a list with an empty list
            if signatureOnly then retType
            else asType Keyword modifiers ++ functionName ++ asType Symbol "() :" ++ retType 
        | many ->
                let allParams =
                    many
                    |> List.map(fun listOfParams ->
                                    listOfParams
                                    |> List.map(fun p -> indent + p.DisplayName.PadRight (padLength) + asType Symbol ":" ++ asType UserType (escapeText (p.Type.Format displayContext)))
                                    |> String.concat (asType Symbol " *" ++ "\n"))
                    |> String.concat (asType Symbol " ->" + "\n") 
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
            asType Symbol "|" ++
            (fse.UnionCases 
            |> Seq.map (getUnioncaseSignature displayContext)
            |> String.concat ("\n" + asType Symbol "| " ) )

        let delegateTip () =
            let invoker =
                fse.MembersFunctionsAndValues |> Seq.find (fun f -> f.DisplayName = "Invoke")
            let invokerSig = getFuncSignature FSharpDisplayContext.Empty invoker 6 true
            asType Symbol " =" + "\n" +
            "   " + asType Keyword "delegate" + " of\n" + invokerSig
                                 
        let typeDisplay = modifier + asType Keyword typeName ++ asType UserType fse.DisplayName
        let fullName = "\n\nFull name: " + fse.FullName
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

    let getTooltipFromSymbolUse (symbol:FSharpSymbolUse) (backUpSig: Lazy<_>) =
        match symbol with
        | Entity fse ->
            try
                let signature = getEntitySignature symbol.DisplayContext fse
                ToolTip(signature, getSummaryFromSymbol fse backUpSig)
            with exn -> ToolTips.EmptyTip

        | Constructor func ->
            if func.EnclosingEntity.IsValueType || func.EnclosingEntity.IsEnum then
                //ValueTypes
                let signature = getFuncSignature symbol.DisplayContext func 3 false
                ToolTip(signature, getSummaryFromSymbol func backUpSig)
            else
                //ReferenceType constructor
                let signature = getFuncSignature symbol.DisplayContext func 3 false
                ToolTip(signature, getSummaryFromSymbol func backUpSig)

        | Operator func ->
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            ToolTip(signature, getSummaryFromSymbol func backUpSig)

        | Pattern func ->
            //Active pattern or operator
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            ToolTip(signature, getSummaryFromSymbol func backUpSig)

        | ClosureOrNestedFunction func ->
            //represents a closure or nested function, needs FCS support
            let signature = escapeText <|func.FullType.Format symbol.DisplayContext
            let summary = getSummaryFromSymbol func backUpSig
            ToolTip(signature, summary)

        | Function func ->
            let signature = getFuncSignature symbol.DisplayContext func 3 false
            ToolTip(signature, getSummaryFromSymbol func backUpSig) 

        | Val func ->
            //val name : Type
            let signature = getValSignature symbol.DisplayContext func
            ToolTip(signature, getSummaryFromSymbol func backUpSig)

        | Field fsf ->
            let signature = getFieldSignature symbol.DisplayContext fsf
            ToolTip(signature, getSummaryFromSymbol fsf backUpSig)

        | UnionCase uc ->
            let signature = getUnioncaseSignature symbol.DisplayContext uc
            ToolTip(signature, getSummaryFromSymbol uc backUpSig)

        | ActivePatternCase _apc ->
            //Theres not enough information to build this?
            ToolTips.EmptyTip
           
        | _ ->
            ToolTips.EmptyTip
