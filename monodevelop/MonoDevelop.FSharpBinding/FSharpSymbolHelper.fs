module MonoDevelop.FSharp.FSharpSymbolHelper
open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Compiler.SourceCodeServices
open Mono.TextEditor
open MonoDevelop.Ide
open MonoDevelop.Components

[<AutoOpen>]
module FSharpTypeExt =
    let isOperatorOrActivePattern (name: string) =
            if name.StartsWith "( " && name.EndsWith " )" && name.Length > 4 then
                name.Substring (2, name.Length - 4) |> String.forall (fun c -> c <> ' ')
            else false

    let rec getAbbreviatedType (fsharpType: FSharpType) =
        if fsharpType.IsAbbreviation then
            let typ = fsharpType.AbbreviatedType
            if typ.HasTypeDefinition then getAbbreviatedType typ
            else fsharpType
        else fsharpType

    let isConstructor (func: FSharpMemberOrFunctionOrValue) =
        func.CompiledName = ".ctor"

    let isReferenceCell (fsharpType: FSharpType) = 
        let ty = getAbbreviatedType fsharpType
        ty.HasTypeDefinition && ty.TypeDefinition.IsFSharpRecord && ty.TypeDefinition.FullName = "Microsoft.FSharp.Core.FSharpRef`1"
    
    type FSharpType with
        member x.IsReferenceCell =
            isReferenceCell x
        
        member this.NonAbbreviatedTypeName = 
            if this.IsAbbreviation then this.AbbreviatedType.NonAbbreviatedTypeName
            else this.TypeDefinition.FullName

        member this.NonAbbreviatedType =
            Type.GetType(this.NonAbbreviatedTypeName)

    type Type with
        member this.PublicInstanceMembers =
            this.GetMembers(BindingFlags.Instance ||| BindingFlags.FlattenHierarchy ||| BindingFlags.Public)

    type MemberInfo with
        member this.DeclaringTypeValue =
            match this with
            | :? MethodInfo as mi -> mi.GetBaseDefinition().DeclaringType
            | _ -> this.DeclaringType

[<AutoOpen>]
module CorePatterns =
    let (|ActivePatternCase|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpActivePatternCase as ap-> ActivePatternCase(ap) |> Some
        | _ -> None

    let (|Entity|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpEntity as ent -> Entity(ent) |> Some
        | _ -> None

    let (|Field|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpField as field-> Field (field) |> Some
        |  _ -> None

    let (|GenericParameter|_|) (symbol: FSharpSymbol) = 
        match symbol with
        | :? FSharpGenericParameter as gp -> GenericParameter(gp) |> Some
        | _ -> None

    let (|MemberFunctionOrValue|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as func -> MemberFunctionOrValue(func) |> Some
        | _ -> None

    let (|Parameter|_|) (symbol : FSharpSymbol) = 
        match symbol with
        | :? FSharpParameter as param -> Parameter(param) |> Some
        | _ -> None

    let (|StaticParameter|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpStaticParameter as sp -> StaticParameter(sp) |> Some
        | _ -> None

    let (|UnionCase|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpUnionCase as uc-> UnionCase(uc) |> Some
        | _ -> None

[<AutoOpen>]
module ExtendedPatterns = 
    let (|TypeAbbreviation|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsFSharpAbbreviation -> Some TypeAbbreviation
        | _ -> None

    let (|Class|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsClass -> Some(Class)
        | CorePatterns.MemberFunctionOrValue symbol when
            symbol.IsImplicitConstructor || isConstructor symbol -> Some(Class)
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

    let (|Function|Operator|Pattern|ClosureOrNested|Val|Unknown|) (symbolUse:FSharpSymbolUse) =
        match symbolUse.Symbol with
        | CorePatterns.MemberFunctionOrValue symbol
            when not (isConstructor symbol) ->
                if symbol.FullType.IsFunctionType 
                    && not symbol.IsPropertyGetterMethod 
                    && not symbol.IsPropertySetterMethod then 
                    if FSharpTypeExt.isOperatorOrActivePattern symbol.DisplayName then
                        if symbolUse.IsFromPattern then Pattern
                        else Operator
                    else
                        if not symbol.IsModuleValueOrMember then ClosureOrNested
                        else Function                            
                else Val
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

    let getSummaryFromSymbol (symbol:FSharpSymbol) (backupSig: Lazy<Option<string * string>> option) =
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
                match backupSig with
                | Some backup ->
                     match backup.Force() with
                     | Some (key, file) ->Lookup (key, Some file)
                     | None -> XmlDoc.EmptyDoc
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

    let getTooltipFromSymbolUse (symbolUse:FSharpSymbolUse) (backUpSig: Lazy<_> option) =
        match symbolUse.Symbol with
        | Entity fse ->
            try
                let signature = getEntitySignature symbolUse.DisplayContext fse
                ToolTip(signature, getSummaryFromSymbol fse backUpSig)
            with exn -> ToolTips.EmptyTip

        | MemberFunctionOrValue func ->
            try
            if isConstructor func then 
                if func.EnclosingEntity.IsValueType || func.EnclosingEntity.IsEnum then
                    //ValueTypes
                    let signature = getFuncSignature symbolUse.DisplayContext func 3 false
                    ToolTip(signature, getSummaryFromSymbol func backUpSig)
                else
                    //ReferenceType constructor
                    let signature = getFuncSignature symbolUse.DisplayContext func 3 false
                    ToolTip(signature, getSummaryFromSymbol func backUpSig)

            elif func.FullType.IsFunctionType && not func.IsPropertyGetterMethod && not func.IsPropertySetterMethod && not symbolUse.IsFromComputationExpression then 
                if isOperatorOrActivePattern func.DisplayName then
                    //Active pattern or operator
                    let signature = getFuncSignature symbolUse.DisplayContext func 3 false
                    ToolTip(signature, getSummaryFromSymbol func backUpSig)
                else
                    //closure/nested functions
                    if not func.IsModuleValueOrMember then
                        //represents a closure or nested function, needs FCS support
                        let signature = func.FullType.Format symbolUse.DisplayContext
                        let summary = getSummaryFromSymbol func backUpSig
                        ToolTip(signature, summary)
                    else
                        let signature = getFuncSignature symbolUse.DisplayContext func 3 false
                        ToolTip(signature, getSummaryFromSymbol func backUpSig)                            

            else
                //val name : Type
                let signature = getValSignature symbolUse.DisplayContext func
                ToolTip(signature, getSummaryFromSymbol func backUpSig)
            with exn -> ToolTips.EmptyTip

        | Field fsf ->
            let signature = getFieldSignature symbolUse.DisplayContext fsf
            ToolTip(signature, getSummaryFromSymbol fsf backUpSig)

        | UnionCase uc ->
            let signature = getUnioncaseSignature symbolUse.DisplayContext uc
            ToolTip(signature, getSummaryFromSymbol uc backUpSig)

        | ActivePatternCase apc ->
            //Theres not enough information to build this?
            ToolTips.EmptyTip
           
        | _ -> ToolTips.EmptyTip

    let getTooltipFromSymbol (symbol:FSharpSymbol) displayContext (backUpSig: Lazy<_> option) =
        match symbol with
        | Entity fse ->
            try
                let signature = getEntitySignature displayContext fse
                ToolTip(signature, getSummaryFromSymbol fse backUpSig)
            with exn -> ToolTips.EmptyTip

        | MemberFunctionOrValue func ->
            try
            if isConstructor func then 
                if func.EnclosingEntity.IsValueType || func.EnclosingEntity.IsEnum then
                    //ValueTypes
                    let signature = getFuncSignature displayContext func 3 false
                    ToolTip(signature, getSummaryFromSymbol func backUpSig)
                else
                    //ReferenceType constructor
                    let signature = getFuncSignature displayContext func 3 false
                    ToolTip(signature, getSummaryFromSymbol func backUpSig)

            elif func.FullType.IsFunctionType && not func.IsPropertyGetterMethod && not func.IsPropertySetterMethod (*&& not symbolUse.IsFromComputationExpression*) then 
                if isOperatorOrActivePattern func.DisplayName then
                    //Active pattern or operator
                    let signature = getFuncSignature displayContext func 3 false
                    ToolTip(signature, getSummaryFromSymbol func backUpSig)
                else
                    //closure/nested functions
                    if not func.IsModuleValueOrMember then
                        //represents a closure or nested function, needs FCS support
                        let signature = func.FullType.Format displayContext
                        let summary = getSummaryFromSymbol func backUpSig
                        ToolTip(signature, summary)
                    else
                        let signature = getFuncSignature displayContext func 3 false
                        ToolTip(signature, getSummaryFromSymbol func backUpSig)                            

            else
                //val name : Type
                let signature = getValSignature displayContext func
                ToolTip(signature, getSummaryFromSymbol func backUpSig)
            with exn -> ToolTips.EmptyTip

        | Field fsf ->
            let signature = getFieldSignature displayContext fsf
            ToolTip(signature, getSummaryFromSymbol fsf backUpSig)

        | UnionCase uc ->
            let signature = getUnioncaseSignature displayContext uc
            ToolTip(signature, getSummaryFromSymbol uc backUpSig)

        | ActivePatternCase apc ->
            //Theres not enough information to build this?
            ToolTips.EmptyTip
           
        | _ -> ToolTips.EmptyTip
