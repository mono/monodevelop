	
ASSEMBLY = $(top_builddir)/build/bin/ICSharpCode.NRefactory.dll
  

REFS =  \
	-r:../../build/bin/Mono.Cecil.dll \
	-r:System \
	-r:System.Core \
	-r:System.Xml \
	-r:System.Xml.Linq
	
SIGNING_KEY=$(srcdir)/ICSharpCode.NRefactory.snk

FILES =  \
	Completion/CompletionCategory.cs \
	Completion/DisplayFlags.cs \
	Completion/ICompletionData.cs \
	Completion/IEntityCompletionData.cs \
	Completion/IParameterDataProvider.cs \
	Completion/IVariableCompletionData.cs \
	Documentation/DocumentationComment.cs \
	Documentation/GetPotentiallyNestedClassTypeReference.cs \
	Documentation/IDocumentationProvider.cs \
	Documentation/IdStringMemberReference.cs \
	Documentation/IdStringProvider.cs \
	Documentation/XmlDocumentationProvider.cs \
	Editor/IDocument.cs \
	Editor/IDocumentLine.cs \
	Editor/ISegment.cs \
	Editor/ITextAnchor.cs \
	Editor/ITextSource.cs \
	Editor/ReadOnlyDocument.cs \
	Editor/StringTextSource.cs \
	Editor/TextChangeEventArgs.cs \
	IAnnotatable.cs \
	PatternMatching/AnyNode.cs \
	PatternMatching/Backreference.cs \
	PatternMatching/BacktrackingInfo.cs \
	PatternMatching/Choice.cs \
	PatternMatching/INode.cs \
	PatternMatching/Match.cs \
	PatternMatching/NamedNode.cs \
	PatternMatching/OptionalNode.cs \
	PatternMatching/Pattern.cs \
	PatternMatching/Repeat.cs \
	Properties/AssemblyInfo.cs \
	Role.cs \
	Semantics/AmbiguousResolveResult.cs \
	Semantics/ArrayAccessResolveResult.cs \
	Semantics/ArrayCreateResolveResult.cs \
	Semantics/ByReferenceResolveResult.cs \
	Semantics/ConstantResolveResult.cs \
	Semantics/Conversion.cs \
	Semantics/ConversionResolveResult.cs \
	Semantics/ErrorResolveResult.cs \
	Semantics/ForEachResolveResult.cs \
	Semantics/InvocationResolveResult.cs \
	Semantics/LocalResolveResult.cs \
	Semantics/MemberResolveResult.cs \
	Semantics/NamespaceResolveResult.cs \
	Semantics/OperatorResolveResult.cs \
	Semantics/ResolveResult.cs \
	Semantics/ThisResolveResult.cs \
	Semantics/TypeIsResolveResult.cs \
	Semantics/TypeOfResolveResult.cs \
	Semantics/TypeResolveResult.cs \
	Semantics/UnknownMemberResolveResult.cs \
	TextLocation.cs \
	TypeSystem/Accessibility.cs \
	TypeSystem/AnonymousType.cs \
	TypeSystem/ArrayType.cs \
	TypeSystem/ByReferenceType.cs \
	TypeSystem/CecilLoader.cs \
	TypeSystem/ComHelper.cs \
	TypeSystem/DomRegion.cs \
	TypeSystem/EntityType.cs \
	TypeSystem/Error.cs \
	TypeSystem/ExtensionMethods.cs \
	TypeSystem/IAmbience.cs \
	TypeSystem/IAssembly.cs \
	TypeSystem/IAttribute.cs \
	TypeSystem/ICompilation.cs \
	TypeSystem/IConstantValue.cs \
	TypeSystem/IEntity.cs \
	TypeSystem/IEvent.cs \
	TypeSystem/IField.cs \
	TypeSystem/IFreezable.cs \
	TypeSystem/IInterningProvider.cs \
	TypeSystem/IMember.cs \
	TypeSystem/IMethod.cs \
	TypeSystem/Implementation/AbstractFreezable.cs \
	TypeSystem/Implementation/AbstractResolvedEntity.cs \
	TypeSystem/Implementation/AbstractResolvedMember.cs \
	TypeSystem/Implementation/AbstractResolvedTypeParameter.cs \
	TypeSystem/Implementation/AbstractType.cs \
	TypeSystem/Implementation/AbstractUnresolvedEntity.cs \
	TypeSystem/Implementation/AbstractUnresolvedMember.cs \
	TypeSystem/Implementation/BaseTypeCollector.cs \
	TypeSystem/Implementation/DefaultAssemblyReference.cs \
	TypeSystem/Implementation/DefaultMemberReference.cs \
	TypeSystem/Implementation/DefaultParameter.cs \
	TypeSystem/Implementation/DefaultResolvedEvent.cs \
	TypeSystem/Implementation/DefaultResolvedField.cs \
	TypeSystem/Implementation/DefaultResolvedMethod.cs \
	TypeSystem/Implementation/DefaultResolvedProperty.cs \
	TypeSystem/Implementation/DefaultResolvedTypeDefinition.cs \
	TypeSystem/Implementation/DefaultResolvedTypeParameter.cs \
	TypeSystem/Implementation/DefaultSolutionSnapshot.cs \
	TypeSystem/Implementation/DefaultUnresolvedAssembly.cs \
	TypeSystem/Implementation/DefaultUnresolvedAttribute.cs \
	TypeSystem/Implementation/DefaultUnresolvedEvent.cs \
	TypeSystem/Implementation/DefaultUnresolvedField.cs \
	TypeSystem/Implementation/DefaultUnresolvedMethod.cs \
	TypeSystem/Implementation/DefaultUnresolvedParameter.cs \
	TypeSystem/Implementation/DefaultUnresolvedProperty.cs \
	TypeSystem/Implementation/DefaultUnresolvedTypeDefinition.cs \
	TypeSystem/Implementation/DefaultUnresolvedTypeParameter.cs \
	TypeSystem/Implementation/DummyTypeParameter.cs \
	TypeSystem/Implementation/ExplicitInterfaceImplementationMemberReference.cs \
	TypeSystem/Implementation/FullNameAndTypeParameterCount.cs \
	TypeSystem/Implementation/GetClassTypeReference.cs \
	TypeSystem/Implementation/GetMembersHelper.cs \
	TypeSystem/Implementation/KnownTypeCache.cs \
	TypeSystem/Implementation/MergedNamespace.cs \
	TypeSystem/Implementation/MinimalCorlib.cs \
	TypeSystem/Implementation/NestedTypeReference.cs \
	TypeSystem/Implementation/SimpleCompilation.cs \
	TypeSystem/Implementation/SimpleConstantValue.cs \
	TypeSystem/Implementation/SimpleInterningProvider.cs \
	TypeSystem/Implementation/SimpleTypeResolveContext.cs \
	TypeSystem/Implementation/SpecializedEvent.cs \
	TypeSystem/Implementation/SpecializedField.cs \
	TypeSystem/Implementation/SpecializedMember.cs \
	TypeSystem/Implementation/SpecializedMethod.cs \
	TypeSystem/Implementation/SpecializedProperty.cs \
	TypeSystem/Implementation/SpecializingMemberReference.cs \
	TypeSystem/Implementation/TypeParameterReference.cs \
	TypeSystem/Implementation/TypeParameterSubstitution.cs \
	TypeSystem/Implementation/TypeWithElementType.cs \
	TypeSystem/Implementation/UnknownType.cs \
	TypeSystem/Implementation/VoidTypeDefinition.cs \
	TypeSystem/INamedElement.cs \
	TypeSystem/INamespace.cs \
	TypeSystem/InheritanceHelper.cs \
	TypeSystem/IntersectionType.cs \
	TypeSystem/IParameter.cs \
	TypeSystem/IParameterizedMember.cs \
	TypeSystem/IParsedFile.cs \
	TypeSystem/IProjectContent.cs \
	TypeSystem/IProperty.cs \
	TypeSystem/ISolutionSnapshot.cs \
	TypeSystem/ISupportsInterning.cs \
	TypeSystem/IType.cs \
	TypeSystem/ITypeDefinition.cs \
	TypeSystem/ITypeParameter.cs \
	TypeSystem/ITypeReference.cs \
	TypeSystem/IVariable.cs \
	TypeSystem/KnownTypeReference.cs \
	TypeSystem/NullableType.cs \
	TypeSystem/ParameterizedType.cs \
	TypeSystem/ParameterListComparer.cs \
	TypeSystem/PointerType.cs \
	TypeSystem/ReflectionHelper.cs \
	TypeSystem/ReflectionNameParseException.cs \
	TypeSystem/SpecialType.cs \
	TypeSystem/TypeKind.cs \
	TypeSystem/TypeVisitor.cs \
	Utils/7BitEncodedInts.cs \
	Utils/BitVector16.cs \
	Utils/BusyManager.cs \
	Utils/CacheManager.cs \
	Utils/CSharpPrimitiveCast.cs \
	Utils/EmptyList.cs \
	Utils/ExtensionMethods.cs \
	Utils/FastSerializer.cs \
	Utils/GraphVizGraph.cs \
	Utils/ImmutableStack.cs \
	Utils/KeyComparer.cs \
	Utils/LazyInit.cs \
	Utils/Platform.cs \
	Utils/ProjectedList.cs \
	Utils/ReferenceComparer.cs \
	Utils/TreeTraversal.cs 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) -keyfile:"$(SIGNING_KEY)" "-define:NET35;FULL_AST;BOOTSTRAP_BASIC" -debug -out:$@ -target:library $(REFS) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(SIGNING_KEY)

include $(top_srcdir)/Makefile.include
