namespace MonoDevelop.FSharp

open System
open System.Collections.Immutable
open Mono.TextEditor
open MonoDevelop.Ide.FindInFiles
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.TypeSystem.Implementation
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Utilities to produce NRefactory ISymbol, IEntity, IVariable etc. implementations based on FSharpSymbol
/// objects returned by FSharp.Compiler.Service.
module NRefactory =
    /// If an NRefactory symbol implements this, it indicates it is associated with an FSharpSymbol from the
    /// F# compiler.
    type IHasFSharpSymbol =
        abstract FSharpSymbol : FSharpSymbol
        /// Last identifier used in the resolution that produced this symbol
        abstract LastIdent : string

    /// An NRefactory symbol for an F# type definition, module or exception declaration.
    type FSharpResolvedTypeDefinition(context,unresolvedTypeDef,symbol:FSharpSymbol, lastIdent) =
        inherit DefaultResolvedTypeDefinition(context, [| unresolvedTypeDef |])

        //by default the kind will always be class because unresolvedTypeDef.Kind is used in the DefaultResolvedTypeDefinition.
        //The rename UI only makes a ditinction between class and interface, others such as Enum are seperate entities
        override x.Kind =
            match symbol with
            | :? FSharpEntity as fse when fse.IsInterface -> TypeKind.Interface
            | _ -> base.Kind
        interface IHasFSharpSymbol with
            member x.FSharpSymbol = symbol
            member x.LastIdent = lastIdent

    /// An NRefactory symbol for an F# method, property or other member definition.
    type FSharpResolvedMethod(unresolvedMember:DefaultUnresolvedMethod, context, symbol, lastIdent) =
        inherit DefaultResolvedMethod(unresolvedMember, context)
        interface IHasFSharpSymbol with
            member x.FSharpSymbol = symbol
            member x.LastIdent = lastIdent

    /// An NRefactory symbol for an unresolved F# method, property or other member definition.
    type FSharpUnresolvedMethod(unresolvedTypeDef, name, symbol, lastIdent) =
        inherit DefaultUnresolvedMethod(unresolvedTypeDef, name)
        interface IHasFSharpSymbol with
            member x.FSharpSymbol = symbol
            member x.LastIdent = lastIdent

    /// An NRefactory symbol for a local F# symbol.
    type FSharpResolvedVariable(name, region, symbol, lastIdent) as this =
        interface IHasFSharpSymbol with
            member x.FSharpSymbol = symbol
            member x.LastIdent = lastIdent
        interface ISymbol with
            member x.SymbolKind = SymbolKind.Variable
            member x.Name = name
            member x.ToReference() =
                { new ISymbolReference with
                  member x.Resolve(context) = this :> _}
        interface IVariable with
            member x.Name = name
            member x.Region = region
            member x.Type = (SpecialType.UnknownType :> _)
            member x.IsConst = false
            member x.ConstantValue = null

    /// Build an NRefactory symbol for an F# symbol.
    let createSymbol (fsSymbol: FSharpSymbol, lastIdent, region) =
        match fsSymbol with

        // Type Definitions, Type Abbreviations, Exception Definitions and Modules
        | :? FSharpEntity as fsEntity ->

           // The accessibility used here can influence the scope of a renaming operation.
           // For now we just use 'public' to rename across the whole solution (though really it will just be
           // across the whole project because of the limits of the range of symbols returned by F.C.S).
           let access = Accessibility.Public

           // The 'DefaultUnresolvedTypeDefinition' has an UnresolvedFile property. However it doesn't seem needed.
           // let unresolvedFile = MonoDevelop.Ide.TypeSystem.DefaultParsedDocument(e.DeclarationLocation.FileName)

           // Create a resolution context for the resolved type definition.
           let nsp = match fsEntity.Namespace with None -> "" | Some n -> n
           let unresolvedTypeDef = DefaultUnresolvedTypeDefinition (nsp, Region=region, Name=lastIdent, Accessibility=access (* , UnresolvedFile=unresolvedFile *) )

           // TODO: Add base type references, this will allow 'Go To Base' to work.
           // It may also allow 'Find Derived Types' to work. This would require generating
           // ITypeReference nodes to reference other type definitions in the assembly being edited.
           // That is not easy.
           //
           //unresolvedTypeDef.BaseTypes <- [ for x in fsEntity.DeclaredInterfaces -> Def ]

           // Create an IUnresolveAssembly holding the type definition.
           // For scripts, there is no assembly so avoid errors caused by null in XS
           let assemblyFilename =
                match fsEntity.Assembly.FileName with
                | None -> "fakeassembly.dll"
                | Some n -> n
           let assemblyName = fsEntity.Assembly.QualifiedName
           let unresolvedAssembly = DefaultUnresolvedAssembly(assemblyName, Location = assemblyFilename)
           unresolvedAssembly.AddTypeDefinition(unresolvedTypeDef)

           // We create a fake 'Compilation' for the symbol to contain the unresolvedTypeDef
           //
           // TODO; this is surely not correct, we should be using the compilation retrieved from the
           // appropriate document.  However it doesn't seem to matter in practice.

           let comp = SimpleCompilation(unresolvedAssembly, [||]) //TODO get AssemblyReferences

           // Create a resolution context for the resolved type definition.
           let resolvedAssembly = unresolvedAssembly.Resolve(comp.TypeResolveContext)
           let context = SimpleTypeResolveContext(resolvedAssembly)

           // Finally, create the resolved type definition, which wraps the unresolved one
           let resolvedTypeDef = FSharpResolvedTypeDefinition(context, unresolvedTypeDef, fsSymbol, lastIdent)
           resolvedTypeDef :> ISymbol

        // Members, Module-defined functions and Module-defined values
        | :? FSharpMemberOrFunctionOrValue as fsMember when fsMember.IsModuleValueOrMember && fsMember.CurriedParameterGroups.Count > 0 ->

           let getAssemblyFilename (assembly:FSharpAssembly) =
               match assembly.FileName with
               | Some name -> name
               | _ -> "fakeassembly.dll"


           // This is more or less like the case above for entities.
           let access = Accessibility.Public
           let nsp, name, assemblyFilename =
              match fsMember.EnclosingEntitySafe with
              | Some ent ->
                  let nsp = match ent.Namespace with None -> "" | Some n -> n
                  let name = ent.DisplayName
                  // For scripts, there is no assembly so avoid errors caused by null in XS
                  let assemblyFilename = getAssemblyFilename ent.Assembly
                  nsp, name, assemblyFilename
              | _ -> "", fsMember.DisplayName, getAssemblyFilename fsMember.Assembly

           // We create a fake 'Compilation', 'TypeDefinition' and 'Assembly' for the symbol
           let unresolvedTypeDef = DefaultUnresolvedTypeDefinition (nsp, name, Accessibility=access)

           // We use an IUnresolvedMethod for the symbol regardless of whether it is a property, event,
           // method or function. For the operations we're implementing (Find-all references and rename refactoring)
           // it doesn't seem to matter.
           let unresolvedMember = FSharpUnresolvedMethod(unresolvedTypeDef, fsMember.DisplayName, fsSymbol, lastIdent, Region=region, Accessibility=access)

           let assemblyName = fsMember.Assembly.QualifiedName
           let unresolvedAssembly = DefaultUnresolvedAssembly(assemblyName, Location = assemblyFilename)

           unresolvedTypeDef.Members.Add(unresolvedMember)
           unresolvedAssembly.AddTypeDefinition(unresolvedTypeDef)

           // We create a fake 'Compilation' for the symbol to contain the unresolvedTypeDef
           //
           // TODO; this is surely not correct, we should be using the compilation retrieved from the
           // appropriate document.  However it doesn't seem to matter in practice.
           let comp = SimpleCompilation(unresolvedAssembly, [||]) //TODO projectContent.AssemblyReferences

           // Create a resolution context for the resolved method definition.
           let resolvedAssembly = unresolvedAssembly.Resolve(comp.TypeResolveContext)
           let context = SimpleTypeResolveContext(resolvedAssembly)
           let resolvedTypeDef = DefaultResolvedTypeDefinition(context, unresolvedTypeDef)
           let context = context.WithCurrentTypeDefinition(resolvedTypeDef)

           // Finally, create the resolved method definition, which wraps the unresolved one
           let resolvedMember = FSharpResolvedMethod(unresolvedMember, context, fsSymbol, lastIdent)
           resolvedMember :> ISymbol

        | _ ->
            // All other cases are treated as 'local variables'. This will not be renamed across files.
            FSharpResolvedVariable(lastIdent, region, fsSymbol, lastIdent) :> ISymbol

    /// Create an NRefactory MemberReference for an F# symbol.
    ///
    /// symbolDeclLocOpt is used to modify the MemberReferences ReferenceUsageType in the case of highlight usages
    let createMemberReference(doc:MonoDevelop.Ide.Editor.TextEditor, symbolUse: FSharpSymbolUse, lastIdentAtLoc:string) =
        let start, finish = Symbol.trimSymbolRegion symbolUse lastIdentAtLoc
        let filename = doc.FileName.ToString()
        let offset = doc.LocationToOffset(start.Line, start.Column+1)
        let domRegion = DomRegion(filename, start.Line, start.Column+1, finish.Line, finish.Column+1)

        let symbol = createSymbol(symbolUse.Symbol, lastIdentAtLoc, domRegion)
        let memberRef = MemberReference(symbol, filename, offset, lastIdentAtLoc.Length)

        //if the current range is a symbol range and the fileNameOfRefs match change the ReferenceUsageType
        if symbolUse.IsFromDefinition then
            memberRef.ReferenceUsageType <- ReferenceUsageType.Write

        memberRef

    ///// Create an NRefactory ResolveResult for an F# symbol.
    //let createResolveResult(doc: MonoDevelop.Ide.Gui.Document, fsSymbol: FSharpSymbol, lastIdent, region) =
    //    let sym = createSymbol(doc, fsSymbol, lastIdent, region)
    //    match sym with
    //    | :? IType as ty -> TypeResolveResult(ty) :> ResolveResult
    //    | :? IMember as memb ->
    //        let sym = FSharpResolvedVariable(lastIdent, region, fsSymbol, lastIdent)
    //        let thisRes = LocalResolveResult(sym) :> ResolveResult
    //        MemberResolveResult(thisRes, memb) :> ResolveResult
    //    | _ ->
    //        let sym = FSharpResolvedVariable(lastIdent, region, fsSymbol, lastIdent)
    //        LocalResolveResult(sym) :> ResolveResult

module Roslyn =

    ///Barebones symbol
    type FsharpSymbol (symbolUse:FSharpSymbolUse, locations) =
        interface Microsoft.CodeAnalysis.ISymbol with
            member x.Kind = Microsoft.CodeAnalysis.SymbolKind.Local
            member x.Language = "F#"
            member x.Name = symbolUse.Symbol.DisplayName
            member x.MetadataName = symbolUse.Symbol.FullName
            member x.ContainingSymbol = null //TODO
            member x.ContainingAssembly = null //TODO
            member x.ContainingModule = null //TODO
            member x.ContainingType = null ////TODO for entities or functions this will be available
            member x.ContainingNamespace = null //TODO
            member x.IsDefinition = symbolUse.IsFromDefinition
            member x.IsStatic = false //TODO
            member x.IsVirtual = false //TODO
            member x.IsOverride = false //TODO
            member x.IsAbstract = false //TODO
            member x.IsSealed = false //TODO
            member x.IsExtern = false //TODO
            member x.IsImplicitlyDeclared = false //TODO
            member x.CanBeReferencedByName = true //TODO
            member x.Locations = locations
            member x.DeclaringSyntaxReferences = ImmutableArray.Empty //TODO
            member x.GetAttributes () = ImmutableArray.Empty //TODO
            member x.DeclaredAccessibility = Microsoft.CodeAnalysis.Accessibility.NotApplicable //TOdo
            member x.OriginalDefinition = null //TODO
            member x.Accept (_visitor:Microsoft.CodeAnalysis.SymbolVisitor) = () //TODO
            member x.Accept<'a> (_visitor: Microsoft.CodeAnalysis.SymbolVisitor<'a>) = Unchecked.defaultof<'a>
            member x.GetDocumentationCommentId () = "" //TODO
            member x.GetDocumentationCommentXml (_culture, _expand, _token) = "" //TODO
            member x.ToDisplayString _format = symbolUse.Symbol.DisplayName //TODO format?
            member x.ToDisplayParts _format = ImmutableArray.Empty //TODO
            member x.ToMinimalDisplayString (_semanticModel, _position, _format) = symbolUse.Symbol.DisplayName //TODO format?
            member x.ToMinimalDisplayParts (_semanticModel, _position, _format) = ImmutableArray.Empty //TODO
            member x.HasUnsupportedMetadata = false //TODO
            member x.Equals (other:Microsoft.CodeAnalysis.ISymbol) = x.Equals(other)
