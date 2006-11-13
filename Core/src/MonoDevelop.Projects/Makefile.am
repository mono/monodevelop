
ASSEMBLY = $(top_builddir)/build/AddIns/MonoDevelop.Projects.dll
CONFIG = MonoDevelop.Projects.dll.config
  
REFS = \
 	-r:System \
 	-r:System.Xml \
 	-r:System.Drawing \
 	-r:Mono.Posix \
 	-r:$(top_builddir)/build/bin/Mono.Cecil.dll \
 	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
 	$(MONODOC_LIBS)

FILES= \
MonoDevelop.Projects.Ambience/Ambience.cs \
MonoDevelop.Projects.Ambience/AmbienceService.cs \
MonoDevelop.Projects.Ambience/CodeDOMGeneratorUtility.cs \
MonoDevelop.Projects.Ambience/IAmbience.cs \
MonoDevelop.Projects.Ambience/NetAmbience.cs \
MonoDevelop.Projects.Deployment/DeployHandler.cs \
MonoDevelop.Projects.Deployment/DeployService.cs \
MonoDevelop.Projects.Deployment/DeployTarget.cs \
MonoDevelop.Projects.Deployment/DirectoryDeployTarget.cs \
MonoDevelop.Projects.Deployment/DirectoryDeployHandler.cs \
MonoDevelop.Projects.Deployment/FileDeployTarget.cs \
MonoDevelop.Projects.Deployment/IDeployHandler.cs \
MonoDevelop.Projects.Deployment/UnknownDeployTarget.cs \
MonoDevelop.Projects/Language.cs \
MonoDevelop.Projects/LanguageService.cs \
MonoDevelop.Projects/LanguageBindingService.cs \
MonoDevelop.Projects.Parser/AssemblyInformationEventHandler.cs \
MonoDevelop.Projects.Parser/AssemblyCodeCompletionDatabase.cs \
MonoDevelop.Projects.Parser/AssemblyInformation.cs \
MonoDevelop.Projects.Parser/ClassEntry.cs \
MonoDevelop.Projects.Parser/ClassInformationEventHandler.cs \
MonoDevelop.Projects.Parser/ClassProxy.cs \
MonoDevelop.Projects.Parser/ClassProxyCollection.cs \
MonoDevelop.Projects.Parser/CodeCompletionDatabase.cs \
MonoDevelop.Projects.Parser/DefaultParserService.cs \
MonoDevelop.Projects.Parser/DatabaseGeneratorTool.cs \
MonoDevelop.Projects.Parser/FileEntry.cs \
MonoDevelop.Projects.Parser/IParserService.cs \
MonoDevelop.Projects.Parser/NamespaceEntry.cs \
MonoDevelop.Projects.Parser/ParseInformation.cs \
MonoDevelop.Projects.Parser/ProjectCodeCompletionDatabase.cs \
MonoDevelop.Projects.Parser/ReferenceEntry.cs \
MonoDevelop.Projects.Parser/SimpleCodeCompletionDatabase.cs \
MonoDevelop.Projects.Parser/ILanguageItem.cs \
MonoDevelop.Projects.Parser/LanguageItemCollection.cs \
MonoDevelop.Projects.Parser/LocalVariable.cs \
MonoDevelop.Projects/CombineEventArgs.cs \
MonoDevelop.Projects/FileFormatManager.cs \
MonoDevelop.Projects/IProjectService.cs \
MonoDevelop.Projects.Parser/ParseInformationEventHandler.cs \
MonoDevelop.Projects/ProjectEventArgs.cs \
MonoDevelop.Projects/ProjectRenameEventArgs.cs \
MonoDevelop.Projects/ProjectService.cs \
MonoDevelop.Projects.Extensions/DataTypeCodon.cs \
MonoDevelop.Projects.Extensions/ItemPropertyCodon.cs \
MonoDevelop.Projects.Extensions/FileFormatCodon.cs \
MonoDevelop.Projects.Utility/Comparers.cs \
MonoDevelop.Projects.Utility/DiffUtility.cs \
MonoDevelop.Projects.Utility/SortUtility.cs \
MonoDevelop.Projects.Utility/SortUtilityBase.cs \
MonoDevelop.Projects.Utility/PriorityQueue.cs \
MonoDevelop.Projects.Parser/ClassType.cs \
MonoDevelop.Projects.Parser/Comment.cs \
MonoDevelop.Projects.Parser/IAttribute.cs \
MonoDevelop.Projects.Parser/IClass.cs \
MonoDevelop.Projects.Parser/IComment.cs \
MonoDevelop.Projects.Parser/ICompilationUnit.cs \
MonoDevelop.Projects.Parser/ICompilationUnitBase.cs \
MonoDevelop.Projects.Parser/IDecoration.cs \
MonoDevelop.Projects.Parser/IEvent.cs \
MonoDevelop.Projects.Parser/IExpressionFinder.cs \
MonoDevelop.Projects.Parser/IField.cs \
MonoDevelop.Projects.Parser/IIndexer.cs \
MonoDevelop.Projects.Parser/IMember.cs \
MonoDevelop.Projects.Parser/IMethod.cs \
MonoDevelop.Projects.Parser/IParameter.cs \
MonoDevelop.Projects.Parser/IParser.cs \
MonoDevelop.Projects.Parser/IProperty.cs \
MonoDevelop.Projects.Parser/IRegion.cs \
MonoDevelop.Projects.Parser/IReturnType.cs \
MonoDevelop.Projects.Parser/IUsing.cs \
MonoDevelop.Projects.Parser/ExpressionContext.cs \
MonoDevelop.Projects.Parser/ModifierEnum.cs \
MonoDevelop.Projects.Parser/Namespace.cs \
MonoDevelop.Projects.Parser/ParameterModifier.cs \
MonoDevelop.Projects.Parser/Tag.cs \
MonoDevelop.Projects.Parser/AttributeCollection.cs \
MonoDevelop.Projects.Parser/AttributeSectionCollection.cs \
MonoDevelop.Projects.Parser/ClassCollection.cs \
MonoDevelop.Projects.Parser/CommentCollection.cs \
MonoDevelop.Projects.Parser/EventCollection.cs \
MonoDevelop.Projects.Parser/FieldCollection.cs \
MonoDevelop.Projects.Parser/IUsingCollection.cs \
MonoDevelop.Projects.Parser/MethodCollection.cs \
MonoDevelop.Projects.Parser/ParameterCollection.cs \
MonoDevelop.Projects.Parser/PropertyCollection.cs \
MonoDevelop.Projects.Parser/TagCollection.cs \
MonoDevelop.Projects.Parser/TypedCSharpCollection.cs \
MonoDevelop.Projects.Parser/AbstractAttribute.cs \
MonoDevelop.Projects.Parser/AbstractClass.cs \
MonoDevelop.Projects.Parser/AbstractComment.cs \
MonoDevelop.Projects.Parser/AbstractCompilationUnit.cs \
MonoDevelop.Projects.Parser/AbstractDecoration.cs \
MonoDevelop.Projects.Parser/AbstractEvent.cs \
MonoDevelop.Projects.Parser/AbstractField.cs \
MonoDevelop.Projects.Parser/AbstractIndexer.cs \
MonoDevelop.Projects.Parser/AbstractMember.cs \
MonoDevelop.Projects.Parser/AbstractMethod.cs \
MonoDevelop.Projects.Parser/AbstractNamedEntity.cs \
MonoDevelop.Projects.Parser/AbstractParameter.cs \
MonoDevelop.Projects.Parser/AbstractProperty.cs \
MonoDevelop.Projects.Parser/AbstractReturnType.cs \
MonoDevelop.Projects.Parser/AbstractUsing.cs \
MonoDevelop.Projects.Parser/DefaultRegion.cs \
MonoDevelop.Projects.Parser/PersistentAttribute.cs \
MonoDevelop.Projects.Parser/PersistentAttributeSection.cs \
MonoDevelop.Projects.Parser/PersistentClass.cs \
MonoDevelop.Projects.Parser/PersistentEvent.cs \
MonoDevelop.Projects.Parser/PersistentField.cs \
MonoDevelop.Projects.Parser/PersistentIndexer.cs \
MonoDevelop.Projects.Parser/PersistentMethod.cs \
MonoDevelop.Projects.Parser/PersistentParameter.cs \
MonoDevelop.Projects.Parser/PersistentProperty.cs \
MonoDevelop.Projects.Parser/PersistentReturnType.cs \
MonoDevelop.Projects.Parser/ReflectionClass.cs \
MonoDevelop.Projects.Parser/ReflectionEvent.cs \
MonoDevelop.Projects.Parser/ReflectionField.cs \
MonoDevelop.Projects.Parser/ReflectionIndexer.cs \
MonoDevelop.Projects.Parser/ReflectionMethod.cs \
MonoDevelop.Projects.Parser/ReflectionParameter.cs \
MonoDevelop.Projects.Parser/ReflectionProperty.cs \
MonoDevelop.Projects.Parser/ReflectionReturnType.cs \
MonoDevelop.Projects.Parser/CommentTasksChangedEventHandler.cs \
MonoDevelop.Projects/CmbxFileFormat.cs \
MonoDevelop.Projects/IFileFormat.cs \
MonoDevelop.Projects/MdpFileFormat.cs \
MonoDevelop.Projects/MdsFileFormat.cs \
MonoDevelop.Projects/PrjxFileFormat.cs \
MonoDevelop.Projects/ProjectPathItemPropertyAttribute.cs \
MonoDevelop.Projects/Combine.cs \
MonoDevelop.Projects/CombineConfiguration.cs \
MonoDevelop.Projects/CombineEntry.cs \
MonoDevelop.Projects/CombineEntryEventArgs.cs \
MonoDevelop.Projects/CombineEntryRenamedEventArgs.cs \
MonoDevelop.Projects/CombineExecuteDefinition.cs \
MonoDevelop.Projects/CyclicBuildOrderException.cs \
MonoDevelop.Projects/NoStartupCombineDefinedException.cs \
MonoDevelop.Projects/AbstractConfiguration.cs \
MonoDevelop.Projects/AbstractProjectConfiguration.cs \
MonoDevelop.Projects/ConvertXml.cs \
MonoDevelop.Projects/DotNetProject.cs \
MonoDevelop.Projects/DotNetProjectBinding.cs \
MonoDevelop.Projects/DotNetProjectConfiguration.cs \
MonoDevelop.Projects/IConfiguration.cs \
MonoDevelop.Projects/IBuildStep.cs \
MonoDevelop.Projects/Project.cs \
MonoDevelop.Projects/BuildEventHandler.cs \
MonoDevelop.Projects/ProjectFile.cs \
MonoDevelop.Projects/ProjectFileEventArgs.cs \
MonoDevelop.Projects/ProjectReference.cs \
MonoDevelop.Projects/ProjectReferenceEventArgs.cs \
MonoDevelop.Projects/TypelibImporter.cs \
MonoDevelop.Projects/UnknownProjectVersionException.cs \
MonoDevelop.Projects/ProjectFileCollection.cs \
MonoDevelop.Projects/ProjectReferenceCollection.cs \
MonoDevelop.Projects.Serialization/ArrayHandler.cs \
MonoDevelop.Projects.Serialization/ArrayListHandler.cs \
MonoDevelop.Projects.Serialization/ClassDataType.cs \
MonoDevelop.Projects.Serialization/CollectionDataType.cs \
MonoDevelop.Projects.Serialization/DataCollection.cs \
MonoDevelop.Projects.Serialization/DataContext.cs \
MonoDevelop.Projects.Serialization/DataIncludeAttribute.cs \
MonoDevelop.Projects.Serialization/DataItem.cs \
MonoDevelop.Projects.Serialization/DataItemAttribute.cs \
MonoDevelop.Projects.Serialization/DataNode.cs \
MonoDevelop.Projects.Serialization/DataSerializer.cs \
MonoDevelop.Projects.Serialization/DataType.cs \
MonoDevelop.Projects.Serialization/DataValue.cs \
MonoDevelop.Projects.Serialization/EnumDataType.cs \
MonoDevelop.Projects.Serialization/ExpandedCollectionAttribute.cs \
MonoDevelop.Projects.Serialization/GenericCollectionHandler.cs \
MonoDevelop.Projects.Serialization/ICollectionHandler.cs \
MonoDevelop.Projects.Serialization/IExtendedDataItem.cs \
MonoDevelop.Projects.Serialization/ItemProperty.cs \
MonoDevelop.Projects.Serialization/ItemPropertyAttribute.cs \
MonoDevelop.Projects.Serialization/PrimitiveDataType.cs \
MonoDevelop.Projects.Serialization/SerializationContext.cs \
MonoDevelop.Projects.Serialization/XmlDataSerializer.cs \
MonoDevelop.Projects.Parser/IParseInformationListener.cs \
MonoDevelop.Projects.CodeGeneration/BaseRefactorer.cs \
MonoDevelop.Projects.CodeGeneration/CodeRefactorer.cs \
MonoDevelop.Projects.CodeGeneration/IRefactorer.cs \
MonoDevelop.Projects.CodeGeneration/RefactorerContext.cs \
MonoDevelop.Projects.CodeGeneration/RefactorOperations.cs \
MonoDevelop.Projects.CodeGeneration/XmlCodeDomReader.cs \
MonoDevelop.Projects.Text/IEditableTextFile.cs \
MonoDevelop.Projects.Text/ITextFile.cs \
MonoDevelop.Projects.Text/ITextFileProvider.cs \
MonoDevelop.Projects.Text/TextFile.cs \
MonoDevelop.Projects.Text/TextEncoding.cs \
MonoDevelop.Projects/ProjectCreateInformation.cs \
MonoDevelop.Projects/IProjectBinding.cs \
MonoDevelop.Projects.Extensions/ProjectBindingCodon.cs \
MonoDevelop.Projects/DefaultCompilerResult.cs \
MonoDevelop.Projects.Extensions/FileFilterCodon.cs \
MonoDevelop.Projects/ICompilerResult.cs \
MonoDevelop.Projects/ILanguageBinding.cs \
MonoDevelop.Projects/IDotNetLanguageBinding.cs \
MonoDevelop.Projects.Extensions/LanguageBindingCodon.cs \
MonoDevelop.Projects/CombineEntryCollection.cs \
MonoDevelop.Projects/ConfigurationCollection.cs \
MonoDevelop.Projects/ConfigurationEventHandler.cs \
MonoDevelop.Projects/ExecutionContext.cs \
MonoDevelop.Projects/BuildTool.cs \
MonoDevelop.Projects/ProjectsServices.cs \
MonoDevelop.Projects.Parser/GenericParameter.cs \
MonoDevelop.Projects.Parser/ReturnTypeList.cs \
MonoDevelop.Projects.Parser/GenericParameterList.cs \
MonoDevelop.Projects.Parser/PersistentGenericParamater.cs \
MonoDevelop.Projects/UnknownCombineEntry.cs \
MonoDevelop.Projects.Documentation/IDocumentationService.cs

GENERATED_FILES = \
AssemblyInfo.cs

ADDIN = MonoDevelop.Projects.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_sources) \
	&& cp $(CONFIG) $(top_builddir)/build/AddIns/.

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(CONFIG)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = $(CONFIG)

EXTRA_DIST = $(FILES) $(CONFIG).in $(ADDIN)

include $(top_srcdir)/Makefile.include
