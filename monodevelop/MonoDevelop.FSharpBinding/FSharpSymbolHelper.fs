module MonoDevelop.FSharp.FSharpSymbolHelper
open System
open System.Reflection
open Microsoft.FSharp.Compiler.SourceCodeServices

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

    let isConstructor (func: FSharpMemberFunctionOrValue) =
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
        | :? FSharpMemberFunctionOrValue as func -> MemberFunctionOrValue(func) |> Some
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
                    && not symbol.IsPropertySetterMethod 
                    && not symbolUse.IsFromComputationExpression then 
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