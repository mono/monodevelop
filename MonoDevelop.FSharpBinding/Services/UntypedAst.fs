// This code borrowed from https://github.com/fsprojects/VisualFSharpPowerTools/
namespace MonoDevelop.FSharp

module UntypedAstUtils =
    open System

    open Microsoft.FSharp.Compiler.Ast
    open System.Collections.Generic
    open Microsoft.FSharp.Compiler

    type internal ShortIdent = string
    type internal Idents = ShortIdent[]

    let internal longIdentToArray (longIdent: LongIdent): Idents =
        longIdent |> List.map string |> List.toArray

    /// Returns all Idents and LongIdents found in an untyped AST.
    let internal getLongIdents (input: ParsedInput option) : IDictionary<Range.pos, Idents> =
        let identsByEndPos = Dictionary<Range.pos, Idents>()

        let addLongIdent (longIdent: LongIdent) =
            let idents = longIdentToArray longIdent
            for ident in longIdent do
                identsByEndPos.[ident.idRange.End] <- idents

        let addLongIdentWithDots (LongIdentWithDots (longIdent, lids) as value) = 
            match longIdentToArray longIdent with
            | [||] -> ()
            | [|_|] as idents -> identsByEndPos.[value.Range.End] <- idents
            | idents ->
                for dotRange in lids do 
                    identsByEndPos.[Range.mkPos dotRange.EndLine (dotRange.EndColumn - 1)] <- idents
                identsByEndPos.[value.Range.End] <- idents
        
        let addIdent (ident: Ident) = 
            identsByEndPos.[ident.idRange.End] <- [|ident.idText|]

        let (|ConstructorPats|) = function
            | Pats ps -> ps
            | NamePatPairs(xs, _) -> List.map snd xs

        let rec walkImplFileInput (ParsedImplFileInput(_, _, _, _, _, moduleOrNamespaceList, _)) = 
            List.iter walkSynModuleOrNamespace moduleOrNamespaceList

        and walkSynModuleOrNamespace (SynModuleOrNamespace(_, _, decls, _, attrs, _, _)) =
            List.iter walkAttribute attrs
            List.iter walkSynModuleDecl decls

        and walkAttribute (attr: SynAttribute) = 
            addLongIdentWithDots attr.TypeName 
            walkExpr attr.ArgExpr

        and walkTyparDecl (SynTyparDecl.TyparDecl (attrs, typar)) = 
            List.iter walkAttribute attrs
            walkTypar typar
                
        and walkTypeConstraint = function
            | SynTypeConstraint.WhereTyparDefaultsToType (t1, t2, _) -> 
                walkTypar t1
                walkType t2
            | SynTypeConstraint.WhereTyparIsValueType(t, _) -> walkTypar t
            | SynTypeConstraint.WhereTyparIsReferenceType(t, _) -> walkTypar t
            | SynTypeConstraint.WhereTyparIsUnmanaged(t, _) -> walkTypar t
            | SynTypeConstraint.WhereTyparSupportsNull (t, _) -> walkTypar t
            | SynTypeConstraint.WhereTyparIsComparable(t, _) -> walkTypar t
            | SynTypeConstraint.WhereTyparIsEquatable(t, _) -> walkTypar t
            | SynTypeConstraint.WhereTyparSubtypeOfType(t, ty, _) -> 
                walkTypar t
                walkType ty
            | SynTypeConstraint.WhereTyparSupportsMember(ts, sign, _) -> 
                List.iter walkTypar ts 
                walkMemberSig sign
            | SynTypeConstraint.WhereTyparIsEnum(t, ts, _) -> 
                walkTypar t
                List.iter walkType ts
            | SynTypeConstraint.WhereTyparIsDelegate(t, ts, _) -> 
                walkTypar t
                List.iter walkType ts

        and walkPat = function
            | SynPat.Ands (pats, _) -> List.iter walkPat pats
            | SynPat.Named (pat, ident, _, _, _) -> 
                walkPat pat
                addIdent ident
            | SynPat.Typed(pat, t, _) -> 
                walkPat pat
                walkType t
            | SynPat.Attrib(pat, attrs, _) -> 
                walkPat pat
                List.iter walkAttribute attrs
            | SynPat.Or(pat1, pat2, _) -> List.iter walkPat [pat1; pat2]
            | SynPat.LongIdent(ident, _, typars, ConstructorPats pats, _, _) -> 
                addLongIdentWithDots ident
                typars
                |> Option.iter (fun (SynValTyparDecls (typars, _, constraints)) ->
                     List.iter walkTyparDecl typars
                     List.iter walkTypeConstraint constraints)
                List.iter walkPat pats
            | SynPat.Tuple(pats, _) -> List.iter walkPat pats
            | SynPat.Paren(pat, _) -> walkPat pat
            | SynPat.ArrayOrList(_, pats, _) -> List.iter walkPat pats
            | SynPat.IsInst(t, _) -> walkType t
            | SynPat.QuoteExpr(e, _) -> walkExpr e
            | _ -> ()

        and walkTypar (Typar (_, _, _)) = ()

        and walkBinding (SynBinding.Binding(_, _, _, _, attrs, _, _, pat, returnInfo, e, _, _)) =
            List.iter walkAttribute attrs
            walkPat pat
            walkExpr e
            returnInfo |> Option.iter (fun (SynBindingReturnInfo (t, _, _)) -> walkType t)

        and walkInterfaceImpl (InterfaceImpl(_, bindings, _)) = List.iter walkBinding bindings

        and walkIndexerArg = function
            | SynIndexerArg.One e -> walkExpr e
            | SynIndexerArg.Two (e1, e2) -> List.iter walkExpr [e1; e2]

        and walkType = function
            | SynType.LongIdent ident -> addLongIdentWithDots ident
            | SynType.App(ty, _, types, _, _, _, _) -> 
                walkType ty
                List.iter walkType types
            | SynType.LongIdentApp(_, _, _, types, _, _, _) -> List.iter walkType types
            | SynType.Tuple(ts, _) -> ts |> List.iter (fun (_, t) -> walkType t)
            | SynType.Array(_, t, _) -> walkType t
            | SynType.Fun(t1, t2, _) -> 
                walkType t1
                walkType t2
            | SynType.WithGlobalConstraints(t, _, _) -> walkType t
            | SynType.HashConstraint(t, _) -> walkType t
            | SynType.MeasureDivide(t1, t2, _) -> 
                walkType t1
                walkType t2
            | SynType.MeasurePower(t, _, _) -> walkType t
            | _ -> ()

        and walkClause (Clause(pat, e1, e2, _, _)) =
            walkPat pat 
            walkExpr e2
            e1 |> Option.iter walkExpr

        and walkExpr = function
            | SynExpr.LongIdent (_, ident, _, _) -> addLongIdentWithDots ident
            | SynExpr.Ident ident -> addIdent ident
            | SynExpr.Paren (e, _, _, _) -> walkExpr e
            | SynExpr.Quote(_, _, e, _, _) -> walkExpr e
            | SynExpr.Typed(e, _, _) -> walkExpr e
            | SynExpr.Tuple(es, _, _) -> List.iter walkExpr es
            | SynExpr.ArrayOrList(_, es, _) -> List.iter walkExpr es
            | SynExpr.Record(_, _, fields, _) -> 
                fields |> List.iter (fun ((ident, _), e, _) -> 
                            addLongIdentWithDots ident
                            e |> Option.iter walkExpr)
            | SynExpr.New(_, t, e, _) -> 
                walkExpr e
                walkType t
            | SynExpr.ObjExpr(ty, argOpt, bindings, ifaces, _, _) -> 
                argOpt |> Option.iter (fun (e, ident) -> 
                    walkExpr e
                    ident |> Option.iter addIdent)
                walkType ty
                List.iter walkBinding bindings
                List.iter walkInterfaceImpl ifaces
            | SynExpr.While(_, e1, e2, _) -> List.iter walkExpr [e1; e2]
            | SynExpr.For(_, ident, e1, _, e2, e3, _) -> 
                addIdent ident
                List.iter walkExpr [e1; e2; e3]
            | SynExpr.ForEach(_, _, _, pat, e1, e2, _) -> 
                walkPat pat
                List.iter walkExpr [e1; e2]
            | SynExpr.ArrayOrListOfSeqExpr(_, e, _) -> walkExpr e
            | SynExpr.CompExpr(_, _, e, _) -> walkExpr e
            | SynExpr.Lambda(_, _, _, e, _) -> walkExpr e
            | SynExpr.MatchLambda(_, _, synMatchClauseList, _, _) -> 
                List.iter walkClause synMatchClauseList
            | SynExpr.Match(_, e, synMatchClauseList, _, _) -> 
                walkExpr e 
                List.iter walkClause synMatchClauseList
            | SynExpr.Do(e, _) -> walkExpr e
            | SynExpr.Assert(e, _) -> walkExpr e
            | SynExpr.App(_, _, e1, e2, _) -> List.iter walkExpr [e1; e2]
            | SynExpr.TypeApp(e, _, tys, _, _, _, _) -> 
                walkExpr e 
                List.iter walkType tys
            | SynExpr.LetOrUse(_, _, bindings, e, _) -> 
                List.iter walkBinding bindings 
                walkExpr e
            | SynExpr.TryWith(e, _, clauses, _, _, _, _) -> 
                List.iter walkClause clauses
                walkExpr e
            | SynExpr.TryFinally(e1, e2, _, _, _) -> List.iter walkExpr [e1; e2]
            | SynExpr.Lazy(e, _) -> walkExpr e
            | SynExpr.Sequential(_, _, e1, e2, _) -> List.iter walkExpr [e1; e2]
            | SynExpr.IfThenElse(e1, e2, e3, _, _, _, _) -> 
                List.iter walkExpr [e1; e2]
                e3 |> Option.iter walkExpr
            | SynExpr.LongIdentSet(ident, e, _) -> 
                addLongIdentWithDots ident
                walkExpr e
            | SynExpr.DotGet(e, _, idents, _) -> 
                addLongIdentWithDots idents
                walkExpr e
            | SynExpr.DotSet(e1, idents, e2, _) -> 
                walkExpr e1
                addLongIdentWithDots idents
                walkExpr e2
            | SynExpr.DotIndexedGet(e, args, _, _) -> 
                walkExpr e
                List.iter walkIndexerArg args
            | SynExpr.DotIndexedSet(e1, args, e2, _, _, _) -> 
                walkExpr e1
                List.iter walkIndexerArg args
                walkExpr e2
            | SynExpr.NamedIndexedPropertySet(ident, e1, e2, _) -> 
                addLongIdentWithDots ident
                List.iter walkExpr [e1; e2]
            | SynExpr.DotNamedIndexedPropertySet(e1, ident, e2, e3, _) -> 
                addLongIdentWithDots ident
                List.iter walkExpr [e1; e2; e3]
            | SynExpr.TypeTest(e, t, _) -> 
                walkExpr e
                walkType t
            | SynExpr.Upcast(e, t, _) -> 
                walkExpr e
                walkType t
            | SynExpr.Downcast(e, t, _) -> 
                walkExpr e
                walkType t
            | SynExpr.InferredUpcast(e, _) -> walkExpr e
            | SynExpr.InferredDowncast(e, _) -> walkExpr e
            | SynExpr.AddressOf(_, e, _, _) -> walkExpr e
            | SynExpr.JoinIn(e1, _, e2, _) -> List.iter walkExpr [e1; e2]
            | SynExpr.YieldOrReturn(_, e, _) -> walkExpr e
            | SynExpr.YieldOrReturnFrom(_, e, _) -> walkExpr e
            | SynExpr.LetOrUseBang(_, _, _, pat, e1, e2, _) -> 
                walkPat pat
                List.iter walkExpr [e1; e2]
            | SynExpr.DoBang(e, _) -> walkExpr e
            | SynExpr.TraitCall (ts, sign, e, _) ->
                List.iter walkTypar ts 
                walkMemberSig sign
                walkExpr e
            | _ -> ()

        and walkSimplePat = function
            | SynSimplePat.Attrib (pat, attrs, _) ->
                walkSimplePat pat 
                List.iter walkAttribute attrs
            | SynSimplePat.Typed(pat, t, _) ->
                walkSimplePat pat
                walkType t
            | _ -> ()

        and walkField (SynField.Field(attrs, _, _, t, _, _, _, _)) =
            List.iter walkAttribute attrs 
            walkType t

        and walkValSig (SynValSig.ValSpfn(attrs, _, _, t, _, _, _, _, _, _, _)) =
            List.iter walkAttribute attrs 
            walkType t

        and walkMemberSig = function
            | SynMemberSig.Inherit (t, _) -> walkType t
            | SynMemberSig.Member(vs, _, _) -> walkValSig vs
            | SynMemberSig.Interface(t, _) -> walkType t
            | SynMemberSig.ValField(f, _) -> walkField f
            | SynMemberSig.NestedType(SynTypeDefnSig.TypeDefnSig (info, repr, memberSigs, _), _) -> 
                let isTypeExtensionOrAlias = 
                    match repr with
                    | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _)
                    | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAbbrev, _, _)
                    | SynTypeDefnSigRepr.Simple(SynTypeDefnSimpleRepr.TypeAbbrev _, _) -> true
                    | _ -> false
                walkComponentInfo isTypeExtensionOrAlias info
                walkTypeDefnSigRepr repr
                List.iter walkMemberSig memberSigs

        and walkMember = function
            | SynMemberDefn.AbstractSlot (valSig, _, _) -> walkValSig valSig
            | SynMemberDefn.Member(binding, _) -> walkBinding binding
            | SynMemberDefn.ImplicitCtor(_, attrs, pats, _, _) -> 
                List.iter walkAttribute attrs 
                List.iter walkSimplePat pats
            | SynMemberDefn.ImplicitInherit(t, e, _, _) -> walkType t; walkExpr e
            | SynMemberDefn.LetBindings(bindings, _, _, _) -> List.iter walkBinding bindings
            | SynMemberDefn.Interface(t, members, _) -> 
                walkType t 
                members |> Option.iter (List.iter walkMember)
            | SynMemberDefn.Inherit(t, _, _) -> walkType t
            | SynMemberDefn.ValField(field, _) -> walkField field
            | SynMemberDefn.NestedType(tdef, _, _) -> walkTypeDefn tdef
            | SynMemberDefn.AutoProperty(attrs, _, _, t, _, _, _, _, e, _, _) -> 
                List.iter walkAttribute attrs
                Option.iter walkType t
                walkExpr e
            | _ -> ()

        and walkEnumCase (EnumCase(attrs, _, _, _, _)) = List.iter walkAttribute attrs

        and walkUnionCaseType = function
            | SynUnionCaseType.UnionCaseFields fields -> List.iter walkField fields
            | SynUnionCaseType.UnionCaseFullType(t, _) -> walkType t

        and walkUnionCase (SynUnionCase.UnionCase(attrs, _, t, _, _, _)) = 
            List.iter walkAttribute attrs 
            walkUnionCaseType t

        and walkTypeDefnSimple = function
            | SynTypeDefnSimpleRepr.Enum (cases, _) -> List.iter walkEnumCase cases
            | SynTypeDefnSimpleRepr.Union(_, cases, _) -> List.iter walkUnionCase cases
            | SynTypeDefnSimpleRepr.Record(_, fields, _) -> List.iter walkField fields
            | SynTypeDefnSimpleRepr.TypeAbbrev(_, t, _) -> walkType t
            | _ -> ()

        and walkComponentInfo isTypeExtensionOrAlias (ComponentInfo(attrs, typars, constraints, longIdent, _, _, _, _)) =
            List.iter walkAttribute attrs
            List.iter walkTyparDecl typars
            List.iter walkTypeConstraint constraints
            if isTypeExtensionOrAlias then
                addLongIdent longIdent

        and walkTypeDefnRepr = function
            | SynTypeDefnRepr.ObjectModel (_, defns, _) -> List.iter walkMember defns
            | SynTypeDefnRepr.Simple(defn, _) -> walkTypeDefnSimple defn

        and walkTypeDefnSigRepr = function
            | SynTypeDefnSigRepr.ObjectModel (_, defns, _) -> List.iter walkMemberSig defns
            | SynTypeDefnSigRepr.Simple(defn, _) -> walkTypeDefnSimple defn

        and walkTypeDefn (TypeDefn (info, repr, members, _)) =
            let isTypeExtensionOrAlias = 
                match repr with
                | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAugmentation, _, _)
                | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAbbrev, _, _)
                | SynTypeDefnRepr.Simple (SynTypeDefnSimpleRepr.TypeAbbrev _, _) -> true
                | _ -> false
            walkComponentInfo isTypeExtensionOrAlias info
            walkTypeDefnRepr repr
            List.iter walkMember members

        and walkSynModuleDecl (decl: SynModuleDecl) =
            match decl with
            | SynModuleDecl.NamespaceFragment fragment -> walkSynModuleOrNamespace fragment
            | SynModuleDecl.NestedModule(info, modules, _, _) ->
                walkComponentInfo false info
                List.iter walkSynModuleDecl modules
            | SynModuleDecl.Let (_, bindings, _) -> List.iter walkBinding bindings
            | SynModuleDecl.DoExpr (_, expr, _) -> walkExpr expr
            | SynModuleDecl.Types (types, _) -> List.iter walkTypeDefn types
            | SynModuleDecl.Attributes (attrs, _) -> List.iter walkAttribute attrs
            | _ -> ()

        match input with 
        | Some (ParsedInput.ImplFile input) -> 
             walkImplFileInput input
        | _ -> ()
        //debug "%A" idents
        identsByEndPos :> _

    let getLongIdentAt ast pos =
        let idents = getLongIdents (Some ast)
        match idents.TryGetValue pos with
        | true, idents -> Some idents
        | _ -> None

    /// Returns ranges of all quotations found in an untyped AST
    let getQuatationRanges ast =
        let quotationRanges = ResizeArray()

        let rec visitExpr = function
            | SynExpr.IfThenElse(cond, trueBranch, falseBranchOpt, _, _, _, _) ->
                visitExpr cond
                visitExpr trueBranch
                falseBranchOpt |> Option.iter visitExpr 
            | SynExpr.LetOrUse (_, _, bindings, body, _) -> 
                visitBindindgs bindings
                visitExpr body
            | SynExpr.LetOrUseBang (_, _, _, _, rhsExpr, body, _) -> 
                visitExpr rhsExpr
                visitExpr body
            | SynExpr.Quote (_, _isRaw, _quotedExpr, _, range) -> quotationRanges.Add range
            | SynExpr.App (_,_, funcExpr, argExpr, _) -> 
                visitExpr argExpr
                visitExpr funcExpr
            | SynExpr.Lambda (_, _, _, expr, _) -> visitExpr expr
            | SynExpr.Record (_, _, fields, _) ->
                fields |> List.choose (fun (_, expr, _) -> expr) |> List.iter visitExpr
            | SynExpr.ArrayOrListOfSeqExpr (_, expr, _) -> visitExpr expr
            | SynExpr.CompExpr (_, _, expr, _) -> visitExpr expr
            | SynExpr.ForEach (_, _, _, _, _, body, _) -> visitExpr body
            | SynExpr.YieldOrReturn (_, expr, _) -> visitExpr expr
            | SynExpr.YieldOrReturnFrom (_, expr, _) -> visitExpr expr
            | SynExpr.Do (expr, _) -> visitExpr expr
            | SynExpr.DoBang (expr, _) -> visitExpr expr
            | SynExpr.Downcast (expr, _, _) -> visitExpr expr
            | SynExpr.For (_, _, _, _, _, expr, _) -> visitExpr expr
            | SynExpr.Lazy (expr, _) -> visitExpr expr
            | SynExpr.Match (_, expr, clauses, _, _) -> 
                visitExpr expr
                visitMatches clauses 
            | SynExpr.MatchLambda (_, _, clauses, _, _) -> visitMatches clauses
            | SynExpr.ObjExpr (_, _, bindings, _, _ , _) -> visitBindindgs bindings
            | SynExpr.Typed (expr, _, _) -> visitExpr expr
            | SynExpr.Paren (expr, _, _, _) -> visitExpr expr
            | SynExpr.Sequential (_, _, expr1, expr2, _) ->
                visitExpr expr1
                visitExpr expr2
            | SynExpr.LongIdentSet (_, expr, _) -> visitExpr expr
            | SynExpr.Tuple (exprs, _, _) -> 
                for expr in exprs do 
                    visitExpr expr
            | SynExpr.TryFinally (expr1, expr2, _, _, _) ->
                visitExpr expr1
                visitExpr expr2
            | SynExpr.TryWith (expr, _, clauses, _, _, _, _) ->
                visitExpr expr
                visitMatches clauses
            | SynExpr.ArrayOrList(_, exprs, _) -> List.iter visitExpr exprs
            | SynExpr.New(_, _, expr, _) -> visitExpr expr
            | SynExpr.While(_, expr1, expr2, _) -> 
                visitExpr expr1
                visitExpr expr2
            | SynExpr.Assert(expr, _) -> visitExpr expr
            | SynExpr.TypeApp(expr, _, _, _, _, _, _) -> visitExpr expr
            | SynExpr.DotSet(_, _, expr, _) -> visitExpr expr
            | SynExpr.DotIndexedSet(_, _, expr, _, _, _) -> visitExpr expr
            | SynExpr.NamedIndexedPropertySet(_, _, expr, _) -> visitExpr expr
            | SynExpr.DotNamedIndexedPropertySet(_, _, _, expr, _) -> visitExpr expr
            | SynExpr.TypeTest(expr, _, _) -> visitExpr expr
            | SynExpr.Upcast(expr, _, _) -> visitExpr expr
            | SynExpr.InferredUpcast(expr, _) -> visitExpr expr
            | SynExpr.InferredDowncast(expr, _) -> visitExpr expr
            | SynExpr.AddressOf(_, expr, _, _) -> visitExpr expr
            | _ -> ()

        and visitBinding (Binding(_, _, _, _, _, _, _, _, _, body, _, _)) = visitExpr body
        and visitBindindgs = List.iter visitBinding
        and visitMatch (SynMatchClause.Clause (_, _, expr, _, _)) = visitExpr expr
        and visitMatches = List.iter visitMatch
        
        let visitMember = function
            | SynMemberDefn.LetBindings (bindings, _, _, _) -> visitBindindgs bindings
            | SynMemberDefn.Member (binding, _) -> visitBinding binding
            | SynMemberDefn.AutoProperty (_, _, _, _, _, _, _, _, expr, _, _) -> visitExpr expr
            | _ -> () 

        let visitType ty =
            let (SynTypeDefn.TypeDefn (_, repr, _, _)) = ty
            match repr with
            | SynTypeDefnRepr.ObjectModel (_, defns, _) ->
                for d in defns do visitMember d
            | _ -> ()

        let rec visitDeclarations decls = 
            for declaration in decls do
                match declaration with
                | SynModuleDecl.Let (_, bindings, _) -> visitBindindgs bindings
                | SynModuleDecl.DoExpr (_, expr, _) -> visitExpr expr
                | SynModuleDecl.Types (types, _) -> for ty in types do visitType ty
                | SynModuleDecl.NestedModule (_, decls, _, _) -> visitDeclarations decls
                | _ -> ()

        let visitModulesAndNamespaces modulesOrNss =
            for moduleOrNs in modulesOrNss do
                let (SynModuleOrNamespace(_, _, decls, _, _, _, _)) = moduleOrNs
                visitDeclarations decls

        ast 
        |> Option.iter (function
            | ParsedInput.ImplFile implFile ->
                let (ParsedImplFileInput(_, _, _, _, _, modules, _)) = implFile
                visitModulesAndNamespaces modules
            | _ -> ())
        quotationRanges
        
    let private singleArgumentPrintfFunctions = set [ "printf"; "printfn"; "sprintf"; "failwithf"; "eprintf"; "eprintfn" ]
    let private twoArgumentsPrintfFunctions = set [ "fprintf"; "fprintfn"; "kprintf"; "ksprintf"; "bprintf" ]

    /// Returns ranges of all printf format string literals.
    let getPrintfLiterals ast =
        let ranges = ResizeArray()

        let rec visitExpr = function
            | SynExpr.IfThenElse(cond, trueBranch, falseBranchOpt, _, _, _, _) ->
                visitExpr cond
                visitExpr trueBranch
                falseBranchOpt |> Option.iter visitExpr 
            | SynExpr.LetOrUse (_, _, bindings, body, _) -> 
                visitBindindgs bindings
                visitExpr body
            | SynExpr.LetOrUseBang (_, _, _, _, rhsExpr, body, _) -> 
                visitExpr rhsExpr
                visitExpr body
            | SynExpr.App (_,_, SynExpr.Ident funcIdent, SynExpr.Const (SynConst.String (_, r), _), _) -> 
                if singleArgumentPrintfFunctions |> Set.contains funcIdent.idText then
                    ranges.Add r
            | SynExpr.App (_,_, SynExpr.LongIdent(_, LongIdentWithDots(idents, _), _, _), SynExpr.Const (SynConst.String (_, r), _), _) -> 
                idents |> List.rev |> Seq.tryHead |> Option.iter (fun funcIdent ->
                    if singleArgumentPrintfFunctions |> Set.contains funcIdent.idText then
                        ranges.Add r)
            | SynExpr.App (_,_, SynExpr.App (_, _, SynExpr.Ident funcIdent, _, _), SynExpr.Const (SynConst.String (_, r), _), _) -> 
                if twoArgumentsPrintfFunctions |> Set.contains funcIdent.idText then
                    ranges.Add r
            | SynExpr.App (_,_, SynExpr.App (_, _, SynExpr.LongIdent(_, LongIdentWithDots(idents, _), _, _), _, _), 
                           SynExpr.Const (SynConst.String (_, r), _), _) -> 
                idents |> List.rev |> Seq.tryHead |> Option.iter (fun funcIdent ->
                    if twoArgumentsPrintfFunctions |> Set.contains funcIdent.idText then
                        ranges.Add r)
            | SynExpr.App (_,_, funcExpr, argExpr, _) -> 
                visitExpr argExpr
                visitExpr funcExpr
            | SynExpr.Lambda (_, _, _, e, _) -> visitExpr e
            | SynExpr.Record (_, _, fields, _) ->
                fields |> List.choose (fun (_, e, _) -> e) |> List.iter visitExpr
            | SynExpr.ArrayOrListOfSeqExpr (_, e, _) -> visitExpr e
            | SynExpr.CompExpr (_, _, e, _) -> visitExpr e
            | SynExpr.ForEach (_, _, _, _, e, body, _) -> 
                visitExpr e
                visitExpr body
            | SynExpr.YieldOrReturn (_, e, _) -> visitExpr e
            | SynExpr.YieldOrReturnFrom (_, e, _) -> visitExpr e
            | SynExpr.Do (e, _) -> visitExpr e
            | SynExpr.DoBang (e, _) -> visitExpr e
            | SynExpr.Downcast (e, _, _) -> visitExpr e
            | SynExpr.For (_, _, _, _, e1, e2, _) -> 
                visitExpr e1
                visitExpr e2
            | SynExpr.Lazy (e, _) -> visitExpr e
            | SynExpr.Match (_, e, clauses, _, _) -> 
                visitExpr e
                visitMatches clauses 
            | SynExpr.MatchLambda (_, _, clauses, _, _) -> visitMatches clauses
            | SynExpr.ObjExpr (_, _, bindings, _, _ , _) -> visitBindindgs bindings
            | SynExpr.Typed (e, _, _) -> visitExpr e
            | SynExpr.Paren (e, _, _, _) -> visitExpr e
            | SynExpr.Sequential (_, _, e1, e2, _) ->
                visitExpr e1
                visitExpr e2
            | SynExpr.LongIdentSet (_, e, _) -> visitExpr e
            | SynExpr.Tuple (es, _, _) -> List.iter visitExpr es
            | SynExpr.ArrayOrList(_, es, _) -> List.iter visitExpr es
            | SynExpr.New(_, _, e, _) -> visitExpr e
            | SynExpr.While(_, e1, e2, _) -> visitExpr e1; visitExpr e2
            | SynExpr.Assert(e, _) -> visitExpr e
            | SynExpr.TryWith(e, _, clauses, _, _, _, _) -> visitExpr e; visitMatches clauses
            | SynExpr.TryFinally(e1, e2, _, _, _) -> visitExpr e1; visitExpr e2
            | SynExpr.NamedIndexedPropertySet(_, e1, e2, _) -> visitExpr e1; visitExpr e2
            | SynExpr.DotNamedIndexedPropertySet(_, _, e1, e2, _) -> visitExpr e1; visitExpr e2
            | SynExpr.TypeTest(e, _, _) -> visitExpr e
            | SynExpr.Upcast(e, _, _) -> visitExpr e
            | SynExpr.InferredUpcast(e, _) -> visitExpr e
            | SynExpr.InferredDowncast(e, _) -> visitExpr e
            | SynExpr.DotGet(e, _, _, _) -> visitExpr e
            | SynExpr.Quote(_, _, e, _, _) -> visitExpr e
            | SynExpr.TypeApp(e, _, _, _, _, _, _) -> visitExpr e
            | SynExpr.DotSet(_, _, e, _) -> visitExpr e
            | SynExpr.DotIndexedGet(e, _, _, _) -> visitExpr e
            | SynExpr.DotIndexedSet(e1, _, e2, _, _, _) -> 
                visitExpr e1
                visitExpr e2
            | _ -> ()

        and visitBinding (Binding(_, _, _, _, _, _, _, _, _, body, _, _)) = visitExpr body
        and visitBindindgs = List.iter visitBinding
        and visitMatch (SynMatchClause.Clause (_, _, expr, _, _)) = visitExpr expr
        and visitMatches = List.iter visitMatch
        
        let visitMember = function
            | SynMemberDefn.LetBindings (bindings, _, _, _) -> visitBindindgs bindings
            | SynMemberDefn.Member (binding, _) -> visitBinding binding
            | SynMemberDefn.AutoProperty (_, _, _, _, _, _, _, _, expr, _, _) -> visitExpr expr
            | _ -> () 

        let visitType ty =
            let (SynTypeDefn.TypeDefn (_, repr, memberDefns, _)) = ty
            match repr with
            | SynTypeDefnRepr.ObjectModel (_, defns, _) ->
                for d in defns do visitMember d
            | _ -> ()
            List.iter visitMember memberDefns

        let rec visitDeclarations decls = 
            for declaration in decls do
                match declaration with
                | SynModuleDecl.Let (_, bindings, _) -> visitBindindgs bindings
                | SynModuleDecl.DoExpr (_, expr, _) -> visitExpr expr
                | SynModuleDecl.Types (types, _) -> for ty in types do visitType ty
                | SynModuleDecl.NestedModule (_, decls, _, _) -> visitDeclarations decls
                | _ -> ()

        let visitModulesAndNamespaces modulesOrNss =
            for moduleOrNs in modulesOrNss do
                let (SynModuleOrNamespace(_, _, decls, _, _, _, _)) = moduleOrNs
                visitDeclarations decls

        ast 
        |> Option.iter (function
            | ParsedInput.ImplFile implFile ->
                let (ParsedImplFileInput(_, _, _, _, _, modules, _)) = implFile
                visitModulesAndNamespaces modules
            | _ -> ())

        List.ofSeq ranges
        
    /// Returns all string literal ranges
    let internal getStringLiterals ast : Range.range list =
        let result = ResizeArray() 
         
        let rec visitExpr = function 
            | SynExpr.IfThenElse(cond, trueBranch, falseBranchOpt, _, _, _, _) ->
                visitExpr cond
                visitExpr trueBranch
                falseBranchOpt |> Option.iter visitExpr 
            | SynExpr.LetOrUse (_, _, bindings, body, _) -> 
                visitBindindgs bindings
                visitExpr body
            | SynExpr.LetOrUseBang (_, _, _, _, rhsExpr, body, _) -> 
                visitExpr rhsExpr
                visitExpr body
            | SynExpr.App (_,_, funcExpr, argExpr, _) -> 
                visitExpr argExpr
                visitExpr funcExpr
            | SynExpr.Lambda (_, _, _, expr, _) -> visitExpr expr
            | SynExpr.Record (_, _, fields, _) ->
                fields |> List.choose (fun (_, expr, _) -> expr) |> List.iter visitExpr
            | SynExpr.ArrayOrListOfSeqExpr (_, expr, _) -> visitExpr expr
            | SynExpr.CompExpr (_, _, expr, _) -> visitExpr expr
            | SynExpr.ForEach (_, _, _, _, _, body, _) -> visitExpr body
            | SynExpr.YieldOrReturn (_, expr, _) -> visitExpr expr
            | SynExpr.YieldOrReturnFrom (_, expr, _) -> visitExpr expr
            | SynExpr.Do (expr, _) -> visitExpr expr
            | SynExpr.DoBang (expr, _) -> visitExpr expr
            | SynExpr.Downcast (expr, _, _) -> visitExpr expr
            | SynExpr.For (_, _, _, _, _, expr, _) -> visitExpr expr
            | SynExpr.Lazy (expr, _) -> visitExpr expr
            | SynExpr.Match (_, expr, clauses, _, _) -> 
                visitExpr expr
                visitMatches clauses 
            | SynExpr.MatchLambda (_, _, clauses, _, _) -> visitMatches clauses
            | SynExpr.ObjExpr (_, _, bindings, _, _ , _) -> visitBindindgs bindings
            | SynExpr.Typed (expr, _, _) -> visitExpr expr
            | SynExpr.Paren (expr, _, _, _) -> visitExpr expr
            | SynExpr.Sequential (_, _, expr1, expr2, _) ->
                visitExpr expr1
                visitExpr expr2
            | SynExpr.LongIdentSet (_, expr, _) -> visitExpr expr
            | SynExpr.Tuple (exprs, _, _) -> List.iter visitExpr exprs
            | SynExpr.Const (SynConst.String (_, r), _) -> result.Add r
            | SynExpr.ArrayOrList(_, exprs, _) -> List.iter visitExpr exprs
            | SynExpr.New(_, _, expr, _) -> visitExpr expr
            | SynExpr.While(_, e1, e2, _) -> visitExpr e1; visitExpr e2
            | SynExpr.Assert(e, _) -> visitExpr e
            | SynExpr.TryWith(e, _, clauses, _, _, _, _) -> visitExpr e; visitMatches clauses
            | SynExpr.TryFinally(e1, e2, _, _, _) -> visitExpr e1; visitExpr e2
            | SynExpr.NamedIndexedPropertySet(_, e1, e2, _) -> visitExpr e1; visitExpr e2
            | SynExpr.DotNamedIndexedPropertySet(_, _, e1, e2, _) -> visitExpr e1; visitExpr e2
            | SynExpr.TypeTest(e, _, _) -> visitExpr e
            | SynExpr.Upcast(e, _, _) -> visitExpr e
            | SynExpr.InferredUpcast(e, _) -> visitExpr e
            | SynExpr.InferredDowncast(e, _) -> visitExpr e
            | SynExpr.DotGet(e, _, _, _) -> visitExpr e
            | _ -> ()
             
        and visitBinding (Binding(_, _, _, _, _, _, _, _, _, body, _, _)) = visitExpr body
        and visitBindindgs = List.iter visitBinding
        and visitMatch (SynMatchClause.Clause (_, _, expr, _, _)) = visitExpr expr
        and visitMatches = List.iter visitMatch
        
        let visitMember = function
            | SynMemberDefn.LetBindings (bindings, _, _, _) -> visitBindindgs bindings
            | SynMemberDefn.Member (binding, _) -> visitBinding binding
            | SynMemberDefn.AutoProperty (_, _, _, _, _, _, _, _, expr, _, _) -> visitExpr expr
            | _ -> () 

        let visitType ty =
            let (SynTypeDefn.TypeDefn (_, repr, memberDefns, _)) = ty
            match repr with
            | SynTypeDefnRepr.ObjectModel (_, defns, _) ->
                for d in defns do visitMember d
            | _ -> ()
            List.iter visitMember memberDefns

        let rec visitDeclarations decls = 
            for declaration in decls do
                match declaration with
                | SynModuleDecl.Let (_, bindings, _) -> visitBindindgs bindings
                | SynModuleDecl.DoExpr (_, expr, _) -> visitExpr expr
                | SynModuleDecl.Types (types, _) -> for ty in types do visitType ty
                | SynModuleDecl.NestedModule (_, decls, _, _) -> visitDeclarations decls
                | _ -> ()

        let visitModulesAndNamespaces modulesOrNss =
            for moduleOrNs in modulesOrNss do
                let (SynModuleOrNamespace(_, _, decls, _, _, _, _)) = moduleOrNs
                visitDeclarations decls

        ast 
        |> Option.iter (function
            | ParsedInput.ImplFile implFile ->
                let (ParsedImplFileInput(_, _, _, _, _, modules, _)) = implFile
                visitModulesAndNamespaces modules
            | _ -> ())

        List.ofSeq result