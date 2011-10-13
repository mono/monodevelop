	
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
	Documentation/IDStringProvider.cs \
	Documentation/XmlDocumentationProvider.cs \
	Editor/IDocument.cs \
	Editor/IDocumentLine.cs \
	Editor/ISegment.cs \
	Editor/ITextAnchor.cs \
	Editor/ITextSource.cs \
	Editor/ReadOnlyDocument.cs \
	Editor/StringTextSource.cs \
	Editor/TextChangeEventArgs.cs \
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
	Role.cs \
	Semantics/AmbiguousResolveResult.cs \
	Semantics/ArrayAccessResolveResult.cs \
	Semantics/ArrayCreateResolveResult.cs \
	Semantics/ByReferenceResolveResult.cs \
	Semantics/ConstantResolveResult.cs \
	Semantics/InvocationResolveResult.cs \
	Semantics/LocalResolveResult.cs \
	Semantics/MemberResolveResult.cs \
	Semantics/NamespaceResolveResult.cs \
	Semantics/ResolveResult.cs \
	Semantics/TypeOfResolveResult.cs \
	Semantics/TypeResolveResult.cs \
	Semantics/UnknownMemberResolveResult.cs \
	TextLocation.cs \
	TypeSystem/Accessibility.cs \
	TypeSystem/ArrayType.cs \
	TypeSystem/ByReferenceType.cs \
	TypeSystem/CecilLoader.cs \
	TypeSystem/DomRegion.cs \
	TypeSystem/EntityType.cs \
	TypeSystem/Error.cs \
	TypeSystem/ExtensionMethods.cs \
	TypeSystem/IAccessor.cs \
	TypeSystem/IAmbience.cs \
	TypeSystem/IAnnotatable.cs \
	TypeSystem/IAttribute.cs \
	TypeSystem/IConstantValue.cs \
	TypeSystem/IDocumentationProvider.cs \
	TypeSystem/IEntity.cs \
	TypeSystem/IEvent.cs \
	TypeSystem/IExplicitInterfaceImplementation.cs \
	TypeSystem/IField.cs \
	TypeSystem/IFreezable.cs \
	TypeSystem/IInterningProvider.cs \
	TypeSystem/IMember.cs \
	TypeSystem/IMethod.cs \
	TypeSystem/Implementation/AbstractFreezable.cs \
	TypeSystem/Implementation/AbstractMember.cs \
	TypeSystem/Implementation/AbstractType.cs \
	TypeSystem/Implementation/BaseTypeCollector.cs \
	TypeSystem/Implementation/CompositeTypeResolveContext.cs \
	TypeSystem/Implementation/CompoundTypeDefinition.cs \
	TypeSystem/Implementation/DefaultAccessor.cs \
	TypeSystem/Implementation/DefaultAttribute.cs \
	TypeSystem/Implementation/DefaultEvent.cs \
	TypeSystem/Implementation/DefaultExplicitInterfaceImplementation.cs \
	TypeSystem/Implementation/DefaultField.cs \
	TypeSystem/Implementation/DefaultMethod.cs \
	TypeSystem/Implementation/DefaultParameter.cs \
	TypeSystem/Implementation/DefaultProperty.cs \
	TypeSystem/Implementation/DefaultTypeDefinition.cs \
	TypeSystem/Implementation/DefaultTypeParameter.cs \
	TypeSystem/Implementation/GetClassTypeReference.cs \
	TypeSystem/Implementation/GetMembersHelper.cs \
	TypeSystem/Implementation/MinimalResolveContext.cs \
	TypeSystem/Implementation/NestedTypeReference.cs \
	TypeSystem/Implementation/ProxyTypeResolveContext.cs \
	TypeSystem/Implementation/SimpleConstantValue.cs \
	TypeSystem/Implementation/SimpleInterningProvider.cs \
	TypeSystem/Implementation/SimpleProjectContent.cs \
	TypeSystem/Implementation/SpecializedEvent.cs \
	TypeSystem/Implementation/SpecializedField.cs \
	TypeSystem/Implementation/SpecializedMember.cs \
	TypeSystem/Implementation/SpecializedMethod.cs \
	TypeSystem/Implementation/SpecializedProperty.cs \
	TypeSystem/Implementation/SubstitutionTypeReference.cs \
	TypeSystem/Implementation/TypeParameterSubstitution.cs \
	TypeSystem/Implementation/TypeStorage.cs \
	TypeSystem/Implementation/TypeWithElementType.cs \
	TypeSystem/Implementation/VoidTypeDefinition.cs \
	TypeSystem/INamedElement.cs \
	TypeSystem/IntersectionType.cs \
	TypeSystem/IParameter.cs \
	TypeSystem/IParameterizedMember.cs \
	TypeSystem/IParsedFile.cs \
	TypeSystem/IProjectContent.cs \
	TypeSystem/IProperty.cs \
	TypeSystem/ISupportsInterning.cs \
	TypeSystem/ISynchronizedTypeResolveContext.cs \
	TypeSystem/IType.cs \
	TypeSystem/ITypeDefinition.cs \
	TypeSystem/ITypeParameter.cs \
	TypeSystem/ITypeReference.cs \
	TypeSystem/ITypeResolveContext.cs \
	TypeSystem/IVariable.cs \
	TypeSystem/KnownTypeReference.cs \
	TypeSystem/NullableType.cs \
	TypeSystem/ParameterizedType.cs \
	TypeSystem/ParameterListComparer.cs \
	TypeSystem/PointerType.cs \
	TypeSystem/ReflectionHelper.cs \
	TypeSystem/ReflectionNameParseException.cs \
	TypeSystem/SharedTypes.cs \
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
	Utils/Platform.cs \
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
