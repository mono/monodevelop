ADDIN_BUILD = @MD_BUILDDIR@/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.ValaBinding.dll

REFS =  \
	-pkg:glib-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:mono-addins \
	-r:@MD_ADDIN_DIR@/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:@MD_ADDIN_DIR@/MonoDevelop.Deployment/MonoDevelop.Deployment.Linux.dll \
	-r:@MD_ASSEMBLY_DIR@/MonoDevelop.Components.dll \
	-r:@MD_ASSEMBLY_DIR@/MonoDevelop.Core.dll \
	-r:@MD_ASSEMBLY_DIR@/MonoDevelop.Core.Gui.dll \
	-r:@MD_ASSEMBLY_DIR@/MonoDevelop.Ide.dll \
	-r:@MD_ASSEMBLY_DIR@/MonoDevelop.Projects.dll \
	-r:@MD_ASSEMBLY_DIR@/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml
	

FILES =  \
	Compiler/ICompiler.cs \
	Compiler/ValaCompiler.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.ValaBinding.AddLibraryDialog.cs \
	gtk-gui/MonoDevelop.ValaBinding.AddPathDialog.cs \
	gtk-gui/MonoDevelop.ValaBinding.CodeGenerationPanel.cs \
	gtk-gui/MonoDevelop.ValaBinding.GeneralOptionsPanel.cs \
	gtk-gui/MonoDevelop.ValaBinding.OutputOptionsPanel.cs \
	Gui/AddLibraryDialog.cs \
	Gui/AddPathDialog.cs \
	Gui/CodeGenerationPanel.cs \
	Gui/DataProvider.cs \
	Gui/GeneralOptionsPanel.cs \
	Gui/OutputOptionsPanel.cs \
	Gui/ValaTextEditorExtension.cs \
	Project/ProjectPackage.cs \
	Project/ProjectPackageCollection.cs \
	Project/ProjectPackageEventArgs.cs \
	Project/ValaCompilationParameters.cs \
	Project/ValaProject.cs \
	Project/ValaProjectBinding.cs \
	Project/ValaProjectConfiguration.cs \
	Project/ValaProjectServiceExtension.cs \
	Parser/Class.cs \
	Parser/Enumeration.cs \
	Parser/Enumerator.cs \
	Parser/Function.cs \
	Parser/LanguageItem.cs \
	Parser/Local.cs \
	Parser/Macro.cs \
	Parser/Member.cs \
	Parser/Namespace.cs \
	Parser/ProjectInformation.cs \
	Parser/ProjectInformationManager.cs \
	Parser/Structure.cs \
	Parser/Tag.cs \
	Parser/TagDatabaseManager.cs \
	Parser/Typedef.cs \
	Parser/Union.cs \
	Parser/Variable.cs \
	Navigation/ClassNodeBuilder.cs \
	Navigation/ClassPadEventArgs.cs \
	Navigation/EnumerationNodeBuilder.cs \
	Navigation/EnumeratorNodeBuilder.cs \
	Navigation/FunctionNodeBuilder.cs \
	Navigation/GlobalsNodeBuilder.cs \
	Navigation/LanguageItemCommandHandler.cs \
	Navigation/LanguageItemEventArgs.cs \
	Navigation/MacroDefinitionsNodeBuilder.cs \
	Navigation/MacroNodeBuilder.cs \
	Navigation/MemberNodeBuilder.cs \
	Navigation/NamespaceNodeBuilder.cs \
	Navigation/ProjectNodeBuilderExtension.cs \
	Navigation/StructureNodeBuilder.cs \
	Navigation/TypedefNodeBuilder.cs \
	Navigation/UnionNodeBuilder.cs \
	Navigation/VariableNodeBuilder.cs \
	ProjectPad/ProjectReferencesExtension.cs \
	ProjectPad/ProjectResourcesExtension.cs \
	ValaLanguageBinding.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/Icons.16x16.PrivateUnion \
	icons/Icons.16x16.ProjectReference \
	icons/Icons.16x16.ProtectedUnion \
	icons/Icons.16x16.Union \
	icons/text-x-vala.16x16.png \
	icons/text-x-vala.24x24.png \
	icons/text-x-vala.32x32.png \
	icons/text-x-vala.48x48.png \
	icons/vala-icon-32.png \
	templates/ConsoleValaProject.xpt.xml \
	templates/EmptyValaProject.xpt.xml \
	templates/EmptyValaSourceFile.xft.xml \
	templates/SharedLibraryValaProject.xpt.xml \
	ValaBinding.addin.xml

if ENABLE_VALA
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

if ENABLE_VALA
valabindinglibdir = $(prefix)/lib/monodevelop/AddIns/BackendBindings
valabindinglib_DATA = $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

# include $(top_srcdir)/Makefile.include

build_sources = $(addprefix $(srcdir)/, $(FILES)) $(GENERATED_FILES)
build_resources = $(addprefix $(srcdir)/, $(RES))
build_test_sources = $(addprefix $(srcdir)/, $(TESTFILES)) $(GENERATED_FILES)

