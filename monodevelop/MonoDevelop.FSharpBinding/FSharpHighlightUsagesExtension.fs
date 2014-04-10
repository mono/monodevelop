namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Linq
open Mono.TextEditor
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Ide
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open MonoDevelop.SourceEditor.QuickTasks
open ICSharpCode.NRefactory
open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.TypeSystem.Implementation
open Cairo
open MonoDevelop.SourceEditor.QuickTasks

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
    type FSharpResolvedMethod(unresolvedMember, context, symbol, lastIdent) = 
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
    let createSymbol (projectContent: IProjectContent, fsSymbol: FSharpSymbol, lastIdent, region) = 
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
           let assemblyFilename = match fsEntity.Assembly.FileName with None -> "" | Some n -> n
           let unresolvedAssembly = DefaultUnresolvedAssembly(projectContent.AssemblyName, Location = assemblyFilename)
           unresolvedAssembly.AddTypeDefinition(unresolvedTypeDef)

           // We create a fake 'Compilation' for the symbol to contain the unresolvedTypeDef
           //
           // TODO; this is surely not correct, we should be using the compilation retrieved from the 
           // appropriate document.  However it doesn't seem to matter in practice.
           let comp = SimpleCompilation(unresolvedAssembly, projectContent.AssemblyReferences)

           // Create a resolution context for the resolved type definition.
           let resolvedAssembly = unresolvedAssembly.Resolve(comp.TypeResolveContext)
           let context = SimpleTypeResolveContext(resolvedAssembly)

           // Finally, create the resolved type definition, which wraps the unresolved one
           let resolvedTypeDef = FSharpResolvedTypeDefinition(context, unresolvedTypeDef, fsSymbol, lastIdent)
           resolvedTypeDef :> ISymbol

        // Members, Module-defined functions and Module-definned values
        | :? FSharpMemberFunctionOrValue as fsMember when fsMember.IsModuleValueOrMember -> 

           // This is more or less like the case above for entities.
           let access = Accessibility.Public
           let fsEntity = fsMember.EnclosingEntity

           // We create a fake 'Compilation', 'TypeDefinition' and 'Assembly' for the symbol 
           let nsp = match fsEntity.Namespace with None -> "" | Some n -> n
           let unresolvedTypeDef = DefaultUnresolvedTypeDefinition (nsp, fsEntity.DisplayName, Accessibility=access)

           // We use an IUnresolvedMethod for the symbol regardless of whether it is a property, event, 
           // method or function. For the operations we're implementing (Find-all references and rename refactoring)
           // it doesn't seem to matter.
           let unresolvedMember = FSharpUnresolvedMethod(unresolvedTypeDef, fsMember.DisplayName, fsSymbol, lastIdent, Region=region, Accessibility=access)
           let assemblyFilename = match fsEntity.Assembly.FileName with None -> "" | Some n -> n
           let unresolvedAssembly = DefaultUnresolvedAssembly(projectContent.AssemblyName, Location = assemblyFilename)
           unresolvedTypeDef.Members.Add(unresolvedMember)
           unresolvedAssembly.AddTypeDefinition(unresolvedTypeDef)

           // We create a fake 'Compilation' for the symbol to contain the unresolvedTypeDef
           //
           // TODO; this is surely not correct, we should be using the compilation retrieved from the 
           // appropriate document.  However it doesn't seem to matter in practice.
           let comp = SimpleCompilation(unresolvedAssembly, projectContent.AssemblyReferences)

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
    let createMemberReference(projectContent, symbolUse: FSharpSymbolUse, fileNameOfRef, text, lastIdentAtLoc:string) =
         let m = symbolUse.RangeAlternate
         let ((beginLine, beginCol), (endLine, endCol)) = ((m.StartLine, m.StartColumn), (m.EndLine, m.EndColumn))
         
         // We always know the text of the identifier that resolved to symbol.
         // Trim the range of the referring text to only include this identifier.
         // This means references like A.B.C are trimmed to "C".  This allows renaming to just
         // rename "C". 
         let (beginLine, beginCol) =
             if endCol >=lastIdentAtLoc.Length && (beginLine <> endLine || (endCol-beginCol) >= lastIdentAtLoc.Length) then 
                 (endLine,endCol-lastIdentAtLoc.Length)
             else
                 (beginLine, beginCol)
             
         let document = TextDocument(text)
         let offset = document.LocationToOffset(beginLine, beginCol+1)
         let domRegion = DomRegion(fileNameOfRef, beginLine, beginCol+1, endLine, endCol+1)

         let symbol = createSymbol(projectContent, symbolUse.Symbol, lastIdentAtLoc, domRegion)
         let memberRef = MemberReference(symbol, domRegion, offset, lastIdentAtLoc.Length)

         //if the current range is a symbol range and the fileNameOfRefs match change the ReferenceUsageType
         if symbolUse.FileName = fileNameOfRef && symbolUse.IsFromDefinition then
            memberRef.ReferenceUsageType <- ReferenceUsageType.Write

         memberRef

    /// Create an NRefactory ResolveResult for an F# symbol.
    let createResolveResult(projectContent, fsSymbol: FSharpSymbol, lastIdent, region) =
        let sym = createSymbol(projectContent, fsSymbol, lastIdent, region)
        match sym with 
        | :? IType as ty -> TypeResolveResult(ty) :> ResolveResult
        | :? IMember as memb -> 
            let sym = FSharpResolvedVariable(lastIdent, region, fsSymbol, lastIdent)
            let thisRes = LocalResolveResult(sym) :> ResolveResult
            MemberResolveResult(thisRes, memb) :> ResolveResult
        | _ -> 
            let sym = FSharpResolvedVariable(lastIdent, region, fsSymbol, lastIdent)
            LocalResolveResult(sym) :> ResolveResult


/// MD/XS extension for highlighting the usages of a symbol within the current buffer.
type HighlightUsagesExtension() as this =
    inherit MonoDevelop.SourceEditor.AbstractUsagesExtension<ResolveResult>()
            
    override x.TryResolve(resolveResult) =
        true

    override x.GetReferences(_, token) =
        try
            let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(this.Editor.Caret.Offset, this.Editor.Document)
            let currentFile = FilePath(this.Editor.FileName).ToString()
            let source = this.Editor.Text
            let projectContent = this.Document.ProjectContent

            let projectFilename, files, args, framework = MonoDevelop.getCheckerArgsFromProject(this.Document.Project :?> DotNetProject, IdeApp.Workspace.ActiveConfiguration)

            let symbolReferences =
                Async.RunSynchronously(async{return! MDLanguageService.Instance.GetUsesOfSymbolAtLocationInFile(projectFilename, currentFile, source, files, line, col, lineStr, args, framework)},
                                       cancellationToken = token)

            match symbolReferences with
            | Some(fsSymbolName, references) -> 
                seq{for symbolUse in references do
                        //We only want symbol refs from the current file as we are highlighting text
                        if symbolUse.FileName = currentFile then 
                            yield NRefactory.createMemberReference(projectContent, symbolUse, currentFile, source, fsSymbolName) }
            | _ -> Seq.empty
                            
        with exn -> LoggingService.LogError("Unhandled Exception in F# HighlightingUsagesExtension", exn)
                    Seq.empty       