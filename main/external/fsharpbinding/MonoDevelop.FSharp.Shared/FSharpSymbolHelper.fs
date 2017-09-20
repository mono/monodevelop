namespace MonoDevelop.FSharp.Shared
open System
open System.Collections.Generic
open System.Text
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

module Symbols =
    let getLocationFromSymbolUse (s: FSharpSymbolUse) =
        [s.Symbol.DeclarationLocation; s.Symbol.SignatureLocation]
        |> List.choose id
        |> List.distinctBy (fun r -> r.FileName)

    let getLocationFromSymbol (s:FSharpSymbol) =
        [s.DeclarationLocation; s.SignatureLocation]
        |> List.choose id
        |> List.distinctBy (fun r -> r.FileName)

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
        not symbol.IsConstructor && not symbol.IsPropertyGetterMethod && not symbol.IsPropertySetterMethod

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

type XmlDoc =
  ///A full xmldoc tooltip
| Full of string
  ///A lookup of key, filename
| Lookup of string * string option
  ///No xmldoc
| EmptyDoc

type ParameterTooltip =
  ///A ToolTip of signature, summary
  | ToolTip of signature:string * doc:XmlDoc * parameters: string array
    ///A empty tip
  | EmptyTip

type ToolTips =
  ///A ToolTip of signature, summary
  | ToolTip of signature:string * doc:XmlDoc * footer:string
    ///A empty tip
  | EmptyTip

[<AutoOpen>]
module PrintParameter =
    let print sb = Printf.bprintf sb "%s"


module SymbolTooltips =
    let maxPadding = 20
    type NestedFunctionParams =
    | GenericParam of FSharpGenericParameter
    | TupleParam of IList<FSharpType>
    | NamedType of FSharpType

    /// Concat two strings with a space between if both a and b are not IsNullOrWhiteSpace
    let internal (++) (a:string) (b:string) =
        match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> a + " " + b
         
    let getKeywordTooltip (keyword:string) =
        //let signatureline = syntaxHighlight keyword ++ "(keyword)"
        let signatureline = keyword ++ "(keyword)"
        let summary =
            match KeywordList.keywordDescriptions.TryGetValue keyword with
            | true, description -> Full description
            | false, _ -> EmptyDoc
        signatureline, summary, ""

    let getSummaryFromSymbol (symbol:FSharpSymbol) =
        let getXmlDocSig() =
            try
                symbol.XmlDocSig
            with
            | exn -> ""

        let xmlDoc, xmlDocSig =
            match symbol with
            | :? FSharpMemberOrFunctionOrValue as func -> func.XmlDoc, getXmlDocSig()
            | :? FSharpEntity as fse -> fse.XmlDoc, getXmlDocSig()
            | :? FSharpField as fsf -> fsf.XmlDoc, getXmlDocSig()
            | :? FSharpUnionCase as fsu -> fsu.XmlDoc, getXmlDocSig()
            | :? FSharpActivePatternCase as apc -> apc.XmlDoc, getXmlDocSig()
            | :? FSharpGenericParameter as gp -> gp.XmlDoc, ""
            | _ -> ResizeArray() :> IList<_>, ""

        if xmlDoc.Count > 0
        then Full (String.Join( "\n", xmlDoc))
        else Lookup(xmlDocSig, symbol.Assembly.FileName)
    
    let getUnioncaseSignature displayContext (unionCase:FSharpUnionCase) =
        if unionCase.UnionCaseFields.Count > 0 then
            let typeList =
                unionCase.UnionCaseFields
                |> Seq.map (fun unionField -> unionField.Name ++ ":" ++ ((unionField.FieldType.Format displayContext)))
                |> String.concat " * "
            unionCase.DisplayName ++ "of" ++ typeList
         else unionCase.DisplayName

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
                if s.StartsWith("get_", StringComparison.Ordinal) ||
                    s.StartsWith("set_", StringComparison.Ordinal)
                then s
                else chopStringTo s '.'

            if s.Length <= 4 || (let s = s.Substring(0,4) in s <> "get_" && s <> "set_") then
                None
            else
                Some(s.Substring(4,s.Length - 4))

        let asGenericParamName (param: FSharpGenericParameter) =
            (if param.IsSolveAtCompileTime then "^" else "'") + param.Name

        let sb = new StringBuilder()

        print sb (asGenericParamName param)

        let getConstraintSymbols (constrainedBy: FSharpGenericParameterConstraint) =
            let memberConstraint (c: FSharpGenericParameterMemberConstraint) =

                let formattedMemberName, isProperty =
                    match c.IsProperty, tryChopPropertyName c.MemberName with
                    | true, Some(chopped) when chopped <> c.MemberName ->
                        chopped, true
                    | _, _ -> c.MemberName, false

                seq {
                    yield " : ("
                    if c.MemberIsStatic then yield "static "

                    yield "member "
                    yield formattedMemberName
                    yield " : "

                    if isProperty then
                        yield (c.MemberReturnType.Format displayContext)
                    else
                        if c.MemberArgumentTypes.Count <= 1 then
                            yield "unit"
                        else
                            yield asGenericParamName param
                        yield " -> "
                        yield ((c.MemberReturnType.Format displayContext).TrimStart())

                    yield ")"
                }

            let typeConstraint (tc: FSharpType) =
                seq {
                    yield " :> "
                    yield (tc.Format displayContext)
                }

            let constructorConstraint () =
                seq {
                    yield " : "
                    yield "("
                    yield "new"
                    yield " : "
                    yield "unit"
                    yield " -> '"
                    yield param.DisplayName
                    yield ")"
                }
            let enumConstraint (ec: FSharpType) =
                seq {
                    yield " : "
                    yield "enum"
                    yield "<"
                    yield ec.Format displayContext
                    yield ">"
                }

            let delegateConstraint (tc: FSharpGenericParameterDelegateConstraint) =
                seq {
                    yield " : "
                    yield "delegate"
                    yield "<"
                    yield tc.DelegateTupledArgumentType.Format displayContext
                    yield ", "
                    yield tc.DelegateReturnType.Format displayContext
                    yield ">"
                }

            let symbols =
                match constrainedBy with
                | _ when constrainedBy.IsCoercesToConstraint -> typeConstraint constrainedBy.CoercesToTarget
                | _ when constrainedBy.IsMemberConstraint -> memberConstraint constrainedBy.MemberConstraintData
                | _ when constrainedBy.IsSupportsNullConstraint -> seq { yield " : "; yield "null" }
                | _ when constrainedBy.IsRequiresDefaultConstructorConstraint -> constructorConstraint()
                | _ when constrainedBy.IsReferenceTypeConstraint -> seq { yield " : "; yield "not struct" }
                | _ when constrainedBy.IsEnumConstraint -> enumConstraint constrainedBy.EnumConstraintTarget
                | _ when constrainedBy.IsComparisonConstraint -> seq { yield " : "; yield "comparison" }
                | _ when constrainedBy.IsEqualityConstraint -> seq { yield " : "; yield "equality" }
                | _ when constrainedBy.IsDelegateConstraint -> delegateConstraint constrainedBy.DelegateConstraintData
                | _ when constrainedBy.IsUnmanagedConstraint -> seq { yield " : "; yield "unmanaged"}
                | _ when constrainedBy.IsNonNullableValueTypeConstraint -> seq { yield " : "; yield "struct" }
                | _ -> Seq.empty

            seq {
                yield " when "
                yield asGenericParamName param
                yield! symbols
            }

        if param.Constraints.Count > 0 then
            param.Constraints
            |> Seq.collect getConstraintSymbols
            |> Seq.iter(fun symbol -> print sb symbol)

        sb.ToString()

    type FormatOptions =
      {Indent : int; Highlight :string option}
        static member Default = {Indent=3;Highlight=None}

    let getFuncSignatureWithFormat displayContext (func: FSharpMemberOrFunctionOrValue) (format:FormatOptions) =
        let indent = String.replicate format.Indent " "
        let functionName =
            let name =
                if func.IsConstructor then
                    match func.EnclosingEntity with
                    | Some ent -> ent.DisplayName
                    | _ ->
                        //LoggingService.LogWarning(sprintf "getFuncSignatureWithFormat: No enclosing entity found for: %s" func.DisplayName)
                        func.DisplayName
                elif func.IsOperatorOrActivePattern then func.DisplayName
                elif func.DisplayName.StartsWith "( " then PrettyNaming.QuoteIdentifierIfNeeded func.LogicalName
                else func.DisplayName
            name

        let modifiers =
            let accessibility =
                match func.Accessibility with
                | a when a.IsInternal -> "internal"
                | a when a.IsPrivate -> "private"
                | _ -> ""

            let modifier =
                //F# types are prefixed with new, should non F# types be too for consistancy?
                if func.IsConstructor then
                    match func.EnclosingEntity with
                    | Some ent -> if ent.IsFSharp then "new" ++ accessibility
                                  else accessibility
                    | _ ->
                      //LoggingService.LogWarning(sprintf "getFuncSignatureWithFormat: No enclosing entity found for: %s" func.DisplayName)
                      accessibility
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
                func.ReturnParameter.Type.Format displayContext
            with _ex ->
                try
                    if func.FullType.GenericArguments.Count > 0 then
                        let lastArg = func.FullType.GenericArguments |> Seq.last
                        lastArg.Format displayContext
                    else "Unknown"
                with _ -> "Unknown"

        let padLength =
            let allLengths =
                argInfos
                |> List.concat
                |> List.map (fun p -> match p.Name with Some name -> name.Length | None -> p.DisplayName.Length)
            match allLengths with
            | [] -> 0
            | l -> l |> List.maxUnderThreshold maxPadding

        let asUnderline = sprintf "_STARTUNDERLINE_%s_ENDUNDERLINE_" // we replace with real markup after highlighting

        let formatName indent padding (parameter:FSharpParameter) =
            let name = match parameter.Name with Some name -> name | None -> parameter.DisplayName
            match format.Highlight with
            | Some paramName when paramName = name ->
                match padding - name.Length with
                | i when i > 0 -> indent + asUnderline name + String.replicate i " " + ":"
                | _ -> indent + asUnderline name + ":"
            | _ -> indent + name.PadRight padding + ":"

        let isDelegate =
            match func.EnclosingEntity with
            | Some ent -> ent.IsDelegate
            | _ ->
                //LoggingService.logWarning "getFuncSignatureWithFormat: No enclosing entity found for: %s" func.DisplayName
                false

        match argInfos with
        | [] ->
            //When does this occur, val type within  module?
            if isDelegate then retType
            else modifiers ++ functionName ++ ":" ++ retType

        | [[]] ->
            //A ctor with () parameters seems to be a list with an empty list
            if isDelegate then retType
            else modifiers ++ functionName ++ "() :" ++ retType
        | many ->
              let formatParameter (p:FSharpParameter) =
                  try
                      p.Type.Format displayContext
                  with
                  | :? InvalidOperationException -> p.DisplayName

              let allParamsLengths =
                  many |> List.map (List.map (fun p -> (formatParameter p).Length) >> List.sum)
              let maxLength = (allParamsLengths |> List.maxUnderThreshold maxPadding)+1

              let parameterTypeWithPadding (p: FSharpParameter) length =
                  (formatParameter p) + (String.replicate (if length >= maxLength then 1 else maxLength - length) " ")

              let allParams =
                  List.zip many allParamsLengths
                  |> List.map(fun (paramTypes, length) ->
                                  paramTypes
                                  |> List.map(fun p -> formatName indent padLength p ++ (parameterTypeWithPadding p length))
                                  |> String.concat (" *" ++ "\n"))
                  |> String.concat ("->\n")

              let typeArguments =
                  allParams +  "\n" + indent + (String.replicate (max (padLength-1) 0) " ") + "->" ++ retType

              if isDelegate then typeArguments
              else modifiers ++ functionName ++ ":" + "\n" + typeArguments

    let getFuncSignature f c = getFuncSignatureWithFormat f c FormatOptions.Default

    let getEntitySignature displayContext (fse: FSharpEntity) =
        let modifier =
            match fse.Accessibility with
            | a when a.IsInternal -> "internal "
            | a when a.IsPrivate -> "private "
            | _ -> ""

        let typeName =
            match fse with
            | _ when fse.IsFSharpModule -> "module"
            | _ when fse.IsEnum         -> "enum"
            | _ when fse.IsValueType    -> "struct"
            | _ when fse.IsNamespace    -> "namespace"
            | _                         -> "type"

        let enumtip () =
            " =\n" +
            "|" ++
            (fse.FSharpFields
            |> Seq.filter (fun f -> not f.IsCompilerGenerated)
            |> Seq.map (fun field -> match field.LiteralValue with
                                     | Some lv -> field.Name + " = " + (string lv)
                                     | None -> field.Name )
            |> String.concat ("\n" + "| " ) )

        let uniontip () =
            " =" + "\n" +
            "|" ++ (fse.UnionCases
                                  |> Seq.map (getUnioncaseSignature displayContext)
                                  |> String.concat ("\n" + "| " ) )

        let delegateTip () =
            let invoker =
                fse.MembersFunctionsAndValues |> Seq.find (fun f -> f.DisplayName = "Invoke")
            let invokerSig = getFuncSignatureWithFormat displayContext invoker {Indent=6;Highlight=None}
            " =" + "\n" +
            "   " + "delegate" + " of\n" + invokerSig

        let typeDisplay =
            let name =
                if fse.GenericParameters.Count > 0 then
                    let p = fse.GenericParameters |> Seq.map (formatGenericParameter displayContext) |> String.concat ","
                    fse.DisplayName + ("<") + p + (">")
                else fse.DisplayName

            let basicName = modifier + typeName ++ name

            if fse.IsFSharpAbbreviation then
                let unannotatedType = fse.UnAnnotate()
                basicName ++ "=" ++ (unannotatedType.DisplayName)
            else
                basicName

        

        if fse.IsFSharpUnion then typeDisplay + uniontip ()
        elif fse.IsEnum then typeDisplay + enumtip ()
        elif fse.IsDelegate then typeDisplay + delegateTip ()
        else typeDisplay

    let getValSignature displayContext (v:FSharpMemberOrFunctionOrValue) =
        let retType = v.FullType.Format displayContext
        let prefix =
            if v.IsMutable then "val" ++ "mutable"
            else "val"
        let name =
            if v.DisplayName.StartsWith "( "
            then PrettyNaming.QuoteIdentifierIfNeeded v.LogicalName
            else v.DisplayName
        prefix ++ name ++ ":" ++ retType

    let getFieldSignature displayContext (field: FSharpField) =
        let retType = field.FieldType.Format displayContext
        match field.LiteralValue with
        | Some lv -> field.DisplayName ++ ":" ++ retType ++ "=" ++ (string lv)
        | None ->
            let prefix =
                if field.IsMutable then "val" ++ "mutable"
                else "val"
            prefix ++ field.DisplayName ++ ":" ++ retType

    let getAPCaseSignature displayContext (apc:FSharpActivePatternCase) =
      let findVal =
          apc.Group.EnclosingEntity
          |> Option.bind (fun ent -> ent.MembersFunctionsAndValues
                                    |> Seq.tryFind (fun func -> func.DisplayName.Contains apc.DisplayName)
                                    |> Option.map (getFuncSignature displayContext))

      match findVal with
      | Some v -> v
      | None -> apc.Group.OverallType.Format displayContext

    let footerForType (entity:FSharpSymbolUse) =
        match entity with
        | MemberFunctionOrValue m ->
            if m.FullType.HasTypeDefinition then
                let ent = m.FullType.TypeDefinition
                let parent = ent.UnAnnotate()
                let parentType = parent.DisplayName
                let parentDesc = if parent.IsFSharpModule then "module" else "type"
                sprintf "<small>From %s:\t%s</small>%s<small>Assembly:\t%s</small>" parentDesc parentType Environment.NewLine ent.Assembly.SimpleName
            else
                sprintf "<small>Assembly:\t%s</small>" m.Assembly.SimpleName
      
        | Entity c ->
            let ns = c.Namespace |> Option.getOrElse (fun () -> c.AccessPath)
            let fullName =
                match c.TryGetFullNameWithUnderScoreTypes() with
                | Some fullname -> "<small>Full name: " + fullname + "</small>"
                | None -> "<small>Full name: " + c.QualifiedName + "</small>"

            sprintf "%s%s<small>Namespace:\t%s</small>%s<small>Assembly:\t%s</small>" fullName Environment.NewLine ns Environment.NewLine c.Assembly.SimpleName
      
        | Field f ->
            let parent = f.DeclaringEntity.UnAnnotate().DisplayName
            sprintf "<small>From type:\t%s</small>%s<small>Assembly:\t%s</small>" parent Environment.NewLine f.Assembly.SimpleName
      
        | ActivePatternCase ap ->
          let parent =
              ap.Group.EnclosingEntity
              |> Option.map (fun enclosing -> enclosing.UnAnnotate().DisplayName)
              |> Option.fill "None"
          sprintf "<small>From type:\t%s</small>%s<small>Assembly:\t%s</small>" parent Environment.NewLine ap.Assembly.SimpleName
      
        |  UnionCase uc ->
            let parent = uc.ReturnType.TypeDefinition.UnAnnotate().DisplayName
            sprintf "<small>From type:\t%s</small>%s<small>Assembly:\t%s</small>" parent Environment.NewLine uc.Assembly.SimpleName
        | _ -> ""

    let getTooltipFromSymbolUse (symbol:FSharpSymbolUse) =
        match symbol with
        | Entity fse ->
            try
                let signature = getEntitySignature symbol.DisplayContext fse
                Some(signature, getSummaryFromSymbol fse, footerForType symbol)
            with exn ->
                //MonoDevelop.Core.LoggingService.LogWarning (sprintf "getTooltipFromSymbolUse: Error occured processing %A" fse)
                None

        | Constructor func ->
            match func.EnclosingEntity with
            | Some ent when ent.IsValueType || ent.IsEnum ->
                  //ValueTypes
                  let signature = getFuncSignature symbol.DisplayContext func
                  Some(signature, getSummaryFromSymbol func, footerForType symbol)
            | _ ->
                  //ReferenceType constructor
                  let signature = getFuncSignature symbol.DisplayContext func
                  Some(signature, getSummaryFromSymbol func, footerForType symbol)

        | Operator func ->
            let signature = getFuncSignature symbol.DisplayContext func
            Some(signature, getSummaryFromSymbol func, footerForType symbol)

        | Pattern func ->
            //Active pattern or operator
            let signature = getFuncSignature symbol.DisplayContext func
            Some(signature, getSummaryFromSymbol func, footerForType symbol)

        | ClosureOrNestedFunction func ->
            //represents a closure or nested function
            let signature = getFuncSignature symbol.DisplayContext func
            let summary = getSummaryFromSymbol func
            Some(signature, summary, footerForType symbol)

        | Function func ->
            let signature = getFuncSignature symbol.DisplayContext func
            Some(signature, getSummaryFromSymbol func, footerForType symbol)

        | Val func ->
            //val name : Type
            let signature = getValSignature symbol.DisplayContext func
            Some(signature, getSummaryFromSymbol func, footerForType symbol)

        | Property prop ->
            let signature = getFuncSignature symbol.DisplayContext prop
            Some(signature, getSummaryFromSymbol prop, footerForType symbol)

        | Field fsf ->
            let signature = getFieldSignature symbol.DisplayContext fsf
            Some(signature, getSummaryFromSymbol fsf, footerForType symbol)

        | UnionCase uc ->
            let signature = getUnioncaseSignature symbol.DisplayContext uc
            Some(signature, getSummaryFromSymbol uc, footerForType symbol)

        | ActivePatternCase apc ->
            let signature = getAPCaseSignature symbol.DisplayContext apc
            Some(signature, getSummaryFromSymbol apc, footerForType symbol)

        | ActivePattern ap ->
            let signature = getFuncSignature symbol.DisplayContext ap
            Some(signature, getSummaryFromSymbol ap, footerForType symbol)
            
        | GenericParameter gp ->
            let signature = formatGenericParameter symbol.DisplayContext gp
            Some(signature, getSummaryFromSymbol gp, footerForType symbol)
            
        | other ->
            //LoggingService.logWarning "F# Tooltip not rendered for: %A" other.Symbol
            None

    let getTooltipFromParameter (p:FSharpParameter) context =
      let typ = p.Type.Format context
      let signature =
          match p.Name with
          | Some name -> name ++ ":" ++ typ
          | None -> typ

      signature, getSummaryFromSymbol p

