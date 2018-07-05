namespace rec Roslyn.FSharp

open System
open System.Collections.Immutable
open System.Collections.Generic
open System.Linq
open System.Reflection
open System.Runtime.CompilerServices
open Microsoft.CodeAnalysis
open Microsoft.FSharp.Compiler.SourceCodeServices

module EntityLookup =
    let lookup = ConditionalWeakTable<FSharpEntity, FSharpNamedTypeSymbol>()
    let getOrCreate (entity:FSharpEntity) =
        match lookup.TryGetValue (entity) with
        | true, symbol -> symbol
        | false, _ ->
            let symbol = FSharpNamedTypeSymbol(entity)
            lookup.Add(entity, symbol)
            symbol

[<AutoOpen>]
module TypeHelpers =
    let namedTypeFromEntity (entity:FSharpEntity) =
        EntityLookup.getOrCreate entity :> INamedTypeSymbol

    let namespaceOrTypeSymbol (entity:FSharpEntity) =
        if entity.IsNamespace then
            FSharpEntityNamespaceSymbol(entity) :> INamespaceOrTypeSymbol
        else
            EntityLookup.getOrCreate entity :> INamespaceOrTypeSymbol

type FSharpTypeSymbol (entity:FSharpEntity) =
    inherit FSharpNamespaceOrTypeSymbol(entity)

    member x.Entity = entity

    override this.GetAttributes () =
        entity.Attributes
        |> Seq.map(fun a -> FSharpEntityAttributeData(a, entity) :> AttributeData)
        |> Seq.toImmutableArray

    override x.Equals (other:obj) =
        match other with
        | :? FSharpTypeSymbol as symbol ->
            entity = symbol.Entity
        | _ -> false

    override x.GetHashCode() = entity.GetHashCode()

    override x.CommonEquals(other) = x.Equals(other)

    static member op_Equality(left:FSharpTypeSymbol, right:FSharpTypeSymbol) =
        left.Equals(right)

    override this.ContainingType =
        entity.DeclaringEntity
        |> Option.filter(fun e -> not e.IsNamespace)
        |> Option.map namedTypeFromEntity
        |> Option.toObj

    override this.GetDocumentationCommentId() = entity.XmlDocSig

    override this.GetDocumentationCommentXml(culture, _expand, token) =
        if entity.XmlDoc.Count > 0 then
            String.concat "\n" entity.XmlDoc
        else
            XmlDocumentation.getXmlDocFromAssembly entity.Assembly.FileName entity.XmlDocSig culture token

    interface ITypeSymbol with
        member x.AllInterfaces =
            entity.AllInterfaces
            |> Seq.choose typeDefinitionSafe
            |> Seq.map namedTypeFromEntity
            |> Seq.toImmutableArray

        member x.BaseType =
            let baseType =
                try
                    entity.BaseType
                with 
                // entity.BaseType can throw if we don't reference the
                // assembly that the base type comes from
                | _ex -> None

            baseType
            |> Option.bind typeDefinitionSafe
            |> Option.map namedTypeFromEntity
            |> Option.toObj

        member x.Interfaces =
            entity.DeclaredInterfaces
            |> Seq.choose typeDefinitionSafe
            |> Seq.map namedTypeFromEntity
            |> Seq.toImmutableArray

        member x.IsAnonymousType = false

        member x.IsReferenceType = not entity.IsValueType && not entity.IsFSharpModule

        member x.IsTupleType = notImplemented()

        member x.IsValueType = entity.IsValueType

        /// Currently we only care about definitions for entities, not uses
        member x.OriginalDefinition = x :> ITypeSymbol

        member x.SpecialType = 
            let fullName =
                entity.AbbreviatedTypeSafe
                |> Option.bind (fun t -> t.TypeDefinitionSafe)
                |> Option.map (fun typeDefinition -> typeDefinition.FullName)

            match fullName with
            | Some "System.Boolean" -> SpecialType.System_Boolean
            | Some "System.SByte" -> SpecialType.System_SByte
            | Some "System.Int16" -> SpecialType.System_Int16
            | Some "System.Int32" -> SpecialType.System_Int32
            | Some "System.Int64" -> SpecialType.System_Int64
            | Some "System.Byte" -> SpecialType.System_Byte
            | Some "System.UInt16" -> SpecialType.System_UInt16
            | Some "System.UInt32" -> SpecialType.System_UInt32
            | Some "System.UInt64" -> SpecialType.System_UInt64
            | Some "System.Single" -> SpecialType.System_Single
            | Some "System.Double" -> SpecialType.System_Double
            | Some "System.Char" -> SpecialType.System_Char
            | Some "System.String" -> SpecialType.System_String
            | Some "System.Object" -> SpecialType.System_Object
            //TODO: Many more special types
            | _ -> SpecialType.None
        member x.TypeKind =
            match entity with
            | _ when entity.IsArrayType -> TypeKind.Array
            | _ when entity.IsClass -> TypeKind.Class
            | _ when entity.IsDelegate -> TypeKind.Delegate
            | _ when entity.IsEnum -> TypeKind.Enum
            | _ when entity.IsInterface -> TypeKind.Interface
            | _ when entity.IsFSharpModule -> TypeKind.Module // TODO: Is this the same meaning?
            | _ when entity.IsValueType && not entity.IsEnum -> TypeKind.Struct
            | _ -> TypeKind.Unknown

        member x.FindImplementationForInterfaceMember(interfaceMember) =
            notImplemented()

/// Represents a type other than an array, a pointer, a type parameter, and dynamic.
and FSharpNamedTypeSymbol (entity: FSharpEntity) as this =
    inherit FSharpTypeSymbol(entity)

    let constructors() =
        (this :> INamespaceOrTypeSymbol).GetMembers().OfType<IMethodSymbol>()
        |> Seq.filter(fun m -> m.Name = ".ctor")

    member x.Entity = entity
    override this.MetadataName = entity.LogicalName

    override x.Equals (other:obj) =
        match other with
        | :? FSharpNamedTypeSymbol as symbol ->
            entity = symbol.Entity
        | _ -> false

    override x.CommonEquals other = x.Equals other

    override x.GetHashCode() = entity.GetHashCode()

    interface INamedTypeSymbol with
        member x.Arity = entity.GenericParameters.Count

        member x.AssociatedSymbol = notImplemented()

        member x.ConstructedFrom = notImplemented()

        member x.Constructors = constructors() |> Seq.toImmutableArray

        member x.DelegateInvokeMethod = notImplemented()

        member x.EnumUnderlyingType = notImplemented()

        member x.InstanceConstructors =
            constructors()
            |> Seq.filter(fun m -> not m.IsStatic)
            |> Seq.toImmutableArray

        member x.IsComImport = notImplemented()

        member x.IsGenericType = entity.GenericParameters.Count > 0

        member x.IsImplicitClass = notImplemented()

        member x.IsSerializable = false

        member x.IsScriptClass = entity.DeclarationLocation.FileName.EndsWith(".fsx")

        member x.IsUnboundGenericType  = notImplemented()

        member x.MemberNames =
            (x :> INamespaceOrTypeSymbol).GetMembers()
            |> Seq.map(fun m -> m.Name)

        member x.MightContainExtensionMethods = notImplemented()

        member x.OriginalDefinition = x :> INamedTypeSymbol

        member x.StaticConstructors =
            constructors()
            |> Seq.filter(fun m -> m.IsStatic)
            |> Seq.toImmutableArray

        member x.TupleElements = notImplemented()

        member x.TupleUnderlyingType = notImplemented()

        member x.TypeArguments = notImplemented()

        member x.TypeParameters = notImplemented()

        member x.Construct (typeArguments) = notImplemented()

        member x.ConstructUnboundGenericType () = notImplemented()

        member x.GetTypeArgumentCustomModifiers (ordinal) = notImplemented()

and FSharpParameterSymbol(param: FSharpParameter, ordinal:int) =
    inherit FSharpSymbolBase()
    member x.Parameter = param

    override x.GetAttributes() =
        param.Attributes
        |> Seq.map(fun a -> FSharpAttributeData(a) :> AttributeData)
        |> Seq.toImmutableArray

    override x.Equals(other:obj) =
        match other with
        | :? FSharpParameter as otherParam ->
            param = otherParam
        | _ -> false

    override x.CommonEquals other = x.Equals other
    override x.GetHashCode() = param.GetHashCode()

    interface IParameterSymbol with
        member x.CustomModifiers = notImplemented()
        member x.ExplicitDefaultValue = notImplemented()
        member x.HasExplicitDefaultValue = notImplemented()
        member x.IsOptional = param.IsOptionalArg
        member x.IsParams =  notImplemented()
        member x.IsThis = notImplemented()
        member x.Ordinal = ordinal
        member x.OriginalDefinition = x :> IParameterSymbol
        member x.RefCustomModifiers = notImplemented()
        member x.RefKind = notImplemented()
        member x.Type =
            FSharpTypeSymbol(param.Type.TypeDefinition) :> _

and FSharpMemberOrFunctionOrValueSymbol(mfv:FSharpMemberOrFunctionOrValue) =
    inherit FSharpISymbol(mfv)

    member x.Member = mfv
    override x.Name = mfv.CompiledName

    override x.Equals(other:obj) =
        match other with
        | :? FSharpMemberOrFunctionOrValueSymbol as otherMember ->
            mfv = otherMember.Member
        | _ -> false

    override x.CommonEquals other = x.Equals other

    override x.GetHashCode() = mfv.GetHashCode()

    override this.GetDocumentationCommentId() = mfv.XmlDocSig

    override this.GetDocumentationCommentXml(culture, _expand, token) =
        if mfv.XmlDoc.Count > 0 then
            String.concat "\n" mfv.XmlDoc
        else
            XmlDocumentation.getXmlDocFromAssembly mfv.Assembly.FileName mfv.XmlDocSig culture token

and FSharpMethodSymbol (method:FSharpMemberOrFunctionOrValue) =
    inherit FSharpMemberOrFunctionOrValueSymbol(method)

    override x.Kind = SymbolKind.Method

    interface IMethodSymbol with
        member x.Arity = notImplemented()

        member x.AssociatedAnonymousDelegate = notImplemented()

        member x.AssociatedSymbol = notImplemented()

        member x.ConstructedFrom = notImplemented()

        member x.ExplicitInterfaceImplementations = notImplemented()

        member x.HidesBaseMethodsByName = notImplemented()

        member x.IsAsync = notImplemented()

        member x.IsCheckedBuiltin = notImplemented()

        member x.IsExtensionMethod = method.IsExtensionMember

        member x.IsGenericMethod = method.GenericParameters.Count > 0

        member x.IsVararg = notImplemented()

        member x.MethodKind = notImplemented()

        member x.OriginalDefinition = x :> IMethodSymbol

        member x.OverriddenMethod = notImplemented()

        member x.Parameters =
            method.CurriedParameterGroups
            |> Seq.collect id
            |> Seq.indexed
            |> Seq.map(fun (idx, param) -> FSharpParameterSymbol(param, idx) :> IParameterSymbol)
            |> Seq.toImmutableArray

        member x.PartialDefinitionPart = notImplemented()

        member x.PartialImplementationPart = notImplemented()

        member x.ReceiverType = notImplemented()

        member x.ReducedFrom = notImplemented()

        member x.RefCustomModifiers = notImplemented()

        member x.RefKind = notImplemented()

        member x.ReturnsByRef = notImplemented()

        member x.ReturnsByRefReadonly = notImplemented()

        member x.ReturnsVoid = notImplemented()

        member x.ReturnType =
            method.ReturnParameter.Type
            |> typeDefinitionSafe
            |> Option.map(fun e -> namedTypeFromEntity(e) :> ITypeSymbol)
            |> Option.toObj

        member x.ReturnTypeCustomModifiers = notImplemented()

        member x.TypeArguments = notImplemented()

        member x.TypeParameters = notImplemented()

        member x.Construct (typeArguments)= notImplemented()

        member x.GetDllImportData () = notImplemented()

        member x.GetReturnTypeAttributes () = notImplemented()

        member x.GetTypeInferredDuringReduction (reducedFromTypeParameter) = notImplemented()

        member x.ReduceExtensionMethod (receiverType) = notImplemented()

and FSharpNamespaceOrTypeSymbol (entity:FSharpEntity) =
    inherit FSharpISymbol(entity)

    let memberToISymbol (m: FSharpMemberOrFunctionOrValue) : ISymbol =
        match m with
        | _ when m.IsProperty -> FSharpPropertySymbol(m) :> _
        | _ when m.IsMember -> FSharpMethodSymbol(m) :> _
        | _ -> FSharpISymbol(m) :> _

    let getMembers() =
        let fields =
            entity.FSharpFields
            |> Seq.map(fun f -> FSharpFieldSymbol(f) :> ISymbol)

        let mfvs =
            entity.TryGetMembersFunctionsAndValues
            |> Seq.map memberToISymbol
        fields |> Seq.append mfvs

    let getTypeMembers() =
        entity.NestedEntities
        |> Seq.map namedTypeFromEntity

    member x.Entity = entity

    override this.ContainingNamespace =
        let rec firstNamespace (entity:FSharpEntity option) =
            entity
            |> Option.bind(fun e ->
                if e.IsNamespace then Some e else firstNamespace e.DeclaringEntity)

        entity.DeclaringEntity
        |> firstNamespace
        |> Option.map (fun e -> FSharpEntityNamespaceSymbol(e) :> INamespaceSymbol)
        |> Option.toObj
        //// Ideally we would want to be able to fetch the FSharpEntity representing the namespace here
        //// TODO: we can't traverse from NamespaceSymbol to ContainingNamespace
        //// when we construct the NamespaceSymbol this way
        //entity.Namespace
        //|> Option.map (fun n -> FSharpNamespaceSymbol(n, Seq.empty, 0) :> INamespaceSymbol)
        //|> Option.toObj
    interface INamespaceOrTypeSymbol with
        member x.IsNamespace = entity.IsNamespace

        member x.IsType = not entity.IsNamespace && not entity.IsArrayType
            //TODO: && not TypeParameter - how?
        member x.GetMembers () =
            getMembers()
            |> Seq.toImmutableArray

        member x.GetMembers (name) =
            getMembers()
            |> Seq.filter(fun m -> m.Name = name)
            |> Seq.toImmutableArray

        /// Get all the members of this symbol that are types
        member x.GetTypeMembers () =
            getTypeMembers()
            |> Seq.toImmutableArray

        /// Get all the members of this symbol that are types with the given name
        member x.GetTypeMembers (name) =
            getTypeMembers()
            |> Seq.filter(fun m -> m.Name = name)
            |> Seq.toImmutableArray

        /// Get all the members of this symbol that are types with the given name and arity
        member x.GetTypeMembers (name, arity) =
            getTypeMembers()
            |> Seq.filter(fun m -> m.Name = name)
            |> Seq.filter(fun m -> m.Arity = arity)
            |> Seq.toImmutableArray

and FSharpFieldSymbol (field:FSharpField) =
    inherit FSharpSymbolBase()

    override this.Name = field.DisplayName

    override x.Kind = SymbolKind.Field

    interface IFieldSymbol with
        member x.IsReadOnly = notImplemented()
        member x.IsVolatile = notImplemented()
        member x.HasConstantValue =  field.LiteralValue.IsSome
        member x.Type =
            field.FieldType
            |> typeDefinitionSafe
            |> Option.map(fun e -> namedTypeFromEntity(e) :> ITypeSymbol)
            |> Option.toObj
        member x.ConstantValue =
            field.LiteralValue
            |> Option.toObj

        member x.CustomModifiers = notImplemented()

        member x.AssociatedSymbol = notImplemented()
        member x.IsConst = notImplemented()
        member x.OriginalDefinition = x :> IFieldSymbol
        member x.CorrespondingTupleField = notImplemented()

and FSharpPropertySymbol (property:FSharpMemberOrFunctionOrValue) =
    inherit FSharpMemberOrFunctionOrValueSymbol(property)

    override this.Name = property.DisplayName

    override x.Kind = SymbolKind.Property

    interface IPropertySymbol with
        member x.ExplicitInterfaceImplementations = notImplemented()

        member x.GetMethod =
            if property.HasGetterMethod then
                FSharpMethodSymbol(property.GetterMethod) :> _
            else
                null

        member x.IsIndexer = false //TODO:

        member x.IsReadOnly = not property.HasSetterMethod

        member x.IsWithEvents = notImplemented()

        member x.IsWriteOnly = not property.HasGetterMethod

        member x.OriginalDefinition = x :> IPropertySymbol

        member x.OverriddenProperty = notImplemented()

        member x.Parameters = notImplemented()

        member x.RefCustomModifiers = notImplemented()

        member x.RefKind = notImplemented()

        member x.ReturnsByRef = notImplemented()

        member x.ReturnsByRefReadonly = notImplemented()

        member x.SetMethod =
            if property.HasSetterMethod then
                FSharpMethodSymbol(property.SetterMethod) :> _
            else
                null

        member x.Type =
            property.ReturnParameter.Type
            |> typeDefinitionSafe
            |> Option.map(fun e -> namedTypeFromEntity(e) :> ITypeSymbol)
            |> Option.toObj

        member x.TypeCustomModifiers = notImplemented()

and FSharpEntityNamespaceSymbol(entity: FSharpEntity) =
    inherit FSharpNamespaceOrTypeSymbol(entity)
    let nestedSymbols() =
        entity.NestedEntities
        |> Seq.map namespaceOrTypeSymbol

    override x.MetadataName = x.Name

    override x.ToDisplayString(_format) =
        match entity.Namespace with
        | Some ns -> sprintf "%s.%s" ns entity.DisplayName
        | None -> entity.DisplayName

    override x.Equals(other:obj) =
        match other with
        | :? FSharpNamespaceOrTypeSymbol as oth -> entity = oth.Entity
        | :? FSharpNamespaceSymbol as oth ->
            oth.LongNamespaceName = x.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
        | _ -> false

    override x.GetHashCode() = entity.GetHashCode()

    override x.CommonEquals(other) = x.Equals(other)

    interface INamespaceSymbol with
        member x.GetNamespaceMembers () : INamespaceSymbol seq =
            entity.NestedEntities
            |> Seq.filter (fun e -> e.IsNamespace)
            |> Seq.map (fun e -> FSharpEntityNamespaceSymbol(e) :> INamespaceSymbol)

        member x.GetMembers () : INamespaceOrTypeSymbol seq =
            nestedSymbols()

        member x.GetMembers (name) : INamespaceOrTypeSymbol seq =
            nestedSymbols()
            |> Seq.filter(fun s -> s.Name = name)

        member x.ConstituentNamespaces = notImplemented()
        member x.ContainingCompilation = notImplemented()
        member x.IsGlobalNamespace = entity.DisplayName = "global"
        member x.NamespaceKind = notImplemented()

and FSharpNamespaceSymbol (namespaceName: string, entities: FSharpEntity seq, namespaceLevel: int) =
    inherit FSharpSymbolBase()
    let getNamedTypes() =
        entities
        |> Seq.map namedTypeFromEntity

    member x.LongNamespaceName = namespaceName
    override x.Name =
        namespaceName.Split('.')
        |> Seq.last

    override x.DeclaredAccessibility = Accessibility.Public
    override x.Kind = SymbolKind.Namespace
    override x.Equals(other:obj) =
        match other with
        | :? FSharpNamespaceOrTypeSymbol as oth ->
            x.LongNamespaceName = oth.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
        | :? FSharpNamespaceSymbol as oth ->
            oth.LongNamespaceName = x.LongNamespaceName
        | _ -> false

    override x.GetHashCode() = namespaceName.GetHashCode()

    override x.CommonEquals(other) = x.Equals(other)

    override x.ToDisplayString(_format) = namespaceName

    override x.ToString() = namespaceName
    override x.MetadataName = x.Name

    override this.ContainingNamespace = null

    interface INamespaceSymbol with
        member x.ConstituentNamespaces = notImplemented()
        member x.ContainingCompilation = notImplemented()
        member x.IsGlobalNamespace = namespaceName = "global"
        member x.NamespaceKind = notImplemented()
        member x.GetMembers () : ImmutableArray<ISymbol> =
            getNamedTypes()
            |> Seq.cast<ISymbol>
            |> Seq.toImmutableArray

        member x.GetMembers () : INamespaceOrTypeSymbol seq =
            getNamedTypes()
            |> Seq.cast<INamespaceOrTypeSymbol>

        member x.GetMembers (name:string) : INamespaceOrTypeSymbol seq =
            getNamedTypes()
            |> Seq.cast<INamespaceOrTypeSymbol>
            |> Seq.filter(fun t -> t.Name = name)

        member x.GetMembers (name:string) : ImmutableArray<ISymbol> =
            getNamedTypes()
            |> Seq.cast<ISymbol>
            |> Seq.filter(fun t -> t.Name = name)
            |> Seq.toImmutableArray

        member x.GetTypeMembers () =
            getNamedTypes()
            |> Seq.toImmutableArray

        member x.GetTypeMembers (name:string) =
            getNamedTypes()
            |> Seq.filter(fun t -> t.Name = name)
            |> Seq.toImmutableArray

        member x.GetTypeMembers (name:string, arity:int) =
            getNamedTypes()
            |> Seq.filter(fun t -> t.Name = name)
            |> Seq.filter(fun t -> t.Arity = arity)
            |> Seq.toImmutableArray

        member x.GetNamespaceMembers () =
            // e.g. For System.Collections.Generic
            // level 1 - Group entities by 'System'
            // level 2 - Group entities by 'System.Collections'
            // level 3 - Group entities by 'System.Collections.Generic'
            // level 0 - reserved for global namespace
            entities
            |> Seq.groupBy(fun entity ->
                match entity.Namespace with
                | Some ns -> 
                    let namespaceParts = ns.Split('.')
                    if namespaceParts.Length > namespaceLevel then
                        namespaceParts
                        |> Array.take (namespaceLevel + 1)
                        |> String.concat "."
                        |> Some
                    else
                        None
                | None -> None)

            |> Seq.choose(fun (ns, entities) ->
               ns |> Option.map(fun n -> n, entities))
            |> Seq.map (fun (ns, entities) -> 
                FSharpNamespaceSymbol(ns, entities, namespaceLevel+1) :> INamespaceSymbol)
            |> Seq.sortBy(fun n -> n.Name) // Roslyn sorts these

        member x.IsNamespace = true
        member x.IsType = false

and FSharpAssemblySymbol (assembly: FSharpAssemblySignature, name) =
    inherit FSharpSymbolBase()

    new(assembly: FSharpAssembly) =
        FSharpAssemblySymbol(assembly.Contents, assembly.SimpleName)
    override x.Name = name

    override this.GetAttributes () =
        assembly.Attributes
        |> Seq.map(fun attr -> FSharpAttributeData(attr) :> AttributeData)
        |> Seq.toImmutableArray

    override this.ToString() = name

    interface IAssemblySymbol with
        member x.GlobalNamespace =
            let entities =
                assembly.Attributes
                |> Seq.map(fun a -> a.AttributeType)
                |> Seq.append assembly.Entities
            FSharpNamespaceSymbol("global", entities, 0) :> INamespaceSymbol
        member x.Identity =
            //match assembly.FileName with
            //| Some filename ->
            //    let asm = System.Reflection.Assembly.ReflectionOnlyLoadFrom(filename)
            //    AssemblyIdentity.FromAssemblyDefinition asm
            ////TODO: Probably better to instantiate this directly,
            //// but I don't know where to get the information needed to construct
            ////AssemblyIdentity(name,version,cultureName,publicKey,hasPublicKey, isRetargetable,contentType)
            //| None ->
            AssemblyIdentity(name)
        member x.IsInteractive = notImplemented()
        member x.MightContainExtensionMethods = true //TODO: no idea
        member x.Modules = notImplemented()
        member x.NamespaceNames =
            assembly.Entities
            |> Seq.choose(fun entity -> entity.Namespace)
            |> Seq.distinct
            |> Seq.collect(fun ns -> ns.Split('.'))
            |> Seq.distinct
            |> Seq.sort // Roslyn sorts these
            |> Seq.toCollection

        member x.TypeNames =
            assembly.Entities
            |> Seq.map(fun entity -> entity.CompiledName)
            |> Seq.toCollection

        member x.GetMetadata ()= notImplemented()
        member x.GetTypeByMetadataName (fullyQualifiedMetadataName) =
            let path = pathFromFullyQualifiedMetadataName fullyQualifiedMetadataName

            assembly.FindEntityByPath path
            |> Option.map namedTypeFromEntity
            |> Option.toObj

        member x.GivesAccessTo (toAssembly)= notImplemented()
        member x.ResolveForwardedType (fullyQualifiedMetadataName)= notImplemented()
        member x.Kind = SymbolKind.Assembly

and FSharpAttributeData(attribute: FSharpAttribute) =
    inherit AttributeData()

    let getTypeKind(entity:FSharpEntity) =
        let fullName =
            entity.AbbreviatedTypeSafe
            |> Option.bind (fun t -> t.TypeDefinitionSafe)
            |> Option.map (fun typeDefinition -> typeDefinition.FullName)

        match fullName with
        | Some "System.Boolean"
        | Some "System.SByte"
        | Some "System.Int16"
        | Some "System.Int32"
        | Some "System.Int64"
        | Some "System.Byte"
        | Some "System.UInt16"
        | Some "System.UInt32"
        | Some "System.UInt64"
        | Some "System.Single"
        | Some "System.Double"
        | Some "System.Char"
        | Some "System.String"
        | Some "System.Object" ->
            TypedConstantKind.Primitive
        | _ ->
            match entity with
            | _ when entity.IsArrayType -> TypedConstantKind.Array
            | _ when entity.IsEnum -> TypedConstantKind.Enum
            | _ when entity.IsFSharpModule || entity.IsClass -> TypedConstantKind.Type //TODO: no idea
            | _ -> TypedConstantKind.Error

    static let typedConstantCtor =
        typeof<TypedConstant>.GetConstructors(BindingFlags.Instance ||| BindingFlags.NonPublic)
        |> Seq.find(fun c -> c.GetParameters().Length = 3)

    override x.CommonAttributeClass =
        FSharpNamedTypeSymbol(attribute.AttributeType) :> INamedTypeSymbol

    override x.CommonConstructorArguments =
        attribute.ConstructorArguments
        |> Seq.choose (fun (ty, obj) ->
            ty.TypeDefinitionSafe
            |> Option.map(fun entity ->
                let typeSymbol = namedTypeFromEntity(entity)
                let typeKind = getTypeKind entity
                let args = [| box typeSymbol; box typeKind; obj |]
                // Microsoft.CodeAnalysis.TypedConstant has internal constructors - see https://github.com/dotnet/roslyn/issues/25669
                typedConstantCtor.Invoke(args) :?> TypedConstant))
        |> Seq.toImmutableArray

    override x.CommonAttributeConstructor =
        let constructors =
            attribute.AttributeType.MembersFunctionsAndValues
            |> Seq.filter(fun m -> m.CompiledName = ".ctor")

        let argTypes = attribute.ConstructorArguments |> Seq.map fst
        let compareParamTypes(c:FSharpMemberOrFunctionOrValue) =
            let parameters = c.CurriedParameterGroups |> Seq.collect id
            let parameterTypes = parameters |> Seq.map(fun p -> p.Type)
            argTypes.SequenceEqual(parameterTypes)

        let constructor =
            constructors
            |> Seq.find compareParamTypes

        FSharpMethodSymbol(constructor) :> _

    override x.CommonApplicationSyntaxReference = notImplemented()

    override x.CommonNamedArguments =
        attribute.NamedArguments
        |> Seq.choose (fun (ty, nm, isField, obj) ->
            ty.TypeDefinitionSafe
            |> Option.map(fun entity ->
                let typeSymbol = namedTypeFromEntity entity
                let typeKind = getTypeKind entity
                let args = [| box typeSymbol; box typeKind; obj |]
                // Microsoft.CodeAnalysis.TypedConstant has internal constructors - see https://github.com/dotnet/roslyn/issues/25669
                let constant = typedConstantCtor.Invoke(args) :?> TypedConstant
                KeyValuePair(nm, constant)))
        |> Seq.toImmutableArray

and FSharpEntityAttributeData(attribute: FSharpAttribute, entity: FSharpEntity) =
    inherit FSharpAttributeData(attribute)

    override x.CommonApplicationSyntaxReference =
        FSharpSyntaxReference(entity) :> SyntaxReference