namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open System.Reflection
open System.Text
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.PrettyNaming
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Components
open ExtCore.Control

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

    let getOffsetAndLength lastIdent (symbolUse:FSharpSymbolUse) =
        let editor = getEditorForFileName symbolUse.RangeAlternate.FileName
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
      not symbol.IsConstructor && not symbol.IsPropertyGetterMethod && not symbol.IsPropertySetterMethod

    let (|Method|_|) (symbolUse:FSharpSymbolUse) =
      match symbolUse with
      | MemberFunctionOrValue symbol when symbol.IsModuleValueOrMember  &&
                                          not symbolUse.IsFromPattern &&
                                          not symbol.IsOperatorOrActivePattern &&
                                          not symbol.IsPropertyGetterMethod && 
                                          not symbol.IsPropertySetterMethod -> Some symbol
      | _ -> None

    let (|Function|_|) = function
      | MemberFunctionOrValue symbol when notCtorOrProp symbol  &&
                                          symbol.IsModuleValueOrMember &&
                                          not symbol.IsOperatorOrActivePattern ->
          match symbol.FullTypeSafe with
          | Some fullType when fullType.IsFunctionType -> Some symbol                       
          | _ -> None
      | _ -> None

    let (|Operator|_|) (symbolUse:FSharpSymbolUse) =
      match symbolUse with
      | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                          not symbolUse.IsFromPattern &&
                                          symbol.IsOperatorOrActivePattern ->
         match symbol.FullTypeSafe with
          | Some fullType when fullType.IsFunctionType -> Some symbol                       
          | _ -> None
      | _ -> None

    let (|Pattern|_|) (symbolUse:FSharpSymbolUse) =
      match symbolUse with
      | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                          symbol.IsOperatorOrActivePattern &&
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
    
    let asSymbol = asType Symbol
    let asKeyword = asType Keyword
    let asBrackets = asType Brackets
    let asUserType = asType UserType


type TooltipResults =
| ParseAndCheckNotFound
| NoToolTipText
| NoToolTipData
| Tooltip of MonoDevelop.Ide.Editor.TooltipItem

[<AutoOpen>]
module PrintParameter =
    let print sb = Printf.bprintf sb "%s"

    let asGenericParamName (param: FSharpGenericParameter) =
        asSymbol (if param.IsSolveAtCompileTime then "^" else "'") + param.Name

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
      let signatureline = asKeyword keyword ++ "(keyword)"
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
              |> Seq.map (fun unionField -> unionField.Name ++ asSymbol ":" ++ asUserType (escapeText (unionField.FieldType.Format displayContext)))
              |> String.concat (asSymbol " * " )
           unionCase.Name ++ asKeyword "of" ++ typeList
         else unionCase.Name

    let formatGenericParameter displayContext (param:FSharpGenericParameter) =
        let chopStringTo (s:string) (c:char) =
            // chopStringTo "abcdef" 'c' --> "def"
            if s.IndexOf c <> -1 then
                let i =  s.IndexOf c + 1
                s.Substring(i, s.Length - i)
            else
                s
    
        let tryChopPropertyName (s: string) =
            // member names start with get_ or set_ when the member is a property
            let s = 
                if s.StartsWith("get_", System.StringComparison.Ordinal) || 
                   s.StartsWith("set_", System.StringComparison.Ordinal) 
                then s 
                else chopStringTo s '.'
        
            if s.Length <= 4 || (let s = s.Substring(0,4) in s <> "get_" && s <> "set_") then
                None
            else 
                Some(s.Substring(4,s.Length - 4))

        let sb = new StringBuilder()
        
        print sb (asGenericParamName param)

        let getConstraintSymbols (constrainedBy: FSharpGenericParameterConstraint) =
            let memberConstraint (c: FSharpGenericParameterMemberConstraint) =
                let hasPropertyShape =
                    (c.MemberIsStatic && c.MemberArgumentTypes.Count = 0) ||
                    (not c.MemberIsStatic && c.MemberArgumentTypes.Count = 1)

                let formattedMemberName, isProperty =
                    match hasPropertyShape, tryChopPropertyName c.MemberName with
                    | true, Some(chopped) when chopped <> c.MemberName ->
                        chopped, true
                    | _, _ -> c.MemberName, false

                seq {
                    yield asSymbol " : ("
                    if c.MemberIsStatic then
                        yield asKeyword "static "

                    yield asKeyword "member "
                    yield asUserType formattedMemberName
                    yield asSymbol " : "

                    if isProperty then
                        yield asUserType (c.MemberReturnType.Format displayContext)
                    else 
                        if c.MemberArgumentTypes.Count <= 1 then
                            yield asUserType "unit"
                        else
                            printGenericParamName sb param
                        yield asSymbol " -> "
                        yield asUserType ((c.MemberReturnType.Format displayContext).TrimStart())
                    
                    yield asBrackets ")"
                }

            let typeConstraint (tc: FSharpType) =
                seq {
                    yield asSymbol " :> "
                    yield asUserType (tc.Format displayContext)
                }
            
            let constructorConstraint () =
                seq {
                    yield asSymbol " : "
                    yield asBrackets "("
                    yield asKeyword "new"
                    yield asSymbol " : "
                    yield asKeyword "unit"
                    yield asSymbol " -> '"
                    yield param.DisplayName
                    yield asBrackets ")" 
                }
            let enumConstraint (ec: FSharpType) =
                seq {
                    yield asSymbol " : "
                    yield asKeyword "enum"
                    yield asBrackets (escapeText "<")
                    yield asUserType (ec.Format displayContext)
                    yield asBrackets (escapeText ">")
                }

            let delegateConstraint (tc: FSharpGenericParameterDelegateConstraint) =
                seq {
                    yield asSymbol " : "
                    yield asKeyword "delegate"
                    yield asBrackets (escapeText "<")
                    yield asUserType (tc.DelegateTupledArgumentType.Format displayContext)
                    yield asSymbol ", "
                    yield asUserType (tc.DelegateReturnType.Format displayContext)
                    yield asBrackets (escapeText ">")
                }

            let symbols = 
                match constrainedBy with
                | _ when constrainedBy.IsCoercesToConstraint -> typeConstraint constrainedBy.CoercesToTarget
                | _ when constrainedBy.IsMemberConstraint -> memberConstraint constrainedBy.MemberConstraintData
                | _ when constrainedBy.IsSupportsNullConstraint -> seq { yield asSymbol " : "; yield asKeyword "null" }
                | _ when constrainedBy.IsRequiresDefaultConstructorConstraint -> constructorConstraint()
                | _ when constrainedBy.IsReferenceTypeConstraint -> seq { yield asSymbol " : "; yield asKeyword "not struct" }
                | _ when constrainedBy.IsEnumConstraint -> enumConstraint constrainedBy.EnumConstraintTarget
                | _ when constrainedBy.IsComparisonConstraint -> seq { yield asSymbol " : "; yield asKeyword "comparison" }
                | _ when constrainedBy.IsEqualityConstraint -> seq { yield asSymbol " : "; yield asKeyword "equality" }
                | _ when constrainedBy.IsDelegateConstraint -> delegateConstraint constrainedBy.DelegateConstraintData
                | _ when constrainedBy.IsUnmanagedConstraint -> seq { yield asSymbol " : "; yield asKeyword "unmanaged"}
                | _ -> Seq.empty

            seq { 
                yield asKeyword " when "
                yield asGenericParamName param
                yield! symbols
            }

        if param.Constraints.Count > 0 then
            param.Constraints 
            |> Seq.collect getConstraintSymbols 
            |> Seq.iter(fun symbol -> print sb symbol)

        sb.ToString()

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
                | a when a.IsInternal -> asKeyword "internal"
                | a when a.IsPrivate -> asKeyword "private"
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
                asUserType (escapeText(func.ReturnParameter.Type.Format displayContext))
            with _ex ->
                try
                    if func.FullType.GenericArguments.Count > 0 then
                        let lastArg = func.FullType.GenericArguments |> Seq.last
                        asUserType (escapeText(lastArg.Format displayContext))
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
          | Some name -> indent + name.PadRight (padding) + asSymbol ":" 

        match argInfos with
        | [] ->
            //When does this occur, val type within  module?
            if signatureOnly then retType
            else asKeyword modifiers ++ functionName ++ asSymbol ":" ++ retType
                   
        | [[]] ->
            //A ctor with () parameters seems to be a list with an empty list
            if signatureOnly then retType
            else asKeyword modifiers ++ functionName ++ asSymbol "() :" ++ retType 
        | many ->
            let allParamsLengths = 
                many |> List.map (List.map (fun p -> (p.Type.Format displayContext).Length) >> List.sum)
            let maxLength = allParamsLengths |> List.map ((+) 1) |> List.max
  
            let parameterTypeWithPadding (p: FSharpParameter) length =
                escapeText (p.Type.Format displayContext) + (String.replicate (maxLength - length) " ")

            let allParams =
                List.zip many allParamsLengths
                |> List.map(fun (paramTypes, length) ->
                                paramTypes
                                |> List.map(fun p -> formatName indent padLength p ++ asUserType (parameterTypeWithPadding p length))
                                |> String.concat (asSymbol " *" ++ "\n"))
                |> String.concat (asSymbol "->" + "\n")
            
            let typeArguments =
                allParams +  "\n" + indent + (String.replicate (max (padLength-1) 0) " ") +  asSymbol "->" ++ retType

            if signatureOnly then typeArguments
            else asKeyword modifiers ++ functionName ++ asSymbol ":" + "\n" + typeArguments

    let getEntitySignature displayContext (fse: FSharpEntity) =
        let modifier =
            match fse.Accessibility with
            | a when a.IsInternal -> asKeyword "internal "
            | a when a.IsPrivate -> asKeyword "private "
            | _ -> ""

        let typeName =
            match fse with
            | _ when fse.IsFSharpModule -> "module"
            | _ when fse.IsEnum         -> "enum"
            | _ when fse.IsValueType    -> "struct"
            | _ when fse.IsNamespace    -> "namespace"
            | _                         -> "type"

        let enumtip () =
            asSymbol " =" + "\n" + 
            asSymbol "|" ++
            (fse.FSharpFields
            |> Seq.filter (fun f -> not f.IsCompilerGenerated)
            |> Seq.map (fun field -> match field.LiteralValue with
                                     | Some lv -> field.Name + asSymbol " = " + asType Number (string lv)
                                     | None -> field.Name )
            |> String.concat ("\n" + asSymbol "| " ) )

        let uniontip () = 
            asSymbol " =" + "\n" + 
            asSymbol "|" ++ (fse.UnionCases 
                                  |> Seq.map (getUnioncaseSignature displayContext)
                                  |> String.concat ("\n" + asSymbol "| " ) )

        let delegateTip () =
            let invoker =
                fse.MembersFunctionsAndValues |> Seq.find (fun f -> f.DisplayName = "Invoke")
            let invokerSig = getFuncSignature FSharpDisplayContext.Empty invoker 6 true
            asSymbol " =" + "\n" +
            "   " + asKeyword "delegate" + " of\n" + invokerSig
                       
        let typeDisplay =
            let name =
              if fse.GenericParameters.Count > 0 then
                let p = fse.GenericParameters |> Seq.map (formatGenericParameter displayContext) |> String.concat ","
                asUserType fse.DisplayName + asBrackets (escapeText "<") + asUserType p + asBrackets (escapeText ">")
              else asUserType fse.DisplayName

            let basicName = modifier + asKeyword typeName ++ name

            if fse.IsFSharpAbbreviation then
              basicName ++ asBrackets "=" ++ asKeyword (fse.AbbreviatedType.Format displayContext)
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
        let retType = asUserType (escapeText(v.FullType.Format(displayContext)))
        let prefix = 
            if v.IsMutable then asKeyword "val" ++ asKeyword "mutable"
            else asKeyword "val"
        prefix ++ v.DisplayName ++ asSymbol ":" ++ retType

    let getFieldSignature displayContext (field: FSharpField) =
        let retType = asUserType (escapeText(field.FieldType.Format displayContext))
        match field.LiteralValue with
        | Some lv -> field.DisplayName ++ asSymbol ":" ++ retType ++ asSymbol "=" ++ asType Number (string lv)
        | None ->
            let prefix = 
                if field.IsMutable then asKeyword "val" ++ asKeyword "mutable"
                else asKeyword "val"
            prefix ++ field.DisplayName ++ asSymbol ":" ++ retType

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

    let getTooltipInformation symbol =
      async {
          let tip = getTooltipFromSymbolUse symbol
          match tip  with
          | ToolTips.ToolTip (signature, xmldoc) ->
              let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
              let result = 
                match xmldoc with
                | Full(summary) -> toolTipInfo.SummaryMarkup <- summary
                                   toolTipInfo
                | Lookup(key, potentialFilename) ->
                    let summary = 
                      maybe {let! filename = potentialFilename
                             let! markup = TooltipXmlDoc.findDocForEntity(filename, key)
                             let summary = TooltipsXml.getTooltipSummary Styles.simpleMarkup markup
                             return summary }
                    summary |> Option.iter (fun summary -> toolTipInfo.SummaryMarkup <- summary)
                    toolTipInfo
                | EmptyDoc -> toolTipInfo
              return result
          | _ -> return TooltipInformation() }
