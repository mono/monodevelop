namespace FSharp.CompilerBinding

// This code borrowed from https://github.com/fsprojects/VisualFSharpPowerTools/

open System
open System.IO
open System.Diagnostics
open System.Collections.Generic
open System.CodeDom.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices

[<RequireQualifiedAccess; NoEquality; NoComparison>]
type InterfaceData =
    | Interface of SynType * SynMemberDefns option
    | ObjExpr of SynType * SynBinding list
    member x.Range =
        match x with
        | InterfaceData.Interface(typ, _) -> 
            typ.Range
        | InterfaceData.ObjExpr(typ, _) -> 
            typ.Range
    member x.MemberCount =
        match x with
        | InterfaceData.Interface(_, None) -> 
            0
        | InterfaceData.Interface(_, Some members) -> 
            List.length members
        | InterfaceData.ObjExpr(_, bindings) ->
            List.length bindings
    member x.TypeParameters = 
        match x with
        | InterfaceData.Interface(typ, _)
        | InterfaceData.ObjExpr(typ, _) ->
            match typ with
            | SynType.App(_, _, ts, _, _, _, _)
            | SynType.LongIdentApp(_, _, _, ts, _, _, _) ->
                let (|TypeIdent|_|) = function
                    | SynType.Var(SynTypar.Typar(s, req , _), _) ->
                        match req with
                        | NoStaticReq -> 
                            Some ("'" + s.idText)
                        | HeadTypeStaticReq -> 
                            Some ("^" + s.idText)
                    | SynType.LongIdent(LongIdentWithDots(xs, _)) ->
                        xs |> Seq.map (fun x -> x.idText) |> String.concat "." |> Some
                    | _ -> 
                        None
                ts |> Seq.choose (|TypeIdent|_|) |> Seq.toArray
            | _ ->
                [||]

module InterfaceStubGenerator =
    type internal ColumnIndentedTextWriter() =
        let stringWriter = new StringWriter()
        let indentWriter = new IndentedTextWriter(stringWriter, " ")

        member x.Write(s: string, [<ParamArray>] objs: obj []) =
            indentWriter.Write(s, objs)

        member x.WriteLine(s: string, [<ParamArray>] objs: obj []) =
            indentWriter.WriteLine(s, objs)

        member x.Indent i = 
            indentWriter.Indent <- indentWriter.Indent + i

        member x.Unindent i = 
            indentWriter.Indent <- max 0 (indentWriter.Indent - i)

        member x.Writer = 
            indentWriter :> TextWriter

        member x.Dump() =
            indentWriter.InnerWriter.ToString()

        interface IDisposable with
            member x.Dispose() =
                stringWriter.Dispose()
                indentWriter.Dispose()  

    [<NoComparison>]
    type internal Context =
        {
            Writer: ColumnIndentedTextWriter
            /// Map generic types to specific instances for specialized interface implementation
            TypeInstantations: Map<string, string>
            /// Indentation inside method bodies
            Indentation: int
            /// Object identifier of the interface e.g. 'x', 'this', '__', etc.
            ObjectIdent: string
            /// A list of lines represents skeleton of each member
            MethodBody: string []
            DisplayContext: FSharpDisplayContext
        }

    // Adapt from MetadataFormat module in FSharp.Formatting 

    let internal (|AllAndLast|_|) (xs: 'T list) = 
        match xs with
        | [] -> 
            None
        | _ -> 
            let revd = List.rev xs
            Some(List.rev revd.Tail, revd.Head)

    let internal isAttrib<'T> (attrib: FSharpAttribute)  =
        attrib.AttributeType.CompiledName = typeof<'T>.Name

    let internal hasAttrib<'T> (attribs: IList<FSharpAttribute>) = 
        attribs |> Seq.exists (fun a -> isAttrib<'T>(a))

    let internal (|MeasureProd|_|) (typ: FSharpType) = 
        if typ.HasTypeDefinition && typ.TypeDefinition.LogicalName = "*" && typ.GenericArguments.Count = 2 then
            Some (typ.GenericArguments.[0], typ.GenericArguments.[1])
        else None

    let internal (|MeasureInv|_|) (typ: FSharpType) = 
        if typ.HasTypeDefinition && typ.TypeDefinition.LogicalName = "/" && typ.GenericArguments.Count = 1 then 
            Some typ.GenericArguments.[0]
        else None

    let internal (|MeasureOne|_|) (typ: FSharpType) = 
        if typ.HasTypeDefinition && typ.TypeDefinition.LogicalName = "1" && typ.GenericArguments.Count = 0 then 
            Some ()
        else None

    let internal getTypeParameterName (typar: FSharpGenericParameter) =
        (if typar.IsSolveAtCompileTime then "^" else "'") + typar.Name

    let internal formatTypeArgument (ctx: Context) (typar: FSharpGenericParameter) =
        let genericName = getTypeParameterName typar
        match ctx.TypeInstantations.TryFind(genericName) with
        | Some specificName ->
            specificName
        | None ->
            genericName

    let internal formatTypeArguments ctx (typars:seq<FSharpGenericParameter>) =
        Seq.map (formatTypeArgument ctx) typars |> List.ofSeq

    let internal bracket (str: string) = 
        if str.Contains(" ") then "(" + str + ")" else str

    let internal formatType ctx (typ: FSharpType) =
        let genericDefinition = typ.Format(ctx.DisplayContext)
        (genericDefinition, ctx.TypeInstantations)
        ||> Map.fold (fun s k v -> s.Replace(k, v))

    let internal keywordSet = set Microsoft.FSharp.Compiler.Lexhelp.Keywords.keywordNames

    type NamesWithIndices = Map<string, Set<int>>

    let normalizeArgName (namesWithIndices: NamesWithIndices) nm =
        match nm with
        | "()" -> nm, namesWithIndices
        | _ ->
            let nm = String.lowerCaseFirstChar nm
            let nm, index = String.extractTrailingIndex nm
                
            let index, namesWithIndices =
                match namesWithIndices |> Map.tryFind nm, index with
                | Some indexes, index ->
                    let rec getAvailableIndex idx =
                        if indexes |> Set.contains idx then 
                            getAvailableIndex (idx + 1)
                        else idx
                    let index = index |> Option.getOrElse 1 |> getAvailableIndex
                    Some index, namesWithIndices |> Map.add nm (indexes |> Set.add index)
                | None, Some index -> Some index, namesWithIndices |> Map.add nm (Set.ofList [index])
                | None, None -> None, namesWithIndices |> Map.add nm Set.empty

            let nm = 
                match index with
                | Some index -> sprintf "%s%d" nm index
                | None -> nm
                
            let nm = if Set.contains nm keywordSet then sprintf "``%s``" nm else nm
            nm, namesWithIndices

    // Format each argument, including its name and type 
    let internal formatArgUsage ctx hasTypeAnnotation (namesWithIndices: Map<string, Set<int>>) (arg: FSharpParameter) = 
        let nm = 
            match arg.Name with 
            | None ->
                if arg.Type.HasTypeDefinition && arg.Type.TypeDefinition.XmlDocSig = "T:Microsoft.FSharp.Core.unit" then "()" 
                else sprintf "arg%d" (namesWithIndices |> Map.toList |> List.map snd |> List.sumBy Set.count |> max 1)
            | Some x -> x
        
        let nm, namesWithIndices = normalizeArgName namesWithIndices nm
        
        // Detect an optional argument
        let isOptionalArg = hasAttrib<OptionalArgumentAttribute> arg.Attributes
        let argName = if isOptionalArg then "?" + nm else nm
        (if hasTypeAnnotation && argName <> "()" then 
            argName + ": " + formatType ctx arg.Type
        else argName),
        namesWithIndices

    let internal formatArgsUsage ctx hasTypeAnnotation (v: FSharpMemberFunctionOrValue) args =
        let isItemIndexer = (v.IsInstanceMember && v.DisplayName = "Item")
        let unit, argSep, tupSep = "()", " ", ", "
        let args, namesWithIndices =
            args
            |> List.fold (fun (argsSoFar: string list list, namesWithIndices) args ->
                let argsSoFar', namesWithIndices =
                    args 
                    |> List.fold (fun (acc: string list, allNames) arg -> 
                        let name, allNames = formatArgUsage ctx hasTypeAnnotation allNames arg
                        name :: acc, allNames) ([], namesWithIndices)
                List.rev argsSoFar' :: argsSoFar, namesWithIndices) 
                ([], Map.ofList [ ctx.ObjectIdent, Set.empty ])
        args
        |> List.rev
        |> List.map (function 
            | [] -> unit 
            | [arg] when arg = unit -> unit
            | [arg] when not v.IsMember || isItemIndexer -> arg 
            | args when isItemIndexer -> String.concat tupSep args
            | args -> bracket (String.concat tupSep args))
        |> String.concat argSep
        , namesWithIndices
  
    let internal formatMember (ctx: Context) (v: FSharpMemberFunctionOrValue) = 
        let getParamArgs (argInfos: FSharpParameter list list) = 
            let args, namesWithIndices =
                match argInfos with
                | [[x]] when v.IsGetterMethod && x.Name.IsNone 
                             && x.Type.TypeDefinition.XmlDocSig = "T:Microsoft.FSharp.Core.unit" -> 
                    "", Map.ofList [ctx.ObjectIdent, Set.empty]
                | _  -> formatArgsUsage ctx true v argInfos
             
            if String.IsNullOrWhiteSpace(args) then "" 
            elif args.StartsWith("(") then args
            else sprintf "(%s)" args
            , namesWithIndices

        let buildUsage argInfos = 
            let parArgs, _ = getParamArgs argInfos
            match v.IsMember, v.IsInstanceMember, v.LogicalName, v.DisplayName with
            // Constructors
            | _, _, ".ctor", _ -> "new" + parArgs
            // Properties (skipping arguments)
            | _, true, _, name when v.IsProperty -> name
            // Ordinary instance members
            | _, true, _, name -> name + parArgs
            // Ordinary functions or values
            | false, _, _, name when 
                not (hasAttrib<RequireQualifiedAccessAttribute> v.LogicalEnclosingEntity.Attributes) -> 
                name + " " + parArgs
            // Ordinary static members or things (?) that require fully qualified access
            | _, _, _, name -> name + parArgs

        let modifiers =
            [ if v.InlineAnnotation = FSharpInlineAnnotation.AlwaysInline then yield "inline"
              // Skip dispatch slot because we generate stub implementation
              if v.IsDispatchSlot then () ]

        let argInfos = 
            v.CurriedParameterGroups |> Seq.map Seq.toList |> Seq.toList 
            
        let retType = v.ReturnParameter.Type

        let argInfos, retType = 
            match argInfos, v.IsGetterMethod, v.IsSetterMethod with
            | [ AllAndLast(args, last) ], _, true -> [ args ], Some last.Type
            | [[]], true, _ -> [], Some retType
            | _, _, _ -> argInfos, Some retType

        let retType = defaultArg (retType |> Option.map (formatType ctx)) "unit"
        let usage = buildUsage argInfos

        ctx.Writer.WriteLine("")
        ctx.Writer.Write("member ")
        for modifier in modifiers do
            ctx.Writer.Write("{0} ", modifier)
        ctx.Writer.Write("{0}.", ctx.ObjectIdent)
        
        if v.IsSetterMethod then
            ctx.Writer.WriteLine(usage)
            ctx.Writer.Indent ctx.Indentation
            match getParamArgs argInfos with
            | "", _ | "()", _ ->
                ctx.Writer.WriteLine("with set (v: {0}): unit = ", retType)
            | args, namesWithIndices ->
                let valueArgName, _ = normalizeArgName namesWithIndices "v"
                ctx.Writer.WriteLine("with set {0} ({1}: {2}): unit = ", args, valueArgName, retType)
            ctx.Writer.Indent ctx.Indentation
            for line in ctx.MethodBody do
                ctx.Writer.WriteLine(line)
            ctx.Writer.Unindent ctx.Indentation
            ctx.Writer.Unindent ctx.Indentation
        elif v.IsGetterMethod then
            ctx.Writer.WriteLine(usage)
            ctx.Writer.Indent ctx.Indentation
            match getParamArgs argInfos with
            | "", _ ->
                ctx.Writer.WriteLine("with get (): {0} = ", retType)
            | args, _ ->
                ctx.Writer.WriteLine("with get {0}: {1} = ", args, retType)
            ctx.Writer.Indent ctx.Indentation
            for line in ctx.MethodBody do
                ctx.Writer.WriteLine(line)
            ctx.Writer.Unindent ctx.Indentation
            ctx.Writer.Unindent ctx.Indentation
        else
            ctx.Writer.Write(usage)
            ctx.Writer.WriteLine(": {0} = ", retType)
            ctx.Writer.Indent ctx.Indentation
            for line in ctx.MethodBody do
                ctx.Writer.WriteLine(line)
            ctx.Writer.Unindent ctx.Indentation

    let internal getGenericParameters (e: FSharpEntity) =
        if e.IsFSharpAbbreviation then
            e.AbbreviatedType.TypeDefinition.GenericParameters
        else
            e.GenericParameters

    let rec internal getNonAbbreviatedType (typ: FSharpType) =
        if typ.HasTypeDefinition && typ.TypeDefinition.IsFSharpAbbreviation then
            getNonAbbreviatedType typ.AbbreviatedType
        else typ

    /// Filter out duplicated interfaces in inheritance chain
    let rec internal getInterfaces (e: FSharpEntity) = 
        seq { for iface in e.AllInterfaces ->
                let typ = getNonAbbreviatedType iface
                typ.TypeDefinition 
        }
        |> Seq.distinct

    /// Get members in the decreasing order of inheritance chain
    let internal getInterfaceMembers (e: FSharpEntity) = 
        seq {
            for iface in getInterfaces e do
                yield! iface.MembersFunctionsAndValues |> Seq.filter (fun m -> 
                           // Use this hack when FCS doesn't return enough information on .NET properties
                           iface.IsFSharp || not m.IsProperty)
         }

    let countInterfaceMembers e =
        getInterfaceMembers e |> Seq.length

    let rec isInterface (e: FSharpEntity) =
        e.IsInterface || (e.IsFSharpAbbreviation && isInterface e.AbbreviatedType.TypeDefinition)

    /// Generate stub implementation of an interface at a start column
    let formatInterface startColumn indentation (typeInstances: string []) objectIdent 
        (methodBody: string) (displayContext: FSharpDisplayContext) (e: FSharpEntity) =
        Debug.Assert(isInterface e, "The entity should be an interface.")
        use writer = new ColumnIndentedTextWriter()
        let lines = methodBody.Replace("\r\n", "\n").Split('\n')
        let typeParams = Seq.map getTypeParameterName e.GenericParameters
        let instantiations = 
            let insts =
                Seq.zip typeParams typeInstances
                |> Seq.filter(fun (t1, t2) -> t1 <> t2) 
                |> Map.ofSeq
            // A simple hack to handle instantiation of type alias 
            if e.IsFSharpAbbreviation then
                let typ = getNonAbbreviatedType e.AbbreviatedType
                (typ.TypeDefinition.GenericParameters |> Seq.map getTypeParameterName, 
                 typ.GenericArguments |> Seq.map (fun typ -> typ.Format(displayContext)))
                ||> Seq.zip
                |> Seq.fold (fun acc (x, y) -> Map.add x y acc) insts
            else insts

        let ctx = { Writer = writer; TypeInstantations = instantiations; Indentation = indentation; 
                    ObjectIdent = objectIdent; MethodBody = lines; DisplayContext = displayContext }
        writer.Indent startColumn
        for v in getInterfaceMembers e do
            formatMember ctx v
        writer.Dump()

    let internal (|IndexerArg|) = function
        | SynIndexerArg.Two(e1, e2) -> [e1; e2]
        | SynIndexerArg.One e -> [e]

    let internal (|IndexerArgList|) xs =
        List.collect (|IndexerArg|) xs

    let internal inRange range pos = 
        AstTraversal.rangeContainsPosLeftEdgeInclusive range pos

    let tryFindInterfaceDeclaration (pos: pos) (parsedInput: ParsedInput) =
        let rec walkImplFileInput (ParsedImplFileInput(_name, _isScript, _fileName, _scopedPragmas, _hashDirectives, moduleOrNamespaceList, _)) = 
            List.tryPick walkSynModuleOrNamespace moduleOrNamespaceList

        and walkSynModuleOrNamespace(SynModuleOrNamespace(_lid, _isModule, decls, _xmldoc, _attributes, _access, range)) =
            if not <| inRange range pos then
                None
            else
                List.tryPick walkSynModuleDecl decls

        and walkSynModuleDecl(decl: SynModuleDecl) =
            if not <| inRange decl.Range pos then
                None
            else
                match decl with
                | SynModuleDecl.Exception(ExceptionDefn(_repr, synMembers, _defnRange), _range) -> 
                    List.tryPick walkSynMemberDefn synMembers
                | SynModuleDecl.Let(_isRecursive, bindings, _range) ->
                    List.tryPick walkBinding bindings
                | SynModuleDecl.ModuleAbbrev(_lhs, _rhs, _range) ->
                    None
                | SynModuleDecl.NamespaceFragment(fragment) ->
                    walkSynModuleOrNamespace fragment
                | SynModuleDecl.NestedModule(_componentInfo, modules, _isContinuing, _range) ->
                    List.tryPick walkSynModuleDecl modules
                | SynModuleDecl.Types(typeDefs, _range) ->
                    List.tryPick walkSynTypeDefn typeDefs
                | SynModuleDecl.DoExpr (_, expr, _) ->
                    walkExpr expr
                | SynModuleDecl.Attributes _
                | SynModuleDecl.HashDirective _
                | SynModuleDecl.Open _ -> 
                    None

        and walkSynTypeDefn(TypeDefn(_componentInfo, representation, members, range)) = 
            if not <| inRange range pos then
                None
            else
                walkSynTypeDefnRepr representation
                |> Option.orElse (List.tryPick walkSynMemberDefn members)        

        and walkSynTypeDefnRepr(typeDefnRepr: SynTypeDefnRepr) = 
            if not <| inRange typeDefnRepr.Range pos then
                None
            else
                match typeDefnRepr with
                | SynTypeDefnRepr.ObjectModel(_kind, members, _range) ->
                    List.tryPick walkSynMemberDefn members
                | SynTypeDefnRepr.Simple(_repr, _range) -> 
                    None

        and walkSynMemberDefn (memberDefn: SynMemberDefn) =
            if not <| inRange memberDefn.Range pos then
                None
            else
                match memberDefn with
                | SynMemberDefn.AbstractSlot(_synValSig, _memberFlags, _range) ->
                    None
                | SynMemberDefn.AutoProperty(_attributes, _isStatic, _id, _type, _memberKind, _memberFlags, _xmlDoc, _access, expr, _r1, _r2) ->
                    walkExpr expr
                | SynMemberDefn.Interface(interfaceType, members, _range) ->
                    if inRange interfaceType.Range pos then
                        Some(InterfaceData.Interface(interfaceType, members))
                    else
                        Option.bind (List.tryPick walkSynMemberDefn) members
                | SynMemberDefn.Member(binding, _range) ->
                    walkBinding binding
                | SynMemberDefn.NestedType(typeDef, _access, _range) -> 
                    walkSynTypeDefn typeDef
                | SynMemberDefn.ValField(_field, _range) ->
                    None
                | SynMemberDefn.LetBindings(bindings, _isStatic, _isRec, _range) ->
                    List.tryPick walkBinding bindings
                | SynMemberDefn.Open _
                | SynMemberDefn.ImplicitInherit _
                | SynMemberDefn.Inherit _
                | SynMemberDefn.ImplicitCtor _ -> 
                    None

        and walkBinding (Binding(_access, _bindingKind, _isInline, _isMutable, _attrs, _xmldoc, _valData, _headPat, _retTy, expr, _bindingRange, _seqPoint)) =
            walkExpr expr

        and walkExpr expr =
            if not <| inRange expr.Range pos then 
                None
            else
                match expr with
                | SynExpr.Quote(synExpr1, _, synExpr2, _, _range) ->
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.Const(_synConst, _range) -> 
                    None

                | SynExpr.Paren(synExpr, _, _, _parenRange) ->
                    walkExpr synExpr
                | SynExpr.Typed(synExpr, _synType, _range) -> 
                    walkExpr synExpr

                | SynExpr.Tuple(synExprList, _, _range)
                | SynExpr.ArrayOrList(_, synExprList, _range) ->
                    List.tryPick walkExpr synExprList

                | SynExpr.Record(_inheritOpt, _copyOpt, fields, _range) -> 
                    List.tryPick (fun (_, e, _) -> Option.bind walkExpr e) fields

                | SynExpr.New(_, _synType, synExpr, _range) -> 
                    walkExpr synExpr

                | SynExpr.ObjExpr(ty, baseCallOpt, binds, ifaces, _range1, _range2) -> 
                    match baseCallOpt with
                    | None -> 
                        if inRange ty.Range pos then
                            Some (InterfaceData.ObjExpr(ty, binds))
                        else
                            ifaces |> List.tryPick (fun (InterfaceImpl(ty, binds, range)) ->
                                if inRange range pos then 
                                    Some (InterfaceData.ObjExpr(ty, binds))
                                else None)
                    | Some _ -> 
                        // Ignore object expressions of normal objects
                        None

                | SynExpr.While(_sequencePointInfoForWhileLoop, synExpr1, synExpr2, _range) ->
                    List.tryPick walkExpr [synExpr1; synExpr2]
                | SynExpr.ForEach(_sequencePointInfoForForLoop, _seqExprOnly, _isFromSource, _synPat, synExpr1, synExpr2, _range) -> 
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.For(_sequencePointInfoForForLoop, _ident, synExpr1, _, synExpr2, synExpr3, _range) -> 
                    List.tryPick walkExpr [synExpr1; synExpr2; synExpr3]

                | SynExpr.ArrayOrListOfSeqExpr(_, synExpr, _range) ->
                    walkExpr synExpr
                | SynExpr.CompExpr(_, _, synExpr, _range) ->
                    walkExpr synExpr
                | SynExpr.Lambda(_, _, _synSimplePats, synExpr, _range) ->
                     walkExpr synExpr

                | SynExpr.MatchLambda(_isExnMatch, _argm, synMatchClauseList, _spBind, _wholem) -> 
                    synMatchClauseList |> List.tryPick (fun (Clause(_, _, e, _, _)) -> walkExpr e)
                | SynExpr.Match(_sequencePointInfoForBinding, synExpr, synMatchClauseList, _, _range) ->
                    walkExpr synExpr
                    |> Option.orElse (synMatchClauseList |> List.tryPick (fun (Clause(_, _, e, _, _)) -> walkExpr e))

                | SynExpr.Lazy(synExpr, _range) ->
                    walkExpr synExpr
                | SynExpr.Do(synExpr, _range) ->
                    walkExpr synExpr
                | SynExpr.Assert(synExpr, _range) -> 
                    walkExpr synExpr

                | SynExpr.App(_exprAtomicFlag, _isInfix, synExpr1, synExpr2, _range) ->
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.TypeApp(synExpr, _, _synTypeList, _commas, _, _, _range) -> 
                    walkExpr synExpr

                | SynExpr.LetOrUse(_, _, synBindingList, synExpr, _range) -> 
                    Option.orElse (List.tryPick walkBinding synBindingList) (walkExpr synExpr)

                | SynExpr.TryWith(synExpr, _range, _synMatchClauseList, _range2, _range3, _sequencePointInfoForTry, _sequencePointInfoForWith) -> 
                    walkExpr synExpr

                | SynExpr.TryFinally(synExpr1, synExpr2, _range, _sequencePointInfoForTry, _sequencePointInfoForFinally) -> 
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.Sequential(_sequencePointInfoForSeq, _, synExpr1, synExpr2, _range) -> 
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.IfThenElse(synExpr1, synExpr2, synExprOpt, _sequencePointInfoForBinding, _isRecovery, _range, _range2) -> 
                    match synExprOpt with
                    | Some synExpr3 ->
                        List.tryPick walkExpr [synExpr1; synExpr2; synExpr3]
                    | None ->
                        List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.Ident(_ident) ->
                    None
                | SynExpr.LongIdent(_, _longIdent, _altNameRefCell, _range) -> 
                    None

                | SynExpr.LongIdentSet(_longIdent, synExpr, _range) ->
                    walkExpr synExpr
                | SynExpr.DotGet(synExpr, _dotm, _longIdent, _range) -> 
                    walkExpr synExpr

                | SynExpr.DotSet(synExpr1, _longIdent, synExpr2, _range) ->
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.DotIndexedGet(synExpr, IndexerArgList synExprList, _range, _range2) -> 
                    Option.orElse (walkExpr synExpr) (List.tryPick walkExpr synExprList) 

                | SynExpr.DotIndexedSet(synExpr1, IndexerArgList synExprList, synExpr2, _, _range, _range2) -> 
                    [ yield synExpr1
                      yield! synExprList
                      yield synExpr2 ]
                    |> List.tryPick walkExpr

                | SynExpr.JoinIn(synExpr1, _range, synExpr2, _range2) ->
                    List.tryPick walkExpr [synExpr1; synExpr2]
                | SynExpr.NamedIndexedPropertySet(_longIdent, synExpr1, synExpr2, _range) ->
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.DotNamedIndexedPropertySet(synExpr1, _longIdent, synExpr2, synExpr3, _range) ->  
                    List.tryPick walkExpr [synExpr1; synExpr2; synExpr3]

                | SynExpr.TypeTest(synExpr, _synType, _range)
                | SynExpr.Upcast(synExpr, _synType, _range)
                | SynExpr.Downcast(synExpr, _synType, _range) ->
                    walkExpr synExpr
                | SynExpr.InferredUpcast(synExpr, _range)
                | SynExpr.InferredDowncast(synExpr, _range) ->
                    walkExpr synExpr
                | SynExpr.AddressOf(_, synExpr, _range, _range2) ->
                    walkExpr synExpr
                | SynExpr.TraitCall(_synTyparList, _synMemberSig, synExpr, _range) ->
                    walkExpr synExpr

                | SynExpr.Null(_range)
                | SynExpr.ImplicitZero(_range) -> 
                    None

                | SynExpr.YieldOrReturn(_, synExpr, _range)
                | SynExpr.YieldOrReturnFrom(_, synExpr, _range) 
                | SynExpr.DoBang(synExpr, _range) -> 
                    walkExpr synExpr

                | SynExpr.LetOrUseBang(_sequencePointInfoForBinding, _, _, _synPat, synExpr1, synExpr2, _range) -> 
                    List.tryPick walkExpr [synExpr1; synExpr2]

                | SynExpr.LibraryOnlyILAssembly _
                | SynExpr.LibraryOnlyStaticOptimization _ 
                | SynExpr.LibraryOnlyUnionCaseFieldGet _
                | SynExpr.LibraryOnlyUnionCaseFieldSet _ ->
                    None
                | SynExpr.ArbitraryAfterError(_debugStr, _range) -> 
                    None

                | SynExpr.FromParseError(synExpr, _range)
                | SynExpr.DiscardAfterMissingQualificationAfterDot(synExpr, _range) -> 
                    walkExpr synExpr 

        match parsedInput with
        | ParsedInput.SigFile _input ->
            None
        | ParsedInput.ImplFile input -> 
            walkImplFileInput input




