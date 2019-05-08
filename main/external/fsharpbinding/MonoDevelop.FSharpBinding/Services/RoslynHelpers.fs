namespace MonoDevelop.FSharp

open System
open System.Collections.Immutable
open Microsoft.FSharp.Compiler.SourceCodeServices

module RoslynHelpers =
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
