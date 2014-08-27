module MonoDevelop.FSharp.FSharpSymbolHelper

open Microsoft.FSharp.Compiler.SourceCodeServices

module CorePatterns =
    let (|ActivePatternCase|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpActivePatternCase -> ActivePatternCase(symbol :?> FSharpActivePatternCase) |> Some
        | _ -> None

    let (|Entity|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpEntity -> Entity(symbol :?> FSharpEntity) |> Some
        | _ -> None

    let (|Field|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpField -> Field(symbol :?> FSharpField) |> Some
        |  _ -> None

    let (|GenericParameter|_|) (symbol: FSharpSymbol) = 
        match symbol with
        | :? FSharpGenericParameter -> GenericParameter(symbol :?> FSharpGenericParameter) |> Some
        | _ -> None

    let (|MemberFunctionOrValue|_|) (symbol : FSharpSymbol) =
        match symbol with
        | :? FSharpMemberFunctionOrValue -> MemberFunctionOrValue(symbol:?> FSharpMemberFunctionOrValue) |> Some
        | _ -> None

    let (|Parameter|_|) (symbol: FSharpSymbol) = 
        match symbol with
        | :? FSharpParameter -> Parameter(symbol :?> FSharpParameter) |> Some
        | _ -> None

    let (|StaticParameter|_|) (symbol:FSharpSymbol) =
        match symbol with
        | :? FSharpStaticParameter -> StaticParameter(symbol:?> FSharpStaticParameter) |> Some
        | _ -> None

    let (|UnionCase|_|) (symbol:FSharpSymbol) =
        match symbol with
        | :? FSharpUnionCase -> UnionCase(symbol :?> FSharpUnionCase) |> Some
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

type Microsoft.FSharp.Compiler.SourceCodeServices.FSharpType with
    member this.NonAbbreviatedTypeName
        with get() = 
            match this.IsAbbreviation with
            | false -> this.TypeDefinition.FullName
            | true -> this.AbbreviatedType.NonAbbreviatedTypeName
    member this.NonAbbreviatedType
        with get() =
            System.Type.GetType(this.NonAbbreviatedTypeName)

type System.Type with
    member this.PublicInstanceMembers 
        with get() = 
            this.GetMembers(System.Reflection.BindingFlags.Instance ||| System.Reflection.BindingFlags.FlattenHierarchy ||| System.Reflection.BindingFlags.Public)

type System.Reflection.MemberInfo with
    member this.DeclaringTypeValue 
        with get() =
            match this with
            | :? System.Reflection.MethodInfo -> (this :?> System.Reflection.MethodInfo).GetBaseDefinition().DeclaringType
            | _ -> this.DeclaringType