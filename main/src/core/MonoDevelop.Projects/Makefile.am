
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Projects.dll
CONFIG = MonoDevelop.Projects.dll.config
  
DEPS =  \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
	$(top_builddir)/build/bin/Mono.Cecil.Mdb.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll

REFS =  \
	$(MONO_ADDINS_LIBS) \
	$(MONODOC_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Data \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	MonoDevelop.Projects.CodeGeneration/BaseRefactorer.cs \
	MonoDevelop.Projects.CodeGeneration/CodeRefactorer.cs \
	MonoDevelop.Projects.CodeGeneration/IRefactorer.cs \
	MonoDevelop.Projects.CodeGeneration/RefactorerContext.cs \
	MonoDevelop.Projects.CodeGeneration/RefactorOperations.cs \
	MonoDevelop.Projects.CodeGeneration/XmlCodeDomReader.cs \
	MonoDevelop.Projects.Dom.MemoryDatabase/AssemblyProjectDom.cs \
	MonoDevelop.Projects.Dom.MemoryDatabase/MemoryDatabase.cs \
	MonoDevelop.Projects.Dom.MemoryDatabase/MemoryProjectDom.cs \
	MonoDevelop.Projects.Dom.MemoryDatabase/NamespaceEntry.cs \
	MonoDevelop.Projects.Dom.MemoryDatabase/ProjectMemoryProjectDom.cs \
	MonoDevelop.Projects.Dom.Output/Ambience.cs \
	MonoDevelop.Projects.Dom.Output/AmbienceService.cs \
	MonoDevelop.Projects.Dom.Output/DocumentationService.cs \
	MonoDevelop.Projects.Dom.Output/NetAmbience.cs \
	MonoDevelop.Projects.Dom.Output/OutputFlags.cs \
	MonoDevelop.Projects.Dom.Output/OutputSettings.cs \
	MonoDevelop.Projects.Dom.Parser/AbstractParser.cs \
	MonoDevelop.Projects.Dom.Parser/AssemblyInformationEventHandler.cs \
	MonoDevelop.Projects.Dom.Parser/CommentTag.cs \
	MonoDevelop.Projects.Dom.Parser/CommentTasksChangedEventHandler.cs \
	MonoDevelop.Projects.Dom.Parser/IParser.cs \
	MonoDevelop.Projects.Dom.Parser/IParserDatabase.cs \
	MonoDevelop.Projects.Dom.Parser/IResolver.cs \
	MonoDevelop.Projects.Dom.Parser/ITypeNameResolver.cs \
	MonoDevelop.Projects.Dom.Parser/ProjectDom.cs \
	MonoDevelop.Projects.Dom.Parser/ProjectDomEventArgs.cs \
	MonoDevelop.Projects.Dom.Parser/ProjectDomService.cs \
	MonoDevelop.Projects.Dom.Parser/TypeNameResolver.cs \
	MonoDevelop.Projects.Dom.Parser/TypeResolverVisitor.cs \
	MonoDevelop.Projects.Dom.Refactoring/Change.cs \
	MonoDevelop.Projects.Dom.Refactoring/Refactoring.cs \
	MonoDevelop.Projects.Dom.Refactoring/RefactoringService.cs \
	MonoDevelop.Projects.Dom.Serialization/AssemblyCodeCompletionDatabase.cs \
	MonoDevelop.Projects.Dom.Serialization/ClassEntry.cs \
	MonoDevelop.Projects.Dom.Serialization/ClassInformationEventHandler.cs \
	MonoDevelop.Projects.Dom.Serialization/CodeCompletionDatabase.cs \
	MonoDevelop.Projects.Dom.Serialization/DatabaseProjectDom.cs \
	MonoDevelop.Projects.Dom.Serialization/DomPersistence.cs \
	MonoDevelop.Projects.Dom.Serialization/DomTypeProxy.cs \
	MonoDevelop.Projects.Dom.Serialization/FileEntry.cs \
	MonoDevelop.Projects.Dom.Serialization/IParserService.cs \
	MonoDevelop.Projects.Dom.Serialization/NamespaceEntry.cs \
	MonoDevelop.Projects.Dom.Serialization/ParserDatabase.cs \
	MonoDevelop.Projects.Dom.Serialization/ProjectCodeCompletionDatabase.cs \
	MonoDevelop.Projects.Dom.Serialization/SimpleCodeCompletionDatabase.cs \
	MonoDevelop.Projects.Dom/AbstractMember.cs \
	MonoDevelop.Projects.Dom/AnonymousType.cs \
	MonoDevelop.Projects.Dom/AttributeTarget.cs \
	MonoDevelop.Projects.Dom/ClassType.cs \
	MonoDevelop.Projects.Dom/Comment.cs \
	MonoDevelop.Projects.Dom/CommentType.cs \
	MonoDevelop.Projects.Dom/CompilationUnit.cs \
	MonoDevelop.Projects.Dom/CompoundType.cs \
	MonoDevelop.Projects.Dom/ConditionalRegion.cs \
	MonoDevelop.Projects.Dom/CopyDomVisitor.cs \
	MonoDevelop.Projects.Dom/DomAttribute.cs \
	MonoDevelop.Projects.Dom/DomCecilAttribute.cs \
	MonoDevelop.Projects.Dom/DomCecilCompilationUnit.cs \
	MonoDevelop.Projects.Dom/DomCecilEvent.cs \
	MonoDevelop.Projects.Dom/DomCecilField.cs \
	MonoDevelop.Projects.Dom/DomCecilMethod.cs \
	MonoDevelop.Projects.Dom/DomCecilParameter.cs \
	MonoDevelop.Projects.Dom/DomCecilProperty.cs \
	MonoDevelop.Projects.Dom/DomCecilType.cs \
	MonoDevelop.Projects.Dom/DomEvent.cs \
	MonoDevelop.Projects.Dom/DomField.cs \
	MonoDevelop.Projects.Dom/DomLocation.cs \
	MonoDevelop.Projects.Dom/DomMemberDecorator.cs \
	MonoDevelop.Projects.Dom/DomMethod.cs \
	MonoDevelop.Projects.Dom/DomParameter.cs \
	MonoDevelop.Projects.Dom/DomProperty.cs \
	MonoDevelop.Projects.Dom/DomRegion.cs \
	MonoDevelop.Projects.Dom/DomReturnType.cs \
	MonoDevelop.Projects.Dom/DomType.cs \
	MonoDevelop.Projects.Dom/DomUsing.cs \
	MonoDevelop.Projects.Dom/Error.cs \
	MonoDevelop.Projects.Dom/ExpressionContext.cs \
	MonoDevelop.Projects.Dom/ExpressionResult.cs \
	MonoDevelop.Projects.Dom/ExtensionMethod.cs \
	MonoDevelop.Projects.Dom/FoldingRegion.cs \
	MonoDevelop.Projects.Dom/IAttribute.cs \
	MonoDevelop.Projects.Dom/ICompilationUnit.cs \
	MonoDevelop.Projects.Dom/IDomVisitor.cs \
	MonoDevelop.Projects.Dom/IEvent.cs \
	MonoDevelop.Projects.Dom/IExpressionFinder.cs \
	MonoDevelop.Projects.Dom/IField.cs \
	MonoDevelop.Projects.Dom/IMember.cs \
	MonoDevelop.Projects.Dom/IMethod.cs \
	MonoDevelop.Projects.Dom/InstantiatedMethod.cs \
	MonoDevelop.Projects.Dom/InstantiatedParameterType.cs \
	MonoDevelop.Projects.Dom/InstantiatedType.cs \
	MonoDevelop.Projects.Dom/IParameter.cs \
	MonoDevelop.Projects.Dom/IProperty.cs \
	MonoDevelop.Projects.Dom/IReturnType.cs \
	MonoDevelop.Projects.Dom/IType.cs \
	MonoDevelop.Projects.Dom/ITypeParameter.cs \
	MonoDevelop.Projects.Dom/IUsing.cs \
	MonoDevelop.Projects.Dom/LocalVariable.cs \
	MonoDevelop.Projects.Dom/Modifiers.cs \
	MonoDevelop.Projects.Dom/Namespace.cs \
	MonoDevelop.Projects.Dom/ParameterModifiers.cs \
	MonoDevelop.Projects.Dom/ParsedDocument.cs \
	MonoDevelop.Projects.Dom/ParsedDocumentEventArgs.cs \
	MonoDevelop.Projects.Dom/PreProcessorDefine.cs \
	MonoDevelop.Projects.Dom/ReferenceEntry.cs \
	MonoDevelop.Projects.Dom/ResolveResult.cs \
	MonoDevelop.Projects.Dom/SearchTypeRequest.cs \
	MonoDevelop.Projects.Dom/Tag.cs \
	MonoDevelop.Projects.Dom/TypeParameter.cs \
	MonoDevelop.Projects.Dom/TypeUpdateInformation.cs \
	MonoDevelop.Projects.Extensions/DataTypeCodon.cs \
	MonoDevelop.Projects.Extensions/DotNetProjectNode.cs \
	MonoDevelop.Projects.Extensions/DotNetProjectSubtypeNode.cs \
	MonoDevelop.Projects.Extensions/FileFormatNode.cs \
	MonoDevelop.Projects.Extensions/IFileFormat.cs \
	MonoDevelop.Projects.Extensions/ISolutionItemHandler.cs \
	MonoDevelop.Projects.Extensions/ItemPropertyCodon.cs \
	MonoDevelop.Projects.Extensions/ItemTypeCondition.cs \
	MonoDevelop.Projects.Extensions/ItemTypeNode.cs \
	MonoDevelop.Projects.Extensions/LanguageBindingCodon.cs \
	MonoDevelop.Projects.Extensions/PolicyNode.cs \
	MonoDevelop.Projects.Extensions/PolicySetNode.cs \
	MonoDevelop.Projects.Extensions/ProjectBindingCodon.cs \
	MonoDevelop.Projects.Extensions/ProjectExtensionUtil.cs \
	MonoDevelop.Projects.Extensions/SerlializationMapNode.cs \
	MonoDevelop.Projects.Extensions/SolutionItemHandler.cs \
	MonoDevelop.Projects.Extensions/SolutionItemNode.cs \
	MonoDevelop.Projects.Formats.MD1/BuildActionDataType.cs \
	MonoDevelop.Projects.Formats.MD1/CmbxFileFormat.cs \
	MonoDevelop.Projects.Formats.MD1/CombineConfiguration.cs \
	MonoDevelop.Projects.Formats.MD1/CombineConfigurationEntry.cs \
	MonoDevelop.Projects.Formats.MD1/CombineConfigurationSet.cs \
	MonoDevelop.Projects.Formats.MD1/CombineStartupMode.cs \
	MonoDevelop.Projects.Formats.MD1/MD1CustomDataItem.cs \
	MonoDevelop.Projects.Formats.MD1/MD1DotNetProjectHandler.cs \
	MonoDevelop.Projects.Formats.MD1/MD1FileFormat.cs \
	MonoDevelop.Projects.Formats.MD1/MD1ProjectService.cs \
	MonoDevelop.Projects.Formats.MD1/MD1SolutionEntityItemHandler.cs \
	MonoDevelop.Projects.Formats.MD1/MD1SolutionItemHandler.cs \
	MonoDevelop.Projects.Formats.MD1/PrjxFileFormat.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionAndExpression.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionExpression.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionFactorExpresion.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionFunctionExpression.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionNotExpression.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionOrExpression.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionParser.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionRelationalExpression.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ConditionTokenizer.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/ExpressionParseException.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/InvalidProjectFileException.cs \
	MonoDevelop.Projects.Formats.MSBuild.Conditions/Token.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildFileFormat.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildHandler.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildImportAttribute.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildProject.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildProjectHandler.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildProjectService.cs \
	MonoDevelop.Projects.Formats.MSBuild/SlnData.cs \
	MonoDevelop.Projects.Formats.MSBuild/SlnFileFormat.cs \
	MonoDevelop.Projects.Policies/DotNetNamingPolicy.cs \
	MonoDevelop.Projects.Policies/PolicyBag.cs \
	MonoDevelop.Projects.Policies/PolicyChangedEventArgs.cs \
	MonoDevelop.Projects.Policies/PolicyService.cs \
	MonoDevelop.Projects.Policies/PolicySet.cs \
	MonoDevelop.Projects.Text/CodeFormatDescription.cs \
	MonoDevelop.Projects.Text/IEditableTextFile.cs \
	MonoDevelop.Projects.Text/IFormatter.cs \
	MonoDevelop.Projects.Text/IPrettyPrinter.cs \
	MonoDevelop.Projects.Text/ITextFile.cs \
	MonoDevelop.Projects.Text/ITextFileProvider.cs \
	MonoDevelop.Projects.Text/TextEncoding.cs \
	MonoDevelop.Projects.Text/TextFile.cs \
	MonoDevelop.Projects.Text/TextFileReader.cs \
	MonoDevelop.Projects.Text/TextFileService.cs \
	MonoDevelop.Projects.Text/TextFormatter.cs \
	MonoDevelop.Projects.Text/XmlDefinitionCodon.cs \
	MonoDevelop.Projects.Utility/DiffUtility.cs \
	MonoDevelop.Projects/AbstractProjectConfiguration.cs \
	MonoDevelop.Projects/BuildAction.cs \
	MonoDevelop.Projects/BuildEventHandler.cs \
	MonoDevelop.Projects/BuildResult.cs \
	MonoDevelop.Projects/BuildTool.cs \
	MonoDevelop.Projects/CombineEntryRenamedEventArgs.cs \
	MonoDevelop.Projects/CombineEventArgs.cs \
	MonoDevelop.Projects/ConfigurationEventHandler.cs \
	MonoDevelop.Projects/ConfigurationParameters.cs \
	MonoDevelop.Projects/ConvertXml.cs \
	MonoDevelop.Projects/CustomCommand.cs \
	MonoDevelop.Projects/CustomCommandCollection.cs \
	MonoDevelop.Projects/CustomCommandExtension.cs \
	MonoDevelop.Projects/CustomCommandType.cs \
	MonoDevelop.Projects/CyclicBuildOrderException.cs \
	MonoDevelop.Projects/DotNetProject.cs \
	MonoDevelop.Projects/DotNetProjectBinding.cs \
	MonoDevelop.Projects/DotNetProjectConfiguration.cs \
	MonoDevelop.Projects/ExecutionContext.cs \
	MonoDevelop.Projects/FileCopyMode.cs \
	MonoDevelop.Projects/FileCopySet.cs \
	MonoDevelop.Projects/FileFormat.cs \
	MonoDevelop.Projects/FileFormatManager.cs \
	MonoDevelop.Projects/GenericProject.cs \
	MonoDevelop.Projects/GenericProjectBinding.cs \
	MonoDevelop.Projects/IBuildTarget.cs \
	MonoDevelop.Projects/IConfigurationTarget.cs \
	MonoDevelop.Projects/IDotNetLanguageBinding.cs \
	MonoDevelop.Projects/IFileItem.cs \
	MonoDevelop.Projects/IFolderItem.cs \
	MonoDevelop.Projects/ILanguageBinding.cs \
	MonoDevelop.Projects/IProjectBinding.cs \
	MonoDevelop.Projects/ItemCollection.cs \
	MonoDevelop.Projects/ItemConfiguration.cs \
	MonoDevelop.Projects/ItemConfigurationCollection.cs \
	MonoDevelop.Projects/IWorkspaceObject.cs \
	MonoDevelop.Projects/LanguageBindingService.cs \
	MonoDevelop.Projects/Project.cs \
	MonoDevelop.Projects/ProjectConvertTool.cs \
	MonoDevelop.Projects/ProjectCreateInformation.cs \
	MonoDevelop.Projects/ProjectEventArgs.cs \
	MonoDevelop.Projects/ProjectFile.cs \
	MonoDevelop.Projects/ProjectFileCollection.cs \
	MonoDevelop.Projects/ProjectFileEventArgs.cs \
	MonoDevelop.Projects/ProjectItem.cs \
	MonoDevelop.Projects/ProjectItemCollection.cs \
	MonoDevelop.Projects/ProjectParameters.cs \
	MonoDevelop.Projects/ProjectPathItemPropertyAttribute.cs \
	MonoDevelop.Projects/ProjectReference.cs \
	MonoDevelop.Projects/ProjectReferenceCollection.cs \
	MonoDevelop.Projects/ProjectReferenceEventArgs.cs \
	MonoDevelop.Projects/ProjectRenameEventArgs.cs \
	MonoDevelop.Projects/ProjectService.cs \
	MonoDevelop.Projects/ProjectServiceExtension.cs \
	MonoDevelop.Projects/ProjectsServices.cs \
	MonoDevelop.Projects/PropertyBag.cs \
	MonoDevelop.Projects/SimpleProjectItem.cs \
	MonoDevelop.Projects/Solution.cs \
	MonoDevelop.Projects/SolutionConfiguration.cs \
	MonoDevelop.Projects/SolutionConfigurationCollection.cs \
	MonoDevelop.Projects/SolutionEntityItem.cs \
	MonoDevelop.Projects/SolutionEventArgs.cs \
	MonoDevelop.Projects/SolutionFolder.cs \
	MonoDevelop.Projects/SolutionFolderItemCollection.cs \
	MonoDevelop.Projects/SolutionItem.cs \
	MonoDevelop.Projects/SolutionItemCollection.cs \
	MonoDevelop.Projects/SolutionItemConfiguration.cs \
	MonoDevelop.Projects/SolutionItemConfigurationCollection.cs \
	MonoDevelop.Projects/SolutionItemEventArgs.cs \
	MonoDevelop.Projects/SolutionItemReference.cs \
	MonoDevelop.Projects/UnknownConfiguration.cs \
	MonoDevelop.Projects/UnknownProjectVersionException.cs \
	MonoDevelop.Projects/UnknownSolutionItem.cs \
	MonoDevelop.Projects/UnknownWorkspaceItem.cs \
	MonoDevelop.Projects/Workspace.cs \
	MonoDevelop.Projects/WorkspaceItem.cs \
	MonoDevelop.Projects/WorkspaceItemCollection.cs \
	MonoDevelop.Projects/WorkspaceItemEventArgs.cs \
	MonoDevelop.Projects/WorkspaceItemRenamedEventArgs.cs

RES =  \
	md1format.xml \
	MonoDevelop.Projects.addin.xml 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	$(CSC) $(CSC_FLAGS) -warnaserror -debug -out:$@ -target:library $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) \
	&& cp $(srcdir)/$(CONFIG) $(top_builddir)/build/bin
	
$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(CONFIG)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) $(CONFIG)

include $(top_srcdir)/Makefile.include
