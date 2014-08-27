module MonoDevelop.FSharp.FSharpSymbolHelper
open System
open System.Reflection
open Microsoft.FSharp.Compiler.SourceCodeServices

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
        | CorePatterns.MemberFunctionOrValue symbol when symbol.DisplayName =".ctor" -> Some(Class)
        | _ -> None

    let (|Delegate|_|) symbol =
        match symbol with
        | CorePatterns.Entity symbol when symbol.IsDelegate -> Some(Delegate)
        | _ -> None

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

type FSharpType with
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